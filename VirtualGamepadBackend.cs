using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Exceptions;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace InputStitch
{
    // Concrete virtual-controller implementations live behind this boundary. Higher-level
    // runtime/ownership code continues to use GamepadOutput and does not depend on ViGEm types.
    internal interface IVirtualGamepadBackend
    {
        string ConnectedType { get; }
        bool IsConnected { get; }
        int OwnXboxUserIndex { get; }
        void Connect(string normalizedType);
        void Send(InputSpec input, bool down);
        void NeutralizeAll();
        void Disconnect();
    }

    internal sealed class VigemVirtualGamepadBackend : IVirtualGamepadBackend
    {
        private readonly HashSet<GamepadControl> dPadHeld = new HashSet<GamepadControl>();
        private ViGEmClient client;
        private IVirtualGamepad controller;
        private IXbox360Controller xbox;
        private IDualShock4Controller ds4;
        private string connectedType = "";

        public string ConnectedType { get { return controller == null ? "" : connectedType; } }
        public bool IsConnected { get { return controller != null; } }

        public int OwnXboxUserIndex
        {
            get
            {
                if (xbox == null) return -1;
                try { return (int)xbox.UserIndex; }
                catch { return -1; }
            }
        }

        public void Connect(string normalizedType)
        {
            try
            {
                client = new ViGEmClient();
                if (string.Equals(normalizedType, VirtualGamepadTypes.DualShock4, StringComparison.OrdinalIgnoreCase))
                {
                    ds4 = client.CreateDualShock4Controller();
                    controller = ds4;
                }
                else
                {
                    xbox = client.CreateXbox360Controller();
                    controller = xbox;
                }
                controller.AutoSubmitReport = false;
                controller.Connect();
                connectedType = normalizedType;
                SetNeutralReport();
            }
            catch (Exception ex)
            {
                throw Wrap(ex);
            }
        }

        public void Send(InputSpec input, bool down)
        {
            if (input == null || input.Kind != InputKind.Gamepad) return;
            if (controller == null)
                throw new GamepadOutputException(GamepadFailureKind.Other, Localizer.T("虚拟手柄未连接。"), null);
            try
            {
                Apply(input, down);
                controller.SubmitReport();
            }
            catch (Exception ex)
            {
                throw ex as GamepadOutputException ?? Wrap(ex);
            }
        }

        public void NeutralizeAll()
        {
            if (controller == null) return;
            SetNeutralReport();
        }

        public void Disconnect()
        {
            if (controller != null)
            {
                try { SetNeutralReport(); } catch { }
                try { controller.Disconnect(); } catch { }
            }
            if (ds4 != null) { try { ds4.Dispose(); } catch { } }
            if (client != null) { try { client.Dispose(); } catch { } }
            controller = null;
            xbox = null;
            ds4 = null;
            client = null;
            connectedType = "";
            dPadHeld.Clear();
        }

        private void Apply(InputSpec input, bool down)
        {
            GamepadControl control = input.GamepadControl;
            if (control == GamepadControl.LeftStick || control == GamepadControl.RightStick)
            {
                int x = down ? Clamp(input.GamepadX, -100, 100) : 0;
                int y = down ? Clamp(input.GamepadY, -100, 100) : 0;
                if (xbox != null)
                {
                    Xbox360Axis xAxis = control == GamepadControl.LeftStick ? Xbox360Axis.LeftThumbX : Xbox360Axis.RightThumbX;
                    Xbox360Axis yAxis = control == GamepadControl.LeftStick ? Xbox360Axis.LeftThumbY : Xbox360Axis.RightThumbY;
                    xbox.SetAxisValue(xAxis, PercentToSignedAxis(x));
                    xbox.SetAxisValue(yAxis, PercentToSignedAxis(y));
                }
                else if (ds4 != null)
                {
                    DualShock4Axis xAxis = control == GamepadControl.LeftStick ? DualShock4Axis.LeftThumbX : DualShock4Axis.RightThumbX;
                    DualShock4Axis yAxis = control == GamepadControl.LeftStick ? DualShock4Axis.LeftThumbY : DualShock4Axis.RightThumbY;
                    ds4.SetAxisValue(xAxis, PercentToDs4Axis(x, false));
                    ds4.SetAxisValue(yAxis, PercentToDs4Axis(y, true));
                }
                return;
            }

            if (control == GamepadControl.LeftTrigger || control == GamepadControl.RightTrigger)
            {
                byte value = down ? PercentToByte(Clamp(input.GamepadValue, 0, 100)) : (byte)0;
                if (xbox != null)
                    xbox.SetSliderValue(control == GamepadControl.LeftTrigger ? Xbox360Slider.LeftTrigger : Xbox360Slider.RightTrigger, value);
                else if (ds4 != null)
                {
                    ds4.SetSliderValue(control == GamepadControl.LeftTrigger ? DualShock4Slider.LeftTrigger : DualShock4Slider.RightTrigger, value);
                    ds4.SetButtonState(control == GamepadControl.LeftTrigger ? DualShock4Button.TriggerLeft : DualShock4Button.TriggerRight, down && value > 0);
                }
                return;
            }

            if (IsDPad(control))
            {
                if (down) dPadHeld.Add(control); else dPadHeld.Remove(control);
                if (xbox != null)
                    xbox.SetButtonState(ToXboxButton(control), down);
                else if (ds4 != null)
                    ds4.SetDPadDirection(CurrentDs4DPadDirection());
                return;
            }

            if (xbox != null) xbox.SetButtonState(ToXboxButton(control), down);
            else if (ds4 != null) ds4.SetButtonState(ToDs4Button(control), down);
        }

        private void SetNeutralReport()
        {
            if (controller == null) return;
            dPadHeld.Clear();
            controller.ResetReport();
            if (ds4 != null)
            {
                ds4.SetAxisValue(DualShock4Axis.LeftThumbX, 128);
                ds4.SetAxisValue(DualShock4Axis.LeftThumbY, 128);
                ds4.SetAxisValue(DualShock4Axis.RightThumbX, 128);
                ds4.SetAxisValue(DualShock4Axis.RightThumbY, 128);
                ds4.SetDPadDirection(DualShock4DPadDirection.None);
            }
            controller.SubmitReport();
        }

        private static GamepadOutputException Wrap(Exception ex)
        {
            Exception current = ex;
            while ((current is TargetInvocationException || current is TypeInitializationException || current is AggregateException) && current.InnerException != null)
                current = current.InnerException;
            if (current is VigemBusNotFoundException)
                return new GamepadOutputException(GamepadFailureKind.DriverMissing, Localizer.T("虚拟手柄驱动未安装或未运行。"), current);
            if (current is VigemBusVersionMismatchException)
                return new GamepadOutputException(GamepadFailureKind.VersionMismatch, Localizer.T("已安装的 ViGEmBus 与客户端不兼容。"), current);
            if (current is VigemBusAccessFailedException)
                return new GamepadOutputException(GamepadFailureKind.AccessFailed, Localizer.T("无法访问 ViGEmBus。请尝试重新安装官方驱动。"), current);
            if (current is VigemNoFreeSlotException)
                return new GamepadOutputException(GamepadFailureKind.NoFreeSlot, Localizer.T("没有可用的虚拟手柄槽位。请关闭其他虚拟手柄工具后重试。"), current);
            if (current is DllNotFoundException || current is BadImageFormatException || current is FileLoadException)
                return new GamepadOutputException(GamepadFailureKind.AccessFailed, Localizer.T("虚拟手柄客户端未能加载，可能被安全软件拦截或文件架构不匹配。"), current);
            return new GamepadOutputException(GamepadFailureKind.Other, current == null ? Localizer.T("驱动检测失败") : current.Message, current);
        }

        private static bool IsDPad(GamepadControl control)
        {
            return control == GamepadControl.DPadUp || control == GamepadControl.DPadDown ||
                   control == GamepadControl.DPadLeft || control == GamepadControl.DPadRight;
        }

        private static Xbox360Button ToXboxButton(GamepadControl control)
        {
            switch (control)
            {
                case GamepadControl.South: return Xbox360Button.A;
                case GamepadControl.East: return Xbox360Button.B;
                case GamepadControl.West: return Xbox360Button.X;
                case GamepadControl.North: return Xbox360Button.Y;
                case GamepadControl.DPadUp: return Xbox360Button.Up;
                case GamepadControl.DPadDown: return Xbox360Button.Down;
                case GamepadControl.DPadLeft: return Xbox360Button.Left;
                case GamepadControl.DPadRight: return Xbox360Button.Right;
                case GamepadControl.LeftShoulder: return Xbox360Button.LeftShoulder;
                case GamepadControl.RightShoulder: return Xbox360Button.RightShoulder;
                case GamepadControl.Back: return Xbox360Button.Back;
                case GamepadControl.Start: return Xbox360Button.Start;
                case GamepadControl.LeftThumb: return Xbox360Button.LeftThumb;
                case GamepadControl.RightThumb: return Xbox360Button.RightThumb;
                case GamepadControl.Guide: return Xbox360Button.Guide;
                default: throw new ArgumentOutOfRangeException("control");
            }
        }

        private static DualShock4Button ToDs4Button(GamepadControl control)
        {
            switch (control)
            {
                case GamepadControl.South: return DualShock4Button.Cross;
                case GamepadControl.East: return DualShock4Button.Circle;
                case GamepadControl.West: return DualShock4Button.Square;
                case GamepadControl.North: return DualShock4Button.Triangle;
                case GamepadControl.LeftShoulder: return DualShock4Button.ShoulderLeft;
                case GamepadControl.RightShoulder: return DualShock4Button.ShoulderRight;
                case GamepadControl.Back: return DualShock4Button.Share;
                case GamepadControl.Start: return DualShock4Button.Options;
                case GamepadControl.LeftThumb: return DualShock4Button.ThumbLeft;
                case GamepadControl.RightThumb: return DualShock4Button.ThumbRight;
                case GamepadControl.Guide: return DualShock4SpecialButton.Ps;
                default: throw new ArgumentOutOfRangeException("control");
            }
        }

        private DualShock4DPadDirection CurrentDs4DPadDirection()
        {
            bool up = dPadHeld.Contains(GamepadControl.DPadUp);
            bool down = dPadHeld.Contains(GamepadControl.DPadDown);
            bool left = dPadHeld.Contains(GamepadControl.DPadLeft);
            bool right = dPadHeld.Contains(GamepadControl.DPadRight);
            if (up && left) return DualShock4DPadDirection.Northwest;
            if (up && right) return DualShock4DPadDirection.Northeast;
            if (down && left) return DualShock4DPadDirection.Southwest;
            if (down && right) return DualShock4DPadDirection.Southeast;
            if (up) return DualShock4DPadDirection.North;
            if (down) return DualShock4DPadDirection.South;
            if (left) return DualShock4DPadDirection.West;
            if (right) return DualShock4DPadDirection.East;
            return DualShock4DPadDirection.None;
        }

        private static short PercentToSignedAxis(int percent)
        {
            if (percent <= -100) return short.MinValue;
            if (percent >= 100) return short.MaxValue;
            return (short)Math.Round(percent * 327.67);
        }

        private static byte PercentToDs4Axis(int percent, bool invert)
        {
            int p = invert ? -percent : percent;
            int value = p < 0
                ? (int)Math.Round(128.0 + p * 128.0 / 100.0)
                : (int)Math.Round(128.0 + p * 127.0 / 100.0);
            return (byte)Clamp(value, 0, 255);
        }

        private static byte PercentToByte(int percent)
        {
            return (byte)Clamp((int)Math.Round(percent * 255.0 / 100.0), 0, 255);
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }
}
