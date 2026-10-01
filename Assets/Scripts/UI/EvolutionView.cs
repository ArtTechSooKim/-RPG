using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using WordRPG.Game;
using WordRPG.Monsters;

namespace WordRPG.UI
{
    // 진화의 제단: 파티 몬스터별 진화 조건(레벨·재료)을 보여주고, 조건이 되면 진화.
    // 진화는 되돌릴 수 없으므로 버튼을 두 번 눌러야 한다
    public class EvolutionView
    {
        private class Card
        {
            public GameObject Root;
            public Image Border;
            public Image Swatch;
            public Text Initial;
            public Image Sprite;
            public Image RoleIcon;
            public Text Name;
            public Text Requirement;
            public Button Button;
            public Text ButtonLabel;
        }

        public GameObject Root { get; private set; }
        public bool IsOpen => Root != null && Root.activeSelf;
        public EvolutionCutscene Cutscene { get; private set; }

        private readonly List<Card> cards = new List<Card>();
        private RectTransform materials;
        private Text resultText;
        private GameSession session;
        private Action onChanged;
        private int confirmIndex = -1;

        // Figma '진화의 제단' 화면 / 'Evolution Card'. animScale: 진화 연출 시간 배율 (테스트에서는 아주 작게)
        public static EvolutionView Create(Transform parent, float animScale = 1f)
        {
            var view = new EvolutionView();
            var root = UiKit.Stretch("EvolutionView", parent);
            view.Root = root.gameObject;
            view.Root.AddComponent<Image>().color = Palette.Background;

            UiKit.Display(UiKit.Label("Title", root, "진화의 제단", 56, Palette.Gold, 0, 0.925f, 1, 0.99f));
            UiKit.Label("Subtitle", root, "레벨과 재료가 모이면 진화할 수 있어요", 30, Palette.TextDim, 0, 0.89f, 1, 0.928f);

            // 보유 진화 재료 한눈에 (아이콘 × 개수)
            UiKit.RoundPanel("MaterialsBox", root, Palette.Panel, UiKit.RadiusMd, 0.03f, 0.83f, 0.97f, 0.885f).raycastTarget = false;
            view.materials = UiKit.Rect("Materials", root, 0.03f, 0.83f, 0.97f, 0.885f);
            var layout = view.materials.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 40;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            for (int i = 0; i < GameSession.MaxPartySize; i++)
            {
                float top = 0.815f - i * 0.172f;
                var panel = UiKit.RoundPanel($"EvolveCard_{i}", root, Palette.Panel, UiKit.RadiusLg, 0.03f, top - 0.16f, 0.97f, top);
                var card = new Card { Root = panel.gameObject };
                card.Swatch = UiKit.RoundPanel("Swatch", panel.transform, Palette.PanelLight, UiKit.RadiusMd, 0.025f, 0.1f, 0.2f, 0.9f);
                card.Initial = UiKit.Display(UiKit.Label("Initial", card.Swatch.transform, "", 80, Palette.Text, 0, 0, 1, 1,
                    TextAnchor.MiddleCenter, FontStyle.Normal, true, 24));
                card.Sprite = UiKit.IconImage("Sprite", card.Swatch.transform, null, 0.05f, 0.05f, 0.95f, 0.95f);
                card.RoleIcon = UiKit.IconImage("Role", panel.transform, null, 0.23f, 0.7f, 0.28f, 0.94f);
                card.Name = UiKit.Display(UiKit.Label("Name", panel.transform, "", 42, Palette.Text, 0.29f, 0.68f, 0.98f, 0.96f,
                    TextAnchor.MiddleLeft, FontStyle.Normal, true, 20));
                card.Requirement = UiKit.Label("Info", panel.transform, "", 30, Palette.TextDim, 0.23f, 0.42f, 0.98f, 0.68f,
                    TextAnchor.MiddleLeft, FontStyle.Normal, true, 16);
                card.Button = UiKit.MakeButton($"EvolveButton_{i}", panel.transform, "", Palette.Disabled, 38,
                    0.23f, 0.08f, 0.97f, 0.38f, bestFit: true);
                var colors = card.Button.colors;
                colors.disabledColor = Color.white; // 잠김 색은 직접 지정
                card.Button.colors = colors;
                card.ButtonLabel = UiKit.LabelOf(card.Button);
                card.Border = UiKit.Outline(UiKit.Panel("Border", panel.transform, Color.clear), UiKit.RadiusLg, 4);
                card.Border.raycastTarget = false;
                int index = i;
                card.Button.onClick.AddListener(() => view.OnEvolveClicked(index));
                view.cards.Add(card);
            }

            var resultBox = UiKit.RoundPanel("ResultBox", root, Palette.PanelLight, UiKit.RadiusMd, 0.03f, 0.115f, 0.97f, 0.29f);
            view.resultText = UiKit.Label("Result", resultBox.transform, "", 34, Palette.Text, 0, 0, 1, 1,
                TextAnchor.MiddleCenter, FontStyle.Normal, true, 20);
            UiKit.Pad(view.resultText.rectTransform, 20, 8, 20, 8);

            var close = UiKit.MakeButton("EvolutionCloseButton", root, "닫기", Palette.Neutral, 44,
                0.05f, 0.015f, 0.95f, 0.095f);
            close.onClick.AddListener(view.Hide);

            view.Cutscene = EvolutionCutscene.Create(root, animScale);
            view.Root.SetActive(false);
            return view;
        }

