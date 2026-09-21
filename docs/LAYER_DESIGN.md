# Layer / Mapping Layer design note

Status: **Flexible Layer implemented for `v1.3.1-beta.2`: mandatory Base + arbitrary named user layers, one non-Base layer active at a time, keyboard/mouse/controller switch bindings, all macro types covered.**

Momentary / hold-to-layer semantics were added on post-`v1.4.3` `main` (2026-09-21) and are documented below.

状态：**`v1.3.1-beta.2` 已把 Layer 扩展为“基础层 + 任意多个可命名层”，支持键盘/鼠标/手柄快捷切层，并继续覆盖所有宏类型。**

2026-09-21 在 `v1.4.3` 之后的 `main` 上新增「按住临时切层（Momentary / Hold-to-Layer）」语义，见下文。

## Product role / 产品定位

Layer is an **eligibility/grouping feature** on top of the existing trigger/runtime/output architecture. It does not own a second input engine, macro executor, or output-merging system.

```text
physical keyboard / mouse / controller input
        ↓
Layer-switch trigger? ── yes → change eligibility only
        ↓ no
Layer eligibility filter
        ↓
normal trigger priority
        ↓
Concurrent Macro Runtime / Parallel Held
        ↓
Output Ownership
```

This architectural rule matters more than the UI: Layer must never bypass the common runtime or Output Ownership.

## Configuration model

Runtime model:

- `MappingLayerDefinition { Id, Name, IsBase, SwitchTrigger }`
- `MacroDefinition.MappingLayerId`
- `MacroConfig.MappingLayers`
- mandatory Base ID: `base`
- legacy first user-layer ID: `layer-1`
- newly created layers use stable generated IDs such as `layer-<guid>`; user-facing names are editable independently of IDs.

`SwitchTrigger` is optional and uses the same `TriggerSpec` model as ordinary macro triggers. It can therefore represent keyboard, mouse, or controller input without creating a new shortcut subsystem.

## Runtime model

- `activeMappingLayerId` is runtime-only.
- Empty active ID means **Base only**.
- Base is always eligible.
- Any number of non-Base layers may be defined.
- Exactly one non-Base layer may be active at a time.
- Effective eligibility is therefore always `Base + active named layer`, or Base only.
- Restart returns to Base-only.
- Invalid macro layer IDs normalize to Base.

This intentionally keeps Layer easy to reason about while allowing users to define as many named contexts as they need.

## Macro coverage

Layer applies uniformly to:

- ordinary timed macros;
- Toggle macros;
- finite/infinite worker-backed macros;
- Advanced/complex Hold;
- lightweight Parallel Held Mapping.

`IsMacroLayerEligible` filters candidates before normal trigger selection. Duplicate triggers therefore retain existing macro-list priority inside the currently eligible set.

## Layer-switch triggers

Each layer, including Base, may have its own switch trigger.

Semantics:

- non-Base layer trigger → activate `Base + that layer`;
- Base trigger → return to **Base only**;
- switch trigger matching uses the existing modifier-safety/exact/fallback trigger rules;
- controller capture uses the normal “any visible controller” trigger representation;
- layer switch has dispatch priority over an ordinary macro that uses the same physical trigger;
- the physical input itself is **not suppressed** merely because it switched Layer, so a game key may also serve as an InputStitch context selector.

A layer-switch trigger changes eligibility only. It does not itself submit keyboard/mouse/gamepad output.

## Momentary / hold-to-layer / 按住临时切层

Each layer definition carries an optional `MomentarySwitch` flag. It is stored in `config.xml` as a
plain boolean, so old configuration files load as click-to-switch and older builds simply ignore the
new element.

Semantics:

- a momentary layer becomes active only while its own switch trigger is physically held;
- releasing the trigger returns to the previously active layer instead of unconditionally returning to Base;
- a momentary hold never rewrites the persistent click-to-switch selection, so a temporary hold cannot
  silently change the layer the user chose;
- the Base layer may also be momentary: holding its shortcut temporarily returns to Base-only;
- several momentary layers can be held at once; the newest still-held layer is effective, and releasing
  it falls back to the next still-held layer or to the persistent selection;
- entering and leaving a momentary layer uses the same `SwitchActiveMappingLayer` transaction as any other
  layer change, so leaving it stops or suspends only that layer's sources;
