using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WordRPG.Game;
using WordRPG.Monsters;

namespace WordRPG.UI
{
    // 소지품 > 몬스터 탭 (Figma '소지품 — 몬스터'): 파티 카드(Monster Tab) 3장 + 고른 몬스터의
    // 능력치(Stat Tile) · 기술(Skill Info Row) · 진화 조건. 여행 중에 몬스터를 살펴보는 곳
    public class MonsterInfoPage
    {
        private const int MaxSkillRows = 3;

        private class Card
        {
            public GameObject Root;
            public Image Panel, Border, Avatar, Sprite, HpFill;
            public Text Initial, Name, Sub;
        }

        private class SkillRow
        {
            public GameObject Root;
            public Image IconBack, Icon, QuizBack;
            public Text Name, Quiz, Effect, Description;
        }

        public GameObject Root { get; private set; }
        public int Selected { get; private set; }

        private readonly List<Card> cards = new List<Card>();
        private readonly List<SkillRow> skillRows = new List<SkillRow>();
        private Image art, artSprite, roleIcon, expFill;
        private Text artInitial, nameText, roleText, nextText, description, evolutionLine1, evolutionLine2;
        private readonly Text[] statValues = new Text[3];
        private GameSession session;

        public static MonsterInfoPage Create(RectTransform parent, float minX, float minY, float maxX, float maxY)
        {
            var page = new MonsterInfoPage();
            var root = UiKit.Rect("MonstersPage", parent, minX, minY, maxX, maxY);
            page.Root = root.gameObject;
            page.BuildCards(UiKit.Rect("Party", root, 0, 0.8f, 1, 1));
            page.BuildDetail(UiKit.RoundPanel("MonsterDetail", root, Palette.PanelLight, UiKit.RadiusLg, 0, 0, 1, 0.782f));
            return page;
        }

        // ------------------------------------------------------------------ 파티 카드

        private void BuildCards(RectTransform row)
        {
            const float gap = 24f / 1032f;
            float w = (1f - 2 * gap) / GameSession.MaxPartySize;
            for (int i = 0; i < GameSession.MaxPartySize; i++)
            {
                float x = i * (w + gap);
                var panel = UiKit.RoundPanel($"MonsterTab_{i}", row, Palette.Panel, UiKit.RadiusLg, x, 0, x + w, 1);
                int index = i;
                UiKit.AddButton(panel).onClick.AddListener(() => Select(index));
                var card = new Card { Root = panel.gameObject, Panel = panel };
                card.Avatar = UiKit.RoundPanel("Avatar", panel.transform, Palette.PanelLight, UiKit.RadiusMd, 0.5f, 1f, 0.5f, 1f);
                card.Avatar.raycastTarget = false;
                card.Avatar.rectTransform.pivot = new Vector2(0.5f, 1f);
                card.Avatar.rectTransform.sizeDelta = new Vector2(96, 96);
                card.Avatar.rectTransform.anchoredPosition = new Vector2(0, -20);
                card.Initial = UiKit.Display(UiKit.Label("Initial", card.Avatar.transform, "", 56, Palette.Text, 0, 0, 1, 1));
                card.Sprite = UiKit.IconImage("Sprite", card.Avatar.transform, null, 0.08f, 0.08f, 0.92f, 0.92f);
                card.Name = UiKit.Display(UiKit.Label("Name", panel.transform, "", 44, Palette.Text, 0.05f, 0.33f, 0.95f, 0.52f,
                    TextAnchor.MiddleCenter, FontStyle.Normal, true, 26));
                card.Sub = OneLine(UiKit.Label("Sub", panel.transform, "", 24, Palette.TextDim, 0.05f, 0.2f, 0.95f, 0.33f,
                    TextAnchor.MiddleCenter, FontStyle.Normal, true, 18));
                var back = UiKit.Pill(UiKit.Panel("HpBack", panel.transform, Palette.Track, 0.07f, 0.08f, 0.93f, 0.16f));
                back.raycastTarget = false;
                card.HpFill = UiKit.Pill(UiKit.Panel("HpFill", back.transform, Palette.Good));
                card.HpFill.raycastTarget = false;
                card.Border = UiKit.Outline(UiKit.Panel("Border", panel.transform, Palette.Gold), UiKit.RadiusLg, 6);
                card.Border.raycastTarget = false;
                cards.Add(card);
            }
        }

        // ------------------------------------------------------------------ 상세

