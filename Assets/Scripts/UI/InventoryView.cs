using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WordRPG.Game;
using WordRPG.Items;
using WordRPG.Monsters;

namespace WordRPG.UI
{
    // 소지품 (Figma '소지품 — 재료' / '소지품 — 징표 진열장'): 재료 탭 = 4칸 격자 + 고른 아이템 상세(쓰는 곳),
    // 징표 탭 = 지역 도감 완성 징표 진열장. 아이템은 세이브에 id만 있어서 GameDatabase로 찾는다
    public class InventoryView
    {
        public const int SlotCount = 8;
        private const int ShelfCount = 3;

        private class Slot
        {
            public GameObject Root;
            public Image Icon;
            public Text Count;
            public Image Border;
        }

        private class Shelf
        {
            public GameObject Root;
            public Image Display;
            public Image Glow;
            public Image Icon;
            public Text Region;
            public Text Name;
            public Image Border;
        }

        public GameObject Root { get; private set; }
        public bool IsOpen => Root != null && Root.activeSelf;
        public bool ShowingKeepsakes { get; private set; }

        private Text goldText;
        private Button materialsTab, keepsakesTab;
        private GameObject materialsPage, keepsakesPage;
        private readonly List<Slot> slots = new List<Slot>();
        private readonly List<Shelf> shelves = new List<Shelf>();
        private Image detailIcon, usageIcon;
        private Text detailName, detailMeta, detailDesc, usageLine1, usageLine2;
        private GameObject usageBox;

        private GameSession session;
        private GameDatabase database;
        private readonly List<ItemData> materials = new List<ItemData>();
        private List<KeepsakeEntry> keepsakes = new List<KeepsakeEntry>();

        public static InventoryView Create(Transform parent)
        {
            var view = new InventoryView();
            var root = UiKit.Stretch("InventoryView", parent);
            view.Root = root.gameObject;
            view.Root.AddComponent<Image>().color = Palette.Background;

            UiKit.Display(UiKit.Label("Title", root, "소지품", 56, Palette.Gold, 0, 0.93f, 1, 0.99f));
            view.goldText = UiKit.IconTitle("Gold", root, UiKit.Icon("gold"), "", 44, Palette.Text, 0, 0.885f, 1, 0.925f);

            var tabs = UiKit.RoundPanel("Tabs", root, Palette.Track, UiKit.RadiusMd, 0.022f, 0.815f, 0.978f, 0.872f);
            view.materialsTab = UiKit.MakeButton("TabMaterials", tabs.transform, "재료", Palette.Button, 44, 0.008f, 0.1f, 0.496f, 0.9f);
            view.keepsakesTab = UiKit.MakeButton("TabKeepsakes", tabs.transform, "징표", Color.clear, 44, 0.504f, 0.1f, 0.992f, 0.9f);
            view.materialsTab.onClick.AddListener(() => view.SelectTab(false));
            view.keepsakesTab.onClick.AddListener(() => view.SelectTab(true));

            view.BuildMaterials(root);
            view.BuildKeepsakes(root);
            view.BuildDetail(root);

            var close = UiKit.MakeButton("InventoryCloseButton", root, "닫기", Palette.Neutral, 44, 0.05f, 0.015f, 0.95f, 0.095f);
            close.onClick.AddListener(view.Hide);

            view.Root.SetActive(false);
            return view;
        }

        // 4칸 × 2줄 (Figma 'Item Slot' 240×240)
        private void BuildMaterials(RectTransform root)
        {
            var page = UiKit.Rect("MaterialsPage", root, 0.022f, 0.53f, 0.978f, 0.8f);
            materialsPage = page.gameObject;
            const float gapX = 24f / 1032f, gapY = 24f / 518f;
            float w = (1f - 3 * gapX) / 4f, h = (1f - gapY) / 2f;
            for (int i = 0; i < SlotCount; i++)
            {
                int col = i % 4, row = i / 4;
                float x = col * (w + gapX), top = 1f - row * (h + gapY);
                var panel = UiKit.RoundPanel($"ItemSlot_{i}", page, Palette.Panel, UiKit.RadiusMd, x, top - h, x + w, top);
                var button = UiKit.AddButton(panel);
                var colors = button.colors;
                colors.disabledColor = Color.white; // 빈 칸 색은 직접 지정 (반투명 판)
                button.colors = colors;
                int index = i;
                button.onClick.AddListener(() => ShowMaterial(index));
                var slot = new Slot { Root = panel.gameObject };
                slot.Icon = UiKit.IconImage("Icon", panel.transform, null, 0.18f, 0.32f, 0.82f, 0.92f);
                slot.Count = UiKit.Display(UiKit.Label("Count", panel.transform, "", 44, Palette.Text, 0, 0.04f, 1, 0.32f));
                slot.Border = UiKit.Outline(UiKit.Panel("Border", panel.transform, Palette.Gold), UiKit.RadiusMd, 6);
                slot.Border.raycastTarget = false;
                slots.Add(slot);
            }
        }

