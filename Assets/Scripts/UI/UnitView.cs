using System;
using UnityEngine;
using UnityEngine.UI;
using WordRPG.Battle;

namespace WordRPG.UI
{
    // 전투 화면의 몬스터 카드 1장 (아군/적 공용) — Figma 'Monster Card'.
    // 둥근 카드 + 그림 칸(몬스터 그림이 있으면 그림, 없으면 이름 첫 글자) + 이름 + HP 바 + 보호막 태그
    public class UnitView
    {
        public RectTransform Root { get; private set; }
        public BattleUnit Unit { get; private set; }
        public Button Button { get; private set; }

        private Image frame;
        private Image art;
        private Image artSprite;
        private Text initial;
        private Text title;
        private RectTransform hpFill;
        private Image hpFillImage;
        private Text hpText;
        private GameObject shieldTag;
        private Text shieldText;
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

            var card = UiKit.RoundPanel("Card", view.Root, Palette.Panel, UiKit.RadiusLg);
            card.raycastTarget = false;

            view.art = UiKit.RoundPanel("Art", card.transform, Palette.PanelLight, UiKit.RadiusMd, 0.06f, 0.42f, 0.94f, 0.95f);
            view.art.raycastTarget = false;
            view.initial = UiKit.Display(UiKit.Label("Initial", view.art.transform, "", 96, Palette.Text, 0, 0, 1, 1,
                TextAnchor.MiddleCenter, FontStyle.Normal, true, 30));
            view.artSprite = UiKit.IconImage("Sprite", view.art.transform, null, 0.04f, 0.04f, 0.96f, 0.96f);

            view.title = UiKit.Display(UiKit.Label("Title", card.transform, "", 40, Palette.Text, 0.04f, 0.26f, 0.96f, 0.41f,
                TextAnchor.MiddleCenter, FontStyle.Normal, true, 18));

            var hpBack = UiKit.Pill(UiKit.Panel("HpBack", card.transform, Palette.Track, 0.08f, 0.185f, 0.92f, 0.24f));
            hpBack.raycastTarget = false;
            view.hpFillImage = UiKit.Pill(UiKit.Panel("HpFill", hpBack.transform, Palette.Good));
            view.hpFillImage.raycastTarget = false;
            view.hpFill = view.hpFillImage.rectTransform;

            view.hpText = UiKit.Label("HpText", card.transform, "", 26, Palette.TextDim, 0, 0.03f, 1, 0.17f,
                TextAnchor.MiddleCenter, FontStyle.Normal, true, 16);

            // 보호막 태그 (그림 칸 오른쪽 위)
            var tag = UiKit.Pill(UiKit.Panel("ShieldTag", card.transform, Palette.Guard, 0.56f, 0.82f, 0.95f, 0.93f));
            tag.raycastTarget = false;
            UiKit.IconImage("Icon", tag.transform, UiKit.Icon("skill_guard"), 0.08f, 0.15f, 0.38f, 0.85f);
            view.shieldText = UiKit.Display(UiKit.Label("Value", tag.transform, "", 30, Palette.Text, 0.36f, 0, 0.94f, 1,
                TextAnchor.MiddleCenter, FontStyle.Normal, true, 14));
            view.shieldTag = tag.gameObject;
            view.shieldTag.SetActive(false);

            // 차례(금색)·대상(파랑) 표시 테두리
            view.frame = UiKit.Outline(UiKit.Panel("Frame", view.Root, Color.clear), UiKit.RadiusLg, 6);
            view.frame.raycastTarget = false;
            return view;
        }

        public void Bind(BattleUnit unit)
        {
            Unit = unit;
            var species = unit.Monster.Species;
            baseColor = Color.Lerp(Palette.PanelLight, species.PlaceholderColor, 0.65f);
            string name = species.DisplayName;
            initial.text = name.Length > 0 ? name.Substring(0, 1) : "?";
            title.text = $"{name}  Lv{unit.Monster.Level}";

            // 몬스터 그림이 지정돼 있으면 첫 글자 대신 그림
            artSprite.sprite = species.Sprite;
            artSprite.enabled = species.Sprite != null;
            initial.enabled = species.Sprite == null;

            SetFrame(null);
            Sync(unit.Hp, unit.Shield);
        }

        // 화면에 보이는 HP/보호막을 갱신. 전투 로직은 한 번에 끝나지만 연출은 사건 순서대로 보여주기 위해 값을 따로 받는다
        public void Sync(int hp, int shield)
        {
            int maxHp = Math.Max(1, Unit.MaxHp);
            float ratio = Mathf.Clamp01((float)hp / maxHp);
            hpFill.anchorMax = new Vector2(ratio, 1);
            hpFillImage.enabled = ratio > 0f;
            hpFillImage.color = ratio > 0.5f ? Palette.Good : ratio > 0.25f ? Palette.Gold : Palette.Bad;
            hpText.text = $"HP {hp}/{maxHp}";
            shieldTag.SetActive(shield > 0);
            shieldText.text = shield.ToString();

            bool fainted = hp <= 0;
            art.color = fainted ? new Color(0.22f, 0.22f, 0.26f) : baseColor;
            initial.color = fainted ? new Color(1, 1, 1, 0.25f) : new Color(1, 1, 1, 0.92f);
            artSprite.color = fainted ? new Color(0.35f, 0.35f, 0.35f, 0.6f) : Color.white;
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
