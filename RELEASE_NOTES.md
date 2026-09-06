# InputStitch 1.1.1-beta.1

## 简体中文

这是第一批稳定性改进的测试版，**不是推荐所有用户升级的正式版**。v1.1.0 保持为最新正式版，现有自动更新不会提示安装此 Beta。

- 闲置手柄输入新增仅负责释放的后台定时器：界面不再刷新时，也能释放已经发出的脉冲。
- 新增回调代次/所有权检查，防止旧定时器误释放新输入；宏启动、紧急停止、退出仍同步清理已有脉冲。
- 回归测试进入工程和 GitHub Actions，包括 1 万次模拟启停、界面停顿、过期回调、键盘/快捷键和双语设置检查。
- 版本信息由 ReleaseInfo.cs 集中提供，Beta 使用独立校验清单与指定版本下载链接。

**更新方式：** 本 Beta 不自动联网检查更新。手动点击“检查更新”并确认后打开 GitHub Releases，由用户自行下载和替换。旧正式版的手动检查仍只检查正式更新。

**测试前：** 退出正在运行的 InputStitch，备份 `%APPDATA%\InputStitch`，将 Beta 与正式版 EXE 分开放置。两者目前共用配置，不要同时运行。Beta 是公开可见的预发布版，不是私有发布。

**边界：** 已做注入测试，不代表真实硬件或 GTA Online 后台兼容性实测通过；Windows 调度和底层驱动阻塞仍可能延迟释放。仍需要 ViGEmBus 才能使用手柄功能；不会静默安装驱动。EXE 未签名。请遵守所用软件/游戏的自动化规则。

## English

This is the first reliability Beta, **not a stable upgrade recommended to all users**. v1.1.0 remains Latest; existing automatic updates will not offer this Beta.

- Release-only background deadlines neutralize an owned idle pulse even when UI ticks stop.
- Generation/ownership checks keep stale callbacks from releasing newer input. Macro handoff, Emergency Stop and shutdown retain synchronous pulse cleanup.
- Repository/CI tests include 10,000 injected start/cancel cycles, stalled UI ticks, stale callbacks, keyboard/shortcut regression and bilingual settings smoke tests.
- Release metadata is centralized in ReleaseInfo.cs. Beta uses a separate checksum manifest and version-pinned download URLs.

**Updates:** this Beta performs no automatic update check. Manual Check, after confirmation, opens GitHub Releases for manual downloading/replacement. Old stable versions still check stable updates only, including manual checks.

**Before testing:** close InputStitch, back up `%APPDATA%\InputStitch`, and keep Beta/stable EXEs separately. They currently share configuration; run one at a time. This is a public pre-release, not a private release.

**Limitations:** injected tests are not real hardware/GTA Online background compatibility tests. Windows scheduling and blocked native drivers can still delay release. Gamepad output requires ViGEmBus; no silent driver installation. EXEs are unsigned. Follow the target software/game's automation rules.

## Files / 文件

- `InputStitch-1.1.1-beta.1-Windows-x64.exe`
- `InputStitch-1.1.1-beta.1-Windows-x86.exe`
- `InputStitch-1.1.1-beta.1-Source.zip`
- `InputStitch-beta.xml` — Beta-only manifest, not used by stable updaters / 仅测试版清单
- `SHA256SUMS.txt`
