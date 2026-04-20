using UnityEngine;

namespace ChatGo.Data
{
    /// <summary>
    /// 全局联系人登记表。把所有 ContactData 都拖到 allContacts 里，
    /// 并把这份资产放到 Assets/Resources/ContactRegistry.asset，
    /// 这样任意场景里都能通过 Resources.Load 拿到完整列表，
    /// 不再依赖 Resources.FindObjectsOfTypeAll<ContactData>() 的"运气"。
    /// </summary>
    [CreateAssetMenu(menuName = "ChatGo/Contact Registry", fileName = "ContactRegistry")]
    public class ContactRegistry : ScriptableObject
    {
        [Tooltip("项目里所有 ContactData，按主菜单期望的初始顺序排列即可")]
        public ContactData[] allContacts;

        private const string ResourcePath = "ContactRegistry";

        private static ContactRegistry cached;

        public static ContactRegistry Get()
        {
            if (cached != null) return cached;
            cached = Resources.Load<ContactRegistry>(ResourcePath);
            if (cached == null)
            {
                Debug.LogWarning($"ContactRegistry: 未在 Resources 下找到 '{ResourcePath}.asset'。请创建 Assets/Resources/ContactRegistry.asset 并把所有 ContactData 拖入 allContacts。");
            }
            return cached;
        }
    }
}
