# InputStitch 1.4.3

## 一句话看懂

**修复运行一段时间后键盘热键可能突然整体失效的问题：当 Windows 低级键盘钩子漏掉真实按键时，InputStitch 现在会用独立的 Raw Input 通道继续识别触发，并自动恢复键盘钩子。**

1.4.3 不修改配置格式，不改变宏、Held Mapping、Layer、Output Ownership、Router 或手柄接管的既有语义。Controller Takeover 仍然是 Experimental。

## 键盘热键可靠性修复

### 1. 增加独立的物理键盘兜底通道

InputStitch 原有全局键盘触发主要依赖 Windows `WH_KEYBOARD_LL` 低级键盘钩子。实际 GTA V 使用中发现，程序运行一段时间后可能出现低级钩子不再送达新的物理键盘事件，但主界面的“启动”按钮仍可正常执行同一宏的情况。

1.4.3 新增被动 Raw Input 键盘通道。正常情况下仍由原有低级钩子作为主通道；只有当 Raw Input 看到了真实物理按键边沿，而低级钩子没有在对应时间窗口内观察到同一边沿时，才进入兜底路径。

兜底输入仍复用原有的触发选择、Layer、UI 安全、Emergency Stop、并发运行时和 Output Ownership 路径，不另建第二套宏引擎。

### 2. 自动恢复低级键盘钩子

发现漏钩后，InputStitch 会等待当前观察到的物理键盘按键全部松开，再把恢复操作推迟到下一轮 WinForms 消息循环，并设置恢复冷却，避免按住键时重装造成重复触发或恢复风暴。

恢复只重建键盘钩子，不重建鼠标钩子，因此鼠标侧键、鼠标 Held Mapping 和鼠标 suppression 状态不会因为键盘自愈而被无关地重置。

### 3. 增加可观察诊断

运行观察和详细诊断现在会显示：

- Raw Input 观察到的键盘事件数量；
- 实际走过兜底路径的键盘边沿数量；
- 自动恢复键盘钩子的次数；
- 兜底发生时，本应屏蔽触发键但已经无法追溯屏蔽原始按键的次数。

这让“热键为什么还能触发 / 为什么发生过自愈”可以直接从诊断中确认，而不是只看到一个并不能代表实时健康状态的“Hooks: 已安装”。

## 一个需要明确的边界

正常低级钩子工作时，原有触发键 suppression 行为保持不变。

如果某一次物理按键已经被低级钩子漏掉，Raw Input 可以补上宏触发并推动钩子恢复，但它收到事件时原始按键已经被 Windows 送往前台应用，因此不能“回到过去”再把这一次原始按键屏蔽掉。这个限制只发生在已经进入漏钩兜底的那一次边沿。

## 验证

最终发布树已完成：

- x64 / x86 Release build + Release Verification；
- 键盘/触发专项 **329 PASS**；
- Output Ownership **303,716 PASS**；
- 其余完整回归、更新器/网络、UI Safety、Productivity、中英文 Settings smoke 和旧配置兼容测试全部通过；
- 当前最新版 Input Lab 黑盒验收 **77/77 PASS，failures=0**；
- GTA V 实机物理 `C` 键热键验收通过。

---

## English summary

**InputStitch 1.4.3 fixes a reliability issue where global keyboard hotkeys could stop responding after the application had been running for a while.**

The existing `WH_KEYBOARD_LL` hook remains the primary keyboard-trigger lane. A passive Raw Input keyboard lane now independently observes physical edges; when Raw Input sees an edge that the low-level hook did not observe, InputStitch feeds that edge through the existing trigger/runtime path and schedules keyboard-hook recovery after all observed keyboard keys have been released.

Recovery reinstalls only the keyboard hook, preserving mouse-hook and active mouse-mapping state. Runtime Observation and Diagnostics now expose Raw Input observations, fallback edges, hook recoveries, and fallback cases where trigger suppression could not be applied retroactively.

Configuration format and macro semantics are unchanged. Normal suppression behavior is unchanged while the primary hook is healthy. If the primary hook has already missed a physical edge, Raw Input can recover macro triggering but cannot retroactively suppress that already-delivered original key event.

Verification includes x64/x86 Release Verification, 329 keyboard checks, 303,716 Output Ownership checks, the full regression suite, Input Lab 77/77 PASS, and a physical GTA V `C`-trigger acceptance test.
