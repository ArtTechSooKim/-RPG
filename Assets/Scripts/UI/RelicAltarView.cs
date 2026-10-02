using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using WordRPG.Game;
using WordRPG.Heroes;
using WordRPG.Monsters;

namespace WordRPG.UI
{
    // 성유물 제단 (Figma '성유물 제단' / 'Relic Upgrade Card'): 모은 성유물마다 다음 단계 비용(재료·골드)을 보여주고 강화.
    // 재료·골드를 쓰므로 버튼을 두 번 눌러야 한다. +3이 되면 각성 연출(기술이 더 강한 기술로)
    public class RelicAltarView
    {
        public const int MaxCards = 5; // 지금 데이터의 성유물 수. 더 늘리면 스크롤로 바꿀 것

        private class Card
        {
            public GameObject Root;
            public Image Border;
            public Image Swatch;
            public Image Sprite;
            public Image RoleIcon;
            public Text Name;
            public Text Requirement;
            public Button Button;
            public Text ButtonLabel;
        }

        public GameObject Root { get; private set; }
        public bool IsOpen => Root != null && Root.activeSelf;
        public AwakeningCutscene Cutscene { get; private set; }

        private readonly List<Card> cards = new List<Card>();
        private RectTransform materials;
        private Text resultText;
        private GameSession session;
        private Action onChanged;
        private int confirmIndex = -1;

        // animScale: 각성 연출 시간 배율 (테스트에서는 아주 작게)
        public static RelicAltarView Create(Transform parent, float animScale = 1f)
        {
            var view = new RelicAltarView();
            var root = UiKit.Stretch("RelicAltarView", parent);
            view.Root = root.gameObject;
            view.Root.AddComponent<Image>().color = Palette.Background;

            UiKit.Display(UiKit.Label("Title", root, "성유물 제단", 56, Palette.Gold, 0, 0.925f, 1, 0.99f));
            UiKit.Label("Subtitle", root, "재료와 골드로 성유물을 강화해요 · +3이 되면 각성", 30, Palette.TextDim, 0, 0.89f, 1, 0.928f);

            // 보유 재료 · 골드 한눈에 (아이콘 × 개수)
            UiKit.RoundPanel("MaterialsBox", root, Palette.Panel, UiKit.RadiusMd, 0.03f, 0.83f, 0.97f, 0.885f).raycastTarget = false;
            view.materials = UiKit.Rect("Materials", root, 0.03f, 0.83f, 0.97f, 0.885f);
            var layout = view.materials.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 40;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            for (int i = 0; i < MaxCards; i++) view.cards.Add(view.BuildCard(root, i));

            var resultBox = UiKit.RoundPanel("ResultBox", root, Palette.PanelLight, UiKit.RadiusMd, 0.03f, 0.115f, 0.97f, 0.235f);
            view.resultText = UiKit.Label("Result", resultBox.transform, "", 32, Palette.Text, 0, 0, 1, 1,
                TextAnchor.MiddleCenter, FontStyle.Normal, true, 20);
            UiKit.Pad(view.resultText.rectTransform, 20, 8, 20, 8);

            var close = UiKit.MakeButton("RelicAltarCloseButton", root, "닫기", Palette.Neutral, 44, 0.05f, 0.015f, 0.95f, 0.095f);
            close.onClick.AddListener(view.Hide);

            view.Cutscene = AwakeningCutscene.Create(root, animScale);
            view.Root.SetActive(false);
            return view;
        }

