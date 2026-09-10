using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace InputStitch
{
    internal static class ConfigPackageSerializer
    {
        internal static MacroConfig CloneConfig(MacroConfig source)
        {
            if (source == null) return new MacroConfig();
            XmlSerializer xs = new XmlSerializer(typeof(MacroConfig));
            using (MemoryStream ms = new MemoryStream())
            {
                xs.Serialize(ms, source);
                ms.Position = 0;
                MacroConfig clone = xs.Deserialize(ms) as MacroConfig;
                if (clone == null) clone = new MacroConfig();
                NormalizeConfig(clone);
                return clone;
            }
        }

        internal static void SerializeMacroPackageToFile(MacroPackage value, string path)
        {
            if (value == null || value.Macros == null) throw new InvalidDataException("宏包为空。");
            value.FormatVersion = AppInfo.MacroPackageFormatVersion;
            XmlSerializer xs = new XmlSerializer(typeof(MacroPackage));
            using (FileStream fs = File.Create(path)) xs.Serialize(fs, value);
        }

        internal static void SerializeProfilePackageToFile(ProfilePackage value, string path)
        {
            if (value == null || value.Config == null) throw new InvalidDataException("配置方案为空。");
            value.FormatVersion = AppInfo.ProfileFormatVersion;
            XmlSerializer xs = new XmlSerializer(typeof(ProfilePackage));
            using (FileStream fs = File.Create(path)) xs.Serialize(fs, value);
        }

        internal static ProfilePackage DeserializeProfilePackageOrLegacy(string path)
        {
            FileInfo info = new FileInfo(path);
            if (!info.Exists) throw new FileNotFoundException("配置方案不存在。", path);
            if (info.Length > 8L * 1024L * 1024L) throw new InvalidDataException("配置方案过大，已拒绝载入。");
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(ProfilePackage));
                using (FileStream fs = File.OpenRead(path))
                {
                    ProfilePackage p = xs.Deserialize(fs) as ProfilePackage;
                    if (p != null && p.Config != null)
                    {
                        EnsureSupportedFormatVersion(p.FormatVersion, AppInfo.ProfileFormatVersion, "配置方案");
                        NormalizeConfig(p.Config);
                        return p;
                    }
                }
            }
            catch (InvalidOperationException) { }

            MacroConfig legacy = DeserializeConfigFromFile(path);
            NormalizeConfig(legacy);
            ProfilePackage converted = new ProfilePackage();
            converted.ProfileName = Path.GetFileNameWithoutExtension(path);
            converted.Config = legacy;
            return converted;
        }

        internal static MacroPackage DeserializeMacroPackageOrLegacyConfig(string path)
        {
            FileInfo info = new FileInfo(path);
            if (!info.Exists) throw new FileNotFoundException("文件不存在。", path);
            if (info.Length > 8L * 1024L * 1024L) throw new InvalidDataException("文件过大，已拒绝导入。");

            try
            {
                XmlSerializer packageSerializer = new XmlSerializer(typeof(MacroPackage));
                using (FileStream fs = File.OpenRead(path))
                {
                    MacroPackage package = packageSerializer.Deserialize(fs) as MacroPackage;
                    if (package != null)
                    {
                        EnsureSupportedFormatVersion(package.FormatVersion, AppInfo.MacroPackageFormatVersion, "宏包");
                        if (package.Macros == null) package.Macros = new List<MacroDefinition>();
                        NormalizeMacroDefinitions(package.Macros);
                        return package;
                    }
                }
            }
            catch (InvalidOperationException) { }

            try
            {
                ProfilePackage profile = DeserializeProfilePackageOrLegacy(path);
                if (profile != null && profile.Config != null)
                {
                    MacroPackage convertedProfile = new MacroPackage();
                    convertedProfile.Macros = profile.Config.Macros == null ? new List<MacroDefinition>() : profile.Config.Macros;
                    NormalizeMacroDefinitions(convertedProfile.Macros);
                    return convertedProfile;
                }
            }
            catch { }

            try
            {
                MacroConfig legacy = DeserializeConfigFromFile(path);
                NormalizeConfig(legacy);
                MacroPackage converted = new MacroPackage();
                converted.Macros = legacy.Macros == null ? new List<MacroDefinition>() : legacy.Macros;
                NormalizeMacroDefinitions(converted.Macros);
                return converted;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("不是有效的 InputStitch 宏包、配置方案或兼容旧配置。", ex);
            }
        }

        internal static void NormalizeMacroDefinitions(List<MacroDefinition> macros)
        {
            if (macros == null) return;
            macros.RemoveAll(delegate(MacroDefinition item) { return item == null; });
            foreach (MacroDefinition m in macros)
            {
                if (string.IsNullOrWhiteSpace(m.Name)) m.Name = "未命名宏";
                if (m.Description == null) m.Description = "";
                if (m.Trigger == null) m.Trigger = new TriggerSpec();
                if (m.Trigger.Kind == InputKind.Gamepad)
                {
                    if (!Enum.IsDefined(typeof(GamepadControl), m.Trigger.GamepadControl)) m.Trigger.GamepadControl = GamepadControl.South;
                    if (m.Trigger.GamepadUserIndex < -1 || m.Trigger.GamepadUserIndex > 3) m.Trigger.GamepadUserIndex = -1;
                    m.Trigger.Ctrl = m.Trigger.Shift = m.Trigger.Alt = m.Trigger.Win = false;
                }
                if (m.RepeatCount < 1) m.RepeatCount = 1;
                if (m.RepeatCount > 100000000) m.RepeatCount = 100000000;
                if (m.Steps == null) m.Steps = new List<MacroStep>();
                m.Steps.RemoveAll(delegate(MacroStep item) { return item == null; });
                foreach (MacroStep step in m.Steps)
                {
                    if (step.GamepadX < -100) step.GamepadX = -100;
                    if (step.GamepadX > 100) step.GamepadX = 100;
                    if (step.GamepadY < -100) step.GamepadY = -100;
                    if (step.GamepadY > 100) step.GamepadY = 100;
                    if (step.GamepadValue < 0) step.GamepadValue = 0;
                    if (step.GamepadValue > 100) step.GamepadValue = 100;
                    if (step.HoldMs < 0) step.HoldMs = 0;
                    if (step.HoldMs > 600000) step.HoldMs = 600000;
                    if (step.DelayMs < 0) step.DelayMs = 0;
                    if (step.DelayMs > 600000) step.DelayMs = 600000;
                    if (step.RandomDelayMinMs < 0) step.RandomDelayMinMs = 0;
                    if (step.RandomDelayMaxMs < 0) step.RandomDelayMaxMs = 0;
                    if (step.RandomDelayMinMs > 600000) step.RandomDelayMinMs = 600000;
                    if (step.RandomDelayMaxMs > 600000) step.RandomDelayMaxMs = 600000;
                }
            }
        }

        internal static void EnsureSupportedFormatVersion(string found, string current, string label)
        {
            int foundValue;
            int currentValue;
            if (string.IsNullOrWhiteSpace(found) || !int.TryParse(found, out foundValue)) return;
            if (!int.TryParse(current, out currentValue)) return;
            if (foundValue > currentValue)
                throw new InvalidDataException(label + "由更高版本的 InputStitch 创建（格式版本 " + found + "），当前版本无法安全读取。请先更新程序。");
        }

        internal static MacroConfig DeserializeConfigFromFile(string path)
        {
            FileInfo info = new FileInfo(path);
            if (!info.Exists) throw new FileNotFoundException("配置文件不存在。", path);
            if (info.Length > 8L * 1024L * 1024L) throw new InvalidDataException("配置文件过大，已拒绝导入。");
            XmlSerializer xs = new XmlSerializer(typeof(MacroConfig));
            using (FileStream fs = File.OpenRead(path))
            {
                MacroConfig value = xs.Deserialize(fs) as MacroConfig;
                if (value == null) throw new InvalidDataException("不是有效的 InputStitch 配置文件。");
                NormalizeConfig(value);
                return value;
            }
        }

        internal static void NormalizeMappingLayers(MacroConfig value)
        {
            if (value.MappingLayers == null) value.MappingLayers = new List<MappingLayerDefinition>();
            value.MappingLayers.RemoveAll(delegate(MappingLayerDefinition item) { return item == null; });
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = value.MappingLayers.Count - 1; i >= 0; i--)
            {
                MappingLayerDefinition layer = value.MappingLayers[i];
                layer.Id = (layer.Id ?? "").Trim();
                if (layer.Id.Length == 0 || ids.Contains(layer.Id)) { value.MappingLayers.RemoveAt(i); continue; }
                ids.Add(layer.Id);
                layer.IsBase = string.Equals(layer.Id, MappingLayerIds.Base, StringComparison.OrdinalIgnoreCase);
                if (string.IsNullOrWhiteSpace(layer.Name)) layer.Name = layer.IsBase ? "基础层" : "映射层";
            }
            if (!ids.Contains(MappingLayerIds.Base))
                value.MappingLayers.Insert(0, new MappingLayerDefinition { Id = MappingLayerIds.Base, Name = "基础层", IsBase = true });
        }

        internal static void NormalizeConfig(MacroConfig value)
        {
            if (value == null) throw new InvalidDataException("配置为空。");
            EnsureSupportedFormatVersion(value.FormatVersion, AppInfo.ConfigFormatVersion, "配置文件");
            value.FormatVersion = AppInfo.ConfigFormatVersion;
            if (value.Macros == null) value.Macros = new List<MacroDefinition>();
            NormalizeMappingLayers(value);
            NormalizeMacroDefinitions(value.Macros);
            HashSet<string> validLayerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (MappingLayerDefinition layer in value.MappingLayers)
                if (layer != null && !string.IsNullOrWhiteSpace(layer.Id)) validLayerIds.Add(layer.Id);
            foreach (MacroDefinition macro in value.Macros)
            {
                if (macro == null) continue;
                macro.MappingLayerId = string.IsNullOrWhiteSpace(macro.MappingLayerId) ? MappingLayerIds.Base : macro.MappingLayerId.Trim();
                if (!validLayerIds.Contains(macro.MappingLayerId)) macro.MappingLayerId = MappingLayerIds.Base;
            }
            if (value.PanicTrigger == null) value.PanicTrigger = new MacroConfig().PanicTrigger;
            if (value.PanicTrigger.Kind == InputKind.Gamepad) value.PanicTrigger = new MacroConfig().PanicTrigger;
            if (!string.Equals(value.Language, Localizer.English, StringComparison.OrdinalIgnoreCase)) value.Language = Localizer.Chinese;
            if (!string.Equals(value.UpdateMode, UpdateModes.Manual, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(value.UpdateMode, UpdateModes.Disabled, StringComparison.OrdinalIgnoreCase))
                value.UpdateMode = UpdateModes.Automatic;
            value.GamepadDeviceType = VirtualGamepadTypes.Normalize(value.GamepadDeviceType);
            if (value.GamepadRouterEnabled) value.GamepadDeviceType = VirtualGamepadTypes.Xbox360;
            if (value.IdleGamepad == null) value.IdleGamepad = new IdleGamepadOptions();
            if (VirtualGamepadTypes.IsDisabled(value.GamepadDeviceType)) value.IdleGamepad.Enabled = false;
            value.LastShownReleaseSummaryVersion = value.LastShownReleaseSummaryVersion ?? "";
            value.IdleGamepad.TargetProcessName = value.IdleGamepad.TargetProcessName ?? "";
            value.IdleGamepad.TargetWindowTitle = value.IdleGamepad.TargetWindowTitle ?? "";
            value.IdleGamepad.TargetWindowClass = value.IdleGamepad.TargetWindowClass ?? "";
            if (!value.IdleGamepad.TargetScopeInitialized)
            {
                // One-time beta compatibility migration. Earlier beta.4 builds scoped idle input
                // through the main target window. Copy that identity once, then keep both targets independent.
                bool idleTargetEmpty = string.IsNullOrWhiteSpace(value.IdleGamepad.TargetProcessName)
                    && string.IsNullOrWhiteSpace(value.IdleGamepad.TargetWindowTitle)
                    && string.IsNullOrWhiteSpace(value.IdleGamepad.TargetWindowClass);
                if (value.IdleGamepad.Enabled && idleTargetEmpty)
                {
                    value.IdleGamepad.TargetProcessName = value.TargetProcessName ?? "";
                    value.IdleGamepad.TargetWindowTitle = value.TargetWindowTitle ?? "";
                    value.IdleGamepad.TargetWindowClass = value.TargetWindowClass ?? "";
                }
                value.IdleGamepad.TargetScopeInitialized = true;
            }
            if (value.UiRunStartDelayMs < 0) value.UiRunStartDelayMs = 0;
            if (value.UiRunStartDelayMs > 5000) value.UiRunStartDelayMs = 5000;
        }
    }
}
