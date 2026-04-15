using System;
using UnityEngine;

namespace ChatGo.Data
{
    [Serializable]
    public class GradeResult
    {
        public string gradeName;
        [Tooltip("playerHP / opponentHP >= 此阈值时获得该等级（从高到低判断）")]
        public float minRatio;
        [TextArea(2, 4)]
        public string resultText;
    }

    public class LevelResultConfig : MonoBehaviour
    {
        [Tooltip("从高到低排列，例如 S(minRatio=2) -> A(minRatio=1) -> B(minRatio=0)")]
        [SerializeField] private GradeResult[] grades = new[]
        {
            new GradeResult { gradeName = "S", minRatio = 2f, resultText = "完美通关" },
            new GradeResult { gradeName = "A", minRatio = 1f, resultText = "优秀通关" },
            new GradeResult { gradeName = "B", minRatio = 0f, resultText = "勉强通关" }
        };

        [Header("特殊文本")]
        [SerializeField] private string failureText = "对话失败";
        [SerializeField] private string opponentDefeatedText = "对手已被说服！";

        public string FailureText => failureText;
        public string OpponentDefeatedText => opponentDefeatedText;

        public GradeResult GetHighestGrade()
        {
            if (grades == null || grades.Length == 0)
            {
                return null;
            }

            return grades[0];
        }

        public GradeResult EvaluateGrade(float ratio)
        {
            if (grades == null || grades.Length == 0)
            {
                return null;
            }

            foreach (GradeResult grade in grades)
            {
                if (ratio >= grade.minRatio)
                {
                    return grade;
                }
            }

            return grades[grades.Length - 1];
        }
    }
}