        private Card BuildCard(RectTransform root, int index)
        {
            var panel = UiKit.RoundPanel($"RelicCard_{index}", root, Palette.Panel, UiKit.RadiusLg);
            var card = new Card { Root = panel.gameObject };
            card.Swatch = UiKit.RoundPanel("Swatch", panel.transform, Palette.PanelLight, UiKit.RadiusMd, 0.025f, 0.1f, 0.2f, 0.9f);
            card.Sprite = UiKit.IconImage("Sprite", card.Swatch.transform, null, 0.1f, 0.1f, 0.9f, 0.9f);
            card.RoleIcon = UiKit.IconImage("Role", panel.transform, null, 0.23f, 0.7f, 0.28f, 0.94f);
            card.Name = UiKit.Display(UiKit.Label("Name", panel.transform, "", 42, Palette.Text, 0.29f, 0.68f, 0.98f, 0.96f,
                TextAnchor.MiddleLeft, FontStyle.Normal, true, 20));
            card.Requirement = UiKit.Label("Info", panel.transform, "", 30, Palette.TextDim, 0.23f, 0.42f, 0.98f, 0.68f,
                TextAnchor.MiddleLeft, FontStyle.Normal, true, 16);
            card.Button = UiKit.MakeButton($"UpgradeButton_{index}", panel.transform, "", Palette.Disabled, 38,
                0.23f, 0.08f, 0.97f, 0.38f, bestFit: true);
            var colors = card.Button.colors;
            colors.disabledColor = Color.white; // 잠김 색은 직접 지정
            card.Button.colors = colors;
            card.ButtonLabel = UiKit.LabelOf(card.Button);
            card.Border = UiKit.Outline(UiKit.Panel("Border", panel.transform, Color.clear), UiKit.RadiusLg, 4);
            card.Border.raycastTarget = false;
            card.Button.onClick.AddListener(() => OnUpgradeClicked(index));
            return card;
        }

        public void Show(GameSession gameSession, Action changed)
        {
            session = gameSession;
            onChanged = changed;
            confirmIndex = -1;
            resultText.text = "강화할 성유물을 고르세요\n+3에서 각성하면 기술이 더 강한 기술로 바뀌어요";
            Root.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            confirmIndex = -1;
            Cutscene.Stop();
            Root.SetActive(false);
        }

        private void Refresh()
        {
            RefreshMaterials();
            var relics = session.Hero.Relics;
            int n = Mathf.Min(relics.Count, cards.Count);
            // 카드 수에 맞춰 세로로 나눠 놓는다 (3장까지는 넉넉하게)
            const float top = 0.815f, bottom = 0.25f, gap = 0.012f;
            float h = n > 0 ? Mathf.Min(0.16f, (top - bottom - (n - 1) * gap) / n) : 0.16f;
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                bool has = i < n;
                card.Root.SetActive(has);
                if (!has) continue;
                var rt = (RectTransform)card.Root.transform;
                float cardTop = top - i * (h + gap);
                rt.anchorMin = new Vector2(0.03f, cardTop - h);
                rt.anchorMax = new Vector2(0.97f, cardTop);

                var relic = relics[i];
                card.Swatch.color = Color.Lerp(Palette.PanelLight, relic.Data.PlaceholderColor, 0.22f);
                card.Sprite.sprite = UiKit.RelicIcon(relic.Data);
                card.Sprite.enabled = card.Sprite.sprite != null;
                var role = UiKit.Icon(InventoryView.RoleIconName(relic.Data.Role));
                card.RoleIcon.sprite = role;
                card.RoleIcon.enabled = role != null;
                card.Name.text = $"{relic.Data.DisplayName}  +{relic.Level}" + (session.Hero.IsEquipped(relic) ? "   <size=26><color=#A6B3D1>장착 중</color></size>" : "");
                card.Requirement.text = Requirement(relic);

                var status = RelicUpgrade.Check(relic, session.Inventory);
                bool ready = status == UpgradeStatus.Ready;
                bool confirming = confirmIndex == i && ready;
                card.Button.interactable = ready;
                UiKit.SetColor(card.Button, confirming ? Palette.Confirm : ready ? Palette.Evolve : Palette.Disabled);
                card.ButtonLabel.color = ready ? Palette.Text : Palette.TextDim;
                card.ButtonLabel.text = confirming ? "한 번 더 누르면 강화!" : ButtonText(status);
                card.Border.color = confirming ? Palette.Confirm : ready ? Palette.Gold : Color.clear;
            }
        }

