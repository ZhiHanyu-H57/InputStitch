# InputStitch 1.3.1-beta.2

## 一句话看懂

**现在映射层可以自由新增、命名、删除和设置快捷切换；实验性手柄接管也会在运行中持续自检，发现关键状态异常时自动退出接管并尝试恢复原手柄。**

这是基于 `v1.3.1-beta.1` 继续开发的测试版。它不会替换正式版 `v1.3.0`，也不会修改正式版的自动更新通道。

## 这次最主要的变化

### 1. 映射层不再只有“基础层 + 映射层 1”

现在可以在主界面的“管理映射层”中：

- 新增任意数量的映射层；
- 给每个层起自己的名字；
- 重命名已有层；
- 删除不再需要的层；
- 为每个层设置切换快捷键；
- 给基础层本身设置“回到仅基础层”的快捷键。

每一个宏都可以选择属于哪个层，包括普通时序宏、按一次启动/停止的宏、复杂按住宏和轻量持续按住映射。

基础层始终有效；除此之外当前仍一次只激活一个额外层。切换层时，旧层正在运行的宏会停止，而且只释放旧层自己的输出。基础层和其他仍有效来源不会被误伤。

如果切入新层时某个触发键本来就已经按着，InputStitch 不会因为键盘自动重复或手柄轮询而突然启动宏；必须先松开，再重新按下。

### 2. Layer 可以用键盘、鼠标或手柄快捷切换

每个层都可以录制自己的切换方式，使用的是 InputStitch 已有的统一触发系统，因此可以是：

- 键盘键或组合键；
- 鼠标按钮；
- 手柄按键；
- 手柄扳机。

层切换本身不创建第二套宏执行器。它只改变“哪些宏当前有资格被触发”，之后仍然使用同一套并发运行、输出合并、停止和释放机制。

### 3. 删除 Layer 后不会再在重启时“自己复活”

开发中发现并修复了一个实际配置问题：旧的 XML 反序列化方式会先构造默认的“基础层 + 映射层 1”，再把保存文件里的层追加进去。因此即使用户已经删除“映射层 1”，重新启动后它仍有可能再次出现。

现在 Layer 列表使用替换式 XML 序列化：

- 真正的旧配置里完全没有 Layer 数据时，仍自动迁移为“基础层 + 映射层 1”；
- 新版配置一旦已经保存 Layer 列表，文件里的列表就是权威数据；
- 用户删除“映射层 1”后，保存、退出、重新启动，它不会再被默认值偷偷创建回来；
- 用户删除某个自定义层时，其中的宏会安全移回基础层，不会留下失效的层 ID。

这条行为已经用真实的 XML 保存 → 重新加载往返测试锁住。

### 4. 实验性手柄接管现在会持续自检

`v1.3.1-beta.1` 已经能在进入 HidHide 隐藏之前安全取得 XInput 0 号槽位，并在失败时回滚。`beta.2` 继续补上“接管已经运行以后怎么办”。

接管激活后，InputStitch 会在后台持续检查：

- InputStitch 虚拟 Xbox 是否仍然在目标 0 号槽位；
- 手柄汇总 Router 是否仍然就绪；
- 接管前记录的外部 XInput 来源是否仍然能被 InputStitch 自己读取；
- HidHide 总隐藏开关是否仍然开启；
- 用户选择接管的原手柄是否仍在隐藏列表；
- InputStitch 自己是否仍在 HidHide 应用白名单中。

这些检查不放在 10 ms 的 UI/手柄轮询线程里，避免 HidHide CLI 或设备枚举拖慢界面。健康检查在后台运行。

一次瞬时失败不会立即退出接管；当前需要**连续两次**检查失败才确认异常，减少设备枚举短暂抖动造成的误判。

### 5. 接管健康异常会自动脱离，而不是继续带病运行

如果连续健康检查确认异常，InputStitch 会：

1. 停止健康监控，避免重复恢复；
2. 调用接管事务恢复 InputStitch 自己造成的 HidHide 隐藏、总开关和白名单变化；
3. 停止手柄汇总 Router；
4. 清理 Router/宏当前持有的受管输出并让虚拟手柄回到中立；
5. 把“手柄汇总”配置关闭，避免原手柄重新可见后又同时发送一份聚合输出，造成双输入；
6. 记录明确的运行日志和状态提示。

如果恢复本身失败，恢复日志会继续保留，下次启动仍会尝试恢复，并向用户显示警告。

### 6. 运行观察和诊断信息更完整

“工具 → 运行观察”现在会直接显示：

- 当前活动映射层的用户名称；
- InputStitch 虚拟 Xbox 当前槽位；
- 手柄汇总是否开启、当前路由了几个来源；
- 手柄接管当前状态；
- 接管健康监控是否正在运行；
- 最近一次健康检查结果；
- 连续失败次数；
- 接管事务预期仍可见的 XInput 来源。

详细诊断信息还会列出：

