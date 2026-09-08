# InputStitch 1.3.0-beta.2

> **Pre-release / 预发布。** Stable 仍为 **v1.2.0**。本 Beta 使用独立的 `InputStitch-beta.xml`，不会替换 Stable 的 `releases/latest` 或 `InputStitch-update.xml`。

## 简体中文

### 这一版的重点：任意正常宏类型多并发

beta.2 在 beta.1 的 **Input Ownership** 基础上完成了统一 **Concurrent Macro Runtime**。现在多个不同宏可以真正同时运行，而不再只允许“多个 Held Mapping + 一个普通宏”。

当前支持同时存在：

- 多个普通时序 / Toggle 宏；
- 多个高级 / 复杂 Hold 宏；
- 多个纯状态型 Parallel Held Mapping；
- 键盘、鼠标、虚拟手柄输出混合；
- `Down / Up / Press`；
- 固定延迟或随机延迟；
- 有限循环或无限运行。

每个 worker 运行实例都有独立 `RunId / SourceId / Stop / timing / progress`。复杂 Hold 还会按 Run 独立保存物理触发键与 lost-KeyUp 松键恢复状态，因此松开一个 Hold 只停止它自己，不会误伤其他并发宏。

### Held Mapping 与复杂 Hold 的区别

简单的无限 Hold、即时手柄 `Down`、零延迟映射继续使用轻量 **Parallel Held Mapping** 快速路径。

一旦 Hold 包含键盘/鼠标输出、`Press/Up` 时序、非零/随机延迟或有限次数执行，就会由统一 classifier 归类为 **可并发高级 Hold / Concurrent Advanced Hold**，进入独立时间线运行时，但仍可与其他宏和 Parallel Held Mapping 同时存在。

有限 Hold 保留旧语义：达到配置的循环次数后可以自然结束；如果尚未结束，物理触发键松开会提前停止该 Run。

### 运行资格实时提示

编辑器里的“运行类别 / 并发能力”不再维护单独的 UI 规则，而是直接读取执行时使用的 `MacroRuntimeClassifier`。

因此取消无限循环、切换 Hold/Toggle、加入键鼠步骤、Press/Up、延迟或随机延迟时，界面会实时显示当前真实运行类别，不会出现“UI 说能并发、Runtime 却不能”的分叉。

### 冲突与安全语义

- 每个 Run 只清理自己的 Output Ownership Source；
- 一个复杂 Hold 的 KeyUp/MouseUp 或 lost-KeyUp fallback 只停止对应 Run；
- 同一数字输出被多个 Source 持有时继续使用引用所有权；
- 如果一个 Source 已持续持有某键/按钮，另一个宏对同一输出执行 pulse/click，不会强制制造 release/repress 抖动；该 pulse 会被确定性屏蔽；
- 同一个 `MacroDefinition` 仍最多有一个活动 Run，避免重复触发同一宏产生不可控重入；
- 同一物理触发键对应多个宏时仍按宏列表顺序决定优先级；
- **Single-step** 仍刻意保持独占，因为它是诊断执行模式，不是正常宏并发类别；
- **Emergency Stop** 和退出清理始终保持全局最高优先级。

### 自动验证

beta.2 发布前完整回归通过：

- 323 keyboard checks；
- 58 Idle Gamepad assertions；
- 43 updater checks；
- 23 UI safety / diagnostics checks；
- 351 productivity checks；
- **303,700 Output Ownership checks**；
- 中英文 Settings、窄窗口、旧 XML 配置兼容、已保存手柄向量初始化/编辑 smoke tests。

新的 Ownership / Runtime 测试覆盖普通时序宏 + 多个 Advanced Hold + Parallel Held 混跑、逐 Hold 松键、独立 lost-KeyUp probe、定义修改只停止目标 Run、有限 Hold 自然完成、同输出引用所有权、pulse masking，以及混合状态 Emergency Stop。

Input Lab 还使用真实 `SendInput` 验证了复杂 Hold 黑盒并发：`F9 → K` 与 `F10 → Mouse X2` 两条带延迟的 Advanced Hold 同时保持；Runtime Observation 同时显示两个 `Concurrent Advanced Hold`；松开 F9 后 K 被释放而 F10/X2 继续保持；松开 F10 后 X2 再释放。进入 XInput 预检前 **18/18 checks PASS，0 failures**。

本测试电脑仍存在间歇性的 ViGEm/XInput 环境问题：虚拟 Xbox 可以枚举，但 XInput 偶尔停留在 neutral `packet=1`。Acceptance 会把它报告为 `SUMMARY: BLOCKED` / `failures=0`，而不是伪装成产品 PASS 或误判为 InputStitch FAIL。

