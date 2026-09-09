# InputStitch 1.4.0

## 一句话看懂

**1.4.0 把 1.3.1 Beta 线里已经验证的手柄输入/路由、多映射层、运行观察、更新与配置安全能力正式纳入 Stable；实验性手柄接管继续保留，但仍明确标记为 Experimental。**

这是从 `v1.3.0` 升级而来的新 minor release。它不是单纯修补几个问题，而是把键盘、鼠标和手柄输入进一步统一到同一套并发、可观察、可恢复的输入编排平台中。

## 主要变化

### 1. XInput 手柄成为正式输入来源

Windows 可见的 Xbox 兼容手柄现在可以直接作为宏触发来源：

- 正面按键、方向键、肩键、菜单键、摇杆按下；
- LT / RT 扳机；
- 按住类宏会记住真正启动它的那只手柄，另一只手柄松开同名按键不会误停止。

手柄触发仍然复用同一套宏执行系统，所以可以输出：

- 键盘；
- 鼠标；
- 虚拟手柄；
- 键盘 + 鼠标 + 手柄混合操作。

### 2. 多个外部 XInput 手柄可以汇总到 InputStitch 虚拟 Xbox

Router 会把每只外部 XInput 手柄作为独立来源送入 Output Ownership：

- 按键可以共同保持；
- 扳机按确定规则合并；
- 摇杆使用向量合并/归一化；
- 某一只手柄松开、回中或断开，只删除这一只手柄自己的贡献。

Router 本身并不隐藏原手柄，所以默认仍属于“汇总/增强”，不是拦截。

### 3. 可以完全不创建虚拟手柄

“虚拟手柄类型”新增：

**不创建虚拟手柄 / Do not create a virtual controller**

选择后：

- 键盘/鼠标宏仍然可用；
- 实体/其他 XInput 手柄仍可作为键鼠宏触发来源；
- 编辑手柄输出步骤不会因为保存配置而偷偷创建 ViGEm；
- 真正要求虚拟手柄输出的功能会明确拒绝，而不是静默覆盖用户偏好。

### 4. 映射层升级为任意多个可命名 Layer

基础层始终有效；除此之外可以创建任意多个自定义层。

支持：

- 新增、重命名、删除 Layer；
- 所有宏类型都可以分层；
- 每个 Layer（包括基础层）都能绑定键盘、鼠标或手柄快捷切换；
- 删除 Layer 时其中的宏安全移回基础层；
- 删除当前活动层时先回到“仅基础层”；
- 切层只停止旧层自己的运行来源，不误伤基础层或其他仍有效来源；
- 新层中原本已经按住的触发必须先松开再重新按，避免切层瞬间误触发。

同时修复了旧“映射层 1”删除后重启又自动出现的问题。新版 XML 中，明确保存的 Layer 列表就是权威数据；真正的旧配置仍能兼容迁移。

### 5. 并发宏继续使用统一 Output Ownership

1.4.0 继承并继续稳定使用 1.3.0 的并发架构：

- 多个普通时序宏；
- 多个 Toggle；
- 多个复杂 Hold；
- 多个轻量持续按住映射；
- Router、Idle 和宏输出；

都通过独立 SourceId / ownership 合并。

一个来源结束只释放自己的贡献，不会把另一个来源正在保持的键、鼠标或手柄状态一起清掉。

### 6. 运行观察和诊断更完整

运行观察/诊断现在能直接看到：

- 活动宏及 RunId / SourceId；
- 当前 step / phase / stop reason；
- 当前 Layer 名称；
- 活跃输出来源与 merged output；
- 虚拟 Xbox 当前槽位；
- Router 状态；
- 实验性接管状态；
- 健康监控最近结果、连续失败次数与恢复状态。

诊断本身不会为了“查看状态”而创建虚拟手柄。

### 7. 实验性 Controller Takeover 保留，但不宣称硬件成熟

`工具 → 手柄接管（实验）` 继续存在。

当前已实现：

- 在任何 HidHide 隐藏前先安全取得并硬验证 XInput 0 号槽位；
- 临时槽位重排与 HidHide 隐藏各自有独立恢复事务；
- 只隐藏用户明确选择的外部设备；
- 任一步失败都在隐藏前 fail closed 或尝试回滚；
- 崩溃恢复 journal；
- 接管运行期间持续检查槽位、Router、预期 XInput 来源、HidHide cloak/hidden-device/application whitelist；
- 一次瞬时失败容忍，连续两次失败才触发一次 fail-safe disengage；
- 确认失效后恢复 InputStitch 自己造成的隐藏变化、停止 Router 并清理受管输出，避免原输入 + 路由输入双份发送。

