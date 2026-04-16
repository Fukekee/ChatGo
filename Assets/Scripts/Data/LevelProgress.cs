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

        public static void SaveResult(string levelId, string grade)
        {
            var data = GetCache();
            if (!data.TryGetValue(levelId, out var record))
            {
                record = new LevelRecord();
                data[levelId] = record;
            }

            record.completed = true;

            if (string.IsNullOrEmpty(record.bestGrade) || CompareGrade(grade, record.bestGrade) > 0)
            {
                record.bestGrade = grade;
            }

            if (record.unlockTimestamp == 0)
            {
                record.unlockTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
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

        public static bool IsUnlocked(ContactData contact, int levelIndex)
        {
            if (contact == null || contact.levels == null) return false;
            if (levelIndex < 0 || levelIndex >= contact.levels.Length) return false;
            if (levelIndex == 0) return true;

            var prevLevel = contact.levels[levelIndex - 1];
            var currentLevel = contact.levels[levelIndex];

            if (!IsCompleted(prevLevel.levelId)) return false;

            if (string.IsNullOrEmpty(currentLevel.requiredGrade)) return true;

            string bestGrade = GetBestGrade(prevLevel.levelId);
            return !string.IsNullOrEmpty(bestGrade) && CompareGrade(bestGrade, currentLevel.requiredGrade) >= 0;
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
