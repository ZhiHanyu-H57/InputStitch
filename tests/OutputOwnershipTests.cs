using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using InputStitch;

internal static class OutputOwnershipTests
{
    private sealed class FakeBackend : IOutputBackend
    {
        public readonly List<string> Events = new List<string>();
        public readonly List<InputSpec> Downs = new List<InputSpec>();
        public readonly List<InputSpec> Ups = new List<InputSpec>();
        public int NeutralizeCount;
        public bool FailNextDown;

        public void SendDown(InputSpec input)
        {
            if (FailNextDown)
            {
                FailNextDown = false;
                throw new InvalidOperationException("injected send failure");
            }
            Downs.Add(input.Clone());
            Events.Add("D:" + Describe(input));
        }

        public void SendUp(InputSpec input)
        {
            Ups.Add(input.Clone());
            Events.Add("U:" + Describe(input));
        }

        public void NeutralizeGamepad()
        {
            NeutralizeCount++;
            Events.Add("N");
        }

        private static string Describe(InputSpec input)
        {
            if (input.Kind == InputKind.Gamepad)
                return input.GamepadControl + ":" + input.GamepadX + ":" + input.GamepadY + ":" + input.GamepadValue;
            return input.Kind + ":" + input.VirtualKey;
        }
    }

    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Check failed: " + message);
    }

    private static InputSpec Pad(GamepadControl control, int x, int y, int value)
    {
        return new InputSpec { Kind = InputKind.Gamepad, GamepadControl = control, GamepadX = x, GamepadY = y, GamepadValue = value };
    }

    private static InputSpec Key(Keys key)
    {
        return new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)key };
    }

    private static InputSpec Merged(OutputOwnershipManager manager, GamepadControl control)
    {
        OutputOwnershipSnapshot snapshot = manager.Snapshot();
        return snapshot.Merged.FirstOrDefault(delegate(InputSpec x) { return x.Kind == InputKind.Gamepad && x.GamepadControl == control; });
    }

    private static object Field(object obj, string name)
    {
        return obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
    }

    private static void SetField(object obj, string name, object value)
    {
        obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
    }

    private static object Call(object obj, string name, params object[] args)
    {
        return obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, args);
    }

    private static bool WaitUntil(Func<bool> condition, int milliseconds)
    {
        int elapsed = 0;
        while (elapsed < milliseconds)
        {
            if (condition()) return true;
            Thread.Sleep(10);
            elapsed += 10;
        }
        return condition();
    }

    private static bool WaitUntilPumping(Func<bool> condition, int milliseconds)
    {
        int elapsed = 0;
        while (elapsed < milliseconds)
        {
            Application.DoEvents();
            if (condition()) return true;
            Thread.Sleep(10);
            elapsed += 10;
        }
        Application.DoEvents();
        return condition();
    }

    private static List<MacroRunRuntime> ActiveRuns(MainForm form)
    {
        return (List<MacroRunRuntime>)Call(form, "SnapshotActiveMacroRuns");
    }

    private static MacroRunRuntime ActiveSingleStepRun(MainForm form)
    {
        return ActiveRuns(form).FirstOrDefault(delegate(MacroRunRuntime runtime) { return runtime != null && runtime.SingleStep; });
    }

    private static List<int[]> Permutations(int count)
    {
        int[] values = Enumerable.Range(0, count).ToArray();
        List<int[]> result = new List<int[]>();
        Permute(values, 0, result);
        return result;
    }

    private static void Permute(int[] values, int index, List<int[]> result)
    {
        if (index >= values.Length)
        {
            result.Add((int[])values.Clone());
            return;
        }
        for (int i = index; i < values.Length; i++)
        {
            int tmp = values[index]; values[index] = values[i]; values[i] = tmp;
            Permute(values, index + 1, result);
            tmp = values[index]; values[index] = values[i]; values[i] = tmp;
        }
    }

    private static void CheckMixedState(OutputOwnershipManager manager, bool[] active, string label)
    {
        OutputOwnershipSnapshot snapshot = manager.Snapshot();
        InputSpec stick = snapshot.Merged.FirstOrDefault(delegate(InputSpec x)
        {
            return x.Kind == InputKind.Gamepad && x.GamepadControl == GamepadControl.LeftStick;
        });
        int expectedX = active[1] ? 100 : 0;
        int expectedY = active[0] ? 100 : 0;
        OutputOwnershipManager.NormalizeStick(ref expectedX, ref expectedY);
        Check((stick == null && expectedX == 0 && expectedY == 0) ||
              (stick != null && stick.GamepadX == expectedX && stick.GamepadY == expectedY),
            label + " stick");

        InputSpec trigger = snapshot.Merged.FirstOrDefault(delegate(InputSpec x)
        {
            return x.Kind == InputKind.Gamepad && x.GamepadControl == GamepadControl.RightTrigger;
        });
        int expectedTrigger = active[3] ? 100 : (active[2] ? 50 : 0);
        Check((trigger == null && expectedTrigger == 0) || (trigger != null && trigger.GamepadValue == expectedTrigger),
            label + " trigger");

        bool button = snapshot.Merged.Any(delegate(InputSpec x)
        {
            return x.Kind == InputKind.Gamepad && x.GamepadControl == GamepadControl.South;
        });
        Check(button == active[4], label + " digital button");
    }

    private static void DigitalReferenceOwnership()
    {
        FakeBackend backend = new FakeBackend();
        OutputOwnershipManager manager = new OutputOwnershipManager(backend);
        InputSpec a = Pad(GamepadControl.South, 0, 0, 100);
        manager.SetDown("one", a);
        Check(backend.Downs.Count == 1 && backend.Ups.Count == 0, "first digital owner sends Down");
        manager.SetDown("two", a);
        Check(backend.Downs.Count == 1, "second digital owner does not duplicate Down");
        manager.ClearSource("one");
        Check(backend.Ups.Count == 0, "clearing one of two digital owners keeps output down");
        manager.ClearSource("two");
        Check(backend.Ups.Count == 1, "last digital owner releases output");
        Check(manager.ActiveSourceCount == 0 && manager.Snapshot().Merged.Count == 0, "digital sources fully clear");

        backend = new FakeBackend();
        manager = new OutputOwnershipManager(backend);
        InputSpec q = Key(Keys.Q);
        manager.SetDown("k1", q);
        manager.SetDown("k2", q);
        manager.SetUp("k1", q);
        Check(backend.Downs.Count == 1 && backend.Ups.Count == 0, "keyboard output is reference-owned");
        manager.SetUp("k2", q);
        Check(backend.Ups.Count == 1, "keyboard releases after last source");
    }

    private static void TriggerMaxMerge()
    {
        FakeBackend backend = new FakeBackend();
        OutputOwnershipManager manager = new OutputOwnershipManager(backend);
        InputSpec low = Pad(GamepadControl.RightTrigger, 0, 0, 30);
        InputSpec high = Pad(GamepadControl.RightTrigger, 0, 0, 80);
        manager.SetDown("low", low);
        Check(Merged(manager, GamepadControl.RightTrigger).GamepadValue == 30, "first trigger value is 30");
        manager.SetDown("high", high);
        Check(Merged(manager, GamepadControl.RightTrigger).GamepadValue == 80, "trigger merge uses max");
        Check(backend.Downs.Last().GamepadValue == 80, "raising max emits updated trigger value");
        manager.ClearSource("high");
        Check(Merged(manager, GamepadControl.RightTrigger).GamepadValue == 30, "removing max owner restores lower trigger");
        Check(backend.Downs.Last().GamepadValue == 30, "restored lower trigger is emitted");
        manager.ClearSource("low");
        Check(Merged(manager, GamepadControl.RightTrigger) == null && backend.Ups.Count == 1, "last trigger owner releases to zero");
    }

    private static void DPadConflictMerge()
    {
        FakeBackend backend = new FakeBackend();
        OutputOwnershipManager manager = new OutputOwnershipManager(backend);
        manager.SetDown("up", Pad(GamepadControl.DPadUp, 0, 0, 100));
        manager.SetDown("down", Pad(GamepadControl.DPadDown, 0, 0, 100));
        OutputOwnershipSnapshot conflict = manager.Snapshot();
        Check(!conflict.Merged.Any(delegate(InputSpec x) { return x.Kind == InputKind.Gamepad &&
            (x.GamepadControl == GamepadControl.DPadUp || x.GamepadControl == GamepadControl.DPadDown); }),
            "opposed D-pad vertical directions cancel");
        manager.SetDown("right", Pad(GamepadControl.DPadRight, 0, 0, 100));
        OutputOwnershipSnapshot withRight = manager.Snapshot();
        Check(withRight.Merged.Any(delegate(InputSpec x) { return x.Kind == InputKind.Gamepad && x.GamepadControl == GamepadControl.DPadRight; }),
            "orthogonal D-pad direction survives an opposed vertical axis");
        manager.ClearSource("down");
        OutputOwnershipSnapshot restored = manager.Snapshot();
        Check(restored.Merged.Any(delegate(InputSpec x) { return x.Kind == InputKind.Gamepad && x.GamepadControl == GamepadControl.DPadUp; }) &&
              restored.Merged.Any(delegate(InputSpec x) { return x.Kind == InputKind.Gamepad && x.GamepadControl == GamepadControl.DPadRight; }),
            "removing one conflicting D-pad source restores diagonal");
        manager.ClearAll("dpad-end");
    }

    private static void StickVectorMerge()
    {
        FakeBackend backend = new FakeBackend();
        OutputOwnershipManager manager = new OutputOwnershipManager(backend);
        InputSpec forward = Pad(GamepadControl.LeftStick, 0, 100, 100);
        InputSpec right = Pad(GamepadControl.LeftStick, 100, 0, 100);
        manager.SetDown("W", forward);
        manager.SetDown("D", right);
        InputSpec diagonal = Merged(manager, GamepadControl.LeftStick);
        Check(diagonal != null && diagonal.GamepadX == 71 && diagonal.GamepadY == 71, "W+D normalizes to circular 71/71 diagonal");
        manager.ClearSource("W");
        InputSpec onlyRight = Merged(manager, GamepadControl.LeftStick);
        Check(onlyRight != null && onlyRight.GamepadX == 100 && onlyRight.GamepadY == 0, "releasing W preserves D contribution");
        manager.ClearSource("D");
        Check(Merged(manager, GamepadControl.LeftStick) == null, "last stick source returns neutral");

        backend = new FakeBackend();
        manager = new OutputOwnershipManager(backend);
        manager.SetDown("W", forward);
        manager.SetDown("S", Pad(GamepadControl.LeftStick, 0, -100, 100));
        Check(Merged(manager, GamepadControl.LeftStick) == null, "opposed stick vectors cancel to neutral");
        manager.ClearSource("S");
        InputSpec restored = Merged(manager, GamepadControl.LeftStick);
        Check(restored != null && restored.GamepadY == 100, "removing opposing source restores remaining vector");
    }

    private static void SourceSuspendResume()
    {
        FakeBackend backend = new FakeBackend();
        OutputOwnershipManager manager = new OutputOwnershipManager(backend);
        InputSpec rt = Pad(GamepadControl.RightTrigger, 0, 0, 60);
        manager.SetDown("macro", rt);
        manager.SuspendSource("macro");
        Check(Merged(manager, GamepadControl.RightTrigger) == null && backend.Ups.Count == 1, "suspended source temporarily releases physical output");
        OutputOwnershipSnapshot suspended = manager.Snapshot();
        Check(suspended.Sources.Count == 1 && suspended.Sources[0].Suspended, "suspended source retains logical contribution");
        manager.ResumeSource("macro");
        Check(Merged(manager, GamepadControl.RightTrigger).GamepadValue == 60 && backend.Downs.Count == 2, "resume restores source contribution");
    }

    private static void InstantAndEmergencyBoundaries()
    {
        FakeBackend backend = new FakeBackend();
        OutputOwnershipManager manager = new OutputOwnershipManager(backend);
        InputSpec wheel = new InputSpec { Kind = InputKind.WheelUp };
        manager.SetDown("macro", wheel);
        Check(backend.Downs.Count == 1 && manager.ActiveSourceCount == 0, "wheel edge bypasses persistent ownership");

        manager.SetDown("keyboard", Key(Keys.LControlKey));
        manager.SetDown("pad", Pad(GamepadControl.LeftStick, 0, 100, 100));
        manager.ClearAll("test-emergency");
        Check(manager.ActiveSourceCount == 0 && manager.Snapshot().Merged.Count == 0, "ClearAll drops every logical source");
        Check(backend.Ups.Any(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.LControlKey; }), "ClearAll explicitly releases keyboard state");
        Check(backend.NeutralizeCount == 1, "ClearAll hard-neutralizes gamepad once");
        Check(manager.Snapshot().LastReason == "test-emergency", "ClearAll records reason for observation");
    }

    private static void FailureIsFailClosed()
    {
        FakeBackend backend = new FakeBackend();
        OutputOwnershipManager manager = new OutputOwnershipManager(backend);
        manager.SetDown("safe", Key(Keys.A));
        backend.FailNextDown = true;
        bool threw = false;
        try { manager.SetDown("boom", Pad(GamepadControl.RightTrigger, 0, 0, 70)); }
        catch (OutputOwnershipException) { threw = true; }
        Check(threw, "backend failure surfaces to caller");
        Check(manager.ActiveSourceCount == 0 && manager.Snapshot().Merged.Count == 0, "backend failure clears all logical ownership");
        Check(backend.NeutralizeCount == 1, "backend failure hard-neutralizes gamepad");
        Check(backend.Ups.Any(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.A; }), "backend failure releases known keyboard state");
        Check(manager.Snapshot().LastReason.StartsWith("fail-closed:", StringComparison.Ordinal), "failure state is diagnosable");
    }

    private static void StressOwnership()
    {
        FakeBackend backend = new FakeBackend();
        OutputOwnershipManager manager = new OutputOwnershipManager(backend);
        Random random = new Random(13001);
        for (int i = 0; i < 10000; i++)
        {
            string source = "s" + random.Next(0, 48).ToString();
            int op = random.Next(0, 7);
            if (op == 0) manager.SetDown(source, Pad(GamepadControl.LeftStick, random.Next(-100, 101), random.Next(-100, 101), 100));
            else if (op == 1) manager.SetDown(source, Pad(GamepadControl.RightTrigger, 0, 0, random.Next(1, 101)));
            else if (op == 2) manager.SetDown(source, Pad(GamepadControl.LeftShoulder, 0, 0, 100));
            else if (op == 3) manager.ClearSource(source);
            else if (op == 4) manager.SuspendSource(source);
            else if (op == 5) manager.ResumeSource(source);
            else manager.SetDown(source, Pad(GamepadControl.RightStick, random.Next(-100, 101), random.Next(-100, 101), 100));

            OutputOwnershipSnapshot snapshot = manager.Snapshot();
            foreach (InputSpec state in snapshot.Merged)
            {
                if (state.Kind != InputKind.Gamepad) continue;
                if (state.GamepadControl == GamepadControl.LeftStick || state.GamepadControl == GamepadControl.RightStick)
                {
                    double magnitude = Math.Sqrt((double)state.GamepadX * state.GamepadX + (double)state.GamepadY * state.GamepadY);
                    Check(magnitude <= 101.0, "stress merged stick remains inside circular range");
                }
                if (state.GamepadControl == GamepadControl.LeftTrigger || state.GamepadControl == GamepadControl.RightTrigger)
                    Check(state.GamepadValue >= 0 && state.GamepadValue <= 100, "stress trigger remains clamped");
            }
        }
        manager.ClearAll("stress-end");
        Check(manager.ActiveSourceCount == 0, "stress cleanup leaves no source");
    }

    private static void MixedPermutationMatrix()
    {
        InputSpec[] inputs = new InputSpec[]
        {
            Pad(GamepadControl.LeftStick, 0, 100, 100),
            Pad(GamepadControl.LeftStick, 100, 0, 100),
            Pad(GamepadControl.RightTrigger, 0, 0, 50),
            Pad(GamepadControl.RightTrigger, 0, 0, 100),
            Pad(GamepadControl.South, 0, 0, 100)
        };
        string[] ids = new string[] { "W", "D", "RT50", "RT100", "South" };
        List<int[]> orders = Permutations(inputs.Length);
        Check(orders.Count == 120, "five-source permutation count");

        foreach (int[] activation in orders)
        {
            foreach (int[] release in orders)
            {
                OutputOwnershipManager manager = new OutputOwnershipManager(new FakeBackend());
                bool[] active = new bool[inputs.Length];
                foreach (int index in activation)
                {
                    manager.SetDown(ids[index], inputs[index]);
                    active[index] = true;
                }
                CheckMixedState(manager, active, "permutation all-active");
                foreach (int index in release)
                {
                    manager.ClearSource(ids[index]);
                    active[index] = false;
                    CheckMixedState(manager, active, "permutation release");
                }
                Check(manager.ActiveSourceCount == 0 && manager.Snapshot().Merged.Count == 0,
                    "permutation case ends fully released");
            }
        }
    }

    private static void RuntimeCompositionAndSingleStep()
    {
        string root = Path.Combine(Path.GetTempPath(), "InputStitch-ownership-runtime-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        FakeBackend backend = new FakeBackend();
        MacroConfig config = new MacroConfig();
        MacroDefinition w = new MacroDefinition
        {
            Name = "W forward",
            Infinite = true,
            RunMode = TriggerRunMode.Hold,
            Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.W },
            Steps = new List<MacroStep> { new MacroStep { Kind = InputKind.Gamepad, Action = MacroAction.Down, GamepadControl = GamepadControl.LeftStick, GamepadY = 100, DelayMs = 0 } }
        };
        MacroDefinition d = new MacroDefinition
        {
            Name = "D right",
            Infinite = true,
            RunMode = TriggerRunMode.Hold,
            Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.D },
            Steps = new List<MacroStep> { new MacroStep { Kind = InputKind.Gamepad, Action = MacroAction.Down, GamepadControl = GamepadControl.LeftStick, GamepadX = 100, GamepadY = 0, DelayMs = 0 } }
        };
        MacroDefinition ordinary = new MacroDefinition
        {
            Name = "ordinary",
            RunMode = TriggerRunMode.Toggle,
            Steps = new List<MacroStep>
            {
                new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.A, Action = MacroAction.Press, HoldMs = 1 },
                new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.B, Action = MacroAction.Press, HoldMs = 1 }
            }
        };
        config.Macros.Add(w); config.Macros.Add(d); config.Macros.Add(ordinary);

        try
        {
            using (MainForm form = new MainForm(config, root, backend))
            {
                MethodInfo parallelPolicy = typeof(MainForm).GetMethod("IsParallelHeldMapping", BindingFlags.Static | BindingFlags.NonPublic);
                Check((bool)parallelPolicy.Invoke(null, new object[] { w }), "Immediate gamepad Hold qualifies for parallel Held Mapping");
                MacroDefinition timedHold = w.Clone(); timedHold.Steps[0].DelayMs = 5;
                Check(!(bool)parallelPolicy.Invoke(null, new object[] { timedHold }), "Timed Hold is not the state-only Parallel Held Mapping fast path");
                Check(MacroRuntimeClassifier.Classify(timedHold).Category == MacroRuntimeCategory.ConcurrentHoldMacro &&
                    MacroRuntimeClassifier.Classify(timedHold).CanStart,
                    "Timed Hold is classified for the concurrent Hold runtime");

                Call(form, "StartParallelHeldMapping", w);
                Call(form, "StartParallelHeldMapping", d);
                Check((int)Call(form, "ActiveParallelHeldMappingCount") == 2, "two Held Mappings are active together");
                OutputOwnershipManager manager = (OutputOwnershipManager)Field(form, "outputOwnership");
                InputSpec diagonal = manager.Snapshot().Merged.First(delegate(InputSpec x) { return x.Kind == InputKind.Gamepad && x.GamepadControl == GamepadControl.LeftStick; });
                Check(diagonal.GamepadX == 71 && diagonal.GamepadY == 71, "runtime two-Held path reaches merged diagonal");

                MethodInfo start = typeof(MainForm).GetMethod("StartMacro", BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new Type[] { typeof(MacroDefinition), typeof(int), typeof(TriggerSpec), typeof(bool), typeof(bool) }, null);
                start.Invoke(form, new object[] { ordinary, 0, null, false, false });
                Check(WaitUntil(delegate { return !(bool)Call(form, "HasWorkerBackedRuns"); }, 2000), "ordinary worker completes beside Held Mappings");
                Check((int)Call(form, "ActiveParallelHeldMappingCount") == 2, "ordinary worker completion preserves both Held Mappings");
                diagonal = manager.Snapshot().Merged.First(delegate(InputSpec x) { return x.Kind == InputKind.Gamepad && x.GamepadControl == GamepadControl.LeftStick; });
                Check(diagonal.GamepadX == 71 && diagonal.GamepadY == 71, "ordinary worker cleanup does not neutralize Held Mapping output");

                start.Invoke(form, new object[] { ordinary, 0, null, false, true });
                Check(WaitUntil(delegate
                {
                    MacroRunRuntime run = ActiveSingleStepRun(form);
                    return run != null && run.SingleStepWaiting;
                }, 2000), "single-step pauses after first step");
                Check((int)Call(form, "ActiveParallelHeldMappingCount") == 2, "single-step wait preserves parallel Held Mappings");
                MacroRunRuntime singleStepRun = ActiveSingleStepRun(form);
                AutoResetEvent gate = singleStepRun == null ? null : singleStepRun.StepGate;
                Check(gate != null, "single-step owns an advance gate");
                gate.Set();
                Check(WaitUntil(delegate { return !(bool)Call(form, "HasWorkerBackedRuns"); }, 2000), "single-step completes after Next");
                Check((int)Call(form, "ActiveParallelHeldMappingCount") == 2, "single-step completion preserves Held Mappings");

                string observation = (string)Call(form, "BuildRuntimeObservationText");
                Check(observation.Contains("W forward") && observation.Contains("D right") &&
                    (observation.Contains("Merged output") || observation.Contains("合并后输出")),
                    "runtime observation exposes active mappings and merged output");

                Call(form, "StopParallelHeldMapping", w, "test");
                InputSpec right = manager.Snapshot().Merged.First(delegate(InputSpec x) { return x.Kind == InputKind.Gamepad && x.GamepadControl == GamepadControl.LeftStick; });
                Check(right.GamepadX == 100 && right.GamepadY == 0, "releasing W leaves D at runtime layer");
                Call(form, "StopParallelHeldMapping", d, "test");
                Check(manager.Snapshot().Merged.Count == 0, "releasing last runtime Held Mapping returns neutral");

                Call(form, "StartParallelHeldMapping", w);
                Check((int)Call(form, "ActiveParallelHeldMappingCount") == 1, "Held Mapping can restart after release");
                Call(form, "StopMacroForDefinitionChange", w, "test-definition-change");
                Check((int)Call(form, "ActiveParallelHeldMappingCount") == 0 && manager.Snapshot().Merged.Count == 0,
                    "definition change stops the live Held Mapping before mutation");

                Call(form, "StartParallelHeldMapping", w);
                Call(form, "StartParallelHeldMapping", d);
                start.Invoke(form, new object[] { ordinary, 0, null, false, true });
                Check(WaitUntil(delegate
                {
                    MacroRunRuntime run = ActiveSingleStepRun(form);
                    return run != null && run.SingleStepWaiting;
                }, 2000), "single-step reaches wait state before Emergency Stop test");
                Call(form, "EmergencyStop", "ownership-runtime-test");
                Check(WaitUntil(delegate { return !(bool)Call(form, "HasWorkerBackedRuns"); }, 2000),
                    "Emergency Stop terminates single-step worker");
                Check((int)Call(form, "ActiveParallelHeldMappingCount") == 0,
                    "Emergency Stop clears every parallel Held Mapping");
                Check(manager.Snapshot().Merged.Count == 0,
                    "Emergency Stop leaves ownership output fully neutral");
                Check(manager.Snapshot().LastReason.Contains("emergency-stop"),
                    "Emergency Stop reason remains visible to runtime diagnostics");
            }
        }
        finally { try { Directory.Delete(root, true); } catch { } }
    }

    private static void ConcurrentTimedRuntimeAndOverlapSemantics()
    {
        string root = Path.Combine(Path.GetTempPath(), "InputStitch-concurrent-runtime-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        FakeBackend backend = new FakeBackend();
        MacroConfig config = new MacroConfig();

        MacroDefinition timedKey = new MacroDefinition
        {
            Name = "Timed key A",
            RunMode = TriggerRunMode.Toggle,
            RepeatCount = 1,
            Steps = new List<MacroStep>
            {
                new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.A, Action = MacroAction.Down, DelayMs = 5000 },
                new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.A, Action = MacroAction.Up, DelayMs = 0 }
            }
        };
        MacroDefinition timedMouse = new MacroDefinition
        {
            Name = "Timed mouse X1",
            RunMode = TriggerRunMode.Toggle,
            RepeatCount = 1,
            Steps = new List<MacroStep>
            {
                new MacroStep { Kind = InputKind.MouseX1, Action = MacroAction.Down, DelayMs = 5000 },
                new MacroStep { Kind = InputKind.MouseX1, Action = MacroAction.Up, DelayMs = 0 }
            }
        };
        MacroDefinition timedStick = new MacroDefinition
        {
            Name = "Timed stick right",
            RunMode = TriggerRunMode.Toggle,
            RepeatCount = 1,
            Steps = new List<MacroStep>
            {
                new MacroStep { Kind = InputKind.Gamepad, Action = MacroAction.Down, GamepadControl = GamepadControl.LeftStick, GamepadX = 100, GamepadY = 0, DelayMs = 5000 },
                new MacroStep { Kind = InputKind.Gamepad, Action = MacroAction.Up, GamepadControl = GamepadControl.LeftStick, GamepadX = 100, GamepadY = 0, DelayMs = 0 }
            }
        };
        MacroDefinition heldUp = new MacroDefinition
        {
            Name = "Held up",
            Infinite = true,
            RunMode = TriggerRunMode.Hold,
            Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.W },
            Steps = new List<MacroStep>
            {
                new MacroStep { Kind = InputKind.Gamepad, Action = MacroAction.Down, GamepadControl = GamepadControl.LeftStick, GamepadX = 0, GamepadY = 100, DelayMs = 0 }
            }
        };
        MacroDefinition secondA = new MacroDefinition
        {
            Name = "Second A owner",
            RunMode = TriggerRunMode.Toggle,
            Steps = new List<MacroStep>
            {
                new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.A, Action = MacroAction.Down, DelayMs = 5000 },
                new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.A, Action = MacroAction.Up, DelayMs = 0 }
            }
        };
        MacroDefinition pulseA = new MacroDefinition
        {
            Name = "A pulse",
            RunMode = TriggerRunMode.Toggle,
            Steps = new List<MacroStep>
            {
                new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.A, Action = MacroAction.Press, HoldMs = 25, DelayMs = 0 }
            }
        };
        MacroDefinition finite = new MacroDefinition
        {
            Name = "Finite B repeat",
            RunMode = TriggerRunMode.Toggle,
            Infinite = false,
            RepeatCount = 3,
            Steps = new List<MacroStep>
            {
                new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.B, Action = MacroAction.Press, HoldMs = 40, DelayMs = 60 }
            }
        };
        MacroDefinition advancedHoldKey = new MacroDefinition
        {
            Name = "Advanced Hold C",
            RunMode = TriggerRunMode.Hold,
            Infinite = true,
            Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F9 },
            Steps = new List<MacroStep>
            {
                new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.C, Action = MacroAction.Down, DelayMs = 5000 },
                new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.C, Action = MacroAction.Up, DelayMs = 0 }
            }
        };
        MacroDefinition advancedHoldMouse = new MacroDefinition
        {
            Name = "Advanced Hold X2",
            RunMode = TriggerRunMode.Hold,
            Infinite = true,
            Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F10 },
            Steps = new List<MacroStep>
            {
                new MacroStep { Kind = InputKind.MouseX2, Action = MacroAction.Down, DelayMs = 5000 },
                new MacroStep { Kind = InputKind.MouseX2, Action = MacroAction.Up, DelayMs = 0 }
            }
        };
        MacroDefinition advancedHoldTrigger = new MacroDefinition
        {
            Name = "Advanced Hold RT",
            RunMode = TriggerRunMode.Hold,
            Infinite = true,
            Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F11 },
            Steps = new List<MacroStep>
            {
                new MacroStep { Kind = InputKind.Gamepad, GamepadControl = GamepadControl.RightTrigger, GamepadValue = 60, Action = MacroAction.Down, DelayMs = 5000 },
                new MacroStep { Kind = InputKind.Gamepad, GamepadControl = GamepadControl.RightTrigger, GamepadValue = 60, Action = MacroAction.Up, DelayMs = 0 }
            }
        };
        MacroDefinition finiteHold = new MacroDefinition
        {
            Name = "Finite Hold E",
            RunMode = TriggerRunMode.Hold,
            Infinite = false,
            RepeatCount = 2,
            Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F12 },
            Steps = new List<MacroStep>
            {
                new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.E, Action = MacroAction.Press, HoldMs = 30, DelayMs = 40 }
            }
        };
        MacroDefinition wheelHold = finiteHold.Clone();
        wheelHold.Name = "Wheel Hold manual-only";
        wheelHold.Trigger = new TriggerSpec { Kind = InputKind.WheelDown };
        config.Macros.Add(timedKey);
        config.Macros.Add(timedMouse);
        config.Macros.Add(timedStick);
        config.Macros.Add(heldUp);
        config.Macros.Add(secondA);
        config.Macros.Add(pulseA);
        config.Macros.Add(finite);
        config.Macros.Add(advancedHoldKey);
        config.Macros.Add(advancedHoldMouse);
        config.Macros.Add(advancedHoldTrigger);
        config.Macros.Add(finiteHold);
        config.Macros.Add(wheelHold);

        try
        {
            using (MainForm form = new MainForm(config, root, backend))
            {
                MacroRuntimeCapability keyCapability = MacroRuntimeClassifier.Classify(timedKey);
                Check(keyCapability.Category == MacroRuntimeCategory.ConcurrentTimedMacro && keyCapability.CanStart,
                    "ordinary delayed keyboard macro is classified as concurrent timed");
                Check(MacroRuntimeClassifier.Classify(heldUp).Category == MacroRuntimeCategory.ParallelHeldMapping,
                    "simple Hold remains classified as Parallel Held Mapping");
                MacroDefinition chordHeld = heldUp.Clone();
                chordHeld.Name = "Shift+W Held up";
                chordHeld.Trigger.Shift = true;
                Check(MacroRuntimeClassifier.IsHoldTriggerSupported(chordHeld.Trigger) &&
                      MacroRuntimeClassifier.Classify(chordHeld).Category == MacroRuntimeCategory.ParallelHeldMapping,
                    "modifier chord can use the state-only Parallel Held path");
                MacroDefinition advancedHold = heldUp.Clone();
                advancedHold.Infinite = false;
                Check(MacroRuntimeClassifier.Classify(advancedHold).Category == MacroRuntimeCategory.ConcurrentHoldMacro &&
                    MacroRuntimeClassifier.Classify(advancedHold).CanStart,
                    "finite Hold is classified as concurrent advanced Hold");
                MacroDefinition chordAdvancedHold = advancedHoldKey.Clone();
                chordAdvancedHold.Name = "Shift+F9 Advanced Hold";
                chordAdvancedHold.Trigger.Shift = true;
                Check(MacroRuntimeClassifier.IsHoldTriggerSupported(chordAdvancedHold.Trigger) &&
                      MacroRuntimeClassifier.Classify(chordAdvancedHold).Category == MacroRuntimeCategory.ConcurrentHoldMacro,
                    "modifier chord can use the concurrent Advanced Hold runtime");
                Check(MacroRuntimeClassifier.Classify(advancedHoldKey).Category == MacroRuntimeCategory.ConcurrentHoldMacro &&
                    MacroRuntimeClassifier.Classify(advancedHoldMouse).Category == MacroRuntimeCategory.ConcurrentHoldMacro &&
                    MacroRuntimeClassifier.Classify(advancedHoldTrigger).Category == MacroRuntimeCategory.ConcurrentHoldMacro,
                    "keyboard, mouse and delayed gamepad Hold timelines share the concurrent Hold category");
                MacroDefinition pressHold = heldUp.Clone();
                pressHold.Steps[0].Action = MacroAction.Press;
                pressHold.Steps[0].HoldMs = 25;
                Check(MacroRuntimeClassifier.Classify(pressHold).Category == MacroRuntimeCategory.ConcurrentHoldMacro,
                    "gamepad Press sequencing in Hold mode is concurrent rather than exclusive");
                MacroDefinition randomDelayHold = heldUp.Clone();
                randomDelayHold.Steps[0].RandomDelay = true;
                randomDelayHold.Steps[0].RandomDelayMinMs = 10;
                randomDelayHold.Steps[0].RandomDelayMaxMs = 25;
                Check(MacroRuntimeClassifier.Classify(randomDelayHold).Category == MacroRuntimeCategory.ConcurrentHoldMacro,
                    "random-delay Hold is concurrent rather than exclusive");
                MacroRuntimeCapability wheelCapability = MacroRuntimeClassifier.Classify(wheelHold);
                Check(wheelCapability.Category == MacroRuntimeCategory.ConcurrentHoldMacro && wheelCapability.CanStart &&
                    !wheelCapability.CanStartFromConfiguredTrigger && wheelCapability.ReasonCode == "hold-trigger",
                    "wheel Hold remains manually runnable but is ineligible for physical Hold triggering");
                Check(wheelCapability.TriggerEligibilityText(false).Contains("不可用"),
                    "wheel Hold trigger eligibility text reports unavailable");

                Label capabilityLabel = (Label)Field(form, "runtimeCapabilityLabel");
                Check(capabilityLabel != null && capabilityLabel.Text.Contains(keyCapability.CategoryText(Localizer.IsEnglish)),
                    "editor runtime eligibility text is driven by the authoritative classifier");
                ListBox macroList = (ListBox)Field(form, "macroList");
                macroList.SelectedIndex = config.Macros.IndexOf(wheelHold);
                Call(form, "RefreshRuntimeCapabilityUi");
                Check(capabilityLabel.Text.Contains(wheelCapability.CategoryText(Localizer.IsEnglish)) &&
                    capabilityLabel.Text.Contains(wheelCapability.TriggerEligibilityText(Localizer.IsEnglish)),
                    "editor exposes the same wheel Hold trigger eligibility returned by the classifier");
                macroList.SelectedIndex = config.Macros.IndexOf(timedKey);
                Call(form, "RefreshRuntimeCapabilityUi");

                MethodInfo start = typeof(MainForm).GetMethod("StartMacro", BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new Type[] { typeof(MacroDefinition), typeof(int), typeof(TriggerSpec), typeof(bool), typeof(bool) }, null);
                OutputOwnershipManager manager = (OutputOwnershipManager)Field(form, "outputOwnership");

                Call(form, "StartParallelHeldMapping", heldUp);
                start.Invoke(form, new object[] { timedKey, 0, null, false, false });
                start.Invoke(form, new object[] { timedMouse, 0, null, false, false });
                start.Invoke(form, new object[] { timedStick, 0, null, false, false });
                Check(WaitUntil(delegate { return ActiveRuns(form).Count == 3; }, 2000),
                    "three distinct ordinary timed macros run concurrently");
                Check(WaitUntil(delegate
                {
                    OutputOwnershipSnapshot s = manager.Snapshot();
                    bool key = s.Merged.Any(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.A; });
                    bool mouse = s.Merged.Any(delegate(InputSpec x) { return x.Kind == InputKind.MouseX1; });
                    InputSpec stick = s.Merged.FirstOrDefault(delegate(InputSpec x) { return x.Kind == InputKind.Gamepad && x.GamepadControl == GamepadControl.LeftStick; });
                    return key && mouse && stick != null && stick.GamepadX == 71 && stick.GamepadY == 71;
                }, 2000), "keyboard + mouse + timed gamepad outputs merge while Held Mapping remains active");

                string observation = (string)Call(form, "BuildRuntimeObservationText");
                Check(observation.Contains("Timed key A") && observation.Contains("Timed mouse X1") && observation.Contains("Timed stick right"),
                    "runtime observation lists every concurrent ordinary run");

                Call(form, "StopMacro", timedKey, "test-stop-one");
                Check(WaitUntil(delegate { return !IsRunning(form, timedKey) && ActiveRuns(form).Count == 2; }, 2000),
                    "stopping one ordinary run leaves the other two alive");
                OutputOwnershipSnapshot afterOneStop = manager.Snapshot();
                Check(!afterOneStop.Merged.Any(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.A; }),
                    "stopped run releases only its keyboard contribution");
                Check(afterOneStop.Merged.Any(delegate(InputSpec x) { return x.Kind == InputKind.MouseX1; }),
                    "unrelated mouse run remains active after keyboard run stops");
                InputSpec diagonal = afterOneStop.Merged.FirstOrDefault(delegate(InputSpec x) { return x.Kind == InputKind.Gamepad && x.GamepadControl == GamepadControl.LeftStick; });
                Check(diagonal != null && diagonal.GamepadX == 71 && diagonal.GamepadY == 71,
                    "unrelated timed stick + Held Mapping remain merged after one run stops");

                Call(form, "StopMacro", timedMouse, "test-cleanup");
                Call(form, "StopMacro", timedStick, "test-cleanup");
                Check(WaitUntil(delegate { return ActiveRuns(form).Count == 0; }, 2000), "first concurrent group stops cleanly");
                Call(form, "StopParallelHeldMapping", heldUp, "test-cleanup");
                Check(manager.Snapshot().Merged.Count == 0, "first concurrent group cleanup returns neutral");

                // Modifier-chord Hold release is source-local for both Hold execution paths.
                Call(form, "StartParallelHeldMapping", chordHeld);
                Check((int)Call(form, "ActiveParallelHeldMappingCount") == 1,
                    "Shift+W Parallel Held mapping can start");
                InputEventInfo shiftRelease = new InputEventInfo
                {
                    Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.LShiftKey },
                    Shift = false
                };
                Check(ModifierSafetyPolicy.HoldTriggerReleasedByEvent(chordHeld.Trigger, shiftRelease),
                    "required Shift release is recognized for Shift+W Parallel Held mapping");
                Call(form, "StopParallelHeldMapping", chordHeld, "modifier-release-test");
                Check((int)Call(form, "ActiveParallelHeldMappingCount") == 0,
                    "Shift+W Parallel Held mapping cleanup remains source-local");

                start.Invoke(form, new object[] { chordAdvancedHold, 0, null, true, false });
                Check(WaitUntil(delegate { return IsRunning(form, chordAdvancedHold); }, 1000),
                    "Shift+F9 Advanced Hold can start");
                Check(ModifierSafetyPolicy.HoldTriggerReleasedByEvent(chordAdvancedHold.Trigger, shiftRelease),
                    "required Shift release is recognized for Shift+F9 Advanced Hold");
                Call(form, "StopMacro", chordAdvancedHold, "modifier-release-test");
                Check(WaitUntil(delegate { return !IsRunning(form, chordAdvancedHold) &&
                    ActiveRuns(form).Count == 0 && manager.Snapshot().Merged.Count == 0; }, 2000),
                    "Shift+F9 Advanced Hold cleanup stops only that run and releases its source");

                // Universal concurrency: multiple complex Hold timelines, an ordinary timed macro,
                // and the state-only Parallel Held fast path all coexist. Each physical terminal
                // release must target only the matching Hold run.
                Check(form.Handle != IntPtr.Zero, "test form handle is created for queued terminal-release dispatch");
                Call(form, "StartParallelHeldMapping", heldUp);
                start.Invoke(form, new object[] { advancedHoldKey, 0, null, true, false });
                start.Invoke(form, new object[] { advancedHoldMouse, 0, null, true, false });
                start.Invoke(form, new object[] { advancedHoldTrigger, 0, null, true, false });
                start.Invoke(form, new object[] { timedStick, 0, null, false, false });
                Check(WaitUntil(delegate { return ActiveRuns(form).Count == 4; }, 2000),
                    "three Advanced Hold timelines and one ordinary timed macro run concurrently");
                Check(WaitUntil(delegate
                {
                    OutputOwnershipSnapshot s = manager.Snapshot();
                    bool keyC = s.Merged.Any(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.C; });
                    bool mouseX2 = s.Merged.Any(delegate(InputSpec x) { return x.Kind == InputKind.MouseX2; });
                    InputSpec rt = s.Merged.FirstOrDefault(delegate(InputSpec x) { return x.Kind == InputKind.Gamepad && x.GamepadControl == GamepadControl.RightTrigger; });
                    InputSpec stick = s.Merged.FirstOrDefault(delegate(InputSpec x) { return x.Kind == InputKind.Gamepad && x.GamepadControl == GamepadControl.LeftStick; });
                    return keyC && mouseX2 && rt != null && rt.GamepadValue == 60 && stick != null && stick.GamepadX == 71 && stick.GamepadY == 71;
                }, 2000), "complex keyboard/mouse/gamepad Hold + timed stick + Parallel Held output all merge");
                Check(ActiveRuns(form).Count(delegate(MacroRunRuntime r) { return r != null && r.HoldControlled && r.HoldTrigger != null; }) == 3,
                    "each physical Advanced Hold run owns an independent trigger snapshot");
                observation = (string)Call(form, "BuildRuntimeObservationText");
                string concurrentHoldCategory = MacroRuntimeClassifier.Classify(advancedHoldKey).CategoryText(Localizer.IsEnglish);
                Check(observation.Contains("Advanced Hold C") && observation.Contains("Advanced Hold X2") &&
                      observation.Contains("Advanced Hold RT") && observation.Contains(concurrentHoldCategory),
                    "runtime observation exposes every concurrent Advanced Hold and its category");

                Call(form, "HandleTerminalInputReleased", new InputEventInfo
                {
                    Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F9 }
                });
                Check(WaitUntilPumping(delegate { return !IsRunning(form, advancedHoldKey) && IsRunning(form, advancedHoldMouse) &&
                    IsRunning(form, advancedHoldTrigger) && IsRunning(form, timedStick); }, 2000),
                    "F9 release stops only its Advanced Hold while unrelated Hold/timed runs continue");
                OutputOwnershipSnapshot afterF9 = manager.Snapshot();
                Check(!afterF9.Merged.Any(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.C; }) &&
                      afterF9.Merged.Any(delegate(InputSpec x) { return x.Kind == InputKind.MouseX2; }) &&
                      afterF9.Merged.Any(delegate(InputSpec x) { return x.Kind == InputKind.Gamepad && x.GamepadControl == GamepadControl.RightTrigger; }),
                    "terminal release clears only the released Hold run's ownership source");

                Call(form, "HandleTerminalInputReleased", new InputEventInfo
                {
                    Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F10 }
                });
                Check(WaitUntilPumping(delegate { return !IsRunning(form, advancedHoldMouse) && IsRunning(form, advancedHoldTrigger) && IsRunning(form, timedStick); }, 2000),
                    "second Advanced Hold release is independent of remaining runs");
                Call(form, "HandleTerminalInputReleased", new InputEventInfo
                {
                    Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F11 }
                });
                Check(WaitUntilPumping(delegate { return !IsRunning(form, advancedHoldTrigger) && IsRunning(form, timedStick); }, 2000),
                    "delayed gamepad Advanced Hold releases without stopping ordinary timed run");
                Call(form, "StopMacro", timedStick, "advanced-hold-mix-cleanup");
                Check(WaitUntil(delegate { return ActiveRuns(form).Count == 0; }, 2000), "mixed Advanced Hold/timed run group stops cleanly");
                Call(form, "StopParallelHeldMapping", heldUp, "advanced-hold-mix-cleanup");
                Check(manager.Snapshot().Merged.Count == 0, "mixed Advanced Hold/Parallel Held cleanup returns neutral");

                // A definition mutation must signal only the affected live timeline; another
                // Advanced Hold continues on its immutable run snapshot.
                start.Invoke(form, new object[] { advancedHoldKey, 0, null, true, false });
                start.Invoke(form, new object[] { advancedHoldMouse, 0, null, true, false });
                Check(WaitUntil(delegate { return ActiveRuns(form).Count == 2; }, 2000), "two Advanced Holds active before definition-change test");
                Call(form, "StopMacroForDefinitionChange", advancedHoldKey, "test-definition-change");
                Check(WaitUntil(delegate { return !IsRunning(form, advancedHoldKey) && IsRunning(form, advancedHoldMouse); }, 2000),
                    "definition change stops only the affected Advanced Hold run");
                Call(form, "HandleTerminalInputReleased", new InputEventInfo
                {
                    Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F10 }
                });
                Check(WaitUntilPumping(delegate { return ActiveRuns(form).Count == 0; }, 2000), "definition-change Advanced Hold group cleans up");
                Check(manager.Snapshot().Merged.Count == 0, "definition change plus terminal release leaves neutral output");

                int eDownBefore = backend.Downs.Count(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.E; });
                int eUpBefore = backend.Ups.Count(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.E; });
                start.Invoke(form, new object[] { advancedHoldKey, 0, null, true, false });
                start.Invoke(form, new object[] { finiteHold, 0, null, false, false });
                Check(WaitUntil(delegate { return ActiveRuns(form).Count == 2; }, 1000), "finite Advanced Hold overlaps an infinite physical Advanced Hold");
                Check(WaitUntil(delegate { return !IsRunning(form, finiteHold) && IsRunning(form, advancedHoldKey); }, 2500),
                    "finite Advanced Hold completes independently while another Hold remains active");
                Check(backend.Downs.Count(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.E; }) == eDownBefore + 2 &&
                      backend.Ups.Count(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.E; }) == eUpBefore + 2,
                    "finite Hold repeat count remains exact under universal concurrency");
                Call(form, "HandleTerminalInputReleased", new InputEventInfo
                {
                    Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F9 }
                });
                Check(WaitUntilPumping(delegate { return ActiveRuns(form).Count == 0; }, 2000), "finite/infinite Hold overlap cleanup completes");

                start.Invoke(form, new object[] { timedKey, 0, null, false, false });
                start.Invoke(form, new object[] { timedKey, 0, null, false, false });
                Check(WaitUntil(delegate { return ActiveRuns(form).Count == 1 && IsRunning(form, timedKey); }, 2000),
                    "retrigger/start request does not create a second instance of the same macro definition");
                Call(form, "StopMacro", timedKey, "duplicate-instance-test");
                Check(WaitUntil(delegate { return ActiveRuns(form).Count == 0; }, 2000), "same-macro duplicate-start test cleans up");

                int aDownBefore = backend.Downs.Count(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.A; });
                int aUpBefore = backend.Ups.Count(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.A; });
                start.Invoke(form, new object[] { timedKey, 0, null, false, false });
                start.Invoke(form, new object[] { secondA, 0, null, false, false });
                Check(WaitUntil(delegate { return ActiveRuns(form).Count == 2 && manager.Snapshot().Merged.Any(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.A; }); }, 2000),
                    "two timed runs can own the same digital key together");
                Check(backend.Downs.Count(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.A; }) == aDownBefore + 1,
                    "second digital owner does not emit a duplicate physical Down edge");

                Call(form, "StopMacro", timedKey, "test-first-owner-release");
                Check(WaitUntil(delegate { return !IsRunning(form, timedKey) && IsRunning(form, secondA); }, 2000),
                    "first same-key owner stops while second owner remains");
                Check(manager.Snapshot().Merged.Any(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.A; }),
                    "releasing first same-key owner keeps merged key Down");
                Check(backend.Ups.Count(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.A; }) == aUpBefore,
                    "first same-key owner does not emit physical Up while another owner remains");

                int downsBeforePulse = backend.Downs.Count(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.A; });
                int upsBeforePulse = backend.Ups.Count(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.A; });
                start.Invoke(form, new object[] { pulseA, 0, null, false, false });
                Check(WaitUntil(delegate { return !IsRunning(form, pulseA); }, 2000), "overlapping same-key pulse completes independently");
                Check(IsRunning(form, secondA), "persistent same-key owner survives overlapping pulse");
                Check(backend.Downs.Count(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.A; }) == downsBeforePulse &&
                      backend.Ups.Count(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.A; }) == upsBeforePulse,
                    "pulse on an already-held digital control is deterministically masked instead of forcing a bounce");

                Call(form, "StopMacro", secondA, "test-last-owner-release");
                Check(WaitUntil(delegate { return ActiveRuns(form).Count == 0; }, 2000), "last same-key owner stops");
                Check(!manager.Snapshot().Merged.Any(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.A; }),
                    "last same-key owner releases merged key");
                Check(backend.Ups.Count(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.A; }) == aUpBefore + 1,
                    "last same-key owner emits exactly one physical Up");

                int bDownBefore = backend.Downs.Count(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.B; });
                int bUpBefore = backend.Ups.Count(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.B; });
                start.Invoke(form, new object[] { timedMouse, 0, null, false, false });
                start.Invoke(form, new object[] { finite, 0, null, false, false });
                Check(WaitUntil(delegate { return ActiveRuns(form).Count == 2 && IsRunning(form, timedMouse) && IsRunning(form, finite); }, 1000),
                    "finite-repeat ordinary macro overlaps another delayed ordinary macro");
                Check(WaitUntil(delegate { return !IsRunning(form, finite) && IsRunning(form, timedMouse); }, 2500),
                    "finite repeat macro completes independently while another timed macro remains active");
                Check(backend.Downs.Count(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.B; }) == bDownBefore + 3 &&
                      backend.Ups.Count(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.B; }) == bUpBefore + 3,
                    "finite repeat count remains exact under multi-run runtime");
                Call(form, "StopMacro", timedMouse, "finite-overlap-cleanup");
                Check(WaitUntil(delegate { return ActiveRuns(form).Count == 0; }, 2000), "finite-repeat overlap cleanup completes");

                Call(form, "StartParallelHeldMapping", heldUp);
                start.Invoke(form, new object[] { timedKey, 0, null, false, false });
                start.Invoke(form, new object[] { timedMouse, 0, null, false, false });
                start.Invoke(form, new object[] { advancedHoldKey, 0, null, true, false });
                start.Invoke(form, new object[] { advancedHoldMouse, 0, null, true, false });
                Check(WaitUntil(delegate { return ActiveRuns(form).Count == 4; }, 2000),
                    "ordinary timed + Advanced Hold runs are active together before Emergency Stop");
                Call(form, "EmergencyStop", "concurrent-runtime-test");
                Check(WaitUntil(delegate { return ActiveRuns(form).Count == 0; }, 2000), "Emergency Stop terminates every timed/Hold run");
                Check((int)Call(form, "ActiveParallelHeldMappingCount") == 0, "Emergency Stop clears Parallel Held beside arbitrary macro runs");
                Check(manager.Snapshot().Merged.Count == 0, "Emergency Stop leaves universal concurrent runtime fully neutral");
            }
        }
        finally { try { Directory.Delete(root, true); } catch { } }
    }

    private static bool IsRunning(MainForm form, MacroDefinition macro)
    {
        return (bool)Call(form, "IsMacroActuallyRunning", macro);
    }

    private static void RuntimeUiSafetyComposition()
    {
        string root = Path.Combine(Path.GetTempPath(), "InputStitch-ownership-ui-safety-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        FakeBackend backend = new FakeBackend();
        MacroConfig config = new MacroConfig();
        MacroDefinition w = new MacroDefinition
        {
            Name = "W",
            Infinite = true,
            RunMode = TriggerRunMode.Hold,
            Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.W },
            Steps = new List<MacroStep> { new MacroStep { Kind = InputKind.Gamepad, Action = MacroAction.Down,
                GamepadControl = GamepadControl.LeftStick, GamepadY = 100, DelayMs = 0 } }
        };
        MacroDefinition d = new MacroDefinition
        {
            Name = "D",
            Infinite = true,
            RunMode = TriggerRunMode.Hold,
            Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.D },
            Steps = new List<MacroStep> { new MacroStep { Kind = InputKind.Gamepad, Action = MacroAction.Down,
                GamepadControl = GamepadControl.LeftStick, GamepadX = 100, GamepadY = 0, DelayMs = 0 } }
        };
        MacroDefinition ordinary = new MacroDefinition
        {
            Name = "UI safety ordinary",
            RunMode = TriggerRunMode.Toggle,
            Steps = new List<MacroStep>
            {
                new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.A, Action = MacroAction.Down, DelayMs = 900 },
                new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.A, Action = MacroAction.Up, DelayMs = 0 }
            }
        };
        config.Macros.Add(w); config.Macros.Add(d); config.Macros.Add(ordinary);

        try
        {
            using (MainForm form = new MainForm(config, root, backend))
            {
                Call(form, "StartParallelHeldMapping", w);
                Call(form, "StartParallelHeldMapping", d);
                MethodInfo start = typeof(MainForm).GetMethod("StartMacro", BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new Type[] { typeof(MacroDefinition), typeof(int), typeof(TriggerSpec), typeof(bool), typeof(bool) }, null);
                start.Invoke(form, new object[] { ordinary, 0, null, false, false });
                OutputOwnershipManager manager = (OutputOwnershipManager)Field(form, "outputOwnership");
                Check(WaitUntil(delegate
                {
                    OutputOwnershipSnapshot s = manager.Snapshot();
                    return s.Sources.Any(delegate(OutputSourceSnapshot source) { return source.SourceId.StartsWith("macro:", StringComparison.Ordinal); });
                }, 2000), "ordinary source becomes observable before UI safety pause");

                SetField(form, "uiSafetyPauseRequested", true);
                Check(WaitUntil(delegate
                {
                    return manager.Snapshot().Sources.Any(delegate(OutputSourceSnapshot source)
                    {
                        return source.SourceId.StartsWith("macro:", StringComparison.Ordinal) && source.Suspended;
                    });
                }, 2000), "UI protection suspends only the ordinary source");
                OutputOwnershipSnapshot paused = manager.Snapshot();
                InputSpec diagonal = paused.Merged.FirstOrDefault(delegate(InputSpec x)
                {
                    return x.Kind == InputKind.Gamepad && x.GamepadControl == GamepadControl.LeftStick;
                });
                Check(diagonal != null && diagonal.GamepadX == 71 && diagonal.GamepadY == 71,
                    "UI safety pause preserves parallel Held Mapping merged stick");
                Check(!paused.Merged.Any(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.A; }),
                    "UI safety pause releases ordinary macro keyboard output");

                SetField(form, "uiSafetyPauseRequested", false);
                Check(WaitUntil(delegate
                {
                    OutputOwnershipSnapshot s = manager.Snapshot();
                    return s.Sources.Any(delegate(OutputSourceSnapshot source)
                    {
                        return source.SourceId.StartsWith("macro:", StringComparison.Ordinal) && !source.Suspended;
                    }) && s.Merged.Any(delegate(InputSpec x) { return x.Kind == InputKind.Keyboard && x.VirtualKey == (int)Keys.A; });
                }, 2000), "leaving UI protection restores ordinary source without disturbing mappings");

                Call(form, "StopMacro", ordinary, "ui-safety-test-stop");
                Check(WaitUntil(delegate { return !(bool)Call(form, "HasWorkerBackedRuns"); }, 2000), "UI safety ordinary worker stops cleanly");
                Check((int)Call(form, "ActiveParallelHeldMappingCount") == 2,
                    "stopping ordinary worker after UI safety leaves Held Mappings active");
                Call(form, "StopAllParallelHeldMappings", "ui-safety-test-end");
                Check(manager.Snapshot().Merged.Count == 0, "UI safety composition cleanup returns neutral");
            }
        }
        finally { try { Directory.Delete(root, true); } catch { } }
    }

    private static void RecordingStopsAllActiveRuntime()
    {
        string root = Path.Combine(Path.GetTempPath(), "InputStitch-recording-runtime-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        MacroConfig config = new MacroConfig();
        MacroDefinition recordTarget = new MacroDefinition { Name = "record target", Steps = new List<MacroStep>() };
        MacroDefinition held = new MacroDefinition
        {
            Name = "held while recording starts",
            Infinite = true,
            RunMode = TriggerRunMode.Hold,
            Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.W },
            Steps = new List<MacroStep>
            {
                new MacroStep { Kind = InputKind.Gamepad, Action = MacroAction.Down, GamepadControl = GamepadControl.LeftStick, GamepadY = 100, DelayMs = 0 }
            }
        };
        config.Macros.Add(recordTarget);
        config.Macros.Add(held);
        FakeBackend backend = new FakeBackend();
        try
        {
            using (MainForm form = new MainForm(config, root, backend))
            {
                Call(form, "StartParallelHeldMapping", held);
                Check((int)Call(form, "ActiveParallelHeldMappingCount") == 1,
                    "recording precondition has a pure Parallel Held runtime with no worker");
                Check(!(bool)Call(form, "HasWorkerBackedRuns") && (bool)Call(form, "HasAnyActiveRuntime"),
                    "active-runtime helper distinguishes Parallel Held from worker-backed runs");
                Call(form, "StartMacroRecording");
                Check((int)Call(form, "ActiveParallelHeldMappingCount") == 0,
                    "starting recording stops pure Parallel Held runtime before capture");
                OutputOwnershipManager manager = (OutputOwnershipManager)Field(form, "outputOwnership");
                Check(manager.Snapshot().Merged.Count == 0,
                    "recording start clears the stopped Held source output");
                Check((bool)Field(form, "recordingActive"), "recording starts after runtime cleanup");
                Call(form, "StopMacroRecording", false, true);
            }
        }
        finally { try { Directory.Delete(root, true); } catch { } }
    }

    [STAThread]
    public static int Main()
    {
        try
        {
            Application.EnableVisualStyles();
            DigitalReferenceOwnership();
            TriggerMaxMerge();
            DPadConflictMerge();
            StickVectorMerge();
            SourceSuspendResume();
            InstantAndEmergencyBoundaries();
            FailureIsFailClosed();
            StressOwnership();
            MixedPermutationMatrix();
            RuntimeCompositionAndSingleStep();
            ConcurrentTimedRuntimeAndOverlapSemantics();
            RuntimeUiSafetyComposition();
            RecordingStopsAllActiveRuntime();
            Console.WriteLine("PASS output ownership: " + checks + " checks; fake backend only, no real input or virtual device.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
