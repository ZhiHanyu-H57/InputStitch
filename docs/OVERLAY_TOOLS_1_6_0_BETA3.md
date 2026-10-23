# InputStitch 1.6.0-beta.3 — 独立网络控制与计时器分段

2026-10-24，本地测试版。实现提交：`30f99034988ee6a6a081adf3b79a380cdf0aae31`。

## 使用

“断网与恢复”分别提供断开、恢复快捷键。重复按断开键保持断开；恢复键用于恢复。
两个键也可以设成同一组合，继续按一次切换。旧配置的单一快捷键沿用这种方式。
断开和恢复各有可独立关闭的音效及试听按钮；仅在确认状态改变后提示。
取消授权、重复按键及启动程序不播放状态音效。声音由程序生成，没有外部音频文件。

计时器“内容与样式”页列出时、分、秒、毫秒和自定义文字。勾选控制显示，
选中一段后单独编辑字体、字号、粗体、斜体、描边、颜色、字距及不透明度。
可以添加文字、修改内容类型、调整顺序和毫秒位数；文字支持中英混排及换行。
提供“分:秒.毫秒”“时:分:秒.毫秒”“秒.毫秒”预设。
隐藏小时后分钟累计，例如 1 小时 1 分 1.123 秒显示为 `61:01.123`。
不同字号共用基线，整体不透明度与各段不透明度叠加生效。

旧计时器模板保留原显示方式。只改位置、快捷键等设置不会强制转换；编辑内容或样式后
保存为分段。保存、取消、退出再开和克隆均已验证。普通置顶与 RTSS 均支持分段。
运行测试版前正常退出当前 InputStitch；各版本共用用户配置。正式更新源保持 1.5.2。

## 自动化结果

按改动范围执行 **9／42 组**回归及 SettingsSmoke，通过后完成以下检查：

| 项目 | 实际结果 |
|---|---|
| 计时器核心 | 97 项；格式、样式、旧 XML、真实配置重开与克隆、缓存和释放通过 |
| 网络控制核心 | 29 组；独立命令幂等、旧键兼容、音效触发／静音、PCM 解析和故障路径通过 |
| 工具集成 | 153 项；两键冲突、等待授权时恢复、编辑保护、同键切换及保存取消通过 |
| 中英文界面 | 37,689 项、32 组窗口；100%、125%、150%、200% 及窗口缩小后通过 |
| 默认窗口密度 | 全部样式控件、预设加文字的内容列表、网络设置均可直接看全 |
| 原有宏创建到运行 | 142 项，隔离编辑器和运行观察器，通过 |
| 宏运行相关 | UI 安全、输入协调、运行协调和生产力配置检查通过；宏引擎及输出代码未改 |
| 实际桌面绘制 | x64、x86 各 109 项；分段显示、显隐、鼠标穿透、不抢焦点及释放通过 |
| 打包程序 | 两种架构的实际消息循环、独立样式和双快捷键恢复、启动不阻断网络、退出释放通过 |

完整正式发布回归留待正式发布。复验命令：

```powershell
.\build.ps1 -SkipSourceArchive
.\tests\Run-Tests.ps1 -Suite SourceManifestTests,TimerTests,NetworkControlTests,ToolIntegrationTests,UiSafetyTests,InputPipelineIntegrationTests,MacroRunCoordinatorTests,ProductivityTests,ReleasePolicyTests -SettingsSmoke
.\artifacts\tests\ToolIntegrationTests.exe --ui
.\tests\Run-ProductAcceptance.ps1 -JourneyOnly
.\tests\Run-TimerAcceptance.ps1 -Backend Desktop -Architecture x64
.\tests\Run-TimerAcceptance.ps1 -Backend Desktop -Architecture x86
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File .\tests\Run-PackagedSmoke.ps1 -Architecture x64
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File .\tests\Run-PackagedSmoke.ps1 -Architecture x86
```

## 实际 RTSS／OBS 与隔离网络

OBS 32.2.2 的 `game_capture`、原生 x64 D3D9 目标、当前生产计时器：
4 段 MKV 共 48 帧通过。较小、斜体的毫秒与大字一同被 RTSS 绘制和录入。
普通置顶计时器在桌面正对照中可见，在游戏捕获中未出现。
关闭第三方叠加层、只开普通计时器的对照通过；黄色文字 12 帧有 12 个不同掩模，
证明时间持续更新。最终录制的输出及渲染跳帧均为 0。

专用测试目标临时启用了 RTSS Microsoft Detours，完成后已删除该 profile。
现有 RTSS 两份配置及 OBS 的 17 份配置哈希一致。直接 D3D9 检查的独立基准文字
未显示，该轮记为 BLOCKED；实际绘制结论来自原生 OBS 捕获及桌面正对照。
记录工具的 ffmpeg 路径修正后重跑了完整流程。复验：[OBS 自动验收](OBS_TIMER_ACCEPTANCE.md)。

隔离 Windows Server 2022 来宾的真实 WFP 验收 **304 项通过**，约 100.7 秒。
覆盖程序 A 被阻断／B 保持连通，以及整台来宾主动恢复、正常退出、实际强制结束
主进程、实际强制结束网络助手。每项检查 IPv4／IPv6、TCP／UDP、新连接及已有流；
恢复后可重连，原有 UDP 套接字恢复。五个场景确认重复断开不会恢复网络。
宿主整机阻断为 0，HTTPS 检查返回 200。真实网络助手代码未改；NGEN 仅用于来宾
验收程序预编译。来宾、回声端、OBS、目标及计时器测试进程均已退出。
复验：[隔离网络验收](ISOLATED_NETWORK_ACCEPTANCE.md)。

## 开销和文件

66 个生产源码中修改 8 个，没有新增生产源码、驱动或运行依赖。
桌面分段缓存连续 60 秒观察，每例 600 帧，无强制 GC：

| 样例 | 每帧平均绘制 | 单核 CPU | GDI 起止 | 缓存条目 |
|---|---:|---:|---:|---:|
| 默认分段 | 1.019 ms | 1.614% | 3→3 | 12 |
| 96px 双语、36px 毫秒 | 4.122 ms | 7.965% | 3→3 | 43 |

数字复用字形缓存，RTSS 各段共用 PNG 图集；100 次更新不重写 PNG。
其他 RTSS 客户端文字和字体保持一致。桌面与 RTSS 软件采样的 6,689 个可见像素
轮廓差异为 0。CPU 数值为进程消耗相对一个核心的比例。

| 程序 | 大小 | 相对 beta.2 增量 | SHA-256 |
|---|---:|---:|---|
| x64 InputStitch.exe | 2,188,288 B（2.09 MiB） | 69.5 KiB | `0155C37CFCB8C88BF71FD17706470500191FBE9EA4C2D9A742E7210FF016FDAC` |
| x86 InputStitch.exe | 2,003,968 B（1.91 MiB） | 55.5 KiB | `2D0F8826FD99790EF019FCB5D68C21E6A63D93ACD4E7810EAB85555A922C55DA` |

产物：聊天工作区 `outputs/beta160-3`。包含程序、日志、中英文截图、OBS 实录及
脱敏来宾网络证据；不含授权令牌或 OBS 凭据。未推送或发布至 GitHub。
