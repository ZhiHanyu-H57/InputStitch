#!/usr/bin/env python3
"""Offline verification for the derived R2 Stable update manifest."""

from __future__ import annotations

import argparse
import importlib.util
from pathlib import Path
import tempfile
import xml.etree.ElementTree as ET


def load_publisher(script_path: Path):
    spec = importlib.util.spec_from_file_location("inputstitch_publish_r2", script_path)
    if spec is None or spec.loader is None:
        raise RuntimeError("Could not load Publish-R2.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest", required=True, type=Path)
    parser.add_argument("--version", required=True)
    args = parser.parse_args()

    manifest = args.manifest.resolve()
    if not manifest.is_file():
        raise RuntimeError(f"Manifest does not exist: {manifest}")

    publisher = load_publisher(Path(__file__).with_name("Publish-R2.py"))
    source_tree = ET.parse(manifest)
    source_root = source_tree.getroot()
    original = {
        (asset.get("Architecture") or "").lower(): {
            "FileName": asset.get("FileName") or "",
            "Sha256": asset.get("Sha256") or "",
            "Url": asset.get("Url") or "",
        }
        for asset in source_root.findall("Asset")
    }

    with tempfile.TemporaryDirectory(prefix="inputstitch-r2-manifest-") as temp_dir:
        derived = Path(temp_dir) / "InputStitch-update.xml"
        publisher.build_r2_stable_manifest(manifest, derived, args.version)
        root = ET.parse(derived).getroot()

    if (root.findtext("Version") or "").strip() != args.version:
        raise RuntimeError("Derived R2 manifest changed or lost the release version.")
    release_url = (root.findtext("ReleaseUrl") or "").strip()
    if release_url != f"https://github.com/ZhiHanyu-H57/InputStitch/releases/tag/v{args.version}":
        raise RuntimeError("Derived R2 manifest changed the canonical release-notes URL.")

    assets = root.findall("Asset")
    if len(assets) != 2:
        raise RuntimeError("Derived R2 manifest must contain exactly two assets.")
    for architecture in ("x64", "x86"):
        candidates = [a for a in assets if (a.get("Architecture") or "").lower() == architecture]
        if len(candidates) != 1:
            raise RuntimeError(f"Derived R2 manifest has invalid {architecture} asset entries.")
        asset = candidates[0]
        expected_name = f"InputStitch-{args.version}-Windows-{architecture}.exe"
        expected_url = f"https://download.zhihanyu.com/releases/v{args.version}/{expected_name}"
        if asset.get("FileName") != expected_name:
            raise RuntimeError(f"Derived R2 {architecture} file name is invalid.")
        if asset.get("Url") != expected_url:
            raise RuntimeError(f"Derived R2 {architecture} URL is invalid.")
        source = original.get(architecture)
        if source is None or asset.get("Sha256") != source["Sha256"]:
            raise RuntimeError(f"Derived R2 {architecture} SHA-256 changed unexpectedly.")
        if not source["Url"].startswith("https://github.com/ZhiHanyu-H57/InputStitch/releases/"):
            raise RuntimeError("Canonical Stable manifest is no longer GitHub-compatible for legacy clients.")

    print("PASS R2 Stable manifest derivation: GitHub legacy manifest preserved; R2 latest uses version-pinned website assets.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
