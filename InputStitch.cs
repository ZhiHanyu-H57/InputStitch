using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

[assembly: AssemblyTitle("InputStitch")]
[assembly: AssemblyProduct("InputStitch")]
[assembly: AssemblyDescription("Lightweight visual keyboard and mouse macro tool for Windows")]
[assembly: AssemblyCompany("InputStitch Project")]
[assembly: AssemblyCopyright("Copyright © 2026")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]

namespace InputStitch
{
    public static class AppInfo
    {
        public const string ProductName = "InputStitch";
        public const string Version = "1.0.0";
        public const string ConfigFormatVersion = "2";
        public const string MacroPackageFormatVersion = "2";
        public const string ProfileFormatVersion = "2";
        public const string UpdateManifestUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases/latest/download/InputStitch-update.xml";
        public const string LatestReleaseUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases/latest";
    }

    public static class UpdateModes
    {
        public const string Automatic = "Automatic";
        public const string Manual = "Manual";
        public const string Disabled = "Disabled";
    }


    public static class Localizer
    {
        public const string Chinese = "zh-CN";
        public const string English = "en-US";
        private static string currentLanguage = Chinese;

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>()
        {
            { "键鼠宏工具", "Keyboard & Mouse Macro Tool" },
            { "宏列表", "Macros" },
            { "导入宏", "Import Macros" },
            { "导出宏", "Export Macros" },
            { "载入方案", "Load Profile" },
            { "保存方案", "Save Profile" },
            { "打开配置文件夹", "Open Config Folder" },
            { "工具 ▾", "Tools ▾" },
            { "设置...", "Settings..." },
            { "设置", "Settings" },
            { "常规", "General" },
            { "输入与安全", "Input & Safety" },
            { "目标窗口与方案", "Target Window & Profiles" },
            { "软件更新", "Software Updates" },
            { "这些选项通常保持默认即可。修改会在点击确定后生效。", "These options normally work best at their defaults. Changes apply after you click OK." },
            { "界面语言：", "Interface language:" },
            { "使用扫描码发送键盘输入（推荐游戏）", "Send keyboard input using scan codes (game-friendly)" },
            { "在编辑界面时暂停宏输出（推荐）", "Pause macro output while editing the UI (recommended)" },
            { "保持主窗口置顶", "Keep the main window always on top" },
            { "从界面运行时先切换到目标窗口", "Switch to the target window before UI-started runs" },
            { "切换后等待：", "Wait after switching:" },
            { "按前台程序自动切换已绑定方案", "Automatically switch bound profiles by foreground app" },
            { "更新方式：", "Update mode:" },
            { "启动时自动检查并提示安装", "Check automatically at startup and offer installation" },
            { "仅手动检查（推荐）", "Check only when requested (recommended)" },
            { "不检查更新", "Do not check for updates" },
            { "立即检查更新", "Check Now" },
            { "正在检查更新…", "Checking for updates…" },
            { "正在下载并验证更新…", "Downloading and verifying the update…" },
            { "已是最新版本。", "You already have the latest build." },
            { "检查更新失败。", "Update check failed." },
            { "发现可用更新", "Update Available" },
            { "安装更新", "Install Update" },
            { "稍后", "Later" },
            { "当前公开版本仍为 1.0.0，但 GitHub 上已有更新的安全构建。是否下载并安装？", "The public version is still 1.0.0, but a newer secured build is available on GitHub. Download and install it?" },
            { "更新已下载并通过 SHA-256 校验。InputStitch 将关闭、替换程序文件并重新启动。是否现在安装？", "The update was downloaded and passed SHA-256 verification. InputStitch will close, replace the program file, and restart. Install now?" },
            { "无法安装更新。", "The update could not be installed." },
            { "网络访问仅用于从官方 GitHub Release 检查和下载更新；下载后必须通过 SHA-256 校验。", "Network access is used only to check and download updates from the official GitHub Release; every download must pass SHA-256 verification." },
            { "恢复默认", "Restore Defaults" },
            { "宏信息", "Macro Details" },
            { "触发与循环", "Trigger and Repetition" },
            { "目标窗口", "Target Window" },
            { "新增", "New" },
            { "复制", "Copy" },
            { "删除", "Delete" },
            { "名称：", "Name:" },
            { "备注：", "Notes:" },
            { "启用全局触发", "Enable global trigger" },
            { "UI编辑保护（编辑时暂停宏）", "UI edit protection (pause while editing)" },
            { "触发键：", "Trigger:" },
            { "录制触发键", "Capture Trigger" },
            { "触发方式：", "Trigger mode:" },
            { "按一次切换启动/停止", "Press once to start/stop" },
            { "按住运行，松开停止", "Run while held, stop on release" },
            { "执行次数：", "Repeat count:" },
            { "无限循环（再次触发或点击停止）", "Infinite loop (trigger again or click Stop)" },
            { "无限循环（松开触发键或点击停止）", "Infinite loop (release trigger or click Stop)" },
            { "触发时屏蔽最后一个触发键/鼠标事件", "Suppress final trigger key/mouse event" },
            { "扫描码输出（推荐游戏）", "Scan-code output (game-friendly)" },
            { "UI执行时自动切换到目标窗口（可选）", "Auto-switch to target on UI run" },
            { "切换后延迟：", "Post-switch delay:" },
            { "窗口置顶", "Always on top" },
            { "目标窗口：", "Target window:" },
            { "锁定刚才的目标窗口", "Lock Recent Target" },
            { "清除目标", "Clear Target" },
            { "按前台程序自动切换已绑定的配置方案", "Auto-switch bound profile by foreground app" },
            { "保存方案时可绑定当前目标进程", "A profile can be bound to the current target process when saved" },
            { "执行步骤", "Macro Steps" },
            { "● 录制宏", "● Record Macro" },
            { "■ 停止录制", "■ Stop Recording" },
            { "操作", "Action" },
            { "按键 / 鼠标", "Key / Mouse" },
            { "按住时长 (ms)", "Hold time (ms)" },
            { "步骤后间隔 (ms)", "Post-step delay (ms)" },
            { "添加", "Add" },
            { "编辑", "Edit" },
            { "复制步骤", "Copy Steps" },
            { "上移", "Move Up" },
            { "下移", "Move Down" },
            { "批量间隔", "Batch Delay" },
            { "▶ 执行所选宏", "▶ Run Selected Macro" },
            { "■ 停止当前宏", "■ Stop Current Macro" },
            { "■ 正在停止…", "■ Stopping…" },
            { "状态：空闲", "Status: Idle" },
            { "宏包用于追加/分享宏；配置方案用于保存或切换整套设置。", "Macro packages append/share macros; profiles save or switch complete setups." },

            { "导入一个或多个宏包，把其中的宏追加到当前列表末尾，不覆盖已有宏。", "Import one or more macro packages and append their macros without overwriting existing macros." },
            { "选择一个或多个宏导出为宏包，适合分享、备份或合并到其他列表。", "Export one or more macros as a package for sharing, backup, or merging into another list." },
            { "载入一套完整配置方案。方案会替换当前宏列表和相关设置，载入前自动备份。", "Load a complete profile. It replaces the current macro list and related settings after making a backup." },
            { "把当前宏列表和程序设置保存为一套方案；可选择绑定目标进程以供自动切换。", "Save the current macro list and app settings as a profile, optionally bound to a target process for auto switching." },
            { "打开本地配置目录，里面包含宏包、配置方案、备份和日志。", "Open the local configuration folder containing macro packages, profiles, backups, and logs." },
            { "打开安全、诊断、托盘和关于选项。", "Open safety, diagnostics, tray, language, and About options." },
            { "将当前宏在列表中上移一位。", "Move the selected macro up one position." },
            { "将当前宏在列表中下移一位。", "Move the selected macro down one position." },
            { "控制这个宏是否响应全局触发键。关闭后仍可用 UI 按钮执行。", "Controls whether this macro responds to its global trigger. It can still be run from the UI when disabled." },
            { "InputStitch 在前台时，进入名称、触发键、延迟、步骤、复制或删除等编辑区域会暂停宏；离开后继续。", "When InputStitch is foreground, entering editing or destructive UI areas pauses macro output and resumes it after you leave." },
            { "给宏添加简短用途说明。备注会随宏包一起导出。", "Add a short description. Notes are included when the macro is exported." },
            { "持续重复整个宏，直到再次触发、松开按住型触发键、点击停止或使用紧急停止键。", "Repeat the entire macro until triggered again, a hold trigger is released, Stop is clicked, or Emergency Stop is used." },
            { "用键盘扫描码发送按键，通常更适合游戏；鼠标操作不受影响。", "Send keyboard input by scan code, which is usually more game-friendly. Mouse input is unchanged." },
            { "宏完整执行的次数。勾选无限循环后忽略此数值。", "Number of complete macro repetitions. Ignored when infinite looping is enabled." },
            { "选择按一次切换启动/停止，或按住触发键运行、松开立即停止。", "Choose toggle mode, or run only while the trigger is held and stop immediately on release." },
            { "点击后按下希望使用的键盘键、鼠标按钮或滚轮方向来设置触发方式。", "Click, then press the keyboard key, mouse button, or wheel direction to use as the trigger." },
            { "触发宏时阻止最后一个实际按键或鼠标事件继续传给当前程序；其他输入不受影响。", "Prevent the final physical trigger key/mouse event from reaching the current app. Other input is unaffected." },
            { "开启后，从 UI 点击执行会先切换到已锁定的目标窗口；关闭后直接执行，不主动切换窗口。", "When enabled, running from the UI first activates the locked target window. When disabled, the macro runs without switching windows." },
            { "自动切换到目标窗口后，等待多少毫秒再开始发送宏输入。", "How many milliseconds to wait after activating the target window before sending macro input." },
            { "让 InputStitch 窗口保持在其他普通窗口上方，不改变宏输入目标。", "Keep the InputStitch window above normal windows without changing the macro input target." },
            { "当前锁定的目标窗口。用于 UI 自动切换和保存方案时的进程绑定。", "The currently locked target window, used for UI auto-switching and optional profile process binding." },
            { "先切到目标程序，再切回 InputStitch 后点击这里；程序会锁定最近有效的外部前台窗口。", "Switch to the target app, return to InputStitch, then click here to lock the most recent valid external foreground window." },
            { "清除当前目标窗口，不删除任何宏。", "Clear the target window without deleting any macros." },
            { "开启后，当前台程序与某个已绑定方案的进程匹配时，自动载入该方案。紧急停止键等安全设置不会随方案切换。", "Automatically load a bound profile when its process becomes foreground. Safety settings such as Emergency Stop are preserved." },
            { "录制物理键盘、鼠标按钮和滚轮输入，自动换算为宏步骤；不记录鼠标移动。", "Record physical keyboard, mouse button, and wheel input and convert it into macro steps. Mouse movement is not recorded." },
            { "复制当前选中的一个或多个步骤，并插入到所选步骤之后。", "Copy the selected step(s) and insert the copies after the selection." },
            { "批量设置所选步骤的固定间隔或简单随机间隔范围。", "Set a fixed or simple random delay range for all selected steps." },
            { "开始执行当前宏；有宏正在运行时用于停止当前宏。", "Run the selected macro; while a macro is running, this button stops it." },

            { "添加宏步骤", "Add Macro Step" },
            { "编辑宏步骤", "Edit Macro Step" },
            { "操作：", "Action:" },
            { "按一下（按下→等待→松开）", "Press (Down → Wait → Up)" },
            { "按一下", "Press" },
            { "按住 / KeyDown", "Hold / KeyDown" },
            { "松开 / KeyUp", "Release / KeyUp" },
            { "按键/鼠标：", "Key/Mouse:" },
            { "捕获输入", "Capture Input" },
            { "按住时长：", "Hold time:" },
            { "随机步骤间隔", "Random post-step delay" },
            { "步骤后间隔：", "Post-step delay:" },
            { "随机范围：", "Random range:" },
            { "确定", "OK" },
            { "取消", "Cancel" },
            { "请按键…", "Press a key…" },
            { "选择要导出的宏", "Select Macros to Export" },
            { "勾选一个或多个宏。导出的宏包只包含宏本身，不会改变程序全局设置。", "Select one or more macros. The exported package contains only macros and does not change global app settings." },
            { "（空宏）", "(Empty macro)" },
            { "全选", "Select All" },
            { "全不选", "Select None" },
            { "导出", "Export" },
            { "批量设置步骤间隔", "Batch Step Delay" },
            { "固定间隔", "Fixed delay" },
            { "随机间隔", "Random delay" },
            { "仅修改当前选中的步骤，不改变按住时长。", "Only changes the selected steps; hold times are not modified." },
            { " - 诊断信息", " - Diagnostics" },
            { "关闭", "Close" },
            { "关于 ", "About " },
            { "轻量级 Windows 可视化键鼠宏工具\r\n专注精确时序、游戏场景下的可靠控制与安全停止。\r\n\r\n支持键盘、鼠标侧键、滚轮、扫描码、按住运行、宏录制、宏包与配置方案。\r\n\r\n配置目录：\r\n", "Lightweight visual keyboard and mouse macro tool for Windows.\r\nFocused on precise timing, reliable game-friendly control, and safe stopping.\r\n\r\nSupports keyboard input, mouse side buttons, wheel input, scan codes, hold-to-run, macro recording, macro packages, and profiles.\r\n\r\nConfig folder:\r\n" },
            { "打开配置目录", "Open Config Folder" },

            { "暂停全局宏触发", "Pause Global Macro Triggers" },
            { "最小化到系统托盘", "Minimize to System Tray" },
            { "诊断信息...", "Diagnostics..." },
            { "打开日志文件夹", "Open Log Folder" },
            { "显示 ", "Show " },
            { "紧急停止", "Emergency Stop" },
            { "退出", "Exit" },
            { "语言 / Language", "Language / 语言" },
            { "简体中文", "Simplified Chinese" },
            { "English", "English" },
            { "设置紧急停止键...（当前：", "Set Emergency Stop hotkey... (current: " },
            { "请按新的紧急停止键…", "Press the new Emergency Stop hotkey…" },

            { "鼠标左键", "Mouse Left" },
            { "鼠标右键", "Mouse Right" },
            { "鼠标中键", "Mouse Middle" },
            { "鼠标侧键1 (X1)", "Mouse Side Button 1 (X1)" },
            { "鼠标侧键2 (X2)", "Mouse Side Button 2 (X2)" },
            { "滚轮向上", "Wheel Up" },
            { "滚轮向下", "Wheel Down" },
            { "未设置", "Not set" },
            { "未安装", "Not installed" },
            { "已安装", "Installed" },
            { "开启", "On" },
            { "是", "Yes" },
            { "否", "No" },
            { "未知", "Unknown" },
            { "（当前 config.xml）", "(current config.xml)" },
            { "欢迎使用 ", "Welcome to " },
            { "默认紧急停止键：", "Default Emergency Stop hotkey: " },
            { "宏录制只记录键盘、鼠标按钮和滚轮，不记录鼠标移动。", "Macro recording captures keyboard, mouse buttons, and wheel input, but not mouse movement." },
            { "请按触发键…", "Press trigger…" },
            { "正在执行", "Running" },
            { "正在停止", "Stopping" },
            { "已暂停", "Paused" },
            { "等待", "waiting" },
            { "空闲", "Idle" },
            { "已导入", "Imported" },
            { "已导出", "Exported" },
            { "已保存", "Saved" },
            { "已载入", "Loaded" },
            { "已打开配置文件夹", "Config folder opened" },
            { "重复的已启用触发键", "duplicate enabled triggers" },
            { "出错", "error" },
            { "无法", "unable" },
            { "失败", "failed" },
            { "录制", "recording" },
            { "提示", "notice" },
            { "开始执行", "starting" },
            { "无", "None" },
            { "未锁定", "Not locked" },
            { "未知进程", "Unknown process" },
            { "（无窗口标题）", "(No window title)" },
            { "已锁定（窗口信息读取失败）", "Locked (window information unavailable)" },
            { "未找到：", "Not found: " },
            { "○ （空宏）", "○ (Empty macro)" },

            { "绑定方案到目标进程", "Bind Profile to Target Process" },
            { "载入配置方案", "Load Profile" },
            { "录制宏", "Record Macro" },
            { "请先选择一个按键或鼠标输入。", "Please select a keyboard or mouse input first." },
            { "滚轮只支持“按一下”动作，因为滚轮没有持续按下/松开的状态。", "Mouse wheel input only supports Press because a wheel direction has no persistent down/up state." },
            { "随机间隔的最小值不能大于最大值。", "The minimum random delay cannot be greater than the maximum." },
            { "请至少选择一个宏。", "Please select at least one macro." },
            { "当前没有可导出的宏。", "There are no macros to export." },
            { "所选文件中没有可导入的宏。", "The selected file(s) contain no importable macros." },
            { "请先停止正在执行的宏。", "Please stop the running macro first." },
            { "这个宏还没有任何执行步骤。", "This macro has no steps yet." },
            { "当前宏未能及时停止。为避免配置与执行线程状态不一致，本次操作已取消。", "The current macro did not stop in time. This operation was cancelled to avoid configuration and worker state inconsistency." },
            { "提示：按住运行模式仅支持不含 Ctrl/Shift/Alt/Win 的单个键盘键或鼠标按钮；请重新录制触发键。", "Hold-to-run supports only a single keyboard key or mouse button without Ctrl/Shift/Alt/Win. Please capture the trigger again." },
            { "提示：按住运行模式不支持修饰键组合或滚轮，请改用单个键盘键/鼠标按钮。", "Hold-to-run does not support modifier combinations or wheel triggers. Use a single keyboard key or mouse button." },
            { "无法启动：按住运行模式仅支持不含 Ctrl/Shift/Alt/Win 的单个键盘键或鼠标按钮。", "Cannot start: hold-to-run supports only a single keyboard key or mouse button without Ctrl/Shift/Alt/Win." },
            { "提示：单步编辑一次只能选择一个步骤；批量修改间隔请使用“批量间隔”。", "Single-step editing requires exactly one selected step. Use Batch Delay to modify multiple delays." },

            { "载入方案会停止当前宏，并用所选方案替换当前宏列表和大部分程序设置。\r\n\r\n紧急停止键、自动方案切换和托盘设置保持不变；当前配置会先自动备份。是否继续？", "Loading a profile stops the current macro and replaces the current macro list and most app settings.\r\n\r\nEmergency Stop, automatic profile switching, and tray settings are preserved. The current configuration is backed up first. Continue?" },
            { "当前宏已经有执行步骤。\r\n\r\n选择“是”：把录制结果追加到现有步骤末尾。\r\n选择“否”：用录制结果替换现有步骤。\r\n选择“取消”：不开始录制。", "This macro already has steps.\r\n\r\nYes: append the recording to the existing steps.\r\nNo: replace the existing steps with the recording.\r\nCancel: do not start recording." },
            { "导出 InputStitch 宏包", "Export InputStitch Macro Package" },
            { "导入 InputStitch 宏", "Import InputStitch Macros" },
            { "保存 InputStitch 配置方案", "Save InputStitch Profile" },
            { "载入 InputStitch 配置方案", "Load InputStitch Profile" },
            { "InputStitch 宏包 (*.mpmacro)|*.mpmacro|所有文件 (*.*)|*.*", "InputStitch Macro Package (*.mpmacro)|*.mpmacro|All Files (*.*)|*.*" },
            { "InputStitch 宏包 (*.mpmacro)|*.mpmacro|兼容配置 (*.xml;*.mpprofile)|*.xml;*.mpprofile|所有文件 (*.*)|*.*", "InputStitch Macro Package (*.mpmacro)|*.mpmacro|Compatible Config (*.xml;*.mpprofile)|*.xml;*.mpprofile|All Files (*.*)|*.*" },
            { "InputStitch 配置方案 (*.mpprofile)|*.mpprofile|所有文件 (*.*)|*.*", "InputStitch Profile (*.mpprofile)|*.mpprofile|All Files (*.*)|*.*" },
            { "InputStitch 配置方案 (*.mpprofile)|*.mpprofile|旧版 XML 配置 (*.xml)|*.xml|所有文件 (*.*)|*.*", "InputStitch Profile (*.mpprofile)|*.mpprofile|Legacy XML Config (*.xml)|*.xml|All Files (*.*)|*.*" },

            { "宏包为空。", "The macro package is empty." },
            { "配置方案为空。", "The profile is empty." },
            { "配置方案不存在。", "The profile does not exist." },
            { "配置方案过大，已拒绝载入。", "The profile is too large and was not loaded." },
            { "文件不存在。", "The file does not exist." },
            { "文件过大，已拒绝导入。", "The file is too large and was not imported." },
            { "不是有效的 InputStitch 宏包、配置方案或兼容旧配置。", "This is not a valid InputStitch macro package, profile, or compatible legacy configuration." },
            { "配置为空。", "The configuration is empty." },
            { "配置文件不存在。", "The configuration file does not exist." },
            { "配置文件过大，已拒绝导入。", "The configuration file is too large and was not imported." },
            { "不是有效的 InputStitch 配置文件。", "This is not a valid InputStitch configuration file." },
            { "新宏", "New Macro" },
            { " - 副本", " - Copy" },
            { "未命名宏", "Untitled Macro" },
            { " (导入)", " (Imported)" },
            { "示例：快速按 E", "Example: Quick E" },
            { "录制中：切到目标程序进行操作；回到 InputStitch 后点击“停止录制”。", "Recording: switch to the target app and perform the actions; return to InputStitch and click Stop Recording." },

            { "保存配置方案", "Save Profile" },
            { "打开工具菜单", "Open Tools Menu" },
            { "选择宏", "Select Macro" },
            { "新增宏", "Add Macro" },
            { "复制宏", "Copy Macro" },
            { "删除宏", "Delete Macro" },
            { "调整宏列表顺序", "Reorder Macro List" },
            { "编辑名称", "Edit Name" },
            { "编辑备注", "Edit Notes" },
            { "修改全局触发设置", "Edit Global Trigger Setting" },
            { "触发键设置", "Edit Trigger" },
            { "修改触发方式", "Edit Trigger Mode" },
            { "修改触发屏蔽设置", "Edit Trigger Suppression" },
            { "修改执行次数", "Edit Repeat Count" },
            { "修改循环设置", "Edit Loop Setting" },
            { "修改输出方式", "Edit Output Mode" },
            { "修改目标窗口设置", "Edit Target Switching" },
            { "修改切换后延迟", "Edit Post-switch Delay" },
            { "修改窗口置顶设置", "Edit Always-on-top Setting" },
            { "修改 UI 编辑保护", "Edit UI Protection" },
            { "锁定目标窗口", "Lock Target Window" },
            { "清除目标窗口", "Clear Target Window" },
            { "修改自动方案切换", "Edit Auto Profile Switching" },
            { "复制宏步骤", "Copy Macro Steps" },
            { "删除宏步骤", "Delete Macro Steps" },
            { "调整宏步骤顺序", "Reorder Macro Steps" },
            { "批量修改步骤间隔", "Batch Edit Step Delays" },
            { "编辑控件", "Edit Control" },
            { "托盘紧急停止", "Tray Emergency Stop" },
            { "录制紧急停止键", "Capture Emergency Stop Hotkey" },
            { "录制宏步骤按键", "Capture Macro Step Input" },
            { "全局紧急停止键", "Global Emergency Stop Hotkey" },
            { "提示：紧急停止键不建议使用滚轮，请选择键盘键或鼠标按钮。", "Emergency Stop should not use the mouse wheel. Choose a keyboard key or mouse button." },
            { "配置方案", "Profile" },
            { "宏包", "Macro package" },
            { "配置文件", "Configuration file" },
            { "键盘输入", "Keyboard input" },
            { "鼠标滚轮输入", "Mouse wheel input" },
            { "鼠标按钮输入", "Mouse button input" },
            { "无法安装全局键盘/鼠标钩子。请尝试以管理员身份运行。\n", "Unable to install global keyboard/mouse hooks. Try running as administrator.\n" }
        };

        private static readonly KeyValuePair<string, string>[] DynamicPairs = new KeyValuePair<string, string>[]
        {
            new KeyValuePair<string,string>(" - 诊断信息", " - Diagnostics"),
            new KeyValuePair<string,string>("关于 ", "About "),
            new KeyValuePair<string,string>("无法打开配置文件夹：\r\n", "Unable to open config folder:\r\n"),
            new KeyValuePair<string,string>("导出宏失败：\r\n", "Failed to export macro package:\r\n"),
            new KeyValuePair<string,string>("导入宏失败，当前宏列表未改变：\r\n", "Macro import failed; the current macro list was not changed:\r\n"),
            new KeyValuePair<string,string>("保存配置方案失败：\r\n", "Failed to save profile:\r\n"),
            new KeyValuePair<string,string>("载入配置方案失败，当前配置未被替换：\r\n", "Failed to load profile; the current configuration was not replaced:\r\n"),
            new KeyValuePair<string,string>("是否将此方案绑定到当前目标进程：\r\n", "Bind this profile to the current target process:\r\n"),
            new KeyValuePair<string,string>("\r\n\r\n选择“是”后，开启“按前台程序自动切换方案”时可自动载入。\r\n选择“否”则保存为不自动绑定的普通方案。", "\r\n\r\nYes: the profile can be auto-loaded when foreground-app profile switching is enabled.\r\nNo: save it as a normal unbound profile."),
            new KeyValuePair<string,string>("确定删除宏“", "Delete macro “"),
            new KeyValuePair<string,string>("”吗？", "”?"),
            new KeyValuePair<string,string>("InputStitch 遇到未处理的界面错误。\r\n\r\n", "InputStitch encountered an unhandled UI error.\r\n\r\n"),
            new KeyValuePair<string,string>("\r\n\r\n日志：", "\r\n\r\nLog: "),
            new KeyValuePair<string,string>("配置文件无法读取，已使用默认配置。", "The configuration file could not be read; defaults were loaded."),
            new KeyValuePair<string,string>("\r\n原文件已备份到：\r\n", "\r\nThe original file was backed up to:\r\n"),
            new KeyValuePair<string,string>("\r\n\r\n详细错误已写入日志。", "\r\n\r\nDetailed error information was written to the log."),
            new KeyValuePair<string,string>("状态：正在停止当前宏…", "Status: stopping current macro…"),
            new KeyValuePair<string,string>("状态：全局宏触发已暂停；紧急停止键仍有效。", "Status: global macro triggers paused; Emergency Stop remains active."),
            new KeyValuePair<string,string>("状态：全局宏触发已恢复。", "Status: global macro triggers resumed."),
            new KeyValuePair<string,string>("状态：正在录制紧急停止键（按 Esc 取消）", "Status: capturing Emergency Stop hotkey (Esc to cancel)"),
            new KeyValuePair<string,string>("状态：正在录制触发键（按 Esc 取消）", "Status: capturing macro trigger (Esc to cancel)"),
            new KeyValuePair<string,string>("状态：请按下要使用的键/鼠标按钮/滚轮（Esc 取消）", "Status: press the key, mouse button, or wheel direction to use (Esc to cancel)"),
            new KeyValuePair<string,string>("紧急停止：已发送停止信号并释放宏按住的输入。", "Emergency Stop: stop requested and macro-held inputs released."),
            new KeyValuePair<string,string>("紧急停止：当前没有正在执行的宏。", "Emergency Stop: no macro is currently running."),
            new KeyValuePair<string,string>("状态：已打开配置文件夹。", "Status: config folder opened."),
            new KeyValuePair<string,string>("状态：打开配置文件夹失败。", "Status: failed to open config folder."),
            new KeyValuePair<string,string>("状态：未发现已启用的触发键冲突。", "Status: no enabled trigger conflicts found."),
            new KeyValuePair<string,string>("状态：自动切换配置方案失败。", "Status: automatic profile switch failed."),
            new KeyValuePair<string,string>("状态：已调整宏列表顺序。", "Status: macro order updated."),
            new KeyValuePair<string,string>("状态：已取消宏录制。", "Status: macro recording cancelled."),
            new KeyValuePair<string,string>("状态：录制目标宏已不存在，录制结果未保存。", "Status: recording target no longer exists; result was not saved."),
            new KeyValuePair<string,string>("状态：录制结束，没有记录到目标程序中的键鼠操作。", "Status: recording ended with no keyboard/mouse input captured from the target app."),
            new KeyValuePair<string,string>("状态：开始执行（不切换窗口）。", "Status: starting macro without switching windows."),
            new KeyValuePair<string,string>("状态：上一宏仍在停止中。为避免重复执行，已取消本次启动；请稍后再试。", "Status: previous macro is still stopping. Start cancelled to prevent overlapping workers; try again shortly."),
            new KeyValuePair<string,string>("状态：已有宏正在运行，未启动新的宏。", "Status: another macro is already running; no new macro was started."),
            new KeyValuePair<string,string>("状态：空闲", "Status: Idle"),
            new KeyValuePair<string,string>("正在执行：", "Running: "),
            new KeyValuePair<string,string>("准备执行：", "Preparing: "),
            new KeyValuePair<string,string>("已暂停：", "Paused: "),
            new KeyValuePair<string,string>("执行出错：", "Execution error: "),
            new KeyValuePair<string,string>("提示：", "Notice: "),
            new KeyValuePair<string,string>("状态：", "Status: "),
            new KeyValuePair<string,string>("紧急停止：", "Emergency Stop: "),
            new KeyValuePair<string,string>("（UI编辑保护：", " (UI edit protection: "),
            new KeyValuePair<string,string>("（UI编辑保护已恢复）", " (UI edit protection resumed)"),
            new KeyValuePair<string,string>("（等待手动修饰键松开）", " (waiting for physical modifier release)"),
            new KeyValuePair<string,string>("（等待避免特殊快捷键冲突）", " (waiting to avoid a shortcut conflict)"),
            new KeyValuePair<string,string>("（等待触发键松开）", " (waiting for trigger release)"),
            new KeyValuePair<string,string>("（等待目标窗口稳定 ", " (waiting for target window "),
            new KeyValuePair<string,string>(" 次，无限循环）", " / infinite)"),
            new KeyValuePair<string,string>("（第 ", " (iteration "),
            new KeyValuePair<string,string>(" 毫秒）", " ms)"),
            new KeyValuePair<string,string>(" 个宏，已追加到列表末尾。", " macro(s), appended to the end of the list."),
            new KeyValuePair<string,string>(" 个宏）", " macro(s))"),
            new KeyValuePair<string,string>(" 个步骤。", " step(s)."),
            new KeyValuePair<string,string>("已导入 ", "Imported "),
            new KeyValuePair<string,string>("已导出宏包：", "Exported macro package: "),
            new KeyValuePair<string,string>("已保存配置方案：", "Saved profile: "),
            new KeyValuePair<string,string>("已保存并绑定方案：", "Saved and bound profile: "),
            new KeyValuePair<string,string>("已载入配置方案：", "Loaded profile: "),
            new KeyValuePair<string,string>("已按前台程序自动切换方案：", "Auto-switched profile for foreground app: "),
            new KeyValuePair<string,string>("未找到：", "Not found: "),
            new KeyValuePair<string,string>("已锁定目标窗口：", "Locked target window: "),
            new KeyValuePair<string,string>("已清除目标窗口。", "Target window cleared."),
            new KeyValuePair<string,string>("录制完成，已生成 ", "Recording complete; generated "),
            new KeyValuePair<string,string>("请先切到目标程序，再切回 InputStitch。", "Switch to the target app first, then return to InputStitch."),
            new KeyValuePair<string,string>("当前没有正在执行的宏。", "No macro is currently running."),
            new KeyValuePair<string,string>("轻量级 Windows 可视化键鼠宏工具\r\n专注精确时序、游戏场景下的可靠控制与安全停止。\r\n\r\n支持键盘、鼠标侧键、滚轮、扫描码、按住运行、宏录制、宏包与配置方案。\r\n\r\n配置目录：\r\n", "Lightweight visual keyboard and mouse macro tool for Windows.\r\nFocused on precise timing, reliable game-friendly control, and safe stopping.\r\n\r\nSupports keyboard input, mouse side buttons, wheel input, scan codes, hold-to-run, macro recording, macro packages, and profiles.\r\n\r\nConfig folder:\r\n"),
            new KeyValuePair<string,string>("状态：已导入 ", "Status: imported "),
            new KeyValuePair<string,string>(" 个宏；发现 ", " macro(s); found "),
            new KeyValuePair<string,string>(" 组重复的已启用触发键，请检查。", " duplicate enabled trigger group(s); please review them."),
            new KeyValuePair<string,string>("提示：触发键冲突：", "Notice: trigger conflict: "),
            new KeyValuePair<string,string>("状态：已导出宏包：", "Status: exported macro package: "),
            new KeyValuePair<string,string>("状态：导出宏失败。", "Status: macro export failed."),
            new KeyValuePair<string,string>("状态：导入宏失败。", "Status: macro import failed."),
            new KeyValuePair<string,string>("状态：已保存配置方案：", "Status: saved profile: "),
            new KeyValuePair<string,string>("状态：已保存并绑定方案：", "Status: saved and bound profile: "),
            new KeyValuePair<string,string>("状态：保存配置方案失败。", "Status: failed to save profile."),
            new KeyValuePair<string,string>("状态：已载入配置方案：", "Status: loaded profile: "),
            new KeyValuePair<string,string>("状态：载入配置方案失败。", "Status: failed to load profile."),
            new KeyValuePair<string,string>("状态：操作失败：当前宏仍在停止中。", "Status: operation failed because the current macro is still stopping."),
            new KeyValuePair<string,string>("状态：已按前台程序自动切换方案：", "Status: auto-switched profile for foreground app: "),
            new KeyValuePair<string,string>("状态：没有可锁定的最近外部前台窗口。请先切到目标程序，再切回 InputStitch。", "Status: no recent external foreground window is available. Switch to the target app, then return to InputStitch."),
            new KeyValuePair<string,string>("状态：无法读取目标窗口信息。", "Status: unable to read target window information."),
            new KeyValuePair<string,string>("状态：已跳过临时/已消失的切换窗口并锁定：", "Status: skipped a temporary/disappeared switcher window and locked: "),
            new KeyValuePair<string,string>("状态：已锁定目标窗口：", "Status: locked target window: "),
            new KeyValuePair<string,string>("状态：已清除目标窗口。", "Status: target window cleared."),
            new KeyValuePair<string,string>("状态：已保存的目标窗口当前未找到。请启动目标程序，或重新锁定目标窗口。", "Status: the saved target window is not currently available. Start the target app or lock the target again."),
            new KeyValuePair<string,string>("状态：尚未锁定目标窗口。请先切到目标程序，再切回 InputStitch，点击“锁定刚才的目标窗口”。", "Status: no target window is locked. Switch to the target app, return to InputStitch, and click Lock Recent Target Window."),
            new KeyValuePair<string,string>("状态：无法激活已锁定的目标窗口。请手动切到目标程序后用热键触发，或重新锁定目标。", "Status: unable to activate the locked target window. Switch to the target app manually and use a hotkey, or lock the target again."),
            new KeyValuePair<string,string>("状态：录制完成，已生成 ", "Status: recording complete; generated "),
            new KeyValuePair<string,string>("发送失败（SendInput 返回 ", "Send failed (SendInput returned "),
            new KeyValuePair<string,string>("）。如果目标程序权限更高，请以管理员身份运行 InputStitch。", "). If the target app runs at a higher integrity level, run InputStitch as administrator."),
            new KeyValuePair<string,string>("由更高版本的 InputStitch 创建（格式版本 ", " was created by a newer InputStitch version (format version "),
            new KeyValuePair<string,string>("），当前版本无法安全读取。请先更新程序。", "). This version cannot read it safely; update InputStitch first."),
            new KeyValuePair<string,string>("失败", "failed"),
            new KeyValuePair<string,string>("无法", "Unable to ")
        };