- duplicate press edges (key repeat or repeated controller reports) for an already-held momentary layer are ignored;
- a trigger that was already held when the momentary layer becomes eligible still requires release + fresh press;
- missed release edges (hook loss or a dropped controller edge) are recovered by the same physical release
  probe used by held mappings, so a momentary layer cannot stay stuck;
- deleting a layer, or switching it back to click-to-switch while it is held, safely drops the hold;
- layer management exposes 按住生效（松开返回） / "Hold to activate (release returns)" beside the switch
  shortcut, and layer names plus diagnostics show the momentary mode.

This remains eligibility/grouping only: momentary switching does not suppress the physical input, does not
change Emergency Stop, UI safety, Output Ownership or source-local cleanup semantics, and never becomes a
second macro engine.

## Layer context conditions / 层上下文条件（stage 1）

Implemented 2026-09-21 (development plan H2.1). A layer can carry an optional `ContextCondition`
(`MacroConditionConfig`: foreground process/title, stable `DeviceKey`, analog zone). It reuses the same
AND-condition evaluator as per-macro gates — there is no parallel rule set and no new application switch.

Semantics:

- precedence is `momentary hold > matching context layer > persistent (click-to-switch) selection`;
- the first matching context layer in configured list order wins; the choice is deterministic and visible in
  diagnostics;
- a matching context never rewrites the persistent selection: as soon as the context stops matching, the
  runtime returns to the layer the user selected;
- Base may carry a context too, which temporarily returns the runtime to Base-only;
- context conditions are re-evaluated on the foreground-tracking cadence (50 ms timer) and on every layer or
  condition edit; a context change needs no input edge;
- an unresolvable `DeviceKey` or unevaluable analog condition simply does not match (fail-closed);
- layer display names, Runtime Observation and Diagnostics mark context layers and report which one matched.

Stage 1 covers model, config, precedence, evaluation, diagnostics and tests; the layer-management entry for
editing a context condition is the follow-up UI slice.

## Switching semantics

A layer switch follows this order:

1. compare old and new eligibility;
2. identify macros that were eligible but will cease to be eligible;
3. stop those active runs/held sources;
4. immediately suspend worker-backed old-layer sources so they cannot reassert output while observing stop;
5. rebuild the “blocked until release” set for newly eligible triggers;
6. publish the new active layer;
7. require a real release + fresh press for a newly eligible trigger that was already held.

Base is continuously eligible and must never be treated as newly activated during ordinary switching.

This preserves source-local cleanup: leaving one layer cannot globally neutralize Base output or any still-valid ownership source.

## Layer management

The main UI exposes:

- **当前映射层 / Active layer** — runtime selector;
- **管理映射层… / Manage layers...** — add / rename / delete / shortcut actions;
- **此宏属于 / Macro layer** — per-macro assignment.

Management behavior:

### Add

- create stable generated ID;
- ask user for editable display name;
- append to configured layer list.

### Rename

- change `Name` only;
- stable ID and macro assignments stay intact.

### Delete

- Base cannot be deleted;
- if deleting the active layer, switch to Base-only first;
- stop affected active macro sources;
- move every macro assigned to the deleted layer back to Base;
- remove the layer definition.

This guarantees no dangling `MappingLayerId` survives a user-level delete operation.

## XML compatibility: authoritative replacement list

A subtle XmlSerializer problem was discovered during beta.2 testing.

Historical model used a public collection field initialized like:

```text
MappingLayers = [Base, Layer 1]
```

`XmlSerializer` constructs `MacroConfig` first and then appends deserialized collection items. Therefore a modern file explicitly containing only Base could deserialize as:

```text
constructor default: Base + Layer 1
XML append:          + Base
```

That made a user-deleted legacy Layer 1 reappear after restart.

Current solution:

- runtime list `MappingLayers` is `[XmlIgnore]`;
- XML is exposed through a replacing array proxy using the same `<MappingLayers><MappingLayerDefinition>...` shape;
- if an old XML file has **no** MappingLayers element, the setter is never called and constructor default Base + Layer 1 remains as backward-compatible migration;
- if a modern file contains an explicit list, the setter **replaces** constructor defaults with that list;
- normalization thereafter guarantees Base but does not force Layer 1.

Result:

```text
truly old config, no Layer data → Base + Layer 1 migration
modern config, explicit [Base] → Base only, persists across restart
```

A real serialize → deserialize → normalize regression test locks this behavior.

