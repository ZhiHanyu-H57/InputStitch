# InputStitch 1.3.0-beta.1

> **Pre-release / 预发布。** Stable 仍为 **v1.2.0**。本 Beta 使用独立的 `InputStitch-beta.xml`，不会替换 Stable 的 `releases/latest` 或 `InputStitch-update.xml`。

## 简体中文

### 这一版解决什么

1.3.0-beta.1 是 InputStitch 第一次引入 **Input Ownership / 输入所有权** 的结构性版本。旧版本的运行时本质上只有一个当前宏；现在持续输出由独立 Source 拥有，再由统一合并器计算最终键鼠/虚拟手柄状态。

最直接的结果是：**多个符合 Held Mapping 形态的按住映射可以同时保持，并且还能同时运行最多一个普通时序宏。**

例如可以同时使用：

- `W → 左摇杆向前`
- `A → 左摇杆向左`
- `S → 左摇杆向后`
- `D → 左摇杆向右`
- `Shift → RT`
- `Ctrl → LT`
- `Mouse X1 → LB`
- `Mouse X2 → RB`

按住多个触发键时，每个映射只拥有自己的输出贡献；松开其中一个不会再把其他映射的状态一起清零。

### 合并规则

统一 Output Ownership 当前采用以下规则：

- **键盘 / 鼠标按钮 / 手柄数字按钮**：引用所有权。只要还有任意 Source 持有，最终状态就继续保持 Down。
- **LT / RT 扳机**：取所有来源请求值的 **最大值**。
- **摇杆**：所有来源的 X/Y 向量先相加，再按圆形摇杆范围归一化；相反方向自然抵消。
- **D-pad**：上下、左右分别作为两个轴处理；同轴相反方向相互取消，正交方向可以组成斜向。
- **Emergency Stop**：高于普通合并规则，直接清空所有 Source，并强制释放键鼠与中立化手柄。

普通宏、并行 Held Mapping 和 Idle Gamepad 已统一使用同一 ownership 核心。普通宏结束、UI 编辑保护暂时释放自身输出、Idle pulse 结束时，都不会再误伤其他仍然有效的 Source。

### 并行范围

本 Beta **没有**开放“任意多个普通宏并行”。边界刻意保持较窄：

- 多个 **Held Mapping** 可以并行；
- 最多一个普通 **Toggle / 时序宏** 可以与这些 Held Mapping 同时存在；
- 高级/复杂 Hold 宏仍保持独占，不与并行 Held Mapping 混跑；
- 同一个物理触发键对应多个已启用宏时，仍按照**宏列表从上到下**决定优先级；不会因为 ownership 自动把同触发键的多个宏一起启动；
- Emergency Stop 始终拥有绝对优先级。

第一版可并行 Held Mapping 的运行时形态是：Hold + Infinite、单键/单独 Ctrl/Shift/Alt/Win 或鼠标按钮触发、不使用修饰键组合/滚轮，执行内容为即时的手柄 Down 状态。**快捷创建 → 按住映射**生成的就是这种形态。

### 轻量运行观察

工具菜单新增 **运行观察…**。这是一个轻量、实时刷新且非模态的观察窗口，可以看到：

- 当前普通时序宏、RunId、执行阶段、迭代和步骤；
- 当前并行 Held Mapping；
- 每个 Output Source 的贡献；
- Source 是否因 UI 安全保护而暂时 Suspended；
- 合并后的最终输出；
- 最近停止原因 / ownership 最近变化原因。

它不是断点调试器，也不会持续记录用户的普通键盘输入。

### 单步执行

普通时序宏新增 **单步执行**：

1. 点击“单步执行”开始；
2. 第一条步骤执行完后等待；
3. 点击“下一步”继续；
4. 最后一步结束后自动退出。

单步模式只执行一轮，不使用步骤之间的普通 Delay 作为自动推进；每个步骤内部的 Press Hold 时间仍按原设置执行。正常“停止当前宏”和 Emergency Stop 在等待期间仍然有效。Held Mapping 不使用单步按钮，应继续通过物理触发键测试。

### 安全边界

- 修改正在活动的 Held Mapping 的关键定义（触发方式、触发键、循环方式、步骤、禁用、删除等）时，会先停止该映射，再修改配置，避免“界面定义已经变化、旧输出 Source 还活着”。
- UI 编辑保护现在只 Suspend 普通宏自己的 Source；其他并行 Held Mapping 的手柄贡献不会因为普通宏暂停而被一起清除。
- ownership 后端发生发送失败时会 **fail closed**：清空逻辑 Source、释放已知键鼠状态并中立化虚拟手柄，然后向上报告错误。
- 退出程序时 ownership 也作为最终清理权威，避免普通 worker 未及时结束时残留已按住输出。
- Emergency Stop 会解除单步等待、停止普通 worker、清除所有并行映射，并再次强制中立化手柄。

### Layer / 映射层

Layer 已经完成下一阶段设计，但**没有在 beta.1 中启用**。这是有意的：Input Ownership 是第一次结构性并发改造，在缺少真实游戏验收前再叠加 Layer，会让问题定位困难。

当前架构已经为 Layer 做好准备。拟定的第一版是 `Base + 一个活动 Layer`，只筛选 Held Mapping；切层时先移除旧层 Source，不会为切层前已经按住的键自动补触发，必须松开后重新按下。详细设计见 `docs/LAYER_DESIGN.md`。

### 自动验证

当前源码完整回归通过：

