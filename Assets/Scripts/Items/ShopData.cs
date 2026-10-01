using System;
using System.Collections.Generic;
using UnityEngine;

namespace WordRPG.Items
{
    [Serializable]
    public class ShopEntry
    {
        [SerializeField] private ItemData item;
        [SerializeField] private int price = 50;

        public ItemData Item => item;
        public int Price => price;

        private ShopEntry() { } // Unity 직렬화용

        public ShopEntry(ItemData item, int price)
        {
            this.item = item;
            this.price = price;
        }
    }

    public enum PurchaseResult
    {
        Bought,
        NotEnoughGold,
        NotForSale // 아이템이 비었거나 가격이 0 이하 (데이터 오류)
    }

    // 상점 하나의 판매 목록. 지역(FieldArea)에 연결한다
    [CreateAssetMenu(fileName = "NewShop", menuName = "WordRPG/Shop", order = 21)]
    public class ShopData : ScriptableObject
    {
        [SerializeField] private string displayName; // 예: "초원 마을 잡화점"
        [SerializeField] private List<ShopEntry> entries = new List<ShopEntry>();

        public string DisplayName => displayName;
        public IReadOnlyList<ShopEntry> Entries => entries;
    }

    public static class Shop
    {
        public static PurchaseResult TryBuy(Inventory inventory, ShopEntry entry)
        {
            if (entry == null || entry.Item == null || entry.Price <= 0) return PurchaseResult.NotForSale;
            if (!inventory.TrySpendGold(entry.Price)) return PurchaseResult.NotEnoughGold;
            inventory.Add(entry.Item);
            return PurchaseResult.Bought;
        }
    }
}
