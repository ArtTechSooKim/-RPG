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
            public Image Swatch;
            public Text Initial;
            public Text Info;
            public Button Button;
            public Text ButtonLabel;
        }

        private static readonly Color ReadyColor = new Color(0.75f, 0.45f, 0.1f);
        private static readonly Color ConfirmColor = new Color(0.85f, 0.25f, 0.25f);
        private static readonly Color LockedColor = new Color(0.3f, 0.32f, 0.4f);

        public GameObject Root { get; private set; }
        public bool IsOpen => Root != null && Root.activeSelf;

        private readonly List<Card> cards = new List<Card>();
        private Text resultText;
        private GameSession session;
        private Action onChanged;
        private int confirmIndex = -1;

        public static EvolutionView Create(Transform parent)
        {
            var view = new EvolutionView();
            var root = UiKit.Stretch("EvolutionView", parent);
            view.Root = root.gameObject;
            view.Root.AddComponent<Image>().color = Palette.Background;

            UiKit.Label("Title", root, "진화의 제단", 50, Palette.Gold, 0, 0.92f, 1, 0.99f, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Label("Subtitle", root, "레벨과 재료가 모이면 진화할 수 있어요", 30, Palette.TextDim, 0, 0.885f, 1, 0.925f);

            for (int i = 0; i < GameSession.MaxPartySize; i++)
            {
                float top = 0.87f - i * 0.185f;
                var panel = UiKit.Panel($"EvolveCard_{i}", root, Palette.Panel, 0.03f, top - 0.17f, 0.97f, top);
                var card = new Card { Root = panel.gameObject };
                card.Swatch = UiKit.Panel("Swatch", panel.transform, Color.gray, 0.025f, 0.12f, 0.2f, 0.88f);
                card.Initial = UiKit.Label("Initial", card.Swatch.transform, "", 70, Color.white, 0, 0, 1, 1,
                    TextAnchor.MiddleCenter, FontStyle.Bold, true, 24);
                card.Info = UiKit.Label("Info", panel.transform, "", 32, Palette.Text, 0.23f, 0.42f, 0.98f, 0.97f,
                    TextAnchor.MiddleLeft, FontStyle.Normal, true, 18);
                card.Button = UiKit.MakeButton($"EvolveButton_{i}", panel.transform, "", LockedColor, 34,
                    0.23f, 0.08f, 0.97f, 0.38f, bestFit: true);
                card.ButtonLabel = UiKit.LabelOf(card.Button);
                int index = i;
                card.Button.onClick.AddListener(() => view.OnEvolveClicked(index));
                view.cards.Add(card);
            }

            var resultBox = UiKit.Panel("ResultBox", root, Palette.PanelLight, 0.03f, 0.115f, 0.97f, 0.31f);
            view.resultText = UiKit.Label("Result", resultBox.transform, "", 34, Palette.Text, 0, 0, 1, 1,
                TextAnchor.MiddleCenter, FontStyle.Normal, true, 20);
            UiKit.Pad(view.resultText.rectTransform, 20, 8, 20, 8);

            var close = UiKit.MakeButton("EvolutionCloseButton", root, "닫기", new Color(0.35f, 0.35f, 0.4f), 44,
                0.25f, 0.015f, 0.75f, 0.095f);
            close.onClick.AddListener(view.Hide);

            view.Root.SetActive(false);
            return view;
        }

        public void Show(GameSession gameSession, Action changed)
        {
            session = gameSession;
            onChanged = changed;
            confirmIndex = -1;
            resultText.text = "진화시킬 몬스터를 고르세요";
            Root.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            confirmIndex = -1;
            Root.SetActive(false);
        }

        private void Refresh()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                bool has = i < session.Party.Count;
                card.Root.SetActive(has);
                if (!has) continue;

                var monster = session.Party[i];
                var species = monster.Species;
                card.Swatch.color = species.PlaceholderColor;
                card.Initial.text = species.DisplayName.Length > 0 ? species.DisplayName.Substring(0, 1) : "?";
                card.Info.text = $"<b>{species.DisplayName}</b>  Lv{monster.Level}  {species.Role.DisplayName()}\n{Requirement(monster)}";

                var status = Evolution.Check(monster, session.Inventory);
                bool confirming = confirmIndex == i && status == EvolutionStatus.Ready;
                card.Button.interactable = status == EvolutionStatus.Ready;
                UiKit.SetColor(card.Button, confirming ? ConfirmColor : status == EvolutionStatus.Ready ? ReadyColor : LockedColor);
                card.ButtonLabel.text = confirming ? "한 번 더 누르면 진화!" : ButtonText(status);
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
            }
            Refresh();
        }
    }
}