        // 진열장 선반 3칸 + 나무 선반 (Figma 'Keepsake Slot')
        private void BuildKeepsakes(RectTransform root)
        {
            var page = UiKit.Rect("KeepsakesPage", root, 0.022f, 0.53f, 0.978f, 0.8f);
            keepsakesPage = page.gameObject;
            UiKit.Label("Intro", page, "지역 단어 도감을 모두 채우면 그 지역의 징표를 받아요", 30, Palette.TextDim, 0, 0.9f, 1, 1);
            UiKit.Pill(UiKit.Panel("Plank", page, new Color(0.42f, 0.31f, 0.22f), 0, 0.02f, 1, 0.06f)).raycastTarget = false;
            const float gap = 36f / 1032f;
            float w = (1f - 2 * gap) / ShelfCount;
            for (int i = 0; i < ShelfCount; i++)
            {
                float x = i * (w + gap);
                var panel = UiKit.RoundPanel($"KeepsakeSlot_{i}", page, Palette.Panel, UiKit.RadiusLg, x, 0.07f, x + w, 0.88f);
                var button = UiKit.AddButton(panel);
                int index = i;
                button.onClick.AddListener(() => ShowKeepsake(index));
                var shelf = new Shelf { Root = panel.gameObject };
                shelf.Glow = UiKit.IconImage("Glow", panel.transform, UiKit.GlowSprite(), 0.5f, 0.66f, 0.5f, 0.66f);
                shelf.Glow.rectTransform.sizeDelta = new Vector2(280, 280);
                shelf.Glow.color = new Color(Palette.Gold.r, Palette.Gold.g, Palette.Gold.b, 0.45f);
                shelf.Display = UiKit.Pill(UiKit.Panel("Display", panel.transform, Palette.PanelLight, 0.5f, 0.66f, 0.5f, 0.66f));
                shelf.Display.raycastTarget = false;
                shelf.Display.rectTransform.sizeDelta = new Vector2(190, 190);
                shelf.Icon = UiKit.IconImage("Icon", shelf.Display.transform, null, 0.14f, 0.14f, 0.86f, 0.86f);
                shelf.Region = UiKit.Label("Region", panel.transform, "", 28, Palette.Gold, 0.04f, 0.2f, 0.96f, 0.33f);
                shelf.Name = UiKit.Label("Name", panel.transform, "", 32, Palette.Text, 0.04f, 0.04f, 0.96f, 0.2f,
                    TextAnchor.MiddleCenter, FontStyle.Bold, true, 20);
                shelf.Border = UiKit.Outline(UiKit.Panel("Border", panel.transform, Palette.Gold), UiKit.RadiusLg, 4);
                shelf.Border.raycastTarget = false;
                shelves.Add(shelf);
            }
        }

        // 고른 아이템 상세: 그림 · 이름 · 보유 수 · 설명 · 쓰는 곳
        private void BuildDetail(RectTransform root)
        {
            var box = UiKit.RoundPanel("DetailBox", root, Palette.PanelLight, UiKit.RadiusLg, 0.022f, 0.28f, 0.978f, 0.515f);
            detailIcon = UiKit.IconImage("Icon", box.transform, null, 0.03f, 0.6f, 0.155f, 0.92f);
            detailName = UiKit.Display(UiKit.Label("Name", box.transform, "", 44, Palette.Text, 0.18f, 0.76f, 0.97f, 0.93f,
                TextAnchor.MiddleLeft, FontStyle.Normal, true, 24));
            detailMeta = UiKit.Label("Meta", box.transform, "", 30, Palette.TextDim, 0.18f, 0.6f, 0.97f, 0.76f,
                TextAnchor.MiddleLeft, FontStyle.Normal, true, 18);
            detailDesc = UiKit.Label("Description", box.transform, "", 34, Palette.Text, 0.031f, 0.37f, 0.97f, 0.59f,
                TextAnchor.MiddleLeft, FontStyle.Normal, true, 20);
            var usage = UiKit.RoundPanel("Usage", box.transform, Palette.Panel, UiKit.RadiusMd, 0.031f, 0.06f, 0.969f, 0.34f);
            usageBox = usage.gameObject;
            usageIcon = UiKit.IconImage("Icon", usage.transform, null, 0.02f, 0.2f, 0.085f, 0.8f);
            usageLine1 = UiKit.Label("Line1", usage.transform, "", 30, Palette.Text, 0.11f, 0.5f, 0.98f, 0.92f,
                TextAnchor.MiddleLeft, FontStyle.Normal, true, 18);
            usageLine2 = UiKit.Label("Line2", usage.transform, "", 30, Palette.TextDim, 0.11f, 0.08f, 0.98f, 0.5f,
                TextAnchor.MiddleLeft, FontStyle.Normal, true, 18);
        }

