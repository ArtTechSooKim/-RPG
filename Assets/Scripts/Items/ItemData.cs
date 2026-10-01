using UnityEngine;

namespace WordRPG.Items
{
    public enum ItemKind
    {
        EvolutionMaterial, // 진화 재료
        Consumable         // 소모품 (MVP 이후)
    }

    [CreateAssetMenu(fileName = "NewItem", menuName = "WordRPG/Item", order = 20)]
    public class ItemData : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName; // 예: "빛나는 잉크"
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private ItemKind kind;
        [SerializeField] private Sprite icon;
        [Tooltip("아트가 없을 때 쓰는 임시 색")]
        [SerializeField] private Color placeholderColor = Color.white;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public ItemKind Kind => kind;
        public Sprite Icon => icon;
        public Color PlaceholderColor => placeholderColor;
    }
}
