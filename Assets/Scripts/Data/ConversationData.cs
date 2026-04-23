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

        [Header("机关（对应玩家气泡 BubblePlatform.hazardGroups 下标）")]
        [Tooltip("勾选后，根据 hazardGroupIndex 启用预制体上 hazardGroups 中的一项")]
        public bool useHazardGroup;
        [Tooltip("hazardGroups 数组下标，从 0 开始")]
        public int hazardGroupIndex;
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
