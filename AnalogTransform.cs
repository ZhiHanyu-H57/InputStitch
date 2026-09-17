using System;
using System.Collections.Generic;
using System.Globalization;

namespace InputStitch
{
    internal enum AnalogZoneMatch
    {
        Below,
        Inside,
        Above
    }

    [Serializable]
    public sealed class AnalogStickTransformConfig
    {
        public int InnerDeadzonePercent = 0;
        public int OuterDeadzonePercent = 0;
        public int CurveExponentPercent = 100;
        public int ScalePercent = 100;
        public bool InvertX;
        public bool InvertY;
        public int MaxOutputPercent = 100;

        public AnalogStickTransformConfig Clone()
        {
            AnalogStickTransformConfig copy = new AnalogStickTransformConfig();
            copy.InnerDeadzonePercent = InnerDeadzonePercent;
            copy.OuterDeadzonePercent = OuterDeadzonePercent;
            copy.CurveExponentPercent = CurveExponentPercent;
            copy.ScalePercent = ScalePercent;
            copy.InvertX = InvertX;
            copy.InvertY = InvertY;
            copy.MaxOutputPercent = MaxOutputPercent;
            Normalize(copy);
            return copy;
        }

        public static void Normalize(AnalogStickTransformConfig value)
        {
            if (value == null) return;
            value.InnerDeadzonePercent = Clamp(value.InnerDeadzonePercent, 0, 99);
            value.OuterDeadzonePercent = Clamp(value.OuterDeadzonePercent, 0, 99 - value.InnerDeadzonePercent);
            value.CurveExponentPercent = Clamp(value.CurveExponentPercent, 25, 400);
            value.ScalePercent = Clamp(value.ScalePercent, 0, 300);
            value.MaxOutputPercent = Clamp(value.MaxOutputPercent, 0, 100);
        }