### 仍需真实游戏验收

beta.2 的下一步是重点实测新增路径，而不是机械重测没改动的底层 SendInput/ViGEm 基础兼容性：

- 两个以上复杂 Hold 同时保持；
- 复杂 Hold + 普通时序/Toggle + Parallel Held 混跑；
- 分别松键/停止，确认只清自己的输出；
- 一个有限复杂 Hold 与一个无限 Hold 共存；
- 至少一次 Alt+Tab 后在后台松开多个 Hold/Held 触发键，确认 lost-KeyUp fallback 按 Source 清理；
- 混合状态下 Emergency Stop 回到完全中立。

如果这轮真实使用验收干净，下一步就是 Stable `v1.3.0`。

## English

### Main change: universal normal-macro concurrency

beta.2 extends beta.1's **Input Ownership** foundation into a unified **Concurrent Macro Runtime**. Multiple distinct macros may now run at the same time instead of being limited to “multiple Held Mappings + one ordinary macro”.

Supported concurrent execution includes:

- multiple ordinary timed / Toggle macros;
- multiple Advanced / complex Hold macros;
- multiple state-only Parallel Held Mappings;
- mixed keyboard, mouse and virtual-gamepad output;
- `Down / Up / Press` sequencing;
- fixed or random delays;
- finite or infinite execution.

Every worker-backed run has independent `RunId / SourceId / Stop / timing / progress` state. Complex Hold runs additionally snapshot their physical trigger and lost-KeyUp release probe per run, so releasing one Hold stops only that run.

### Parallel Held vs Advanced Hold

Simple infinite, immediate gamepad-Down, zero-delay mappings retain the lightweight **Parallel Held Mapping** fast path.

Hold macros containing keyboard/mouse output, `Press/Up` sequencing, non-zero/random delay or finite execution are classified by the authoritative runtime classifier as **Concurrent Advanced Hold**. They use independent timelines but can coexist with ordinary macros, other Advanced Hold runs and Parallel Held Mappings.

Finite Hold preserves historical semantics: it may complete naturally when its configured repetitions finish; physical release stops it early only while the run is still active.

### Live runtime capability display

The editor's runtime/concurrency eligibility display now reads the same `MacroRuntimeClassifier` used by execution. Editing Hold/Toggle mode, infinite/finite behavior, output type, Press/Up sequencing or delay immediately updates the real runtime category instead of maintaining a separate UI rule table.

### Deterministic safety semantics

- one run ending/stopping/failing clears only its own Output Ownership source;
- terminal release and lost-KeyUp fallback target the matching Hold run only;
- shared digital output keeps reference ownership;
- pulse/click on a key/button already persistently held by another source is deterministically masked instead of forcing a release/repress bounce;
- one active run per `MacroDefinition` remains intentional;
- duplicate physical triggers still use macro-list priority;
- **Single-step** remains intentionally exclusive as a diagnostic mode;
- **Emergency Stop** and shutdown remain the global highest-priority cleanup paths.

### Verification

Pre-release regression passed:

- 323 keyboard checks;
- 58 Idle Gamepad assertions;
- 43 updater checks;
- 23 UI safety / diagnostics checks;
- 351 productivity checks;
- **303,700 Output Ownership checks**;
- bilingual Settings, narrow-layout, legacy XML compatibility and saved gamepad-vector smoke tests.

Input Lab also validated real-`SendInput` complex-Hold overlap: delayed `F9 → K` and `F10 → Mouse X2` Advanced Hold timelines overlap, Runtime Observation lists both as `Concurrent Advanced Hold`, releasing F9 removes only K while F10/X2 remains, and releasing F10 clears X2. All **18 pre-XInput checks PASS with 0 failures**.

This laptop still has an intermittent local ViGEm/XInput observation issue where the virtual Xbox enumerates but XInput may stay at neutral `packet=1`. Acceptance reports this as `SUMMARY: BLOCKED` with `failures=0`, distinct from an InputStitch product failure.

### Remaining real-game gate

Focus live testing on the newly affected paths: multiple complex Holds, complex Hold + ordinary timed/Toggle + Parallel Held coexistence, independent release order, finite + infinite Hold overlap, one Alt+Tab/lost-KeyUp sample, and mixed-state Emergency Stop. If that gate is clean, the next promotion target is Stable `v1.3.0`.

## Files / 文件

- `InputStitch-1.3.0-beta.2-Windows-x64.exe`
- `InputStitch-1.3.0-beta.2-Windows-x86.exe`
- `InputStitch-1.3.0-beta.2-Source.zip`
- `InputStitch-beta.xml`
- `SHA256SUMS.txt`
