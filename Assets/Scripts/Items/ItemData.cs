using UnityEngine;

namespace WordRPG.Items
{
    public enum ItemKind
    {
        Material,   // 성유물 강화 재료 (전투·보물상자·보스에서 얻음. 상점에서는 팔지 않음)
        Consumable, // 상처약 등 소모품 — healAmount만큼 회복, 필드·전투에서 사용
        Keepsake    // 지역 도감 완성 징표 (기념물). 모으는 용도
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
        [Tooltip("소모품: 쓰면 회복하는 HP")]
        [SerializeField] private int healAmount;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public ItemKind Kind => kind;
        public Sprite Icon => icon;
        public Color PlaceholderColor => placeholderColor;
        public int HealAmount => healAmount;

        // 필드·전투에서 쓸 수 있는 회복 아이템
        public bool IsHealingItem => kind == ItemKind.Consumable && healAmount > 0;
    }
}