        private void BuildDetail(Image panel)
        {
            panel.raycastTarget = false;
            var box = panel.rectTransform;

            // 머리: 그림 · 이름 · 역할·레벨 · 경험치
            art = UiKit.RoundPanel("Art", box, Palette.PanelLight, UiKit.RadiusLg, 0.027f, 0.781f, 0.221f, 0.973f);
            art.raycastTarget = false;
            artInitial = UiKit.Display(UiKit.Label("Initial", art.transform, "", 110, Palette.Text, 0, 0, 1, 1));
            artSprite = UiKit.IconImage("Sprite", art.transform, null, 0.08f, 0.08f, 0.92f, 0.92f);
            nameText = UiKit.Display(UiKit.Label("Name", box, "", 56, Palette.Text, 0.252f, 0.895f, 0.973f, 0.973f,
                TextAnchor.MiddleLeft, FontStyle.Normal, true, 30));
            roleIcon = UiKit.IconImage("Role", box, null, 0.252f, 0.852f, 0.291f, 0.89f);
            roleText = OneLine(UiKit.Label("Role", box, "", 30, Palette.TextDim, 0.3f, 0.85f, 0.973f, 0.892f, TextAnchor.MiddleLeft));
            OneLine(UiKit.Label("ExpLabel", box, "경험치", 24, Palette.TextDim, 0.252f, 0.812f, 0.36f, 0.846f, TextAnchor.MiddleLeft));
            var track = UiKit.Pill(UiKit.Panel("ExpBack", box, Palette.Track, 0.37f, 0.821f, 0.973f, 0.837f));
            track.raycastTarget = false;
            expFill = UiKit.Pill(UiKit.Panel("ExpFill", track.transform, Palette.Gold));
            expFill.raycastTarget = false;
            nextText = OneLine(UiKit.Label("Next", box, "", 24, Palette.TextDim, 0.252f, 0.778f, 0.973f, 0.81f, TextAnchor.MiddleLeft));

            description = UiKit.Label("Description", box, "", 32, Palette.TextDim, 0.027f, 0.7f, 0.973f, 0.768f,
                TextAnchor.MiddleLeft, FontStyle.Normal, true, 22);

            // 능력치 3칸 (Figma 'Stat Tile')
            string[] labels = { "HP", "공격", "방어" };
            const float gap = 24f / 1032f;
            float w = (0.946f - 2 * gap) / 3f;
            for (int i = 0; i < 3; i++)
            {
                float x = 0.027f + i * (w + gap);
                var tile = UiKit.RoundPanel($"Stat_{labels[i]}", box, Palette.Panel, UiKit.RadiusMd, x, 0.578f, x + w, 0.69f);
                tile.raycastTarget = false;
                OneLine(UiKit.Label("Label", tile.transform, labels[i], 28, Palette.TextDim, 0, 0.6f, 1, 0.94f));
                statValues[i] = UiKit.Display(UiKit.Label("Value", tile.transform, "", 56, Palette.Text, 0, 0.04f, 1, 0.66f,
                    TextAnchor.MiddleCenter, FontStyle.Normal, true, 30));
            }

            // 기술 (Figma 'Skill Info Row' + 'Quiz Tag')
            UiKit.Display(UiKit.Label("SkillsHeader", box, "기술", 44, Palette.Gold, 0.027f, 0.522f, 0.5f, 0.568f, TextAnchor.MiddleLeft));
            var skillArea = UiKit.Rect("Skills", box, 0.027f, 0.19f, 0.973f, 0.515f);
            for (int i = 0; i < MaxSkillRows; i++) skillRows.Add(BuildSkillRow(skillArea, i));

            // 진화 조건
            var evolution = UiKit.RoundPanel("Evolution", box, Palette.Panel, UiKit.RadiusMd, 0.027f, 0.027f, 0.973f, 0.172f);
            evolution.raycastTarget = false;
            var star = UiKit.IconImage("Icon", evolution.transform, UiKit.Icon("star_full"), 0f, 0.5f, 0f, 0.5f);
            star.rectTransform.pivot = new Vector2(0f, 0.5f);
            star.rectTransform.sizeDelta = new Vector2(44, 44);
            star.rectTransform.anchoredPosition = new Vector2(24, 0);
            evolutionLine1 = UiKit.Label("Line1", evolution.transform, "", 30, Palette.Text, 0.085f, 0.5f, 0.98f, 0.9f,
                TextAnchor.MiddleLeft, FontStyle.Normal, true, 20);
            evolutionLine2 = UiKit.Label("Line2", evolution.transform, "", 30, Palette.TextDim, 0.085f, 0.1f, 0.98f, 0.5f,
                TextAnchor.MiddleLeft, FontStyle.Normal, true, 20);
        }

