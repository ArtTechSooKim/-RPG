using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WordRPG.Game;
using WordRPG.Items;

namespace WordRPG.UI
{
    // 마을 상점: 골드로 아이템(지금은 진화 재료) 구매
    public class ShopView
    {
        private const int MaxRows = 6;

        private class Row
        {
            public GameObject Root;
            public Text Info;
            public Button Buy;
            public Text BuyLabel;
        }

        public GameObject Root { get; private set; }
        public bool IsOpen => Root != null && Root.activeSelf;

        private readonly List<Row> rows = new List<Row>();
        private Text title, goldText, messageText;
        private ShopData shop;
        private GameSession session;
        private Action onChanged;

        public static ShopView Create(Transform parent)
        {
            var view = new ShopView();
            var root = UiKit.Stretch("ShopView", parent);
            view.Root = root.gameObject;
            view.Root.AddComponent<Image>().color = Palette.Background;

            view.title = UiKit.Label("Title", root, "상점", 50, Palette.Gold, 0, 0.92f, 1, 0.99f, TextAnchor.MiddleCenter, FontStyle.Bold);
            view.goldText = UiKit.Label("Gold", root, "", 36, Palette.Text, 0, 0.88f, 1, 0.925f);

            for (int i = 0; i < MaxRows; i++)
            {
                float top = 0.865f - i * 0.1f;
                var panel = UiKit.Panel($"ShopRow_{i}", root, Palette.Panel, 0.03f, top - 0.09f, 0.97f, top);
                var row = new Row { Root = panel.gameObject };
                row.Info = UiKit.Label("Info", panel.transform, "", 30, Palette.Text, 0.03f, 0.05f, 0.7f, 0.95f,
                    TextAnchor.MiddleLeft, FontStyle.Normal, true, 18);
                row.Buy = UiKit.MakeButton($"BuyButton_{i}", panel.transform, "", Palette.Button, 34, 0.72f, 0.12f, 0.97f, 0.88f);
                row.BuyLabel = UiKit.LabelOf(row.Buy);
                int index = i;
                row.Buy.onClick.AddListener(() => view.OnBuy(index));
                view.rows.Add(row);
            }

            var messageBox = UiKit.Panel("MessageBox", root, Palette.PanelLight, 0.03f, 0.115f, 0.97f, 0.24f);
            view.messageText = UiKit.Label("Message", messageBox.transform, "", 34, Palette.Text, 0, 0, 1, 1,
                TextAnchor.MiddleCenter, FontStyle.Normal, true, 20);
            UiKit.Pad(view.messageText.rectTransform, 20, 6, 20, 6);

            var close = UiKit.MakeButton("ShopCloseButton", root, "닫기", new Color(0.35f, 0.35f, 0.4f), 44,
                0.25f, 0.015f, 0.75f, 0.095f);
            close.onClick.AddListener(view.Hide);

            view.Root.SetActive(false);
            return view;
        }

        public void Show(ShopData shopData, GameSession gameSession, Action changed)
        {
            shop = shopData;
            session = gameSession;
            onChanged = changed;
            title.text = string.IsNullOrEmpty(shop.DisplayName) ? "상점" : shop.DisplayName;
            messageText.text = "어서 오세요! 진화 재료가 부족하면 여기서 사 가세요";
            Root.SetActive(true);
            Refresh();
        }

        public void Hide() => Root.SetActive(false);

        private void Refresh()
        {
            goldText.text = $"보유 골드  {session.Inventory.Gold}G";
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                bool has = i < shop.Entries.Count && shop.Entries[i].Item != null;
                row.Root.SetActive(has);
                if (!has) continue;

                var entry = shop.Entries[i];
                int owned = session.Inventory.GetCount(entry.Item);
                row.Info.text = $"<b>{entry.Item.DisplayName}</b>   보유 {owned}\n<size=24>{entry.Item.Description}</size>";
                row.BuyLabel.text = $"{entry.Price}G";
                row.Buy.interactable = session.Inventory.Gold >= entry.Price;
            }
        }

        private void OnBuy(int index)
        {
            var entry = shop.Entries[index];
            switch (Shop.TryBuy(session.Inventory, entry))
            {
                case PurchaseResult.Bought:
                    messageText.text = $"{UiKit.WithJosa(entry.Item.DisplayName, "을", "를")} 샀다! (보유 {session.Inventory.GetCount(entry.Item)}개)";
                    onChanged?.Invoke();
                    break;
                case PurchaseResult.NotEnoughGold:
                    messageText.text = "골드가 부족해요. 전투와 보물상자로 모아 보세요";
                    break;
                default:
                    messageText.text = "지금은 팔지 않는 물건이에요";
                    break;
            }
            Refresh();
        }
    }
}
