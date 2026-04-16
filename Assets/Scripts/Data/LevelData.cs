using System;
using UnityEngine;

namespace ChatGo.Data
{
    [Serializable]
    public class LevelData
    {
        public string levelId;
        public string displayName;
        public string sceneName;

        [TextArea(1, 2)]
        public string lastMessage;
        public string timestamp;

        [Tooltip("解锁条件：上一关需达到的最低评级（如 B）。首关留空表示默认解锁")]
        public string requiredGrade;
    }
}