        public static string Language { get { return currentLanguage; } }
        public static bool IsEnglish { get { return string.Equals(currentLanguage, English, StringComparison.OrdinalIgnoreCase); } }

        public static void SetLanguage(string language)
        {
            currentLanguage = string.Equals(language, English, StringComparison.OrdinalIgnoreCase) ? English : Chinese;
        }

        public static string T(string source)
        {
            if (!IsEnglish || string.IsNullOrEmpty(source)) return source;
            string value;
            return En.TryGetValue(source, out value) ? value : source;
        }

        public static string Dynamic(string source)
        {
            if (!IsEnglish || string.IsNullOrEmpty(source)) return source;
            string exact;
            if (En.TryGetValue(source, out exact)) return exact;
            string value = source;
            for (int i = 0; i < DynamicPairs.Length; i++)
                value = value.Replace(DynamicPairs[i].Key, DynamicPairs[i].Value);
            return value;
        }

        public static bool ContainsMeaning(string text, string chineseKey)
        {
            if (string.IsNullOrEmpty(text)) return false;
            if (text.IndexOf(chineseKey, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            string translated = T(chineseKey);
            return !string.Equals(translated, chineseKey, StringComparison.Ordinal) && text.IndexOf(translated, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static void ApplyStaticControls(Control root)
        {
            if (root == null) return;
            root.Text = Dynamic(root.Text);
            foreach (Control child in root.Controls) ApplyStaticControls(child);
        }
    }

    public static class LocalizedMessageBox
    {
        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return MessageBox.Show(owner, Localizer.Dynamic(text), Localizer.Dynamic(caption), buttons, icon);
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return MessageBox.Show(Localizer.Dynamic(text), Localizer.Dynamic(caption), buttons, icon);
        }
    }

    public static class AppPaths
    {
        public static readonly string Root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppInfo.ProductName);
        public static readonly string Config = Path.Combine(Root, "config.xml");
        public static readonly string MacroPackages = Path.Combine(Root, "macro-packages");
        public static readonly string Profiles = Path.Combine(Root, "profiles");
        public static readonly string Backups = Path.Combine(Root, "backups");
        public static readonly string Logs = Path.Combine(Root, "logs");

        public static void EnsureDirectories()
        {
            try
            {
                Directory.CreateDirectory(Root);
                Directory.CreateDirectory(MacroPackages);
                Directory.CreateDirectory(Profiles);
                Directory.CreateDirectory(Backups);
                Directory.CreateDirectory(Logs);

            }
            catch (Exception ex)
            {
                AppLog.Write("Application data directory initialization failed", ex);
            }
        }
    }

    public static class AppLog
    {
        private static readonly object Sync = new object();
        public static string LogPath
        {
            get { return Path.Combine(AppPaths.Logs, "InputStitch.log"); }
        }

        public static void Write(string message)
        {
            Write(message, null);
        }

        public static void Write(string message, Exception ex)
        {
            try
            {
                Directory.CreateDirectory(AppPaths.Logs);
                StringBuilder line = new StringBuilder();
                line.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                line.Append("  ");
                line.Append(message ?? "");
                if (ex != null)
                {
                    line.Append(Environment.NewLine);
                    line.Append(ex.ToString());
                }
                line.Append(Environment.NewLine);
                lock (Sync) File.AppendAllText(LogPath, line.ToString(), Encoding.UTF8);
            }
            catch { }
        }
    }

    [Serializable]
    [XmlRoot("InputStitchUpdate")]
    public class UpdateManifest
    {
        public string Version = "";
        public string ReleaseUrl = "";
        [XmlElement("Asset")]
        public List<UpdateAsset> Assets = new List<UpdateAsset>();
    }

    [Serializable]
    public class UpdateAsset
    {
        [XmlAttribute]
        public string Architecture = "";
        [XmlAttribute]
        public string FileName = "";
        [XmlAttribute]
        public string Url = "";
        [XmlAttribute]
        public string Sha256 = "";
    }

    [Serializable]
    public class PendingUpdate
    {
        public string Token = "";
        public string SourcePath = "";
        public string TargetPath = "";
        public string Sha256 = "";
        public string CreatedUtc = "";
    }

    public sealed class UpdateCheckResult
    {
        public UpdateManifest Manifest;
        public UpdateAsset Asset;
        public bool IsAvailable;
        public bool IsSameVersionReplacement;
        public string CurrentSha256 = "";
    }

    public static class UpdateManager
    {
        private static readonly string UpdatesDirectory = Path.Combine(AppPaths.Root, "updates");
        private static readonly string PendingPath = Path.Combine(UpdatesDirectory, "pending-update.xml");

        public static async Task<UpdateCheckResult> CheckAsync()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            string xml;
            using (WebClient client = CreateClient())
            {
                Uri uri = new Uri(AppInfo.UpdateManifestUrl + "?cache=" + DateTime.UtcNow.Ticks.ToString());
                xml = await client.DownloadStringTaskAsync(uri);
            }

            UpdateManifest manifest;
            XmlSerializer serializer = new XmlSerializer(typeof(UpdateManifest));
            using (StringReader reader = new StringReader(xml))
                manifest = serializer.Deserialize(reader) as UpdateManifest;
            if (manifest == null) throw new InvalidDataException("The update manifest is invalid.");

            Version remoteVersion;
            Version currentVersion;
            if (!System.Version.TryParse(manifest.Version, out remoteVersion) ||
                !System.Version.TryParse(AppInfo.Version, out currentVersion))
                throw new InvalidDataException("The update manifest contains an invalid version.");

            string architecture = Environment.Is64BitProcess ? "x64" : "x86";
            UpdateAsset asset = null;
            if (manifest.Assets != null)
            {
                foreach (UpdateAsset candidate in manifest.Assets)
                {
                    if (candidate != null && string.Equals(candidate.Architecture, architecture, StringComparison.OrdinalIgnoreCase))
                    {
                        asset = candidate;
                        break;
                    }
                }
            }
            ValidateAsset(asset, architecture);

            string currentHash = ComputeSha256(Application.ExecutablePath);
            bool newerVersion = remoteVersion > currentVersion;
            bool sameVersionReplacement = remoteVersion == currentVersion &&
                !string.Equals(currentHash, asset.Sha256, StringComparison.OrdinalIgnoreCase);
            UpdateCheckResult result = new UpdateCheckResult();
            result.Manifest = manifest;
            result.Asset = asset;
            result.IsAvailable = newerVersion || sameVersionReplacement;
            result.IsSameVersionReplacement = sameVersionReplacement;
            result.CurrentSha256 = currentHash;
            return result;
        }

        public static async Task<string> DownloadAsync(UpdateCheckResult update)
        {
            if (update == null || update.Asset == null || !update.IsAvailable)
                throw new InvalidOperationException("No update is available.");
            ValidateAsset(update.Asset, Environment.Is64BitProcess ? "x64" : "x86");
            Directory.CreateDirectory(UpdatesDirectory);
            string destination = Path.Combine(UpdatesDirectory, Guid.NewGuid().ToString("N") + ".exe");
            try
            {
                using (WebClient client = CreateClient())
                    await client.DownloadFileTaskAsync(new Uri(update.Asset.Url), destination);
                string hash = ComputeSha256(destination);
                if (!string.Equals(hash, update.Asset.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The downloaded update failed SHA-256 verification.");
                FileVersionInfo version = FileVersionInfo.GetVersionInfo(destination);
                if (!string.Equals(version.ProductName, AppInfo.ProductName, StringComparison.Ordinal) ||
                    !string.Equals(version.ProductVersion, update.Manifest.Version, StringComparison.Ordinal))
                    throw new InvalidDataException("The downloaded file is not the expected InputStitch build.");
                return destination;
            }
            catch
            {
                try { if (File.Exists(destination)) File.Delete(destination); } catch { }
                throw;
            }
        }

        public static void BeginInstall(string downloadedPath, string expectedSha256)
        {
            if (string.IsNullOrWhiteSpace(downloadedPath) || !File.Exists(downloadedPath))
                throw new FileNotFoundException("The downloaded update was not found.", downloadedPath);
            string source = Path.GetFullPath(downloadedPath);
            string target = Path.GetFullPath(Application.ExecutablePath);
            if (!IsUnderDirectory(source, UpdatesDirectory)) throw new InvalidDataException("Invalid update source path.");
            if (!string.Equals(ComputeSha256(source), expectedSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The update hash changed before installation.");

            byte[] tokenBytes = new byte[32];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create()) random.GetBytes(tokenBytes);
            string token = BitConverter.ToString(tokenBytes).Replace("-", "").ToLowerInvariant();
            PendingUpdate pending = new PendingUpdate();
            pending.Token = token;
            pending.SourcePath = source;
            pending.TargetPath = target;
            pending.Sha256 = expectedSha256.ToLowerInvariant();
            pending.CreatedUtc = DateTime.UtcNow.ToString("o");
            Directory.CreateDirectory(UpdatesDirectory);
            XmlSerializer serializer = new XmlSerializer(typeof(PendingUpdate));
            using (FileStream stream = File.Create(PendingPath)) serializer.Serialize(stream, pending);

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = source;
            start.Arguments = "--apply-update " + QuoteArgument(token) + " " + Process.GetCurrentProcess().Id.ToString();
            start.UseShellExecute = true;
            start.WorkingDirectory = Path.GetDirectoryName(target);
            Process.Start(start);
        }

        public static bool TryApplyPendingUpdate(string[] args)
        {
            if (args == null || args.Length == 0 || !string.Equals(args[0], "--apply-update", StringComparison.Ordinal)) return false;
            try
            {
                if (args.Length != 3 || !File.Exists(PendingPath)) throw new InvalidDataException("Invalid update request.");
                PendingUpdate pending;
                XmlSerializer serializer = new XmlSerializer(typeof(PendingUpdate));
                using (FileStream stream = File.OpenRead(PendingPath)) pending = serializer.Deserialize(stream) as PendingUpdate;
                if (pending == null || !string.Equals(pending.Token, args[1], StringComparison.Ordinal))
                    throw new InvalidDataException("The update authorization token is invalid.");
                DateTime created;
                if (!DateTime.TryParse(pending.CreatedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out created) ||
                    DateTime.UtcNow - created.ToUniversalTime() > TimeSpan.FromHours(2))
                    throw new InvalidDataException("The update request has expired.");

                string source = Path.GetFullPath(pending.SourcePath);
                string target = Path.GetFullPath(pending.TargetPath);
                if (!string.Equals(source, Path.GetFullPath(Application.ExecutablePath), StringComparison.OrdinalIgnoreCase) ||
                    !IsUnderDirectory(source, UpdatesDirectory) || !File.Exists(target))
                    throw new InvalidDataException("The update paths are invalid.");
                FileVersionInfo targetInfo = FileVersionInfo.GetVersionInfo(target);
                if (!string.Equals(targetInfo.ProductName, AppInfo.ProductName, StringComparison.Ordinal))
                    throw new InvalidDataException("The update target is not InputStitch.");
                if (!string.Equals(ComputeSha256(source), pending.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The pending update failed SHA-256 verification.");

                int oldProcessId;
                if (!int.TryParse(args[2], out oldProcessId)) throw new InvalidDataException("Invalid process identifier.");
                try
                {
                    Process oldProcess = Process.GetProcessById(oldProcessId);
                    oldProcess.WaitForExit(15000);
                    oldProcess.Dispose();
                }
                catch (ArgumentException) { }

                Exception copyError = null;
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    try
                    {
                        File.Copy(source, target, true);
                        copyError = null;
                        break;
                    }
                    catch (Exception ex)
                    {
                        copyError = ex;
                        Thread.Sleep(250);
                    }
                }
                if (copyError != null) throw copyError;
                try { File.Delete(PendingPath); } catch { }
                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(target) });
            }
            catch (Exception ex)
            {
                try { AppLog.Write("Update installation failed", ex); } catch { }
                try { MessageBox.Show("InputStitch 更新安装失败。\r\n\r\n" + ex.Message, AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error); } catch { }
            }
            return true;
        }

        public static void CleanupDownloads()
        {
            try
            {
                if (!Directory.Exists(UpdatesDirectory)) return;
                string current = Path.GetFullPath(Application.ExecutablePath);
                foreach (string file in Directory.GetFiles(UpdatesDirectory, "*.exe"))
                    if (!string.Equals(Path.GetFullPath(file), current, StringComparison.OrdinalIgnoreCase)) File.Delete(file);
            }
            catch (Exception ex) { AppLog.Write("Update download cleanup failed", ex); }
        }

        public static string ComputeSha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private static WebClient CreateClient()
        {
            WebClient client = new WebClient();
            client.Encoding = Encoding.UTF8;
            client.Headers[HttpRequestHeader.UserAgent] = AppInfo.ProductName + "/" + AppInfo.Version;
            client.Headers[HttpRequestHeader.Accept] = "application/xml, text/xml, */*";
            return client;
        }

        private static void ValidateAsset(UpdateAsset asset, string architecture)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.Url) || string.IsNullOrWhiteSpace(asset.Sha256))
                throw new InvalidDataException("The update manifest does not contain an asset for " + architecture + ".");
            Uri uri;
            if (!Uri.TryCreate(asset.Url, UriKind.Absolute, out uri) || uri.Scheme != Uri.UriSchemeHttps ||
                !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase) ||
                !uri.AbsolutePath.StartsWith("/ZhiHanyu-H57/InputStitch/releases/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The update asset URL is not an official InputStitch GitHub Release URL.");
            string hash = asset.Sha256.Trim();
            if (hash.Length != 64)
                throw new InvalidDataException("The update manifest contains an invalid SHA-256 value.");
            for (int i = 0; i < hash.Length; i++)
                if (!Uri.IsHexDigit(hash[i])) throw new InvalidDataException("The update manifest contains an invalid SHA-256 value.");
            asset.Sha256 = hash.ToLowerInvariant();
        }

        private static bool IsUnderDirectory(string path, string directory)
        {
            string fullPath = Path.GetFullPath(path);
            string fullDirectory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
        }

        private static string QuoteArgument(string value)
        {
            return "\"" + (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
    }

    public enum InputKind
    {
        Keyboard,
        MouseLeft,
        MouseRight,
        MouseMiddle,
        MouseX1,
        MouseX2,
        WheelUp,
        WheelDown
    }

    public enum MacroAction
    {
        Press,
        Down,
        Up
    }

    public enum TriggerRunMode
    {
        Toggle,
        Hold
    }

    [Serializable]
    public class InputSpec
    {
        public InputKind Kind = InputKind.Keyboard;
        public int VirtualKey = (int)Keys.A;
        // Captured hardware scan code. 0 means "derive from VirtualKey when sending".
        public int ScanCode = 0;
        public bool Extended = false;

        public InputSpec Clone()
        {
            InputSpec x = new InputSpec();
            x.Kind = Kind;
            x.VirtualKey = VirtualKey;
            x.ScanCode = ScanCode;
            x.Extended = Extended;
            return x;
        }
    }

    [Serializable]
    public class TriggerSpec
    {
        public bool Ctrl;
        public bool Shift;
        public bool Alt;
        public bool Win;
        public InputKind Kind = InputKind.Keyboard;
        public int VirtualKey = (int)Keys.F8;

        public TriggerSpec Clone()
        {
            TriggerSpec x = new TriggerSpec();
            x.Ctrl = Ctrl;
            x.Shift = Shift;
            x.Alt = Alt;
            x.Win = Win;
            x.Kind = Kind;
            x.VirtualKey = VirtualKey;
            return x;
        }
    }

    [Serializable]
    public class MacroStep
    {
        public MacroAction Action = MacroAction.Press;
        public InputKind Kind = InputKind.Keyboard;
        public int VirtualKey = (int)Keys.A;
        public int ScanCode = 0;
        public bool Extended = false;
        public int HoldMs = 30;
        // Fixed post-step delay retained for compatibility with existing configuration files.
        public int DelayMs = 50;
        // Optional simple random range. When enabled, each execution samples uniformly from Min..Max.
        public bool RandomDelay = false;
        public int RandomDelayMinMs = 45;
        public int RandomDelayMaxMs = 60;

        public MacroStep Clone()
        {
            MacroStep x = new MacroStep();
            x.Action = Action;
            x.Kind = Kind;
            x.VirtualKey = VirtualKey;
            x.ScanCode = ScanCode;
            x.Extended = Extended;
            x.HoldMs = HoldMs;
            x.DelayMs = DelayMs;
            x.RandomDelay = RandomDelay;
            x.RandomDelayMinMs = RandomDelayMinMs;
            x.RandomDelayMaxMs = RandomDelayMaxMs;
            return x;
        }
    }

    [Serializable]
    public class MacroDefinition
    {
        public string Name = "新宏";
        public string Description = "";
        public bool Enabled = true;
        public TriggerSpec Trigger = new TriggerSpec();
        public TriggerRunMode RunMode = TriggerRunMode.Toggle;
        public bool Infinite = false;
        public int RepeatCount = 1;
        public bool SuppressTrigger = true;
        public List<MacroStep> Steps = new List<MacroStep>();

        public MacroDefinition Clone()
        {
            MacroDefinition x = new MacroDefinition();
            x.Name = Name + Localizer.T(" - 副本");
            x.Description = Description;
            x.Enabled = Enabled;
            x.Trigger = Trigger == null ? new TriggerSpec() : Trigger.Clone();
            x.RunMode = RunMode;
            x.Infinite = Infinite;
            x.RepeatCount = RepeatCount;
            x.SuppressTrigger = SuppressTrigger;
            x.Steps = new List<MacroStep>();
            if (Steps != null)
            {
                foreach (MacroStep s in Steps) x.Steps.Add(s.Clone());
            }
            return x;
        }
    }

    [Serializable]
    public class MacroConfig
    {
        public string FormatVersion = AppInfo.ConfigFormatVersion;
        public List<MacroDefinition> Macros = new List<MacroDefinition>();
        // Scan-code SendInput is substantially more game-friendly than virtual-key SendInput.
        public bool UseScanCodeInput = true;
        // Legacy option kept only so older config.xml files continue to deserialize cleanly.
        // Older configuration files may use an explicit optional target-window model alongside the direct UI-run mode.
        public bool RestorePreviousWindowOnUiRun = true;
        public bool ActivateTargetWindowOnUiRun = false;
        public int UiRunStartDelayMs = 300;
        public bool KeepWindowTopMost = false;
        // When InputStitch itself is foreground and an editable/destructive control has focus,
        // pause macro output so injected keys cannot accidentally edit settings or activate UI.
        public bool PauseMacroInRiskyUi = true;
        // Emergency stop is global, has priority over all macro triggers, and remains active while normal triggers are suspended.
        public TriggerSpec PanicTrigger = CreateDefaultPanicTrigger();
        public bool AutoSwitchProfiles = false;
        public bool MinimizeToTray = true;
        // UI language is global and is intentionally preserved when loading a profile.
        public string Language = Localizer.Chinese;
        // Update preference is global and is intentionally preserved when loading a profile.
        public string UpdateMode = UpdateModes.Automatic;
        public bool HasSeenWelcome = false;

        private static TriggerSpec CreateDefaultPanicTrigger()
        {
            TriggerSpec t = new TriggerSpec();
            t.Ctrl = true;
            t.Shift = true;
            t.Kind = InputKind.Keyboard;
            t.VirtualKey = (int)Keys.F12;
            return t;
        }

        // Persistent identity of the target window. The HWND itself is intentionally not serialized
        // because window handles change whenever the target program is restarted.
        public string TargetProcessName = "";
        public string TargetWindowTitle = "";
        public string TargetWindowClass = "";
    }

    [Serializable]
    public class MacroPackage
    {
        public string FormatVersion = AppInfo.MacroPackageFormatVersion;
        public List<MacroDefinition> Macros = new List<MacroDefinition>();
    }

    [Serializable]
    public class ProfilePackage
    {
        public string FormatVersion = AppInfo.ProfileFormatVersion;
        public string ProfileName = "";
        public string BoundProcessName = "";
        public MacroConfig Config = new MacroConfig();
    }

    internal sealed class RecordedInputEvent
    {
        public InputSpec Input;
        public bool IsDown;
        public long TimestampMs;
    }

    public class InputEventInfo
    {
        public InputSpec Input;
        public bool Ctrl;
        public bool Shift;
        public bool Alt;
        public bool Win;
    }

    public static class ModifierSafetyPolicy
    {
        public const int Ctrl = 1;
        public const int Shift = 2;
        public const int Alt = 4;
        public const int Win = 8;
        public const int All = Ctrl | Shift | Alt | Win;

        public static int GetTriggerModifierMask(TriggerSpec trigger)
        {
            if (trigger == null) return 0;
            int mask = 0;
            if (trigger.Ctrl) mask |= Ctrl;
            if (trigger.Shift) mask |= Shift;
            if (trigger.Alt) mask |= Alt;
            if (trigger.Win) mask |= Win;
            return mask;
        }

        public static int GetEventModifierMask(InputEventInfo inputEvent)
        {
            if (inputEvent == null) return 0;
            int mask = 0;
            if (inputEvent.Ctrl) mask |= Ctrl;
            if (inputEvent.Shift) mask |= Shift;
            if (inputEvent.Alt) mask |= Alt;
            if (inputEvent.Win) mask |= Win;
            return mask;
        }

        public static bool TriggerTerminalMatches(TriggerSpec trigger, InputEventInfo inputEvent)
        {
            return trigger != null && inputEvent != null && inputEvent.Input != null &&
                   trigger.Kind == inputEvent.Input.Kind && trigger.VirtualKey == inputEvent.Input.VirtualKey;
        }

        public static bool TriggerMatchesExactly(TriggerSpec trigger, InputEventInfo inputEvent)
        {
            return TriggerTerminalMatches(trigger, inputEvent) &&
                   GetTriggerModifierMask(trigger) == GetEventModifierMask(inputEvent);
        }

        public static bool TriggerRequiredModifiersMatch(TriggerSpec trigger, InputEventInfo inputEvent)
        {
            if (!TriggerTerminalMatches(trigger, inputEvent)) return false;
            int required = GetTriggerModifierMask(trigger);
            return (GetEventModifierMask(inputEvent) & required) == required;
        }

        public static int TriggerSpecificity(TriggerSpec trigger)
        {
            int mask = GetTriggerModifierMask(trigger);
            int count = 0;
            while (mask != 0)
            {
                count += mask & 1;
                mask >>= 1;
            }
            return count;
        }

        public static bool SupportsExtraPhysicalModifiers(TriggerSpec trigger)
        {
            if (trigger == null) return false;
            if (GetTriggerModifierMask(trigger) != 0) return true;
            if (trigger.Kind != InputKind.Keyboard) return true;

            Keys key = (Keys)trigger.VirtualKey;
            if (key >= Keys.F1 && key <= Keys.F24) return true;
            if (key >= Keys.NumPad0 && key <= Keys.Divide) return true;
            return false;
        }

        // Return only PHYSICAL modifiers that would turn this single macro step into a
        // high-confidence Windows/system shortcut. Macro-authored modifier sequences remain
        // untouched; mouse and wheel steps are intentionally always allowed.
        public static int GetDangerousPhysicalModifierMask(MacroStep step, int activePhysicalModifiers)
        {
            if (step == null || step.Action == MacroAction.Up || step.Kind != InputKind.Keyboard) return 0;

            int vk = step.VirtualKey;
            if (IsModifierVirtualKey(vk)) return 0;

            int dangerous = 0;
            if ((activePhysicalModifiers & Alt) != 0 &&
                (vk == (int)Keys.Enter || vk == (int)Keys.F4 || vk == (int)Keys.Tab ||
                 vk == (int)Keys.Escape || vk == (int)Keys.Space))
                dangerous |= Alt;

            if ((activePhysicalModifiers & Ctrl) != 0 && vk == (int)Keys.Escape)
                dangerous |= Ctrl;

            if ((activePhysicalModifiers & (Ctrl | Alt)) == (Ctrl | Alt) && vk == (int)Keys.Delete)
                dangerous |= Ctrl | Alt;

            // Win shortcuts vary across Windows releases, OEM utilities, and game overlays.
            // Treat a physically held Win key as shell-reserved for keyboard Press/Down steps.
            if ((activePhysicalModifiers & Win) != 0)
                dangerous |= Win;

            return dangerous;
        }

        public static bool IsModifierVirtualKey(int vk)
        {
            return vk == (int)Keys.LControlKey || vk == (int)Keys.RControlKey || vk == (int)Keys.ControlKey ||
                   vk == (int)Keys.LShiftKey || vk == (int)Keys.RShiftKey || vk == (int)Keys.ShiftKey ||
                   vk == (int)Keys.LMenu || vk == (int)Keys.RMenu || vk == (int)Keys.Menu ||
                   vk == (int)Keys.LWin || vk == (int)Keys.RWin;
        }
    }

    public static class InputNames
    {
        public static string FormatInput(InputKind kind, int vk)
        {
            switch (kind)
            {
                case InputKind.MouseLeft: return Localizer.T("鼠标左键");
                case InputKind.MouseRight: return Localizer.T("鼠标右键");
                case InputKind.MouseMiddle: return Localizer.T("鼠标中键");
                case InputKind.MouseX1: return Localizer.T("鼠标侧键1 (X1)");
                case InputKind.MouseX2: return Localizer.T("鼠标侧键2 (X2)");
                case InputKind.WheelUp: return Localizer.T("滚轮向上");
                case InputKind.WheelDown: return Localizer.T("滚轮向下");
            }

            Keys key = (Keys)vk;
            if (key >= Keys.A && key <= Keys.Z) return key.ToString();
            if (key >= Keys.D0 && key <= Keys.D9) return ((char)('0' + (vk - (int)Keys.D0))).ToString();
            if (key >= Keys.F1 && key <= Keys.F24) return key.ToString();
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9) return "Num" + (vk - (int)Keys.NumPad0).ToString();

            switch (key)
            {
                case Keys.LControlKey: return "LCtrl";
                case Keys.RControlKey: return "RCtrl";
                case Keys.ControlKey: return "Ctrl";
                case Keys.LShiftKey: return "LShift";
                case Keys.RShiftKey: return "RShift";
                case Keys.ShiftKey: return "Shift";
                case Keys.LMenu: return "LAlt";
                case Keys.RMenu: return "RAlt";
                case Keys.Menu: return "Alt";
                case Keys.LWin: return "LWin";
                case Keys.RWin: return "RWin";
                case Keys.Space: return "Space";
                case Keys.Return: return "Enter";
                case Keys.Escape: return "Esc";
                case Keys.Tab: return "Tab";
                case Keys.Back: return "Backspace";
                case Keys.Delete: return "Delete";
                case Keys.Insert: return "Insert";
                case Keys.Home: return "Home";
                case Keys.End: return "End";
                case Keys.PageUp: return "PageUp";
                case Keys.PageDown: return "PageDown";
                case Keys.Up: return "↑";
                case Keys.Down: return "↓";
                case Keys.Left: return "←";
                case Keys.Right: return "→";
                case Keys.Capital: return "CapsLock";
                case Keys.NumLock: return "NumLock";
                case Keys.Scroll: return "ScrollLock";
                case Keys.PrintScreen: return "PrintScreen";
                case Keys.Pause: return "Pause";
                case Keys.Oemtilde: return "` / ~";
                case Keys.OemMinus: return "- / _";
                case Keys.Oemplus: return "= / +";
                case Keys.OemOpenBrackets: return "[ / {";
                case Keys.Oem6: return "] / }";
                case Keys.Oem5: return "\\ / |";
                case Keys.Oem1: return "; / :";
                case Keys.Oem7: return "' / \"";
                case Keys.Oemcomma: return ", / <";
                case Keys.OemPeriod: return ". / >";
                case Keys.OemQuestion: return "/ / ?";
                case Keys.Add: return "Num +";
                case Keys.Subtract: return "Num -";
                case Keys.Multiply: return "Num *";
                case Keys.Divide: return "Num /";
                case Keys.Decimal: return "Num .";
            }

            return key.ToString();
        }

        public static string FormatTrigger(TriggerSpec t)
        {
            if (t == null) return Localizer.T("未设置");
            List<string> p = new List<string>();
            if (t.Ctrl) p.Add("Ctrl");
            if (t.Shift) p.Add("Shift");
            if (t.Alt) p.Add("Alt");
            if (t.Win) p.Add("Win");
            p.Add(FormatInput(t.Kind, t.VirtualKey));
            return string.Join(" + ", p.ToArray());
        }

        public static string FormatAction(MacroAction action)
        {
            if (action == MacroAction.Press) return Localizer.T("按一下");
            if (action == MacroAction.Down) return Localizer.T("按住 / KeyDown");
            return Localizer.T("松开 / KeyUp");
        }
    }

    public class HookManager : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int HC_ACTION = 0;

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_XBUTTONUP = 0x020C;
        private const int WM_MOUSEWHEEL = 0x020A;

        private const uint LLKHF_EXTENDED = 0x01;
        private const uint LLKHF_INJECTED = 0x10;
        private const uint LLMHF_INJECTED = 0x00000001;

        private IntPtr keyboardHook = IntPtr.Zero;
        private IntPtr mouseHook = IntPtr.Zero;
        private LowLevelKeyboardProc keyboardProc;
        private LowLevelMouseProc mouseProc;

        // Do not query GetAsyncKeyState from inside LowLevelKeyboardProc: Windows calls
        // the hook before it updates asynchronous key state. Track physical state ourselves.
        private readonly HashSet<int> physicalKeysDown = new HashSet<int>();
        private readonly HashSet<int> suppressedKeysUntilUp = new HashSet<int>();
        private readonly HashSet<InputKind> suppressedMouseUntilUp = new HashSet<InputKind>();
        // Updated only from non-injected keyboard events. The worker thread reads this atomic
        // snapshot so a physically held modifier cannot accidentally combine with macro output.
        private volatile int physicalModifierMask;
        private int staleStateRepairCount;
        private long physicalEventCount;

        public Func<InputEventInfo, bool> OnTerminalInput;
        public Action<InputEventInfo> OnTerminalInputReleased;
        public Func<bool> ShouldReportModifierInput;

        public HookManager()
        {
            SeedModifierState((int)Keys.LControlKey);
            SeedModifierState((int)Keys.RControlKey);
            SeedModifierState((int)Keys.LShiftKey);
            SeedModifierState((int)Keys.RShiftKey);
            SeedModifierState((int)Keys.LMenu);
            SeedModifierState((int)Keys.RMenu);
            SeedModifierState((int)Keys.LWin);
            SeedModifierState((int)Keys.RWin);

            keyboardProc = KeyboardCallback;
            mouseProc = MouseCallback;
            IntPtr module = GetModuleHandle(null);
            keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProc, module, 0);
            mouseHook = SetWindowsHookEx(WH_MOUSE_LL, mouseProc, module, 0);

            if (keyboardHook == IntPtr.Zero || mouseHook == IntPtr.Zero)
            {
                Dispose();
                throw new Win32Exception(Marshal.GetLastWin32Error(), "无法安装全局键盘/鼠标钩子。请尝试以管理员身份运行。\n");
            }
        }

        private void SeedModifierState(int vk)
        {
            if ((GetAsyncKeyState(vk) & 0x8000) != 0) physicalKeysDown.Add(vk);
            RefreshPhysicalModifierMask();
        }

