# InputStitch 1.4.2

## 一句话看懂

**正式版更新链现在优先使用 `download.zhihanyu.com`，官网不可用时自动回退到官方 GitHub Release；同时完成一轮不改变宏行为的内部架构整理。**

1.4.2 不修改配置格式，不改变宏、Held Mapping、Layer、Output Ownership、Router 或手柄接管的既有语义。Controller Takeover 仍然是 Experimental。

## 更新下载链路

### 1. 官网成为 Stable 更新主源

Stable 版现在先从：

`https://download.zhihanyu.com/latest/InputStitch-update.xml`

检查更新。R2 的 `/latest` 清单会把 x64/x86 可执行文件指向对应版本的不可变对象，例如：

`https://download.zhihanyu.com/releases/v1.4.2/InputStitch-1.4.2-Windows-x64.exe`

这样从官网下载 InputStitch、程序内检查更新、下载新版三段链路都不再要求 GitHub 可访问。

### 2. GitHub 仍然作为自动 fallback

如果官网更新清单发生网络错误、解析失败或安全校验失败，新版会自动尝试官方 GitHub Stable 清单。下载 EXE 时同样先使用官网版本化地址；如果下载、SHA-256 或产品/版本验证失败，则自动尝试同一版本的 GitHub Release 文件。

只有官网和 GitHub 都失败后，才进入原有“检查更新失败 / 下载更新失败”处理。

### 3. 保留 1.4.1 → 1.4.2 的兼容升级路径

1.4.1 的更新器只接受 GitHub Release 资产 URL。因此 1.4.2 发布时，GitHub Release 中的 `InputStitch-update.xml` 仍保留 GitHub 资产 URL；R2 镜像阶段会单独派生 `/latest/InputStitch-update.xml`，只在这份 R2 清单中改为 `download.zhihanyu.com` 资产 URL。

这保证旧 1.4.1 仍能通过历史 GitHub 逻辑升级到 1.4.2，而 1.4.2 及之后版本使用新的官网优先双源策略。

### 4. 更新安全边界没有放宽

- 仍只接受官方 `download.zhihanyu.com` 和 `github.com/ZhiHanyu-H57/InputStitch` 更新资产。
- 文件名必须严格匹配清单版本和当前架构。
- 下载后仍必须通过 SHA-256 校验。
- 下载文件的 `ProductName` / `ProductVersion` 仍必须与预期一致。
- GitHub fallback 固定到已经检查到的具体版本，不使用可变化的 `latest` EXE 地址。

## 内部架构整理

这轮同时完成了几项以保持行为不变为目标的重构：

- 将重复的原生 XInput fallback 访问集中到 `XInputNativeReader`。
- 将重复的恢复日志原子 XML 写入集中到 `AtomicXmlFileStore<T>`。
- 将配置/宏包/方案序列化和兼容性归一化移出 `MainForm`。
- 引入 `IVirtualGamepadBackend`，把 ViGEm 具体实现收敛到 `VigemVirtualGamepadBackend`，上层仍通过 `GamepadOutput` 使用相同语义。
- 将目标窗口/Idle 作用域策略、方案绑定扫描和软件更新 UI 协调继续从 `MainForm` 拆分。

这些调整主要为后续 Device Identity、Router source policy 和其他平台能力降低耦合，不代表增加新的虚拟手柄驱动或改变宏执行时序。

## 验证

发布前要求继续包括完整 `tests/Run-Tests.ps1`、中英文 Settings smoke、x64/x86 Release verification，以及 R2 Stable 清单派生的 Python CI 验证。更新网络测试覆盖官网优先、GitHub fallback、双源失败、有限重试、半包清理以及版本/架构绑定。

---

## English summary

**InputStitch 1.4.2 moves the Stable update path to `download.zhihanyu.com` first, with automatic fallback to the official GitHub Release, and completes a no-semantic-change architecture cleanup pass.**

Stable clients now check the website manifest first and download version-pinned executables from the website. If the website manifest, download, or validation fails, InputStitch falls back to the matching official GitHub source. Both sources must fail before the existing update failure UI is shown.

For upgrade compatibility, the GitHub Release manifest keeps GitHub asset URLs so existing 1.4.1 clients can still upgrade. The R2 mirror derives a separate `/latest/InputStitch-update.xml` whose assets point to immutable `download.zhihanyu.com/releases/v<version>/...` objects.

SHA-256, product/version validation, official-host restrictions, and strict version/architecture file-name checks remain mandatory. The release also deduplicates shared XInput/XML infrastructure, abstracts the virtual gamepad backend, and moves more target/profile/update coordination out of `MainForm` without changing macro, Layer, Output Ownership, Router, configuration, or Controller Takeover semantics.
