using System;
using UnityEngine;

namespace ChatGo.Data
{
    public enum UnlockTrigger
    {
        Immediate,
        NextCalendarDay,
        DelayHours
    }

    [Serializable]
    public class UnlockCondition
    {
        [Tooltip("必须先通关的关卡 ID（可跨联系人）")]
        public string requiredLevelId;

        [Tooltip("前置关需达到的最低评级。留空 = 通关即可")]
        public string minimumGrade;

        [Tooltip("满足以上条件后的解锁触发方式")]
        public UnlockTrigger trigger = UnlockTrigger.Immediate;

        [Tooltip("仅在 trigger = DelayHours 时使用")]
        public int delayHours;
    }

    [Serializable]
    public class LevelData
    {
        [Header("基本信息")]
        public string levelId;
        public string displayName;
        public string sceneName;

        [Header("聊天预览")]
        [TextArea(1, 2)]
        public string lastMessage;
        public string timestamp;

        [Header("解锁规则")]
        [Tooltip("游戏开始就解锁（用于游戏的起始关卡）")]
        public bool unlockedFromStart;

        [Tooltip("所有条件都满足才解锁（AND 关系）。为空时退回到旧的「上一关 + requiredGrade」逻辑")]
        public UnlockCondition[] unlockConditions;

        [Header("旧版兼容（不推荐继续使用）")]
        [Tooltip("旧版字段：上一关需达到的评级。仅当 unlockConditions 为空且 unlockedFromStart=false 时生效")]
        public string requiredGrade;
    }
}
