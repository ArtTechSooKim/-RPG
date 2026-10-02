using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WordRPG.Heroes;
using WordRPG.Monsters;

namespace WordRPG.UI
{
    // 성유물 각성 연출 (Figma '각성 연출 1·2·3'): ① 성유물에 담긴 단어들이 반짝이기 시작 ② 알파벳 고리가 금빛으로 물든 성유물 둘레를
    // 돌며 모여듦 ③ "각성 성공!" 보너스 변화 + 강해진 기술. 단어 게임다운 우리만의 연출 (다른 게임의 대사·하얀 실루엣은 쓰지 않는다).
    // 화면을 누르면 ③으로 건너뛴다. [좋아요!]를 누르면 닫히고 finished
    public class AwakeningCutscene : MonoBehaviour
    {
        private const float CardSize = 420f;
        private const float ResultCardSize = 360f;
        private const string RingLetters = "ABCDEFGHIJKL";

        public bool IsPlaying => gameObject.activeSelf;
        public bool ShowingResult { get; private set; }

        private float animationScale = 1f;
        private bool skip;
        private Action onFinished;

        private RectTransform card, glow, rays, letterRing;
        private readonly List<Text> letters = new List<Text>();
        private Image cardImage, cardSprite, cardBorder, glowImage, raysImage, flash;
        private Text cardInitial, message, title, headline, sub, skillTitle, skillName, skillDetail;
        private GameObject messageBox, resultGroup, skillBox;
        private Image skillIconBack, skillIcon, roleIcon;
        private readonly Text[] statOld = new Text[3], statNew = new Text[3], statDelta = new Text[3];

        private RelicData relic;

        public static AwakeningCutscene Create(Transform parent, float animScale)
        {
            var root = UiKit.Panel("AwakeningCutscene", parent, Palette.Background);
            var cutscene = root.gameObject.AddComponent<AwakeningCutscene>();
            cutscene.animationScale = animScale;
            cutscene.Build(root.rectTransform);
            root.gameObject.SetActive(false);
            return cutscene;
        }

        private void Build(RectTransform root)
        {
            // 화면 아무 데나 누르면 결과로 건너뛰기 (맨 뒤에 깔아 둔다)
            var skipArea = UiKit.AddButton(UiKit.Panel("AwakeningSkipArea", root, Color.clear), clickSound: false);
            skipArea.transition = Selectable.Transition.None;
            skipArea.onClick.AddListener(() => skip = true);

            glowImage = UiKit.IconImage("Glow", root, UiKit.GlowSprite(), 0.5f, 0.62f, 0.5f, 0.62f);
            glow = glowImage.rectTransform;
            glow.sizeDelta = new Vector2(900, 900);
            raysImage = UiKit.IconImage("Rays", root, UiKit.RaysSprite(), 0.5f, 0.62f, 0.5f, 0.62f);
            rays = raysImage.rectTransform;
            rays.sizeDelta = new Vector2(2000, 2000);

            cardImage = UiKit.RoundPanel("Card", root, Palette.PanelLight, UiKit.RadiusLg, 0.5f, 0.62f, 0.5f, 0.62f);
            cardImage.raycastTarget = false;
            card = cardImage.rectTransform;
            card.sizeDelta = new Vector2(CardSize, CardSize);
            cardInitial = UiKit.Display(UiKit.Label("Initial", card, "", 176, Palette.Text, 0, 0, 1, 1));
            cardSprite = UiKit.IconImage("Sprite", card, null, 0.12f, 0.12f, 0.88f, 0.88f);
            cardBorder = UiKit.Outline(UiKit.Panel("Border", card, Palette.Gold), UiKit.RadiusLg, 8);
            cardBorder.raycastTarget = false;

            // ② 몬스터 둘레를 도는 알파벳 고리 (자리는 Run에서 매 프레임 계산)
            letterRing = UiKit.Rect("Letters", root, 0.5f, 0.62f, 0.5f, 0.62f);
            for (int i = 0; i < RingLetters.Length; i++)
            {
                var letter = UiKit.Display(UiKit.Label($"Letter_{RingLetters[i]}", letterRing, RingLetters[i].ToString(),
                    i % 3 == 0 ? 76 : 60, Palette.Gold, 0.5f, 0.5f, 0.5f, 0.5f));
                letter.rectTransform.sizeDelta = new Vector2(100, 100);
                letter.horizontalOverflow = HorizontalWrapMode.Overflow;
                letters.Add(letter);
            }
            letterRing.gameObject.SetActive(false);

            var box = UiKit.RoundPanel("MessageBox", root, Palette.PanelLight, UiKit.RadiusLg, 0.067f, 0.115f, 0.933f, 0.19f);
            box.raycastTarget = false;
            messageBox = box.gameObject;
            message = UiKit.Label("Message", box.transform, "", 40, Palette.Text, 0.04f, 0, 0.96f, 1,
                TextAnchor.MiddleCenter, FontStyle.Bold, true, 24);

            BuildResult(root);

            flash = UiKit.Panel("Flash", root, Color.clear);
            flash.raycastTarget = false;
        }

