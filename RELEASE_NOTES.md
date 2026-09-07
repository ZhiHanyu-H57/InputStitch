# InputStitch 1.2.0

## 简体中文

InputStitch 1.2.0 将 1.1.1 Beta 1–4 中经过实际使用和回归验证的可靠性、配置安全和易用性改进正式晋升为 Stable。

### 配置与安全

- `config.xml` 采用暂存写入、刷新到磁盘、重新反序列化验证后再替换主配置；失败时尽量保留上一份有效配置。
- 自动保留最近 **5 份有效主配置备份**；保存失败会明确显示“未保存”。
- 强化前台/后台切换、修饰键状态和 Held Mapping 释放逻辑；Alt+Tab 等情况下即使丢失 KeyUp，也有物理状态兜底释放。
- Emergency Stop、编辑保护和有限运行诊断继续保留。

### 更容易创建和编辑宏

- “新增”菜单提供 **按住映射 / 定次数连按 / 顺序执行** 三种快捷创建方式，同时保留空白宏高级编辑。
- Held Mapping 支持普通键盘键、鼠标按钮和**单独的左右 Ctrl / Shift / Alt / Win**。
- 原始触发输入继续透传；多个已启用宏共用同一触发键时，**宏列表靠上的优先**，↑ / ↓ 会立即改变优先级。
- 步骤编辑支持 Undo / Redo，覆盖添加、删除、编辑、移动和批量操作，最多 50 次历史。

### 闲置自动手柄输入

- 真实鼠标移动改由 **Raw Input** 判断，游戏自己锁定/重定位光标不会再被误认为持续操作。
- “设置 → 自动化”中加入独立的**挂机目标**，不依赖也不会修改主界面的目标窗口、UI 运行切换或自动方案切换。
- 设置挂机目标后，在浏览器、聊天软件等其他程序中打字或移动鼠标，不会阻止后台目标程序按时收到挂机手柄脉冲。
- 已设置的挂机目标不存在时，闲置输出完全暂停；目标重新出现后从完整空闲周期重新计时。
- 正式默认值：**关闭、120 秒、左摇杆向下、150 ms**。已有用户保存的自定义值不会被覆盖。
- 窄设置窗口下，过长目标名称保持单行省略显示，按钮不会被覆盖；悬停可查看完整名称。

### 升级提示

- 已经使用过 InputStitch 的用户，在升级到新版本后首次启动时，会看到一次简短的“本次更新内容”摘要。
- 同一个版本只显示一次；全新安装仍只显示普通 Welcome，不会连续弹两个提示框。

### 更新与发布

- 1.2.0 恢复 Stable 自动更新通道，正式清单为 `InputStitch-update.xml`，下载仍执行 SHA-256 校验。
- Beta 以后继续使用独立 prerelease / `InputStitch-beta.xml`，不会混入 Stable 更新提醒。

### 验证与已知边界

当前源码通过：323 项 keyboard checks、58 项 idle-gamepad assertions、43 项 updater checks、23 项 UI safety/diagnostics checks、348 项 productivity checks，以及中英文 Settings / 旧配置兼容 / 手柄参数 smoke tests。

自动测试不会发送真实键鼠输入或创建真实虚拟手柄，因此不能证明所有游戏兼容。当前仍保持**普通时序宏一次只运行一个**；多来源 Held Mapping / Input Ownership 将进入 1.3.0 开发周期。

虚拟手柄输出仍依赖 ViGEmBus；EXE 未签名。

## English

InputStitch 1.2.0 promotes the reliability, configuration-safety and productivity work validated through the 1.1.1 Beta 1–4 cycle to Stable.

### Configuration and safety

- `config.xml` is staged, flushed and deserialized for verification before the main file is replaced; failures preserve/recover the previous valid configuration when possible.
- Keep the five most recent valid main-config backups and surface an explicit Not saved warning on failure.
- Harden foreground/background, modifier-state and Held Mapping release behavior. A physical-state fallback releases Hold mappings even if a KeyUp is lost around Alt+Tab.
- Emergency Stop, edit protection and bounded runtime diagnostics remain in place.

### Easier creation and editing

- New Quick Create options: **Held Mapping / Repeat a fixed number of times / Sequence**, while preserving Blank macro advanced editing.
- Held Mapping accepts normal keyboard keys, mouse buttons and **standalone left/right Ctrl / Shift / Alt / Win**.
- The original physical trigger passes through. When multiple enabled macros share a trigger, the **first macro in list order wins**; Up / Down changes that priority immediately.
- Step Undo / Redo covers add, delete, edit, move and batch operations with a 50-operation history cap.

### Idle gamepad input

- Physical mouse movement uses **Raw Input**, so games that lock/recenter the cursor no longer fake continuous user activity.
- Settings → Automation now has an independent **Idle target** that does not depend on or modify the main target window, UI-run activation or automatic profile switching.
- Typing/moving the mouse in other apps does not block the background target's idle pulse.
- If a configured Idle target is absent, idle output pauses completely; when it returns, a fresh full idle interval begins.
- Formal defaults: **off, 120 seconds, left stick down, 150 ms**. Existing saved custom values are not overwritten.
- Long target names ellipsize on one line in narrow Settings windows; buttons remain visible and a Tooltip exposes the full name.

### Upgrade summary

- Existing users see a short What's new summary once after upgrading to a new version.
- The same version is never shown twice; fresh installs still show only the normal Welcome dialog.

### Updates and release channel

- Stable 1.2.0 uses `InputStitch-update.xml` and the existing SHA-256-verified Stable update path.
- Future Beta builds remain separate prereleases with `InputStitch-beta.xml` and do not replace Stable update notifications.

### Verification and limits

The current source passes 323 keyboard checks, 58 idle-gamepad assertions, 43 updater checks, 23 UI-safety/diagnostic checks, 348 productivity checks, plus bilingual Settings / legacy-config / gamepad-vector smoke tests.

Automated tests send no real keyboard/mouse input and create no real virtual controller, so universal game compatibility is not claimed. Ordinary timed macros remain single-active; multi-source Held Mapping / Input Ownership is planned for the 1.3.0 development cycle.

Virtual gamepad output still requires ViGEmBus. The executables are unsigned.

## Files / 文件

- InputStitch-1.2.0-Windows-x64.exe
- InputStitch-1.2.0-Windows-x86.exe
- InputStitch-1.2.0-Source.zip
- InputStitch-update.xml
- SHA256SUMS.txt
