# 关卡 / 联系人系统：设计决策备忘

> 本文档**只记录"为什么这么设计"以及关键不变量**，不重复 `WhatsAppLevelSystem.md` 已经写过的"怎么用"。
> 当你 N 个月后回来想"咦这里为什么是这样"时，先翻这一页。

---

## 一、核心设计原则

### 原则 1：全局只允许 **一个** `unlockedFromStart` 关卡

整个项目的所有 `ContactData` 加起来，只能有 **唯一一关** 把 `unlockedFromStart` 勾上。所有其他关卡都必须通过 `unlockConditions` 解锁。

**为什么这么设计：**

- **叙事节奏**：玩家进入游戏看到的是一条聊天 → 一段剧情起点，而不是面对一堆并列入口。
- **简化排序**：见原则 2，整个排序模型依赖"unlockedFromStart 关卡的 unlockTimestamp 永远是 0"这个假设，多于一个会破坏排序语义。
- **解锁瀑布的源头清晰**：所有进度都源于这一关，调试 / 回归 / 重置都好定位。

> ⚠️ 修改 `ContactData.asset` 时务必保证：所有联系人下，**`unlockedFromStart: 1` 的 LevelData 加起来只有 1 条**。

### 原则 2：`unlockTimestamp` 只代表"被前置条件激活的时刻"

| 关卡类型 | `unlockTimestamp` 的值 | 谁写的 |
|---|---|---|
| `unlockedFromStart` | **永远 0** | 没人写（这是设计） |
| 通过 `unlockConditions` 解锁 | 满足条件那一刻的 Unix 秒 | `LevelProgress.MarkUnlocked` |

`SaveResult`（通关时）**不会**回填 `unlockTimestamp`。语义上"开局就解锁"的关卡的"解锁时刻"是无意义的，永远 0 才是正确表述。

### 原则 3：联系人排序键 = 该联系人下所有关卡 `unlockTimestamp` 的最大值，**降序**

实现见 `LevelProgress.GetContactLatestTimestamp`，配合 `LevelSelectUI.GetSortedContacts` 使用。

得出的结论（**这就是为什么"开局解锁"的关卡永远沉底**）：

| 状态 | 排序键 |
|---|---|
| 只有 `unlockedFromStart` 关卡 | 0 |
| 通过解锁瀑布激活了任何新关卡 | > 0（严格大于 0） |

→ 任何"被剧情解锁的联系人" **永远排在** "只有起始关的联系人" **之上**，且不会出现 tie。

排序 tie-break 退化到 `Inspector contacts[]` 数组顺序。

---

## 二、关键不变量（写代码 / 改数据时务必维护）

1. **全项目 `unlockedFromStart: 1` 的 LevelData 数量恒等于 1**
2. **`SaveResult` 不写 `unlockTimestamp`**（只写 `completed` / `bestGrade` / `completedTimestamp`）
3. **`MarkUnlocked` 是 `unlockTimestamp` 的唯一写入点**
4. **`unlockTimestamp` / `completedTimestamp` 单位是 Unix 秒**（不是毫秒）
5. **`levelId` 一旦发布给玩家就不能改**（PlayerPrefs 用它作 key）

---

## 三、易错与历史踩坑

### 坑 1：曾经的"通关回填 unlockTimestamp"

旧版 `SaveResult` 里有：

```csharp
if (record.unlockTimestamp == 0)
{
    record.unlockTimestamp = now;
}
```

后果：`unlockedFromStart` 关卡通关时会被打上当前时间，与刚被 `MarkUnlocked` 的下一关在 **同一 Unix 秒**，触发排序 tie → 起始联系人反而排在新解锁联系人之上。

**已删除**。如果你某天想"为了某种功能再加回来"，请先确认它不会让原则 2 失效。

### 坑 2：`Resources.FindObjectsOfTypeAll<ContactData>()` 在战斗场景里可能找不到资产

`GameFlowUI.TryUnlockNextLevel` 用这个 API 扫描所有联系人。**仅当 ContactData 资产被某个加载中的对象引用时**才在内存里——LevelSelect 场景里有 `[SerializeField] ContactData[] contacts`，所以那里没问题；但如果你以后做"在战斗场景里弹出聊天列表"之类的功能，可能会扑空。

**临时绕开**：在战斗场景里挂一个隐藏 GameObject，`[SerializeField]` 引用一份 ContactData 列表，强制把它们留在内存。
**根治**：改成显式的 `ContactRegistry` ScriptableObject，所有 ContactData 都登记进去。

### 坑 3：`requiredGrade` 旧字段已彻底移除

不要在新关卡里依赖 "上一关 + 评级" 这种隐式回退。所有评级要求都通过 `UnlockCondition.minimumGrade` 显式声明。

### 坑 4：`Unix 秒`精度引发的 tie

`DateTimeOffset.UtcNow.ToUnixTimeSeconds()` 是整秒。如果你日后**再写两处时间戳**且它们可能在同一秒被触发，请注意可能的 tie。如果出现新场景需要更细精度，整体迁到 `ToUnixTimeMilliseconds()`，并同步更新 `LevelRecord` 的语义注释。

---

## 四、若需要修改设计，请同步更新的位置

| 修改 | 需要同步的代码 / 文档 |
|---|---|
| 允许多个 `unlockedFromStart` 关卡 | 重新设计排序：可能要引入"启动序号"或换成 `Max(unlockTs, completedTs)` 作为排序键 |
| 改 `unlockTimestamp` 为毫秒 | `LevelRecord` 注释、`MarkUnlocked`、本文 §二·4、`WhatsAppLevelSystem.md` §2.4 |
| 加新的进度字段 | 同步 `LevelRecord` / `ProgressStore` / 本文 §二、`WhatsAppLevelSystem.md` §2.4 |
| 改 `SaveResult` 让它再次写 `unlockTimestamp` | 重新检查原则 2 与坑 1 |

---

## 五、相关文件

- `Assets/Scripts/Data/LevelProgress.cs` — 进度存档、解锁判定、排序键
- `Assets/Scripts/Data/LevelData.cs` — 关卡数据 + `UnlockCondition`
- `Assets/Scripts/Data/ContactData.cs` — 联系人数据
- `Assets/Scripts/UI/LevelSelectUI.cs` — 主菜单聊天列表 + 排序调用方
- `Assets/Scripts/UI/GameFlowUI.cs` — 通关结算 + `TryUnlockNextLevel`
- `Docs/WhatsAppLevelSystem.md` — 系统使用手册（与本文互补）