        private void BuildResult(RectTransform root)
        {
            var group = UiKit.Stretch("Result", root);
            resultGroup = group.gameObject;
            title = UiKit.Display(UiKit.Label("Title", group, "각성 성공!", 64, Palette.Gold, 0, 0.9f, 1, 0.955f));
            headline = UiKit.Display(UiKit.Label("Headline", group, "", 46, Palette.Text, 0.03f, 0.635f, 0.97f, 0.675f,
                TextAnchor.MiddleCenter, FontStyle.Normal, true, 26));
            var subRow = UiKit.Rect("Sub", group, 0.03f, 0.605f, 0.97f, 0.633f);
            roleIcon = UiKit.IconImage("Role", subRow, null, 0.2f, 0.05f, 0.24f, 0.95f);
            sub = UiKit.Label("Text", subRow, "", 30, Palette.TextDim, 0.25f, 0, 1f, 1, TextAnchor.MiddleLeft);

            // 능력치 (HP · 공격 · 방어)
            var stats = UiKit.RoundPanel("Stats", group, Palette.Panel, UiKit.RadiusLg, 0.067f, 0.36f, 0.933f, 0.588f);
            stats.raycastTarget = false;
            UiKit.Label("Header", stats.transform, "성유물 보너스", 30, Palette.Gold, 0.045f, 0.82f, 0.5f, 0.96f, TextAnchor.MiddleLeft);
            string[] names = { "HP", "공격", "방어" };
            for (int i = 0; i < 3; i++)
            {
                float top = 0.8f - i * 0.26f, bottom = top - 0.22f;
                var row = UiKit.Rect($"Stat_{names[i]}", stats.transform, 0.045f, bottom, 0.955f, top);
                UiKit.Label("Name", row, names[i], 34, Palette.Text, 0, 0, 0.17f, 1, TextAnchor.MiddleLeft, FontStyle.Bold);
                statOld[i] = UiKit.Display(UiKit.Label("Old", row, "", 46, Palette.TextDim, 0.17f, 0, 0.3f, 1, TextAnchor.MiddleRight));
                UiKit.Label("Arrow", row, "→", 34, Palette.TextDim, 0.3f, 0, 0.38f, 1, TextAnchor.MiddleCenter, FontStyle.Bold);
                statNew[i] = UiKit.Display(UiKit.Label("New", row, "", 46, Palette.Good, 0.38f, 0, 0.55f, 1, TextAnchor.MiddleLeft));
                var chip = UiKit.Pill(UiKit.Panel("Delta", row, new Color(Palette.Good.r, Palette.Good.g, Palette.Good.b, 0.18f), 0.84f, 0.15f, 1f, 0.85f));
                chip.raycastTarget = false;
                statDelta[i] = UiKit.Label("Text", chip.transform, "", 30, Palette.Good, 0, 0, 1, 1);
            }

            // 새 기술
            var skill = UiKit.RoundPanel("NewSkill", group, Palette.PanelLight, UiKit.RadiusLg, 0.067f, 0.243f, 0.933f, 0.345f);
            skill.raycastTarget = false;
            skillBox = skill.gameObject;
            var border = UiKit.Outline(UiKit.Panel("Border", skill.transform, new Color(Palette.Gold.r, Palette.Gold.g, Palette.Gold.b, 0.7f)), UiKit.RadiusLg, 3);
            border.raycastTarget = false;
            skillIconBack = UiKit.RoundPanel("IconBack", skill.transform, Palette.Attack, UiKit.RadiusMd, 0.034f, 0.255f, 0.137f, 0.745f);
            skillIconBack.raycastTarget = false;
            skillIcon = UiKit.IconImage("Icon", skillIconBack.transform, null, 0.15f, 0.15f, 0.85f, 0.85f);
            skillTitle = UiKit.Label("Label", skill.transform, "기술이 강해졌어요!", 30, Palette.Gold, 0.17f, 0.64f, 0.97f, 0.9f, TextAnchor.MiddleLeft);
            skillName = UiKit.Display(UiKit.Label("Name", skill.transform, "", 44, Palette.Text, 0.17f, 0.36f, 0.97f, 0.66f, TextAnchor.MiddleLeft));
            skillDetail = UiKit.Label("Detail", skill.transform, "", 24, Palette.TextDim, 0.17f, 0.08f, 0.97f, 0.36f,
                TextAnchor.MiddleLeft, FontStyle.Normal, true, 16);

            var ok = UiKit.MakeButton("AwakeningOkButton", group, "좋아요!", Palette.Gold, 48, 0.022f, 0.0125f, 0.978f, 0.071f);
            UiKit.LabelOf(ok).color = Palette.OnAccent;
            ok.onClick.AddListener(Finish);
        }