        public void Show(GameSession gameSession, GameDatabase gameDatabase)
        {
            session = gameSession;
            database = gameDatabase;
            goldText.text = $"보유 골드  {session.Inventory.Gold}G";

            materials.Clear();
            foreach (var stack in session.Inventory.Stacks)
            {
                var item = database != null ? database.FindItem(stack.ItemId) : null;
                if (item != null && item.Kind != ItemKind.Keepsake) materials.Add(item);
            }
            keepsakes = Keepsakes.Collect(database, session);

            Root.transform.SetAsLastSibling();
            Root.SetActive(true);
            SelectTab(false);
        }

        public void Hide() => Root.SetActive(false);

        private void SelectTab(bool keepsakeTab)
        {
            ShowingKeepsakes = keepsakeTab;
            materialsPage.SetActive(!keepsakeTab);
            keepsakesPage.SetActive(keepsakeTab);
            StyleTab(materialsTab, !keepsakeTab);
            StyleTab(keepsakesTab, keepsakeTab);
            if (keepsakeTab) ShowKeepsake(0);
            else ShowMaterial(0);
        }

        private static void StyleTab(Button tab, bool selected)
        {
            UiKit.SetColor(tab, selected ? Palette.Button : Color.clear);
            UiKit.LabelOf(tab).color = selected ? Palette.Text : Palette.TextDim;
        }

        // ------------------------------------------------------------------ 재료

        private void ShowMaterial(int selected)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                bool has = i < materials.Count;
                slot.Root.GetComponent<Image>().color = has ? (i == selected ? Palette.PanelLight : Palette.Panel)
                    : new Color(Palette.Panel.r, Palette.Panel.g, Palette.Panel.b, 0.5f);
                slot.Root.GetComponent<Button>().interactable = has;
                slot.Border.enabled = has && i == selected;
                slot.Icon.sprite = has ? UiKit.ItemIcon(materials[i]) : null;
                slot.Icon.enabled = slot.Icon.sprite != null;
                slot.Count.text = has ? $"× {session.Inventory.GetCount(materials[i])}" : "";
            }

            if (materials.Count == 0)
            {
                SetDetail(UiKit.Icon("bag"), "아직 재료가 없어요", "", "보물상자 · 전투 · 상점에서 진화 재료를 모아 보세요");
                usageBox.SetActive(false);
                return;
            }

