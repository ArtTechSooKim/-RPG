using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WordRPG.Game;
using WordRPG.Heroes;
using WordRPG.Items;
using WordRPG.Monsters;

namespace WordRPG.UI
{
    // 기술 배우기 (Figma '기술 배우기 — 빈 칸' / '가득 차서 바꾸기'): 기술문서로 배운 기술이나 성유물 기술을 기술 칸(3칸)에 넣는다.
    // 빈 칸이 있으면 [배우기]/[나중에], 가득이면 바꿀 기술을 하나 골라 [바꾸기]/[배우지 않기]. 기본 기술은 항상 남는다
    public class SkillLearnView
    {
        private const float PanelWidth = 1000f;
        private const float RowHeight = 190f;

        public GameObject Root { get; private set; }
        public bool IsOpen => Root != null && Root.activeSelf;

        private RectTransform panel;
        private Text title, from, status, note;
        private SkillRowView newRow;
        private RectTransform newRowArea;
        private readonly List<(RectTransform area, SkillRowView row, Image border)> replaceRows =
            new List<(RectTransform, SkillRowView, Image)>();
        private Button confirm, cancel;
        private Text confirmLabel, cancelLabel;

        private GameSession session;
        private SkillData offered;
        private Func<int, bool> learn; // 칸 번호(-1 = 빈 칸) → 배웠는지
        private Action<bool> done;
        private int selected = -1;

        public static SkillLearnView Create(Transform parent)
        {
            var view = new SkillLearnView();
            var root = UiKit.Stretch("SkillLearnView", parent);
            view.Root = root.gameObject;
            view.Root.AddComponent<Image>().color = Palette.Scrim; // 뒤 화면 누르기 막기

            var panelImage = UiKit.RoundPanel("LearnPanel", root, Palette.Panel, UiKit.RadiusLg, 0.5f, 0.5f, 0.5f, 0.5f);
            view.panel = panelImage.rectTransform;
            var border = UiKit.Outline(UiKit.Panel("Border", view.panel, Palette.Gold), UiKit.RadiusLg, 4);
            border.raycastTarget = false;

            view.title = UiKit.Display(UiKit.OneLine(UiKit.Label("Title", view.panel, "", 52, Palette.Gold, 0, 1, 1, 1)));
            view.from = UiKit.OneLine(UiKit.Label("From", view.panel, "", 30, Palette.TextDim, 0, 1, 1, 1));
            view.newRowArea = UiKit.Rect("NewSkillArea", view.panel, 0, 1, 1, 1);
            view.newRow = SkillRowView.Create(view.newRowArea, "NewSkill");
            view.status = UiKit.OneLine(UiKit.Label("Status", view.panel, "", 30, Palette.Text, 0, 1, 1, 1,
                TextAnchor.MiddleCenter, FontStyle.Bold));
            for (int i = 0; i < Hero.SkillSlotCount; i++)
            {
                int index = i;
                var area = UiKit.Rect($"Replace_{i}", view.panel, 0, 1, 1, 1);
                var row = SkillRowView.Create(area, "Row");
                var hit = UiKit.Panel("Hit", area, Color.clear);
                UiKit.AddButton(hit).onClick.AddListener(() => view.SelectReplace(index));
                var rowBorder = UiKit.Outline(UiKit.Panel("Border", area, Palette.Gold), UiKit.RadiusMd, 5);
                rowBorder.raycastTarget = false;
                view.replaceRows.Add((area, row, rowBorder));
            }
            view.confirm = UiKit.MakeButton("LearnConfirmButton", view.panel, "", Palette.Gold, 44, 0, 1, 0.5f, 1);
            view.confirmLabel = UiKit.LabelOf(view.confirm);
            view.confirm.onClick.AddListener(view.OnConfirm);
            view.cancel = UiKit.MakeButton("LearnCancelButton", view.panel, "", Palette.Neutral, 44, 0.5f, 1, 1, 1);
            view.cancelLabel = UiKit.LabelOf(view.cancel);
            view.cancel.onClick.AddListener(() => view.Close(false));
            view.note = UiKit.Label("Note", view.panel, "", 24, Palette.TextDim, 0, 1, 1, 1,
                TextAnchor.MiddleCenter, FontStyle.Normal, true, 16);
            view.Root.SetActive(false);
            return view;
        }

        // 기술문서로 배우기. done(배웠는지)
        public void ShowDocument(GameSession gameSession, ItemData document, Action<bool> onDone)
        {
            Open(gameSession, document.TaughtSkill, "새 기술을 배울 수 있어요!", $"{document.DisplayName}",
                replace => gameSession.LearnSkill(document, replace),
                "기술문서는 가방에 남아 언제든 다시 배울 수 있어요", onDone);
        }