- 323 keyboard checks；
- 58 idle-gamepad assertions；
- 43 updater checks；
- 23 UI-safety / diagnostics checks；
- 348 productivity checks；
- **303,643 output-ownership checks**；
- 中英文 Settings、604×441 窄窗口、旧 XML 配置兼容、已保存手柄向量初始化/编辑 smoke tests。

Ownership 测试包含数字输出多来源引用、trigger max、摇杆/D-pad 冲突、后端故障 fail-closed、10,000 次随机来源操作、5 种混合来源的完整激活/释放顺序组合、实际 MainForm 路径下 `2 Held Mapping + 1 ordinary`、单步 + Emergency Stop、UI safety suspend/resume。测试使用 fake backend，**不会发送真实键鼠输入，也不会创建真实虚拟手柄**。

因此这些测试不能替代真实游戏验收。请重点测试：WASD 组合、Shift/Ctrl 扳机、鼠标侧键按钮、不同松开顺序、普通时序宏共存、Alt+Tab 后释放、Emergency Stop，以及多次启停后是否存在残留状态。

虚拟手柄仍依赖 ViGEmBus；EXE 仍未签名。Beta 与 Stable 共用 `%APPDATA%\InputStitch`，请不要同时运行两个版本。

## English

### What this beta changes

1.3.0-beta.1 introduces InputStitch's first explicit **Input Ownership** runtime. Persistent output now belongs to independent sources and a central merge layer computes the final keyboard/mouse/virtual-controller state.

The practical result is that **multiple qualifying Held Mappings can remain active together while one ordinary timed macro also runs**. Releasing one mapping removes only its contribution instead of neutralizing unrelated mappings.

Typical simultaneous mappings include WASD → left-stick directions, Shift/Ctrl → triggers, and mouse side buttons → shoulders.

### Merge rules

- **Keyboard, mouse buttons, and digital gamepad buttons:** reference ownership; output remains Down while any source owns it.
- **Analog triggers:** maximum requested value wins.
- **Sticks:** source X/Y vectors are summed and then normalized to the circular stick range; opposing vectors cancel naturally.
- **D-pad:** opposite directions cancel per axis; orthogonal directions can remain together as a diagonal.
- **Emergency Stop:** bypasses ordinary merge rules, clears every source, releases keyboard/mouse state and forces the virtual pad neutral.

Ordinary macros, parallel Held Mappings and Idle Gamepad now share the same ownership core, so one source finishing or being UI-suspended no longer clears another source.

### Concurrency boundary

This beta does **not** enable arbitrary concurrent timed macros:

- multiple qualifying Held Mappings may run together;
- at most one ordinary Toggle/timed worker may coexist with them;
- advanced/complex Hold workers remain exclusive;
- duplicate physical triggers still use macro-list priority rather than launching every duplicate;
- Emergency Stop remains absolute priority.

Quick Create → Held Mapping produces the intended parallel shape: supported single terminal trigger, Hold + Infinite, and immediate gamepad Down output.

### Runtime observation

Tools now includes **Runtime observation...**, a lightweight non-modal live view of active sources, each source contribution, merged output, ordinary macro RunId/phase/step, suspended state and recent stop/ownership reasons. It is not a full debugger and does not continuously record ordinary typing.

### Single-step execution

Ordinary timed macros gain **Single-step / Next Step**. Single-step runs one iteration, pauses between steps until Next Step is pressed, and remains interruptible by normal Stop and Emergency Stop. Per-step Press hold duration is still honored; the normal automatic post-step delay is not used to advance to the next step. Hold mappings continue to be tested with their physical triggers.

### Safety boundaries

- Definition edits that would invalidate a live Held Mapping stop that mapping before mutation.
- UI edit protection suspends only the ordinary macro source and leaves unrelated Held Mapping contributions intact.
- Ownership backend failure fails closed and clears every logical/physical output state it can own.
- Shutdown performs a final ownership ClearAll even if a worker did not finish within the bounded join.
- Emergency Stop releases single-step waits, the ordinary worker and all parallel mappings.

### Layer status

Layer / Mapping Layer is **designed but intentionally deferred** until this first ownership beta receives real-game acceptance. The proposed first design is Base + one active mapping layer, scoped to Held Mapping only. Switching removes old-layer sources and does not synthesize a trigger for keys that were already held before the switch. See `docs/LAYER_DESIGN.md`.

### Verification and limits

The current full suite passes 323 keyboard checks, 58 idle-gamepad assertions, 43 updater checks, 23 UI-safety/diagnostic checks, 348 productivity checks and **303,643 ownership checks**, plus bilingual Settings, narrow-layout, legacy-config and gamepad-vector smoke tests.

Ownership coverage includes same-output reference ownership, trigger max, stick/D-pad conflict resolution, fail-closed faults, 10,000 randomized source operations, exhaustive activation/release ordering across five mixed sources, real MainForm composition with two Held Mappings + one ordinary worker, single-step/Emergency Stop, and UI-safety suspend/resume through a fake backend. These tests send no real keyboard/mouse input and create no real virtual controller, so game acceptance still requires live testing.

Virtual gamepad output still requires ViGEmBus. Executables are unsigned. Beta and Stable share `%APPDATA%\InputStitch`; do not run both at the same time.

## Files / 文件

- `InputStitch-1.3.0-beta.1-Windows-x64.exe`
- `InputStitch-1.3.0-beta.1-Windows-x86.exe`
- `InputStitch-1.3.0-beta.1-Source.zip`
- `InputStitch-beta.xml`
- `SHA256SUMS.txt`
