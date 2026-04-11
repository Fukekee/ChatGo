using System;
using UnityEngine;

namespace ChatGo.Data
{
    public enum SpeakerSide
    {
        Opponent = 0,
        Player = 1
    }

    [Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string text;

        public SpeakerSide speaker = SpeakerSide.Opponent;

        [Header("当 speaker 为 Player 时使用")]
        public string replyOption1;
        public string replyOption2;
    }

    [CreateAssetMenu(menuName = "ChatGo/Conversation Data", fileName = "ConversationData")]
    public class ConversationData : ScriptableObject
    {
        public DialogueLine[] lines;
    }
}
