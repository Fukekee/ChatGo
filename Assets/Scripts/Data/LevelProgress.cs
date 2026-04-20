using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChatGo.Data
{
    [Serializable]
    internal class LevelRecord
    {
        public bool completed;
        public string bestGrade;
        public long unlockTimestamp;
        public long completedTimestamp;
    }

    [Serializable]
    internal class ProgressStore
    {
        public List<string> keys = new List<string>();
        public List<LevelRecord> values = new List<LevelRecord>();

        public Dictionary<string, LevelRecord> ToDictionary()
        {
            var dict = new Dictionary<string, LevelRecord>();
            for (int i = 0; i < keys.Count && i < values.Count; i++)
            {
                dict[keys[i]] = values[i];
            }
            return dict;
        }

        public static ProgressStore FromDictionary(Dictionary<string, LevelRecord> dict)
        {
            var store = new ProgressStore();
            foreach (var kvp in dict)
            {
                store.keys.Add(kvp.Key);
                store.values.Add(kvp.Value);
            }
            return store;
        }
    }

    public static class LevelProgress
    {
        private const string SaveKey = "LevelProgress_Data";

        private static readonly string[] GradeOrder = { "S", "A", "B" };

        private static Dictionary<string, LevelRecord> cache;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            cache = null;
        }

        private static Dictionary<string, LevelRecord> GetCache()
        {
            if (cache != null) return cache;

            string json = PlayerPrefs.GetString(SaveKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                var store = JsonUtility.FromJson<ProgressStore>(json);
                cache = store != null ? store.ToDictionary() : new Dictionary<string, LevelRecord>();
            }
            else
            {
                cache = new Dictionary<string, LevelRecord>();
            }
            return cache;
        }

        private static void Save()
        {
            var store = ProgressStore.FromDictionary(GetCache());
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(store));
            PlayerPrefs.Save();
        }

        public static bool IsCompleted(string levelId)
        {
            return GetCache().TryGetValue(levelId, out var record) && record.completed;
        }

        public static string GetBestGrade(string levelId)
        {
            return GetCache().TryGetValue(levelId, out var record) ? record.bestGrade : null;
        }

        public static long GetUnlockTimestamp(string levelId)
        {
            return GetCache().TryGetValue(levelId, out var record) ? record.unlockTimestamp : 0;
        }

        public static long GetCompletedTimestamp(string levelId)
        {
            return GetCache().TryGetValue(levelId, out var record) ? record.completedTimestamp : 0;
        }

        public static void SaveResult(string levelId, string grade)
        {
            var data = GetCache();
            if (!data.TryGetValue(levelId, out var record))
            {
                record = new LevelRecord();
                data[levelId] = record;
            }

            bool firstCompletion = !record.completed;
            record.completed = true;

            if (string.IsNullOrEmpty(record.bestGrade) || CompareGrade(grade, record.bestGrade) > 0)
            {
                record.bestGrade = grade;
            }

            if (firstCompletion && record.completedTimestamp == 0)
            {
                record.completedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }

            Save();
            Debug.Log($"LevelProgress: 保存 {levelId} 评级={grade} 最佳={record.bestGrade}");
        }

        public static void MarkUnlocked(string levelId)
        {
            var data = GetCache();
            if (!data.TryGetValue(levelId, out var record))
            {
                record = new LevelRecord();
                data[levelId] = record;
            }

            if (record.unlockTimestamp == 0)
            {
                record.unlockTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                Save();
            }
        }

        /// <summary>
        /// New API: evaluate unlock condition based on the level's own configuration.
        /// </summary>
        public static bool IsUnlocked(LevelData level)
        {
            if (level == null) return false;
            if (level.unlockedFromStart) return true;

            if (level.unlockConditions != null && level.unlockConditions.Length > 0)
            {
                foreach (var cond in level.unlockConditions)
                {
                    if (!EvaluateCondition(cond)) return false;
                }
                return true;
            }

            return false;
        }

        private static bool EvaluateCondition(UnlockCondition cond)
        {
            if (cond == null || string.IsNullOrEmpty(cond.requiredLevelId)) return true;

            if (!IsCompleted(cond.requiredLevelId)) return false;

            if (!string.IsNullOrEmpty(cond.minimumGrade))
            {
                string best = GetBestGrade(cond.requiredLevelId);
                if (string.IsNullOrEmpty(best)) return false;
                if (CompareGrade(best, cond.minimumGrade) < 0) return false;
            }

            return CheckTimeGate(cond);
        }

        private static bool CheckTimeGate(UnlockCondition cond)
        {
            switch (cond.trigger)
            {
                case UnlockTrigger.Immediate:
                    return true;

                case UnlockTrigger.NextCalendarDay:
                {
                    long ts = GetCompletedTimestamp(cond.requiredLevelId);
                    if (ts == 0) return false;
                    var completedDate = DateTimeOffset.FromUnixTimeSeconds(ts).LocalDateTime.Date;
                    return DateTime.Now.Date > completedDate;
                }

                case UnlockTrigger.DelayHours:
                {
                    long ts = GetCompletedTimestamp(cond.requiredLevelId);
                    if (ts == 0) return false;
                    var completedAt = DateTimeOffset.FromUnixTimeSeconds(ts).LocalDateTime;
                    return (DateTime.Now - completedAt).TotalHours >= cond.delayHours;
                }
            }
            return false;
        }

        /// <summary>
        /// 按联系人数组下标取关卡，解锁规则与 <see cref="IsUnlocked(LevelData)"/> 一致。
        /// </summary>
        public static bool IsUnlocked(ContactData contact, int levelIndex)
        {
            if (contact == null || contact.levels == null) return false;
            if (levelIndex < 0 || levelIndex >= contact.levels.Length) return false;
            return IsUnlocked(contact.levels[levelIndex]);
        }

        /// <summary>
        /// Returns a user-friendly description of why a level is locked, or null if unlocked.
        /// </summary>
        public static string GetLockReason(LevelData level)
        {
            if (level == null) return null;
            if (level.unlockedFromStart) return null;

            if (level.unlockConditions != null && level.unlockConditions.Length > 0)
            {
                foreach (var cond in level.unlockConditions)
                {
                    string reason = GetConditionLockReason(cond);
                    if (reason != null) return reason;
                }
                return null;
            }

            return "🔒 未解锁";
        }

        private static string GetConditionLockReason(UnlockCondition cond)
        {
            if (cond == null || string.IsNullOrEmpty(cond.requiredLevelId)) return null;

            string prereqName = ResolveLevelDisplayName(cond.requiredLevelId);

            if (!IsCompleted(cond.requiredLevelId))
            {
                return $"🔒 先完成「{prereqName}」";
            }

            if (!string.IsNullOrEmpty(cond.minimumGrade))
            {
                string best = GetBestGrade(cond.requiredLevelId);
                if (string.IsNullOrEmpty(best) || CompareGrade(best, cond.minimumGrade) < 0)
                {
                    return $"🔒 「{prereqName}」需达到 {cond.minimumGrade}";
                }
            }

            switch (cond.trigger)
            {
                case UnlockTrigger.NextCalendarDay:
                {
                    long ts = GetCompletedTimestamp(cond.requiredLevelId);
                    if (ts > 0)
                    {
                        var completedDate = DateTimeOffset.FromUnixTimeSeconds(ts).LocalDateTime.Date;
                        if (DateTime.Now.Date <= completedDate)
                        {
                            return "🕒 明天再来";
                        }
                    }
                    break;
                }

                case UnlockTrigger.DelayHours:
                {
                    long ts = GetCompletedTimestamp(cond.requiredLevelId);
                    if (ts > 0)
                    {
                        var completedAt = DateTimeOffset.FromUnixTimeSeconds(ts).LocalDateTime;
                        double remaining = cond.delayHours - (DateTime.Now - completedAt).TotalHours;
                        if (remaining > 0)
                        {
                            return $"🕒 还需等待 {Math.Ceiling(remaining)} 小时";
                        }
                    }
                    break;
                }
            }

            return null;
        }

        private static string ResolveLevelDisplayName(string levelId)
        {
            var allContacts = Resources.FindObjectsOfTypeAll<ContactData>();
            foreach (var contact in allContacts)
            {
                if (contact == null || contact.levels == null) continue;
                foreach (var lvl in contact.levels)
                {
                    if (lvl != null && lvl.levelId == levelId)
                    {
                        if (!string.IsNullOrEmpty(lvl.displayName))
                        {
                            return $"{contact.displayName}·{lvl.displayName}";
                        }
                        return levelId;
                    }
                }
            }
            return levelId;
        }

        /// <summary>
        /// Returns the index of the latest level the player should play for this contact:
        /// the first unlocked-but-not-completed level, or the last level if all completed.
        /// </summary>
        public static int GetCurrentLevelIndex(ContactData contact)
        {
            if (contact == null || contact.levels == null || contact.levels.Length == 0) return 0;

            for (int i = 0; i < contact.levels.Length; i++)
            {
                if (!IsUnlocked(contact, i)) return Mathf.Max(0, i - 1);
                if (!IsCompleted(contact.levels[i].levelId)) return i;
            }

            return contact.levels.Length - 1;
        }

        /// <summary>
        /// Gets the latest unlock timestamp across all levels of a contact.
        /// Returns 0 for contacts whose first level has never been touched.
        /// </summary>
        public static long GetContactLatestTimestamp(ContactData contact)
        {
            if (contact == null || contact.levels == null) return 0;

            long latest = 0;
            foreach (var level in contact.levels)
            {
                long ts = GetUnlockTimestamp(level.levelId);
                if (ts > latest) latest = ts;
            }
            return latest;
        }

        /// <summary>
        /// Positive if a is better than b, 0 if equal, negative if worse.
        /// </summary>
        public static int CompareGrade(string a, string b)
        {
            return GetGradeRank(a) - GetGradeRank(b);
        }

        private static int GetGradeRank(string grade)
        {
            if (string.IsNullOrEmpty(grade)) return -1;
            for (int i = 0; i < GradeOrder.Length; i++)
            {
                if (string.Equals(grade, GradeOrder[i], StringComparison.OrdinalIgnoreCase))
                {
                    return GradeOrder.Length - i;
                }
            }
            return 0;
        }

        public static void ClearAll()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            cache = null;
        }
    }
}
