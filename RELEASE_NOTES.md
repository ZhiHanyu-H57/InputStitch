# InputStitch 1.1.1-beta.2

## 简体中文

公开测试版；**v1.1.0 正式版及自动更新入口保持不变**。请手动下载；旧 Beta 发行记录不覆盖。

### Shift 与手柄同时工作

独立 Shift（左、右或通用）在“按住触发”模式下，如果宏全部为手柄步骤，现在会把原始 Shift 同时传给游戏，不受原先勾选的“屏蔽触发键”影响。界面显示“Shift 同时传给游戏（按住映射手柄）”。摇杆前推与游戏奔跑键可同时输出；其他模式仍保留原来的屏蔽偏好。

修复了已确认的屏蔽冲突，**尚未在真实 GTA 游戏中验收**。请退出旧版，单独启动此 Beta，使用原先的 Shift/左摇杆宏，测试按住时移动、松开时停止和紧急停止。若仍无效果，请报告宏状态是空闲、执行中还是暂停。

### 更新安装保护

安装器改为暂存校验后替换，保留同目录唯一的旧 EXE 备份（*.previous.exe），并将主 config.xml 快照保存到配置目录 backups/update-*/config.xml。旧进程未退出则不替换；替换或启动失败时尝试恢复旧 EXE。恢复失败会提示备份路径并保留备份。

Beta 仍不自动检查或安装更新，手动检查经确认打开 GitHub；手动下载此 Beta **不会调用此安装器或创建更新备份**。这些底层改进为后续正式版准备。只备份主配置，不含 profiles/macro-packages；不含新版成功启动后又崩溃的自动回退、断电恢复或代码签名。

### 验证与注意

- 292 项键盘检查（含 30 项新增 Shift 检查）、51 项闲置手柄断言、修饰键安全回归、43 项更新故障检查，以及双语设置和手柄参数检查。
- 更新测试仅操作专用临时文件，不启动程序、不触碰用户配置。游戏/驱动兼容性需复测。
- Beta 与正式版共用配置。测试前退出 InputStitch，备份整个 %APPDATA%\InputStitch，保留旧 EXE，勿同时运行。
- EXE 未签名；手柄输出需要 ViGEmBus。请遵守目标软件和游戏规则。

## English

Public opt-in Beta. **Stable v1.1.0 and its automatic update endpoint are unchanged.**

- Standalone Shift in Hold mode with gamepad-only steps now passes the native key through to preserve sprint alongside controller output. The bilingual suppression control explains the exception; other modes retain the stored preference. This fixes the confirmed suppression conflict; GTA compatibility still needs user testing.
- Installer: verified staging, unique adjacent *.previous.exe backup, main config.xml snapshot under backups/update-*, original-process exit check, executable recovery on replacement/launch failure. Failed recovery retains the backup and reports its location.
- Beta remains manual-download-only. Manual Check opens GitHub after confirmation; downloading Beta does not invoke the installer or create its backups. These changes prepare a future stable release. Profiles/packages, post-launch crashes, power-loss recovery and signing are not covered.
- Tested: 292 keyboard checks, 51 idle assertions, modifier safety, 43 updater checks on temporary files with injected faults, bilingual settings and saved gamepad vectors. No real game/hardware validation claimed.
- Before testing, close InputStitch, back up the whole %APPDATA%\InputStitch folder and keep the old EXE. Versions share configuration. Unsigned EXEs; ViGEmBus required for gamepad output. Follow target software/game rules.

## Files / 文件

- InputStitch-1.1.1-beta.2-Windows-x64.exe
- InputStitch-1.1.1-beta.2-Windows-x86.exe
- InputStitch-1.1.1-beta.2-Source.zip
- InputStitch-beta.xml
- SHA256SUMS.txt