        // 끼운 성유물의 기술을 기술 칸에 다시 넣기
        public void ShowRelicSkill(GameSession gameSession, OwnedRelic relic, Action<bool> onDone)
        {
            Open(gameSession, relic.Skill, "기술 칸에 넣기", $"{relic.Data.DisplayName} +{relic.Level}의 기술",
                replace => gameSession.Hero.PlaceRelicSkill(relic, replace),
                "뺀 기술은 성유물·기술문서로 언제든 다시 넣을 수 있어요", onDone);
        }

        private void Open(GameSession gameSession, SkillData skill, string titleText, string fromText, Func<int, bool> learnAction,
            string noteText, Action<bool> onDone)
        {
            session = gameSession;
            offered = skill;
            learn = learnAction;
            done = onDone;
            selected = -1;
            title.text = titleText;
            from.text = fromText;
            note.text = noteText;
            newRow.Show(skill, "", skill.Description);
            Root.transform.SetAsLastSibling();
            Root.SetActive(true);
            Layout();
        }

        // 위에서부터 차례로 쌓는다 (빈 칸이면 바꿀 기술 목록 없이 짧게)
        private void Layout()
        {
            var hero = session.Hero;
            bool full = !hero.HasSkillRoom;
            float y = 40f;
            Place(title.rectTransform, ref y, 70f, 0f);
            Place(from.rectTransform, ref y, 44f, 12f);
            Place(newRowArea, ref y, RowHeight, 16f);
            status.text = full
                ? "기술 칸이 가득 찼어요 — 바꿀 기술을 고르세요"
                : $"기술 칸 {hero.SkillSlots.Count} / {Hero.SkillSlotCount} — 빈 칸에 넣어요 (기본 기술 '{hero.Data.BasicSkill?.DisplayName}'은 항상 남아요)";
            status.color = full ? Palette.Bad : Palette.Text;
            Place(status.rectTransform, ref y, 50f, 16f);

            for (int i = 0; i < replaceRows.Count; i++)
            {
                var (area, row, border) = replaceRows[i];
                bool show = full && i < hero.SkillSlots.Count;
                area.gameObject.SetActive(show);
                if (!show) continue;
                var slot = hero.SkillSlots[i];
                string source = slot.Relic != null ? $"{slot.SourceName} +{slot.Relic.Level} · " : "기술문서 · ";
                row.Show(slot.Skill, source, slot.Skill.Description);
                border.enabled = i == selected;
                Place(area, ref y, RowHeight, 12f);
            }

            float buttonsY = y + 4f;
            Place((RectTransform)confirm.transform, ref buttonsY, 120f, 0f);
            Place((RectTransform)cancel.transform, ref y, 120f, 12f);
            SetHalf((RectTransform)confirm.transform, 0f, 0.5f);
            SetHalf((RectTransform)cancel.transform, 0.5f, 1f);
            Place(note.rectTransform, ref y, 70f, 30f);

            if (full)
            {
                bool ready = selected >= 0;
                confirmLabel.text = ready ? $"{hero.SkillSlots[selected].Skill.DisplayName} → {offered.DisplayName}" : "바꿀 기술을 고르세요";
                confirm.interactable = ready;
                cancelLabel.text = "배우지 않기";
            }
            else
            {
                confirmLabel.text = "배우기";
                confirm.interactable = true;
                cancelLabel.text = "나중에";
            }
            UiKit.SetColor(confirm, confirm.interactable ? Palette.Gold : Palette.Disabled);
            confirmLabel.color = confirm.interactable ? Palette.OnAccent : Palette.TextDim;
            panel.sizeDelta = new Vector2(PanelWidth, y);
        }

        // 위에서 y만큼 내려와 높이 h (좌우 40 여백), 다음 y는 h + gap 아래
        private static void Place(RectTransform rect, ref float y, float h, float gap)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.offsetMin = new Vector2(40, -(y + h));
            rect.offsetMax = new Vector2(-40, -y);
            y += h + gap;
        }

        private static void SetHalf(RectTransform rect, float from, float to)
        {
            rect.anchorMin = new Vector2(from, rect.anchorMin.y);
            rect.anchorMax = new Vector2(to, rect.anchorMax.y);
            rect.offsetMin = new Vector2(from > 0 ? 12 : 40, rect.offsetMin.y);
            rect.offsetMax = new Vector2(to < 1 ? -12 : -40, rect.offsetMax.y);
        }

        private void SelectReplace(int index)
        {
            selected = index;
            Layout();
        }

        private void OnConfirm()
        {
            bool full = !session.Hero.HasSkillRoom;
            if (full && selected < 0) return;
            bool learned = learn(full ? selected : -1);
            if (learned) Sound.Play(Sfx.LevelUp);
            Close(learned);
        }

        // 뒤로가기: 배우지 않고 닫기 ([나중에]·[배우지 않기]와 같음)
        public void Cancel()
        {
            if (IsOpen) Close(false);
        }

        private void Close(bool learned)
        {
            Root.SetActive(false);
            var callback = done;
            done = null;
            callback?.Invoke(learned);
        }
    }
}
