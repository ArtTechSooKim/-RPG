using System;
using WordRPG.Items;

namespace WordRPG.Heroes
{
    public enum UpgradeStatus
    {
        Ready,          // 강화할 수 있음
        MaxLevel,       // +5 — 더 강화할 수 없음
        NotEnoughItems, // 재료 부족
        NotEnoughGold   // 골드 부족
    }

    // 성유물 강화 = 재료 + 골드 소모로 +1. 상점에서는 재료를 팔지 않으므로 전투·보물상자·보스로 모은다
    public static class RelicUpgrade
    {
        public static UpgradeStatus Check(OwnedRelic relic, Inventory inventory)
        {
            if (relic == null) throw new ArgumentNullException(nameof(relic));
            if (inventory == null) throw new ArgumentNullException(nameof(inventory));
            var cost = relic.Data.CostFrom(relic.Level);
            if (relic.IsMaxLevel || cost == null) return UpgradeStatus.MaxLevel;
            if (relic.Data.UpgradeItem != null && inventory.GetCount(relic.Data.UpgradeItem) < cost.ItemCount)
                return UpgradeStatus.NotEnoughItems;
            if (inventory.Gold < cost.Gold) return UpgradeStatus.NotEnoughGold;
            return UpgradeStatus.Ready;
        }

        // 성공하면 재료·골드를 쓰고 한 단계 올린다. 반환: 성공 여부
        public static bool TryUpgrade(OwnedRelic relic, Inventory inventory)
        {
            if (Check(relic, inventory) != UpgradeStatus.Ready) return false;
            var cost = relic.Data.CostFrom(relic.Level);
            if (relic.Data.UpgradeItem != null && cost.ItemCount > 0)
                inventory.TryRemove(relic.Data.UpgradeItem, cost.ItemCount);
            inventory.TrySpendGold(cost.Gold);
            relic.Level++;
            return true;
        }
    }
}