        // 글자는 바로 채워 두고(건너뛰어도 같은 내용), 연출만 시간에 따라 보여준다.
        // before/after: 성유물 보너스, newSkill: 각성으로 바뀐 기술, oldSkill: 그 전 기술
        public void Play(RelicData awakened, int level, MonsterStats before, MonsterStats after,
            SkillData newSkill, SkillData oldSkill, Action finished)
        {
            relic = awakened;
            onFinished = finished;
            skip = false;
            ShowingResult = false;

            headline.text = $"<color=#FFD140>{relic.DisplayName}</color>{Josa(relic.DisplayName)} +{level}{UiKit.NumberRo(level)} 각성했다!";
            sub.text = $"{relic.Role.DisplayName()}  ·  +{level}  ·  기술이 바뀌었어요";
            var role = UiKit.Icon(InventoryView.RoleIconName(relic.Role));
            roleIcon.sprite = role;
            roleIcon.enabled = role != null;
            SetStat(0, before.MaxHp, after.MaxHp);
            SetStat(1, before.Attack, after.Attack);
            SetStat(2, before.Defense, after.Defense);

            skillBox.SetActive(newSkill != null);
            if (newSkill != null)
            {
                skillName.text = newSkill.DisplayName;
                string replaced = oldSkill != null && oldSkill != newSkill ? $"  ({oldSkill.DisplayName} 대신)" : "";
                skillDetail.text = BattleScreen.SkillDetail(newSkill) + replaced;
                skillIconBack.color = BattleScreen.SkillColor(newSkill);
                var icon = UiKit.Icon(BattleScreen.SkillIconName(newSkill));
                skillIcon.sprite = icon;
                skillIcon.enabled = icon != null;
            }

            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(Run());
        }

        private static string Josa(string name) => UiKit.WithJosa(name, "이", "가").Substring(name.Length);

        private void SetStat(int i, int before, int after)
        {
            statOld[i].text = before.ToString();
            statNew[i].text = after.ToString();
            int delta = after - before;
            statDelta[i].text = delta >= 0 ? $"+{delta}" : delta.ToString();
        }

