using UnityEngine;

namespace ChatGo.Data
{
    [CreateAssetMenu(menuName = "ChatGo/Contact Data", fileName = "NewContact")]
    public class ContactData : ScriptableObject
    {
        public string contactId;
        public string displayName;
        public Sprite avatar;

        [Tooltip("该联系人下的所有关卡，按剧情顺序排列")]
        public LevelData[] levels;
    }
}
