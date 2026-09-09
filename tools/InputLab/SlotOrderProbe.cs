using System;
using System.Collections.Generic;
using System.Threading;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;

internal static class SlotOrderProbe
{
    private static int ReadIndex(IXbox360Controller pad, string name)
    {
        for (int i = 0; i < 50; i++)
        {
            try
            {
                int index = pad.UserIndex;
                Console.WriteLine(name + " UserIndex=" + index);
                return index;
            }
            catch { Thread.Sleep(50); }
        }
        Console.WriteLine(name + " UserIndex=UNREPORTED");
        return -1;
    }

    private static void SafeDisconnect(IXbox360Controller pad)
    {
        if (pad == null) return;
        try { pad.Disconnect(); } catch { }
    }

    private static void SafeDispose(IXbox360Controller pad)
    {
        IDisposable disposable = pad as IDisposable;
        if (disposable != null) try { disposable.Dispose(); } catch { }
    }

    public static int Main()
    {
        using (ViGEmClient client = new ViGEmClient())
        {
            List<IXbox360Controller> firstExternal = new List<IXbox360Controller>();
            List<IXbox360Controller> restoredExternal = new List<IXbox360Controller>();
            IXbox360Controller firstOwn = null;
            IXbox360Controller finalOwn = null;
            try
            {
                Console.WriteLine("Neutral ViGEm slot-order probe; no button/stick/trigger report is submitted.");
                Console.WriteLine("PHASE1 three external controllers connect before InputStitch");
                bool initialExpected = true;
                for (int i = 0; i < 3; i++)
                {
                    IXbox360Controller pad = client.CreateXbox360Controller();
                    firstExternal.Add(pad);
                    pad.Connect();
                    initialExpected &= ReadIndex(pad, "external-initial-" + i.ToString()) == i;
                }
                firstOwn = client.CreateXbox360Controller();
                firstOwn.Connect();
                initialExpected &= ReadIndex(firstOwn, "inputstitch-initial") == 3;

                Console.WriteLine("PHASE2 remove InputStitch, then all external controllers");
                SafeDisconnect(firstOwn);
                Thread.Sleep(200);
                for (int i = firstExternal.Count - 1; i >= 0; i--) SafeDisconnect(firstExternal[i]);
                Thread.Sleep(400);

                Console.WriteLine("PHASE3 InputStitch reconnects first");
                finalOwn = client.CreateXbox360Controller();
                finalOwn.Connect();
                bool finalExpected = ReadIndex(finalOwn, "inputstitch-final") == 0;

                Console.WriteLine("PHASE4 restore all external controllers after InputStitch");
                for (int i = 0; i < 3; i++)
                {
                    IXbox360Controller pad = client.CreateXbox360Controller();
                    restoredExternal.Add(pad);
                    pad.Connect();
                    finalExpected &= ReadIndex(pad, "external-final-" + i.ToString()) == i + 1;
                }

                bool pass = initialExpected && finalExpected;
                Console.WriteLine("EXPECTED_ORDER=" + pass.ToString());
                return pass ? 0 : 2;
            }
            finally
            {
                for (int i = restoredExternal.Count - 1; i >= 0; i--) SafeDisconnect(restoredExternal[i]);
                SafeDisconnect(finalOwn);
                SafeDisconnect(firstOwn);
                for (int i = firstExternal.Count - 1; i >= 0; i--) SafeDisconnect(firstExternal[i]);
                foreach (IXbox360Controller pad in restoredExternal) SafeDispose(pad);
                SafeDispose(finalOwn);
                SafeDispose(firstOwn);
                foreach (IXbox360Controller pad in firstExternal) SafeDispose(pad);
            }
        }
    }
}
