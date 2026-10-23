# InputStitch 1.6.0-beta.1

本地功能测试版；GitHub 正式版保持 1.5.2。

- 准星增加“缺角方环”，大小、线宽和四角缺口可调。
- 屏幕计时器支持开始、暂停、归零、显隐及快捷键；字体、字号、粗斜体、描边、字距、颜色、不透明度、位置和中英文字可保存。
- 计时器可使用普通置顶窗口或可选 RTSS。OBS 游戏捕获中录入 RTSS 需要开启“捕获第三方叠加层”，并取决于游戏兼容性。
- 网络开关支持指定程序路径或整机，按快捷键断开与恢复。只提权网络助手；退出自动解除临时限制，启动不自动断网。
- 新功能默认关闭，旧宏配置可继续使用。

验证范围与使用说明见 docs/OVERLAY_TOOLS_1_6_0_BETA1.md。

---

# InputStitch 1.5.2

修复鼠标按键选择与录制，完善宏步骤的批量时间编辑。

## 本次更新

- 触发键、录制和执行步骤可选择鼠标左键、右键、中键、前后侧键和滚轮方向，支持键盘修饰键组合。
- 录制可直接切换到其他应用使用，无需设置目标窗口；开始录制时会结束之前未完成的单键录入。
- 选中多个步骤后，可统一修改按住时长、等待时长或随机等待范围，也可应用到全部步骤；支持撤销和重做。
- 添加步骤时，实体按键录入与虚拟键盘／鼠标选择可随时切换。鼠标示意图采用白色外观，并标明侧键位置。

批量修改按住时长适用于自动松开的“按下并松开”步骤；分开的“按下／松开”步骤可通过中间的等待时间调整持续时间。

## 下载与升级

- 64 位 Windows 下载 **InputStitch-1.5.2-Windows-x64.exe**；32 位 Windows 下载 x86。便携单文件程序，需 .NET Framework 4.7.2 或兼容的更高版本。
- 关闭旧版后启动新版，已有宏和设置自动读取。软件内更新后的文件名固定为 **InputStitch.exe**，更新前备份原程序和配置，失败时回滚。
- 键鼠宏直接使用；虚拟手柄输出需 ViGEmBus。准星适用于窗口与无边框窗口。
- 提供完整源码、正式更新清单和 SHA-256 校验文件。

验证结果见 [1.5.2 正式验收记录](https://github.com/ZhiHanyu-H57/InputStitch/blob/main/docs/RELEASE_1_5_2_VERIFICATION.md)。

---

This release restores mouse selection for triggers, recording and macro steps,
including left/right/middle buttons, side buttons, wheel directions and modifier
combinations. Recording works in other applications without configuring a target
window and clears unfinished single-input capture when it starts.

Batch timing edits apply to selected steps or the entire macro, with independent
hold and fixed/random wait settings, undo and redo. Hold duration applies to
automatic-release steps; separate down/up steps use intervening wait times.
Physical capture and the virtual keyboard/mouse selector remain available
together. The mouse diagram now uses a white product image with labeled controls.

Existing settings load automatically. In-app updates use **InputStitch.exe**,
preserve executable/configuration backups and roll back on failure. Choose x64
for 64-bit Windows; .NET Framework 4.7.2 or later is required. See the linked
verification record for automated release results.