        public void Show(GameSession gameSession, Action changed)
        {
            session = gameSession;
            onChanged = changed;
            confirmIndex = -1;
            resultText.text = "진화시킬 몬스터를 고르세요\n진화하면 레벨은 그대로, HP는 모두 회복돼요";
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
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                bool has = i < session.Party.Count;
                card.Root.SetActive(has);
                if (!has) continue;

                var monster = session.Party[i];
                var species = monster.Species;
                card.Swatch.color = Color.Lerp(Palette.PanelLight, species.PlaceholderColor, 0.65f);
                card.Initial.text = species.DisplayName.Length > 0 ? species.DisplayName.Substring(0, 1) : "?";
                card.Sprite.sprite = species.Sprite;
                card.Sprite.enabled = species.Sprite != null;
                card.Initial.enabled = species.Sprite == null;
                var role = UiKit.Icon(RoleIconName(species.Role));
                card.RoleIcon.sprite = role;
                card.RoleIcon.enabled = role != null;
                card.Name.text = $"{species.DisplayName}  Lv{monster.Level}";
                card.Requirement.text = Requirement(monster);

                var status = Evolution.Check(monster, session.Inventory);
                bool ready = status == EvolutionStatus.Ready;
                bool confirming = confirmIndex == i && ready;
                card.Button.interactable = ready;
                UiKit.SetColor(card.Button, confirming ? Palette.Confirm : ready ? Palette.Evolve : Palette.Disabled);
                card.ButtonLabel.color = ready ? Palette.Text : Palette.TextDim;
                card.ButtonLabel.text = confirming ? "한 번 더 누르면 진화!" : ButtonText(status);
                card.Border.color = confirming ? Palette.Confirm : ready ? Palette.Gold : Color.clear;
            }
        }