        public bool IsIdentity
        {
            get
            {
                return InnerDeadzonePercent == 0 && OuterDeadzonePercent == 0 && CurveExponentPercent == 100 &&
                    ScalePercent == 100 && !InvertX && !InvertY && MaxOutputPercent == 100;
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }

    [Serializable]
    public sealed class AnalogTriggerTransformConfig
    {
        public int InnerDeadzonePercent = 0;
        public int OuterDeadzonePercent = 0;
        public int CurveExponentPercent = 100;
        public int ScalePercent = 100;
        public int MaxOutputPercent = 100;

        public AnalogTriggerTransformConfig Clone()
        {
            AnalogTriggerTransformConfig copy = new AnalogTriggerTransformConfig();
            copy.InnerDeadzonePercent = InnerDeadzonePercent;
            copy.OuterDeadzonePercent = OuterDeadzonePercent;
            copy.CurveExponentPercent = CurveExponentPercent;
            copy.ScalePercent = ScalePercent;
            copy.MaxOutputPercent = MaxOutputPercent;
            Normalize(copy);
            return copy;
        }

        public static void Normalize(AnalogTriggerTransformConfig value)
        {
            if (value == null) return;
            value.InnerDeadzonePercent = Clamp(value.InnerDeadzonePercent, 0, 99);
            value.OuterDeadzonePercent = Clamp(value.OuterDeadzonePercent, 0, 99 - value.InnerDeadzonePercent);
            value.CurveExponentPercent = Clamp(value.CurveExponentPercent, 25, 400);
            value.ScalePercent = Clamp(value.ScalePercent, 0, 300);
            value.MaxOutputPercent = Clamp(value.MaxOutputPercent, 0, 100);
        }

        public bool IsIdentity
        {
            get
            {
                return InnerDeadzonePercent == 0 && OuterDeadzonePercent == 0 && CurveExponentPercent == 100 &&
                    ScalePercent == 100 && MaxOutputPercent == 100;
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }

    [Serializable]
    public sealed class AnalogTransformProfile
    {
        public AnalogStickTransformConfig LeftStick = new AnalogStickTransformConfig();
        public AnalogStickTransformConfig RightStick = new AnalogStickTransformConfig();
        public AnalogTriggerTransformConfig LeftTrigger = new AnalogTriggerTransformConfig();
        public AnalogTriggerTransformConfig RightTrigger = new AnalogTriggerTransformConfig();

        public AnalogTransformProfile Clone()
        {
            AnalogTransformProfile copy = new AnalogTransformProfile();
            copy.LeftStick = LeftStick == null ? new AnalogStickTransformConfig() : LeftStick.Clone();
            copy.RightStick = RightStick == null ? new AnalogStickTransformConfig() : RightStick.Clone();
            copy.LeftTrigger = LeftTrigger == null ? new AnalogTriggerTransformConfig() : LeftTrigger.Clone();
            copy.RightTrigger = RightTrigger == null ? new AnalogTriggerTransformConfig() : RightTrigger.Clone();
            Normalize(copy);
            return copy;
        }

        public static void Normalize(AnalogTransformProfile value)
        {
            if (value == null) return;
            if (value.LeftStick == null) value.LeftStick = new AnalogStickTransformConfig();
            if (value.RightStick == null) value.RightStick = new AnalogStickTransformConfig();
            if (value.LeftTrigger == null) value.LeftTrigger = new AnalogTriggerTransformConfig();
            if (value.RightTrigger == null) value.RightTrigger = new AnalogTriggerTransformConfig();
            AnalogStickTransformConfig.Normalize(value.LeftStick);
            AnalogStickTransformConfig.Normalize(value.RightStick);
            AnalogTriggerTransformConfig.Normalize(value.LeftTrigger);
            AnalogTriggerTransformConfig.Normalize(value.RightTrigger);
        }

        public bool IsIdentity
        {
            get
            {
                return LeftStick != null && RightStick != null && LeftTrigger != null && RightTrigger != null &&
                    LeftStick.IsIdentity && RightStick.IsIdentity && LeftTrigger.IsIdentity && RightTrigger.IsIdentity;
            }
        }

        public string Summary
        {
            get
            {
                if (IsIdentity) return "identity";
                List<string> parts = new List<string>();
                if (!LeftStick.IsIdentity) parts.Add("LS=" + AnalogTransformEngine.StickSummary(LeftStick));
                if (!RightStick.IsIdentity) parts.Add("RS=" + AnalogTransformEngine.StickSummary(RightStick));
                if (!LeftTrigger.IsIdentity) parts.Add("LT=" + AnalogTransformEngine.TriggerSummary(LeftTrigger));
                if (!RightTrigger.IsIdentity) parts.Add("RT=" + AnalogTransformEngine.TriggerSummary(RightTrigger));
                return string.Join("; ", parts.ToArray());
            }
        }
    }

    // Reusable pre-ownership analog transform layer. The stable operation order is:
    // deadzone remap -> response curve -> scale -> inversion -> max-output clamp.
    // Identity profiles bypass all math exactly so legacy Router contributions remain unchanged.
    internal static class AnalogTransformEngine
    {
        internal static List<InputSpec> Apply(IEnumerable<InputSpec> inputs, AnalogTransformProfile profile)
        {
            AnalogTransformProfile effective = profile == null ? new AnalogTransformProfile() : profile.Clone();
            bool identity = effective.IsIdentity;
            List<InputSpec> result = new List<InputSpec>();
            if (inputs == null) return result;
            foreach (InputSpec input in inputs)
            {
                if (input == null) continue;
                InputSpec copy = input.Clone();
                if (!identity) ApplyInPlace(copy, effective);
                if (!IsNeutralAnalog(copy)) result.Add(copy);
            }
            return result;
        }

        internal static InputSpec Apply(InputSpec input, AnalogTransformProfile profile)
        {
            if (input == null) return null;
            AnalogTransformProfile effective = profile == null ? new AnalogTransformProfile() : profile.Clone();
            InputSpec copy = input.Clone();
            if (!effective.IsIdentity) ApplyInPlace(copy, effective);
            return copy;
        }

        internal static void ApplyInPlace(InputSpec input, AnalogTransformProfile profile)
        {
            if (input == null || profile == null || input.Kind != InputKind.Gamepad) return;
            if (input.GamepadControl == GamepadControl.LeftStick)
                TransformStick(ref input.GamepadX, ref input.GamepadY, profile.LeftStick);
            else if (input.GamepadControl == GamepadControl.RightStick)
                TransformStick(ref input.GamepadX, ref input.GamepadY, profile.RightStick);
            else if (input.GamepadControl == GamepadControl.LeftTrigger)
                input.GamepadValue = TransformTrigger(input.GamepadValue, profile.LeftTrigger);
            else if (input.GamepadControl == GamepadControl.RightTrigger)
                input.GamepadValue = TransformTrigger(input.GamepadValue, profile.RightTrigger);
        }

        internal static void TransformStick(ref int x, ref int y, AnalogStickTransformConfig config)
        {
            AnalogStickTransformConfig effective = config == null ? new AnalogStickTransformConfig() : config.Clone();
            if (effective.IsIdentity) return;

            x = Clamp(x, -100, 100);
            y = Clamp(y, -100, 100);
            if (x == 0 && y == 0) return;

            double magnitude = Math.Sqrt((double)x * (double)x + (double)y * (double)y);
            double normalizedMagnitude = Math.Min(1.0, magnitude / 100.0);
            double outputMagnitude = TransformMagnitude(normalizedMagnitude,
                effective.InnerDeadzonePercent, effective.OuterDeadzonePercent,
                effective.CurveExponentPercent, effective.ScalePercent, effective.MaxOutputPercent);
            if (outputMagnitude <= 0.0)
            {
                x = 0;
                y = 0;
                return;
            }

            double directionX = x / magnitude;
            double directionY = y / magnitude;
            if (effective.InvertX) directionX = -directionX;
            if (effective.InvertY) directionY = -directionY;
            x = Clamp(RoundPercent(directionX * outputMagnitude * 100.0), -100, 100);
            y = Clamp(RoundPercent(directionY * outputMagnitude * 100.0), -100, 100);
        }

        internal static int TransformTrigger(int value, AnalogTriggerTransformConfig config)
        {
            AnalogTriggerTransformConfig effective = config == null ? new AnalogTriggerTransformConfig() : config.Clone();
            value = Clamp(value, 0, 100);
            if (effective.IsIdentity) return value;
            double output = TransformMagnitude(value / 100.0,
                effective.InnerDeadzonePercent, effective.OuterDeadzonePercent,
                effective.CurveExponentPercent, effective.ScalePercent, effective.MaxOutputPercent);
            return Clamp(RoundPercent(output * 100.0), 0, 100);
        }

        // Scalar signed-axis primitive for later mapping/half-axis consumers. Router sticks use the
        // radial vector transform above so an axial deadzone does not distort stick direction.
        internal static int TransformSignedAxis(int value, AnalogStickTransformConfig config, int halfAxis)
        {
            AnalogStickTransformConfig effective = config == null ? new AnalogStickTransformConfig() : config.Clone();
            value = Clamp(value, -100, 100);
            if (halfAxis > 0) value = Math.Max(0, value);
            else if (halfAxis < 0) value = Math.Max(0, -value);
            double sign = value < 0 ? -1.0 : 1.0;
            double magnitude = Math.Abs(value) / 100.0;
            double output = TransformMagnitude(magnitude, effective.InnerDeadzonePercent, effective.OuterDeadzonePercent,
                effective.CurveExponentPercent, effective.ScalePercent, effective.MaxOutputPercent);
            return Clamp(RoundPercent(sign * output * 100.0), -100, 100);
        }

        // Threshold/zone primitive for the later Condition engine. It deliberately classifies
        // magnitude only; callers decide whether a particular signed direction/half-axis matters.
        internal static AnalogZoneMatch ClassifyMagnitude(int value, int minimumPercent, int maximumPercent)
        {
            int minimum = Clamp(minimumPercent, 0, 100);
            int maximum = Clamp(maximumPercent, 0, 100);
            if (maximum < minimum)
            {
                int swap = minimum;
                minimum = maximum;
                maximum = swap;
            }
            int magnitude = Clamp(Math.Abs(value), 0, 100);
            if (magnitude < minimum) return AnalogZoneMatch.Below;
            if (magnitude > maximum) return AnalogZoneMatch.Above;
            return AnalogZoneMatch.Inside;
        }

        internal static double TransformMagnitude(double magnitude, int innerDeadzonePercent, int outerDeadzonePercent,
            int curveExponentPercent, int scalePercent, int maxOutputPercent)
        {
            magnitude = Clamp01(magnitude);
            int inner = Clamp(innerDeadzonePercent, 0, 99);
            int outer = Clamp(outerDeadzonePercent, 0, 99 - inner);
            double innerPoint = inner / 100.0;
            double outerPoint = 1.0 - outer / 100.0;
            double mapped;
            if (magnitude <= innerPoint) mapped = 0.0;
            else if (magnitude >= outerPoint) mapped = 1.0;
            else mapped = (magnitude - innerPoint) / Math.Max(0.01, outerPoint - innerPoint);

            double exponent = Clamp(curveExponentPercent, 25, 400) / 100.0;
            mapped = Math.Pow(Clamp01(mapped), exponent);
            mapped *= Clamp(scalePercent, 0, 300) / 100.0;
            mapped = Math.Min(mapped, Clamp(maxOutputPercent, 0, 100) / 100.0);
            return Clamp01(mapped);
        }

        internal static string StickSummary(AnalogStickTransformConfig value)
        {
            AnalogStickTransformConfig x = value == null ? new AnalogStickTransformConfig() : value.Clone();
            return "dz=" + x.InnerDeadzonePercent.ToString(CultureInfo.InvariantCulture) + "/" + x.OuterDeadzonePercent.ToString(CultureInfo.InvariantCulture) +
                ",curve=" + x.CurveExponentPercent.ToString(CultureInfo.InvariantCulture) +
                ",scale=" + x.ScalePercent.ToString(CultureInfo.InvariantCulture) +
                ",inv=" + (x.InvertX ? "X" : "") + (x.InvertY ? "Y" : "") +
                ",max=" + x.MaxOutputPercent.ToString(CultureInfo.InvariantCulture);
        }

        internal static string TriggerSummary(AnalogTriggerTransformConfig value)
        {
            AnalogTriggerTransformConfig x = value == null ? new AnalogTriggerTransformConfig() : value.Clone();
            return "dz=" + x.InnerDeadzonePercent.ToString(CultureInfo.InvariantCulture) + "/" + x.OuterDeadzonePercent.ToString(CultureInfo.InvariantCulture) +
                ",curve=" + x.CurveExponentPercent.ToString(CultureInfo.InvariantCulture) +
                ",scale=" + x.ScalePercent.ToString(CultureInfo.InvariantCulture) +
                ",max=" + x.MaxOutputPercent.ToString(CultureInfo.InvariantCulture);
        }

        private static bool IsNeutralAnalog(InputSpec input)
        {
            if (input == null || input.Kind != InputKind.Gamepad) return false;
            if (input.GamepadControl == GamepadControl.LeftStick || input.GamepadControl == GamepadControl.RightStick)
                return input.GamepadX == 0 && input.GamepadY == 0;
            if (input.GamepadControl == GamepadControl.LeftTrigger || input.GamepadControl == GamepadControl.RightTrigger)
                return input.GamepadValue <= 0;
            return false;
        }

        private static int RoundPercent(double value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        private static double Clamp01(double value)
        {
            return value < 0.0 ? 0.0 : (value > 1.0 ? 1.0 : value);
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }
}
