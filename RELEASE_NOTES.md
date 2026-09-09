# InputStitch 1.3.1-beta.1

## 一句话看懂

**现在 InputStitch 可以直接用手柄触发宏、把多个手柄汇总到自己的虚拟手柄，并在实验性接管模式下先安全取得 0 号槽位，再把所选原手柄交给 InputStitch 统一转发。**

这是基于 v1.3.0 正式版继续开发的测试版。它不会替换 v1.3.0，也不会修改正式版的自动更新通道。

## 你现在可以做什么

### 1. 直接用手柄触发宏

Windows 能看到的 Xbox 兼容手柄现在可以直接作为宏触发来源。

例如：

- 手柄 A → 键盘 K；
- 手柄 LB → 鼠标左键；
- 手柄 RT → 一段键盘、鼠标和虚拟手柄混合操作；
- 按住一个手柄按键 → 持续运行按住宏，松开后停止。

配置时默认表示“任意手柄上的这个控制项”。一个按住宏真正启动后会绑定到实际启动它的那只手柄，因此另一只手柄松开相同按键不会误停它。

左右扳机也能作为触发方式。当前使用约 **80% 按下 / 70% 松开**的判断，减少临界位置反复抖动。

### 2. 如果只用键鼠功能，可以完全不创建虚拟手柄

“设置 → 输入与安全 → 虚拟手柄输出 → 手柄类型”现在有第三个选项：

**“不创建虚拟手柄（仅使用键盘/鼠标功能）”**

选中后：

- InputStitch 启动时不会因为配置里保存着虚拟手柄输出宏而创建 ViGEm 设备；
- 键盘/鼠标宏仍然正常运行；
- 实体或其他 XInput 手柄仍然可以作为键盘/鼠标宏的触发来源；
- 保存/编辑一个虚拟手柄输出步骤本身也不会为了编辑配置而创建设备；
- 真正需要虚拟手柄输出、手柄汇总、闲置自动手柄输入或手柄接管时，需要重新选择 Xbox 360 或 PS4 / DualShock 4；手柄汇总/接管使用 Xbox 360。

如果用户先选择“不创建”，临时开启“手柄汇总”会自动切到 Xbox 360；取消汇总后会恢复之前的“不创建”选择。

### 3. 把多个手柄汇总到 InputStitch 自己的虚拟手柄

设置中新增测试选项：

**“把其他 XInput 手柄的操作汇总到 InputStitch 虚拟手柄（测试）”**

开启后，其他可见手柄的：

- 普通按键；
- 方向键；
- 左右摇杆；
- 左右扳机；

会持续汇总到 InputStitch 自己的虚拟 Xbox 360 手柄。

多个手柄可以同时提供输入。一个手柄松开、回中或断开时，只撤销这一只手柄自己的部分，不会把其他手柄或宏正在保持的状态一起释放。

### 4. 映射层现在支持所有宏类型

第一版映射层提供：

- **基础层**：始终有效；
- **映射层 1**：可以在运行时启用或关闭。

现在以下宏都可以选择所属层：

- 普通时序宏；
- 按一次启动/停止的宏；
- 复杂的按住运行宏；
- 轻量的持续按住映射。

离开一个映射层时，该层仍在运行的宏会停止，并只释放这个层自己的输出；基础层继续保留。

切入新层时，如果某个触发键原本已经按着，不会立刻产生意外触发。必须先松开，再重新按下。

当前激活层只是运行时状态，重启 InputStitch 后回到“仅基础层”。

### 5. 更新时断网更安全、更明确

正式版的内置更新器现在对临时断网更耐受：检查更新或下载更新遇到连接失败、DNS 失败、接收中断或无响应时，会在有限范围内自动再尝试 2 次。单次连接/读写连续约 8 秒没有进展就会结束这一轮尝试，不会无限等下去。

下载重试不会续写旧半包：每一次重新尝试前都会先删除上一轮留下的部分文件；三次都失败时也会清掉最终半包，不会进入安装事务、更不会替换当前 InputStitch。后台自动检查阶段断网仍保持安静；一旦用户已经确认更新并进入下载阶段，失败会明确显示“下载更新失败”。如果新 EXE 已完整下载并通过 SHA-256/版本校验，之后即使网络断开，备份、替换和重新启动都在本地完成，不再依赖 GitHub。