        // 모은 성유물의 강화 재료와 보유 개수 + 골드
        private void RefreshMaterials()
        {
            foreach (Transform child in materials)
            {
                child.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
            var seen = new HashSet<string>();
            foreach (var relic in session.Hero.Relics)
            {
                var item = relic.Data.UpgradeItem;
                if (item == null || !seen.Add(item.ItemId)) continue;
                AddEntry(UiKit.ItemIcon(item), $"× {session.Inventory.GetCount(item)}");
            }
            AddEntry(UiKit.Icon("gold"), $"{session.Inventory.Gold}G");
        }

        private void AddEntry(Sprite sprite, string label)
        {
            var entry = UiKit.Rect("Material", materials, 0, 0, 1, 1);
            var row = entry.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter;
            row.spacing = 6;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var icon = UiKit.IconImage("Icon", entry, sprite, 0, 0, 1, 1);
            var size = icon.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = size.preferredHeight = 56;
            var count = UiKit.Display(UiKit.Label("Count", entry, label, 40, Palette.Text, 0, 0, 1, 1));
            count.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private string Requirement(OwnedRelic relic)
        {
            var cost = relic.Data.CostFrom(relic.Level);
            if (relic.IsMaxLevel || cost == null) return "<color=#A0A8C0>더 이상 강화할 수 없는 최대 단계예요</color>";

            var text = new StringBuilder($"→ +{relic.Level + 1}");
            if (relic.Data.AwakensWhenUpgradedFrom(relic.Level)) text.Append($" <color=#FFD140>각성! {relic.Data.AwakenedSkill.DisplayName}</color>");
            text.Append("   ");
            var item = relic.Data.UpgradeItem;
            if (item != null)
            {
                int owned = session.Inventory.GetCount(item);
                text.Append($"{Mark(owned >= cost.ItemCount)}{item.DisplayName} {owned}/{cost.ItemCount}</color> · ");
            }
            text.Append($"{Mark(session.Inventory.Gold >= cost.Gold)}{cost.Gold}G</color>");
            return text.ToString();
        }

        private static string Mark(bool ok) => ok ? "<color=#5FD07A>" : "<color=#F07070>";

        private static string ButtonText(UpgradeStatus status)
        {
            switch (status)
            {
                case UpgradeStatus.Ready: return "강화!";
                case UpgradeStatus.NotEnoughItems: return "재료가 부족해요";
                case UpgradeStatus.NotEnoughGold: return "골드가 부족해요";
                default: return "최대 강화";
            }
        }

        private void OnUpgradeClicked(int index)
        {
            var relics = session.Hero.Relics;
            if (index >= relics.Count) return;
            var relic = relics[index];

            if (confirmIndex != index)
            {
                confirmIndex = index;
                var cost = relic.Data.CostFrom(relic.Level);
                string item = relic.Data.UpgradeItem != null ? $"{relic.Data.UpgradeItem.DisplayName} {cost.ItemCount}개 · " : "";
                int next = relic.Level + 1;
                resultText.text = $"{UiKit.WithJosa(relic.Data.DisplayName, "을", "를")} +{next}{UiKit.NumberRo(next)} 강화할까요?\n{item}{cost.Gold}G를 써요";
                Refresh();
                return;
            }

            confirmIndex = -1;
            var before = relic.Bonus;
            var oldSkill = relic.Skill;
            if (RelicUpgrade.TryUpgrade(relic, session.Inventory))
            {
                var after = relic.Bonus;
                bool awakened = relic.Skill != oldSkill;
                resultText.text = $"★ {relic.Data.DisplayName} +{relic.Level} 강화 성공!\n{BonusChange(before, after)}"
                                  + (awakened ? $"\n각성! {oldSkill.DisplayName} → {relic.Skill.DisplayName}" : "");
                onChanged?.Invoke();
                if (awakened)
                {
                    // 각성 연출 (Figma '각성 연출 1·2·3'). 끝나면 제단 화면으로 돌아온다
                    Cutscene.Play(relic.Data, relic.Level, before, after, relic.Skill, oldSkill, Refresh);
                }
                else Sound.Play(Sfx.LevelUp);
            }
            Refresh();
        }

        internal static string BonusChange(MonsterStats before, MonsterStats after)
        {
            var parts = new List<string>();
            if (after.MaxHp != before.MaxHp) parts.Add($"HP +{before.MaxHp}→+{after.MaxHp}");
            if (after.Attack != before.Attack) parts.Add($"공격 +{before.Attack}→+{after.Attack}");
            if (after.Defense != before.Defense) parts.Add($"방어 +{before.Defense}→+{after.Defense}");
            return parts.Count > 0 ? "보너스 " + string.Join("   ", parts) : "보너스는 그대로";
        }
    }
}