## Why active Layer is not persisted

The active Layer is momentary runtime state. Persisting it would make restart silently change which mappings are live before a fresh user input edge.

Current rule:

```text
restart → Base only
```

An opt-in “remember active layer” should only be considered if repeated real use demonstrates that convenience outweighs startup ambiguity.

## Emergency Stop and UI safety

Emergency Stop remains above Layer:

- Layer never changes the Emergency Stop trigger;
- Emergency Stop globally clears active managed runs/sources;
- Layer filtering cannot prevent Emergency Stop;
- UI/capture/recording protection remains above ordinary macro or Layer-switch dispatch;
- Layer shortcut capture uses the same modifier-defer/cancel behavior as existing trigger capture.

## Runtime observation / diagnostics

Beta.2 observation displays the user-facing active Layer name rather than only internal IDs.

Diagnostics additionally list:

- layer count;
- each layer ID;
- each layer name;
- Base flag;
- formatted switch trigger.

Observation must be side-effect free. In the isolated UI test host, virtual-controller status uses neutral observation helpers instead of touching `GamepadOutput`, so diagnostics do not initialize ViGEm merely to inspect state.

## Automated coverage

`tests/LayerTests.cs` currently verifies **72 checks** with isolated UI/fake output, including:

- Base-only eligibility;
- inactive layer exclusion;
- ordinary timed macro gating;
- priority inside active eligible set;
- all-macro Layer eligibility;
- simultaneous Base + active-layer output;
- leaving layer stops old held and worker-backed output while preserving Base;
- pre-held newly eligible trigger blocking and release cleanup;
- arbitrary named layers coexisting;
- custom layer-switch trigger selection;
- Base return shortcut selection;
- switching between arbitrary layers;
- deleting active layer returns to Base;
- deleted-layer macros move to Base;
- custom rename reflected in display;
- deleting legacy Layer 1 is not undone by normalization;
- deleted legacy Layer 1 survives a real XML save/reload round trip;
- Runtime Observation shows user-facing active layer and controller-platform state;
- Diagnostics enumerate flexible layers and takeover state without loading real devices.
- momentary hold activates the layer and keeps Base eligible;
- releasing a momentary hold returns to the persistent layer, not unconditionally to Base;
- a momentary Base-layer shortcut temporarily returns to Base-only;
- several held momentary layers fall back in activation order;
- releasing a momentary hold stops its worker-backed and held-mapping output while Base survives;
- deleting a held layer safely returns to the persistent layer;
- a missed release edge is recovered by the physical-state fallback;
- momentary mode and switch shortcuts survive an XML save/reload round trip;
- layer display names and diagnostics report the momentary mode.
- a chorded momentary shortcut also returns when its modifier is released first;
- releasing an unrelated key never ends a momentary hold;
- changing the persistent selection while held keeps the hold active and the release lands on the new selection;
- converting a held layer back to click-to-switch drops the hold, and click-to-switch layers refuse momentary holds.

No real input or virtual device is created by this suite.

## Layer + Condition boundary / Layer 与条件边界

Activator + Condition stage 1 now composes additional AND conditions with the existing Layer eligibility check. The macro's `MappingLayerId` remains the authoritative Layer owner; the Condition engine does not store a second Layer field or create another precedence graph. If a Layer switch makes a running WhileHeld macro ineligible, the existing Layer cleanup path remains authoritative and source-local.

## Later Layer work / 后续能力

Only add these when a concrete workflow justifies them:

- user-defined layer ordering if list order becomes meaningful beyond management display;
- reusable Layer presets/groups;
- per-controller Layer state;
- reusable condition groups/presets beyond the current per-macro AND conditions;
- optional active-Layer persistence.

Do not add complexity simply because the data model can support it.

## Non-negotiable constraints

- Base always remains eligible.
- At most one non-Base layer is active under the current model.
- Old-layer sources are removed/stopped before new eligibility is considered authoritative.
- Pre-held newly eligible triggers do not synthesize activation.
- Layer switching never becomes a second macro engine.
- Layer does not alter source-local Output Ownership semantics.
- Deleting a layer cannot leave macros assigned to a nonexistent ID.
- Modern explicit XML layer lists are authoritative; truly old no-layer XML remains backward compatible.
- One macro ending because of a layer switch must not clear output owned by Base or another still-valid source.
- Emergency Stop remains global and independent of Layer.
