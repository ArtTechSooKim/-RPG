using System;
using UnityEngine;
using UnityEngine.UI;
using WordRPG.Battle;

namespace WordRPG.UI
{
    // 전투 화면의 몬스터 카드 1장 (아군/적 공용). 색 사각형 + 이름 + HP 바 (아트 전 플레이스홀더)
    public class UnitView
    {
        public RectTransform Root { get; private set; }
        public BattleUnit Unit { get; private set; }
        public Button Button { get; private set; }

        private Image frame;
        private Image body;
        private Text initial;
        private Text title;
        private RectTransform hpFill;
        private Image hpFillImage;
        private Text hpText;
        private Color baseColor;

        public static UnitView Create(Transform parent, string name, float minX, float minY, float maxX, float maxY)
        {
            var view = new UnitView();
            view.Root = UiKit.Rect(name, parent, minX, minY, maxX, maxY);
            UiKit.Pad(view.Root, 10);

            // 투명하지만 터치는 받는 바닥 (대상 선택용)
            var touch = view.Root.gameObject.AddComponent<Image>();
            touch.color = new Color(0, 0, 0, 0.01f);
            view.Button = view.Root.gameObject.AddComponent<Button>();
            view.Button.targetGraphic = touch;
            view.Button.transition = Selectable.Transition.None;
            view.Button.interactable = false;

            view.frame = UiKit.Panel("Frame", view.Root, Color.clear);
            UiKit.Pad(view.frame.rectTransform, -8);
            view.frame.raycastTarget = false;

            var card = UiKit.Panel("Card", view.Root, Palette.Panel);
            card.raycastTarget = false;

            view.body = UiKit.Panel("Body", view.Root, Palette.PanelLight, 0, 0.40f, 1, 1);
            view.body.raycastTarget = false;
            view.initial = UiKit.Label("Initial", view.body.transform, "", 90, new Color(1, 1, 1, 0.9f), 0, 0, 1, 1,
                TextAnchor.MiddleCenter, FontStyle.Bold, true, 30);

            view.title = UiKit.Label("Title", view.Root, "", 32, Palette.Text, 0, 0.26f, 1, 0.40f,
                TextAnchor.MiddleCenter, FontStyle.Bold, true, 18);

            var hpBack = UiKit.Panel("HpBack", view.Root, new Color(0.04f, 0.05f, 0.09f), 0.04f, 0.17f, 0.96f, 0.25f);
            hpBack.raycastTarget = false;
            view.hpFillImage = UiKit.Panel("HpFill", hpBack.transform, Palette.Good);
            view.hpFillImage.raycastTarget = false;
            view.hpFill = view.hpFillImage.rectTransform;

            view.hpText = UiKit.Label("HpText", view.Root, "", 28, Palette.Text, 0, 0, 1, 0.17f,
                TextAnchor.MiddleCenter, FontStyle.Normal, true, 16);
            return view;
        }

        public void Bind(BattleUnit unit)
        {
            Unit = unit;
            var species = unit.Monster.Species;
            baseColor = species.PlaceholderColor;
            string name = species.DisplayName;
            initial.text = name.Length > 0 ? name.Substring(0, 1) : "?";
            title.text = $"{name}  Lv{unit.Monster.Level}";
            SetFrame(null);
            Sync(unit.Hp, unit.Shield);
        }

        // 화면에 보이는 HP/보호막을 갱신. 전투 로직은 한 번에 끝나지만 연출은 사건 순서대로 보여주기 위해 값을 따로 받는다
        public void Sync(int hp, int shield)
        {
            int maxHp = Math.Max(1, Unit.MaxHp);
            float ratio = Mathf.Clamp01((float)hp / maxHp);
            hpFill.anchorMax = new Vector2(ratio, 1);
            hpFillImage.color = ratio > 0.5f ? Palette.Good : ratio > 0.25f ? Palette.Gold : Palette.Bad;
            hpText.text = shield > 0 ? $"HP {hp}/{maxHp}  보호막 {shield}" : $"HP {hp}/{maxHp}";

            bool fainted = hp <= 0;
            body.color = fainted ? new Color(0.25f, 0.25f, 0.28f) : baseColor;
            initial.color = fainted ? new Color(1, 1, 1, 0.25f) : new Color(1, 1, 1, 0.9f);
        }

        public void SetFrame(Color? color)
        {
            frame.color = color ?? Color.clear;
        }

        public void SetSelectable(bool selectable, Action onClick = null)
        {
            Button.onClick.RemoveAllListeners();
            if (onClick != null) Button.onClick.AddListener(() => onClick());
            Button.interactable = selectable;
        }
    }
}
