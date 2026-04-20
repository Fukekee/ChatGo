# WhatsApp 联系人-关卡系统 使用说明

> 版本：v1.0  
> 适用范围：ChatGo 项目主菜单 / 关卡解锁 / 关卡详情系统  
> 相关脚本：`ContactData`、`LevelData`、`LevelProgress`、`LevelSelectUI`、`ChatRowUI`、`ContactDetailUI`、`GameFlowUI`、`LevelManager`

---

## 目录

1. [系统概述](#一系统概述)
2. [核心数据结构](#二核心数据结构)
3. [Editor 配置流程](#三editor-配置流程)
4. [解锁条件配方](#四解锁条件配方)
5. [运行时行为](#五运行时行为)
6. [UI 交互说明](#六ui-交互说明)
7. [屏幕方向切换](#七屏幕方向切换)
8. [调试与排查](#八调试与排查)
9. [API 速查](#九api-速查)
10. [扩展性建议](#十扩展性建议)

---

## 一、系统概述

本系统将游戏主菜单做成**仿 WhatsApp 竖屏聊天列表**：

- **主菜单（竖屏）**：每一行 `ChatRow` 代表一个**联系人**（如老板、妈妈、同事），不是单个关卡。
- **联系人（ContactData）** 内含若干**关卡（LevelData）**，按剧情顺序排列。
- **进入游戏关卡（横屏）**：点击 ChatRow 行身体 → 进入该联系人当前最新可玩的关卡。
- **重玩 / 关卡列表（侧滑面板）**：点击头像 → 滑入 `ContactDetailUI`，可查看并重玩任意已通关关卡。
- **解锁条件高度灵活**：跨联系人解锁、按评级解锁、次日登录解锁、N 小时延迟解锁全部支持。
- **联系人动态排序**：最近有进展的联系人冒到列表顶部。

```
┌──────────────────────────┐
│   ChatGo (主菜单)         │  ← 竖屏 Portrait
│                          │
│  [👤老板] 请加薪... 14:30 │ ← ChatRow（点身体进入最新关）
│  [👨同事] 一起聚餐  09:12 │     （点头像看全部关卡）
│  [🔒 妈妈] (未解锁不显示) │
└──────────────────────────┘
            ↓ 点击行
┌──────────────────────────────────┐
│  关卡场景 (横屏 LandscapeLeft)    │
└──────────────────────────────────┘
```

---

## 二、核心数据结构

### 2.1 LevelData（关卡）

定义见 `Assets/Scripts/Data/LevelData.cs`。

| 字段 | 类型 | 说明 |
|------|------|------|
| `levelId` | string | **全局唯一** ID，用于解锁引用与进度存档（如 `boss_qingjia`） |
| `displayName` | string | 显示名（如 "请假"） |
| `sceneName` | string | Unity 场景名，需要加入 Build Settings |
| `lastMessage` | string | ChatRow 上的预览消息文本 |
| `timestamp` | string | ChatRow 上显示的时间字符串（仅展示） |
| `unlockedFromStart` | bool | 是否游戏开始就解锁（**全项目只能有 1 关勾选**，详见 `LevelSystemDesignNotes.md`） |
| `unlockConditions` | UnlockCondition[] | 解锁条件数组，**多条件 AND**；为空且 `unlockedFromStart=false` 时永远锁住 |

### 2.2 UnlockCondition（解锁条件）

| 字段 | 类型 | 说明 |
|------|------|------|
| `requiredLevelId` | string | 前置关卡 levelId（**可跨联系人引用**） |
| `minimumGrade` | string | 前置关需要达到的最低评级，留空 = 通关即可 |
| `trigger` | UnlockTrigger | 触发方式：`Immediate` / `NextCalendarDay` / `DelayHours` |
| `delayHours` | int | 仅 `DelayHours` 模式使用，单位小时 |

### 2.3 ContactData（联系人，ScriptableObject）

定义见 `Assets/Scripts/Data/ContactData.cs`。

| 字段 | 类型 | 说明 |
|------|------|------|
| `contactId` | string | 联系人唯一 ID（如 `boss`） |
| `displayName` | string | 显示名（如 "老板"） |
| `avatar` | Sprite | 头像 |
| `levels` | LevelData[] | 该联系人下所有关卡，按剧情顺序排列 |

### 2.4 LevelProgress（运行时进度，静态）

定义见 `Assets/Scripts/Data/LevelProgress.cs`。

每个关卡用一条 `LevelRecord` 记录：

| 字段 | 含义 |
|------|------|
| `completed` | 是否已通关 |
| `bestGrade` | 历史最佳评级 |
| `unlockTimestamp` | 解锁时刻（**Unix 秒**）。仅由 `MarkUnlocked` 写入；`unlockedFromStart` 关卡永远为 0 |
| `completedTimestamp` | 首次通关时刻（**Unix 秒**），时间锁判定基础 |

**持久化**：使用 `PlayerPrefs` + `JsonUtility`，键名 `LevelProgress_Data`。

---

## 三、Editor 配置流程

### Step 1：创建 ContactData 资源

1. 在 `Assets/Data/Contacts/`（自行新建）右键 → **Create → ChatGo → Contact Data**
2. 重命名（如 `Boss.asset`、`Mom.asset`、`Colleague.asset`）
3. 在 Inspector 里填好 `Contact Id` / `Display Name` / `Avatar`

### Step 2：在 ContactData 里添加关卡

`Levels` 数组每一项即一个 `LevelData`，按剧情顺序填好基本信息（见 §2.1）。

> ⚠️ **levelId 必须全局唯一且稳定**。一旦发布给玩家，**不要修改 levelId**，否则旧存档会丢失对应进度。

### Step 3：配置 UnlockConditions

参考 §四 的配方。多条件即 AND 关系。

### Step 4：把场景加入 Build Settings

`File → Build Settings`，把每个关卡场景拖进 Scenes In Build。`LevelManager.LoadLevel(sceneName)` 是按场景名加载的。

### Step 5：在主菜单 LevelSelectUI 里挂上 ContactData

打开主菜单场景，选中挂 `LevelSelectUI` 的 GameObject，把所有 ContactData 拖到 `Contacts` 数组中。

### Step 6：搭建 Prefab（必需）

`LevelSelectUI` 需要两个 Prefab，**必须在 Inspector 中拖入**（未配置时 `Start()` 会直接 LogError 退出）：

- `Chat Row Prefab` → 挂 `ChatRowUI`
- `Contact Detail UI` → 挂 `ContactDetailUI`

> 项目规则：UI 元素由人工在场景 / Prefab 中手动搭建，脚本只负责 `Instantiate` 已有 Prefab 与数据/事件绑定。详见 `.cursor/rules/no-code-ui.mdc`。

`ChatRowUI` 需要在 Prefab 上引用以下子物体（详见脚本 SerializeField）：
- `avatarButton`、`avatarImage`、`contactNameText`、`messagePreviewText`、`timestampText`
- `badgeRoot`（含子 `badgeText`）、`statusIcon`（已读/全 S 状态图标）

> 字体粗细 / 颜色等视觉风格由 Prefab 决定；脚本只赋 `text`，**不再**根据未读状态强制覆盖样式。未读状态完全靠 `badgeRoot` 红点徽章表达。

---

## 四、解锁条件配方

### 配方 1：起始关卡

```
Level Id:               boss_qingjia
Unlocked From Start:    ✅
Unlock Conditions:      [] (空)
```

### 配方 2：评级解锁同联系人下一关

老板·涨工资需要老板·请假至少 A 评级：

```
Level Id:               boss_zhanggongzi
Unlocked From Start:    ❌
Unlock Conditions:
  [0]:
    Required Level Id:  boss_qingjia
    Minimum Grade:      A
    Trigger:            Immediate
```

### 配方 3：跨联系人解锁

老板·请假通关后激活妈妈·第一关：

```
Level Id:               mom_dianhua
Unlock Conditions:
  [0]:
    Required Level Id:  boss_qingjia      ← 跨联系人引用
    Minimum Grade:      (留空)
    Trigger:            Immediate
```

### 配方 4：次日登录解锁

```
Level Id:               colleague_jucan
Unlock Conditions:
  [0]:
    Required Level Id:  mom_dianhua
    Trigger:            NextCalendarDay
```

> **NextCalendarDay 判定的是"日历日变了"，不是"24 小时"**。  
> 例：玩家凌晨 2 点通关，早上 8 点回来即解锁。如果想严格 24 小时请用 `DelayHours = 24`。

### 配方 5：N 小时冷却

```
Level Id:               colleague_jucan
Unlock Conditions:
  [0]:
    Required Level Id:  mom_dianhua
    Trigger:            DelayHours
    Delay Hours:        24
```

### 配方 6：多条件 AND

需要老板·请假拿 A，且妈妈·电话已通关：

```
Unlock Conditions:
  [0]:
    Required Level Id:  boss_qingjia
    Minimum Grade:      A
  [1]:
    Required Level Id:  mom_dianhua
    Minimum Grade:      (留空)
```

---

## 五、运行时行为

### 5.1 ChatRow 的「出现 / 消失」

`LevelSelectUI.BuildChatRows()` 中有过滤：

```csharp
bool hasAnyUnlocked = false;
for (int i = 0; i < contact.levels.Length; i++)
{
    if (LevelProgress.IsUnlocked(contact, i)) { hasAnyUnlocked = true; break; }
}
if (!hasAnyUnlocked) continue;
```

→ **只有该联系人下至少有一关解锁了，他才会出现在主页 ChatList 里**，模拟"陌生人不会出现在聊天列表"。

### 5.2 联系人排序

`GetSortedContacts()` 按"该联系人下所有关卡 `unlockTimestamp` 的最大值"**降序**排列：

| 触发时机 | 排序结果 |
|----------|----------|
| 首次启动 | 唯一可见的起始联系人 ts=0，按 Inspector 中 `contacts[]` 顺序 |
| 通关起始关 → 触发新联系人解锁 | 新联系人 unlockTimestamp = 通关时刻，**严格大于 0**，自动**冒到顶** |
| 仅通关、未触发新关解锁 | 排序不变（`unlockTimestamp` 不被通关行为修改） |

> ⚠️ `unlockedFromStart` 的关卡 `unlockTimestamp` **永远为 0**，因此该联系人在排序里**永远沉底**——这是有意为之，详见 `LevelSystemDesignNotes.md` 原则 2。

### 5.3 通关后的解锁瀑布

通关时 `GameFlowUI.TryUnlockNextLevel()` 会**全图扫描**所有 ContactData 的所有 LevelData，凡是这次通关后满足解锁条件的，立刻 `MarkUnlocked`。下次回主页时它们都已"在那"。

### 5.4 ChatRow 的状态显示

| 视觉元素 | 含义 |
|----------|------|
| 未读红标 `unreadBadge` | 数字 = 该联系人下"已解锁但未通关"的关卡数 |
| 灰色 ✓ `statusIcon` | 全部通关 |
| 蓝色 ✓✓ `statusIcon` | 全部 S 评级（完美通关） |

---

## 六、UI 交互说明

| 操作 | 行为 |
|------|------|
| 点击 ChatRow 行身体 | 进入该联系人当前最新可玩关卡（`GetCurrentLevelIndex` 返回的索引） |
| 点击 ChatRow 头像 | 滑入 `ContactDetailUI`，展示该联系人所有关卡 |
| 详情页内点击已通关关卡 | 重玩该关 |
| 详情页内点击当前关卡 | 进入并显示"▶ 新关卡" |
| 详情页内点击未解锁关卡 | 不可点，显示锁定原因 |
| 详情页 Back 按钮 | 滑出关闭详情面板 |

### 锁定原因文本（GetLockReason）

`LevelProgress.GetLockReason(level)` 会返回类似：

- `🔒 先完成「老板·请假」` — 前置关未完成
- `🔒 老板·请假需要 A 评级` — 评级不达标
- `🔒 明天再来` — `NextCalendarDay` 还没到
- `🔒 还需等待 N 小时` — `DelayHours` 还没到

---

## 七、屏幕方向切换

`LevelManager` 自动管理：

| 调用 | 屏幕方向 |
|------|----------|
| `LevelManager.LoadLevel(sceneName, levelId)` | LandscapeLeft（横屏） |
| `LevelManager.ReturnToMainMenu()` | Portrait（竖屏） |

> 在 Editor 内只 `Debug.Log` 不实际旋转，避免影响 Game 视图调试；在真机上才生效（见 `SetOrientation` 的 `#if UNITY_EDITOR` 分支）。

`ProjectSettings.asset` 中 `defaultScreenOrientation: 4`（AutoRotation），允许运行时动态切换。

---

## 八、调试与排查

### 8.1 重置玩家进度

无 UI 入口，通过控制台或临时脚本：

```csharp
ChatGo.Data.LevelProgress.ClearAll();
```

或 Unity Editor → `Edit → Clear All PlayerPrefs`。

### 8.2 测试时间锁

- 调系统时间往后拨一天测试 `NextCalendarDay`
- 临时把 `DelayHours` 改成 0（约几秒后才解锁可改 1，立即解锁可填 0）

### 8.3 关键调试日志

代码已在以下节点埋了 `Debug.Log`：

- 通关保存：`LevelProgress: 保存 boss_qingjia 评级=A 最佳=A`
- 关卡解锁：`GameFlowUI: 关卡解锁 -> 妈妈·电话`
- 进入关卡：`LevelSelectUI: 进入 [老板] 关卡 [请假] -> QingJia`
- 屏幕方向：`LevelManager: 屏幕方向 -> LandscapeLeft`

### 8.4 常见问题排查

| 问题 | 原因 | 解决 |
|------|------|------|
| 主页空空什么都没有 | 没有任何关卡 `unlockedFromStart=true` | **全项目恰好勾 1 个**起始关（多于 1 个会破坏排序约定） |
| 通关后下一关没出现 | `Required Level Id` 拼错 / 没填 | 仔细对照 levelId |
| 锁定原因显示原始 ID 而不是名字 | 该 ContactData 资源没被加载到内存 | 确保它已被 `LevelSelectUI.contacts` 引用 |
| 时间锁永远不解锁 | 系统时间异常 / 没有 completedTimestamp | 检查 `LevelProgress.GetCompletedTimestamp(...)` |
| 进入关卡场景报错 | 场景未加入 Build Settings | 加入 Build Settings |
| 详情页打不开 | `ContactDetailUI` 引用未挂或 `panelRoot` 未设 | Inspector 中检查引用 |

---

## 九、API 速查

### LevelProgress（静态类）

```csharp
// —— 查询 ——
bool   IsUnlocked(LevelData level)                     // 基于 unlockedFromStart + UnlockConditions
bool   IsUnlocked(ContactData contact, int levelIndex) // 仅做下标校验后转发到上面
bool   IsCompleted(string levelId)
string GetBestGrade(string levelId)
long   GetUnlockTimestamp(string levelId)
long   GetCompletedTimestamp(string levelId)
string GetLockReason(LevelData level)                  // 用户友好的锁定原因文本

// —— 联系人级别 ——
int  GetCurrentLevelIndex(ContactData contact) // 当前可玩的最新关索引
long GetContactLatestTimestamp(ContactData contact) // 用于排序

// —— 写入 ——
void MarkUnlocked(string levelId)              // 唯一会写 unlockTimestamp 的入口
void SaveResult(string levelId, string grade)  // 通关保存评级，首次通关时写 completedTimestamp（不写 unlockTimestamp）
void ClearAll()                                // 清空所有进度
```

### LevelManager（静态类）

```csharp
string CurrentLevelScene  { get; }
string CurrentLevelId     { get; }

void LoadLevel(string sceneName, string levelId = null) // 自动切横屏
void ReturnToMainMenu()                                 // 自动切竖屏
void RestartCurrentLevel()
```

### ChatRowUI 事件

```csharp
event Action OnAvatarClicked;  // 头像点击 → 打开详情页
event Action OnRowClicked;     // 行身体点击 → 进入最新关
void Setup(ContactData contact);
```

### ContactDetailUI

```csharp
void Show(ContactData contact);  // 滑入
void Hide();                     // 滑出
```

---

## 十、扩展性建议

| 需求 | 改造方向 |
|------|----------|
| **OR 条件**（任一满足即可） | 在 `LevelData` 加 `UnlockCondition[] orConditions`，新写一个 `IsAnyConditionMet` 方法 |
| **倒计时实时刷新** | 在 ChatRow / DetailUI 用 `Update` 周期重算 `GetLockReason` |
| **新关卡红点动画** | `LevelRecord` 加 `isNewlyUnlocked`，UI 显示后再清掉 |
| **重复挑战奖励** | `LevelRecord` 加 `replayCount`，结算时按规则发金币 |
| **CD 防作弊** | 在 `LevelProgress` 增加服务器时间校验，本地时间倒退则忽略 |
| **章节/剧情分组** | 在 ContactData 上层再加 `ChapterData` ScriptableObject |
| **存档导入导出** | `LevelProgress` 已是 JSON，可直接读写 `PlayerPrefs` 字符串到文件 |

---

## 附录 A：典型项目结构

```
Assets/
├── Data/
│   └── Contacts/
│       ├── Boss.asset           ← ContactData
│       ├── Mom.asset
│       └── Colleague.asset
├── Prefabs/
│   └── UI/
│       ├── ChatRow.prefab        ← 挂 ChatRowUI
│       └── ContactDetail.prefab  ← 挂 ContactDetailUI
├── Scenes/
│   ├── Main.unity                ← 主菜单（竖屏）
│   ├── BossQingJia.unity         ← 关卡场景（横屏）
│   ├── BossZhangGongZi.unity
│   └── ...
└── Scripts/
    ├── Core/LevelManager.cs
    ├── Data/
    │   ├── ContactData.cs
    │   ├── LevelData.cs
    │   └── LevelProgress.cs
    └── UI/
        ├── ChatRowUI.cs
        ├── ContactDetailUI.cs
        ├── GameFlowUI.cs
        └── LevelSelectUI.cs
```

## 附录 B：完整工作流示例

**目标**：实现"老板请假(必玩起始) → A 评级解锁老板涨工资 / 任意通关解锁妈妈电话 → 妈妈电话通关后次日解锁同事聚餐"。

1. 创建 3 个 ContactData：`Boss`、`Mom`、`Colleague`
2. 老板下加 2 关：

   | levelId | unlockedFromStart | unlockConditions |
   |---------|-------------------|------------------|
   | `boss_qingjia` | ✅ | (空) |
   | `boss_zhanggongzi` | ❌ | [requiredLevelId=`boss_qingjia`, minimumGrade=`A`, Immediate] |

3. 妈妈下加 1 关：

   | levelId | unlockConditions |
   |---------|------------------|
   | `mom_dianhua` | [requiredLevelId=`boss_qingjia`, Immediate] |

4. 同事下加 1 关：

   | levelId | unlockConditions |
   |---------|------------------|
   | `colleague_jucan` | [requiredLevelId=`mom_dianhua`, NextCalendarDay] |

5. 把 4 个场景加进 Build Settings
6. 把 3 个 ContactData 拖进 `LevelSelectUI.contacts`
7. Run 主菜单：

   - 启动后只看到老板（其他都未解锁）
   - 通关老板·请假（任意评级）→ 妈妈出现在列表（且置顶）
   - 通关老板·请假拿到 A → 老板·涨工资也解锁
   - 通关妈妈·电话 → 同事**今天还看不到**，明天再启动游戏才出现

---

> 文档维护者：在新增字段或改动解锁逻辑时，请同步更新本文件的 §二、§四、§九。
