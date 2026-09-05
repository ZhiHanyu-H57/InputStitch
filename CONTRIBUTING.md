# Contributing / 参与贡献

Thank you for helping improve InputStitch. Bug reports, reproducible compatibility findings, documentation improvements, and focused code changes are welcome.

感谢你帮助改进 InputStitch。欢迎提交问题报告、可复现的兼容性发现、文档改进和范围明确的代码修改。

## Before opening an issue / 提交 Issue 前

1. Use the latest public release, currently **1.1.0**. / 使用最新公开正式版，目前为 **1.1.0**。
2. Search existing issues for the same symptom. / 搜索现有 Issue，确认是否已有相同问题。
3. Test with a minimal macro in a harmless application when possible. / 尽可能在无关紧要的程序中用最小宏进行测试。
4. Remove private paths, window titles, process names, profile data, and macro content from screenshots and logs. / 从截图和日志中移除私人路径、窗口标题、进程名、方案数据和宏内容。

A useful bug report includes Windows edition and architecture, display scaling, exact reproduction steps, expected behavior, actual behavior, and whether Emergency Stop still works.

一份有效的问题报告应包含 Windows 版本和架构、显示缩放比例、准确复现步骤、预期行为、实际行为，以及紧急停止是否仍然有效。

For vulnerabilities, follow [SECURITY.md](SECURITY.md) instead of posting exploit details publicly.  
若涉及安全漏洞，请按照 [SECURITY.md](SECURITY.md) 报告，不要公开发布利用细节。

## Pull requests / 拉取请求

- Keep each pull request focused on one problem. / 每个拉取请求只解决一个明确问题。
- Preserve the Simplified Chinese and English interface; add or update both translations for user-visible text. / 保持简体中文和 English 双语界面；新增或修改用户可见文本时，同时维护两种语言。
- Preserve global Emergency Stop priority and input-release safety. / 保持全局紧急停止的最高优先级和输入释放安全性。
- Do not weaken shortcut-conflict protection without tests covering both safety and ordinary held-modifier gameplay. / 不要在缺少安全场景与正常按住修饰键游戏场景测试的情况下削弱快捷键冲突防护。
- Keep UI layouts usable at common display scaling values and with longer English text. / 保持界面在常见显示缩放比例和较长英文文本下可用。
- Avoid unrelated formatting or generated-file changes. / 避免无关的格式化或生成文件变更。
- Update documentation and tests when behavior changes. / 行为发生变化时，同步更新文档和测试。

## Build and verify / 构建与验证

Build on Windows with the .NET Framework 4.7.2 Developer Pack and compatible Visual Studio or Build Tools:

请在 Windows 上使用 .NET Framework 4.7.2 Developer Pack 和兼容的 Visual Studio 或 Build Tools 构建：

```powershell
.\build.ps1
```

Before submitting, verify at least / 提交前至少确认：

- The solution builds for the affected x64 or x86 target. / 受影响的 x64 或 x86 目标能够成功构建。
- Simplified Chinese and English layouts remain readable at 100%, 125%, and 150% scaling. / 简体中文和 English 布局在 100%、125% 和 150% 缩放下仍然清晰可读。
- Starting, stopping, retriggering, recording, and Emergency Stop behave correctly. / 启动、停止、再次触发、录制和紧急停止行为正确。
- Macro-held keyboard and mouse inputs are released after stopping or an error. / 停止或发生错误后，宏按住的键盘和鼠标输入均会释放。
- No private configuration, logs, binaries, or machine-specific files were added. / 没有加入私人配置、日志、二进制文件或仅与本机有关的文件。

## License notice / 许可证说明

This repository currently does not declare an open-source license. A contribution does not change that status. Do not submit code or assets that you do not have the right to contribute.

本仓库目前没有声明开源许可证，提交贡献不会改变这一状态。请勿提交你无权贡献的代码或素材。