## 实验性手柄接管与 0 号槽位

Windows 的 XInput 接口只提供 0、1、2、3 四个手柄槽位，而且槽位由系统自动分配，InputStitch 不能直接写一个“把我设成 0 号”的命令。

本版本已经加入安全的槽位取得流程。你主动在“工具 → 手柄接管（实验）”中开始接管时：

1. InputStitch 会先停止正在受管的手柄输出；
2. 短暂断开自己的虚拟 Xbox，用重新枚举结果排除“自己的设备”，避免误隐藏自己；
3. 如果当前不在 0 号，会临时让所有外部 XInput/XUSB 手柄离开枚举；
4. InputStitch 先重新连接，并**硬验证自己确实成为 0 号**；
5. 立即恢复所有外部手柄，再次确认它们全部重新可读，而且 InputStitch 仍保持 0 号；
6. 只有这些检查全部通过，才允许 HidHide 隐藏用户明确选择的原手柄。

任何一步失败都会中止接管并尝试恢复原状态。**程序不会先隐藏原 0 号手柄，再赌 InputStitch 能不能抢到 0。**

为了取得 0 号，未被选中长期隐藏的外部 XInput 手柄也可能被短暂重新枚举；但最终 HidHide 只处理用户明确选择的设备。当前最多支持 **3 个外部 XInput 手柄 + 1 个 InputStitch 虚拟手柄**，超过四槽位总上限会在修改设备前拒绝接管。

槽位顺序已经在本机用 4 个中立 ViGEm 测试手柄做过真实验证：初始“外部 0/1/2 + InputStitch 3”，按接管顺序重新枚举后得到“InputStitch 0 + 外部 1/2/3”。这个探针不发送按钮、摇杆或扳机输入。

**仍然需要注意：**真正把原设备对普通程序隐藏依赖用户自行安装的 HidHide；当前开发机没有 HidHide 和实体测试手柄，因此“实体手柄 + HidHide + 实际游戏只看到 InputStitch”的最终硬件验收仍未完成。

## 多个输入如何合并

本版本继续使用 v1.3.0 已验证的统一输出合并机制，并把每个被汇总的手柄也作为独立输入来源。

普通用户只需要知道：

- 多个来源都按住同一个数字按键时，一个来源结束不会提前松开；
- 多个来源控制同一个扳机时，保留更大的力度；
- 多个来源控制同一个摇杆时，方向会合并，并限制在正常摇杆范围内；
- 方向键同一轴上的相反方向会互相抵消；
- 紧急停止仍然可以一次释放全部受管输入，并会关闭手柄汇总，防止下一次轮询重新把手柄状态按回去。

## 手柄输入可靠性

本版本还加入了这些保护：

- 同时读取最多四个可见 XInput 槽位；
- 自动排除 InputStitch 自己创建的虚拟 Xbox 手柄，避免“自己输出又触发自己”；
- 启动或手柄重新连接时先记录当前状态，不会因为一个本来就按住的按钮凭空启动宏；
- 手柄断开时会清理该手柄正在保持的触发和汇总状态；
- 多手柄汇总采用整帧更新，避免一个手柄状态变化时产生不必要的中间状态；
- 相同手柄状态不会每 10 ms 重复向虚拟手柄发送完全一样的数据。

## 自动测试

在生成发布包前，本版本的源码回归已通过：

- 323 项键盘与触发测试；
- 58 项闲置手柄测试；
- 26 项实体/虚拟 XInput 输入测试；
- 28 项多手柄汇总测试；
- 33 项接管安全事务测试；
- 15 项“不创建虚拟手柄”偏好测试；
- 24 项全类型映射层测试；
- 7 项宏计时漂移测试；
- 43 项更新安装测试；
- 23 项界面安全与诊断测试；
- 358 项易用性与配置测试；
- 303,716 项输出合并与并发运行测试；
- 其他修饰键、发布通道和兼容性测试。