        private static SkillRow BuildSkillRow(RectTransform parent, int index)
        {
            var panel = UiKit.RoundPanel($"SkillRow_{index}", parent, Palette.Panel, UiKit.RadiusMd);
            panel.raycastTarget = false;
            var row = new SkillRow { Root = panel.gameObject };
            row.IconBack = UiKit.RoundPanel("IconBack", panel.transform, Palette.Attack, UiKit.RadiusMd, 0f, 0.5f, 0f, 0.5f);
            row.IconBack.raycastTarget = false;
            row.IconBack.rectTransform.pivot = new Vector2(0f, 0.5f);
            row.IconBack.rectTransform.sizeDelta = new Vector2(96, 96);
            row.IconBack.rectTransform.anchoredPosition = new Vector2(24, 0);
            row.Icon = UiKit.IconImage("Icon", row.IconBack.transform, null, 0.17f, 0.17f, 0.83f, 0.83f);

            // 이름 + 문제 유형 태그를 한 줄로 (이름 길이에 따라 태그가 붙어 따라온다)
            var head = UiKit.Rect("Head", panel.transform, 0, 0.6f, 1, 0.94f);
            head.offsetMin = new Vector2(144, 0);
            head.offsetMax = new Vector2(-24, 0);
            var layout = head.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 16;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            row.Name = UiKit.Display(UiKit.Label("Name", head, "", 40, Palette.Text, 0, 0, 1, 1, TextAnchor.MiddleLeft));
            row.Name.horizontalOverflow = HorizontalWrapMode.Overflow;
            row.QuizBack = UiKit.Pill(UiKit.Panel("QuizTag", head, Palette.Guard));
            row.QuizBack.raycastTarget = false;
            var tagLayout = row.QuizBack.gameObject.AddComponent<HorizontalLayoutGroup>();
            tagLayout.padding = new RectOffset(16, 16, 4, 4);
            tagLayout.childControlWidth = tagLayout.childControlHeight = true;
            tagLayout.childForceExpandWidth = tagLayout.childForceExpandHeight = false;
            row.Quiz = OneLine(UiKit.Label("Text", row.QuizBack.transform, "", 24, Palette.Text, 0, 0, 1, 1));
            row.Quiz.horizontalOverflow = HorizontalWrapMode.Overflow;

            row.Effect = UiKit.Label("Effect", panel.transform, "", 28, Palette.Gold, 0, 0.34f, 1, 0.6f,
                TextAnchor.MiddleLeft, FontStyle.Normal, true, 18);
            row.Description = UiKit.Label("Description", panel.transform, "", 28, Palette.TextDim, 0, 0.06f, 1, 0.34f,
                TextAnchor.MiddleLeft, FontStyle.Normal, true, 18);
            foreach (var label in new[] { row.Effect, row.Description })
            {
                label.rectTransform.offsetMin = new Vector2(144, 0);
                label.rectTransform.offsetMax = new Vector2(-24, 0);
            }
            return row;
        }

        // ------------------------------------------------------------------ 표시

        public void Show(GameSession gameSession, int selected)
        {
            session = gameSession;
            Root.SetActive(true);
            Select(selected);
        }

        public void Hide() => Root.SetActive(false);

        private void Select(int index)
        {
            var party = session.Party;
            Selected = party.Count == 0 ? 0 : Mathf.Clamp(index, 0, party.Count - 1);
            for (int i = 0; i < cards.Count; i++) ShowCard(cards[i], i < party.Count ? party[i] : null, i == Selected);
            if (party.Count > 0) ShowDetail(party[Selected]);
        }

        private static void ShowCard(Card card, MonsterInstance monster, bool selected)
        {
            card.Root.SetActive(monster != null);
            if (monster == null) return;
            card.Panel.color = selected ? Palette.PanelLight : Palette.Panel;
            card.Border.enabled = selected;
            SetArt(card.Avatar, card.Sprite, card.Initial, monster.Species);
            card.Name.text = monster.DisplayName;
            card.Sub.text = $"Lv{monster.Level} · HP {monster.CurrentHp}/{monster.Stats.MaxHp}";
            float ratio = Mathf.Clamp01((float)monster.CurrentHp / Mathf.Max(1, monster.Stats.MaxHp));
            card.HpFill.enabled = ratio > 0f;
            card.HpFill.rectTransform.anchorMax = new Vector2(ratio, 1);
            card.HpFill.color = ratio > 0.5f ? Palette.Good : ratio > 0.25f ? Palette.Gold : Palette.Bad;
        }