        private IEnumerator Run()
        {
            // ① 시작: 원래 모습, 은은한 빛
            resultGroup.SetActive(false);
            messageBox.SetActive(true);
            raysImage.enabled = false;
            letterRing.gameObject.SetActive(false);
            flash.color = Color.clear;
            ShowRelic(CardSize, new Vector2(0.5f, 0.62f), false);
            message.text = $"{relic.DisplayName}에 담긴 단어들이 반짝이기 시작했어요!";
            float t = 0f, duration = 1.4f * animationScale;
            while (t < duration && !skip)
            {
                float pulse = Mathf.Sin(t * 6f) * 0.03f;
                card.localScale = Vector3.one * (1f + pulse);
                glowImage.color = Tint(Palette.Gold, 0.18f + pulse * 2f);
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            // ② 글자: 몬스터는 금빛으로 물들고, 알파벳 고리가 점점 빨리 돌며 몬스터 쪽으로 모여든다
            if (!skip)
            {
                message.text = $"글자들이 {UiKit.WithJosa(relic.DisplayName, "을", "를")} 감싸며 빙글빙글 돌고 있어요…";
                Sound.Play(Sfx.EvolveLight);
                raysImage.enabled = true;
                letterRing.gameObject.SetActive(true);
                var startColor = cardImage.color;
                var goldCard = new Color(0.98f, 0.8f, 0.42f);
                var goldSprite = new Color(1f, 0.9f, 0.6f);
                t = 0f;
                duration = 1.8f * animationScale;
                while (t < duration && !skip)
                {
                    float k = t / Mathf.Max(0.001f, duration);
                    cardImage.color = Color.Lerp(startColor, goldCard, Mathf.Clamp01(k * 2f));
                    cardSprite.color = Color.Lerp(Color.white, goldSprite, Mathf.Clamp01(k * 2f));
                    card.localScale = Vector3.one * (1f + Mathf.Sin(t * 5f) * 0.04f);
                    PlaceLetters(t, k);
                    rays.localEulerAngles = new Vector3(0, 0, -t * 30f);
                    raysImage.color = Tint(new Color(1f, 0.95f, 0.77f), 0.12f + 0.16f * k);
                    glowImage.color = Tint(Palette.Gold, 0.3f + 0.3f * k);
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            // ③ 완료: 번쩍 → 새 모습 + 결과
            ShowResult();
            t = 0f;
            duration = 0.4f * animationScale;
            while (t < duration)
            {
                flash.color = new Color(1f, 1f, 1f, 1f - t / Mathf.Max(0.001f, duration));
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            flash.color = Color.clear;
            while (true)
            {
                rays.localEulerAngles += new Vector3(0, 0, -Time.unscaledDeltaTime * 8f);
                yield return null;
            }
        }

        // 글자 고리: 회전이 점점 빨라지고 반지름은 줄어들어 마지막에 몬스터 속으로 모인다. 처음엔 서서히 나타남
        private void PlaceLetters(float t, float k)
        {
            float spin = t * (1.2f + 2.4f * k);
            float radius = Mathf.Lerp(360f, 140f, k * k);
            float alpha = Mathf.Clamp01(t * 4f) * (1f - Mathf.Clamp01((k - 0.85f) / 0.15f));
            for (int i = 0; i < letters.Count; i++)
            {
                float angle = i / (float)letters.Count * Mathf.PI * 2f + spin;
                float r = radius + (i % 2 == 0 ? 20f : -20f);
                letters[i].rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                letters[i].color = Tint(Palette.Gold, alpha * (i % 3 == 0 ? 1f : 0.75f));
            }
        }

        private void ShowResult()
        {
            ShowingResult = true;
            Sound.Play(Sfx.Evolve);
            letterRing.gameObject.SetActive(false);
            cardSprite.color = Color.white;
            messageBox.SetActive(false);
            resultGroup.SetActive(true);
            card.localScale = Vector3.one;
            ShowRelic(ResultCardSize, new Vector2(0.5f, 0.786f), true);
            glowImage.color = Tint(Palette.Gold, 0.3f);
            raysImage.enabled = true;
            raysImage.color = Tint(new Color(1f, 0.95f, 0.77f), 0.12f);
        }

        private void ShowRelic(float size, Vector2 anchor, bool border)
        {
            foreach (var rt in new[] { card, glow, rays, letterRing })
                rt.anchorMin = rt.anchorMax = anchor;
            card.sizeDelta = new Vector2(size, size);
            cardImage.color = Color.Lerp(Palette.PanelLight, relic.PlaceholderColor, 0.22f);
            var sprite = UiKit.RelicIcon(relic);
            cardInitial.text = relic.DisplayName.Length > 0 ? relic.DisplayName.Substring(0, 1) : "?";
            cardInitial.fontSize = Mathf.RoundToInt(size * 0.42f);
            cardSprite.sprite = sprite;
            cardSprite.color = Color.white;
            cardSprite.enabled = sprite != null;
            cardInitial.enabled = sprite == null;
            cardBorder.enabled = border;
        }

        private static Color Tint(Color color, float alpha) => new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));

        private void Finish()
        {
            StopAllCoroutines();
            gameObject.SetActive(false);
            var finished = onFinished;
            onFinished = null;
            finished?.Invoke();
        }

        // 제단 화면을 닫을 때 연출도 같이 정리
        public void Stop()
        {
            StopAllCoroutines();
            onFinished = null;
            gameObject.SetActive(false);
        }
    }
}
