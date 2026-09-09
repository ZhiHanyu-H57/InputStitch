# Layer / Mapping Layer design note

Status: **Layer v1 implemented in `v1.3.1-beta.1`; current scope is Base + one runtime-active Layer and applies to every macro type.**

状态：**Layer v1 已在 `v1.3.1-beta.1` 实现；当前是 Base + 一个运行时活动 Layer，并且已经覆盖所有宏类型。**

## Product role / 产品定位

Layer is an **eligibility/grouping feature** on top of the existing trigger/runtime/output architecture. It does not own a second input engine or a second macro executor.

```text
physical input / controller input
        ↓
Layer eligibility filter
        ↓
normal trigger priority
        ↓
Concurrent Macro Runtime / Parallel Held
        ↓
Output Ownership
```

## Implemented Layer v1 model

Configuration:

- `MappingLayerDefinition { Id, Name, IsBase }`
- `MacroDefinition.MappingLayerId`
- `MacroConfig.MappingLayers`
- Base ID: `base`
- first additional layer ID: `layer-1`

Runtime:

- `activeMappingLayerId` is runtime-only;
- empty runtime active ID means **Base only**;
- Base is always eligible;
- one non-Base layer can be active in addition to Base;
- restart returns to Base-only;
- legacy/invalid layer IDs normalize to Base.

## Macro coverage

Unlike the original pre-implementation proposal, Layer v1 is **not limited to Parallel Held Mapping**.

It applies to:

- ordinary timed macros;
- Toggle macros;
- finite/infinite worker-backed macros;
- Advanced/complex Hold;
- lightweight Parallel Held Mapping.

The same `IsMacroLayerEligible` filter is used before normal trigger selection. Duplicate triggers therefore retain the existing macro-list priority inside the currently eligible set.

## Switching semantics

A layer switch follows this order:

1. compute macros that were eligible in the old layer but will not be eligible in the new layer;
2. stop those active macro/held sources;
3. worker-backed sources are suspended immediately during the stop transition so they cannot briefly reassert output while their thread observes the stop request;
4. publish the new active layer;
5. identify newly eligible triggers that are already physically held;
6. block those macros until the matching physical control is released;
7. require a genuine new press before they can start.

Base is continuously eligible and must never be treated as a newly activated layer during ordinary switching.

This preserves the deliberate “release + press again” rule and prevents phantom activations from stale physical state.

## Why active Layer is not persisted

The active Layer is momentary runtime state. Persisting it would make an application restart silently change which mappings are live before the user has provided a fresh physical input edge.

Current rule:

```text
restart → Base only
```

A future opt-in “remember active layer” can be considered only after real use demonstrates that the convenience is worth the startup ambiguity.

## Emergency Stop and safety

Emergency Stop remains above Layer:

- Layer never changes the Emergency Stop trigger;
- Emergency Stop clears active worker and Held sources globally;
- Layer filtering cannot prevent Emergency Stop;
- UI/capture/recording safety remains above ordinary Layer-trigger dispatch.

## Current UI

Main trigger/run section exposes:

- **当前映射层 / Active layer** — runtime layer selector;
- **此宏属于 / Macro layer** — per-macro layer assignment.

Current public v1 scope intentionally has only Base + Layer 1. A later arbitrary-layer manager should not be added until there is a concrete workflow for naming, ordering and switching multiple layers.

## Automated coverage

`tests/LayerTests.cs` currently verifies 24 checks with isolated UI/fake output, including:

- Base-only eligibility;
- inactive Layer exclusion;
- ordinary timed macro gating;
- priority inside active eligible set;
- simultaneous Base + Layer output;
- switching stops old Layer output but preserves Base;
- worker-backed macro stop/suspend behavior;
- newly eligible pre-held trigger blocking;
- release clears the block.

No real input or virtual device is created by this suite.

## Later Layer work / 后续 Layer 能力

Only add these after repeated real use justifies them:

- arbitrary named layers;
- layer reorder/rename/delete UI;
- dedicated Layer-switch hotkeys/buttons;
- per-controller Layer state;
- temporary/momentary Layer while a key is held;
- conditions/groups that compose with Layer;
- optional active-Layer persistence.

Any future Layer selector trigger must have a clear conflict rule with ordinary macro triggers. It must not create a new macro engine or bypass Output Ownership.

## Non-negotiable constraints

- Base always remains eligible.
- Old-layer sources are removed/stopped before new eligibility is published.
- Pre-held newly eligible triggers do not synthesize activation.
- Layer does not alter source-local ownership semantics.
- One macro ending because of a layer switch must not clear outputs owned by Base or another still-eligible source.
- Emergency Stop remains global and independent of Layer.
