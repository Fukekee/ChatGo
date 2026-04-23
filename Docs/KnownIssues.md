# 已踩过的坑 / 已知问题登记

记录排查过的、容易反复踩的非显然问题。每条都包含**症状 / 真因 / 排查路径 / 修复方式 / 预防措施**，方便日后快速定位。

新增条目时按"日期 + 模块 + 简要标题"格式追加，置于文末，避免改动既有锚点。

---

## 2026-04-18 · Conversation · HandPlaced 模式下出现"额外的 bubble 平台"

### 症状

`ConversationManager.platformMode = HandPlacedInScene` 模式下，第一句对话出来后，场景里**多出**一个不在预期顺序里的 bubble（看起来像是 player 样式的 bubble，实际就是某个本不该激活的 BubblePlatform）。

直觉上会怀疑是 `BubblePool` 被错误调用，或者 `SpawnHandPlacedLine` 的池逻辑/说话方判断出了 bug。

### 真因（不是代码问题）

**场景里手摆了不止 N 个 BubblePlatform，但只有 N 个被拖进了 `ConversationManager.handPlacedPlatforms` 数组**。多出来的那些 BubblePlatform 在场景里默认是 `SetActive(true)`，开局就显示在世界里。

`PrepareHandPlacedPlatformsInactive` 只对**数组里**的平台做 `SetActive(false)`：

```csharp
for (int i = 0; i < handPlacedPlatforms.Length; i++)
{
    if (handPlacedPlatforms[i] != null)
    {
        handPlacedPlatforms[i].gameObject.SetActive(false);
        idleHandPlacedBubbles.Enqueue(handPlacedPlatforms[i]);
    }
}
```

数组没收录的平台**不会**被关掉、也**不会**进对象池。它就那样以"上次手搭时的状态"挂在场景里——`typingIndicator` 可能是开着的，`chatText` 可能默认写着"正在输入中..."、或者 prefab 本身就是 player 样式（avatar 在右），于是看起来"好像 BubblePool 又生成了一个 player bubble"。

### 排查路径

1. 启动游戏，第一句对话弹出后**暂停**。
2. Hierarchy 里搜所有挂 `BubblePlatform` 组件的 GameObject。
3. 对照 `ConversationManager` Inspector 里 `Hand Placed Platforms` 数组的所有项。
4. 出现在 Hierarchy 但**不在数组里**的，就是肇事者。

> 顺便排除另一个嫌疑：`GameBase.unity` 里那个 `BubblePool` GameObject 即便 `Enabled = false`，`Awake` 也会跑（Unity 的 `Awake` 不受组件 enabled 状态影响），它会在场景里 `new` 出 `OpponentBubbleTemplate` / `PlayerBubbleTemplate` 等占位 GameObject。它们**应当**被立刻 `SetActive(false)`，但如果你看到 hierarchy 里有这些名字、且它们是 active 的，那就是 BubblePool 在闯祸。

### 修复方式

任选其一：

- **把多余的 BubblePlatform 删掉**（如果是测试残留）。
- **拖进 `handPlacedPlatforms` 数组**（如果你确实需要它作为可复用槽位）。
- **手动 SetActive(false)**（不推荐，治标不治本——下次别人不知道为什么它是关的）。

### 预防措施

- 在场景里"手摆 bubble platform"和"把它拖进 ConversationManager 数组"应当是**配对操作**。建议在场景里建一个空 GameObject 比如 `BubblePlatformPool`，**所有手摆的 BubblePlatform 必须挂在它下面**，并约定"`BubblePlatformPool` 的子节点必须和 `handPlacedPlatforms` 数组一一对应"。这样目检 hierarchy 就能立刻发现"漏拖"。
- 长期看，可以在 `ConversationManager.Start` 或 `ValidateHandPlacedSetup` 里加一段自检：扫场景里所有 `BubblePlatform`，凡是不在 `handPlacedPlatforms` 数组里的就 `Debug.LogWarning`，避免下次再栽。
- 如果项目里彻底不用 RuntimeBubblePool 模式，建议把 `GameBase.unity` 里那个 `BubblePool` GameObject 删掉，杜绝 `BubbleRuntimeFactory` 在 `Awake` 时 `new GameObject` 创建带 collider + TextMeshPro 的占位模板（这个工厂同时也违反 `.cursor/rules/no-code-ui.mdc` 里"禁止运行时程序化生成 UI"的约定）。

### 相关文件

- `Assets/Scripts/Conversation/ConversationManager.cs` — `PrepareHandPlacedPlatformsInactive` / `SpawnHandPlacedLine`
- `Assets/Scripts/Bubble/BubblePool.cs` — `BubbleRuntimeFactory.CreateTemplate`
- `Assets/Scripts/Bubble/BubblePlatform.cs` — `Init` 中根据 speaker 切换 typing indicator 与文本

---