        // 파티가 진화에 쓰는 재료와 보유 개수
        private void RefreshMaterials()
        {
            foreach (Transform child in materials)
            {
                child.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
            var seen = new HashSet<string>();
            foreach (var monster in session.Party)
            {
                var item = monster.Species.EvolvesTo != null ? monster.Species.EvolveItem : null;
                if (item == null || !seen.Add(item.ItemId)) continue;
                var entry = UiKit.Rect("Material", materials, 0, 0, 1, 1);
                var row = entry.gameObject.AddComponent<HorizontalLayoutGroup>();
                row.childAlignment = TextAnchor.MiddleCenter;
                row.spacing = 6;
                row.childControlWidth = row.childControlHeight = true;
                row.childForceExpandWidth = row.childForceExpandHeight = false;
                var icon = UiKit.IconImage("Icon", entry, UiKit.ItemIcon(item), 0, 0, 1, 1);
                var size = icon.gameObject.AddComponent<LayoutElement>();
                size.preferredWidth = size.preferredHeight = 64;
                var count = UiKit.Display(UiKit.Label("Count", entry, $"× {session.Inventory.GetCount(item)}", 40, Palette.Text, 0, 0, 1, 1));
                count.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
        }

        private static string RoleIconName(MonsterRole role)
        {
            switch (role)
            {
                case MonsterRole.Attacker: return "role_attacker";
                case MonsterRole.Defender: return "role_defender";
                default: return "role_supporter";
            }
        }

        private string Requirement(MonsterInstance monster)
        {
            var species = monster.Species;
            if (species.EvolvesTo == null) return "<color=#A0A8C0>더 이상 진화하지 않는 최종 형태예요</color>";

            bool levelOk = monster.Level >= species.EvolveLevel;
            var text = new StringBuilder($"→ {species.EvolvesTo.DisplayName}   ");
            text.Append($"{Mark(levelOk)}Lv{species.EvolveLevel}</color>");
            if (species.EvolveItem != null)
            {
                int owned = session.Inventory.GetCount(species.EvolveItem);
                bool itemsOk = owned >= species.EvolveItemCount;
                text.Append($" · {Mark(itemsOk)}{species.EvolveItem.DisplayName} {owned}/{species.EvolveItemCount}</color>");
            }
            return text.ToString();
        }

        private static string Mark(bool ok) => ok ? "<color=#5FD07A>" : "<color=#F07070>";

        private static string ButtonText(EvolutionStatus status)
        {
            switch (status)
            {
                case EvolutionStatus.Ready: return "진화!";
                case EvolutionStatus.LevelTooLow: return "레벨이 부족해요";
                case EvolutionStatus.NotEnoughItems: return "재료가 부족해요";
                default: return "최종 형태";
            }
        }

        private void OnEvolveClicked(int index)
        {
            if (index >= session.Party.Count) return;
            var monster = session.Party[index];

            if (confirmIndex != index)
            {
                confirmIndex = index;
                resultText.text = $"{UiKit.WithJosa(monster.DisplayName, "을", "를")} 진화시킬까요?\n진화는 되돌릴 수 없어요";
                Refresh();
                return;
            }

            confirmIndex = -1;
            var fromSpecies = monster.Species;
            var before = monster.Stats;
            if (Evolution.TryEvolve(monster, session.Inventory))
            {
                var after = monster.Stats;
                var text = new StringBuilder();
                text.AppendLine($"★ {UiKit.WithJosa(fromSpecies.DisplayName, "이", "가")} {UiKit.WithJosa(monster.DisplayName, "으로", "로")} 진화했다!");
                text.Append($"HP {before.MaxHp}→{after.MaxHp}   공격 {before.Attack}→{after.Attack}   방어 {before.Defense}→{after.Defense}");
                var newSkills = Evolution.NewSkills(fromSpecies, monster.Species);
                if (newSkills.Count > 0)
                {
                    var names = new List<string>();
                    foreach (var skill in newSkills) names.Add(skill.DisplayName);
                    text.Append($"\n새 기술: {string.Join(", ", names)}");
                }
                resultText.text = text.ToString();
                onChanged?.Invoke();
                // 진화 연출 (Figma '진화 연출 1·2·3'). 끝나면 제단 화면으로 돌아온다
                var lostSkills = Evolution.NewSkills(monster.Species, fromSpecies);
                Cutscene.Play(fromSpecies, monster.Species, monster.Level, before, after, newSkills, lostSkills, Refresh);
            }
            Refresh();
        }
    }
}
