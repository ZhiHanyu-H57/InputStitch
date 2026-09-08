# InputStitch 1.3.0

InputStitch 1.3.0 promotes the Input Ownership and universal Concurrent Macro Runtime work from the 1.3 Beta line to Stable.

## Highlights

- **Universal normal-macro concurrency.** Multiple distinct ordinary timed/Toggle macros, Advanced/complex Hold timelines, and state-only Parallel Held Mappings can run at the same time.
- **Independent runtime ownership.** Worker-backed runs have independent `RunId` / `SourceId` / stop / timing / progress state, while final keyboard, mouse, and virtual-gamepad output is merged through Output Ownership.
- **Source-local cleanup.** Stopping, completing, or releasing one macro no longer clears unrelated active sources. Emergency Stop remains the global fail-safe and releases everything.
- **Advanced Hold is concurrent.** Keyboard/mouse Hold output, gamepad Press/Up sequencing, finite Hold, and Hold steps with fixed/random delays use the concurrent runtime instead of an exclusive worker.
- **Modifier-chord Hold.** Triggers such as `Shift+E`, `Ctrl+F`, and `Ctrl+Shift+K` can be used with Hold mode. Releasing the terminal key or any required modifier stops only that Hold source; lost-KeyUp fallback evaluates the complete chord.
- **Shift-friendly ordinary triggers.** If no more-specific explicit chord exists, a bare ordinary key such as `E` can still trigger while Shift is physically held. Explicit `Shift+E` remains higher priority. Ctrl/Alt/Win stay strict for bare-key fallback.
- **Quick Create follows the same rules.** Modifier-chord Held Mapping triggers are accepted; wheel directions remain ineligible for physical Hold triggering because they have no persistent down state.
- **Clearer runtime eligibility.** The editor uses the same authoritative `MacroRuntimeClassifier` as execution and now distinguishes manual/UI execution from physical Hold-trigger eligibility.
- **Runtime observation and single-step.** Runtime Observation lists active runs/sources/merged state. Single-step remains an intentionally exclusive diagnostic mode, not a normal concurrency restriction.
- **Project link in the gear menu.** `Open Project on GitHub` / `打开项目 GitHub` opens the InputStitch repository in the default browser.

## Deterministic overlap behavior

Persistent digital ownership is state-first. If one source already holds a key/button, another source's pulse/click on the same control does not force an artificial release/repress bounce. The persistent state stays down until its final owner releases.

Analog controller merging remains deterministic:

- triggers: maximum requested value;
- sticks: source vectors sum and normalize to the circular range;
- D-pad: opposing directions cancel per axis;
- digital keyboard/mouse/controller buttons: reference ownership.

## Reliability and compatibility

- Emergency Stop, shutdown, and backend failure retain fail-closed cleanup.
- UI edit protection suspends/resumes only affected worker-backed sources instead of globally neutralizing unrelated Held Mappings.
- Starting macro recording now treats state-only Parallel Held Mapping as active runtime and stops active runtime before capture.
- Removed obsolete singleton-era helpers/events/localization and one unused legacy configuration field; old XML containing the removed field remains covered by compatibility tests.
- One active run per `MacroDefinition` remains intentional.
- Duplicate physical triggers continue to use macro-list priority.
- Wheel directions remain unsupported as **physical Hold triggers**; a wheel-triggered Hold definition may still be run manually from the UI.

## Validation

The exact Stable 1.3.0 source passed the full isolated regression suite and Windows x64/x86 release verification before publication. The final release gate includes:

- 323 keyboard checks;
- 58 Idle Gamepad assertions;
- 43 updater checks;
- 23 UI safety / diagnostics checks;
- 358 productivity/UI checks, including the localized gear-menu GitHub item;
- 303,716 Output Ownership/runtime checks;
- zh-CN / en-US Settings smoke tests at normal and narrow sizes;
- old XML compatibility;
- gamepad vector editor smoke coverage.

Input Lab's real-SendInput pre-XInput concurrency lanes also remain part of the 1.3 validation history. This laptop can intermittently enumerate the ViGEm Xbox controller while XInput remains neutral; that environment condition is reported as `BLOCKED` with zero product assertion failures rather than being misreported as an InputStitch failure.

## Requirements

- Windows
- .NET Framework 4.7.2 or a compatible later release
- ViGEmBus is required only for virtual-controller output
- Keyboard/mouse-only macros do not require ViGEmBus

InputStitch remains portable and unsigned.

## Assets

- `InputStitch-1.3.0-Windows-x64.exe`
- `InputStitch-1.3.0-Windows-x86.exe`
- `InputStitch-1.3.0-Source.zip`
- `InputStitch-update.xml`
- `SHA256SUMS.txt`

---

# 中文说明

InputStitch 1.3.0 将 1.3 Beta 阶段完成并验证的 **Input Ownership + 统一 Concurrent Macro Runtime** 正式晋升到 Stable。

主要变化：

- 多个不同的普通时序 / Toggle 宏、多个高级/复杂 Hold、多个 Parallel Held Mapping 可以同时运行；
- 每个 worker 运行实例拥有独立 RunId / SourceId / 停止 / 时序 / 进度状态，一个宏结束或停止不会清除其他来源；
- 高级 Hold 不再独占，键鼠 Hold、Press/Up、非零或随机延迟、有限/无限 Hold 都可进入并发运行时；
- `Shift+E`、`Ctrl+F`、`Ctrl+Shift+K` 等修饰键组合可以作为 Hold 触发器，组合中任意必需按键松开都只停止对应 Hold；
- 按住 Shift 时，如果不存在更具体的显式 `Shift+E`，单独配置的裸 `E` 仍可正常触发；Ctrl/Alt/Win 不做这种裸键 fallback；
- Quick Create 与高级编辑器使用同一套 Hold 规则；滚轮因为没有持续按下状态，仍不能作为物理 Hold 触发器；
- 运行资格提示与实际执行使用同一个 classifier，并明确区分“可手动运行”和“可由物理 Hold 触发”；
- 齿轮菜单新增 **“打开项目 GitHub”**，可一键在默认浏览器中打开 InputStitch 项目主页；
- Emergency Stop 继续保持最高优先级，可一次释放全部受管输出。

下一阶段的最高优先级是 **Physical Gamepad Input + Hybrid Controller Routing**：先让 Windows XInput 手柄成为正式触发/输入源，再逐步实现 Controller → Keyboard/Mouse 的混合映射；Layer 继续后移。