这些自动测试使用注入状态或假输出后端，不会发送真实键鼠输入，也不会创建真实测试用虚拟手柄。最终发布前还会对实际生成的 x64/x86 EXE、设置界面、版本信息、更新清单和 SHA-256 校验执行发布包验证。

## 技术细节

普通用户不需要阅读这一节。

- 物理/已有虚拟 Xbox 兼容手柄通过 Windows **XInput** 接口读取。
- 每个非 InputStitch 自身的槽位以独立路由来源进入统一输出合并器。
- InputStitch 自己的 ViGEm Xbox 用户槽位会动态排除，避免反馈循环。
- 手柄汇总采用一次完整状态替换一个来源的方式更新，而不是逐按钮修改。
- v1.3.1-beta.1 的“路由”已经可以把 1/2/3 等其他可见槽位持续镜像到 InputStitch 自己的虚拟 Xbox 手柄。
- 新增可恢复的 XInput 槽位取得事务：临时重排所有外部 XUSB 设备、让 InputStitch 先重新连接并验证 0 号，再恢复外部设备；PnP 临时禁用不使用持久禁用标志，并有独立崩溃恢复日志。
- 槽位取得与 HidHide 隐藏是两道独立事务。只有槽位、Router、外部源身份全部再次通过验证，HidHide 阶段才可达。
- 当前开发机只安装了 ViGEmBus，没有 HidHide，因此已经完成真实 ViGEm 槽位顺序验证，但还不会宣称实体原手柄隐藏已经在本机完成硬件验收。

## 系统要求

- Windows
- .NET Framework 4.7.2 或兼容的更高版本
- 只使用键盘/鼠标宏，或只把实体/其他 XInput 手柄作为键鼠宏触发来源，并选择“不创建虚拟手柄”时，不需要为了这些功能创建 ViGEm 虚拟设备
- 使用虚拟手柄输出、手柄汇总、闲置自动手柄输入或手柄接管时需要 ViGEmBus

InputStitch 仍是便携式、未签名程序。

## 发布文件

- `InputStitch-1.3.1-beta.1-Windows-x64.exe`
- `InputStitch-1.3.1-beta.1-Windows-x86.exe`
- `InputStitch-1.3.1-beta.1-Source.zip`
- `InputStitch-beta.xml`
- `SHA256SUMS.txt`

---

# English summary

**InputStitch can now use connected controllers to trigger macros, merge multiple XInput controllers into its own virtual Xbox controller, and in experimental takeover mode safely acquire XInput slot 0 before handing selected original controllers to the routed virtual controller.**

Key points:

- Controller buttons and triggers can start keyboard, mouse, gamepad, or hybrid macros.
- Settings can choose **Do not create a virtual controller**. Keyboard/mouse macros and controller-triggered keyboard/mouse macros still work, while saved gamepad-output macros no longer force ViGEm creation at startup.
- Stable-channel update networking now uses bounded timeout/retry handling for transient outages. Partial executables are deleted before every retry and after final failure; once a verified executable has downloaded, installation is fully local and no longer needs GitHub/network access.
- Optional controller merging mirrors buttons, D-pad, sticks, and triggers from all other visible XInput slots into the InputStitch virtual Xbox controller.
- Experimental takeover now has a recoverable slot-acquisition transaction. If InputStitch is not already slot 0, all present external XUSB controllers are temporarily re-enumerated, InputStitch reconnects first and must verify slot 0, then the external controllers are restored and re-verified before HidHide is allowed to hide any selected device.
- Unselected controllers may be temporarily cycled to make slot 0 available, but only explicitly selected external device identities are passed to HidHide.
- The current XInput backend supports at most three external controllers plus the InputStitch virtual Xbox controller.
- Base is always active; timed, toggle, complex Hold, and lightweight Held Mapping macros can all belong to Layer 1.
- Leaving a layer stops that layer's active runs and removes only their owned output.
- A neutral four-controller ViGEm probe on this development machine verified the real ordering transition `external 0/1/2 + InputStitch 3 -> InputStitch 0 + external 1/2/3` without submitting button/stick/trigger input.
- Real physical-controller + HidHide + target-game takeover is still pending because HidHide and a physical XInput test controller are not available on this development laptop.
- Stable v1.3.0 and its automatic update channel remain unchanged.
