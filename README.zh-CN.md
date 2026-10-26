# InputStitch

[English](README.md) | 简体中文

Windows 键盘、鼠标与手柄宏工具。可以录制操作、编辑步骤、设置触发键，也可使用屏幕准星、计时器和断网快捷键。界面支持简体中文和 English。

## 下载 1.6.1

| 系统 | 下载 |
| --- | --- |
| 64 位 Windows（通常选这个） | [Windows x64](https://download.zhihanyu.com/latest/InputStitch-Windows-x64.exe) |
| 32 位 Windows | [Windows x86](https://download.zhihanyu.com/latest/InputStitch-Windows-x86.exe) |
| 源码与其他版本 | [GitHub Releases](https://github.com/ZhiHanyu-H57/InputStitch/releases) |

下载后放入常用文件夹，双击 EXE 运行。系统需要 .NET Framework 4.7.2 或兼容的更高版本。

1.6.1 修复鼠标触发拦截软件界面点击，以及按住 Shift 时 PageDown 等单键无法触发的问题。[查看更新记录](RELEASE_NOTES.md)。

## 创建第一个宏

以“按 F8 开始重复点击，再按 F8 停止”为例：

1. 点击左侧“＋ 新建宏”，选择“重复操作”，填写名称。
2. 点击“触发键”并按 F8，也可通过旁边的下拉按钮打开虚拟键盘。
3. “触发方式”选择“按一次启动，再按停止”，勾选“无限循环”。
4. 添加鼠标左键步骤，操作选“按一下”，例如按住 25 ms、等待 120 ms。
5. 设置自动保存。切到要操作的窗口，按 F8 开始，再按一次停止。

底部“全部停止”或默认紧急停止键 **Ctrl + Shift + F12** 可停止运行并释放输出。紧急停止还会暂停全局宏触发；在齿轮菜单取消“暂停全局宏触发”后恢复。

## 录制与编辑

- 点击“录制宏”，切到要操作的程序进行键盘、鼠标按键或滚轮操作，再返回 InputStitch 停止录制。录制可直接使用，无需设置目标窗口；鼠标移动不录入。
- 手动添加步骤时，可直接录入按键，或用虚拟键盘、鼠标示意图选择；侧键 X1/X2 分别对应鼠标的两个侧键。
- 双击步骤编辑。按 Ctrl / Shift 多选步骤，打开“批量时间”，可统一修改按住时长、等待时长，或两者一起改。
- “按住运行，松开停止”适合按住映射；“按顺序执行”适合组合动作。执行次数和无限循环在主界面设置。

[宏设置与编辑说明](https://inputstitch.zhihanyu.com/guide#steps)

## 屏幕计时器

1. 点击左侧“计时器…”，勾选“启用计时器”和“显示计时器”。
2. 在“内容与样式”选择“分:秒.毫秒”等预设。左侧勾选框控制各部分显隐；选中一行后，在右侧编辑内容与格式。
3. 在“显示与位置”选择“普通置顶窗口”，用滑块粗调位置、X/Y 数字框逐像素微调。
4. 在“快捷键”选择控制方式并设置按键，然后保存。

单键循环依次执行“开始并显示 → 暂停 → 归零并隐藏”；也可选双键、三键，或“自定义”，为未开始、计时中和已暂停分别设置动作。

需要统一格式时，Ctrl / Shift 多选或点“全选”，再改字体、字号、颜色、描边、字距等。“套用首项样式”复制第一个选中部分的完整格式。用“添加文字”加入中英文，用“换行”分行，在“显示与位置”调行距与刷新间隔。

OBS **游戏捕获**录入计时器时，选择“RTSS 游戏内绘制”：先安装并启动 [RTSS 7.3.7+](https://www.guru3d.com/download/rtss-rivatuner-statistics-server-download/)，在 RTSS Setup 开启 **Use Microsoft Detours API hooking**，并在 OBS 游戏捕获属性中开启“捕获第三方叠加层”。普通置顶计时器适合屏幕上自用；OBS 显示器捕获会录入它。

[计时器完整步骤与快捷键对照](https://inputstitch.zhihanyu.com/guide#timer)

## 断网与恢复

1. 打开齿轮菜单 →“断网与恢复…”，勾选“启用网络控制快捷键”。
2. 选择“指定程序”，点击“添加程序…”选择实际运行的 EXE，可添加多个；或选择“整台电脑”。
3. 分别设置断开与恢复快捷键，保存后使用。两个键相同时，按一次断开，再按一次恢复。首次断网时允许 Windows 管理员授权。
4. 在“音效”分别开关提示音、调整音量，也可导入 8/16 位 PCM WAV 并试听。

按恢复快捷键，或在齿轮菜单点“恢复网络”即可恢复。紧急停止和退出 InputStitch 也会解除断网；下次启动保持联网。

[断网设置步骤](https://inputstitch.zhihanyu.com/guide#network)

## 屏幕准星

点击左侧“屏幕准星…”，开启绘制并选择样式。**缺角方环**是四角留有缺口的正方形环，用“大小”“线宽”“四角缺口”调整形状，再设置颜色、描边和不透明度。

准星默认位于所选显示器中央，X/Y 偏移可逐像素调整。点击“保存准星设置”保留设置，默认用 **Ctrl + Alt + F10** 显隐。游戏请使用窗口化或无边框窗口模式。

[准星设置步骤](https://inputstitch.zhihanyu.com/guide#crosshair)

## 手柄、备份与更新

- 键盘和鼠标宏可直接使用。虚拟 Xbox 360 / DualShock 4 输出需要 [ViGEmBus](https://github.com/nefarius/ViGEmBus/releases/latest)。在设置中选择手柄类型并连接；仅用键鼠时可选“不创建虚拟手柄”。[手柄设置](https://inputstitch.zhihanyu.com/guide#gamepad)
- 在“导入与备份”导出宏或配置方案，用于备份、分享和换电脑。配置位于 `%APPDATA%\InputStitch`。
- 在设置中检查更新，或关闭旧程序后运行新版。1.4.3 及 1.5.x 的宏与设置自动读取。软件内更新使用固定文件名 `InputStitch.exe`，更新前备份原程序和配置。

[官网与使用指南](https://inputstitch.zhihanyu.com/guide) · [问题反馈](https://github.com/ZhiHanyu-H57/InputStitch/issues) · [参与开发](CONTRIBUTING.md)

配置、宏和日志保存在本机。更新检查可在设置中选择自动、手动或关闭。

## 许可证

本仓库未声明开源许可证。第三方依赖的许可证与声明见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。