        private IntPtr KeyboardCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode == HC_ACTION)
            {
                int msg = wParam.ToInt32();
                bool isDown = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;
                bool isUp = msg == WM_KEYUP || msg == WM_SYSKEYUP;
                if (isDown || isUp)
                {
                    KBDLLHOOKSTRUCT data = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                    if ((data.flags & LLKHF_INJECTED) == 0)
                    {
                        Interlocked.Increment(ref physicalEventCount);
                        int vk = (int)data.vkCode;
                        if (isUp)
                        {
                            physicalKeysDown.Remove(vk);
                            RefreshPhysicalModifierMask();
                            if (OnTerminalInputReleased != null)
                            {
                                try { OnTerminalInputReleased(BuildKeyboardEvent(vk, (int)data.scanCode, (data.flags & LLKHF_EXTENDED) != 0)); }
                                catch { }
                            }
                            if (suppressedKeysUntilUp.Remove(vk)) return (IntPtr)1;
                        }
                        else
                        {
                            // Add() returns false for Windows key-repeat. Only fire a macro on
                            // the physical up->down edge, not on every repeated WM_KEYDOWN.
                            bool firstDown = physicalKeysDown.Add(vk);
                            if (!firstDown && !IsAsyncKeyDown(vk))
                            {
                                // A KeyUp can occasionally be lost while an exclusive-fullscreen
                                // application is changing foreground ownership. In that case the old
                                // entry must not make every later press look like keyboard auto-repeat.
                                suppressedKeysUntilUp.Remove(vk);
                                firstDown = true;
                                Interlocked.Increment(ref staleStateRepairCount);
                            }
                            RefreshPhysicalModifierMask();
                            bool reportModifier = IsModifierKey(vk) && ShouldReportModifierInput != null && ShouldReportModifierInput();
                            if (firstDown && (!IsModifierKey(vk) || reportModifier))
                            {
                                InputEventInfo e = BuildKeyboardEvent(vk, (int)data.scanCode, (data.flags & LLKHF_EXTENDED) != 0);
                                if (OnTerminalInput != null && OnTerminalInput(e))
                                {
                                    suppressedKeysUntilUp.Add(vk);
                                    return (IntPtr)1;
                                }
                            }
                            else if (!firstDown && suppressedKeysUntilUp.Contains(vk))
                            {
                                // Suppress repeated key-down messages until the physical key is released.
                                return (IntPtr)1;
                            }
                        }
                    }
                }
            }
            return CallNextHookEx(keyboardHook, nCode, wParam, lParam);
        }

        private IntPtr MouseCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode == HC_ACTION)
            {
                MSLLHOOKSTRUCT data = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                if ((data.flags & LLMHF_INJECTED) == 0)
                {
                    Interlocked.Increment(ref physicalEventCount);
                    InputKind? kind = null;
                    bool isButtonUp = false;
                    int msg = wParam.ToInt32();
                    if (msg == WM_LBUTTONDOWN) kind = InputKind.MouseLeft;
                    else if (msg == WM_LBUTTONUP) { kind = InputKind.MouseLeft; isButtonUp = true; }
                    else if (msg == WM_RBUTTONDOWN) kind = InputKind.MouseRight;
                    else if (msg == WM_RBUTTONUP) { kind = InputKind.MouseRight; isButtonUp = true; }
                    else if (msg == WM_MBUTTONDOWN) kind = InputKind.MouseMiddle;
                    else if (msg == WM_MBUTTONUP) { kind = InputKind.MouseMiddle; isButtonUp = true; }
                    else if (msg == WM_XBUTTONDOWN || msg == WM_XBUTTONUP)
                    {
                        int high = (short)((data.mouseData >> 16) & 0xffff);
                        kind = high == 1 ? InputKind.MouseX1 : InputKind.MouseX2;
                        isButtonUp = msg == WM_XBUTTONUP;
                    }
                    else if (msg == WM_MOUSEWHEEL)
                    {
                        short delta = (short)((data.mouseData >> 16) & 0xffff);
                        kind = delta > 0 ? InputKind.WheelUp : InputKind.WheelDown;
                    }

                    if (kind.HasValue)
                    {
                        if (isButtonUp)
                        {
                            if (OnTerminalInputReleased != null)
                            {
                                try { OnTerminalInputReleased(BuildMouseEvent(kind.Value)); }
                                catch { }
                            }
                            if (suppressedMouseUntilUp.Remove(kind.Value)) return (IntPtr)1;
                        }
                        else
                        {
                            InputEventInfo e = BuildMouseEvent(kind.Value);
                            if (OnTerminalInput != null && OnTerminalInput(e))
                            {
                                if (kind.Value != InputKind.WheelUp && kind.Value != InputKind.WheelDown)
                                    suppressedMouseUntilUp.Add(kind.Value);
                                return (IntPtr)1;
                            }
                        }
                    }
                }
            }
            return CallNextHookEx(mouseHook, nCode, wParam, lParam);
        }

        private static bool IsModifierKey(int vk)
        {
            return vk == (int)Keys.LControlKey || vk == (int)Keys.RControlKey || vk == (int)Keys.ControlKey ||
                   vk == (int)Keys.LShiftKey || vk == (int)Keys.RShiftKey || vk == (int)Keys.ShiftKey ||
                   vk == (int)Keys.LMenu || vk == (int)Keys.RMenu || vk == (int)Keys.Menu ||
                   vk == (int)Keys.LWin || vk == (int)Keys.RWin;
        }

        public int PhysicalModifierMaskSnapshot
        {
            get { return physicalModifierMask; }
        }

        public int StaleStateRepairCount
        {
            get { return Volatile.Read(ref staleStateRepairCount); }
        }

        public long PhysicalEventCount
        {
            get { return Interlocked.Read(ref physicalEventCount); }
        }

        public int ReconcilePhysicalState()
        {
            int repaired = 0;
            if (physicalKeysDown.Count > 0)
            {
                List<int> staleKeys = new List<int>();
                foreach (int vk in physicalKeysDown)
                    if (!IsAsyncKeyDown(vk)) staleKeys.Add(vk);
                foreach (int vk in staleKeys)
                {
                    if (physicalKeysDown.Remove(vk)) repaired++;
                    suppressedKeysUntilUp.Remove(vk);
                }
            }

            if (suppressedKeysUntilUp.Count > 0)
            {
                List<int> staleSuppressedKeys = new List<int>();
                foreach (int vk in suppressedKeysUntilUp)
                    if (!IsAsyncKeyDown(vk)) staleSuppressedKeys.Add(vk);
                foreach (int vk in staleSuppressedKeys)
                    if (suppressedKeysUntilUp.Remove(vk)) repaired++;
            }

            if (suppressedMouseUntilUp.Count > 0)
            {
                List<InputKind> staleMouse = new List<InputKind>();
                foreach (InputKind kind in suppressedMouseUntilUp)
                {
                    int vk = MouseVirtualKey(kind);
                    if (vk != 0 && !IsAsyncKeyDown(vk)) staleMouse.Add(kind);
                }
                foreach (InputKind kind in staleMouse)
                    if (suppressedMouseUntilUp.Remove(kind)) repaired++;
            }

            RefreshPhysicalModifierMask();
            if (repaired > 0) Interlocked.Add(ref staleStateRepairCount, repaired);
            return repaired;
        }

        public string PhysicalModifierText
        {
            get
            {
                int mask = physicalModifierMask;
                List<string> parts = new List<string>();
                if ((mask & 1) != 0) parts.Add("Ctrl");
                if ((mask & 2) != 0) parts.Add("Shift");
                if ((mask & 4) != 0) parts.Add("Alt");
                if ((mask & 8) != 0) parts.Add("Win");
                return parts.Count == 0 ? Localizer.T("无") : string.Join(" + ", parts.ToArray());
            }
        }

        private void RefreshPhysicalModifierMask()
        {
            int mask = 0;
            if (physicalKeysDown.Contains((int)Keys.LControlKey) || physicalKeysDown.Contains((int)Keys.RControlKey) || physicalKeysDown.Contains((int)Keys.ControlKey)) mask |= 1;
            if (physicalKeysDown.Contains((int)Keys.LShiftKey) || physicalKeysDown.Contains((int)Keys.RShiftKey) || physicalKeysDown.Contains((int)Keys.ShiftKey)) mask |= 2;
            if (physicalKeysDown.Contains((int)Keys.LMenu) || physicalKeysDown.Contains((int)Keys.RMenu) || physicalKeysDown.Contains((int)Keys.Menu)) mask |= 4;
            if (physicalKeysDown.Contains((int)Keys.LWin) || physicalKeysDown.Contains((int)Keys.RWin)) mask |= 8;
            if (physicalModifierMask != mask)
            {
                physicalModifierMask = mask;
            }
        }

        private static bool IsAsyncKeyDown(int vk)
        {
            return (GetAsyncKeyState(vk) & 0x8000) != 0;
        }

        private static int MouseVirtualKey(InputKind kind)
        {
            if (kind == InputKind.MouseLeft) return 0x01;
            if (kind == InputKind.MouseRight) return 0x02;
            if (kind == InputKind.MouseMiddle) return 0x04;
            if (kind == InputKind.MouseX1) return 0x05;
            if (kind == InputKind.MouseX2) return 0x06;
            return 0;
        }

        private bool CtrlDown()
        {
            return physicalKeysDown.Contains((int)Keys.LControlKey) || physicalKeysDown.Contains((int)Keys.RControlKey) || physicalKeysDown.Contains((int)Keys.ControlKey);
        }

        private bool ShiftDown()
        {
            return physicalKeysDown.Contains((int)Keys.LShiftKey) || physicalKeysDown.Contains((int)Keys.RShiftKey) || physicalKeysDown.Contains((int)Keys.ShiftKey);
        }

        private bool AltDown()
        {
            return physicalKeysDown.Contains((int)Keys.LMenu) || physicalKeysDown.Contains((int)Keys.RMenu) || physicalKeysDown.Contains((int)Keys.Menu);
        }

        private bool WinDown()
        {
            return physicalKeysDown.Contains((int)Keys.LWin) || physicalKeysDown.Contains((int)Keys.RWin);
        }

        private InputEventInfo BuildKeyboardEvent(int vk, int scanCode, bool extended)
        {
            InputEventInfo e = new InputEventInfo();
            e.Input = new InputSpec();
            e.Input.Kind = InputKind.Keyboard;
            e.Input.VirtualKey = vk;
            e.Input.ScanCode = scanCode;
            e.Input.Extended = extended;
            e.Ctrl = CtrlDown();
            e.Shift = ShiftDown();
            e.Alt = AltDown();
            e.Win = WinDown();
            return e;
        }

        private InputEventInfo BuildMouseEvent(InputKind kind)
        {
            InputEventInfo e = new InputEventInfo();
            e.Input = new InputSpec();
            e.Input.Kind = kind;
            e.Input.VirtualKey = 0;
            e.Ctrl = CtrlDown();
            e.Shift = ShiftDown();
            e.Alt = AltDown();
            e.Win = WinDown();
            return e;
        }

        public void Dispose()
        {
            if (keyboardHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(keyboardHook);
                keyboardHook = IntPtr.Zero;
            }
            if (mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(mouseHook);
                mouseHook = IntPtr.Zero;
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
    }

    public static class PhysicalInputState
    {
        // GetAsyncKeyState is intentionally used OUTSIDE LowLevelKeyboardProc. Here it is a
        // convenient way to wait until the terminal trigger key/button is physically released.
        // Modifier safety is evaluated separately for each macro step so holding Shift to sprint
        // does not block unrelated game input.
        public static bool IsTriggerTerminalReleased(TriggerSpec trigger)
        {
            if (trigger == null) return true;

            if (trigger.Kind == InputKind.Keyboard) return !IsDown(trigger.VirtualKey);
            if (trigger.Kind == InputKind.MouseLeft) return !IsDown(0x01);
            if (trigger.Kind == InputKind.MouseRight) return !IsDown(0x02);
            if (trigger.Kind == InputKind.MouseMiddle) return !IsDown(0x04);
            if (trigger.Kind == InputKind.MouseX1) return !IsDown(0x05);
            if (trigger.Kind == InputKind.MouseX2) return !IsDown(0x06);

            // Mouse wheel events have no persistent down state.
            return true;
        }

        public static int GetModifierMask()
        {
            int mask = 0;
            if (IsDown(0x11)) mask |= ModifierSafetyPolicy.Ctrl;
            if (IsDown(0x10)) mask |= ModifierSafetyPolicy.Shift;
            if (IsDown(0x12)) mask |= ModifierSafetyPolicy.Alt;
            if (IsDown(0x5B) || IsDown(0x5C)) mask |= ModifierSafetyPolicy.Win;
            return mask;
        }

        private static bool IsDown(int virtualKey)
        {
            return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
    }

    public static class InputSender
    {
        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;

        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_SCANCODE = 0x0008;
        private const uint MAPVK_VK_TO_VSC = 0;

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_XDOWN = 0x0080;
        private const uint MOUSEEVENTF_XUP = 0x0100;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint XBUTTON1 = 0x0001;
        private const uint XBUTTON2 = 0x0002;
        private const int WHEEL_DELTA = 120;

        public static bool UseScanCodeInput = true;

        public static void SendDown(InputSpec input)
        {
            if (input.Kind == InputKind.Keyboard) SendKeyboard(input, false);
            else if (input.Kind == InputKind.WheelUp) SendWheel(WHEEL_DELTA);
            else if (input.Kind == InputKind.WheelDown) SendWheel(-WHEEL_DELTA);
            else SendMouseButton(input.Kind, false);
        }

        public static void SendUp(InputSpec input)
        {
            if (input.Kind == InputKind.Keyboard) SendKeyboard(input, true);
            else if (input.Kind != InputKind.WheelUp && input.Kind != InputKind.WheelDown) SendMouseButton(input.Kind, true);
        }

        public static bool IsHoldable(InputSpec input)
        {
            return input.Kind != InputKind.WheelUp && input.Kind != InputKind.WheelDown;
        }

        private static void SendKeyboard(InputSpec spec, bool keyUp)
        {
            INPUT input = new INPUT();
            input.type = INPUT_KEYBOARD;
            uint flags = keyUp ? KEYEVENTF_KEYUP : 0;

            // Games often consume physical scan codes rather than text-oriented virtual keys.
            // Use the exact scan code captured by the hook when available; old configs fall back
            // to MapVirtualKey. If mapping fails, keep a virtual-key fallback for compatibility.
            if (UseScanCodeInput)
            {
                int scan = spec.ScanCode;
                if (scan <= 0) scan = (int)MapVirtualKey((uint)spec.VirtualKey, MAPVK_VK_TO_VSC);
                if (scan > 0)
                {
                    input.U.ki.wVk = 0;
                    input.U.ki.wScan = (ushort)scan;
                    flags |= KEYEVENTF_SCANCODE;
                    if (spec.Extended || IsExtendedKey(spec.VirtualKey)) flags |= KEYEVENTF_EXTENDEDKEY;
                }
                else
                {
                    input.U.ki.wVk = (ushort)spec.VirtualKey;
                    input.U.ki.wScan = 0;
                    if (IsExtendedKey(spec.VirtualKey)) flags |= KEYEVENTF_EXTENDEDKEY;
                }
            }
            else
            {
                input.U.ki.wVk = (ushort)spec.VirtualKey;
                input.U.ki.wScan = 0;
                if (IsExtendedKey(spec.VirtualKey)) flags |= KEYEVENTF_EXTENDEDKEY;
            }

            input.U.ki.dwFlags = flags;
            input.U.ki.time = 0;
            input.U.ki.dwExtraInfo = IntPtr.Zero;
            SendOne(input, "键盘输入");
        }

        private static bool IsExtendedKey(int vk)
        {
            Keys k = (Keys)vk;
            return k == Keys.Insert || k == Keys.Delete || k == Keys.Home || k == Keys.End ||
                   k == Keys.PageUp || k == Keys.PageDown || k == Keys.Left || k == Keys.Right ||
                   k == Keys.Up || k == Keys.Down || k == Keys.NumLock || k == Keys.Cancel ||
                   k == Keys.PrintScreen || k == Keys.Divide || k == Keys.RControlKey || k == Keys.RMenu ||
                   k == Keys.LWin || k == Keys.RWin;
        }

        private static void SendWheel(int delta)
        {
            INPUT input = new INPUT();
            input.type = INPUT_MOUSE;
            input.U.mi.dwFlags = MOUSEEVENTF_WHEEL;
            input.U.mi.mouseData = unchecked((uint)delta);
            SendOne(input, "鼠标滚轮输入");
        }

        private static void SendMouseButton(InputKind kind, bool up)
        {
            uint flags = 0;
            uint data = 0;
            if (kind == InputKind.MouseLeft) flags = up ? MOUSEEVENTF_LEFTUP : MOUSEEVENTF_LEFTDOWN;
            else if (kind == InputKind.MouseRight) flags = up ? MOUSEEVENTF_RIGHTUP : MOUSEEVENTF_RIGHTDOWN;
            else if (kind == InputKind.MouseMiddle) flags = up ? MOUSEEVENTF_MIDDLEUP : MOUSEEVENTF_MIDDLEDOWN;
            else if (kind == InputKind.MouseX1)
            {
                flags = up ? MOUSEEVENTF_XUP : MOUSEEVENTF_XDOWN;
                data = XBUTTON1;
            }
            else if (kind == InputKind.MouseX2)
            {
                flags = up ? MOUSEEVENTF_XUP : MOUSEEVENTF_XDOWN;
                data = XBUTTON2;
            }

            if (flags == 0) return;
            INPUT input = new INPUT();
            input.type = INPUT_MOUSE;
            input.U.mi.dwFlags = flags;
            input.U.mi.mouseData = data;
            SendOne(input, "鼠标按钮输入");
        }

        private static void SendOne(INPUT input, string description)
        {
            INPUT[] arr = new INPUT[] { input };
            uint sent = SendInput(1, arr, Marshal.SizeOf(typeof(INPUT)));
            if (sent != 1)
            {
                int err = Marshal.GetLastWin32Error();
                throw new Win32Exception(err, description + "发送失败（SendInput 返回 " + sent.ToString() + "）。如果目标程序权限更高，请以管理员身份运行 InputStitch。");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);
    }

    public sealed class TargetWindowIdentity
    {
        public IntPtr Handle = IntPtr.Zero;
        public string ProcessName = "";
        public string Title = "";
        public string ClassName = "";

        public string DisplayText
        {
            get
            {
                string title = string.IsNullOrWhiteSpace(Title) ? "（无窗口标题）" : Title.Trim();
                string proc = string.IsNullOrWhiteSpace(ProcessName) ? "未知进程" : ProcessName.Trim();
                return title + "  [" + proc + "]";
            }
        }
    }

    public static class NativeWindowFocus
    {
        private const int SW_RESTORE = 9;

        public static IntPtr ForegroundWindow()
        {
            return GetForegroundWindow();
        }

        public static bool IsExternalProcessWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return false;
            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            return pid != 0 && pid != GetCurrentProcessId();
        }

        public static bool IsCurrentProcessWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return false;
            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            return pid != 0 && pid == GetCurrentProcessId();
        }

        public static bool IsUsableExternalWindow(IntPtr hwnd)
        {
            return IsExternalProcessWindow(hwnd) && IsWindowVisible(hwnd);
        }

        public static bool IsLikelyTransientTaskSwitcher(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return false;
            string cls = GetClass(hwnd) ?? "";
            string title = GetText(hwnd) ?? "";

            // These are common shell/task-switcher host names seen on modern Windows. This is
            // deliberately only a heuristic; the foreground-history fallback below is the main
            // protection and does not depend on any one Windows build using a specific class.
            if (cls.IndexOf("MultitaskingViewFrame", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (cls.IndexOf("TaskSwitcher", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (cls.IndexOf("XamlExplorerHostIslandWindow", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (string.Equals(title.Trim(), "Task Switching", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static bool TryActivate(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return false;
            if (IsIconic(hwnd)) ShowWindowAsync(hwnd, SW_RESTORE);
            SetForegroundWindow(hwnd);
            // SetForegroundWindow is subject to Windows foreground-lock rules. Verify the result
            // instead of trusting only the return value.
            for (int i = 0; i < 20; i++)
            {
                if (GetForegroundWindow() == hwnd) return true;
                Thread.Sleep(10);
            }
            return GetForegroundWindow() == hwnd;
        }

        public static TargetWindowIdentity Describe(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return null;
            TargetWindowIdentity x = new TargetWindowIdentity();
            x.Handle = hwnd;
            x.Title = GetText(hwnd);
            x.ClassName = GetClass(hwnd);
            try
            {
                uint pid;
                GetWindowThreadProcessId(hwnd, out pid);
                if (pid != 0)
                {
                    using (Process proc = Process.GetProcessById((int)pid))
                    {
                        x.ProcessName = proc.ProcessName ?? "";
                    }
                }
            }
            catch { }
            return x;
        }

        public static IntPtr ResolveConfiguredTarget(string processName, string title, string className)
        {
            if (string.IsNullOrWhiteSpace(processName) && string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(className))
                return IntPtr.Zero;

            IntPtr best = IntPtr.Zero;
            int bestScore = int.MinValue;
            EnumWindows(delegate(IntPtr hwnd, IntPtr lParam)
            {
                if (!IsUsableExternalWindow(hwnd)) return true;
                TargetWindowIdentity info = Describe(hwnd);
                if (info == null) return true;

                int score = 0;
                if (!string.IsNullOrWhiteSpace(processName))
                {
                    if (!string.Equals(info.ProcessName, processName, StringComparison.OrdinalIgnoreCase)) return true;
                    score += 100;
                }
                if (!string.IsNullOrWhiteSpace(className))
                {
                    if (string.Equals(info.ClassName, className, StringComparison.Ordinal)) score += 30;
                    else if (!string.IsNullOrWhiteSpace(processName)) score -= 5;
                    else return true;
                }
                if (!string.IsNullOrWhiteSpace(title))
                {
                    if (string.Equals(info.Title, title, StringComparison.Ordinal)) score += 50;
                    else if (!string.IsNullOrWhiteSpace(info.Title) && info.Title.IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0) score += 20;
                    else if (!string.IsNullOrWhiteSpace(processName)) score -= 3;
                    else return true;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = hwnd;
                }
                return true;
            }, IntPtr.Zero);
            return best;
        }

        private static string GetText(IntPtr hwnd)
        {
            int len = GetWindowTextLength(hwnd);
            if (len <= 0) return "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder(len + 1);
            GetWindowText(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }

        private static string GetClass(IntPtr hwnd)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
            int n = GetClassName(hwnd, sb, sb.Capacity);
            return n > 0 ? sb.ToString() : "";
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentProcessId();
    }

    public class StepEditDialog : Form
    {
        private ComboBox actionBox;
        private TextBox inputText;
        private Button captureButton;
        private NumericUpDown holdBox;
        private NumericUpDown delayBox;
        private CheckBox randomDelayBox;
        private NumericUpDown randomMinBox;
        private NumericUpDown randomMaxBox;
        private Label holdLabel;
        private Label fixedDelayLabel;
        private Label randomDelayLabel;
        private InputSpec selectedInput;
        private MainForm ownerMain;

        public MacroStep ResultStep;

        public StepEditDialog(MainForm owner, MacroStep source)
        {
            ownerMain = owner;
            Text = source == null ? "添加宏步骤" : "编辑宏步骤";
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(560, 390);
            ClientSize = new Size(650, 410);
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(247, 249, 252);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(20);
            root.ColumnCount = 3;
            root.RowCount = 6;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            for (int i = 0; i < 5; i++) root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Controls.Add(root);

            Label aLabel = new Label();
            aLabel.Text = "操作：";
            aLabel.Dock = DockStyle.Fill;
            aLabel.MinimumSize = new Size(0, 34);
            aLabel.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(aLabel, 0, 0);

            actionBox = new ComboBox();
            actionBox.DropDownStyle = ComboBoxStyle.DropDownList;
            actionBox.Items.AddRange(new object[] { Localizer.T("按一下（按下→等待→松开）"), Localizer.T("按住 / KeyDown"), Localizer.T("松开 / KeyUp") });
            actionBox.Dock = DockStyle.Fill;
            actionBox.Margin = new Padding(3, 4, 3, 6);
            actionBox.SelectedIndexChanged += delegate { UpdateHoldEnabled(); };
            root.Controls.Add(actionBox, 1, 0);
            root.SetColumnSpan(actionBox, 2);

            Label kLabel = new Label();
            kLabel.Text = "按键/鼠标：";
            kLabel.Dock = DockStyle.Fill;
            kLabel.MinimumSize = new Size(0, 34);
            kLabel.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(kLabel, 0, 1);

            inputText = new TextBox();
            inputText.Dock = DockStyle.Fill;
            inputText.Margin = new Padding(3, 4, 8, 6);
            inputText.ReadOnly = true;
            root.Controls.Add(inputText, 1, 1);

            captureButton = new Button();
            captureButton.Text = Localizer.T("捕获输入");
            captureButton.AutoSize = true;
            captureButton.MinimumSize = new Size(122, 32);
            captureButton.Click += CaptureButton_Click;
            root.Controls.Add(captureButton, 2, 1);

            holdLabel = new Label();
            holdLabel.Text = "按住时长：";
            holdLabel.Dock = DockStyle.Fill;
            holdLabel.MinimumSize = new Size(0, 36);
            holdLabel.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(holdLabel, 0, 2);

            FlowLayoutPanel holdRow = new FlowLayoutPanel();
            holdRow.Dock = DockStyle.Fill;
            holdRow.AutoSize = true;
            holdRow.WrapContents = true;
            holdRow.Margin = new Padding(0);

            holdBox = new NumericUpDown();
            holdBox.Size = new Size(110, 26);
            holdBox.Minimum = 0;
            holdBox.Maximum = 600000;
            holdBox.Value = 30;
            holdBox.Margin = new Padding(3, 4, 6, 6);
            holdRow.Controls.Add(holdBox);

            Label ms1 = new Label();
            ms1.Text = "ms";
            ms1.AutoSize = true;
            ms1.Margin = new Padding(0, 7, 24, 0);
            holdRow.Controls.Add(ms1);

            randomDelayBox = new CheckBox();
            randomDelayBox.Text = "随机步骤间隔";
            randomDelayBox.AutoSize = true;
            randomDelayBox.Margin = new Padding(0, 6, 0, 0);
            randomDelayBox.CheckedChanged += delegate { UpdateDelayControls(); };
            holdRow.Controls.Add(randomDelayBox);
            root.Controls.Add(holdRow, 1, 2);
            root.SetColumnSpan(holdRow, 2);

            fixedDelayLabel = new Label();
            fixedDelayLabel.Text = "步骤后间隔：";
            fixedDelayLabel.Dock = DockStyle.Fill;
            fixedDelayLabel.MinimumSize = new Size(0, 36);
            fixedDelayLabel.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(fixedDelayLabel, 0, 3);

            FlowLayoutPanel delayRow = new FlowLayoutPanel();
            delayRow.Dock = DockStyle.Fill;
            delayRow.AutoSize = true;
            delayRow.WrapContents = false;
            delayRow.Margin = new Padding(0);

            delayBox = new NumericUpDown();
            delayBox.Size = new Size(110, 26);
            delayBox.Minimum = 0;
            delayBox.Maximum = 600000;
            delayBox.Value = 50;
            delayBox.Margin = new Padding(3, 4, 6, 6);
            delayRow.Controls.Add(delayBox);

            Label ms2 = new Label();
            ms2.Text = "ms";
            ms2.AutoSize = true;
            ms2.Margin = new Padding(0, 7, 0, 0);
            delayRow.Controls.Add(ms2);
            root.Controls.Add(delayRow, 1, 3);
            root.SetColumnSpan(delayRow, 2);

            randomDelayLabel = new Label();
            randomDelayLabel.Text = "随机范围：";
            randomDelayLabel.Dock = DockStyle.Fill;
            randomDelayLabel.MinimumSize = new Size(0, 36);
            randomDelayLabel.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(randomDelayLabel, 0, 4);

            FlowLayoutPanel randomRow = new FlowLayoutPanel();
            randomRow.Dock = DockStyle.Fill;
            randomRow.AutoSize = true;
            randomRow.WrapContents = false;
            randomRow.Margin = new Padding(0);

            randomMinBox = new NumericUpDown();
            randomMinBox.Size = new Size(90, 26);
            randomMinBox.Minimum = 0;
            randomMinBox.Maximum = 600000;
            randomMinBox.Value = 45;
            randomMinBox.Margin = new Padding(3, 4, 6, 6);
            randomRow.Controls.Add(randomMinBox);

            Label dash = new Label();
            dash.Text = "～";
            dash.AutoSize = true;
            dash.Margin = new Padding(0, 7, 6, 0);
            randomRow.Controls.Add(dash);

            randomMaxBox = new NumericUpDown();
            randomMaxBox.Size = new Size(90, 26);
            randomMaxBox.Minimum = 0;
            randomMaxBox.Maximum = 600000;
            randomMaxBox.Value = 60;
            randomMaxBox.Margin = new Padding(0, 4, 6, 6);
            randomRow.Controls.Add(randomMaxBox);

            Label ms3 = new Label();
            ms3.Text = "ms";
            ms3.AutoSize = true;
            ms3.Margin = new Padding(0, 7, 0, 0);
            randomRow.Controls.Add(ms3);
            root.Controls.Add(randomRow, 1, 4);
            root.SetColumnSpan(randomRow, 2);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.WrapContents = false;
            buttons.Margin = new Padding(0, 16, 0, 0);

            Button ok = new Button();
            ok.Text = "确定";
            ok.AutoSize = true;
            ok.MinimumSize = new Size(96, 34);
            ok.DialogResult = DialogResult.None;
            ok.Click += Ok_Click;
            buttons.Controls.Add(ok);

            Button cancel = new Button();
            cancel.Text = "取消";
            cancel.AutoSize = true;
            cancel.MinimumSize = new Size(96, 34);
            cancel.DialogResult = DialogResult.Cancel;
            buttons.Controls.Add(cancel);
            root.Controls.Add(buttons, 0, 5);
            root.SetColumnSpan(buttons, 3);
            CancelButton = cancel;

            if (source == null)
            {
                actionBox.SelectedIndex = 0;
                selectedInput = new InputSpec();
                selectedInput.Kind = InputKind.Keyboard;
                selectedInput.VirtualKey = (int)Keys.A;
            }
            else
            {
                actionBox.SelectedIndex = source.Action == MacroAction.Press ? 0 : (source.Action == MacroAction.Down ? 1 : 2);
                selectedInput = new InputSpec();
                selectedInput.Kind = source.Kind;
                selectedInput.VirtualKey = source.VirtualKey;
                selectedInput.ScanCode = source.ScanCode;
                selectedInput.Extended = source.Extended;
                holdBox.Value = ClampDecimal(source.HoldMs, holdBox.Minimum, holdBox.Maximum);
                delayBox.Value = ClampDecimal(source.DelayMs, delayBox.Minimum, delayBox.Maximum);
                randomDelayBox.Checked = source.RandomDelay;
                randomMinBox.Value = ClampDecimal(source.RandomDelayMinMs, randomMinBox.Minimum, randomMinBox.Maximum);
                randomMaxBox.Value = ClampDecimal(source.RandomDelayMaxMs, randomMaxBox.Minimum, randomMaxBox.Maximum);
            }
            RefreshInputText();
            UpdateHoldEnabled();
            UpdateDelayControls();
            Localizer.ApplyStaticControls(this);
        }

        private static decimal ClampDecimal(int x, decimal min, decimal max)
        {
            if (x < min) return min;
            if (x > max) return max;
            return x;
        }

        private void CaptureButton_Click(object sender, EventArgs e)
        {
            captureButton.Text = Localizer.T("请按键…");
            captureButton.Enabled = false;
            ownerMain.BeginStepInputCapture(delegate(InputSpec input)
            {
                if (IsDisposed) return;
                if (input != null)
                {
                    selectedInput = input.Clone();
                    RefreshInputText();
                }
                captureButton.Text = Localizer.T("捕获输入");
                captureButton.Enabled = true;
                UpdateHoldEnabled();
            });
        }

        private void RefreshInputText()
        {
            inputText.Text = InputNames.FormatInput(selectedInput.Kind, selectedInput.VirtualKey);
        }

        private void UpdateHoldEnabled()
        {
            bool press = actionBox.SelectedIndex == 0;
            bool holdable = selectedInput == null || InputSender.IsHoldable(selectedInput);
            holdBox.Enabled = press && holdable;
            holdLabel.Enabled = press && holdable;
        }

        private void UpdateDelayControls()
        {
            bool random = randomDelayBox.Checked;
            fixedDelayLabel.Enabled = !random;
            delayBox.Enabled = !random;
            randomDelayLabel.Enabled = random;
            randomMinBox.Enabled = random;
            randomMaxBox.Enabled = random;
        }

        private void Ok_Click(object sender, EventArgs e)
        {
            if (selectedInput == null)
            {
                LocalizedMessageBox.Show(this, "请先选择一个按键或鼠标输入。", AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if ((selectedInput.Kind == InputKind.WheelUp || selectedInput.Kind == InputKind.WheelDown) && actionBox.SelectedIndex != 0)
            {
                LocalizedMessageBox.Show(this, "滚轮只支持“按一下”动作，因为滚轮没有持续按下/松开的状态。", AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (randomDelayBox.Checked && randomMinBox.Value > randomMaxBox.Value)
            {
                LocalizedMessageBox.Show(this, "随机间隔的最小值不能大于最大值。", AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            MacroStep s = new MacroStep();
            s.Action = actionBox.SelectedIndex == 0 ? MacroAction.Press : (actionBox.SelectedIndex == 1 ? MacroAction.Down : MacroAction.Up);
            s.Kind = selectedInput.Kind;
            s.VirtualKey = selectedInput.VirtualKey;
            s.ScanCode = selectedInput.ScanCode;
            s.Extended = selectedInput.Extended;
            s.HoldMs = (int)holdBox.Value;
            s.DelayMs = (int)delayBox.Value;
            s.RandomDelay = randomDelayBox.Checked;
            s.RandomDelayMinMs = (int)randomMinBox.Value;
            s.RandomDelayMaxMs = (int)randomMaxBox.Value;
            ResultStep = s;
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ownerMain.CancelCapture();
            base.OnFormClosed(e);
        }
    }

    internal sealed class InputSinkControl : Control
    {
        public InputSinkControl()
        {
            SetStyle(ControlStyles.Selectable, true);
            TabStop = false;
            Size = new Size(1, 1);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            return true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            e.Handled = true;
        }
    }

    internal sealed class MacroExportSelectionForm : Form
    {
        private CheckedListBox list;
        public List<int> SelectedIndices = new List<int>();

        public MacroExportSelectionForm(List<MacroDefinition> macros, int currentIndex)
        {
            Text = "选择要导出的宏";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(460, 520);
            MinimumSize = new Size(400, 380);
            Font = new Font("Microsoft YaHei UI", 9F);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;

            Label tip = new Label();
            tip.Text = "勾选一个或多个宏。导出的宏包只包含宏本身，不会改变程序全局设置。";
            tip.Location = new Point(14, 14);
            tip.Size = new Size(410, 42);
            Controls.Add(tip);

            list = new CheckedListBox();
            list.CheckOnClick = true;
            list.Location = new Point(14, 58);
            list.Size = new Size(410, 350);
            list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            if (macros != null)
            {
                for (int i = 0; i < macros.Count; i++)
                {
                    MacroDefinition m = macros[i];
                    list.Items.Add(m == null ? Localizer.T("（空宏）") : m.Name, i == currentIndex);
                }
            }
            Controls.Add(list);

            Button all = new Button();
            all.Text = "全选";
            all.Location = new Point(14, 420);
            all.Size = new Size(70, 30);
            all.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            all.Click += delegate
            {
                for (int i = 0; i < list.Items.Count; i++) list.SetItemChecked(i, true);
            };
            Controls.Add(all);

            Button none = new Button();
            none.Text = "全不选";
            none.Location = new Point(90, 420);
            none.Size = new Size(70, 30);
            none.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            none.Click += delegate
            {
                for (int i = 0; i < list.Items.Count; i++) list.SetItemChecked(i, false);
            };
            Controls.Add(none);

            Button ok = new Button();
            ok.Text = "导出";
            ok.Location = new Point(276, 420);
            ok.Size = new Size(70, 30);
            ok.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            ok.Click += delegate
            {
                SelectedIndices.Clear();
                for (int i = 0; i < list.Items.Count; i++)
                    if (list.GetItemChecked(i)) SelectedIndices.Add(i);
                if (SelectedIndices.Count == 0)
                {
                    LocalizedMessageBox.Show(this, "请至少选择一个宏。", AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(ok);

            Button cancel = new Button();
            cancel.Text = "取消";
            cancel.Location = new Point(354, 420);
            cancel.Size = new Size(70, 30);
            cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);

            AcceptButton = ok;
            CancelButton = cancel;
            Localizer.ApplyStaticControls(this);
        }
    }

    internal sealed class BatchDelayDialog : Form
    {
        private RadioButton fixedMode;
        private RadioButton randomMode;
        private NumericUpDown fixedBox;
        private NumericUpDown minBox;
        private NumericUpDown maxBox;
        public bool UseRandom;
        public int FixedMs;
        public int MinMs;
        public int MaxMs;

        public BatchDelayDialog()
        {
            Text = "批量设置步骤间隔";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(420, 210);
            Font = new Font("Microsoft YaHei UI", 9F);

            fixedMode = new RadioButton();
            fixedMode.Text = "固定间隔";
            fixedMode.Location = new Point(20, 24);
            fixedMode.AutoSize = true;
            fixedMode.Checked = true;
            fixedMode.CheckedChanged += delegate { UpdateEnabled(); };
            Controls.Add(fixedMode);

            fixedBox = MakeNumber(135, 20, 50);
            Controls.Add(fixedBox);
            Controls.Add(MakeLabel("ms", 250, 24));

            randomMode = new RadioButton();
            randomMode.Text = "随机间隔";
            randomMode.Location = new Point(20, 72);
            randomMode.AutoSize = true;
            randomMode.CheckedChanged += delegate { UpdateEnabled(); };
            Controls.Add(randomMode);

            minBox = MakeNumber(135, 68, 45);
            Controls.Add(minBox);
            Controls.Add(MakeLabel("～", 250, 72));
            maxBox = MakeNumber(275, 68, 60);
            Controls.Add(maxBox);
            Controls.Add(MakeLabel("ms", 390, 72));

            Label tip = new Label();
            tip.Text = "仅修改当前选中的步骤，不改变按住时长。";
            tip.Location = new Point(20, 115);
            tip.AutoSize = true;
            Controls.Add(tip);

            Button ok = new Button();
            ok.Text = "确定";
            ok.Location = new Point(230, 155);
            ok.Size = new Size(80, 32);
            ok.Click += delegate
            {
                if (randomMode.Checked && minBox.Value > maxBox.Value)
                {
                    LocalizedMessageBox.Show(this, "随机间隔的最小值不能大于最大值。", AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                UseRandom = randomMode.Checked;
                FixedMs = (int)fixedBox.Value;
                MinMs = (int)minBox.Value;
                MaxMs = (int)maxBox.Value;
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(ok);

            Button cancel = new Button();
            cancel.Text = "取消";
            cancel.Location = new Point(320, 155);
            cancel.Size = new Size(80, 32);
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);
            CancelButton = cancel;
            UpdateEnabled();
            Localizer.ApplyStaticControls(this);
        }

        private NumericUpDown MakeNumber(int x, int y, int value)
        {
            NumericUpDown n = new NumericUpDown();
            n.Location = new Point(x, y);
            n.Size = new Size(110, 26);
            n.Minimum = 0;
            n.Maximum = 600000;
            n.Value = value;
            return n;
        }

        private Label MakeLabel(string text, int x, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.Location = new Point(x, y);
            l.AutoSize = true;
            return l;
        }

        private void UpdateEnabled()
        {
            fixedBox.Enabled = fixedMode.Checked;
            minBox.Enabled = randomMode.Checked;
            maxBox.Enabled = randomMode.Checked;
        }
    }

    internal sealed class DiagnosticsForm : Form
    {
        private TextBox text;
        private Func<string> provider;
        private System.Windows.Forms.Timer timer;

        public DiagnosticsForm(Func<string> diagnosticsProvider)
        {
            provider = diagnosticsProvider;
            Text = AppInfo.ProductName + " - 诊断信息";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(650, 520);
            MinimumSize = new Size(520, 380);
            Font = new Font("Microsoft YaHei UI", 9F);

            text = new TextBox();
            text.Multiline = true;
            text.ReadOnly = true;
            text.ScrollBars = ScrollBars.Both;
            text.WordWrap = false;
            text.Font = new Font("Consolas", 9F);
            text.Location = new Point(12, 12);
            text.Size = new Size(610, 410);
            text.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(text);

            Button copy = new Button();
            copy.Text = "复制";
            copy.Location = new Point(452, 435);
            copy.Size = new Size(80, 32);
            copy.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            copy.Click += delegate
            {
                try { Clipboard.SetText(text.Text ?? ""); } catch { }
            };
            Controls.Add(copy);

            Button close = new Button();
            close.Text = "关闭";
            close.Location = new Point(542, 435);
            close.Size = new Size(80, 32);
            close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            close.Click += delegate { Close(); };
            Controls.Add(close);

            Localizer.ApplyStaticControls(this);
            RefreshText();
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 500;
            timer.Tick += delegate { RefreshText(); };
            timer.Start();
        }

        private void RefreshText()
        {
            try
            {
                int sel = text.SelectionStart;
                string value = Localizer.Dynamic(provider == null ? "" : provider());
                if (text.Text != value) text.Text = value;
                if (sel <= text.TextLength) text.SelectionStart = sel;
            }
            catch { }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (timer != null) { timer.Stop(); timer.Dispose(); timer = null; }
            base.OnFormClosed(e);
        }
    }

    internal sealed class AboutForm : Form
    {
        public AboutForm()
        {
            Text = "关于 " + AppInfo.ProductName;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(520, 310);
            Font = new Font("Microsoft YaHei UI", 9F);

            Label title = new Label();
            title.Text = AppInfo.ProductName + " " + AppInfo.Version;
            title.Font = new Font(Font.FontFamily, 16F, FontStyle.Bold);
            title.Location = new Point(24, 22);
            title.AutoSize = true;
            Controls.Add(title);

            Label body = new Label();
            body.Text = "轻量级 Windows 可视化键鼠宏工具\r\n专注精确时序、游戏场景下的可靠控制与安全停止。\r\n\r\n支持键盘、鼠标侧键、滚轮、扫描码、按住运行、宏录制、宏包与配置方案。\r\n\r\n配置目录：\r\n" + AppPaths.Root;
            body.Location = new Point(26, 72);
            body.Size = new Size(465, 170);
            Controls.Add(body);

            Button open = new Button();
            open.Text = "打开配置目录";
            open.Location = new Point(270, 255);
            open.Size = new Size(120, 32);
            open.Click += delegate
            {
                try { Process.Start("explorer.exe", AppPaths.Root); } catch { }
            };
            Controls.Add(open);

            Button close = new Button();
            close.Text = "关闭";
            close.Location = new Point(400, 255);
            close.Size = new Size(90, 32);
            close.Click += delegate { Close(); };
            Controls.Add(close);
            Localizer.ApplyStaticControls(this);
        }
    }

    public class GearButton : Button
    {
        private readonly Font gearFont = new Font("Segoe UI Symbol", 13F, FontStyle.Regular, GraphicsUnit.Point);

        public GearButton()
        {
            Text = "";
            FlatStyle = FlatStyle.Flat;
            BackColor = Color.White;
            FlatAppearance.BorderColor = Color.FromArgb(190, 199, 211);
            FlatAppearance.MouseOverBackColor = Color.FromArgb(236, 242, 250);
            FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 231, 245);
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);
            TextRenderer.DrawText(pevent.Graphics, "⚙", gearFont, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) gearFont.Dispose();
            base.Dispose(disposing);
        }
    }

    public class SettingsDialog : Form
    {
        private readonly CheckBox scanCodeBox;
        private readonly CheckBox uiSafetyBox;
        private readonly CheckBox trayBox;
        private readonly CheckBox topMostBox;
        private readonly CheckBox activateTargetBox;
        private readonly NumericUpDown delayBox;
        private readonly CheckBox autoProfileBox;
        private readonly ComboBox languageBox;
        private readonly ComboBox updateModeBox;

        public event EventHandler CheckForUpdatesRequested;

        public bool UseScanCodeInput { get { return scanCodeBox.Checked; } }
        public bool PauseMacroInRiskyUi { get { return uiSafetyBox.Checked; } }
        public bool MinimizeToTray { get { return trayBox.Checked; } }
        public bool KeepWindowTopMost { get { return topMostBox.Checked; } }
        public bool ActivateTargetWindowOnUiRun { get { return activateTargetBox.Checked; } }
        public int UiRunStartDelayMs { get { return (int)delayBox.Value; } }
        public bool AutoSwitchProfiles { get { return autoProfileBox.Checked; } }
        public string SelectedLanguage { get { return languageBox.SelectedIndex == 1 ? Localizer.English : Localizer.Chinese; } }
        public string SelectedUpdateMode
        {
            get
            {
                if (updateModeBox.SelectedIndex == 1) return UpdateModes.Manual;
                if (updateModeBox.SelectedIndex == 2) return UpdateModes.Disabled;
                return UpdateModes.Automatic;
            }
        }

        public SettingsDialog(MacroConfig config)
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            Text = Localizer.T("设置");
            Font = new Font("Microsoft YaHei UI", 9F);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = false;
            MinimumSize = new Size(560, 700);
            ClientSize = new Size(660, 760);
            BackColor = Color.FromArgb(247, 249, 252);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(18);
            root.ColumnCount = 1;
            root.RowCount = 6;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            Label hint = new Label();
            hint.Text = Localizer.T("这些选项通常保持默认即可。修改会在点击确定后生效。");
            hint.AutoSize = true;
            hint.MaximumSize = new Size(540, 0);
            hint.ForeColor = Color.FromArgb(86, 96, 112);
            hint.Margin = new Padding(3, 0, 3, 14);
            root.Controls.Add(hint, 0, 0);

            GroupBox general = MakeGroup(Localizer.T("常规"));
            TableLayoutPanel generalLayout = MakeStack();
            general.Controls.Add(generalLayout);
            languageBox = new ComboBox();
            languageBox.DropDownStyle = ComboBoxStyle.DropDownList;
            languageBox.Items.Add("简体中文");
            languageBox.Items.Add("English");
            languageBox.SelectedIndex = string.Equals(config.Language, Localizer.English, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            TableLayoutPanel languageRow = new TableLayoutPanel();
            languageRow.AutoSize = true;
            languageRow.Dock = DockStyle.Top;
            languageRow.ColumnCount = 2;
            languageRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            languageRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            Label languageLabel = new Label();
            languageLabel.Text = Localizer.T("界面语言：");
            languageLabel.AutoSize = true;
            languageLabel.Anchor = AnchorStyles.Left;
            languageLabel.Margin = new Padding(0, 5, 12, 5);
            languageBox.Width = 190;
            languageBox.Anchor = AnchorStyles.Left;
            languageRow.Controls.Add(languageLabel, 0, 0);
            languageRow.Controls.Add(languageBox, 1, 0);
            generalLayout.Controls.Add(languageRow);
            trayBox = MakeCheck(Localizer.T("最小化到系统托盘"), config.MinimizeToTray);
            topMostBox = MakeCheck(Localizer.T("保持主窗口置顶"), config.KeepWindowTopMost);
            generalLayout.Controls.Add(trayBox);
            generalLayout.Controls.Add(topMostBox);
            root.Controls.Add(general, 0, 1);

            GroupBox input = MakeGroup(Localizer.T("输入与安全"));
            TableLayoutPanel inputLayout = MakeStack();
            input.Controls.Add(inputLayout);
            scanCodeBox = MakeCheck(Localizer.T("使用扫描码发送键盘输入（推荐游戏）"), config.UseScanCodeInput);
            uiSafetyBox = MakeCheck(Localizer.T("在编辑界面时暂停宏输出（推荐）"), config.PauseMacroInRiskyUi);
            inputLayout.Controls.Add(scanCodeBox);
            inputLayout.Controls.Add(uiSafetyBox);
            root.Controls.Add(input, 0, 2);

            GroupBox target = MakeGroup(Localizer.T("目标窗口与方案"));
            TableLayoutPanel targetLayout = MakeStack();
            target.Controls.Add(targetLayout);
            activateTargetBox = MakeCheck(Localizer.T("从界面运行时先切换到目标窗口"), config.ActivateTargetWindowOnUiRun);
            targetLayout.Controls.Add(activateTargetBox);
            TableLayoutPanel delayRow = new TableLayoutPanel();
            delayRow.AutoSize = true;
            delayRow.Dock = DockStyle.Top;
            delayRow.ColumnCount = 3;
            delayRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            delayRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            delayRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            Label delayLabel = new Label();
            delayLabel.Text = Localizer.T("切换后等待：");
            delayLabel.AutoSize = true;
            delayLabel.Anchor = AnchorStyles.Left;
            delayLabel.Margin = new Padding(24, 5, 12, 5);
            delayBox = new NumericUpDown();
            delayBox.Minimum = 0;
            delayBox.Maximum = 5000;
            delayBox.Increment = 50;
            delayBox.Value = Math.Max(delayBox.Minimum, Math.Min(delayBox.Maximum, config.UiRunStartDelayMs));
            delayBox.Width = 100;
            delayBox.Anchor = AnchorStyles.Left;
            Label ms = new Label();
            ms.Text = "ms";
            ms.AutoSize = true;
            ms.Anchor = AnchorStyles.Left;
            ms.Margin = new Padding(8, 5, 0, 5);
            delayRow.Controls.Add(delayLabel, 0, 0);
            delayRow.Controls.Add(delayBox, 1, 0);
            delayRow.Controls.Add(ms, 2, 0);
            targetLayout.Controls.Add(delayRow);
            autoProfileBox = MakeCheck(Localizer.T("按前台程序自动切换已绑定方案"), config.AutoSwitchProfiles);
            targetLayout.Controls.Add(autoProfileBox);
            root.Controls.Add(target, 0, 3);

            GroupBox updates = MakeGroup(Localizer.T("软件更新"));
            TableLayoutPanel updatesLayout = MakeStack();
            updates.Controls.Add(updatesLayout);
            TableLayoutPanel updateRow = new TableLayoutPanel();
            updateRow.AutoSize = true;
            updateRow.Dock = DockStyle.Top;
            updateRow.ColumnCount = 2;
            updateRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            updateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            Label updateLabel = new Label();
            updateLabel.Text = Localizer.T("更新方式：");
            updateLabel.AutoSize = true;
            updateLabel.Anchor = AnchorStyles.Left;
            updateLabel.Margin = new Padding(0, 5, 12, 5);
            updateModeBox = new ComboBox();
            updateModeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            updateModeBox.Dock = DockStyle.Fill;
            updateModeBox.Items.Add(Localizer.T("启动时自动检查并提示安装"));
            updateModeBox.Items.Add(Localizer.T("仅手动检查（推荐）"));
            updateModeBox.Items.Add(Localizer.T("不检查更新"));
            updateModeBox.SelectedIndex = string.Equals(config.UpdateMode, UpdateModes.Disabled, StringComparison.OrdinalIgnoreCase) ? 2 :
                (string.Equals(config.UpdateMode, UpdateModes.Manual, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
            updateRow.Controls.Add(updateLabel, 0, 0);
            updateRow.Controls.Add(updateModeBox, 1, 0);
            updatesLayout.Controls.Add(updateRow);
            Label updateHint = new Label();
            updateHint.Text = Localizer.T("网络访问仅用于从官方 GitHub Release 检查和下载更新；下载后必须通过 SHA-256 校验。");
            updateHint.AutoSize = true;
            updateHint.MaximumSize = new Size(570, 0);
            updateHint.ForeColor = Color.FromArgb(86, 96, 112);
            updateHint.Margin = new Padding(0, 5, 0, 8);
            updatesLayout.Controls.Add(updateHint);
            Button checkNow = MakeDialogButton(Localizer.T("立即检查更新"));
            checkNow.Margin = new Padding(0, 2, 0, 2);
            checkNow.Click += delegate
            {
                EventHandler handler = CheckForUpdatesRequested;
                if (handler != null) handler(this, EventArgs.Empty);
            };
            updatesLayout.Controls.Add(checkNow);
            root.Controls.Add(updates, 0, 4);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.WrapContents = false;
            Button ok = MakeDialogButton(Localizer.T("确定"));
            ok.DialogResult = DialogResult.OK;
            Button cancel = MakeDialogButton(Localizer.T("取消"));
            cancel.DialogResult = DialogResult.Cancel;
            Button defaults = MakeDialogButton(Localizer.T("恢复默认"));
            defaults.Click += delegate
            {
                scanCodeBox.Checked = true;
                uiSafetyBox.Checked = true;
                trayBox.Checked = true;
                topMostBox.Checked = false;
                activateTargetBox.Checked = false;
                delayBox.Value = 300;
                autoProfileBox.Checked = false;
                updateModeBox.SelectedIndex = 0;
            };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(defaults);
            root.Controls.Add(buttons, 0, 5);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        private static GroupBox MakeGroup(string text)
        {
            GroupBox box = new GroupBox();
            box.Text = text;
            box.Dock = DockStyle.Top;
            box.AutoSize = true;
            box.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            box.Padding = new Padding(14, 10, 14, 12);
            box.Margin = new Padding(0, 0, 0, 12);
            box.BackColor = Color.White;
            return box;
        }

        private static TableLayoutPanel MakeStack()
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Top;
            panel.AutoSize = true;
            panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel.ColumnCount = 1;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            return panel;
        }

        private static CheckBox MakeCheck(string text, bool value)
        {
            CheckBox box = new CheckBox();
            box.Text = text;
            box.Checked = value;
            box.AutoSize = true;
            box.Margin = new Padding(0, 5, 0, 5);
            return box;
        }

        private static Button MakeDialogButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.AutoSize = true;
            button.MinimumSize = new Size(96, 34);
            button.Padding = new Padding(8, 0, 8, 0);
            return button;
        }
    }

    public class MainForm : Form
    {
        private MacroConfig config;
        private string appDir;
        private string configPath;
        private string packagesDir;
        private string profilesDir;
        private HookManager hooks;
        private string startupWarning = "";
        private bool updateCheckBusy;

        private ListBox macroList;
        private TextBox nameBox;
        private TextBox descriptionBox;
        private CheckBox enabledBox;
        private TextBox triggerBox;
        private Button captureTriggerButton;
        private ComboBox triggerModeBox;
        private CheckBox suppressBox;
        private CheckBox infiniteBox;
        private NumericUpDown repeatBox;
        private TextBox targetWindowBox;
        private Button lockTargetButton;
        private Button clearTargetButton;
        private GroupBox targetGroup;
        private DataGridView grid;
        private Button runButton;
        private Button recordButton;
        private Button toolsButton;
        private InputSinkControl inputSink;
        private Label statusLabel;
        private Label panicHintLabel;
        private ToolTip uiToolTip;
        private ContextMenuStrip toolsMenu;
        private ToolStripMenuItem settingsMenuItem;
        private ToolStripMenuItem panicMenuItem;
        private ToolStripMenuItem suspendTriggersMenuItem;
        private ToolStripMenuItem minimizeToTrayMenuItem;
        private ToolStripMenuItem diagnosticsMenuItem;
        private ToolStripMenuItem openConfigFolderMenuItem;
        private ToolStripMenuItem openLogMenuItem;
        private ToolStripMenuItem aboutMenuItem;
        private ToolStripMenuItem languageMenuItem;
        private ToolStripMenuItem chineseLanguageMenuItem;
        private ToolStripMenuItem englishLanguageMenuItem;
        private ToolStripMenuItem trayShowMenuItem;
        private ToolStripMenuItem trayPanicMenuItem;
        private ToolStripMenuItem traySuspendMenuItem;
        private ToolStripMenuItem trayExitMenuItem;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private readonly Dictionary<Control, string> localizableControlTexts = new Dictionary<Control, string>();
        private readonly Dictionary<Control, string> toolTipSourceTexts = new Dictionary<Control, string>();
        private bool translatingStatusText;
        private readonly Dictionary<Control, string> uiSafetyControls = new Dictionary<Control, string>();
        private volatile bool uiSafetyPauseRequested;
        private volatile string uiSafetyPauseReason = "";
        private int uiSafetyModalDepth;

        private bool loadingUi;
        private bool pauseHotkeys;
        private bool manualTriggerSuspend;
        private enum CaptureMode { None, Trigger, StepInput, PanicTrigger }
        private CaptureMode captureMode = CaptureMode.None;
        private Action<InputSpec> stepCaptureCallback;
        private MacroDefinition nameEditingMacro;
        private System.Windows.Forms.Timer foregroundTimer;
        private readonly List<IntPtr> recentExternalForegrounds = new List<IntPtr>();
        private IntPtr lastObservedForeground = IntPtr.Zero;
        private const int ForegroundHistoryLimit = 16;
        private const int PhysicalReleaseSettleMs = 60;
        private int lastLoggedInputStateRepairCount;
        private int dangerousShortcutAvoidanceCount;
        private IntPtr targetWindowHandle = IntPtr.Zero;
        private int targetResolveTicks = 0;
        private string activeProfilePath = "";
        private string lastAutoProfileProcess = "";
        private bool autoProfileSwitchBusy;

        // Macro recorder. It records physical keyboard/mouse button/wheel events only while an
        // external application owns foreground focus; InputStitch UI clicks are intentionally ignored.
        private readonly object recordingLock = new object();
        private readonly List<RecordedInputEvent> recordedEvents = new List<RecordedInputEvent>();
        private readonly HashSet<string> recordingInputsDown = new HashSet<string>();
        private Stopwatch recordingStopwatch;
        private volatile bool recordingActive;
        private MacroDefinition recordingTargetMacro;
        private bool recordingReplaceMode;

        private Thread workerThread;
        private ManualResetEventSlim stopEvent;
        private MacroDefinition runningMacro;
        private volatile MacroDefinition holdControlledMacro;
        private readonly object runLock = new object();
        private long runSequence = 0;
        private long activeRunId = 0;
        private volatile int activeIteration;
        private volatile int activeStepIndex;
        private volatile int activeStepCount;
        private volatile string activeHeldText = "无";
        private readonly Dictionary<string, InputSpec> activeHeldInputs = new Dictionary<string, InputSpec>();

        public MainForm()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            Text = AppInfo.ProductName + " " + AppInfo.Version;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 600);
            Size = new Size(1180, 760);
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(244, 247, 251);
            try
            {
                Icon associated = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (associated != null) Icon = associated;
            }
            catch { }

            AppPaths.EnsureDirectories();
            UpdateManager.CleanupDownloads();
            appDir = AppPaths.Root;
            packagesDir = AppPaths.MacroPackages;
            profilesDir = AppPaths.Profiles;
            configPath = AppPaths.Config;
            config = LoadConfig();
            Localizer.SetLanguage(config.Language);
            Text = AppInfo.ProductName + " " + AppInfo.Version + " - " + Localizer.T("键鼠宏工具");
            EnsureDefaultMacro();

            BuildUi();
            BuildTrayIcon();
            ResolveTargetWindowFromConfig();
            RefreshTargetWindowUi();
            RefreshMacroList(0);
            InputSender.UseScanCodeInput = config.UseScanCodeInput;

            IntPtr initialForeground = NativeWindowFocus.ForegroundWindow();
            lastObservedForeground = initialForeground;
            RememberExternalForeground(initialForeground);

            foregroundTimer = new System.Windows.Forms.Timer();
            foregroundTimer.Interval = 50;
            foregroundTimer.Tick += ForegroundTimer_Tick;
            foregroundTimer.Start();
            Activated += delegate { ReconcileHookState("main-window-activated"); UpdateUiSafetyPauseState(); };
            Deactivate += delegate { ReconcileHookState("main-window-deactivated"); UpdateUiSafetyPauseState(); };
            Resize += MainForm_Resize;

            try
            {
                hooks = new HookManager();
                hooks.OnTerminalInput = HandleTerminalInput;
                hooks.OnTerminalInputReleased = HandleTerminalInputReleased;
                hooks.ShouldReportModifierInput = delegate { return captureMode == CaptureMode.StepInput || recordingActive; };
            }
            catch (Exception ex)
            {
                AppLog.Write("Hook installation failed", ex);
                LocalizedMessageBox.Show(this, ex.Message, AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            FormClosing += MainForm_FormClosing;
            Shown += delegate
            {
                if (!config.HasSeenWelcome)
                {
                    LocalizedMessageBox.Show(this,
                        Localizer.T("欢迎使用 ") + AppInfo.ProductName + " " + AppInfo.Version + ".\r\n\r\n" +
                        Localizer.T("默认紧急停止键：") + InputNames.FormatTrigger(config.PanicTrigger) + ".\r\n" +
                        Localizer.T("宏录制只记录键盘、鼠标按钮和滚轮，不记录鼠标移动。"),
                        AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    config.HasSeenWelcome = true;
                    SaveConfig();
                }
                if (!string.IsNullOrWhiteSpace(startupWarning))
                {
                    LocalizedMessageBox.Show(this, startupWarning, AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    startupWarning = "";
                }
                if (string.Equals(config.UpdateMode, UpdateModes.Automatic, StringComparison.OrdinalIgnoreCase))
                    BeginInvoke((MethodInvoker)delegate { CheckForUpdatesAsync(this, true); });
            };
        }

        private void BuildUi()
        {
            uiToolTip = new ToolTip();
            uiToolTip.InitialDelay = 450;
            uiToolTip.ReshowDelay = 120;
            uiToolTip.AutoPopDelay = 12000;
            uiToolTip.ShowAlways = true;
            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.FixedPanel = FixedPanel.Panel1;
            split.SplitterWidth = 6;
            split.BackColor = Color.FromArgb(220, 226, 235);
            split.Panel1.BackColor = Color.FromArgb(247, 249, 252);
            split.Panel2.BackColor = Color.FromArgb(244, 247, 251);
            Controls.Add(split);
            split.Panel1MinSize = 220;
            split.Panel2MinSize = 600;
            split.SplitterDistance = 260;

            TableLayoutPanel leftRoot = new TableLayoutPanel();
            leftRoot.Dock = DockStyle.Fill;
            leftRoot.Padding = new Padding(12);
            leftRoot.ColumnCount = 1;
            leftRoot.RowCount = 4;
            leftRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            leftRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            split.Panel1.Controls.Add(leftRoot);

            TableLayoutPanel leftHeader = new TableLayoutPanel();
            leftHeader.Dock = DockStyle.Fill;
            leftHeader.AutoSize = true;
            leftHeader.ColumnCount = 2;
            leftHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            leftHeader.Margin = new Padding(0, 0, 0, 10);
            Label leftTitle = new Label();
            leftTitle.Text = "宏列表";
            leftTitle.Font = new Font(Font.FontFamily, 11F, FontStyle.Bold);
            leftTitle.AutoSize = true;
            leftTitle.Anchor = AnchorStyles.Left;
            toolsButton = new GearButton();
            toolsButton.Size = new Size(36, 36);
            toolsButton.Margin = new Padding(6, 0, 0, 0);
            toolsButton.AccessibleName = Localizer.T("设置");
            BuildToolsMenu();
            toolsButton.Click += delegate { if (toolsMenu != null) toolsMenu.Show(toolsButton, new Point(0, toolsButton.Height)); };
            leftHeader.Controls.Add(leftTitle, 0, 0);
            leftHeader.Controls.Add(toolsButton, 1, 0);
            leftRoot.Controls.Add(leftHeader, 0, 0);

            TableLayoutPanel packageButtons = new TableLayoutPanel();
            packageButtons.Dock = DockStyle.Fill;
            packageButtons.AutoSize = true;
            packageButtons.ColumnCount = 2;
            packageButtons.RowCount = 2;
            packageButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            packageButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            packageButtons.Margin = new Padding(0, 0, 0, 10);
            Button importConfigButton = MakeButton("导入宏", 0, 0, 0);
            Button exportConfigButton = MakeButton("导出宏", 0, 0, 0);
            Button loadProfileButton = MakeButton("载入方案", 0, 0, 0);
            Button saveProfileButton = MakeButton("保存方案", 0, 0, 0);
            foreach (Button b in new Button[] { importConfigButton, exportConfigButton, loadProfileButton, saveProfileButton })
            {
                b.Dock = DockStyle.Fill;
                b.AutoSize = true;
                b.MinimumSize = new Size(0, 34);
            }
            importConfigButton.Margin = new Padding(0, 0, 3, 3);
            exportConfigButton.Margin = new Padding(3, 0, 0, 3);
            loadProfileButton.Margin = new Padding(0, 3, 3, 0);
            saveProfileButton.Margin = new Padding(3, 3, 0, 0);
            importConfigButton.Click += ImportConfigButton_Click;
            exportConfigButton.Click += ExportConfigButton_Click;
            loadProfileButton.Click += LoadProfileButton_Click;
            saveProfileButton.Click += SaveProfileButton_Click;
            packageButtons.Controls.Add(importConfigButton, 0, 0);
            packageButtons.Controls.Add(exportConfigButton, 1, 0);
            packageButtons.Controls.Add(loadProfileButton, 0, 1);
            packageButtons.Controls.Add(saveProfileButton, 1, 1);
            leftRoot.Controls.Add(packageButtons, 0, 1);

            macroList = new ListBox();
            macroList.Dock = DockStyle.Fill;
            macroList.IntegralHeight = false;
            macroList.BorderStyle = BorderStyle.FixedSingle;
            macroList.Margin = new Padding(0, 0, 0, 10);
            macroList.SelectedIndexChanged += MacroList_SelectedIndexChanged;
            leftRoot.Controls.Add(macroList, 0, 2);

            FlowLayoutPanel macroButtons = new FlowLayoutPanel();
            macroButtons.Dock = DockStyle.Fill;
            macroButtons.AutoSize = true;
            macroButtons.WrapContents = true;
            macroButtons.Margin = new Padding(0);
            Button addMacro = MakeButton("新增", 0, 0, 0);
            Button copyMacro = MakeButton("复制", 0, 0, 0);
            Button delMacro = MakeButton("删除", 0, 0, 0);
            Button upMacro = MakeButton("↑", 0, 0, 0);
            Button downMacro = MakeButton("↓", 0, 0, 0);
            foreach (Button b in new Button[] { addMacro, copyMacro, delMacro }) { b.AutoSize = true; b.MinimumSize = new Size(54, 32); }
            upMacro.Size = downMacro.Size = new Size(34, 32);
            addMacro.Click += AddMacro_Click;
            copyMacro.Click += CopyMacro_Click;
            delMacro.Click += DeleteMacro_Click;
            upMacro.Click += delegate { MoveSelectedMacro(-1); };
            downMacro.Click += delegate { MoveSelectedMacro(1); };
            macroButtons.Controls.AddRange(new Control[] { addMacro, copyMacro, delMacro, upMacro, downMacro });
            leftRoot.Controls.Add(macroButtons, 0, 3);

            Panel rightHost = new Panel();
            rightHost.Dock = DockStyle.Fill;
            rightHost.AutoScroll = true;
            rightHost.BackColor = Color.FromArgb(244, 247, 251);
            split.Panel2.Controls.Add(rightHost);

            TableLayoutPanel rightRoot = new TableLayoutPanel();
            rightRoot.Dock = DockStyle.Top;
            rightRoot.AutoSize = true;
            rightRoot.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            rightRoot.Padding = new Padding(14, 12, 14, 12);
            rightRoot.ColumnCount = 1;
            rightRoot.RowCount = 4;
            rightRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rightRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rightHost.Controls.Add(rightRoot);
            EventHandler resizeRightContent = delegate
            {
                int width = Math.Max(560, rightHost.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
                int height = Math.Max(650, rightHost.ClientSize.Height);
                rightRoot.MinimumSize = new Size(width, height);
            };
            rightHost.Resize += resizeRightContent;
            Shown += delegate { resizeRightContent(rightHost, EventArgs.Empty); };

            GroupBox detailsGroup = MakeMainGroup("宏信息");
            TableLayoutPanel details = MakeFormGrid();
            detailsGroup.Controls.Add(details);
            Label nameLabel = MakeFieldLabel("名称：");
            nameBox = new TextBox();
            nameBox.Dock = DockStyle.Fill;
            nameBox.TextChanged += NameBox_TextChanged;
            nameBox.Leave += NameBox_Leave;
            enabledBox = new CheckBox();
            enabledBox.Text = "启用全局触发";
            enabledBox.AutoSize = true;
            enabledBox.Anchor = AnchorStyles.Left;
            enabledBox.Margin = new Padding(12, 4, 0, 4);
            enabledBox.CheckedChanged += Settings_Changed;
            details.Controls.Add(nameLabel, 0, 0);
            details.Controls.Add(nameBox, 1, 0);
            details.Controls.Add(enabledBox, 2, 0);
            Label descLabel = MakeFieldLabel("备注：");
            descriptionBox = new TextBox();
            descriptionBox.Dock = DockStyle.Fill;
            descriptionBox.TextChanged += DescriptionBox_TextChanged;
            descriptionBox.Leave += delegate { if (!loadingUi) SaveConfig(); };
            details.Controls.Add(descLabel, 0, 1);
            details.Controls.Add(descriptionBox, 1, 1);
            details.SetColumnSpan(descriptionBox, 2);
            rightRoot.Controls.Add(detailsGroup, 0, 0);

            GroupBox triggerGroup = MakeMainGroup("触发与循环");
            TableLayoutPanel triggerLayout = MakeFormGrid();
            triggerGroup.Controls.Add(triggerLayout);

            FlowLayoutPanel triggerTop = new FlowLayoutPanel();
            triggerTop.Dock = DockStyle.Fill;
            triggerTop.AutoSize = true;
            triggerTop.WrapContents = true;
            triggerTop.FlowDirection = FlowDirection.LeftToRight;
            triggerTop.Margin = new Padding(0, 0, 0, 2);

            TableLayoutPanel triggerKeyLine = new TableLayoutPanel();
            triggerKeyLine.Height = 34;
            triggerKeyLine.Margin = new Padding(0, 0, 8, 0);
            triggerKeyLine.ColumnCount = 3;
            triggerKeyLine.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
            triggerKeyLine.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            triggerKeyLine.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            Label triggerLabel = MakeFieldLabel("触发键：");
            triggerBox = new TextBox();
            triggerBox.Dock = DockStyle.Fill;
            triggerBox.Margin = new Padding(0, 4, 8, 3);
            triggerBox.ReadOnly = true;
            captureTriggerButton = MakeButton(Localizer.T("录制触发键"), 0, 0, 0);
            captureTriggerButton.AutoSize = true;
            captureTriggerButton.MinimumSize = new Size(128, 32);
            captureTriggerButton.Margin = new Padding(0);
            captureTriggerButton.Click += CaptureTriggerButton_Click;
            triggerKeyLine.Controls.Add(triggerLabel, 0, 0);
            triggerKeyLine.Controls.Add(triggerBox, 1, 0);
            triggerKeyLine.Controls.Add(captureTriggerButton, 2, 0);

            TableLayoutPanel triggerModeLine = new TableLayoutPanel();
            triggerModeLine.Height = 34;
            triggerModeLine.Margin = new Padding(0);
            triggerModeLine.ColumnCount = 2;
            triggerModeLine.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108F));
            triggerModeLine.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            Label triggerModeLabel = MakeFieldLabel("触发方式：");
            triggerModeBox = new ComboBox();
            triggerModeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            triggerModeBox.Dock = DockStyle.Fill;
            triggerModeBox.Margin = new Padding(0, 3, 0, 3);
            triggerModeBox.Items.AddRange(new object[] { Localizer.T("按一次切换启动/停止"), Localizer.T("按住运行，松开停止") });
            triggerModeBox.SelectedIndexChanged += TriggerModeBox_SelectedIndexChanged;
            triggerModeLine.Controls.Add(triggerModeLabel, 0, 0);
            triggerModeLine.Controls.Add(triggerModeBox, 1, 0);
            triggerTop.Controls.Add(triggerKeyLine);
            triggerTop.Controls.Add(triggerModeLine);
            triggerLayout.Controls.Add(triggerTop, 0, 0);
            triggerLayout.SetColumnSpan(triggerTop, 3);
            EventHandler resizeTriggerTop = delegate
            {
                int available = triggerLayout.ClientSize.Width;
                if (available <= 0) return;
                if (available >= 720)
                {
                    int first = Math.Max(350, (available - 8) * 52 / 100);
                    triggerKeyLine.Width = first;
                    triggerModeLine.Width = Math.Max(300, available - first - 8);
                }
                else
                {
                    int lineWidth = Math.Max(350, available - 4);
                    triggerKeyLine.Width = lineWidth;
                    triggerModeLine.Width = lineWidth;
                }
            };
            triggerLayout.Resize += resizeTriggerTop;
            Shown += delegate { resizeTriggerTop(triggerLayout, EventArgs.Empty); };

            Label repeatLabel = MakeFieldLabel("执行次数：");
            repeatBox = new NumericUpDown();
            repeatBox.Minimum = 1;
            repeatBox.Maximum = 100000000;
            repeatBox.Value = 1;
            repeatBox.Width = 110;
            repeatBox.Anchor = AnchorStyles.Left;
            repeatBox.ValueChanged += Settings_Changed;
            infiniteBox = new CheckBox();
            infiniteBox.Text = "无限循环（再次触发或点击停止）";
            infiniteBox.AutoSize = true;
            infiniteBox.Anchor = AnchorStyles.Left;
            infiniteBox.Margin = new Padding(12, 4, 0, 4);
            infiniteBox.CheckedChanged += InfiniteBox_CheckedChanged;
            triggerLayout.Controls.Add(repeatLabel, 0, 1);
            triggerLayout.Controls.Add(repeatBox, 1, 1);
            triggerLayout.Controls.Add(infiniteBox, 2, 1);
            suppressBox = new CheckBox();
            suppressBox.Text = "触发时屏蔽最后一个触发键/鼠标事件";
            suppressBox.AutoSize = true;
            suppressBox.Anchor = AnchorStyles.Left;
            suppressBox.CheckedChanged += Settings_Changed;
            triggerLayout.Controls.Add(suppressBox, 1, 2);
            triggerLayout.SetColumnSpan(suppressBox, 2);
            rightRoot.Controls.Add(triggerGroup, 0, 1);

            targetGroup = MakeMainGroup("目标窗口");
            TableLayoutPanel targetLayout = MakeFormGrid();
            targetGroup.Controls.Add(targetLayout);
            Label targetLabel = MakeFieldLabel("目标窗口：");
            targetWindowBox = new TextBox();
            targetWindowBox.Dock = DockStyle.Fill;
            targetWindowBox.ReadOnly = true;
            targetWindowBox.TabStop = false;
            FlowLayoutPanel targetButtons = new FlowLayoutPanel();
            targetButtons.AutoSize = true;
            targetButtons.WrapContents = false;
            targetButtons.Margin = new Padding(8, 0, 0, 0);
            lockTargetButton = MakeButton("锁定刚才的目标窗口", 0, 0, 0);
            lockTargetButton.AutoSize = true;
            lockTargetButton.MinimumSize = new Size(165, 32);
            lockTargetButton.Click += LockTargetButton_Click;
            clearTargetButton = MakeButton("清除目标", 0, 0, 0);
            clearTargetButton.AutoSize = true;
            clearTargetButton.MinimumSize = new Size(96, 32);
            clearTargetButton.Click += ClearTargetButton_Click;
            targetButtons.Controls.Add(lockTargetButton);
            targetButtons.Controls.Add(clearTargetButton);
            targetLayout.Controls.Add(targetLabel, 0, 0);
            targetLayout.Controls.Add(targetWindowBox, 1, 0);
            targetLayout.Controls.Add(targetButtons, 2, 0);
            rightRoot.Controls.Add(targetGroup, 0, 2);

            GroupBox stepsGroup = MakeMainGroup("执行步骤");
            stepsGroup.Dock = DockStyle.Fill;
            stepsGroup.AutoSize = false;
            stepsGroup.MinimumSize = new Size(0, 300);
            TableLayoutPanel stepsLayout = new TableLayoutPanel();
            stepsLayout.Dock = DockStyle.Fill;
            stepsLayout.ColumnCount = 1;
            stepsLayout.RowCount = 3;
            stepsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            stepsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stepsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            stepsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            stepsGroup.Controls.Add(stepsLayout);
            TableLayoutPanel stepsHeader = new TableLayoutPanel();
            stepsHeader.Dock = DockStyle.Fill;
            stepsHeader.AutoSize = true;
            stepsHeader.ColumnCount = 2;
            stepsHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            stepsHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            FlowLayoutPanel primaryActions = new FlowLayoutPanel();
            primaryActions.AutoSize = true;
            primaryActions.WrapContents = false;
            primaryActions.FlowDirection = FlowDirection.RightToLeft;
            primaryActions.Margin = new Padding(0);

            runButton = MakeButton("▶ 执行所选宏", 0, 0, 0);
            runButton.AutoSize = true;
            runButton.MinimumSize = new Size(210, 38);
            runButton.Margin = new Padding(8, 0, 0, 2);
            runButton.Font = new Font(Font, FontStyle.Bold);
            runButton.FlatStyle = FlatStyle.Flat;
            runButton.FlatAppearance.BorderSize = 1;
            runButton.FlatAppearance.BorderColor = SystemColors.ControlDark;
            runButton.FlatAppearance.MouseOverBackColor = SystemColors.ControlLight;
            runButton.FlatAppearance.MouseDownBackColor = SystemColors.ControlLight;
            runButton.BackColor = SystemColors.Control;
            runButton.UseVisualStyleBackColor = false;
            runButton.TabStop = false;
            runButton.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            };
            runButton.Click += RunButton_Click;

            recordButton = MakeButton("● 录制宏", 0, 0, 0);
            recordButton.AutoSize = true;
            recordButton.MinimumSize = new Size(142, 38);
            recordButton.Margin = new Padding(0, 0, 0, 2);
            recordButton.Font = new Font(Font, FontStyle.Bold);
            recordButton.FlatStyle = FlatStyle.Flat;
            recordButton.FlatAppearance.BorderSize = 1;
            recordButton.FlatAppearance.BorderColor = SystemColors.ControlDark;
            recordButton.FlatAppearance.MouseOverBackColor = SystemColors.ControlLight;
            recordButton.FlatAppearance.MouseDownBackColor = SystemColors.ControlLight;
            recordButton.BackColor = SystemColors.Control;
            recordButton.UseVisualStyleBackColor = false;
            recordButton.Click += RecordButton_Click;
            primaryActions.Controls.Add(runButton);
            primaryActions.Controls.Add(recordButton);
            stepsHeader.Controls.Add(primaryActions, 1, 0);
            stepsLayout.Controls.Add(stepsHeader, 0, 0);

            grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.MinimumSize = new Size(0, 130);
            grid.Margin = new Padding(0, 4, 0, 6);
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = true;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.Columns.Add("Index", "#");
            grid.Columns.Add("Action", "操作");
            grid.Columns.Add("Input", "按键 / 鼠标");
            grid.Columns.Add("Hold", "按住时长 (ms)");
            grid.Columns.Add("Delay", "步骤后间隔 (ms)");
            grid.Columns[0].Width = 45;
            grid.Columns[0].MinimumWidth = 40;
            grid.Columns[1].Width = 125;
            grid.Columns[1].MinimumWidth = 105;
            grid.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns[2].MinimumWidth = 160;
            grid.Columns[3].Width = 130;
            grid.Columns[3].MinimumWidth = 115;
            grid.Columns[4].Width = 150;
            grid.Columns[4].MinimumWidth = 130;
            grid.CellDoubleClick += delegate { EditSelectedStep(); };
            stepsLayout.Controls.Add(grid, 0, 1);

            FlowLayoutPanel stepFooter = new FlowLayoutPanel();
            stepFooter.Dock = DockStyle.Fill;
            stepFooter.AutoSize = false;
            stepFooter.WrapContents = false;
            stepFooter.AutoScroll = true;
            Button addStep = MakeButton("添加", 0, 0, 0);
            Button editStep = MakeButton("编辑", 0, 0, 0);
            Button copyStep = MakeButton("复制步骤", 0, 0, 0);
            Button delStep = MakeButton("删除", 0, 0, 0);
            Button upStep = MakeButton("上移", 0, 0, 0);
            Button downStep = MakeButton("下移", 0, 0, 0);
            Button batchDelayStep = MakeButton("批量间隔", 0, 0, 0);
            foreach (Button b in new Button[] { addStep, editStep, copyStep, delStep, upStep, downStep, batchDelayStep })
            {
                b.AutoSize = true;
                b.MinimumSize = new Size(66, 32);
                b.Margin = new Padding(0, 0, 6, 4);
            }
            addStep.Click += delegate { AddStep(); };
            editStep.Click += delegate { EditSelectedStep(); };
            copyStep.Click += delegate { CopySelectedSteps(); };
            delStep.Click += delegate { DeleteSelectedStep(); };
            upStep.Click += delegate { MoveSelectedStep(-1); };
            downStep.Click += delegate { MoveSelectedStep(1); };
            batchDelayStep.Click += delegate { BatchEditSelectedStepDelay(); };
            stepFooter.Controls.AddRange(new Control[] { addStep, editStep, copyStep, delStep, upStep, downStep, batchDelayStep });
            stepsLayout.Controls.Add(stepFooter, 0, 2);
            rightRoot.Controls.Add(stepsGroup, 0, 3);

            SetTip(importConfigButton, "导入一个或多个宏包，把其中的宏追加到当前列表末尾，不覆盖已有宏。" );
            SetTip(exportConfigButton, "选择一个或多个宏导出为宏包，适合分享、备份或合并到其他列表。" );
            SetTip(loadProfileButton, "载入一套完整配置方案。方案会替换当前宏列表和相关设置，载入前自动备份。" );
            SetTip(saveProfileButton, "把当前宏列表和程序设置保存为一套方案；可选择绑定目标进程以供自动切换。" );
            SetTip(toolsButton, "打开安全、诊断、托盘和关于选项。" );
            SetTip(upMacro, "将当前宏在列表中上移一位。" );
            SetTip(downMacro, "将当前宏在列表中下移一位。" );

            SetTip(enabledBox, "控制这个宏是否响应全局触发键。关闭后仍可用 UI 按钮执行。" );
            SetTip(descriptionBox, "给宏添加简短用途说明。备注会随宏包一起导出。" );
            SetTip(infiniteBox, "持续重复整个宏，直到再次触发、松开按住型触发键、点击停止或使用紧急停止键。" );
            SetTip(repeatBox, "宏完整执行的次数。勾选无限循环后忽略此数值。" );
            SetTip(triggerModeBox, "选择按一次切换启动/停止，或按住触发键运行、松开立即停止。" );
            SetTip(captureTriggerButton, "点击后按下希望使用的键盘键、鼠标按钮或滚轮方向来设置触发方式。" );
            SetTip(suppressBox, "触发宏时阻止最后一个实际按键或鼠标事件继续传给当前程序；其他输入不受影响。" );
            SetTip(targetWindowBox, "当前锁定的目标窗口。用于 UI 自动切换和保存方案时的进程绑定。" );
            SetTip(lockTargetButton, "先切到目标程序，再切回 InputStitch 后点击这里；程序会锁定最近有效的外部前台窗口。" );
            SetTip(clearTargetButton, "清除当前目标窗口，不删除任何宏。" );
            SetTip(recordButton, "录制物理键盘、鼠标按钮和滚轮输入，自动换算为宏步骤；不记录鼠标移动。" );
            SetTip(copyStep, "复制当前选中的一个或多个步骤，并插入到所选步骤之后。" );
            SetTip(batchDelayStep, "批量设置所选步骤的固定间隔或简单随机间隔范围。" );
            SetTip(runButton, "开始执行当前宏；有宏正在运行时用于停止当前宏。" );

            RegisterUiSafetyControl(importConfigButton, "导入宏");
            RegisterUiSafetyControl(exportConfigButton, "导出宏");
            RegisterUiSafetyControl(loadProfileButton, "载入配置方案");
            RegisterUiSafetyControl(saveProfileButton, "保存配置方案");
            RegisterUiSafetyControl(toolsButton, "打开工具菜单");
            RegisterUiSafetyControl(macroList, "选择宏");
            RegisterUiSafetyControl(addMacro, "新增宏");
            RegisterUiSafetyControl(copyMacro, "复制宏");
            RegisterUiSafetyControl(delMacro, "删除宏");
            RegisterUiSafetyControl(upMacro, "调整宏列表顺序");
            RegisterUiSafetyControl(downMacro, "调整宏列表顺序");
            RegisterUiSafetyControl(nameBox, "编辑名称");
            RegisterUiSafetyControl(descriptionBox, "编辑备注");
            RegisterUiSafetyControl(enabledBox, "修改全局触发设置");
            RegisterUiSafetyControl(triggerBox, "触发键设置");
            RegisterUiSafetyControl(captureTriggerButton, "录制触发键");
            RegisterUiSafetyControl(triggerModeBox, "修改触发方式");
            RegisterUiSafetyControl(suppressBox, "修改触发屏蔽设置");
            RegisterUiSafetyControl(repeatBox, "修改执行次数");
            RegisterUiSafetyControl(infiniteBox, "修改循环设置");
            RegisterUiSafetyControl(lockTargetButton, "锁定目标窗口");
            RegisterUiSafetyControl(clearTargetButton, "清除目标窗口");
            RegisterUiSafetyControl(grid, "编辑宏步骤");
            RegisterUiSafetyControl(addStep, "添加宏步骤");
            RegisterUiSafetyControl(editStep, "编辑宏步骤");
            RegisterUiSafetyControl(copyStep, "复制宏步骤");
            RegisterUiSafetyControl(delStep, "删除宏步骤");
            RegisterUiSafetyControl(upStep, "调整宏步骤顺序");
            RegisterUiSafetyControl(downStep, "调整宏步骤顺序");
            RegisterUiSafetyControl(batchDelayStep, "批量修改步骤间隔");

            inputSink = new InputSinkControl();
            inputSink.Size = new Size(1, 1);
            split.Panel2.Controls.Add(inputSink);

            MouseEventHandler safeBackgroundClick = delegate
            {
                try { inputSink.Focus(); } catch { }
                UpdateUiSafetyPauseState();
            };
            split.Panel2.MouseDown += safeBackgroundClick;
            rightHost.MouseDown += safeBackgroundClick;
            rightRoot.MouseDown += safeBackgroundClick;
            split.Panel1.MouseDown += safeBackgroundClick;

            Panel bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 62;
            bottom.BackColor = Color.White;
            bottom.MouseDown += safeBackgroundClick;
            Controls.Add(bottom);
            bottom.SendToBack();

            TableLayoutPanel statusLayout = new TableLayoutPanel();
            statusLayout.Dock = DockStyle.Fill;
            statusLayout.Padding = new Padding(14, 7, 14, 6);
            statusLayout.ColumnCount = 1;
            statusLayout.RowCount = 2;
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            bottom.Controls.Add(statusLayout);

            statusLabel = new Label();
            statusLabel.Text = "状态：空闲";
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.AutoEllipsis = true;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.Font = new Font(Font, FontStyle.Bold);
            statusLabel.TextChanged += StatusLabel_TextChanged;
            statusLayout.Controls.Add(statusLabel, 0, 0);

            panicHintLabel = new Label();
            panicHintLabel.Dock = DockStyle.Fill;
            panicHintLabel.AutoEllipsis = true;
            panicHintLabel.TextAlign = ContentAlignment.MiddleLeft;
            panicHintLabel.ForeColor = SystemColors.GrayText;
            statusLayout.Controls.Add(panicHintLabel, 0, 1);
            RefreshPanicUi();
            UpdateTargetSectionVisibility();
            CaptureLocalizableControlTexts(this);
            ApplyLanguageToMainUi(false);
            ApplyStatusVisuals();
        }

        private void CaptureLocalizableControlTexts(Control root)
        {
            if (root == null) return;
            foreach (Control child in root.Controls)
            {
                bool isStaticTextControl = child is Label || child is Button || child is CheckBox || child is RadioButton || child is GroupBox;
                bool isDynamic = child == statusLabel || child == panicHintLabel || child == runButton || child == recordButton || child == infiniteBox || child == captureTriggerButton;
                if (isStaticTextControl && !isDynamic && !localizableControlTexts.ContainsKey(child))
                    localizableControlTexts.Add(child, child.Text ?? "");
                CaptureLocalizableControlTexts(child);
            }
        }

        private void ApplyLanguageToMainUi(bool userInitiated)
        {
            Text = AppInfo.ProductName + " " + AppInfo.Version + " - " + Localizer.T("键鼠宏工具");
            if (toolsButton != null) toolsButton.AccessibleName = Localizer.T("设置");

            foreach (KeyValuePair<Control, string> pair in localizableControlTexts)
            {
                try { if (pair.Key != null && !pair.Key.IsDisposed) pair.Key.Text = Localizer.T(pair.Value); }
                catch { }
            }

            int triggerModeIndex = triggerModeBox == null ? -1 : triggerModeBox.SelectedIndex;
            if (triggerModeBox != null)
            {
                loadingUi = true;
                triggerModeBox.Items.Clear();
                triggerModeBox.Items.Add(Localizer.T("按一次切换启动/停止"));
                triggerModeBox.Items.Add(Localizer.T("按住运行，松开停止"));
                if (triggerModeIndex >= 0 && triggerModeIndex < triggerModeBox.Items.Count) triggerModeBox.SelectedIndex = triggerModeIndex;
                loadingUi = false;
            }

            if (grid != null && grid.Columns.Count >= 5)
            {
                grid.Columns[0].HeaderText = "#";
                grid.Columns[1].HeaderText = Localizer.T("操作");
                grid.Columns[2].HeaderText = Localizer.T("按键 / 鼠标");
                grid.Columns[3].HeaderText = Localizer.T("按住时长 (ms)");
                grid.Columns[4].HeaderText = Localizer.T("步骤后间隔 (ms)");
            }

            RefreshMenuLanguage();
            RefreshToolTipsLanguage();
            RefreshTargetWindowUi();
            UpdateTriggerModeUiText();
            RefreshSteps();
            RefreshPanicUi();
            UpdateRunButton();
            UpdateRecordButton();

            int selected = macroList == null ? -1 : macroList.SelectedIndex;
            if (macroList != null && config != null && config.Macros != null) RefreshMacroList(selected);
            RefreshStatusForLanguage();

            if (userInitiated)
            {
                config.Language = Localizer.Language;
                SaveConfig();
            }
        }

        private void RefreshStatusForLanguage()
        {
            if (statusLabel == null) return;
            bool alive = false;
            string macroName = "";
            lock (runLock)
            {
                alive = workerThread != null && workerThread.IsAlive;
                macroName = runningMacro == null ? "" : runningMacro.Name;
            }
            if (recordingActive)
                statusLabel.Text = Localizer.Dynamic("录制中：切到目标程序进行操作；回到 InputStitch 后点击“停止录制”。");
            else if (alive)
                statusLabel.Text = Localizer.Dynamic("正在执行：" + macroName);
            else if (manualTriggerSuspend)
                statusLabel.Text = Localizer.Dynamic("状态：全局宏触发已暂停；紧急停止键仍有效。");
            else
                statusLabel.Text = Localizer.Dynamic("状态：空闲");
        }

        private void ChangeLanguage(string language)
        {
            string normalized = string.Equals(language, Localizer.English, StringComparison.OrdinalIgnoreCase) ? Localizer.English : Localizer.Chinese;
            if (string.Equals(Localizer.Language, normalized, StringComparison.OrdinalIgnoreCase)) return;
            Localizer.SetLanguage(normalized);
            if (config != null) config.Language = normalized;
            ApplyLanguageToMainUi(true);
        }

        private void RefreshMenuLanguage()
        {
            if (settingsMenuItem != null) settingsMenuItem.Text = Localizer.T("设置...");
            if (suspendTriggersMenuItem != null) suspendTriggersMenuItem.Text = Localizer.T("暂停全局宏触发");
            if (minimizeToTrayMenuItem != null) minimizeToTrayMenuItem.Text = Localizer.T("最小化到系统托盘");
            if (languageMenuItem != null) languageMenuItem.Text = Localizer.T("语言 / Language");
            if (chineseLanguageMenuItem != null)
            {
                chineseLanguageMenuItem.Text = Localizer.IsEnglish ? "Simplified Chinese" : "简体中文";
                chineseLanguageMenuItem.Checked = !Localizer.IsEnglish;
            }
            if (englishLanguageMenuItem != null)
            {
                englishLanguageMenuItem.Text = "English";
                englishLanguageMenuItem.Checked = Localizer.IsEnglish;
            }
            if (diagnosticsMenuItem != null) diagnosticsMenuItem.Text = Localizer.T("诊断信息...");
            if (openConfigFolderMenuItem != null) openConfigFolderMenuItem.Text = Localizer.T("打开配置文件夹");
            if (openLogMenuItem != null) openLogMenuItem.Text = Localizer.T("打开日志文件夹");
            if (aboutMenuItem != null) aboutMenuItem.Text = Localizer.Dynamic("关于 " + AppInfo.ProductName + "...");
            if (trayShowMenuItem != null) trayShowMenuItem.Text = Localizer.Dynamic("显示 " + AppInfo.ProductName);
            if (trayPanicMenuItem != null) trayPanicMenuItem.Text = Localizer.T("紧急停止");
            if (traySuspendMenuItem != null) traySuspendMenuItem.Text = Localizer.T("暂停全局宏触发");
            if (trayExitMenuItem != null) trayExitMenuItem.Text = Localizer.T("退出");
            if (panicMenuItem != null && captureMode != CaptureMode.PanicTrigger)
                panicMenuItem.Text = Localizer.Dynamic("设置紧急停止键...（当前：" + InputNames.FormatTrigger(config == null ? null : config.PanicTrigger) + "）");
        }

        private void RefreshToolTipsLanguage()
        {
            if (uiToolTip == null) return;
            foreach (KeyValuePair<Control, string> pair in toolTipSourceTexts)
            {
                try
                {
                    if (pair.Key != null && !pair.Key.IsDisposed)
                        uiToolTip.SetToolTip(pair.Key, WrapToolTip(Localizer.T(pair.Value), Localizer.IsEnglish ? 440 : 360));
                }
                catch { }
            }
        }

        private void StatusLabel_TextChanged(object sender, EventArgs e)
        {
            if (statusLabel == null) return;
            if (!translatingStatusText && Localizer.IsEnglish)
            {
                string translated = Localizer.Dynamic(statusLabel.Text ?? "");
                if (!string.Equals(translated, statusLabel.Text, StringComparison.Ordinal))
                {
                    translatingStatusText = true;
                    statusLabel.Text = translated;
                    translatingStatusText = false;
                }
            }
            ApplyStatusVisuals();
        }

        private void BuildToolsMenu()
        {
            toolsMenu = new ContextMenuStrip();
            settingsMenuItem = new ToolStripMenuItem();
            settingsMenuItem.Click += delegate { ShowSettingsDialog(); };
            toolsMenu.Items.Add(settingsMenuItem);
            toolsMenu.Items.Add(new ToolStripSeparator());

            panicMenuItem = new ToolStripMenuItem();
            panicMenuItem.Click += delegate { BeginPanicTriggerCapture(); };
            toolsMenu.Items.Add(panicMenuItem);

            suspendTriggersMenuItem = new ToolStripMenuItem();
            suspendTriggersMenuItem.CheckOnClick = true;
            suspendTriggersMenuItem.CheckedChanged += delegate
            {
                manualTriggerSuspend = suspendTriggersMenuItem.Checked;
                UpdateTrayMenuChecks();
                statusLabel.Text = manualTriggerSuspend ? "状态：全局宏触发已暂停；紧急停止键仍有效。" : "状态：全局宏触发已恢复。";
            };
            toolsMenu.Items.Add(suspendTriggersMenuItem);

            minimizeToTrayMenuItem = new ToolStripMenuItem();
            minimizeToTrayMenuItem.CheckOnClick = true;
            minimizeToTrayMenuItem.Checked = config.MinimizeToTray;
            minimizeToTrayMenuItem.CheckedChanged += delegate
            {
                if (loadingUi) return;
                config.MinimizeToTray = minimizeToTrayMenuItem.Checked;
                SaveConfig();
            };
            toolsMenu.Items.Add(minimizeToTrayMenuItem);

            languageMenuItem = new ToolStripMenuItem();
            chineseLanguageMenuItem = new ToolStripMenuItem("简体中文");
            englishLanguageMenuItem = new ToolStripMenuItem("English");
            chineseLanguageMenuItem.Click += delegate { ChangeLanguage(Localizer.Chinese); };
            englishLanguageMenuItem.Click += delegate { ChangeLanguage(Localizer.English); };
            languageMenuItem.DropDownItems.Add(chineseLanguageMenuItem);
            languageMenuItem.DropDownItems.Add(englishLanguageMenuItem);
            toolsMenu.Items.Add(languageMenuItem);

            toolsMenu.Items.Add(new ToolStripSeparator());

            diagnosticsMenuItem = new ToolStripMenuItem();
            diagnosticsMenuItem.Click += delegate
            {
                uiSafetyModalDepth++;
                UpdateUiSafetyPauseState();
                try
                {
                    using (DiagnosticsForm f = new DiagnosticsForm(BuildDiagnosticsText)) f.ShowDialog(this);
                }
                finally
                {
                    uiSafetyModalDepth = Math.Max(0, uiSafetyModalDepth - 1);
                    UpdateUiSafetyPauseState();
                }
            };
            toolsMenu.Items.Add(diagnosticsMenuItem);

            openConfigFolderMenuItem = new ToolStripMenuItem();
            openConfigFolderMenuItem.Click += OpenConfigFolderButton_Click;
            toolsMenu.Items.Add(openConfigFolderMenuItem);

            openLogMenuItem = new ToolStripMenuItem();
            openLogMenuItem.Click += delegate
            {
                try
                {
                    Directory.CreateDirectory(AppPaths.Logs);
                    Process.Start("explorer.exe", AppPaths.Logs);
                }
                catch (Exception ex) { AppLog.Write("Open log folder failed", ex); }
            };
            toolsMenu.Items.Add(openLogMenuItem);

            aboutMenuItem = new ToolStripMenuItem();
            aboutMenuItem.Click += delegate
            {
                uiSafetyModalDepth++;
                UpdateUiSafetyPauseState();
                try { using (AboutForm f = new AboutForm()) f.ShowDialog(this); }
                finally
                {
                    uiSafetyModalDepth = Math.Max(0, uiSafetyModalDepth - 1);
                    UpdateUiSafetyPauseState();
                }
            };
            toolsMenu.Items.Add(new ToolStripSeparator());
            toolsMenu.Items.Add(aboutMenuItem);
            toolsMenu.Opening += delegate { RefreshPanicUi(); RefreshMenuLanguage(); };
            RefreshMenuLanguage();
        }

        private void ShowSettingsDialog()
        {
            uiSafetyModalDepth++;
            UpdateUiSafetyPauseState();
            try
            {
                using (SettingsDialog dialog = new SettingsDialog(config))
                {
                    dialog.CheckForUpdatesRequested += delegate { CheckForUpdatesAsync(dialog, false); };
                    if (dialog.ShowDialog(this) != DialogResult.OK) return;
                    bool autoProfilesWasEnabled = config.AutoSwitchProfiles;
                    config.UseScanCodeInput = dialog.UseScanCodeInput;
                    config.PauseMacroInRiskyUi = dialog.PauseMacroInRiskyUi;
                    config.MinimizeToTray = dialog.MinimizeToTray;
                    config.KeepWindowTopMost = dialog.KeepWindowTopMost;
                    config.ActivateTargetWindowOnUiRun = dialog.ActivateTargetWindowOnUiRun;
                    config.UiRunStartDelayMs = dialog.UiRunStartDelayMs;
                    config.AutoSwitchProfiles = dialog.AutoSwitchProfiles;
                    config.UpdateMode = dialog.SelectedUpdateMode;
                    InputSender.UseScanCodeInput = config.UseScanCodeInput;
                    TopMost = config.KeepWindowTopMost;
                    if (autoProfilesWasEnabled && !config.AutoSwitchProfiles) lastAutoProfileProcess = "";
                    UpdateTargetSectionVisibility();
                    if (minimizeToTrayMenuItem != null)
                    {
                        loadingUi = true;
                        minimizeToTrayMenuItem.Checked = config.MinimizeToTray;
                        loadingUi = false;
                    }
                    if (!string.Equals(config.Language, dialog.SelectedLanguage, StringComparison.OrdinalIgnoreCase))
                    {
                        config.Language = dialog.SelectedLanguage;
                        Localizer.SetLanguage(config.Language);
                        ApplyLanguageToMainUi(false);
                    }
                    UpdateUiSafetyPauseState();
                    SaveConfig();
                }
            }
            finally
            {
                uiSafetyModalDepth = Math.Max(0, uiSafetyModalDepth - 1);
                UpdateUiSafetyPauseState();
            }
        }

        private async void CheckForUpdatesAsync(IWin32Window owner, bool automatic)
        {
            if (updateCheckBusy) return;
            updateCheckBusy = true;
            string previousStatus = statusLabel == null ? "" : statusLabel.Text;
            try
            {
                if (statusLabel != null) statusLabel.Text = Localizer.T("正在检查更新…");
                UpdateCheckResult update = await UpdateManager.CheckAsync();
                if (!update.IsAvailable)
                {
                    if (!automatic)
                        LocalizedMessageBox.Show(owner, Localizer.T("已是最新版本。"), AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string prompt = Localizer.T("当前公开版本仍为 1.0.0，但 GitHub 上已有更新的安全构建。是否下载并安装？") +
                    "\r\n\r\n" + (Localizer.IsEnglish ? "Architecture: " : "架构：") + update.Asset.Architecture +
                    "\r\n" + (Localizer.IsEnglish ? "Release: " : "发布版本：") + update.Manifest.Version;
                DialogResult choice = LocalizedMessageBox.Show(owner, prompt, Localizer.T("发现可用更新"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (choice != DialogResult.Yes) return;

                if (statusLabel != null) statusLabel.Text = Localizer.T("正在下载并验证更新…");
                string downloaded = await UpdateManager.DownloadAsync(update);
                EmergencyStop("software-update");
                SaveConfig();
                UpdateManager.BeginInstall(downloaded, update.Asset.Sha256);
                Form ownerForm = owner as Form;
                if (ownerForm != null && ownerForm != this && !ownerForm.IsDisposed) ownerForm.Close();
                Close();
            }
            catch (Exception ex)
            {
                AppLog.Write("Update check or installation preparation failed", ex);
                if (!automatic)
                    LocalizedMessageBox.Show(owner, Localizer.T("检查更新失败。") + "\r\n\r\n" + ex.Message,
                        AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                updateCheckBusy = false;
                if (statusLabel != null && !statusLabel.IsDisposed && !IsDisposed && !Disposing)
                    statusLabel.Text = previousStatus;
            }
        }

        private void UpdateTargetSectionVisibility()
        {
            if (targetGroup == null || config == null) return;
            bool shouldShow = config.ActivateTargetWindowOnUiRun || config.AutoSwitchProfiles;
            // Assign unconditionally: before the form is shown, Control.Visible reports false
            // when an ancestor is hidden even if the control's own visibility flag is still true.
            targetGroup.Visible = shouldShow;
        }

        private void BuildTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayShowMenuItem = new ToolStripMenuItem();
            trayShowMenuItem.Click += delegate { RestoreFromTray(); };
            trayMenu.Items.Add(trayShowMenuItem);

            trayPanicMenuItem = new ToolStripMenuItem();
            trayPanicMenuItem.Click += delegate { EmergencyStop("托盘紧急停止"); };
            trayMenu.Items.Add(trayPanicMenuItem);

            traySuspendMenuItem = new ToolStripMenuItem();
            traySuspendMenuItem.CheckOnClick = true;
            traySuspendMenuItem.Tag = "suspend";
            traySuspendMenuItem.CheckedChanged += delegate
            {
                if (traySuspendMenuItem.Checked == manualTriggerSuspend) return;
                manualTriggerSuspend = traySuspendMenuItem.Checked;
                if (suspendTriggersMenuItem != null) suspendTriggersMenuItem.Checked = manualTriggerSuspend;
            };
            trayMenu.Items.Add(traySuspendMenuItem);
            trayMenu.Items.Add(new ToolStripSeparator());

            trayExitMenuItem = new ToolStripMenuItem();
            trayExitMenuItem.Click += delegate { Close(); };
            trayMenu.Items.Add(trayExitMenuItem);

            trayIcon = new NotifyIcon();
            try
            {
                trayIcon.Icon = Icon != null ? Icon : Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }
            trayIcon.Text = AppInfo.ProductName + " " + AppInfo.Version;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += delegate { RestoreFromTray(); };
            trayMenu.Opening += delegate { UpdateTrayMenuChecks(); RefreshMenuLanguage(); };
            RefreshMenuLanguage();
        }

        private void UpdateTrayMenuChecks()
        {
            if (trayMenu == null) return;
            foreach (ToolStripItem item in trayMenu.Items)
            {
                ToolStripMenuItem mi = item as ToolStripMenuItem;
                if (mi != null && object.Equals(mi.Tag, "suspend")) mi.Checked = manualTriggerSuspend;
            }
            if (suspendTriggersMenuItem != null && suspendTriggersMenuItem.Checked != manualTriggerSuspend)
                suspendTriggersMenuItem.Checked = manualTriggerSuspend;
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized && config != null && config.MinimizeToTray)
            {
                BeginInvoke((MethodInvoker)delegate { Hide(); });
            }
        }

        private void RestoreFromTray()
        {
            try
            {
                Show();
                WindowState = FormWindowState.Normal;
                Activate();
            }
            catch { }
        }

        private void BeginPanicTriggerCapture()
        {
            if (captureMode == CaptureMode.PanicTrigger)
            {
                CancelCapture();
                return;
            }
            captureMode = CaptureMode.PanicTrigger;
            if (panicMenuItem != null) panicMenuItem.Text = Localizer.T("请按新的紧急停止键…");
            statusLabel.Text = "状态：正在录制紧急停止键（按 Esc 取消）";
            UpdateUiSafetyPauseState();
        }

        private void RefreshPanicUi()
        {
            string text = InputNames.FormatTrigger(config == null ? null : config.PanicTrigger);
            bool conflict = false;
            if (config != null && config.PanicTrigger != null && config.Macros != null)
            {
                foreach (MacroDefinition m in config.Macros)
                {
                    if (m != null && m.Enabled && m.Trigger != null && TriggersEqual(m.Trigger, config.PanicTrigger))
                    {
                        conflict = true;
                        break;
                    }
                }
            }
            if (panicHintLabel != null)
            {
                panicHintLabel.Text = Localizer.IsEnglish
                    ? "Emergency Stop: " + text + (conflict ? "  ⚠ Conflicts with a macro trigger" : "") + "  |  Double-click the tray icon to restore"
                    : "紧急停止：" + text + (conflict ? "　⚠ 与宏触发键冲突" : "") + "　|　双击托盘图标可恢复窗口";
                panicHintLabel.ForeColor = conflict ? Color.DarkOrange : SystemColors.GrayText;
            }
            if (panicMenuItem != null && captureMode != CaptureMode.PanicTrigger) panicMenuItem.Text = Localizer.IsEnglish ? "Set Emergency Stop hotkey... (current: " + text + ")" : "设置紧急停止键...（当前：" + text + "）";
        }

        private void EmergencyStop(string reason)
        {
            try
            {
                if (recordingActive) StopMacroRecording(false, true);
                CancelCapture();
                Thread t = null;
                lock (runLock)
                {
                    if (stopEvent != null) stopEvent.Set();
                    t = workerThread;
                }
                ForceReleaseActiveHeldInputs();
                ReconcileHookState("emergency-stop");
                if (t != null && t.IsAlive)
                    statusLabel.Text = "紧急停止：已发送停止信号并释放宏按住的输入。";
                else
                    statusLabel.Text = "紧急停止：当前没有正在执行的宏。";
                UpdateRunButton();
                AppLog.Write("Emergency stop: " + (reason ?? "unknown"));
            }
            catch (Exception ex)
            {
                AppLog.Write("Emergency stop failed", ex);
            }
        }

        private void ForceReleaseActiveHeldInputs()
        {
            List<InputSpec> release = new List<InputSpec>();
            lock (runLock)
            {
                foreach (KeyValuePair<string, InputSpec> pair in activeHeldInputs)
                    if (pair.Value != null) release.Add(pair.Value.Clone());
                activeHeldInputs.Clear();
                activeHeldText = "无";
            }
            foreach (InputSpec input in release)
            {
                try { InputSender.SendUp(input); } catch (Exception ex) { AppLog.Write("Emergency key release failed", ex); }
            }
        }

        private string BuildDiagnosticsText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(AppInfo.ProductName + " " + AppInfo.Version);
            sb.AppendLine("ConfigFormat: " + (config == null ? "?" : config.FormatVersion));
            sb.AppendLine("ConfigPath: " + configPath);
            sb.AppendLine("LogPath: " + AppLog.LogPath);
            sb.AppendLine("Hooks: " + (hooks == null ? Localizer.T("未安装") : Localizer.T("已安装")));
            sb.AppendLine("ScanCodeInput: " + (config != null && config.UseScanCodeInput ? Localizer.T("开启") : (Localizer.IsEnglish ? "Off" : "关闭")));
            sb.AppendLine("PanicTrigger: " + InputNames.FormatTrigger(config == null ? null : config.PanicTrigger));
            sb.AppendLine("GlobalTriggersSuspended: " + (manualTriggerSuspend ? Localizer.T("是") : Localizer.T("否")));
            sb.AppendLine("UIProtectionPause: " + (uiSafetyPauseRequested ? Localizer.T("是") + " - " + Localizer.Dynamic(uiSafetyPauseReason) : Localizer.T("否")));
            sb.AppendLine("PhysicalModifiers: " + (hooks == null ? Localizer.T("未知") : hooks.PhysicalModifierText));
            sb.AppendLine("PhysicalInputEvents: " + (hooks == null ? "0" : hooks.PhysicalEventCount.ToString()));
            sb.AppendLine("StaleInputStateRepairs: " + (hooks == null ? "0" : hooks.StaleStateRepairCount.ToString()));
            sb.AppendLine("AvoidedPhysicalShortcutConflicts: " + Volatile.Read(ref dangerousShortcutAvoidanceCount).ToString());
            sb.AppendLine("Recording: " + (recordingActive ? Localizer.T("是") : Localizer.T("否")));
            sb.AppendLine("Target: " + (targetWindowBox == null ? "" : targetWindowBox.Text));
            sb.AppendLine("AutoProfileSwitch: " + (config != null && config.AutoSwitchProfiles ? Localizer.T("开启") : (Localizer.IsEnglish ? "Off" : "关闭")));
            sb.AppendLine("ActiveProfile: " + (string.IsNullOrWhiteSpace(activeProfilePath) ? Localizer.T("（当前 config.xml）") : activeProfilePath));
            lock (runLock)
            {
                bool alive = workerThread != null && workerThread.IsAlive;
                sb.AppendLine("WorkerAlive: " + (alive ? Localizer.T("是") : Localizer.T("否")));
                sb.AppendLine("RunId: " + activeRunId.ToString());
                sb.AppendLine("RunningMacro: " + (runningMacro == null ? Localizer.T("无") : runningMacro.Name));
                sb.AppendLine("Iteration: " + activeIteration.ToString());
                sb.AppendLine("Step: " + activeStepIndex.ToString() + "/" + activeStepCount.ToString());
                sb.AppendLine("MacroHeldInputs: " + Localizer.T(activeHeldText));
            }
            sb.AppendLine("TriggerConflicts: " + GetTriggerConflictSummary());
            return sb.ToString();
        }

        private void ApplyStatusVisuals()
        {
            if (statusLabel == null) return;
            string text = statusLabel.Text ?? "";
            if (Localizer.ContainsMeaning(text, "紧急停止"))
                statusLabel.ForeColor = Color.Firebrick;
            else if (Localizer.ContainsMeaning(text, "重复的已启用触发键"))
                statusLabel.ForeColor = Color.DarkOrange;
            else if (Localizer.ContainsMeaning(text, "已暂停") || Localizer.ContainsMeaning(text, "等待"))
                statusLabel.ForeColor = Color.DarkOrange;
            else if (Localizer.ContainsMeaning(text, "正在执行"))
                statusLabel.ForeColor = Color.Firebrick;
            else if (Localizer.ContainsMeaning(text, "正在停止"))
                statusLabel.ForeColor = Color.OrangeRed;
            else if (Localizer.ContainsMeaning(text, "空闲") ||
                     Localizer.ContainsMeaning(text, "已导入") ||
                     Localizer.ContainsMeaning(text, "已导出") ||
                     Localizer.ContainsMeaning(text, "已保存") ||
                     Localizer.ContainsMeaning(text, "已载入") ||
                     Localizer.ContainsMeaning(text, "已打开配置文件夹"))
                statusLabel.ForeColor = Color.ForestGreen;
            else if (Localizer.ContainsMeaning(text, "出错") ||
                     Localizer.ContainsMeaning(text, "无法") ||
                     Localizer.ContainsMeaning(text, "失败"))
                statusLabel.ForeColor = Color.Firebrick;
            else if (Localizer.ContainsMeaning(text, "录制") ||
                     Localizer.ContainsMeaning(text, "提示") ||
                     Localizer.ContainsMeaning(text, "开始执行"))
                statusLabel.ForeColor = Color.RoyalBlue;
            else
                statusLabel.ForeColor = SystemColors.ControlText;
        }

        private Button MakeButton(string text, int x, int y, int w)
        {
            Button b = new Button();
            b.Text = text;
            if (w > 0) b.Size = new Size(w, 32);
            else b.Size = new Size(88, 32);
            b.FlatStyle = FlatStyle.System;
            return b;
        }

        private GroupBox MakeMainGroup(string text)
        {
            GroupBox box = new GroupBox();
            box.Text = text;
            box.Dock = DockStyle.Top;
            box.AutoSize = true;
            box.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            box.Padding = new Padding(12, 9, 12, 10);
            box.Margin = new Padding(0, 0, 0, 9);
            box.BackColor = Color.White;
            return box;
        }

        private TableLayoutPanel MakeFormGrid()
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Top;
            panel.AutoSize = true;
            panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel.ColumnCount = 3;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            return panel;
        }

        private Label MakeFieldLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.AutoSize = false;
            label.MinimumSize = new Size(0, 30);
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Margin = new Padding(0, 2, 8, 2);
            return label;
        }

        private void SetTip(Control control, string text)
        {
            if (uiToolTip == null || control == null || string.IsNullOrWhiteSpace(text)) return;
            toolTipSourceTexts[control] = text;
            uiToolTip.SetToolTip(control, WrapToolTip(Localizer.T(text), Localizer.IsEnglish ? 440 : 360));
        }

        private string WrapToolTip(string text, int maxWidthPx)
        {
            if (string.IsNullOrEmpty(text)) return text;
            List<string> lines = new List<string>();
            string[] paragraphs = text.Replace("\r", "").Split('\n');
            foreach (string paragraph in paragraphs)
            {
                if (paragraph.Length == 0)
                {
                    lines.Add("");
                    continue;
                }
                string line = "";
                int lastBreak = -1;
                for (int i = 0; i < paragraph.Length; i++)
                {
                    char ch = paragraph[i];
                    line += ch;
                    if (char.IsWhiteSpace(ch) || "，。；、：！？,.!?:;)/".IndexOf(ch) >= 0) lastBreak = line.Length;
                    if (TextRenderer.MeasureText(line, Font).Width > maxWidthPx)
                    {
                        int cut = lastBreak > 0 && lastBreak < line.Length ? lastBreak : Math.Max(1, line.Length - 1);
                        string head = line.Substring(0, cut).TrimEnd();
                        if (head.Length > 0) lines.Add(head);
                        line = line.Substring(cut).TrimStart();
                        lastBreak = -1;
                    }
                }
                if (line.Length > 0) lines.Add(line);
            }
            return string.Join(Environment.NewLine, lines.ToArray());
        }

        private void RegisterUiSafetyControl(Control control, string reason)
        {
            if (control == null) return;
            uiSafetyControls[control] = reason;
            control.Enter += delegate { UpdateUiSafetyPauseState(); };
            control.MouseEnter += delegate { UpdateUiSafetyPauseState(); };
            // Defer leave recalculation until the next UI-message turn. This avoids a tiny
            // unprotected gap when focus/mouse moves directly from one protected control to another.
            EventHandler deferredUpdate = delegate
            {
                try { BeginInvoke((MethodInvoker)delegate { UpdateUiSafetyPauseState(); }); } catch { }
            };
            control.Leave += deferredUpdate;
            control.MouseLeave += delegate
            {
                try { BeginInvoke((MethodInvoker)delegate { UpdateUiSafetyPauseState(); }); } catch { }
            };
        }

        private void UpdateUiSafetyPauseState()
        {
            bool shouldPause = false;
            string reason = "";
            try
            {
                if (config != null && config.PauseMacroInRiskyUi && NativeWindowFocus.IsCurrentProcessWindow(NativeWindowFocus.ForegroundWindow()))
                {
                    if (uiSafetyModalDepth > 0)
                    {
                        shouldPause = true;
                        reason = "编辑宏步骤";
                    }
                    else if (captureMode != CaptureMode.None)
                    {
                        shouldPause = true;
                        if (captureMode == CaptureMode.Trigger) reason = "录制触发键";
                        else if (captureMode == CaptureMode.PanicTrigger) reason = "录制紧急停止键";
                        else reason = "录制宏步骤按键";
                    }
                    else
                    {
                        foreach (KeyValuePair<Control, string> pair in uiSafetyControls)
                        {
                            Control c = pair.Key;
                            if (c != null && !c.IsDisposed && c.Enabled)
                            {
                                bool focused = c.Focused || c.ContainsFocus;
                                bool hovered = false;
                                try
                                {
                                    Rectangle screenRect = c.RectangleToScreen(c.ClientRectangle);
                                    hovered = screenRect.Contains(Control.MousePosition);
                                }
                                catch { }
                                if (focused || hovered)
                                {
                                    shouldPause = true;
                                    reason = pair.Value;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                shouldPause = false;
                reason = "";
            }
            bool changed = uiSafetyPauseRequested != shouldPause;
            uiSafetyPauseReason = reason;
            uiSafetyPauseRequested = shouldPause;
            if (changed && runButton != null && !runButton.IsDisposed) UpdateRunButton();
        }

        private void ReconcileHookState(string reason)
        {
            HookManager h = hooks;
            if (h == null) return;
            try
            {
                h.ReconcilePhysicalState();
                int total = h.StaleStateRepairCount;
                if (total > lastLoggedInputStateRepairCount)
                {
                    int repaired = total - lastLoggedInputStateRepairCount;
                    lastLoggedInputStateRepairCount = total;
                    AppLog.Write("Repaired stale physical input state: +" + repaired.ToString() +
                        ", total=" + total.ToString() + ", reason=" + (reason ?? "periodic"));
                }
            }
            catch (Exception ex)
            {
                AppLog.Write("Physical input state reconciliation failed", ex);
            }
        }

        private void ForegroundTimer_Tick(object sender, EventArgs e)
        {
            ReconcileHookState("foreground-timer");
            UpdateUiSafetyPauseState();
            IntPtr hwnd = NativeWindowFocus.ForegroundWindow();
            if (hwnd != lastObservedForeground)
            {
                lastObservedForeground = hwnd;
                RememberExternalForeground(hwnd);
                if (recordingActive && NativeWindowFocus.IsCurrentProcessWindow(hwnd))
                    TrimRecordingTailForUiReturn();
                if (!recordingActive && config != null && config.AutoSwitchProfiles && NativeWindowFocus.IsUsableExternalWindow(hwnd) && !NativeWindowFocus.IsLikelyTransientTaskSwitcher(hwnd))
                {
                    TargetWindowIdentity foregroundInfo = NativeWindowFocus.Describe(hwnd);
                    if (foregroundInfo != null && !string.IsNullOrWhiteSpace(foregroundInfo.ProcessName))
                        TryAutoSwitchProfileForProcess(foregroundInfo.ProcessName);
                }
            }

            // A target program may recreate its top-level window after changing display mode,
            // restarting, or loading into another renderer. If our old HWND becomes invalid,
            // resolve it again from the persistent identity instead of silently falling back to
            // whichever unrelated window happened to be foreground most recently.
            if (targetWindowHandle != IntPtr.Zero && !NativeWindowFocus.IsUsableExternalWindow(targetWindowHandle))
            {
                targetWindowHandle = IntPtr.Zero;
                ResolveTargetWindowFromConfig();
                RefreshTargetWindowUi();
            }
            else if (targetWindowHandle == IntPtr.Zero && HasConfiguredTarget())
            {
                // Re-scan roughly once per second so the UI updates automatically if the saved
                // target program is launched after InputStitch. Avoid EnumWindows on every timer tick.
                targetResolveTicks++;
                if (targetResolveTicks >= 20)
                {
                    targetResolveTicks = 0;
                    ResolveTargetWindowFromConfig();
                    if (targetWindowHandle != IntPtr.Zero) RefreshTargetWindowUi();
                }
            }
            else
            {
                targetResolveTicks = 0;
            }
        }

        private void RememberExternalForeground(IntPtr hwnd)
        {
            if (!NativeWindowFocus.IsUsableExternalWindow(hwnd)) return;

            // Move an already-known HWND to the front rather than storing duplicates.
            for (int i = recentExternalForegrounds.Count - 1; i >= 0; i--)
            {
                if (recentExternalForegrounds[i] == hwnd) recentExternalForegrounds.RemoveAt(i);
            }
            recentExternalForegrounds.Insert(0, hwnd);
            while (recentExternalForegrounds.Count > ForegroundHistoryLimit)
                recentExternalForegrounds.RemoveAt(recentExternalForegrounds.Count - 1);
        }

        private IntPtr GetRecentLockableForeground(out int skippedInvalid)
        {
            skippedInvalid = 0;

            // First pass: skip dead entries and known transient Alt+Tab/task-switcher hosts.
            // This is the normal path after returning from a game to InputStitch.
            for (int i = 0; i < recentExternalForegrounds.Count; i++)
            {
                IntPtr hwnd = recentExternalForegrounds[i];
                if (!NativeWindowFocus.IsUsableExternalWindow(hwnd))
                {
                    skippedInvalid++;
                    continue;
                }
                if (NativeWindowFocus.IsLikelyTransientTaskSwitcher(hwnd))
                {
                    skippedInvalid++;
                    continue;
                }
                return hwnd;
            }

            // Extremely conservative fallback: if Windows changes its shell implementation and
            // every surviving entry looks transient to our heuristic, still allow the user to
            // lock the newest valid external window rather than reporting nothing at all.
            for (int i = 0; i < recentExternalForegrounds.Count; i++)
            {
                IntPtr hwnd = recentExternalForegrounds[i];
                if (NativeWindowFocus.IsUsableExternalWindow(hwnd)) return hwnd;
            }
            return IntPtr.Zero;
        }

        private void LockTargetButton_Click(object sender, EventArgs e)
        {
            int skippedInvalid;
            IntPtr hwnd = GetRecentLockableForeground(out skippedInvalid);
            if (hwnd == IntPtr.Zero)
            {
                statusLabel.Text = "状态：没有可锁定的最近外部前台窗口。请先切到目标程序，再切回 InputStitch。";
                return;
            }

            TargetWindowIdentity info = NativeWindowFocus.Describe(hwnd);
            if (info == null)
            {
                statusLabel.Text = "状态：无法读取目标窗口信息。";
                return;
            }

            targetWindowHandle = hwnd;
            config.TargetProcessName = info.ProcessName ?? "";
            config.TargetWindowTitle = info.Title ?? "";
            config.TargetWindowClass = info.ClassName ?? "";
            SaveConfig();
            RefreshTargetWindowUi();
            statusLabel.Text = skippedInvalid > 0
                ? "状态：已跳过临时/已消失的切换窗口并锁定：" + info.DisplayText
                : "状态：已锁定目标窗口：" + info.DisplayText;
        }

        private void ClearTargetButton_Click(object sender, EventArgs e)
        {
            targetWindowHandle = IntPtr.Zero;
            config.TargetProcessName = "";
            config.TargetWindowTitle = "";
            config.TargetWindowClass = "";
            SaveConfig();
            RefreshTargetWindowUi();
            statusLabel.Text = "状态：已清除目标窗口。";
        }

        private bool HasConfiguredTarget()
        {
            return !string.IsNullOrWhiteSpace(config.TargetProcessName)
                || !string.IsNullOrWhiteSpace(config.TargetWindowTitle)
                || !string.IsNullOrWhiteSpace(config.TargetWindowClass);
        }

        private void ResolveTargetWindowFromConfig()
        {
            if (NativeWindowFocus.IsUsableExternalWindow(targetWindowHandle)) return;
            targetWindowHandle = NativeWindowFocus.ResolveConfiguredTarget(
                config.TargetProcessName,
                config.TargetWindowTitle,
                config.TargetWindowClass);
        }

        private IntPtr GetResolvedTargetWindow()
        {
            if (NativeWindowFocus.IsUsableExternalWindow(targetWindowHandle)) return targetWindowHandle;
            ResolveTargetWindowFromConfig();
            return NativeWindowFocus.IsUsableExternalWindow(targetWindowHandle) ? targetWindowHandle : IntPtr.Zero;
        }

        private void RefreshTargetWindowUi()
        {
            if (targetWindowBox == null) return;
            IntPtr hwnd = GetResolvedTargetWindow();
            if (hwnd != IntPtr.Zero)
            {
                TargetWindowIdentity info = NativeWindowFocus.Describe(hwnd);
                targetWindowBox.Text = info == null ? Localizer.T("已锁定（窗口信息读取失败）") : info.DisplayText;
                try { targetWindowBox.SelectionStart = 0; targetWindowBox.SelectionLength = 0; } catch { }
                return;
            }

            if (HasConfiguredTarget())
            {
                string title = string.IsNullOrWhiteSpace(config.TargetWindowTitle) ? Localizer.T("（无窗口标题）") : config.TargetWindowTitle;
                string proc = string.IsNullOrWhiteSpace(config.TargetProcessName) ? Localizer.T("未知进程") : config.TargetProcessName;
                targetWindowBox.Text = Localizer.T("未找到：") + title + "  [" + proc + "]";
            }
            else
            {
                targetWindowBox.Text = Localizer.T("未锁定");
            }
            try { targetWindowBox.SelectionStart = 0; targetWindowBox.SelectionLength = 0; } catch { }
        }

        private MacroDefinition SelectedMacro
        {
            get
            {
                int i = macroList.SelectedIndex;
                if (i < 0 || i >= config.Macros.Count) return null;
                return config.Macros[i];
            }
        }

        private void EnsureDefaultMacro()
        {
            if (config.Macros == null) config.Macros = new List<MacroDefinition>();
            if (config.Macros.Count == 0)
            {
                MacroDefinition m = new MacroDefinition();
                m.Name = Localizer.T("示例：快速按 E");
                m.Trigger = new TriggerSpec();
                m.Trigger.Kind = InputKind.Keyboard;
                m.Trigger.VirtualKey = (int)Keys.F8;
                m.RepeatCount = 10;
                MacroStep s = new MacroStep();
                s.Action = MacroAction.Press;
                s.Kind = InputKind.Keyboard;
                s.VirtualKey = (int)Keys.E;
                s.HoldMs = 30;
                s.DelayMs = 70;
                m.Steps.Add(s);
                config.Macros.Add(m);
                SaveConfig();
            }
        }

        private MacroConfig LoadConfig()
        {
            try
            {
                if (!File.Exists(configPath)) return new MacroConfig();
                XmlSerializer xs = new XmlSerializer(typeof(MacroConfig));
                using (FileStream fs = File.OpenRead(configPath))
                {
                    MacroConfig c = (MacroConfig)xs.Deserialize(fs);
                    if (c == null) c = new MacroConfig();
                    NormalizeConfig(c);
                    return c;
                }
            }
            catch (Exception ex)
            {
                AppLog.Write("Config load failed", ex);
                string backup = "";
                try
                {
                    Directory.CreateDirectory(AppPaths.Backups);
                    backup = Path.Combine(AppPaths.Backups, "config.broken-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".xml");
                    if (File.Exists(configPath)) File.Copy(configPath, backup, true);
                }
                catch { backup = ""; }
                startupWarning = "配置文件无法读取，已使用默认配置。" +
                    (string.IsNullOrWhiteSpace(backup) ? "" : "\r\n原文件已备份到：\r\n" + backup) +
                    "\r\n\r\n详细错误已写入日志。";
                return new MacroConfig();
            }
        }

        private void SaveConfig()
        {
            try
            {
                config.FormatVersion = AppInfo.ConfigFormatVersion;
                Directory.CreateDirectory(appDir);
                XmlSerializer xs = new XmlSerializer(typeof(MacroConfig));
                string temp = configPath + ".tmp";
                using (FileStream fs = File.Create(temp)) xs.Serialize(fs, config);
                if (File.Exists(configPath)) File.Delete(configPath);
                File.Move(temp, configPath);
            }
            catch (Exception ex)
            {
                AppLog.Write("Config save failed", ex);
            }
        }

        private void RefreshMacroList(int selectedIndex)
        {
            loadingUi = true;
            macroList.Items.Clear();
            foreach (MacroDefinition m in config.Macros)
                macroList.Items.Add(FormatMacroListItem(m));
            if (macroList.Items.Count > 0)
            {
                if (selectedIndex < 0) selectedIndex = 0;
                if (selectedIndex >= macroList.Items.Count) selectedIndex = macroList.Items.Count - 1;
                macroList.SelectedIndex = selectedIndex;
            }
            loadingUi = false;
            LoadSelectedMacroToUi();
        }

        private string FormatMacroListItem(MacroDefinition m)
        {
            if (m == null) return Localizer.T("○ （空宏）");
            string prefix = m.Enabled ? "● " : "○ ";
            if (m.Enabled && IsMacroTriggerConflicted(m)) prefix = "⚠ ";
            return prefix + m.Name;
        }

        private void MacroList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loadingUi) return;
            CommitNameEdit();
            LoadSelectedMacroToUi();
        }

        private void LoadSelectedMacroToUi()
        {
            MacroDefinition m = SelectedMacro;
            loadingUi = true;
            nameEditingMacro = m;
            bool has = m != null;
            nameBox.Enabled = has;
            descriptionBox.Enabled = has;
            enabledBox.Enabled = has;
            captureTriggerButton.Enabled = has;
            triggerModeBox.Enabled = has;
            suppressBox.Enabled = has;
            infiniteBox.Enabled = has;
            repeatBox.Enabled = has && (m == null || !m.Infinite);
            runButton.Enabled = has && !recordingActive;
            recordButton.Enabled = has;

            TopMost = config.KeepWindowTopMost;
            if (minimizeToTrayMenuItem != null) minimizeToTrayMenuItem.Checked = config.MinimizeToTray;

            if (m != null)
            {
                nameBox.Text = m.Name;
                descriptionBox.Text = m.Description ?? "";
                enabledBox.Checked = m.Enabled;
                triggerBox.Text = InputNames.FormatTrigger(m.Trigger);
                try { triggerBox.SelectionStart = 0; triggerBox.SelectionLength = 0; } catch { }
                triggerModeBox.SelectedIndex = m.RunMode == TriggerRunMode.Hold ? 1 : 0;
                suppressBox.Checked = m.SuppressTrigger;
                infiniteBox.Checked = m.Infinite;
                decimal value = m.RepeatCount;
                if (value < repeatBox.Minimum) value = repeatBox.Minimum;
                if (value > repeatBox.Maximum) value = repeatBox.Maximum;
                repeatBox.Value = value;
            }
            else
            {
                nameBox.Text = "";
                descriptionBox.Text = "";
                triggerBox.Text = "";
                triggerModeBox.SelectedIndex = 0;
                enabledBox.Checked = false;
                suppressBox.Checked = false;
                infiniteBox.Checked = false;
            }
            loadingUi = false;
            UpdateTriggerModeUiText();
            RefreshSteps();
            UpdateRunButton();
            UpdateRecordButton();
            RefreshPanicUi();
        }

        private void RefreshSteps()
        {
            grid.Rows.Clear();
            MacroDefinition m = SelectedMacro;
            if (m == null || m.Steps == null) return;
            int i = 1;
            foreach (MacroStep step in m.Steps)
            {
                if (step == null) continue;
                string hold = step.Action == MacroAction.Press && step.Kind != InputKind.WheelUp && step.Kind != InputKind.WheelDown ? step.HoldMs.ToString() : "—";
                string delay = step.RandomDelay
                    ? Math.Min(step.RandomDelayMinMs, step.RandomDelayMaxMs).ToString() + "～" + Math.Max(step.RandomDelayMinMs, step.RandomDelayMaxMs).ToString()
                    : step.DelayMs.ToString();
                grid.Rows.Add(i.ToString(), InputNames.FormatAction(step.Action), InputNames.FormatInput(step.Kind, step.VirtualKey), hold, delay);
                i++;
            }
        }

        private void NameBox_TextChanged(object sender, EventArgs e)
        {
            if (loadingUi) return;
            if (nameEditingMacro == null) return;
            // Do not rebuild/update the ListBox on every keystroke. In WinForms, replacing
            // the selected ListBox item can cause a selection/focus churn, which was the
            // reason the caret jumped to the trigger row and Chinese text appeared reversed.
            nameEditingMacro.Name = nameBox.Text;
        }

        private void NameBox_Leave(object sender, EventArgs e)
        {
            CommitNameEdit();
        }

        private void DescriptionBox_TextChanged(object sender, EventArgs e)
        {
            if (loadingUi) return;
            MacroDefinition m = SelectedMacro;
            if (m != null) m.Description = descriptionBox.Text ?? "";
        }

        private void CommitNameEdit()
        {
            MacroDefinition m = nameEditingMacro;
            if (m == null) return;
            string value = nameBox.Text == null ? "" : nameBox.Text.Trim();
            if (value.Length == 0) value = "未命名宏";
            m.Name = value;
            int idx = config.Macros.IndexOf(m);
            if (idx >= 0) UpdateMacroListItem(idx);
            SaveConfig();
        }

        private void UpdateMacroListItem(int idx)
        {
            if (idx < 0 || idx >= macroList.Items.Count || idx >= config.Macros.Count) return;
            bool oldLoading = loadingUi;
            loadingUi = true;
            try
            {
                MacroDefinition m = config.Macros[idx];
                macroList.BeginUpdate();
                macroList.Items[idx] = FormatMacroListItem(m);
                macroList.EndUpdate();
            }
            finally
            {
                loadingUi = oldLoading;
            }
        }

        private void Settings_Changed(object sender, EventArgs e)
        {
            if (loadingUi) return;
            MacroDefinition m = SelectedMacro;
            if (m == null) return;
            m.Enabled = enabledBox.Checked;
            m.SuppressTrigger = suppressBox.Checked;
            m.RepeatCount = (int)repeatBox.Value;
            int idx = macroList.SelectedIndex;
            SaveConfig();
            if (sender == enabledBox) UpdateMacroListItem(idx);
            RefreshConflictIndicators(false);
        }

        private void TriggerModeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loadingUi) return;
            MacroDefinition m = SelectedMacro;
            if (m == null) return;
            m.RunMode = triggerModeBox.SelectedIndex == 1 ? TriggerRunMode.Hold : TriggerRunMode.Toggle;
            UpdateTriggerModeUiText();
            SaveConfig();

            if (m.RunMode == TriggerRunMode.Hold && !IsHoldTriggerSupported(m.Trigger))
            {
                statusLabel.Text = "提示：按住运行模式仅支持不含 Ctrl/Shift/Alt/Win 的单个键盘键或鼠标按钮；请重新录制触发键。";
            }
        }

        private void UpdateTriggerModeUiText()
        {
            MacroDefinition m = SelectedMacro;
            bool hold = m != null && m.RunMode == TriggerRunMode.Hold;
            infiniteBox.Text = hold ? Localizer.T("无限循环（松开触发键或点击停止）") : Localizer.T("无限循环（再次触发或点击停止）");
        }

        private static bool IsHoldTriggerSupported(TriggerSpec t)
        {
            if (t == null) return false;
            if (t.Ctrl || t.Shift || t.Alt || t.Win) return false;
            return t.Kind != InputKind.WheelUp && t.Kind != InputKind.WheelDown;
        }

        private void InfiniteBox_CheckedChanged(object sender, EventArgs e)
        {
            if (loadingUi) return;
            MacroDefinition m = SelectedMacro;
            if (m == null) return;
            m.Infinite = infiniteBox.Checked;
            repeatBox.Enabled = !m.Infinite;
            SaveConfig();
        }

        private void OpenConfigFolderButton_Click(object sender, EventArgs e)
        {
            try
            {
                Directory.CreateDirectory(appDir);
                Process.Start("explorer.exe", appDir);
                statusLabel.Text = "状态：已打开配置文件夹。";
            }
            catch (Exception ex)
            {
                LocalizedMessageBox.Show(this, "无法打开配置文件夹：\r\n" + ex.Message, AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "状态：打开配置文件夹失败。";
            }
        }

        private void ExportConfigButton_Click(object sender, EventArgs e)
        {
            CommitNameEdit();
            SaveConfig();
            if (config.Macros == null || config.Macros.Count == 0)
            {
                LocalizedMessageBox.Show(this, "当前没有可导出的宏。", AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool previousPauseHotkeys = pauseHotkeys;
            pauseHotkeys = true;
            uiSafetyModalDepth++;
            UpdateUiSafetyPauseState();
            try
            {
                List<int> indices;
                using (MacroExportSelectionForm choose = new MacroExportSelectionForm(config.Macros, macroList.SelectedIndex))
                {
                    if (choose.ShowDialog(this) != DialogResult.OK) return;
                    indices = new List<int>(choose.SelectedIndices);
                }

                MacroPackage package = new MacroPackage();
                foreach (int i in indices)
                {
                    if (i >= 0 && i < config.Macros.Count && config.Macros[i] != null)
                        package.Macros.Add(config.Macros[i]);
                }
                if (package.Macros.Count == 0) return;

                Directory.CreateDirectory(packagesDir);
                using (SaveFileDialog dlg = new SaveFileDialog())
                {
                    dlg.Title = Localizer.T("导出 InputStitch 宏包");
                    dlg.Filter = Localizer.T("InputStitch 宏包 (*.mpmacro)|*.mpmacro|所有文件 (*.*)|*.*");
                    dlg.DefaultExt = "mpmacro";
                    dlg.AddExtension = true;
                    dlg.InitialDirectory = packagesDir;
                    if (package.Macros.Count == 1)
                        dlg.FileName = SafeFileName(package.Macros[0].Name) + ".mpmacro";
                    else
                        dlg.FileName = "InputStitch_macros_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".mpmacro";
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    try
                    {
                        SerializeMacroPackageToFile(package, dlg.FileName);
                        statusLabel.Text = "状态：已导出宏包：" + Path.GetFileName(dlg.FileName) + "（" + package.Macros.Count.ToString() + " 个宏）";
                    }
                    catch (Exception ex)
                    {
                        LocalizedMessageBox.Show(this, "导出宏失败：\r\n" + ex.Message, AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        statusLabel.Text = "状态：导出宏失败。";
                    }
                }
            }
            finally
            {
                uiSafetyModalDepth = Math.Max(0, uiSafetyModalDepth - 1);
                pauseHotkeys = previousPauseHotkeys;
                UpdateUiSafetyPauseState();
            }
        }

        private void ImportConfigButton_Click(object sender, EventArgs e)
        {
            bool previousPauseHotkeys = pauseHotkeys;
            pauseHotkeys = true;
            uiSafetyModalDepth++;
            UpdateUiSafetyPauseState();
            try
            {
                Directory.CreateDirectory(packagesDir);
                using (OpenFileDialog dlg = new OpenFileDialog())
                {
                    dlg.Title = Localizer.T("导入 InputStitch 宏");
                    dlg.Filter = Localizer.T("InputStitch 宏包 (*.mpmacro)|*.mpmacro|兼容配置 (*.xml;*.mpprofile)|*.xml;*.mpprofile|所有文件 (*.*)|*.*");
                    dlg.CheckFileExists = true;
                    dlg.Multiselect = true;
                    dlg.InitialDirectory = packagesDir;
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;

                    CommitNameEdit();
                    List<MacroDefinition> incoming = new List<MacroDefinition>();
                    try
                    {
                        foreach (string file in dlg.FileNames)
                        {
                            MacroPackage package = DeserializeMacroPackageOrLegacyConfig(file);
                            if (package != null && package.Macros != null)
                            {
                                foreach (MacroDefinition m in package.Macros)
                                    if (m != null) incoming.Add(m);
                            }
                        }
                        NormalizeMacroDefinitions(incoming);
                    }
                    catch (Exception ex)
                    {
                        LocalizedMessageBox.Show(this, "导入宏失败，当前宏列表未改变：\r\n" + ex.Message, AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        statusLabel.Text = "状态：导入宏失败。";
                        return;
                    }

                    if (incoming.Count == 0)
                    {
                        LocalizedMessageBox.Show(this, "所选文件中没有可导入的宏。", AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    int firstNewIndex = config.Macros.Count;
                    HashSet<string> usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (MacroDefinition existing in config.Macros)
                        if (existing != null && !string.IsNullOrWhiteSpace(existing.Name)) usedNames.Add(existing.Name);

                    foreach (MacroDefinition m in incoming)
                    {
                        m.Name = MakeUniqueImportedName(m.Name, usedNames);
                        usedNames.Add(m.Name);
                        config.Macros.Add(m);
                    }
                    SaveConfig();
                    RefreshMacroList(firstNewIndex);
                    int conflictGroups = CountEnabledTriggerConflictGroups();
                    if (conflictGroups > 0)
                        statusLabel.Text = "状态：已导入 " + incoming.Count.ToString() + " 个宏；发现 " + conflictGroups.ToString() + " 组重复的已启用触发键，请检查。";
                    else
                        statusLabel.Text = "状态：已导入 " + incoming.Count.ToString() + " 个宏，已追加到列表末尾。";
                }
            }
            finally
            {
                uiSafetyModalDepth = Math.Max(0, uiSafetyModalDepth - 1);
                pauseHotkeys = previousPauseHotkeys;
                UpdateUiSafetyPauseState();
            }
        }

        private static string SafeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "InputStitch_macro";
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            name = name.Trim();
            if (name.Length == 0) name = "InputStitch_macro";
            if (name.Length > 80) name = name.Substring(0, 80);
            return name;
        }

        private static string MakeUniqueImportedName(string original, HashSet<string> used)
        {
            string baseName = string.IsNullOrWhiteSpace(original) ? "未命名宏" : original.Trim();
            if (!used.Contains(baseName)) return baseName;
            string candidate = baseName + " (导入)";
            if (!used.Contains(candidate)) return candidate;
            int n = 2;
            while (true)
            {
                candidate = baseName + " (导入 " + n.ToString() + ")";
                if (!used.Contains(candidate)) return candidate;
                n++;
            }
        }

        private int CountEnabledTriggerConflictGroups()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (MacroDefinition m in config.Macros)
            {
                if (m == null || !m.Enabled || m.Trigger == null) continue;
                string key = TriggerIdentity(m.Trigger);
                int count;
                counts.TryGetValue(key, out count);
                counts[key] = count + 1;
            }
            int groups = 0;
            foreach (KeyValuePair<string, int> pair in counts)
                if (pair.Value > 1) groups++;
            return groups;
        }

        private static string TriggerIdentity(TriggerSpec t)
        {
            if (t == null) return "";
            return (t.Ctrl ? "C" : "-") + (t.Shift ? "S" : "-") + (t.Alt ? "A" : "-") + (t.Win ? "W" : "-") +
                   "|" + ((int)t.Kind).ToString() + "|" + t.VirtualKey.ToString();
        }

        private static bool TriggersEqual(TriggerSpec a, TriggerSpec b)
        {
            if (a == null || b == null) return false;
            return TriggerIdentity(a) == TriggerIdentity(b);
        }

        private bool IsMacroTriggerConflicted(MacroDefinition macro)
        {
            if (macro == null || !macro.Enabled || macro.Trigger == null) return false;
            if (config.PanicTrigger != null && TriggersEqual(macro.Trigger, config.PanicTrigger)) return true;
            foreach (MacroDefinition other in config.Macros)
            {
                if (other == null || other == macro || !other.Enabled || other.Trigger == null) continue;
                if (TriggersEqual(macro.Trigger, other.Trigger)) return true;
            }
            return false;
        }

        private string GetTriggerConflictSummary()
        {
            List<string> messages = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (MacroDefinition m in config.Macros)
            {
                if (m == null || !m.Enabled || m.Trigger == null) continue;
                if (config.PanicTrigger != null && TriggersEqual(m.Trigger, config.PanicTrigger))
                {
                    string msg = Localizer.IsEnglish
                        ? "\"" + m.Name + "\" conflicts with the Emergency Stop hotkey"
                        : "“" + m.Name + "”与紧急停止键冲突";
                    if (seen.Add(msg)) messages.Add(msg);
                }
                foreach (MacroDefinition other in config.Macros)
                {
                    if (other == null || other == m || !other.Enabled || other.Trigger == null) continue;
                    if (TriggersEqual(m.Trigger, other.Trigger))
                    {
                        string a = string.Compare(m.Name, other.Name, StringComparison.OrdinalIgnoreCase) <= 0 ? m.Name : other.Name;
                        string b = a == m.Name ? other.Name : m.Name;
                        string msg = Localizer.IsEnglish
                            ? "\"" + a + "\" and \"" + b + "\" share " + InputNames.FormatTrigger(m.Trigger)
                            : "“" + a + "”与“" + b + "”共用 " + InputNames.FormatTrigger(m.Trigger);
                        if (seen.Add(msg)) messages.Add(msg);
                    }
                }
            }
            return messages.Count == 0 ? Localizer.T("无") : string.Join(Localizer.IsEnglish ? "; " : "；", messages.ToArray());
        }

        private void RefreshConflictIndicators(bool showStatus)
        {
            if (macroList == null || config == null || config.Macros == null) return;
            bool oldLoading = loadingUi;
            loadingUi = true;
            try
            {
                macroList.BeginUpdate();
                int count = Math.Min(macroList.Items.Count, config.Macros.Count);
                for (int i = 0; i < count; i++) macroList.Items[i] = FormatMacroListItem(config.Macros[i]);
                macroList.EndUpdate();
            }
            finally { loadingUi = oldLoading; }
            RefreshPanicUi();
            if (showStatus)
            {
                string summary = GetTriggerConflictSummary();
                statusLabel.Text = string.Equals(summary, Localizer.T("无"), StringComparison.Ordinal) ? "状态：未发现已启用的触发键冲突。" : "提示：触发键冲突：" + summary;
            }
        }

        private void SaveProfileButton_Click(object sender, EventArgs e)
        {
            CommitNameEdit();
            SaveConfig();
            Directory.CreateDirectory(profilesDir);
            bool previousPauseHotkeys = pauseHotkeys;
            pauseHotkeys = true;
            uiSafetyModalDepth++;
            UpdateUiSafetyPauseState();
            try
            {
                using (SaveFileDialog dlg = new SaveFileDialog())
                {
                    dlg.Title = Localizer.T("保存 InputStitch 配置方案");
                    dlg.Filter = Localizer.T("InputStitch 配置方案 (*.mpprofile)|*.mpprofile|所有文件 (*.*)|*.*");
                    dlg.DefaultExt = "mpprofile";
                    dlg.AddExtension = true;
                    dlg.InitialDirectory = profilesDir;
                    dlg.FileName = "InputStitch_profile_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".mpprofile";
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;

                    string boundProcess = "";
                    if (!string.IsNullOrWhiteSpace(config.TargetProcessName))
                    {
                        DialogResult bind = LocalizedMessageBox.Show(this,
                            "是否将此方案绑定到当前目标进程：\r\n" + config.TargetProcessName +
                            "\r\n\r\n选择“是”后，开启“按前台程序自动切换方案”时可自动载入。\r\n选择“否”则保存为不自动绑定的普通方案。",
                            "绑定方案到目标进程", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                        if (bind == DialogResult.Cancel) return;
                        if (bind == DialogResult.Yes) boundProcess = config.TargetProcessName;
                    }

                    try
                    {
                        ProfilePackage package = new ProfilePackage();
                        package.ProfileName = Path.GetFileNameWithoutExtension(dlg.FileName);
                        package.BoundProcessName = boundProcess;
                        package.Config = CloneConfig(config);
                        SerializeProfilePackageToFile(package, dlg.FileName);
                        activeProfilePath = dlg.FileName;
                        statusLabel.Text = string.IsNullOrWhiteSpace(boundProcess)
                            ? "状态：已保存配置方案：" + Path.GetFileName(dlg.FileName)
                            : "状态：已保存并绑定方案：" + Path.GetFileName(dlg.FileName) + " → " + boundProcess;
                    }
                    catch (Exception ex)
                    {
                        AppLog.Write("Save profile failed", ex);
                        LocalizedMessageBox.Show(this, "保存配置方案失败：\r\n" + ex.Message, AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        statusLabel.Text = "状态：保存配置方案失败。";
                    }
                }
            }
            finally
            {
                uiSafetyModalDepth = Math.Max(0, uiSafetyModalDepth - 1);
                pauseHotkeys = previousPauseHotkeys;
                UpdateUiSafetyPauseState();
            }
        }

        private void LoadProfileButton_Click(object sender, EventArgs e)
        {
            Directory.CreateDirectory(profilesDir);
            bool previousPauseHotkeys = pauseHotkeys;
            pauseHotkeys = true;
            uiSafetyModalDepth++;
            UpdateUiSafetyPauseState();
            try
            {
                using (OpenFileDialog dlg = new OpenFileDialog())
                {
                    dlg.Title = Localizer.T("载入 InputStitch 配置方案");
                    dlg.Filter = Localizer.T("InputStitch 配置方案 (*.mpprofile)|*.mpprofile|旧版 XML 配置 (*.xml)|*.xml|所有文件 (*.*)|*.*");
                    dlg.CheckFileExists = true;
                    dlg.Multiselect = false;
                    dlg.InitialDirectory = profilesDir;
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;

                    if (LocalizedMessageBox.Show(this,
                        "载入方案会停止当前宏，并用所选方案替换当前宏列表和大部分程序设置。\r\n\r\n紧急停止键、自动方案切换和托盘设置保持不变；当前配置会先自动备份。是否继续？",
                        "载入配置方案", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

                    CommitNameEdit();
                    if (!StopWorkerForConfigChange()) return;
                    try
                    {
                        ProfilePackage package = DeserializeProfilePackageOrLegacy(dlg.FileName);
                        if (package == null || package.Config == null) throw new InvalidDataException("配置方案为空。");
                        ApplyProfileConfig(package.Config, dlg.FileName, true);
                        statusLabel.Text = "状态：已载入配置方案：" + Path.GetFileName(dlg.FileName);
                    }
                    catch (Exception ex)
                    {
                        AppLog.Write("Load profile failed", ex);
                        LocalizedMessageBox.Show(this, "载入配置方案失败，当前配置未被替换：\r\n" + ex.Message, AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        statusLabel.Text = "状态：载入配置方案失败。";
                    }
                }
            }
            finally
            {
                uiSafetyModalDepth = Math.Max(0, uiSafetyModalDepth - 1);
                pauseHotkeys = previousPauseHotkeys;
                UpdateUiSafetyPauseState();
            }
        }

        private bool StopWorkerForConfigChange()
        {
            if (recordingActive) StopMacroRecording(false, true);
            Thread t = null;
            lock (runLock)
            {
                if (workerThread != null && workerThread.IsAlive)
                {
                    if (stopEvent != null) stopEvent.Set();
                    t = workerThread;
                }
            }
            if (t == null || !t.IsAlive) return true;
            statusLabel.Text = "状态：正在停止当前宏…";
            UpdateRunButton();
            if (t.Join(1500)) return true;
            LocalizedMessageBox.Show(this, "当前宏未能及时停止。为避免配置与执行线程状态不一致，本次操作已取消。", AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            statusLabel.Text = "状态：操作失败：当前宏仍在停止中。";
            return false;
        }

        private void BackupCurrentConfig(string reason)
        {
            try
            {
                if (!File.Exists(configPath)) return;
                Directory.CreateDirectory(AppPaths.Backups);
                string safeReason = SafeFileName(string.IsNullOrWhiteSpace(reason) ? "backup" : reason);
                string backup = Path.Combine(AppPaths.Backups, "config." + safeReason + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".xml");
                File.Copy(configPath, backup, false);
            }
            catch (Exception ex) { AppLog.Write("Config backup failed", ex); }
        }

        private void ApplyProfileConfig(MacroConfig imported, string profilePath, bool createBackup)
        {
            NormalizeConfig(imported);
            if (createBackup) BackupCurrentConfig("before-profile-load");

            MacroConfig previous = config;
            TriggerSpec panic = config.PanicTrigger == null ? null : config.PanicTrigger.Clone();
            bool autoSwitch = config.AutoSwitchProfiles;
            bool minimize = config.MinimizeToTray;
            string language = config.Language;
            string updateMode = config.UpdateMode;
            bool welcome = config.HasSeenWelcome;
            try
            {
                config = imported;
                config.PanicTrigger = panic ?? new MacroConfig().PanicTrigger;
                config.AutoSwitchProfiles = autoSwitch;
                config.MinimizeToTray = minimize;
                config.Language = language;
                config.UpdateMode = updateMode;
                Localizer.SetLanguage(config.Language);
                config.HasSeenWelcome = welcome;
                EnsureDefaultMacro();
                InputSender.UseScanCodeInput = config.UseScanCodeInput;
                targetWindowHandle = IntPtr.Zero;
                ResolveTargetWindowFromConfig();
                RefreshTargetWindowUi();
                UpdateTargetSectionVisibility();
                RefreshMacroList(0);
                TopMost = config.KeepWindowTopMost;
                activeProfilePath = profilePath ?? "";
                UpdateUiSafetyPauseState();
                RefreshPanicUi();
                SaveConfig();
            }
            catch
            {
                config = previous;
                Localizer.SetLanguage(config.Language);
                InputSender.UseScanCodeInput = config.UseScanCodeInput;
                targetWindowHandle = IntPtr.Zero;
                ResolveTargetWindowFromConfig();
                RefreshTargetWindowUi();
                UpdateTargetSectionVisibility();
                RefreshMacroList(0);
                TopMost = config.KeepWindowTopMost;
                UpdateUiSafetyPauseState();
                throw;
            }
        }

        private static MacroConfig CloneConfig(MacroConfig source)
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

        private static void SerializeMacroPackageToFile(MacroPackage value, string path)
        {
            if (value == null || value.Macros == null) throw new InvalidDataException("宏包为空。");
            value.FormatVersion = AppInfo.MacroPackageFormatVersion;
            XmlSerializer xs = new XmlSerializer(typeof(MacroPackage));
            using (FileStream fs = File.Create(path)) xs.Serialize(fs, value);
        }

        private static void SerializeProfilePackageToFile(ProfilePackage value, string path)
        {
            if (value == null || value.Config == null) throw new InvalidDataException("配置方案为空。");
            value.FormatVersion = AppInfo.ProfileFormatVersion;
            XmlSerializer xs = new XmlSerializer(typeof(ProfilePackage));
            using (FileStream fs = File.Create(path)) xs.Serialize(fs, value);
        }

        private static ProfilePackage DeserializeProfilePackageOrLegacy(string path)
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

        private static MacroPackage DeserializeMacroPackageOrLegacyConfig(string path)
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

        private static void NormalizeMacroDefinitions(List<MacroDefinition> macros)
        {
            if (macros == null) return;
            macros.RemoveAll(delegate(MacroDefinition item) { return item == null; });
            foreach (MacroDefinition m in macros)
            {
                if (string.IsNullOrWhiteSpace(m.Name)) m.Name = "未命名宏";
                if (m.Description == null) m.Description = "";
                if (m.Trigger == null) m.Trigger = new TriggerSpec();
                if (m.RepeatCount < 1) m.RepeatCount = 1;
                if (m.RepeatCount > 100000000) m.RepeatCount = 100000000;
                if (m.Steps == null) m.Steps = new List<MacroStep>();
                m.Steps.RemoveAll(delegate(MacroStep item) { return item == null; });
                foreach (MacroStep step in m.Steps)
                {
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

        private static void EnsureSupportedFormatVersion(string found, string current, string label)
        {
            int foundValue;
            int currentValue;
            if (string.IsNullOrWhiteSpace(found) || !int.TryParse(found, out foundValue)) return;
            if (!int.TryParse(current, out currentValue)) return;
            if (foundValue > currentValue)
                throw new InvalidDataException(label + "由更高版本的 InputStitch 创建（格式版本 " + found + "），当前版本无法安全读取。请先更新程序。");
        }

        private static void SerializeConfigToFile(MacroConfig value, string path)
        {
            if (value == null) throw new InvalidDataException("配置为空。");
            value.FormatVersion = AppInfo.ConfigFormatVersion;
            XmlSerializer xs = new XmlSerializer(typeof(MacroConfig));
            using (FileStream fs = File.Create(path)) xs.Serialize(fs, value);
        }

        private static MacroConfig DeserializeConfigFromFile(string path)
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

        private static void NormalizeConfig(MacroConfig value)
        {
            if (value == null) throw new InvalidDataException("配置为空。");
            EnsureSupportedFormatVersion(value.FormatVersion, AppInfo.ConfigFormatVersion, "配置文件");
            value.FormatVersion = AppInfo.ConfigFormatVersion;
            if (value.Macros == null) value.Macros = new List<MacroDefinition>();
            NormalizeMacroDefinitions(value.Macros);
            if (value.PanicTrigger == null) value.PanicTrigger = new MacroConfig().PanicTrigger;
            if (!string.Equals(value.Language, Localizer.English, StringComparison.OrdinalIgnoreCase)) value.Language = Localizer.Chinese;
            if (!string.Equals(value.UpdateMode, UpdateModes.Manual, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(value.UpdateMode, UpdateModes.Disabled, StringComparison.OrdinalIgnoreCase))
                value.UpdateMode = UpdateModes.Automatic;
            if (value.UiRunStartDelayMs < 0) value.UiRunStartDelayMs = 0;
            if (value.UiRunStartDelayMs > 5000) value.UiRunStartDelayMs = 5000;
        }

        private void TryAutoSwitchProfileForProcess(string processName)
        {
            if (recordingActive || autoProfileSwitchBusy || config == null || !config.AutoSwitchProfiles) return;
            if (string.IsNullOrWhiteSpace(processName)) return;
            if (string.Equals(processName, lastAutoProfileProcess, StringComparison.OrdinalIgnoreCase)) return;
            lastAutoProfileProcess = processName;

            string match = FindBoundProfileForProcess(processName);
            if (string.IsNullOrWhiteSpace(match)) return;
            if (!string.IsNullOrWhiteSpace(activeProfilePath) && string.Equals(Path.GetFullPath(match), Path.GetFullPath(activeProfilePath), StringComparison.OrdinalIgnoreCase)) return;

            autoProfileSwitchBusy = true;
            try
            {
                if (!StopWorkerForConfigChange()) return;
                ProfilePackage package = DeserializeProfilePackageOrLegacy(match);
                if (package == null || package.Config == null) return;
                ApplyProfileConfig(package.Config, match, false);
                statusLabel.Text = "状态：已按前台程序自动切换方案：" + Path.GetFileName(match);
            }
            catch (Exception ex)
            {
                AppLog.Write("Auto profile switch failed: " + match, ex);
                statusLabel.Text = "状态：自动切换配置方案失败。";
            }
            finally { autoProfileSwitchBusy = false; }
        }

        private string FindBoundProfileForProcess(string processName)
        {
            try
            {
                if (!Directory.Exists(profilesDir)) return "";
                string[] files = Directory.GetFiles(profilesDir, "*.mpprofile");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                foreach (string file in files)
                {
                    try
                    {
                        ProfilePackage p = DeserializeProfilePackageOrLegacy(file);
                        if (p != null && !string.IsNullOrWhiteSpace(p.BoundProcessName) &&
                            string.Equals(p.BoundProcessName, processName, StringComparison.OrdinalIgnoreCase))
                            return file;
                    }
                    catch (Exception ex) { AppLog.Write("Profile metadata read failed: " + file, ex); }
                }
            }
            catch (Exception ex) { AppLog.Write("Profile scan failed", ex); }
            return "";
        }

        private void MoveSelectedMacro(int delta)
        {
            CommitNameEdit();
            int idx = macroList.SelectedIndex;
            int to = idx + delta;
            if (idx < 0 || idx >= config.Macros.Count || to < 0 || to >= config.Macros.Count) return;
            MacroDefinition m = config.Macros[idx];
            config.Macros.RemoveAt(idx);
            config.Macros.Insert(to, m);
            SaveConfig();
            RefreshMacroList(to);
            statusLabel.Text = "状态：已调整宏列表顺序。";
        }

        private void AddMacro_Click(object sender, EventArgs e)
        {
            MacroDefinition m = new MacroDefinition();
            m.Name = Localizer.T("新宏") + " " + (config.Macros.Count + 1).ToString();
            int vk = (int)Keys.F8 + Math.Min(config.Macros.Count, 4);
            m.Trigger.VirtualKey = vk;
            config.Macros.Add(m);
            SaveConfig();
            RefreshMacroList(config.Macros.Count - 1);
        }

        private void CopyMacro_Click(object sender, EventArgs e)
        {
            MacroDefinition m = SelectedMacro;
            if (m == null) return;
            MacroDefinition copy = m.Clone();
            copy.Enabled = false;
            config.Macros.Add(copy);
            SaveConfig();
            RefreshMacroList(config.Macros.Count - 1);
        }

        private void DeleteMacro_Click(object sender, EventArgs e)
        {
            int idx = macroList.SelectedIndex;
            MacroDefinition m = SelectedMacro;
            if (m == null) return;
            if (runningMacro == m)
            {
                LocalizedMessageBox.Show(this, "请先停止正在执行的宏。", AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (LocalizedMessageBox.Show(this, "确定删除宏“" + m.Name + "”吗？", AppInfo.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            config.Macros.RemoveAt(idx);
            if (config.Macros.Count == 0) config.Macros.Add(new MacroDefinition());
            SaveConfig();
            RefreshMacroList(Math.Min(idx, config.Macros.Count - 1));
        }

        private void CaptureTriggerButton_Click(object sender, EventArgs e)
        {
            if (captureMode == CaptureMode.Trigger)
            {
                CancelCapture();
                return;
            }
            captureMode = CaptureMode.Trigger;
            captureTriggerButton.Text = Localizer.Dynamic("请按触发键…");
            statusLabel.Text = "状态：正在录制触发键（按 Esc 取消）";
            UpdateUiSafetyPauseState();
        }

        public void BeginStepInputCapture(Action<InputSpec> callback)
        {
            captureMode = CaptureMode.StepInput;
            stepCaptureCallback = callback;
            statusLabel.Text = "状态：请按下要使用的键/鼠标按钮/滚轮（Esc 取消）";
            UpdateUiSafetyPauseState();
        }

        public void CancelCapture()
        {
            captureMode = CaptureMode.None;
            stepCaptureCallback = null;
            if (captureTriggerButton != null)
            {
                captureTriggerButton.Text = Localizer.T("录制触发键");
                captureTriggerButton.Enabled = SelectedMacro != null;
            }
            RefreshPanicUi();
            if (!recordingActive && runningMacro == null && statusLabel != null) statusLabel.Text = "状态：空闲";
            UpdateUiSafetyPauseState();
        }

        private static TriggerSpec TriggerFromInputEvent(InputEventInfo e)
        {
            TriggerSpec t = new TriggerSpec();
            t.Ctrl = e.Ctrl;
            t.Shift = e.Shift;
            t.Alt = e.Alt;
            t.Win = e.Win;
            t.Kind = e.Input.Kind;
            t.VirtualKey = e.Input.VirtualKey;
            return t;
        }

        private bool HandleTerminalInput(InputEventInfo e)
        {
            if (e == null || e.Input == null) return false;

            // The configured emergency-stop chord has absolute priority, including while the
            // user is recording a macro or capturing another hotkey. This guarantees a single
            // global escape path from every interactive state.
            if (config != null && config.PanicTrigger != null && ModifierSafetyPolicy.TriggerRequiredModifiersMatch(config.PanicTrigger, e))
            {
                try { BeginInvoke((MethodInvoker)delegate { EmergencyStop("全局紧急停止键"); }); } catch { }
                return true;
            }

            if (captureMode != CaptureMode.None)
            {
                if (e.Input.Kind == InputKind.Keyboard && e.Input.VirtualKey == (int)Keys.Escape && !e.Ctrl && !e.Shift && !e.Alt && !e.Win)
                {
                    Action<InputSpec> cancelledStepCapture = captureMode == CaptureMode.StepInput ? stepCaptureCallback : null;
                    CancelCapture();
                    if (cancelledStepCapture != null) cancelledStepCapture(null);
                    return true;
                }

                if (captureMode == CaptureMode.PanicTrigger)
                {
                    if (e.Input.Kind == InputKind.WheelUp || e.Input.Kind == InputKind.WheelDown)
                    {
                        try { BeginInvoke((MethodInvoker)delegate { statusLabel.Text = "提示：紧急停止键不建议使用滚轮，请选择键盘键或鼠标按钮。"; }); } catch { }
                        return true;
                    }
                    TriggerSpec panic = TriggerFromInputEvent(e);
                    config.PanicTrigger = panic;
                    try
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            SaveConfig();
                            CancelCapture();
                            RefreshPanicUi();
                            RefreshConflictIndicators(true);
                        });
                    }
                    catch { }
                    return true;
                }

                if (captureMode == CaptureMode.Trigger)
                {
                    MacroDefinition m = SelectedMacro;
                    if (m != null)
                    {
                        TriggerSpec t = TriggerFromInputEvent(e);
                        m.Trigger = t;
                        try
                        {
                            BeginInvoke((MethodInvoker)delegate
                            {
                                triggerBox.Text = InputNames.FormatTrigger(t);
                                SaveConfig();
                                CancelCapture();
                                RefreshConflictIndicators(true);
                                if (m.RunMode == TriggerRunMode.Hold && !IsHoldTriggerSupported(m.Trigger))
                                    statusLabel.Text = "提示：按住运行模式不支持修饰键组合或滚轮，请改用单个键盘键/鼠标按钮。";
                            });
                        }
                        catch { }
                    }
                    else CancelCapture();
                    return true;
                }

                if (captureMode == CaptureMode.StepInput)
                {
                    Action<InputSpec> cb = stepCaptureCallback;
                    captureMode = CaptureMode.None;
                    stepCaptureCallback = null;
                    try
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            if (!recordingActive && runningMacro == null) statusLabel.Text = "状态：空闲";
                            if (cb != null) cb(e.Input.Clone());
                            UpdateUiSafetyPauseState();
                        });
                    }
                    catch { }
                    return true;
                }
            }

            if (recordingActive)
            {
                RecordPhysicalInput(e, true);
                return false;
            }

            if (uiSafetyPauseRequested) return false;
            if (pauseHotkeys || manualTriggerSuspend) return false;

            MacroDefinition matchedMacro = null;
            foreach (MacroDefinition m in config.Macros)
            {
                if (m == null || !m.Enabled || m.Trigger == null) continue;
                if (ModifierSafetyPolicy.TriggerMatchesExactly(m.Trigger, e))
                {
                    matchedMacro = m;
                    break;
                }
            }

            // Game-friendly fallback: keep bare printable keys strict, but allow an already-held
            // gameplay modifier (for example Shift while sprinting) around function keys, numpad,
            // mouse triggers, or a trigger chord that already declares at least one modifier.
            // The most specific declared chord wins; configuration order breaks equal ties.
            if (matchedMacro == null)
            {
                int bestSpecificity = -1;
                foreach (MacroDefinition m in config.Macros)
                {
                    if (m == null || !m.Enabled || m.Trigger == null) continue;
                    if (!ModifierSafetyPolicy.SupportsExtraPhysicalModifiers(m.Trigger)) continue;
                    if (!ModifierSafetyPolicy.TriggerRequiredModifiersMatch(m.Trigger, e)) continue;
                    int specificity = ModifierSafetyPolicy.TriggerSpecificity(m.Trigger);
                    if (specificity > bestSpecificity)
                    {
                        matchedMacro = m;
                        bestSpecificity = specificity;
                    }
                }
            }

            if (matchedMacro != null)
            {
                MacroDefinition m = matchedMacro;
                bool suppress = m.SuppressTrigger;
                try
                {
                    if (m.RunMode == TriggerRunMode.Hold)
                        BeginInvoke((MethodInvoker)delegate { StartMacroFromHeldTrigger(m); });
                    else
                        BeginInvoke((MethodInvoker)delegate { ToggleMacroFromHotkey(m); });
                }
                catch { }
                return suppress;
            }
            return false;
        }

        private void HandleTerminalInputReleased(InputEventInfo e)
        {
            if (e == null || e.Input == null) return;
            if (recordingActive) RecordPhysicalInput(e, false);

            MacroDefinition m = holdControlledMacro;
            if (m == null || m.Trigger == null || m.RunMode != TriggerRunMode.Hold) return;
            if (!TerminalInputMatches(m.Trigger, e.Input)) return;
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    if (runningMacro == m && holdControlledMacro == m) StopCurrentMacro();
                });
            }
            catch { }
        }

        private void StartMacroFromHeldTrigger(MacroDefinition m)
        {
            if (m == null) return;
            if (!IsHoldTriggerSupported(m.Trigger))
            {
                statusLabel.Text = "无法启动：按住运行模式仅支持不含 Ctrl/Shift/Alt/Win 的单个键盘键或鼠标按钮。";
                return;
            }
            if (runningMacro == m && holdControlledMacro == m) return;
            StartMacro(m, 0, null, true);
        }

        private void ToggleMacroFromHotkey(MacroDefinition m)
        {
            if (m == null) return;
            if (IsMacroActuallyRunning(m)) StopCurrentMacro();
            else StartMacro(m, 0, m.Trigger == null ? null : m.Trigger.Clone(), false);
        }

        private static bool TerminalInputMatches(TriggerSpec t, InputSpec input)
        {
            return t != null && input != null && t.Kind == input.Kind && t.VirtualKey == input.VirtualKey;
        }

        private void RecordButton_Click(object sender, EventArgs e)
        {
            if (recordingActive)
            {
                StopMacroRecording(true, false);
                return;
            }
            StartMacroRecording();
        }

        private void StartMacroRecording()
        {
            MacroDefinition m = SelectedMacro;
            if (m == null) return;
            if (HasLiveWorker())
            {
                if (!StopWorkerForConfigChange()) return;
            }
            CommitNameEdit();

            bool replace = false;
            if (m.Steps != null && m.Steps.Count > 0)
            {
                DialogResult choice = LocalizedMessageBox.Show(this,
                    "当前宏已经有执行步骤。\r\n\r\n选择“是”：把录制结果追加到现有步骤末尾。\r\n选择“否”：用录制结果替换现有步骤。\r\n选择“取消”：不开始录制。",
                    "录制宏", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (choice == DialogResult.Cancel) return;
                replace = choice == DialogResult.No;
            }

            lock (recordingLock)
            {
                recordedEvents.Clear();
                recordingInputsDown.Clear();
                recordingStopwatch = Stopwatch.StartNew();
                recordingTargetMacro = m;
                recordingReplaceMode = replace;
                recordingActive = true;
            }
            UpdateRecordButton();
            UpdateRunButton();
            statusLabel.Text = "录制中：切到目标程序进行操作；回到 InputStitch 后点击“停止录制”。";
            AppLog.Write("Macro recording started: " + m.Name);
        }

        private void StopMacroRecording(bool applyResult, bool silent)
        {
            MacroDefinition target;
            bool replace;
            List<RecordedInputEvent> events;
            lock (recordingLock)
            {
                if (!recordingActive && recordingStopwatch == null) return;
                recordingActive = false;
                if (recordingStopwatch != null) recordingStopwatch.Stop();
                target = recordingTargetMacro;
                replace = recordingReplaceMode;
                events = new List<RecordedInputEvent>(recordedEvents);
                recordedEvents.Clear();
                recordingInputsDown.Clear();
                recordingStopwatch = null;
                recordingTargetMacro = null;
            }

            UpdateRecordButton();
            UpdateRunButton();

            if (!applyResult)
            {
                if (!silent) statusLabel.Text = "状态：已取消宏录制。";
                return;
            }
            if (target == null || !config.Macros.Contains(target))
            {
                if (!silent) statusLabel.Text = "状态：录制目标宏已不存在，录制结果未保存。";
                return;
            }

            List<MacroStep> steps = ConvertRecordedEventsToSteps(events);
            if (steps.Count == 0)
            {
                if (!silent) statusLabel.Text = "状态：录制结束，没有记录到目标程序中的键鼠操作。";
                return;
            }
            if (target.Steps == null) target.Steps = new List<MacroStep>();
            if (replace) target.Steps.Clear();
            target.Steps.AddRange(steps);
            SaveConfig();
            if (SelectedMacro == target) RefreshSteps();
            if (!silent) statusLabel.Text = "状态：录制完成，已生成 " + steps.Count.ToString() + " 个步骤。";
            AppLog.Write("Macro recording finished: " + target.Name + ", steps=" + steps.Count.ToString());
        }

        private void RecordPhysicalInput(InputEventInfo e, bool isDown)
        {
            if (!recordingActive || e == null || e.Input == null) return;
            bool holdable = InputSender.IsHoldable(e.Input);
            string key = InputKey(e.Input);
            IntPtr foreground = NativeWindowFocus.ForegroundWindow();
            bool externalForeground = NativeWindowFocus.IsUsableExternalWindow(foreground) && !NativeWindowFocus.IsLikelyTransientTaskSwitcher(foreground);

            lock (recordingLock)
            {
                if (!recordingActive || recordingStopwatch == null) return;
                if (isDown)
                {
                    if (!externalForeground) return;
                    RecordedInputEvent item = new RecordedInputEvent();
                    item.Input = e.Input.Clone();
                    item.IsDown = true;
                    item.TimestampMs = recordingStopwatch.ElapsedMilliseconds;
                    recordedEvents.Add(item);
                    if (holdable) recordingInputsDown.Add(key);
                }
                else
                {
                    // Ignore UI mouse/key releases unless their down event was actually recorded.
                    if (!holdable || !recordingInputsDown.Contains(key)) return;
                    RecordedInputEvent item = new RecordedInputEvent();
                    item.Input = e.Input.Clone();
                    item.IsDown = false;
                    item.TimestampMs = recordingStopwatch.ElapsedMilliseconds;
                    recordedEvents.Add(item);
                    recordingInputsDown.Remove(key);
                }
            }
        }

        private void TrimRecordingTailForUiReturn()
        {
            lock (recordingLock)
            {
                if (!recordingActive || recordingStopwatch == null || recordedEvents.Count == 0) return;
                long now = recordingStopwatch.ElapsedMilliseconds;
                int scanStart = Math.Max(0, recordedEvents.Count - 12);
                int firstNavigation = recordedEvents.Count;
                bool sawSwitcherKey = false;
                for (int i = recordedEvents.Count - 1; i >= scanStart; i--)
                {
                    RecordedInputEvent item = recordedEvents[i];
                    if (item == null || item.Input == null) break;
                    if (now - item.TimestampMs > 10000) break;
                    if (!IsUiReturnNavigationInput(item.Input)) break;
                    firstNavigation = i;
                    if (item.Input.Kind == InputKind.Keyboard &&
                        (item.Input.VirtualKey == (int)Keys.Tab || item.Input.VirtualKey == (int)Keys.LWin || item.Input.VirtualKey == (int)Keys.RWin))
                        sawSwitcherKey = true;
                }
                // Only trim when the tail actually looks like Alt+Tab/Win+Tab navigation. A lone
                // Alt/Shift at the end may be intentional macro content and should be preserved.
                if (!sawSwitcherKey || firstNavigation >= recordedEvents.Count) return;

                recordedEvents.RemoveRange(firstNavigation, recordedEvents.Count - firstNavigation);
                recordingInputsDown.Clear();
                foreach (RecordedInputEvent item in recordedEvents)
                {
                    if (item == null || item.Input == null || !InputSender.IsHoldable(item.Input)) continue;
                    string key = InputKey(item.Input);
                    if (item.IsDown) recordingInputsDown.Add(key);
                    else recordingInputsDown.Remove(key);
                }
            }
        }

        private static bool IsUiReturnNavigationInput(InputSpec input)
        {
            if (input == null || input.Kind != InputKind.Keyboard) return false;
            Keys key = (Keys)input.VirtualKey;
            return key == Keys.Tab || key == Keys.Menu || key == Keys.LMenu || key == Keys.RMenu ||
                   key == Keys.LWin || key == Keys.RWin || key == Keys.ShiftKey || key == Keys.LShiftKey || key == Keys.RShiftKey;
        }

        private static List<MacroStep> ConvertRecordedEventsToSteps(List<RecordedInputEvent> events)
        {
            List<MacroStep> steps = new List<MacroStep>();
            if (events == null || events.Count == 0) return steps;
            for (int i = 0; i < events.Count; i++)
            {
                RecordedInputEvent current = events[i];
                if (current == null || current.Input == null) continue;

                if (current.IsDown && InputSender.IsHoldable(current.Input) && i + 1 < events.Count)
                {
                    RecordedInputEvent next = events[i + 1];
                    if (next != null && next.Input != null && !next.IsDown && SameInput(current.Input, next.Input))
                    {
                        MacroStep press = StepFromInput(current.Input, MacroAction.Press);
                        press.HoldMs = ClampRecordedMs(next.TimestampMs - current.TimestampMs);
                        long nextTime = i + 2 < events.Count && events[i + 2] != null ? events[i + 2].TimestampMs : next.TimestampMs;
                        press.DelayMs = ClampRecordedMs(nextTime - next.TimestampMs);
                        steps.Add(press);
                        i++;
                        continue;
                    }
                }

                MacroAction action;
                if (!InputSender.IsHoldable(current.Input)) action = MacroAction.Press;
                else action = current.IsDown ? MacroAction.Down : MacroAction.Up;
                MacroStep step = StepFromInput(current.Input, action);
                long following = i + 1 < events.Count && events[i + 1] != null ? events[i + 1].TimestampMs : current.TimestampMs;
                step.DelayMs = ClampRecordedMs(following - current.TimestampMs);
                steps.Add(step);
            }
            return steps;
        }

        private static MacroStep StepFromInput(InputSpec input, MacroAction action)
        {
            MacroStep step = new MacroStep();
            step.Action = action;
            step.Kind = input.Kind;
            step.VirtualKey = input.VirtualKey;
            step.ScanCode = input.ScanCode;
            step.Extended = input.Extended;
            step.HoldMs = 30;
            step.DelayMs = 0;
            step.RandomDelay = false;
            return step;
        }

        private static bool SameInput(InputSpec a, InputSpec b)
        {
            if (a == null || b == null) return false;
            return a.Kind == b.Kind && a.VirtualKey == b.VirtualKey && a.ScanCode == b.ScanCode;
        }

        private static int ClampRecordedMs(long ms)
        {
            if (ms < 0) return 0;
            if (ms > 600000) return 600000;
            return (int)ms;
        }

        private List<int> GetSelectedStepIndices()
        {
            List<int> indices = new List<int>();
            foreach (DataGridViewRow row in grid.SelectedRows)
            {
                if (row.Index >= 0 && !indices.Contains(row.Index)) indices.Add(row.Index);
            }
            indices.Sort();
            return indices;
        }

        private void SelectStepIndices(List<int> indices)
        {
            grid.ClearSelection();
            if (indices == null) return;
            foreach (int index in indices)
                if (index >= 0 && index < grid.Rows.Count) grid.Rows[index].Selected = true;
        }

        private void AddStep()
        {
            MacroDefinition m = SelectedMacro;
            if (m == null) return;
            pauseHotkeys = true;
            uiSafetyModalDepth++;
            UpdateUiSafetyPauseState();
            try
            {
                using (StepEditDialog d = new StepEditDialog(this, null))
                {
                    if (d.ShowDialog(this) == DialogResult.OK && d.ResultStep != null)
                    {
                        m.Steps.Add(d.ResultStep);
                        SaveConfig();
                        RefreshSteps();
                        if (grid.Rows.Count > 0) grid.Rows[grid.Rows.Count - 1].Selected = true;
                    }
                }
            }
            finally
            {
                uiSafetyModalDepth = Math.Max(0, uiSafetyModalDepth - 1);
                pauseHotkeys = false;
                CancelCapture();
                UpdateUiSafetyPauseState();
            }
        }

        private void EditSelectedStep()
        {
            MacroDefinition m = SelectedMacro;
            List<int> indices = GetSelectedStepIndices();
            if (m == null || indices.Count == 0) return;
            if (indices.Count != 1)
            {
                statusLabel.Text = "提示：单步编辑一次只能选择一个步骤；批量修改间隔请使用“批量间隔”。";
                return;
            }
            int idx = indices[0];
            if (idx < 0 || idx >= m.Steps.Count) return;
            pauseHotkeys = true;
            uiSafetyModalDepth++;
            UpdateUiSafetyPauseState();
            try
            {
                using (StepEditDialog d = new StepEditDialog(this, m.Steps[idx]))
                {
                    if (d.ShowDialog(this) == DialogResult.OK && d.ResultStep != null)
                    {
                        m.Steps[idx] = d.ResultStep;
                        SaveConfig();
                        RefreshSteps();
                        SelectStepIndices(new List<int>(new int[] { idx }));
                    }
                }
            }
            finally
            {
                uiSafetyModalDepth = Math.Max(0, uiSafetyModalDepth - 1);
                pauseHotkeys = false;
                CancelCapture();
                UpdateUiSafetyPauseState();
            }
        }

        private void CopySelectedSteps()
        {
            MacroDefinition m = SelectedMacro;
            List<int> indices = GetSelectedStepIndices();
            if (m == null || indices.Count == 0) return;
            List<MacroStep> copies = new List<MacroStep>();
            foreach (int idx in indices)
                if (idx >= 0 && idx < m.Steps.Count) copies.Add(m.Steps[idx].Clone());
            if (copies.Count == 0) return;
            int insertAt = indices[indices.Count - 1] + 1;
            m.Steps.InsertRange(insertAt, copies);
            SaveConfig();
            RefreshSteps();
            List<int> newIndices = new List<int>();
            for (int i = 0; i < copies.Count; i++) newIndices.Add(insertAt + i);
            SelectStepIndices(newIndices);
        }

        private void DeleteSelectedStep()
        {
            MacroDefinition m = SelectedMacro;
            List<int> indices = GetSelectedStepIndices();
            if (m == null || indices.Count == 0) return;
            for (int i = indices.Count - 1; i >= 0; i--)
            {
                int idx = indices[i];
                if (idx >= 0 && idx < m.Steps.Count) m.Steps.RemoveAt(idx);
            }
            int next = indices[0];
            SaveConfig();
            RefreshSteps();
            if (grid.Rows.Count > 0)
            {
                if (next >= grid.Rows.Count) next = grid.Rows.Count - 1;
                SelectStepIndices(new List<int>(new int[] { next }));
            }
        }

        private void MoveSelectedStep(int delta)
        {
            MacroDefinition m = SelectedMacro;
            List<int> indices = GetSelectedStepIndices();
            if (m == null || indices.Count == 0 || (delta != -1 && delta != 1)) return;
            bool[] selected = new bool[m.Steps.Count];
            foreach (int idx in indices) if (idx >= 0 && idx < selected.Length) selected[idx] = true;

            if (delta < 0)
            {
                for (int i = 1; i < m.Steps.Count; i++)
                {
                    if (selected[i] && !selected[i - 1])
                    {
                        MacroStep tmp = m.Steps[i - 1];
                        m.Steps[i - 1] = m.Steps[i];
                        m.Steps[i] = tmp;
                        selected[i - 1] = true;
                        selected[i] = false;
                    }
                }
            }
            else
            {
                for (int i = m.Steps.Count - 2; i >= 0; i--)
                {
                    if (selected[i] && !selected[i + 1])
                    {
                        MacroStep tmp = m.Steps[i + 1];
                        m.Steps[i + 1] = m.Steps[i];
                        m.Steps[i] = tmp;
                        selected[i + 1] = true;
                        selected[i] = false;
                    }
                }
            }
            SaveConfig();
            RefreshSteps();
            List<int> result = new List<int>();
            for (int i = 0; i < selected.Length; i++) if (selected[i]) result.Add(i);
            SelectStepIndices(result);
        }

        private void BatchEditSelectedStepDelay()
        {
            MacroDefinition m = SelectedMacro;
            List<int> indices = GetSelectedStepIndices();
            if (m == null || indices.Count == 0) return;
            pauseHotkeys = true;
            uiSafetyModalDepth++;
            UpdateUiSafetyPauseState();
            try
            {
                using (BatchDelayDialog d = new BatchDelayDialog())
                {
                    if (d.ShowDialog(this) != DialogResult.OK) return;
                    foreach (int idx in indices)
                    {
                        if (idx < 0 || idx >= m.Steps.Count) continue;
                        MacroStep step = m.Steps[idx];
                        step.RandomDelay = d.UseRandom;
                        if (d.UseRandom)
                        {
                            step.RandomDelayMinMs = d.MinMs;
                            step.RandomDelayMaxMs = d.MaxMs;
                        }
                        else step.DelayMs = d.FixedMs;
                    }
                    SaveConfig();
                    RefreshSteps();
                    SelectStepIndices(indices);
                }
            }
            finally
            {
                uiSafetyModalDepth = Math.Max(0, uiSafetyModalDepth - 1);
                pauseHotkeys = false;
                UpdateUiSafetyPauseState();
            }
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            MacroDefinition m = SelectedMacro;

            // Move focus away from the Run/Stop button so injected Enter/Space cannot click it
            // again while a macro is running. UI execution now has an explicit target window, so
            // there is no need for the old MA_NOACTIVATE foreground trick.
            try
            {
                if (inputSink != null && inputSink.CanFocus) inputSink.Focus();
            }
            catch { }
            UpdateUiSafetyPauseState();

            CommitNameEdit();

            // The UI Run/Stop button is a GLOBAL stop while any worker is alive. Do not key the
            // stop decision only to the currently selected MacroDefinition: changing selection or
            // a stale bookkeeping value must never make an active macro unreachable.
            if (HasLiveWorker())
            {
                StopCurrentMacro();
                ReconcileHookState("ui-stop");
                statusLabel.Text = "状态：正在停止当前宏…";
                UpdateRunButton();
                return;
            }

            if (m == null) return;

            // Explicitly synchronize the run-related controls right before launch. This removes
            // any dependence on WinForms event ordering (and fixes the old "无限循环只跑一次" case).
            m.Infinite = infiniteBox.Checked;
            m.RepeatCount = (int)repeatBox.Value;
            m.Enabled = enabledBox.Checked;
            m.SuppressTrigger = suppressBox.Checked;
            m.RunMode = triggerModeBox.SelectedIndex == 1 ? TriggerRunMode.Hold : TriggerRunMode.Toggle;
            SaveConfig();

            InputSender.UseScanCodeInput = config.UseScanCodeInput;
            int startDelay = 0;
            if (config.ActivateTargetWindowOnUiRun)
            {
                IntPtr target = GetResolvedTargetWindow();
                if (target == IntPtr.Zero)
                {
                    RefreshTargetWindowUi();
                    statusLabel.Text = HasConfiguredTarget()
                        ? "状态：已保存的目标窗口当前未找到。请启动目标程序，或重新锁定目标窗口。"
                        : "状态：尚未锁定目标窗口。请先切到目标程序，再切回 InputStitch，点击“锁定刚才的目标窗口”。";
                    return;
                }
                RefreshTargetWindowUi();
                if (!NativeWindowFocus.TryActivate(target))
                {
                    statusLabel.Text = "状态：无法激活已锁定的目标窗口。请手动切到目标程序后用热键触发，或重新锁定目标。";
                    return;
                }
                startDelay = Math.Max(0, config.UiRunStartDelayMs);
            }
            else
            {
                // Classic/direct UI-run mode: do not require a configured target and do not
                // force any window switch. This is direct UI-run mode: pressing the UI button simply starts the macro immediately and
                // Windows routes SendInput to whichever application owns the input focus at
                // that moment. The hidden input sink keeps injected Enter/Space from clicking
                // InputStitch's Run button again when InputStitch itself has the focus.
                statusLabel.Text = "状态：开始执行（不切换窗口）。";
            }
            StartMacro(m, startDelay, null, false);
        }

        private bool HasLiveWorker()
        {
            lock (runLock)
            {
                return workerThread != null && workerThread.IsAlive;
            }
        }

        private bool IsMacroActuallyRunning(MacroDefinition m)
        {
            lock (runLock)
            {
                return m != null && runningMacro == m && workerThread != null && workerThread.IsAlive &&
                       stopEvent != null && !stopEvent.IsSet;
            }
        }

        private void StartMacro(MacroDefinition m, int startDelayMs, TriggerSpec waitForReleaseTrigger, bool holdControlled)
        {
            if (m == null) return;
            if (m.Steps == null || m.Steps.Count == 0)
            {
                LocalizedMessageBox.Show(this, "这个宏还没有任何执行步骤。", AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Never overlap two worker threads. A same-macro restart must not start a new worker
            // before the old worker's finally block completes. Because both workers would own
            // the same MacroDefinition object, the old finally block could clear runningMacro and
            // workerThread for the NEW worker, leaving an invisible, unstoppable macro behind.
            Thread previous = null;
            lock (runLock)
            {
                if (workerThread != null && workerThread.IsAlive)
                {
                    if (stopEvent != null) stopEvent.Set();
                    previous = workerThread;
                }
                else
                {
                    // Defensive cleanup for stale bookkeeping after a worker has already exited.
                    workerThread = null;
                    stopEvent = null;
                    runningMacro = null;
                    holdControlledMacro = null;
                    activeRunId = 0;
                }
            }

            if (previous != null && previous.IsAlive)
            {
                // All macro delays are stop-aware, so this should normally return almost
                // immediately. If it does not, fail closed instead of creating a second worker.
                if (!previous.Join(1500))
                {
                    SetStatusSafe("状态：上一宏仍在停止中。为避免重复执行，已取消本次启动；请稍后再试。");
                    UpdateRunButtonSafe();
                    return;
                }
            }

            MacroDefinition snapshot = m.Clone();
            snapshot.Name = m.Name;
            InputSender.UseScanCodeInput = config.UseScanCodeInput;

            ManualResetEventSlim thisStop = new ManualResetEventSlim(false);
            Thread thisThread;
            long thisRunId;

            lock (runLock)
            {
                // A second request could only arrive through the UI queue, but keep this guard so
                // the lifecycle remains correct if StartMacro is called from elsewhere later.
                if (workerThread != null && workerThread.IsAlive)
                {
                    try { thisStop.Dispose(); } catch { }
                    SetStatusSafe("状态：已有宏正在运行，未启动新的宏。");
                    UpdateRunButtonSafe();
                    return;
                }

                thisRunId = ++runSequence;
                activeRunId = thisRunId;
                runningMacro = m;
                holdControlledMacro = holdControlled ? m : null;
                stopEvent = thisStop;
                thisThread = new Thread(new ThreadStart(delegate { MacroWorker(m, snapshot, thisStop, startDelayMs, waitForReleaseTrigger, thisRunId); }));
                thisThread.IsBackground = true;
                thisThread.Name = "InputStitch Worker " + thisRunId.ToString();
                workerThread = thisThread;
            }

            thisThread.Start();
            UpdateRunButton();
        }

        private void StopCurrentMacro()
        {
            lock (runLock) StopCurrentMacro_NoLock();
        }

        private void StopCurrentMacro_NoLock()
        {
            // Stop by the actual active stop event, not by UI selection or runningMacro identity.
            if (stopEvent != null) stopEvent.Set();
        }

        private void MacroWorker(MacroDefinition ownerMacro, MacroDefinition snapshot, ManualResetEventSlim stop, int startDelayMs, TriggerSpec waitForReleaseTrigger, long runId)
        {
            Dictionary<string, InputSpec> held = new Dictionary<string, InputSpec>();
            List<MacroStep> steps = new List<MacroStep>();
            foreach (MacroStep original in snapshot.Steps) steps.Add(original.Clone());
            string macroName = snapshot.Name;
            bool infinite = snapshot.Infinite;
            int repeatCount = Math.Max(1, snapshot.RepeatCount);
            string executionError = null;
            Random random = new Random(unchecked(Environment.TickCount * 31 + (int)(runId & 0x7FFFFFFF)));

            lock (runLock)
            {
                if (activeRunId == runId)
                {
                    activeIteration = 0;
                    activeStepIndex = 0;
                    activeStepCount = steps.Count;
                    activeHeldInputs.Clear();
                    activeHeldText = "无";
                }
            }

            try
            {
                if (waitForReleaseTrigger != null)
                {
                    SetStatusSafe("准备执行：" + macroName + "（等待触发键松开）");
                    if (WaitForTriggerReleaseStable(stop, waitForReleaseTrigger, PhysicalReleaseSettleMs)) return;
                }

                if (startDelayMs > 0)
                {
                    SetStatusSafe("准备执行：" + macroName + "（等待目标窗口稳定 " + startDelayMs.ToString() + " ms）");
                    if (WaitOrStopWithUiSafety(stop, startDelayMs, macroName, held)) return;
                }

                if (WaitForUiSafetyClear(stop, macroName, held)) return;

                bool hasNaturalDelay = false;
                foreach (MacroStep s in steps)
                {
                    int possibleDelay = s.RandomDelay ? Math.Max(s.RandomDelayMinMs, s.RandomDelayMaxMs) : s.DelayMs;
                    if (possibleDelay > 0 || (s.Action == MacroAction.Press && s.HoldMs > 0))
                    {
                        hasNaturalDelay = true;
                        break;
                    }
                }

                int iteration = 0;
                while (!stop.IsSet)
                {
                    if (!infinite && iteration >= repeatCount) break;
                    iteration++;
                    lock (runLock) if (activeRunId == runId) activeIteration = iteration;
                    SetStatusSafe("正在执行：" + macroName + (infinite ? "（第 " + iteration.ToString() + " 次，无限循环）" : "（" + iteration.ToString() + "/" + repeatCount.ToString() + "）"));

                    for (int stepIndex = 0; stepIndex < steps.Count; stepIndex++)
                    {
                        MacroStep step = steps[stepIndex];
                        if (stop.IsSet) break;
                        lock (runLock)
                        {
                            if (activeRunId == runId)
                            {
                                activeStepIndex = stepIndex + 1;
                                activeStepCount = steps.Count;
                            }
                        }

                        InputSpec input = new InputSpec();
                        input.Kind = step.Kind;
                        input.VirtualKey = step.VirtualKey;
                        input.ScanCode = step.ScanCode;
                        input.Extended = step.Extended;
                        string key = InputKey(input);

                        if (WaitForUiSafetyClear(stop, macroName, held)) break;
                        // Only delay a step when current physical modifiers would create a
                        // high-confidence Windows shortcut. Shift+ordinary game input, mouse
                        // steps, and every key-up remain immediate.
                        if (WaitForDangerousPhysicalShortcutToClear(stop, macroName, held, step)) break;

                        if (step.Action == MacroAction.Press)
                        {
                            if (InputSender.IsHoldable(input))
                            {
                                InputSender.SendDown(input);
                                held[key] = input.Clone();
                                SyncActiveHeldInputs(runId, held);
                                if (WaitOrStopWithUiSafety(stop, Math.Max(0, step.HoldMs), macroName, held)) break;
                                InputSender.SendUp(input);
                                held.Remove(key);
                                SyncActiveHeldInputs(runId, held);
                            }
                            else
                            {
                                InputSender.SendDown(input);
                            }
                        }
                        else if (step.Action == MacroAction.Down)
                        {
                            InputSender.SendDown(input);
                            if (InputSender.IsHoldable(input))
                            {
                                held[key] = input.Clone();
                                SyncActiveHeldInputs(runId, held);
                            }
                        }
                        else
                        {
                            InputSender.SendUp(input);
                            held.Remove(key);
                            SyncActiveHeldInputs(runId, held);
                        }

                        if (stop.IsSet) break;
                        int stepDelay = ResolveStepDelay(step, random);
                        if (WaitOrStopWithUiSafety(stop, stepDelay, macroName, held)) break;
                    }

                    if (infinite && !hasNaturalDelay && !stop.IsSet)
                    {
                        if (WaitOrStopWithUiSafety(stop, 1, macroName, held)) break;
                    }
                }
            }
            catch (Exception ex)
            {
                executionError = ex.Message;
                AppLog.Write("Macro worker failed: " + macroName, ex);
            }
            finally
            {
                foreach (KeyValuePair<string, InputSpec> pair in held)
                {
                    try { InputSender.SendUp(pair.Value); } catch { }
                }
                held.Clear();
                SyncActiveHeldInputs(runId, held);

                bool ownedActiveState = false;
                lock (runLock)
                {
                    if (activeRunId == runId && workerThread == Thread.CurrentThread && stopEvent == stop)
                    {
                        ownedActiveState = true;
                        runningMacro = null;
                        holdControlledMacro = null;
                        stopEvent = null;
                        workerThread = null;
                        activeRunId = 0;
                        activeIteration = 0;
                        activeStepIndex = 0;
                        activeStepCount = 0;
                        activeHeldInputs.Clear();
                        activeHeldText = "无";
                    }
                }
                try { stop.Dispose(); } catch { }

                if (ownedActiveState)
                {
                    if (executionError != null) SetStatusSafe("执行出错：" + executionError);
                    else SetStatusSafe("状态：空闲");
                    UpdateRunButtonSafe();
                }
            }
        }

        private static int ResolveStepDelay(MacroStep step, Random random)
        {
            if (step == null) return 0;
            if (!step.RandomDelay) return Math.Max(0, step.DelayMs);
            int min = Math.Max(0, Math.Min(step.RandomDelayMinMs, step.RandomDelayMaxMs));
            int max = Math.Max(0, Math.Max(step.RandomDelayMinMs, step.RandomDelayMaxMs));
            if (max <= min) return min;
            return random.Next(min, max + 1);
        }

        private void SyncActiveHeldInputs(long runId, Dictionary<string, InputSpec> held)
        {
            lock (runLock)
            {
                if (activeRunId != runId) return;
                activeHeldInputs.Clear();
                List<string> names = new List<string>();
                if (held != null)
                {
                    foreach (KeyValuePair<string, InputSpec> pair in held)
                    {
                        if (pair.Value == null) continue;
                        activeHeldInputs[pair.Key] = pair.Value.Clone();
                        names.Add(InputNames.FormatInput(pair.Value.Kind, pair.Value.VirtualKey));
                    }
                }
                activeHeldText = names.Count == 0 ? "无" : string.Join(" + ", names.ToArray());
            }
        }

        private bool WaitForUiSafetyClear(ManualResetEventSlim stop, string macroName, Dictionary<string, InputSpec> held)
        {
            if (stop.IsSet) return true;
            if (!uiSafetyPauseRequested) return false;

            // Release macro-held inputs while the UI is protected. This prevents a held modifier
            // or mouse button from leaking into the editor. Keep the logical held dictionary so
            // those inputs can be restored when the user leaves the risky control.
            List<InputSpec> restore = new List<InputSpec>();
            if (held != null)
            {
                foreach (KeyValuePair<string, InputSpec> pair in held)
                {
                    if (pair.Value == null) continue;
                    restore.Add(pair.Value.Clone());
                    try { InputSender.SendUp(pair.Value); } catch { }
                }
            }

            string reason = uiSafetyPauseReason;
            if (string.IsNullOrWhiteSpace(reason)) reason = "编辑控件";
            SetStatusSafe("已暂停：" + macroName + "（UI编辑保护：" + Localizer.T(reason) + "）");

            while (!stop.IsSet && uiSafetyPauseRequested)
            {
                if (stop.Wait(10)) return true;
            }
            if (stop.IsSet) return true;

            // Restore only inputs that are still logically held by the macro.
            foreach (InputSpec input in restore)
            {
                string key = InputKey(input);
                if (held != null && held.ContainsKey(key))
                {
                    try { InputSender.SendDown(input); } catch { }
                }
            }
            SetStatusSafe("正在执行：" + macroName + "（UI编辑保护已恢复）");
            return false;
        }

        private bool WaitOrStopWithUiSafety(ManualResetEventSlim stop, int ms, string macroName, Dictionary<string, InputSpec> held)
        {
            if (WaitForUiSafetyClear(stop, macroName, held)) return true;
            if (ms <= 0) return stop.IsSet;

            int remaining = ms;
            while (remaining > 0 && !stop.IsSet)
            {
                if (WaitForUiSafetyClear(stop, macroName, held)) return true;
                int slice = Math.Min(10, remaining);
                if (stop.Wait(slice)) return true;
                // If protection became active during this short slice, do not count the slice
                // toward the macro delay. This keeps timing paused rather than merely suppressing output.
                if (!uiSafetyPauseRequested) remaining -= slice;
            }
            return stop.IsSet;
        }

        private bool WaitForDangerousPhysicalShortcutToClear(ManualResetEventSlim stop, string macroName, Dictionary<string, InputSpec> held, MacroStep step)
        {
            if (step == null || step.Action == MacroAction.Up || step.Kind != InputKind.Keyboard) return stop.IsSet;

            bool observedConflict = false;
            long conflictFreeSince = 0;
            long requiredTicks = PhysicalReleaseSettleMs * Stopwatch.Frequency / 1000L;
            while (!stop.IsSet)
            {
                if (uiSafetyPauseRequested && WaitForUiSafetyClear(stop, macroName, held)) return true;

                HookManager h = hooks;
                int hookMask = h == null ? 0 : h.PhysicalModifierMaskSnapshot;
                int macroHeldMask = GetMacroHeldModifierMask(held);
                // GetAsyncKeyState includes InputStitch's own SendInput. Remove macro-held
                // modifiers from that source, then add back any same modifier proven physical
                // by the low-level hook snapshot.
                int asyncMask = PhysicalInputState.GetModifierMask();
                int physicalMask = hookMask | (asyncMask & ~macroHeldMask);
                int effectiveMask = physicalMask | macroHeldMask;
                int dangerousMask = ModifierSafetyPolicy.GetDangerousPhysicalModifierMask(step, effectiveMask);
                int physicalBlockers = dangerousMask & physicalMask;

                if (physicalBlockers == 0)
                {
                    if (!observedConflict) return stop.IsSet;
                    long now = Stopwatch.GetTimestamp();
                    if (conflictFreeSince == 0) conflictFreeSince = now;
                    else if (now - conflictFreeSince >= requiredTicks) break;
                }
                else
                {
                    conflictFreeSince = 0;
                    if (!observedConflict)
                    {
                        observedConflict = true;
                        Interlocked.Increment(ref dangerousShortcutAvoidanceCount);
                        AppLog.Write("Delayed macro step to avoid a physical shortcut: macro=" +
                            macroName + ", key=" + InputNames.FormatInput(step.Kind, step.VirtualKey) +
                            ", modifierMask=" + physicalBlockers.ToString());
                        SetStatusSafe("正在执行：" + macroName + "（等待避免特殊快捷键冲突）");
                    }
                }
                if (WaitOrStop(stop, 5)) return true;
            }
            if (!stop.IsSet) SetStatusSafe("正在执行：" + macroName);
            return stop.IsSet;
        }

        private static int GetMacroHeldModifierMask(Dictionary<string, InputSpec> held)
        {
            if (held == null || held.Count == 0) return 0;
            int mask = 0;
            foreach (KeyValuePair<string, InputSpec> pair in held)
            {
                InputSpec input = pair.Value;
                if (input == null || input.Kind != InputKind.Keyboard) continue;
                int vk = input.VirtualKey;
                if (vk == (int)Keys.LControlKey || vk == (int)Keys.RControlKey || vk == (int)Keys.ControlKey)
                    mask |= ModifierSafetyPolicy.Ctrl;
                else if (vk == (int)Keys.LShiftKey || vk == (int)Keys.RShiftKey || vk == (int)Keys.ShiftKey)
                    mask |= ModifierSafetyPolicy.Shift;
                else if (vk == (int)Keys.LMenu || vk == (int)Keys.RMenu || vk == (int)Keys.Menu)
                    mask |= ModifierSafetyPolicy.Alt;
                else if (vk == (int)Keys.LWin || vk == (int)Keys.RWin)
                    mask |= ModifierSafetyPolicy.Win;
            }
            return mask;
        }

        private static bool WaitForTriggerReleaseStable(ManualResetEventSlim stop, TriggerSpec trigger, int settleMs)
        {
            long releasedSince = 0;
            long requiredTicks = Math.Max(0, settleMs) * Stopwatch.Frequency / 1000L;

            while (!stop.IsSet)
            {
                if (!PhysicalInputState.IsTriggerTerminalReleased(trigger))
                {
                    releasedSince = 0;
                }
                else
                {
                    long now = Stopwatch.GetTimestamp();
                    if (releasedSince == 0)
                    {
                        releasedSince = now;
                    }
                    else if (now - releasedSince >= requiredTicks)
                    {
                        return false;
                    }
                }

                if (stop.Wait(5)) return true;
            }
            return true;
        }

        private static string InputKey(InputSpec input)
        {
            return ((int)input.Kind).ToString() + ":" + input.VirtualKey.ToString() + ":" + input.ScanCode.ToString();
        }

        private static bool WaitOrStop(ManualResetEventSlim stop, int ms)
        {
            if (ms <= 0) return stop.IsSet;
            return stop.Wait(ms);
        }

        private void SetStatusSafe(string text)
        {
            if (IsDisposed) return;
            try
            {
                if (InvokeRequired) BeginInvoke((MethodInvoker)delegate { if (!IsDisposed) statusLabel.Text = text; });
                else statusLabel.Text = text;
            }
            catch { }
        }

        private void UpdateRunButtonSafe()
        {
            if (IsDisposed) return;
            try
            {
                if (InvokeRequired) BeginInvoke((MethodInvoker)delegate { UpdateRunButton(); });
                else UpdateRunButton();
            }
            catch { }
        }

        private void UpdateRunButton()
        {
            if (runButton == null || runButton.IsDisposed) return;
            bool alive = false;
            bool stopping = false;
            lock (runLock)
            {
                alive = workerThread != null && workerThread.IsAlive;
                stopping = alive && stopEvent != null && stopEvent.IsSet;
            }
            if (stopping)
            {
                runButton.Text = Localizer.T("■ 正在停止…");
                runButton.ForeColor = Color.DarkOrange;
                runButton.Enabled = true;
            }
            else if (alive)
            {
                runButton.Text = Localizer.T("■ 停止当前宏");
                runButton.ForeColor = uiSafetyPauseRequested ? Color.DarkOrange : Color.Firebrick;
                runButton.Enabled = true;
            }
            else
            {
                runButton.Text = Localizer.T("▶ 执行所选宏");
                runButton.Enabled = !recordingActive && SelectedMacro != null;
                runButton.ForeColor = runButton.Enabled ? Color.ForestGreen : SystemColors.GrayText;
            }
            runButton.BackColor = SystemColors.Control;
        }

        private void UpdateRecordButton()
        {
            if (recordButton == null || recordButton.IsDisposed) return;
            if (recordingActive)
            {
                recordButton.Text = Localizer.T("■ 停止录制");
                recordButton.ForeColor = Color.Firebrick;
                recordButton.Enabled = true;
            }
            else
            {
                recordButton.Text = Localizer.T("● 录制宏");
                recordButton.Enabled = SelectedMacro != null;
                recordButton.ForeColor = recordButton.Enabled ? Color.RoyalBlue : SystemColors.GrayText;
            }
            recordButton.BackColor = SystemColors.Control;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                CommitNameEdit();
                if (recordingActive) StopMacroRecording(false, true);
                CancelCapture();
                uiSafetyPauseRequested = false;
                uiSafetyPauseReason = "";
                if (foregroundTimer != null)
                {
                    foregroundTimer.Stop();
                    foregroundTimer.Dispose();
                    foregroundTimer = null;
                }
                StopCurrentMacro();
                ForceReleaseActiveHeldInputs();
                Thread t = workerThread;
                if (t != null && t.IsAlive) t.Join(800);
                SaveConfig();
                if (hooks != null)
                {
                    hooks.Dispose();
                    hooks = null;
                }
                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                    trayIcon = null;
                }
                if (trayMenu != null) { trayMenu.Dispose(); trayMenu = null; }
                if (toolsMenu != null) { toolsMenu.Dispose(); toolsMenu = null; }
                AppLog.Write("Application closed normally.");
            }
            catch (Exception ex)
            {
                AppLog.Write("Shutdown cleanup failed", ex);
            }
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (UpdateManager.TryApplyPendingUpdate(args)) return;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
            {
                try { AppLog.Write("Unhandled UI exception", e.Exception); } catch { }
                try
                {
                    LocalizedMessageBox.Show("InputStitch 遇到未处理的界面错误。\r\n\r\n" + e.Exception.Message +
                        "\r\n\r\n日志：" + AppLog.LogPath, AppInfo.ProductName,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch { }
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                try { AppLog.Write("Unhandled background exception", e.ExceptionObject as Exception); } catch { }
            };
            try { AppPaths.EnsureDirectories(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}