- 每一个映射层的 ID、名称和切换快捷键；
- 接管恢复日志状态；
- 0 号槽位取得恢复日志状态；
- Router、接管和健康监控的内部状态。

诊断本身不会为了“查看状态”而创建或连接虚拟手柄。隔离测试宿主也不会因为打开运行观察而加载 ViGEm。

### 7. 配置备份在快速连续保存时也会稳定保留真正最近的版本

GitHub Windows runner 在发布验证中暴露出一个原有边界问题：Windows 的墙钟分辨率可能让多个极快连续保存得到完全相同的时间串。旧备份名在这种情况下只能靠后面的随机 GUID 区分，而清理逻辑又按文件名排序，因此“最近 5 个”可能偶尔删错一个较新的版本。

现在有效配置备份使用**严格单调递增**的排序时间戳。生成新备份名时会同时考虑当前 UTC、本进程刚分配过的最新时间以及备份目录中现存的最新时间，因此即使系统时钟粒度较粗、短暂回拨或程序重新启动，也不会让随机 GUID 决定备份新旧顺序。

自动测试会故意把连续保存的墙钟时间冻结成完全相同的值，确认仍只保留正确的最近 5 个版本。

## beta.1 已有能力继续保留

本版本继续包含 `v1.3.1-beta.1` 的这些能力：

- 用可见 XInput 手柄按键/扳机直接触发宏；
- 把手柄输入转换成键盘、鼠标、虚拟手柄或混合操作；
- 把多个其他可见 XInput 手柄汇总到 InputStitch 自己的虚拟 Xbox；
- 设置中可以选择“不创建虚拟手柄”；
- 实验性接管在任何 HidHide 隐藏之前先安全取得并硬验证 XInput 0 号槽位；
- 临时槽位重排和 HidHide 隐藏使用独立的恢复事务；
- 更新器在检查/下载阶段遇到临时断网会有限重试，半包不会进入安装事务；完整下载并验证之后的安装完全离线完成。

## 仍然没有完成的硬件验收

当前开发机没有实体 XInput 手柄，也没有 HidHide 环境，因此：

**“实体手柄 + HidHide + 实际游戏最终只看到 InputStitch 的受控 0 号虚拟手柄”仍然没有真实硬件验收。**

这个限制不会用更多 ViGEm 虚拟手柄伪装成“实体设备验收完成”。已有四槽位中立 ViGEm 探针只证明 Windows/ViGEm 的槽位重新枚举顺序：

`外部 0/1/2 + InputStitch 3 → InputStitch 0 + 外部 1/2/3`

等以后有实体手柄时再补最终 HidHide + 游戏测试。

## 自动测试

本版本在发布前要求至少通过：

- 323 项键盘与触发测试；
- 58 项闲置手柄测试；
- 26 项 XInput 输入测试；
- 28 项多手柄汇总测试；
- **52 项接管安全 / 运行健康测试**；
- 27 项 0 号槽位取得与恢复测试；
- 15 项“不创建虚拟手柄”偏好测试；
- **39 项 Layer / 多层 / XML 往返 / 运行观察测试**；
- 7 项宏计时测试；
- 43 项更新安装测试；
- 18 项更新网络中断/重试测试；
- 23 项界面安全与诊断测试；
- 360 项配置与易用性测试；
- **303,716 项输出合并与并发运行测试**；
- 中英文设置界面与旧 XML 配置冒烟测试。

自动测试使用注入状态或假后端，不会为了测试接管而禁用真实设备或调用真实 HidHide。

## 发布文件

- `InputStitch-1.3.1-beta.2-Windows-x64.exe`
- `InputStitch-1.3.1-beta.2-Windows-x86.exe`
- `InputStitch-1.3.1-beta.2-Source.zip`
- `InputStitch-beta.xml`
- `SHA256SUMS.txt`

---

# English summary

**Mapping layers can now be freely added, named, deleted and bound to switch shortcuts; experimental controller takeover also monitors its runtime safety state and automatically disengages/restores on confirmed failures.**

Key changes:

- Base remains always eligible, with any number of additional named layers.
- Every layer, including Base, may have a keyboard, mouse, or controller switch trigger.
- Deleting a layer moves its macros back to Base.
- The Layer XML model now replaces serialized layer lists rather than appending into constructor defaults, so a deleted legacy Layer 1 does not reappear after restart while truly old configs still migrate to Base + Layer 1.
- Active takeover health monitoring checks slot 0, Router readiness, expected XInput source visibility, HidHide cloak state, selected hidden-device membership, and the InputStitch HidHide application whitelist.
- One transient health failure is tolerated; two consecutive failures trigger one fail-safe disengage.
- Confirmed failure restores InputStitch-owned HidHide changes, disables Router, clears managed routed output, and keeps recovery data if restoration cannot complete.
- Runtime observation/diagnostics expose active layer name, virtual slot, Router/takeover health and recovery state without creating a virtual device merely for observation.
- Real physical-controller + HidHide + target-game acceptance remains pending because no physical controller is available on the development machine.
- Stable `v1.3.0` remains unchanged.