        private void ShowDetail(MonsterInstance monster)
        {
            var species = monster.Species;
            SetArt(art, artSprite, artInitial, species);
            nameText.text = monster.DisplayName;
            var role = UiKit.Icon(InventoryView.RoleIconName(species.Role));
            roleIcon.sprite = role;
            roleIcon.enabled = role != null;
            roleText.text = $"{species.Role.DisplayName()} · Lv{monster.Level}";

            if (monster.IsMaxLevel)
            {
                expFill.enabled = true;
                expFill.rectTransform.anchorMax = Vector2.one;
                nextText.text = "최고 레벨이에요";
            }
            else
            {
                float ratio = Mathf.Clamp01((float)monster.Exp / LevelCurve.ExpToNextLevel(monster.Level));
                expFill.enabled = ratio > 0f;
                expFill.rectTransform.anchorMax = new Vector2(ratio, 1);
                nextText.text = $"다음 레벨까지 {monster.ExpToNextLevel}";
            }
            description.text = species.Description;

            var stats = monster.Stats;
            statValues[0].text = $"{monster.CurrentHp} / {stats.MaxHp}";
            statValues[1].text = stats.Attack.ToString();
            statValues[2].text = stats.Defense.ToString();

            ShowSkills(species.Skills);
            ShowEvolution(monster);
        }

        // 기술 줄은 개수에 맞춰 영역을 나눈다 (2개면 반씩, 3개면 셋으로)
        private void ShowSkills(IReadOnlyList<SkillData> skills)
        {
            int count = Mathf.Min(skills.Count, MaxSkillRows);
            const float gap = 16f / 340f;
            float h = count > 0 ? (1f - (count - 1) * gap) / count : 1f;
            for (int i = 0; i < skillRows.Count; i++)
            {
                var row = skillRows[i];
                bool has = i < count;
                row.Root.SetActive(has);
                if (!has) continue;
                var rt = (RectTransform)row.Root.transform;
                float top = 1f - i * (h + gap);
                rt.anchorMin = new Vector2(0, top - h);
                rt.anchorMax = new Vector2(1, top);

                var skill = skills[i];
                row.IconBack.color = BattleScreen.SkillColor(skill);
                var icon = UiKit.Icon(BattleScreen.SkillIconName(skill));
                row.Icon.sprite = icon;
                row.Icon.enabled = icon != null;
                row.Name.text = skill.DisplayName;
                row.Quiz.text = BattleScreen.QuizText(skill);
                row.QuizBack.color = BattleScreen.IsHardQuiz(skill) ? Palette.Evolve : Palette.Guard;
                row.Effect.text = BattleScreen.SkillEffect(skill);
                row.Description.text = skill.Description;
            }
        }

        private void ShowEvolution(MonsterInstance monster)
        {
            var species = monster.Species;
            if (species.EvolvesTo == null)
            {
                evolutionLine1.text = "최종 모습이에요";
                evolutionLine2.text = "더 이상 진화하지 않아요";
                evolutionLine2.color = Palette.TextDim;
                return;
            }
            string item = species.EvolveItem != null ? $" · {species.EvolveItem.DisplayName} {species.EvolveItemCount}개" : "";
            evolutionLine1.text = $"진화  {species.EvolvesTo.DisplayName}  (진화의 제단 · Lv{species.EvolveLevel}{item})";
            var status = InventoryView.EvolutionStatusLine(monster, session.Inventory);
            evolutionLine2.text = status.Text;
            evolutionLine2.color = status.Color;
        }

        // 한 줄 글자: Noto Sans KR은 줄 높이가 커서 칸이 조금만 낮아도 Truncate면 글자가 통째로 사라진다 → 넘쳐도 그리기
        internal static Text OneLine(Text label)
        {
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private static void SetArt(Image back, Image sprite, Text initial, MonsterSpecies species)
        {
            back.color = UiKit.ArtColor(species);
            sprite.sprite = species.Sprite;
            sprite.enabled = species.Sprite != null;
            initial.enabled = species.Sprite == null;
            initial.text = species.DisplayName.Length > 0 ? species.DisplayName.Substring(0, 1) : "?";
        }
    }
}
