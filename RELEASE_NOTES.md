# InputStitch 1.5.0

这是已完成验收的 1.5.0-beta.15 的正式发布版。已有宏与设置继续兼容，无需重新配置。

## 下载与升级

- 现代 64 位 Windows 请选择 **InputStitch-1.5.0-Windows-x64.exe**；32 位系统请选择 x86。
- 便携式单文件程序。关闭旧版后启动新版；旧配置会自动读取。系统需 .NET Framework 4.7.2 或兼容的更高版本。
- 软件内更新后，主程序统一为 **InputStitch.exe**，以后不再带过时的版本号。常见桌面/开始菜单中指向旧程序的快捷方式会同步目标，保留自定义名称和启动参数；其他位置手动创建的快捷方式可重新指向新文件。
- 原程序及配置先备份，安装或启动失败时恢复旧程序；如果目录中已有另一个 InputStitch.exe，停止更新并保留原文件，避免覆盖。
- 键鼠宏无需额外驱动；虚拟手柄输出沿用单独安装的 ViGEmBus。正式版恢复现有自动更新策略，设置中的更新偏好保持不变。
- 附带完整源码、正式更新清单及 SHA-256 校验文件。

## 日常使用与外观

- 统一主界面及设置、时间与条件、按键选择等窗口的视觉和交互；收紧过宽的框，改善列表密度、展开层级、中英文和高 DPI 显示。
- 创建宏、设置触发、编辑步骤、保存运行的流程更清楚。步骤支持并列选择键盘图或实际按键录入；触发屏蔽直接可见。
- 设置入口更显眼，使用鲜艳的粉紫青渐变钻石图标。

## 屏幕准星

- 支持多种样式、大小、线宽、描边、颜色、透明度、显示器及精确位置调整；保存后下次恢复。
- 默认 **Ctrl + Alt + F10** 显隐，透明窗口穿透鼠标且不抢焦点。
- 支持窗口及无边框窗口。**独占全屏不在支持范围**，无需安装 Game Bar 组件。

## 性能与可靠性

- 减少空 XInput 槽位探测、重复设备元数据和拓扑处理及空闲界面任务。新接入/重连的 XInput 手柄通常最多约半秒被发现，已连接手柄仍按输入周期读取。
- 按实际截止时间等待，使用 Windows 高精度等待对象及兼容回退；宏执行复用专用线程，保留独立运行、停止和输出所有权。
- 修复 SDL3 空设备数组的原生内存释放问题，并去掉重复输入更新。
- 本机短时对照中，空闲单核 CPU 占用约由 23% 降至 4%，下降约 82%～83%。这是单机有限样本，不是所有硬件或负载的保证。未新增第三方运行依赖。

## 自动化验证

- beta.15 同一功能代码已通过 **34 套回归**：6,623 项产品流程断言、303,716 项输出所有权断言、172 项准星检查和 70,499 项布局检查（188 个窗口，零问题）。
- beta.15 的不强制垃圾回收持续运行：**600.1 秒、6,939 轮**，只创建 3 个宏工作线程，结束无残留输出。这是十分钟有界证据。
- 正式版构建另行检查版本/通道、x64/x86 成品启动与缓存/准星恢复、真实键鼠及虚拟手柄输出；GitHub 发布流程在完整回归通过后发布。
- 使用原始 1.4.3 正式 EXE 生成的配置验证升级：实际编辑器加载、保存、再次打开，逐项对照原配置字段，并保留原 XML 备份。老用户无需重新导入宏或重新设置触发键。
- 手柄接管仍属实验性能力，具体实体设备、HidHide 与游戏兼容性单独验证。

调研、架构取舍及测试边界见仓库 `docs/MATURITY_AND_PERFORMANCE_REVIEW.md`。历史测试版记录见 `docs/RELEASE_NOTES_1_5_BETA.md`。

---

This Stable release promotes the accepted beta.15 with clearer bilingual editing, a configurable windowed/borderless crosshair, lower background input overhead and dedicated macro-worker reuse. Existing configuration remains compatible. Use the x64 portable EXE on 64-bit Windows. Crosshairs do not support exclusive fullscreen; controller takeover remains experimental. Keyboard/mouse macros need no extra driver; virtual-controller output requires ViGEmBus.
