using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using InputStitch;

internal static class IdleGamepadTests
{
    private static int assertions;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        assertions++;
        Console.WriteLine("PASS: " + name);
    }

    private sealed class Rig : IDisposable
    {
        public long Now;
        public int Connections;
        public int Failures;
        public bool ConnectThrows, DownThrows, UpThrows;
        public readonly List<string> Events = new List<string>();
        public readonly IdleGamepadService Service;
        public Rig()
        {
            Service = new IdleGamepadService(delegate {
                Connections++;
                if (ConnectThrows) throw new InvalidOperationException("connect");
            }, delegate(InputSpec input, bool down) {
                Events.Add((down ? "down:" : "up:") + input.GamepadControl);
                if ((down && DownThrows) || (!down && UpThrows)) throw new InvalidOperationException(down ? "down" : "up");
            }, delegate { throw new Exception("Native sensor must remain disabled"); }, delegate { return Now; }, false);
            Service.Failed += delegate { Failures++; };
        }
        public void Enable() { Service.Configure(new IdleGamepadOptions { Enabled = true, IdleSeconds = 10, HoldMilliseconds = 150 }, true); }
        public void At(long value) { Now = value; Service.Tick(); }
        public void Dispose() { Service.Dispose(); }
    }

    public static int Main()
    {
        EmbeddedDependencyLoader.Register();
        try
        {
            Options(); Timing(); ActivityAndBusy(); PanicAndDispose(); Failures(); SensorSignatures(); ConcurrentHandoff();
            BackgroundRelease(); StaleReleaseAndStress();
            Console.WriteLine("PASS ALL: " + assertions + " assertions; injected clock/output; no real input or virtual device created.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void BackgroundRelease()
    {
        long offset = 0;
        int downCount = 0, upCount = 0;
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        using (ManualResetEvent released = new ManualResetEvent(false))
        using (IdleGamepadService service = new IdleGamepadService(delegate { }, delegate(InputSpec input, bool down)
        {
            if (down) Interlocked.Increment(ref downCount);
            else { Interlocked.Increment(ref upCount); released.Set(); }
        }, null, delegate { return Interlocked.Read(ref offset) + watch.ElapsedMilliseconds; }, false, true))
        {
            service.Configure(new IdleGamepadOptions { Enabled = true, IdleSeconds = 10, HoldMilliseconds = 80 }, true);
            Interlocked.Exchange(ref offset, 10001);
            service.Tick();
            // No more UI ticks: simulate a stalled message loop, never send native input.
            Check(released.WaitOne(3000), "background deadline releases while UI ticks are stalled");
            Check(!service.IsPulsing && downCount == 1 && upCount == 1, "background timer only releases its owned pulse");
            Interlocked.Exchange(ref offset, 100000);
            Check(downCount == 1, "background deadline never starts an unattended new pulse");
        }
    }

    private static void StaleReleaseAndStress()
    {
        using (Rig r = new Rig())
        {
            r.Enable(); r.At(10000);
            FieldInfo generation = typeof(IdleGamepadService).GetField("releaseGeneration", BindingFlags.Instance | BindingFlags.NonPublic);
            long old = (long)generation.GetValue(r.Service);
            r.Service.SetBusy(true); r.Service.SetBusy(false);
            r.At(20000);
            r.Now = 30000;
            typeof(IdleGamepadService).GetMethod("OnReleaseDeadline", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(r.Service, new object[] { old });
            Check(r.Service.IsPulsing, "stale release callback cannot release a newer pulse");
            r.Service.SetBusy(true);
            r.Events.Clear();
            for (int cycle = 0; cycle < 10000; cycle++)
            {
                r.Service.SetBusy(false);
                r.Now += 10000; r.Service.Tick();
                if (!r.Service.IsPulsing) throw new Exception("Stress pulse missing");
                r.Service.SetBusy(true);
                if (r.Service.IsPulsing) throw new Exception("Stress release missing");
            }
            Check(r.Events.Count == 20000, "10000 start/cancel cycles have exactly balanced down/up pairs");
            for(int i=0; i<r.Events.Count; i+=2)
                if(r.Events[i] != "down:DPadDown" || r.Events[i+1] != "up:DPadDown") throw new Exception("Stress ordering failed");
            Check(true, "10000 cycles preserve ownership and output ordering");
        }
    }

    private static void Options()
    {
        Check(!new IdleGamepadOptions().Enabled, "default disabled");
        IdleGamepadOptions raw = new IdleGamepadOptions { IdleSeconds = -1, HoldMilliseconds = 9000, Pulse = null };
        IdleGamepadOptions normalized = raw.CloneNormalized();
        Check(normalized.IdleSeconds == 10 && normalized.HoldMilliseconds == 2000 && normalized.Pulse.Kind == InputKind.Gamepad, "normalize limits and null pulse");
        normalized.Pulse.GamepadControl = (GamepadControl)9999;
        Check(normalized.CloneNormalized().Pulse.GamepadControl == GamepadControl.DPadDown, "invalid control resets");
    }

    private static void Timing()
    {
        using (Rig r = new Rig())
        {
            r.At(1000000);
            Check(r.Connections == 0 && r.Events.Count == 0, "disabled never connects or sends");
            r.Now = 0; r.Enable(); r.At(9999);
            Check(r.Events.Count == 0, "wait full idle interval");
            r.At(10000); r.At(10149);
            Check(r.Service.IsPulsing && r.Events.Count == 1, "one down and exact minimum hold");
            r.At(10150);
            Check(!r.Service.IsPulsing && r.Events.Count == 2 && r.Events[1] == "up:DPadDown", "matching release at deadline");
            r.At(20149);
            Check(r.Events.Count == 2, "next cycle timed from release");
            r.At(20150);
            Check(r.Events.Count == 3, "repeated pulse after full new interval");
            r.Service.Configure(new IdleGamepadOptions(), false);
            Check(r.Events.Count == 4 && !r.Service.IsEnabled, "disable immediately releases active pulse");
            r.At(999999);
            Check(r.Events.Count == 4, "disabled stays quiet");
        }
        IdleGamepadScheduler scheduler = new IdleGamepadScheduler();
        scheduler.Configure(true, 10, 1000, true);
        Check(!scheduler.TryBeginPulse(500) && !scheduler.TryBeginPulse(10499) && scheduler.TryBeginPulse(10500), "clock rollback starts safe new interval");
    }

    private static void ActivityAndBusy()
    {
        using (Rig r = new Rig())
        {
            r.Enable(); r.Now = 9000; r.Service.NotifyActivity(); r.At(18999);
            Check(r.Events.Count == 0, "manual activity restarts timer");
            r.At(19000); r.Now = 19001; r.Service.NotifyActivity();
            Check(r.Events.Count == 2 && !r.Service.IsPulsing, "manual activity releases pulse immediately");
            r.At(29001); r.Now = 29002; r.Service.SetBusy(true);
            Check(r.Events.Count == 4 && !r.Service.IsPulsing, "macro busy releases before macro output");
            r.At(99999);
            Check(r.Events.Count == 4, "busy never pulses across elapsed interval");
            r.Service.SetBusy(false); r.At(109998);
            Check(r.Events.Count == 4, "macro completion waits a full interval");
            r.At(109999);
            Check(r.Events.Count == 5, "macro completion permits later idle pulse");
        }
        using (Rig r = new Rig())
        {
            IdleGamepadOptions value = new IdleGamepadOptions { Enabled = true, IdleSeconds = 10 };
            r.Service.Configure(value, true); value.Pulse.GamepadControl = GamepadControl.LeftStick;
            r.At(10000);
            Check(r.Events[0] == "down:DPadDown", "configured input is cloned and isolated from caller mutations");
        }
    }

    private static void PanicAndDispose()
    {
        using (Rig r = new Rig())
        {
            r.Enable(); r.At(10000); r.Service.PanicStop();
            Check(r.Service.IsSuspended && !r.Service.IsPulsing && r.Events.Count == 2, "panic releases and suspends synchronously");
            r.At(90000);
            Check(r.Events.Count == 2, "panic stays suspended");
            r.Service.Configure(new IdleGamepadOptions { Enabled = true, IdleSeconds = 10 }, false); r.At(100000);
            Check(r.Events.Count == 2, "ordinary configuration cannot resume panic");
            r.Enable(); r.At(110000);
            Check(r.Events.Count == 3 && !r.Service.IsSuspended, "explicit enable resumes after new interval");
            r.Service.Dispose(); r.Service.Dispose(); r.At(999999);
            Check(r.Events.Count == 4, "dispose releases once and forbids future pulses");
        }
    }

    private static void Failures()
    {
        using (Rig r = new Rig())
        {
            r.ConnectThrows = true; r.Enable(); r.At(10000); r.At(90000);
            Check(r.Connections == 1 && r.Events.Count == 0 && r.Service.HasFailed && r.Failures == 1, "connection failure stops retries and reports once");
        }
        using (Rig r = new Rig())
        {
            r.DownThrows = true; r.Enable(); r.At(10000); r.At(90000);
            Check(r.Events.Count == 2 && r.Events[1] == "up:DPadDown" && r.Failures == 1, "partial down failure attempts matching release and stops");
            r.DownThrows = false; r.Enable(); r.At(100000);
            Check(r.Events.Count == 3 && !r.Service.HasFailed, "explicit enable clears failure");
        }
        using (Rig r = new Rig())
        {
            r.Enable(); r.At(10000); r.UpThrows = true; r.At(10150); r.At(90000);
            Check(r.Events.Count == 2 && r.Service.HasFailed && r.Failures == 1, "release failure faults without retry loop");
        }
    }

    private static byte[] Ds4(bool bluetooth)
    {
        byte[] value = new byte[bluetooth ? 78 : 64];
        value[0] = bluetooth ? (byte)0x11 : (byte)1;
        int offset = bluetooth ? 3 : 1;
        for (int i = 0; i < 4; i++) value[offset + i] = 128;
        value[offset + 4] = 8;
        return value;
    }

    private static void SensorSignatures()
    {
        bool active; ulong signature;
        IdleManualActivitySensor.XINPUT_GAMEPAD pad = new IdleManualActivitySensor.XINPUT_GAMEPAD();
        IdleManualActivitySensor.XboxSignature(pad, out active);
        Check(!active, "neutral Xbox input is idle");
        pad.ThumbLX = 7849; pad.ThumbRY = -8689; pad.LeftTrigger = 30;
        IdleManualActivitySensor.XboxSignature(pad, out active);
        Check(!active, "Xbox deadzone and trigger threshold ignore jitter");
        pad.ThumbLX = short.MinValue;
        IdleManualActivitySensor.XboxSignature(pad, out active);
        Check(active, "held extreme Xbox axis counts without overflow");
        foreach (bool bluetooth in new bool[] { false, true })
        {
            byte[] report = Ds4(bluetooth);
            string transport = bluetooth ? "Bluetooth" : "USB";
            Check(IdleManualActivitySensor.TryDs4Signature(report, out signature, out active) && !active, transport + " DS4 neutral");
            ulong neutral = signature;
            int offset = bluetooth ? 3 : 1;
            report[offset + 6] = 0xFC; report[20] = 250;
            IdleManualActivitySensor.TryDs4Signature(report, out signature, out active);
            Check(!active && signature == neutral, transport + " counters and motion ignored");
            report[offset + 4] = 0x28;
            IdleManualActivitySensor.TryDs4Signature(report, out signature, out active);
            Check(active, transport + " held face button detected");
            report = Ds4(bluetooth); report[offset] = 0;
            IdleManualActivitySensor.TryDs4Signature(report, out signature, out active);
            Check(active, transport + " held stick detected");
        }
        Check(!IdleManualActivitySensor.TryDs4Signature(new byte[3], out signature, out active), "truncated DS4 report rejected");
        Check(!IdleManualActivitySensor.TryDs4Signature(new byte[64], out signature, out active), "unknown DS4 report rejected");
        Check(IdleManualActivitySensor.IsSupportedDs4Device(0x054C, 0x09CC) && !IdleManualActivitySensor.IsSupportedDs4Device(0x054C, 0x0CE6) && !IdleManualActivitySensor.IsSupportedDs4Device(0x045E, 0x09CC), "numeric Sony DS4 identity permits supported models only");
        Type info = typeof(IdleManualActivitySensor).GetNestedType("RID_DEVICE_INFO", BindingFlags.NonPublic);
        Check(Marshal.SizeOf(info) == 32 && Marshal.OffsetOf(info, "VendorId").ToInt32() == 8 && Marshal.OffsetOf(info, "ProductId").ToInt32() == 12, "native HID info layout is correct");
    }

    private static void ConcurrentHandoff()
    {
        long now = 0;
        ManualResetEvent downStarted = new ManualResetEvent(false);
        ManualResetEvent allowDownFinish = new ManualResetEvent(false);
        List<string> output = new List<string>();
        using (IdleGamepadService service = new IdleGamepadService(delegate { }, delegate(InputSpec input, bool down) {
            output.Add(down ? "idle-down" : "idle-up");
            if (down) { downStarted.Set(); if (!allowDownFinish.WaitOne(3000)) throw new Exception("test timeout"); }
        }, null, delegate { return now; }, false))
        {
            service.Configure(new IdleGamepadOptions { Enabled = true, IdleSeconds = 10 }, true);
            now = 10000;
            Thread tick = new Thread(delegate() { service.Tick(); }); tick.Start();
            if (!downStarted.WaitOne(3000)) throw new Exception("test timeout");
            Thread macro = new Thread(delegate() { service.SetBusy(true); output.Add("macro-start"); }); macro.Start();
            allowDownFinish.Set();
            Check(tick.Join(3000) && macro.Join(3000), "concurrent busy handoff completes without deadlock");
            Check(output.Count == 3 && output[0] == "idle-down" && output[1] == "idle-up" && output[2] == "macro-start", "concurrent macro waits for owned pulse release");
        }
        downStarted.Dispose(); allowDownFinish.Dispose();
    }
}