            var item = materials[Mathf.Clamp(selected, 0, materials.Count - 1)];
            SetDetail(UiKit.ItemIcon(item), item.DisplayName, $"보유 {session.Inventory.GetCount(item)}개", item.Description);
            ShowUsage(item);
        }

        private void ShowUsage(ItemData item)
        {
            var user = Evolution.FindUser(session.Party, item);
            if (user == null)
            {
                SetUsage(UiKit.Icon("star_full"), "쓰는 곳  진화의 제단", "지금 파티에는 이 재료로 진화하는 몬스터가 없어요", Palette.TextDim);
                return;
            }

            var species = user.Species;
            string where = $"쓰는 곳  진화의 제단 · {user.DisplayName} → {species.EvolvesTo.DisplayName}";
            var role = UiKit.Icon(RoleIconName(species.Role));
            int owned = session.Inventory.GetCount(item);
            switch (Evolution.Check(user, session.Inventory))
            {
                case EvolutionStatus.Ready:
                    SetUsage(role, where, $"Lv{species.EvolveLevel} ✓   재료 {owned} / {species.EvolveItemCount} ✓   지금 진화할 수 있어요!", Palette.Good);
                    break;
                case EvolutionStatus.LevelTooLow:
                    SetUsage(role, where, $"Lv{species.EvolveLevel}이 되면 진화할 수 있어요 (지금 Lv{user.Level})   재료 {owned} / {species.EvolveItemCount}", Palette.TextDim);
                    break;
                default:
                    SetUsage(role, where, $"재료 {owned} / {species.EvolveItemCount} — {species.EvolveItemCount - owned}개 더 모으면 돼요", Palette.TextDim);
                    break;
            }
        }

        // 쓰는 곳 상자: 두 줄, 둘째 줄이 없으면 첫 줄을 가운데로
        private void SetUsage(Sprite icon, string line1, string line2, Color line2Color)
        {
            usageBox.SetActive(true);
            usageIcon.sprite = icon;
            usageIcon.enabled = icon != null;
            usageLine1.text = line1;
            usageLine2.text = line2 ?? "";
            usageLine2.color = line2Color;
            usageLine1.rectTransform.anchorMin = new Vector2(0.11f, string.IsNullOrEmpty(line2) ? 0.08f : 0.5f);
        }

        internal static string RoleIconName(MonsterRole role)
        {
            switch (role)
            {
                case MonsterRole.Attacker: return "role_attacker";
                case MonsterRole.Defender: return "role_defender";
                default: return "role_supporter";
            }
        }

        // ------------------------------------------------------------------ 징표 진열장

        private void ShowKeepsake(int selected)
        {
            for (int i = 0; i < shelves.Count; i++)
            {
                var shelf = shelves[i];
                var entry = i < keepsakes.Count ? keepsakes[i] : null;
                bool owned = entry != null && entry.Owned;
                shelf.Root.GetComponent<Image>().color = i == selected ? Palette.PanelLight : Palette.Panel;
                shelf.Border.enabled = owned || i == selected;
                shelf.Border.color = owned ? new Color(Palette.Gold.r, Palette.Gold.g, Palette.Gold.b, i == selected ? 1f : 0.6f) : Palette.TextDim;
                shelf.Glow.enabled = owned;
                shelf.Display.color = owned ? Palette.PanelLight : Palette.Track;
                shelf.Icon.sprite = owned ? UiKit.ItemIcon(entry.Keepsake) : UiKit.Icon("lock");
                shelf.Icon.enabled = shelf.Icon.sprite != null;
                shelf.Icon.color = owned ? Color.white : new Color(1f, 1f, 1f, 0.5f);
                shelf.Region.text = entry != null ? entry.RegionName : "다음 지역";
                shelf.Region.color = owned ? Palette.Gold : Palette.TextDim;
                shelf.Name.text = owned ? entry.Keepsake.DisplayName : "???";
                shelf.Name.color = owned ? Palette.Text : Palette.TextDim;
            }

            usageBox.SetActive(false);
            var chosen = selected < keepsakes.Count ? keepsakes[selected] : null;
            if (chosen == null)
            {
                SetDetail(UiKit.Icon("lock"), "???", "다음 지역", "새 지역이 열리면 그 지역의 징표 자리가 생겨요");
                return;
            }

            var progress = chosen.Progress;
            if (!chosen.Owned)
            {
                SetDetail(UiKit.Icon("lock"), "???", $"{chosen.RegionName} 도감 {progress.Discovered} / {progress.Total}",
                    $"{chosen.RegionName} 단어 도감을 모두 채우면 받을 수 있어요");
                return;
            }

            SetDetail(UiKit.ItemIcon(chosen.Keepsake), chosen.Keepsake.DisplayName,
                $"{chosen.RegionName} 도감 {progress.Total} / {progress.Total} 완성", chosen.Keepsake.Description);
            detailMeta.color = Palette.Gold;
            if (chosen.Region.CompletionGold > 0)
                SetUsage(UiKit.Icon("gold"), $"완성 보상으로 {chosen.Region.CompletionGold} 골드도 함께 받았어요", null, Palette.TextDim);
        }

        private void SetDetail(Sprite icon, string name, string meta, string description)
        {
            detailIcon.sprite = icon;
            detailIcon.enabled = icon != null;
            detailName.text = name;
            detailMeta.text = meta;
            detailMeta.color = Palette.TextDim;
            detailDesc.text = description;
        }
    }
}
