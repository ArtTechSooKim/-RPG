using System;
using System.Collections.Generic;
using UnityEngine;

namespace WordRPG.Items
{
    [Serializable]
    public class ItemStack
    {
        [SerializeField] private string itemId;
        [SerializeField] private int count;

        public string ItemId => itemId;
        public int Count { get => count; internal set => count = value; }

        private ItemStack() { } // Unity 직렬화용

        public ItemStack(string itemId, int count)
        {
            this.itemId = itemId;
            this.count = count;
        }
    }

    // 소지품 + 골드. 세이브 파일에 그대로 들어가므로 아이템은 itemId 문자열로 보관
    [Serializable]
    public class Inventory
    {
        [SerializeField] private int gold;
        [SerializeField] private List<ItemStack> stacks = new List<ItemStack>();

        public int Gold => gold;
        public IReadOnlyList<ItemStack> Stacks => stacks;

        public int GetCount(ItemData item) => GetCount(item.ItemId);

        public int GetCount(string itemId)
        {
            var stack = stacks.Find(s => s.ItemId == itemId);
            return stack?.Count ?? 0;
        }

        public void Add(ItemData item, int count = 1) => Add(item.ItemId, count);

        public void Add(string itemId, int count = 1)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            var stack = stacks.Find(s => s.ItemId == itemId);
            if (stack == null) stacks.Add(new ItemStack(itemId, count));
            else stack.Count += count;
        }

        public bool TryRemove(ItemData item, int count = 1) => TryRemove(item.ItemId, count);

        public bool TryRemove(string itemId, int count = 1)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            var stack = stacks.Find(s => s.ItemId == itemId);
            if (stack == null || stack.Count < count) return false;

            stack.Count -= count;
            if (stack.Count == 0) stacks.Remove(stack);
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            gold += amount;
        }

        public bool TrySpendGold(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (gold < amount) return false;
            gold -= amount;
            return true;
        }
    }
}
