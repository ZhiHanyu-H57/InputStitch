# Layer / Mapping Layer design note

Status: **designed, intentionally deferred until after Stable 1.3.0**.

状态：**已完成设计，当前明确延后到 Stable 1.3.0 之后。**

## Why it is deferred / 为什么暂缓

1.3.0-beta.1 changes the runtime ownership model for the first time. Automated coverage can prove deterministic ownership/merge behavior, but the new multi-source Held Mapping path still needs real game acceptance for combinations such as WASD + Shift/Ctrl + mouse buttons, foreground switching and Emergency Stop. Shipping Layer in the same first ownership beta would mix two structural changes and make failures harder to isolate.

1.3.0-beta.1 首次改变运行时所有权模型。自动测试可以证明所有权和合并规则的确定性，但多来源 Held Mapping 仍需要在真实游戏中验证 WASD + Shift/Ctrl + 鼠标侧键、前后台切换和紧急停止等组合。如果在同一个首版 Ownership Beta 中同时加入 Layer，会把两个结构性变化叠在一起，不利于定位真实使用问题。

The architecture added in Beta 1 is Layer-ready: every persistent contributor already has an independent source identity and the merged output manager does not depend on macro-list ownership.

Beta 1 已经为 Layer 做好底层准备：每个持续输出都有独立 Source ID，最终输出由统一合并器负责，不再依赖“当前唯一宏”的所有权。

## Proposed first Layer scope / 第一版 Layer 范围

Layer should initially scope **Held Mapping only**. Ordinary timed macros remain outside Layer eligibility in the first Layer version, but by then the runtime is expected to support multiple concurrent ordinary macro runs.

Layer 第一版只管理 **Held Mapping**；普通时序宏第一版不受 Layer 资格筛选，但底层运行时届时应已经支持多个普通宏并发，而不是回到单全局 worker。

Proposed model:

- one mandatory Base layer;
- zero or more named mapping layers;
- each Held Mapping belongs to exactly one layer in the first implementation;
- one selected layer is active in addition to Base;
- mappings in Base are always eligible;
- mappings outside Base are eligible only when their layer is active;
- duplicate physical triggers use the existing list-priority rule *inside the currently eligible set*;
- Emergency Stop remains above Layer and clears every output source.

建议模型：

- 必须存在一个 Base 基础层；
- 可以创建多个命名映射层；
- 第一版每个 Held Mapping 只属于一个 Layer；
- Base 始终有效，另有一个可切换的活动 Layer；
- 非当前 Layer 的映射不参与触发匹配；
- 当前可用集合内部，同触发键继续使用现有列表顺序优先级；
- Emergency Stop 永远高于 Layer，并清空所有 Output Source。

## Switching semantics / 切层语义

A layer switch must be atomic from the ownership point of view:

1. stop/remove every active Held Mapping source that belongs only to the old layer;
2. change the eligible layer;
3. do **not** auto-start mappings for physical keys that were already held before the switch;
4. require release + press for a newly eligible mapping to start.

切层时应按所有权语义原子化处理：

1. 先移除旧 Layer 独占的全部活动 Held Mapping Source；
2. 再切换当前 Layer；
3. 对切层前已经物理按住的键，不自动在新 Layer 中补触发；
4. 必须松开并重新按下，才启动新 Layer 映射。

The deliberate “release and press again” rule avoids phantom activations, avoids replaying stale physical state, and makes layer transitions easy to diagnose. A later version may add opt-in seamless handoff only if real use demonstrates a need.

“切层后必须重新按下”的规则是刻意选择：它可以避免幽灵触发、避免根据旧物理状态自动补发输出，也更容易诊断。只有真实使用明确需要时，后续版本才考虑可选的无缝接管。

## Layer trigger priority / Layer 触发优先级

Recommended input priority:

1. Emergency Stop;
2. active capture/recording safety handling;
3. Layer selector hotkeys;
4. eligible macro/Held Mapping triggers;
5. native input passthrough.

建议优先级：紧急停止 > 捕获/录制安全状态 > Layer 切换键 > 当前可用宏/映射 > 原始输入透传。

Layer selector hotkeys must not be ordinary macro triggers at the same time. The UI should surface this as a hard conflict, unlike ordinary duplicate mapping triggers where list priority is intentional.

Layer 切换键不能同时作为普通宏触发键；这应当显示为硬冲突。它与“普通重复触发键按列表优先级处理”的现有设计不同。

## Configuration shape / 配置结构建议

Do not overload macro names or descriptions to encode layers. Use explicit fields with stable IDs, for example:

- `MappingLayer { Id, Name, IsBase }`
- `MacroDefinition.MappingLayerId`
- `MacroConfig.MappingLayers`
- runtime-only `ActiveMappingLayerId`

Do not persist momentary layer state until the behavior is validated. Configuration migration should place every legacy Held Mapping into Base automatically.

不要用宏名称或备注暗藏 Layer 信息。应使用显式稳定 ID。旧配置迁移时，所有既有 Held Mapping 自动进入 Base。瞬时活动 Layer 状态在行为验证前不应持久化。

## Release gate before implementation / 实现前门槛

Layer implementation now starts only **after Stable 1.3.0**. Stable 1.3.0 must first complete and accept the Concurrent Macro Runtime: multiple ordinary timed runs, independent source-local cleanup, coexistence with Held Mapping sources, deterministic overlap semantics, Stop/Emergency Stop, automated regression, Input Lab black-box acceptance and targeted real-game validation.

Only after that should Layer be implemented as a thin eligibility/grouping layer on top of the ownership + multi-run runtime, not as a second execution engine.

Layer 当前明确延后到 **Stable 1.3.0 之后**。1.3.0 必须先完成并验收普通宏多并发、独立 Source 清理、与 Held Mapping 共存、确定性的冲突语义、Stop/Emergency Stop 以及自动化/黑盒/真实游戏测试；之后 Layer 才作为现有 Ownership + multi-run runtime 上的“资格筛选/分组层”实现，而不是再造第二套执行引擎。
