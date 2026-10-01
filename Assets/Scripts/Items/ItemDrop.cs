using System;
using UnityEngine;

namespace WordRPG.Items
{
    [Serializable]
    public class ItemDrop
    {
        [SerializeField] private ItemData item;
        [Range(0f, 1f)]
        [SerializeField] private float chance = 0.5f;
        [SerializeField] private int count = 1;

        public ItemData Item => item;
        public float Chance => chance;
        public int Count => count;

        private ItemDrop() { } // Unity 직렬화용

        public ItemDrop(ItemData item, float chance, int count = 1)
        {
            this.item = item;
            this.chance = chance;
            this.count = count;
        }
    }
}
