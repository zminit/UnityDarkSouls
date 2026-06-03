# UnityDarkSouls

这是一个 Unity 3D 动作战斗学习项目。当前目标不是做大型仿魂复刻，而是把一个小而完整的动作战斗 Demo 打磨成客户端开发实习作品：玩家移动、冲刺/跳跃/翻滚、攻击、敌人受击/死亡，以及少量可讲清技术细节的渲染和特效亮点。

本地 DevLog 位于 `D:\UnityDarkSoul\DevLog`。建议从 `D:\UnityDarkSoul\DevLog\index.html` 开始查看 AI 工作流和 14 天开发计划。

## 项目快照

- Unity 版本：`2022.3.62f3c1`
- 渲染管线：URP `14.0.12`
- 主场景：`Assets/Scenes/MainScene.unity`
- 输入系统：Unity Input System `1.14.0`
- 输入配置：`Assets/Setting/PlayerControlls.inputactions`
- 输入生成类：`Assets/Setting/PlayerControlls.cs`
- 摄像机：Cinemachine `2.10.4`
- 当前玩家核心脚本：
  - `Assets/Scripts/InputHandler.cs`
  - `Assets/Scripts/PlayerManager.cs`
  - `Assets/Scripts/PlayerState.cs`
  - `Assets/Scripts/CharacterFSM/*`
  - `Assets/Scripts/Animation/*`

## 当前架构

`MainScene.unity` 当前实际挂载的是 `CFSM.CharacterFSM`，脚本位于 `Assets/Scripts/CharacterFSM/CharacterFSM.cs`。

当前运行链路：

```text
PlayerControlls.inputactions
-> InputHandler
-> CFSM.CharacterFSM
-> LocomotionState / JumpState
-> Animator + PlayerManager
-> Rigidbody movement / rotation
```

当前状态结构：

- 外层 FSM：`LocomotionState`、`JumpState`
- 移动子状态：`Idle`、`Walk`、`Run`、`Sprint`
- `Assets/Scripts/CharacterFSM/AttackState.cs` 和 `DodgeState.cs` 目前还是空壳。
- `Assets/Scripts/CharacterStateMachine/*` 是另一套事件驱动状态机原型，目前没有挂到 `MainScene`。

下一阶段架构决策：

- 暂定 `CFSM.CharacterFSM` 为当前主线，因为它是主场景真实使用的状态机。
- `CharacterStateMachine` 先冻结为参考代码，除非后续明确安排迁移任务。
- 攻击、翻滚、命中窗口和战斗事件都应该进入同一套状态机主线，避免两套系统同时扩展。

## 短期路线

1. 稳定基线
   - 确认 `MainScene` 能打开并进入 Play Mode。
   - 检查 Console 和 Inspector 引用缺失。
   - 每次改动前记录当前 Git 状态。

2. 收敛状态机架构
   - 保留一条活跃 FSM 主线。
   - 明确输入事件如何变成状态请求。
   - 明确状态如何驱动 Animator、Root Motion 和 Rigidbody。

3. 实现战斗 v1
   - 接入轻攻击输入。
   - 增加攻击状态和攻击动画时序。
   - 增加简单命中窗口和单次攻击去重。
   - 增加敌人生命、受击和死亡。

4. 增加战斗反馈
   - Hit stop 或轻量时间脉冲。
   - Camera shake。
   - 敌人受击闪白，优先使用 `MaterialPropertyBlock`。
   - 命中特效和对象池。

5. 增加渲染/特效展示点
   - 武器拖尾。
   - 死亡溶解。
   - 翻滚残影或一个小型 URP 后处理效果。
   - 每个效果提供独立开关，方便演示和性能对比。

## Agent 工作流

本项目由 Codex 作为主 agent。

主 agent 职责：

- 拆解任务。
- 先看全局架构，再改代码。
- 负责 C# 架构、Shader/VFX 实现、整合、Review、README 和 DevLog。
- 控制 Unity 序列化资源改动范围，让每个 diff 都能解释。

推荐辅助角色：

- Locus Explorer：只读检查 Unity Editor，包括场景对象、Prefab、Animator、Console 和截图。
- Locus Worker：做小范围 Editor 操作，例如绑定 Inspector 引用、创建材质、放置测试对象和 Play Mode 检查。
- Codex Reviewer：独立审查，只输出 P0/P1/P2 问题，不参与实现。

协作红线：

不要让多个 agent 同时修改同一个 Prefab、Scene、Animator Controller 或 InputActions。任何 Unity 资源变更都需要在完成后立刻检查 Git diff。

## Definition of Done

一个功能完成需要满足：

- 相关流程能在 Play Mode 跑通。
- Console 没有新增 error。
- Git diff 范围清晰、可解释。
- 没有无关 Prefab、Scene、Animator、材质或输入资源改动。
- Inspector 引用已经检查。
- 功能有最小演示路径。
- README 或 DevLog 至少记录一句技术说明。
- Review 后没有未处理的 P0/P1 问题。

## 每日手动测试路径

每天结束前至少跑一次：

```text
打开 MainScene
-> 进入 Play Mode
-> 移动
-> 冲刺
-> 跳跃或翻滚
-> 攻击
-> 命中敌人
-> 敌人受击
-> 敌人死亡
-> 重开或重复一轮
```

完整测试清单位于 `D:\UnityDarkSoul\DevLog\test_checklist.md`。

## 教程来源

原始教程参考：

`【Unity教程】从0编程制作黑魂：黑暗之魂 DarkSouls 复刻经典教程 ARPG DARK SOULS in Unity3d`

https://www.bilibili.com/video/BV1KU4y1x7jH/
