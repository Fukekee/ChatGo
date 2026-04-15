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
    public class DialogueChoice
    {
        public string choiceText;
        [Tooltip("选了这个选项后跳转到哪个节点 ID")]
        public string targetNodeId;
    }

    [Serializable]
    public class DialogueNode
    {
        public string nodeId;

        [TextArea(2, 4)]
        public string text;

        public SpeakerSide speaker = SpeakerSide.Opponent;

        [Header("对手台词的下一个节点（留空表示对话结束）")]
        public string nextNodeId;

        [Header("玩家选择（speaker 为 Player 时使用）")]
        public DialogueChoice[] choices;
    }

    [CreateAssetMenu(menuName = "ChatGo/Conversation Data", fileName = "ConversationData")]
    public class ConversationData : ScriptableObject
    {
        [Tooltip("对话的起始节点 ID")]
        public string startNodeId;

        public DialogueNode[] nodes;
    }
}