**仍未完成的是实体手柄 + HidHide + 实际游戏的最终硬件验收。** 当前开发机没有实体 XInput 测试手柄，因此 1.4.0 Stable 仍把这一入口明确标为 Experimental，而不会把它宣传成已完全验证的“手柄接管正式能力”。

### 8. 更新器的断网行为更安全

正式版自动更新现在对临时网络故障有明确边界：

- 请求有有限的无响应超时；
- 连接/DNS/接收中断等临时网络错误最多重试 2 次；
- 每次重试前删除上一轮半包；
- 三次都失败后不会写安装事务、不会关闭旧版、不会留下可执行半包；
- 完整下载并通过 SHA-256 + 产品/版本校验后，后续安装完全本地进行，之后断网不影响替换。

### 9. 配置保存和备份继续加固

配置保存继续采用 staged write + validate + replace，不做“先删旧文件再写新文件”。

同时修复快速连续保存时的备份排序边界问题：即使 Windows 墙钟分辨率很粗、连续保存看到相同时间、系统时钟短暂回拨或程序重启，也会稳定保留真正最近的 5 个有效配置备份，而不是让随机 GUID 决定新旧顺序。

## 自动验证

1.4.0 发布候选要求完整通过：

- 323 项键盘/触发检查；
- 58 项 Idle Gamepad 检查；
- 26 项 XInput 输入检查；
- 28 项 Gamepad Router 检查；
- 52 项 Controlled Replacement 事务/健康/恢复检查；
- 27 项槽位取得/PnP 恢复检查；
- 15 项“不创建虚拟手柄”偏好检查；
- 39 项 Layer/XML/运行观察检查；
- 7 项宏计时检查；
- 43 项更新安装/回滚检查；
- 18 项更新网络中断/重试检查；
- 23 项 UI 安全/诊断检查；
- 360 项配置/易用性检查；
- **303,716 项 Output Ownership / 并发合并检查**；
- 中英文设置窗口、旧 XML 和手柄向量编辑冒烟检查。

Input Lab 现有真实键鼠/并发 lane 为：

`18 PASS / failures=0`

之后仍会在本开发机的已知 ViGEm→XInput 环境预检处报告 `BLOCKED`；这不是产品断言失败，也不会被伪装成实体手柄验收。

四槽位中立 ViGEm 探针已验证：

`外部 0/1/2 + InputStitch 3 → InputStitch 0 + 外部 1/2/3`

它只证明真实 Windows/ViGEm 槽位重新枚举顺序，不替代实体手柄 + HidHide + 游戏验收。

## 发布文件

- `InputStitch-1.4.0-Windows-x64.exe`
- `InputStitch-1.4.0-Windows-x86.exe`
- `InputStitch-1.4.0-Source.zip`
- `InputStitch-update.xml`
- `SHA256SUMS.txt`

---

# English summary

**InputStitch 1.4.0 promotes the validated controller-input/routing and flexible-Layer work from the 1.3.1 Beta line into Stable, while keeping controller takeover explicitly Experimental until physical-controller + HidHide + target-game acceptance is available.**

Highlights:

- XInput controller buttons/triggers can start the same keyboard, mouse, virtual-controller and hybrid macros as other triggers.
- Multiple external XInput controllers can route into one InputStitch virtual Xbox through the existing deterministic Output Ownership merge model.
- Virtual-controller creation can be disabled completely for keyboard/mouse-only workflows.
- Base + arbitrary named mapping layers are supported, with keyboard/mouse/controller switch bindings and safe source-local cleanup on layer changes.
- Runtime Observation/diagnostics expose macro runs, sources, merged output, active layer, virtual slot, Router, takeover health and recovery state.
- Stable update downloads now have bounded network retries/timeout behavior and partial-download cleanup.
- Configuration backup ordering is deterministic even under same-clock rapid saves, clock rollback and restart.
- Controller takeover remains **Experimental**. Slot acquisition, HidHide transaction/rollback, crash recovery and runtime health monitoring are implemented, but final physical-controller + HidHide + real-game acceptance remains pending.
