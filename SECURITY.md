# Security Policy / 安全政策

## Supported version / 支持版本

Security fixes are provided for the latest public release, currently **1.0.0**.  
安全修复面向最新公开正式版，目前为 **1.0.0**。

## Reporting a vulnerability / 报告安全问题

Please do not publish an exploitable vulnerability as a public issue before the maintainer has had a reasonable opportunity to investigate it. Use GitHub's **Report a vulnerability** private reporting feature when it is available for this repository. If private reporting is unavailable, open a minimal issue asking the maintainer for a private contact channel, without including exploit details or sensitive data.

请不要在维护者获得合理调查时间之前，把可被利用的漏洞细节作为公开 Issue 发布。如果本仓库启用了 GitHub 的 **Report a vulnerability** 私密报告功能，请优先使用。若私密报告不可用，可以提交一条不含漏洞细节和敏感信息的简短 Issue，请维护者提供私密联系方式。

Include, when possible / 建议提供：

- A concise description of the impact / 简明的影响说明
- Affected Windows edition, architecture, and InputStitch version / 受影响的 Windows 版本、架构和 InputStitch 版本
- Reproduction steps or a minimal proof of concept / 复现步骤或最小验证示例
- Relevant logs with personal paths, window titles, process names, and macro content removed / 已移除个人路径、窗口标题、进程名和宏内容的相关日志
- Any known workaround / 已知的临时规避方式

## Scope / 范围

Examples of relevant reports include unsafe parsing of imported files, unintended privilege or file access, failure of Emergency Stop caused by a reproducible application defect, and leakage of local configuration data.

适合报告的问题包括：导入文件解析不安全、意外的权限或文件访问、可稳定复现且由程序缺陷引起的紧急停止失效，以及本地配置数据泄露。

Game rules, anti-cheat decisions, antivirus false positives without technical evidence, and general compatibility questions are not security vulnerabilities. They may still be reported as ordinary issues.

游戏规则、反作弊判定、没有技术证据的杀毒软件误报，以及一般兼容性问题不属于安全漏洞，但仍可作为普通 Issue 提交。

## Safe handling / 安全处理

Do not upload private profiles, macro packages, configuration files, or raw diagnostics unless you have reviewed and sanitized them. These files may reveal application names, window titles, local paths, and user-created macro content.

未经检查和脱敏，请勿上传私人配置方案、宏包、配置文件或原始诊断信息。这些文件可能泄露程序名称、窗口标题、本地路径和用户创建的宏内容。
