# Website

The public static site is `https://inputstitch.zhihanyu.com/`; source is `website/`.
It is deployed independently of the Windows executable. Website changes do not
require rebuilding or republishing InputStitch.

## Content and visual direction

The October 22 redesign replaces all four pages: overview, download, guide and
404. Use the current diamond identity, a light background, clear type hierarchy,
real application screenshots, short action examples and an interactive crosshair
preview. Keep both Chinese and English, including image alternatives, metadata
and accessible control names. The interface must work at narrow widths and high DPI.

Write for the visitor's next action: download, launch, create, configure, run,
stop or update. Keep UI names consistent with the released application. Explain
requirements next to the affected action. Development history, runtime internals,
test/audit process and defensive commentary belong in repository documentation.
Source downloads are labelled source code; no new licence is declared by the site.

## Release data and assets

`assets/release.json` is a small snapshot of the published Stable release. The
browser can refresh version/date/sizes from the official GitHub Stable endpoint,
with a six-hour local cache and a short timeout. Drafts/Beta do not replace the
Stable display. The local snapshot and permanent website download URLs remain
usable if the external metadata request fails. Refresh the snapshot after a
Stable publication when updating site content.

All download buttons use `download.zhihanyu.com/latest/` aliases, with GitHub
available on the download page. Keep actual published version and asset sizes in
the snapshot. The 1.5.1 screenshot set was rendered from the official x64 EXE
using isolated synthetic configurations; no normal user data/hooks/output were
used. Chinese and English images are lossless WebP. No framework, external font,
analytics library or client-side build dependency is shipped.

## Website-only verification

```powershell
python tests/website/Check-Site.py --online --report artifacts/website-static.json
python tests/website/Serve.py
# Use the printed URL; Node.js with Playwright is a development dependency.
$env:SITE_BASE_URL = 'http://127.0.0.1:<printed-port>'
$env:SITE_TEST_OUTPUT = 'artifacts/website'
node tests/website/BrowserTests.cjs
```

`SITE_TEST_BROWSER` may select an installed Edge/Chromium executable for isolated
headless testing. No user browser profile is used. The full website check covers
four pages, two languages, widths 320/390/768/1024/1440 and device scales 1/2;
inspect representative desktop/mobile screenshots as well. Tests exercise
navigation, keyboard operation, dialog focus, preview controls, language
persistence, published-metadata refresh/failure, blocked storage, no-JS downloads,
image decoding, overflow and button contrast. Run affected website checks for
website edits; the desktop product suite is unrelated.

## Redesign verification — 2026-10-22

Local final results: **278 browser assertions / 40 page-language-width cases**,
**226 static and published-version/size checks**. Zero detected overflow, clipped
text or broken images. The initial page assets total about **83 KB uncompressed**
for Chinese (HTML, CSS, JS, screenshot, diamond and release snapshot). Browser
155.0.4283.62; desktop and mobile screenshots were inspected. No production C#
source or Windows release asset changed. Existing user settings were not used.

Publication is pending authorization for the existing Cloudflare Pages site:
the current OAuth profile can access Workers but lacks `pages:write`; Pages API
returns error 10000. Keep the existing Workers profile intact when authorizing a
separate site-publishing profile. Do not replace this domain with a new site.
