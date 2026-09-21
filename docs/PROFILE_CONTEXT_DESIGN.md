# Profile / context integration design proposal

Status: **stage 1 implemented 2026-09-21** (model field, precedence, shared condition evaluator, diagnostics,
layer-management editor and clear action, 22-check test suite). Decisions taken: layer-level scope (approach A), precedence
`momentary hold > context layer > persistent layer`, stage-1 conditions limited to foreground process/title
plus stable `DeviceKey` (analog zones reuse the same evaluator). The layer-management UI entry for editing a
context condition is implemented through the shared condition dialog in conditions-only mode.

Written 2026-09-21 to answer the second half of `HANDOFF.md` → "Next platform step after v1.4.3" item 1
(Layer ergonomics + profile/context integration).

状态：**提案，尚未实现。** 用于讨论 `HANDOFF.md`「Layer ergonomics + profile/context integration」的后半部分。

## Why this note exists

Momentary / Hold-to-Layer is implemented (2026-09-21). The remaining half of the item is intentionally
underspecified in the roadmap: "build profile/manual-override/fallback behavior on the shared DeviceKey +
Condition model instead of adding application-specific switches." Those are product decisions, not code
decisions, so this note records the options and a recommended default instead of guessing in code.

## Building blocks that already exist

| Block | Location | What it gives us |
|---|---|---|
| Layer model | `InputStitch.cs` (`MappingLayerDefinition`, `SwitchActiveMappingLayer`) | Base + one active named layer, toggle and momentary switching, source-local cleanup, pre-held-trigger rule |
| Condition model | `ActivatorCondition.cs` (`MacroConditionConfig`) | AND gates: foreground process name, foreground title contains, stable `DeviceKey`, analog magnitude/half-axis zone |
| Device identity | `DeviceIdentity.cs` (`DeviceKey`, `DeviceIdentityService`) | Slot-independent, persistent device identity with fail-closed unresolved states |
| Profiles | `ProfileCatalog.cs`, `TargetWindowPolicy.cs`, `AutoSwitchProfiles` | User-bound profile files plus existing target-window-based profile auto-switch |
| Observation | runtime observation + diagnostics | Deterministic, testable runtime state reporting |

## Problem statement

Today a user can express context in two ways only:

1. **profile auto-switch** — swap the whole configuration when a target window appears;
2. **per-macro conditions** — individual macros gate themselves on process/title/device/analog state.

There is no explicit way to say "this *layer* is the right layer while this context holds", short of adding
another application-specific switch. That is exactly what the roadmap says to avoid: context should be
expressed through the existing Condition model, and device context through `DeviceKey`, not through
per-application toggles.

## Candidate approaches

### A. Layer context condition (recommended stage 1)

Add an optional `MappingLayerDefinition.ContextCondition` of the existing `MacroConditionConfig` shape.
A layer with a non-empty condition becomes **context-eligible** while the condition evaluates true.
No new condition types and no new application switches are introduced.

Precedence (recommended):

```text
momentary hold  >  context-eligible layer  >  persistent (click-to-switch) layer  >  Base only
```

- exactly one context layer can be effective; when several match, the first in layer-list order wins
  (deterministic, visible in diagnostics);
- toggling a layer manually still updates the persistent selection, but a matching context layer keeps
  the context active until the condition stops matching;
- leaving a context layer runs the normal `SwitchActiveMappingLayer` transaction, so its sources stop and
  Base output is untouched;
- an unresolvable `DeviceKey`, an unresolved XInput slot or an unevaluable analog zone makes that context
  layer inactive (fail-closed, never guessed).

Configuration stays backward compatible: the element is optional, old files load with no context layer, and
older builds ignore the unknown element.

### B. Profile-level context mapping (larger)

Map contexts to whole profiles (config files) rather than layers, replacing/extending the existing
target-window auto-switch. More powerful, but it duplicates an existing feature, has a much larger blast
radius (config swap while macros run) and mixes two different concepts. Only worth it if a concrete
workflow needs full configuration switching, not just layer eligibility.

### C. Manual-override lock (optional stage 2)

While a context matches, allow an explicit "manual override" that suspends the context until it is cleared
or until the context stops matching. Useful for "the game says Combat, but I want Exploration for the next
minute". Needs a UI affordance and a visible override state, so it is deliberately deferred until stage 1
is in real use.

## Recommended stage 1 scope

1. `MappingLayerDefinition.ContextCondition` (optional, `MacroConditionConfig`, normalized on load).
2. Context evaluation reuses the existing condition evaluator on the foreground timer, not a new engine.
3. Effective-layer computation extends the current momentary-aware chain:
   `momentary holds → context layer → persistent layer`.
4. Layer management gains "设置上下文条件… / Set context condition..." that opens the existing condition
   editor; the layer list and diagnostics mark context layers and their match state.
5. Diagnostics and runtime observation report the active reason (`momentary` / `context` / `manual`) and the
   context condition summary.
6. Tests: XML round trip, precedence with momentary holds, deterministic multi-match ordering,
   fail-closed unresolved `DeviceKey`, condition loss returning to the persistent layer, source-local
   cleanup, diagnostics/observation text.

Explicit non-goals for stage 1: conditional *macros* already exist and stay unchanged; no profile-level
mapping; no per-application special cases; no Layer stacking or inheritance; no second runtime.

## Open questions for the user

1. **Scope** — layer-level context condition (A, recommended) or profile-level context mapping (B)?
2. **Precedence** — `momentary > context > persistent` (recommended), or should a manual switch suppress
   the context until the user clears it (C)?
3. **Stage-1 condition types** — foreground process/title + `DeviceKey` (recommended), with analog zones
   kept available but rarely useful for layer context?
4. **UI surface** — reuse the existing macro condition editor inside "manage layers", or a separate
   context panel?
5. **Visibility** — should context layers be highlighted in the main window when they auto-activate
   (recommended: yes, with the active reason shown in runtime observation)?

Once 1-5 are answered, the implementation is roughly the size of the momentary change: one model field, one
evaluation hook on the existing timer, effective-layer precedence, UI reuse, diagnostics, and one extended
test suite.
