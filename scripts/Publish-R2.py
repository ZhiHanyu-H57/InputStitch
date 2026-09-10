#!/usr/bin/env python3
"""Mirror verified InputStitch release assets to Cloudflare R2.

Versioned objects are immutable: an existing key must carry the same sha256
metadata or the upload is refused. Stable publications also refresh fixed
/latest aliases so the website can keep permanent download URLs.
"""

from __future__ import annotations

import argparse
import hashlib
import mimetypes
import os
from pathlib import Path
import sys

import boto3
from botocore.exceptions import ClientError


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def read_checksums(path: Path) -> dict[str, str]:
    result: dict[str, str] = {}
    for raw in path.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if not line:
            continue
        parts = line.split(maxsplit=1)
        if len(parts) != 2:
            raise RuntimeError(f"Invalid checksum line: {raw!r}")
        digest, name = parts
        result[name.lstrip("*")] = digest.lower()
    return result


def content_type(path: Path) -> str:
    suffix = path.suffix.lower()
    if suffix == ".exe":
        return "application/vnd.microsoft.portable-executable"
    if suffix == ".zip":
        return "application/zip"
    if suffix == ".xml":
        return "application/xml; charset=utf-8"
    if suffix == ".txt":
        return "text/plain; charset=utf-8"
    return mimetypes.guess_type(path.name)[0] or "application/octet-stream"


def head_object(client, bucket: str, key: str):
    try:
        return client.head_object(Bucket=bucket, Key=key)
    except ClientError as exc:
        code = str(exc.response.get("Error", {}).get("Code", ""))
        status = exc.response.get("ResponseMetadata", {}).get("HTTPStatusCode")
        if code in {"404", "NoSuchKey", "NotFound"} or status == 404:
            return None
        raise


def put_object(
    client,
    bucket: str,
    key: str,
    path: Path,
    digest: str,
    *,
    cache_control: str,
    immutable: bool,
    download_name: str | None = None,
) -> None:
    existing = head_object(client, bucket, key)
    if immutable and existing is not None:
        existing_digest = (existing.get("Metadata") or {}).get("sha256", "").lower()
        if existing_digest == digest:
            print(f"unchanged  s3://{bucket}/{key}")
            return
        raise RuntimeError(
            f"Refusing to overwrite immutable R2 object {key}: "
            f"existing sha256={existing_digest or '<missing>'}, expected={digest}"
        )

    kwargs = {
        "Bucket": bucket,
        "Key": key,
        "Body": path.open("rb"),
        "ContentType": content_type(path),
        "CacheControl": cache_control,
        "Metadata": {"sha256": digest},
    }
    if download_name:
        kwargs["ContentDisposition"] = f'attachment; filename="{download_name}"'

    try:
        client.put_object(**kwargs)
    finally:
        kwargs["Body"].close()
    print(f"uploaded   s3://{bucket}/{key}")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--dist", required=True, type=Path)
    parser.add_argument("--version", required=True)
    parser.add_argument("--channel", required=True, choices=("stable", "beta"))
    parser.add_argument("--bucket", default="inputstitch-downloads")
    parser.add_argument("--endpoint", required=True)
    args = parser.parse_args()

    dist = args.dist.resolve()
    if not dist.is_dir():
        raise RuntimeError(f"Release asset directory does not exist: {dist}")

    version = args.version.removeprefix("v")
    manifest_name = "InputStitch-update.xml" if args.channel == "stable" else "InputStitch-beta.xml"
    expected_names = [
        f"InputStitch-{version}-Windows-x64.exe",
        f"InputStitch-{version}-Windows-x86.exe",
        f"InputStitch-{version}-Source.zip",
        manifest_name,
        "SHA256SUMS.txt",
    ]
    paths = {name: dist / name for name in expected_names}
    missing = [name for name, path in paths.items() if not path.is_file()]
    if missing:
        raise RuntimeError("Missing release assets: " + ", ".join(missing))

    listed = read_checksums(paths["SHA256SUMS.txt"])
    digests: dict[str, str] = {}
    for name, path in paths.items():
        digest = sha256_file(path)
        digests[name] = digest
        if name != "SHA256SUMS.txt":
            expected = listed.get(name)
            if expected != digest:
                raise RuntimeError(
                    f"Checksum mismatch for {name}: SHA256SUMS={expected!r}, actual={digest}"
                )

    access_key = os.environ.get("AWS_ACCESS_KEY_ID")
    secret_key = os.environ.get("AWS_SECRET_ACCESS_KEY")
    if not access_key or not secret_key:
        raise RuntimeError("R2 S3 credentials are missing from the environment.")

    client = boto3.client(
        "s3",
        endpoint_url=args.endpoint.rstrip("/"),
        region_name="auto",
        aws_access_key_id=access_key,
        aws_secret_access_key=secret_key,
    )

    version_prefix = f"releases/v{version}"
    for name, path in paths.items():
        put_object(
            client,
            args.bucket,
            f"{version_prefix}/{name}",
            path,
            digests[name],
            cache_control="public, max-age=31536000, immutable",
            immutable=True,
            download_name=name,
        )

    if args.channel == "stable":
        aliases = {
            f"InputStitch-{version}-Windows-x64.exe": "InputStitch-Windows-x64.exe",
            f"InputStitch-{version}-Windows-x86.exe": "InputStitch-Windows-x86.exe",
            f"InputStitch-{version}-Source.zip": "InputStitch-Source.zip",
            "InputStitch-update.xml": "InputStitch-update.xml",
            "SHA256SUMS.txt": "SHA256SUMS.txt",
        }
        for source_name, alias_name in aliases.items():
            put_object(
                client,
                args.bucket,
                f"latest/{alias_name}",
                paths[source_name],
                digests[source_name],
                cache_control="public, max-age=300",
                immutable=False,
                download_name=source_name,
            )

        version_path = dist / ".r2-version.txt"
        version_path.write_text(version + "\n", encoding="utf-8")
        try:
            put_object(
                client,
                args.bucket,
                "latest/version.txt",
                version_path,
                sha256_file(version_path),
                cache_control="public, max-age=300",
                immutable=False,
                download_name=None,
            )
        finally:
            version_path.unlink(missing_ok=True)

    print(f"R2 mirror complete: channel={args.channel}, version={version}, bucket={args.bucket}")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        sys.exit(1)
