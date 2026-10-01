using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WordRPG.Monsters;

namespace WordRPG.UI
{
    // 진화 연출 (Figma '진화 연출 1·2·3'): ① "어라…?" ② 빛줄기 속 하얀 실루엣 ③ "축하해요!" 새 모습 + 능력치 변화 + 새 기술.
    // 화면을 누르면 ③으로 건너뛴다. [좋아요!]를 누르면 닫히고 finished
    public class EvolutionCutscene : MonoBehaviour
    {
        private const float CardSize = 420f;
        private const float ResultCardSize = 360f;

        public bool IsPlaying => gameObject.activeSelf;
        public bool ShowingResult { get; private set; }

        private float animationScale = 1f;
        private bool skip;
        private Action onFinished;

        private RectTransform card, glow, rays;
        private Image cardImage, cardSprite, cardBorder, glowImage, raysImage, flash;
        private Text cardInitial, message, title, headline, sub, skillTitle, skillName, skillDetail;
        private GameObject messageBox, resultGroup, skillBox;
        private Image skillIconBack, skillIcon, roleIcon;
        private readonly Text[] statOld = new Text[3], statNew = new Text[3], statDelta = new Text[3];

        private MonsterSpecies from, to;

        public static EvolutionCutscene Create(Transform parent, float animScale)
        {
            var root = UiKit.Panel("EvolutionCutscene", parent, Palette.Background);
            var cutscene = root.gameObject.AddComponent<EvolutionCutscene>();
            cutscene.animationScale = animScale;
            cutscene.Build(root.rectTransform);
            root.gameObject.SetActive(false);
            return cutscene;
        }

        private void Build(RectTransform root)
        {
            // 화면 아무 데나 누르면 결과로 건너뛰기 (맨 뒤에 깔아 둔다)
            var skipArea = UiKit.AddButton(UiKit.Panel("EvolutionSkipArea", root, Color.clear));
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
            cardSprite = UiKit.IconImage("Sprite", card, null, 0.06f, 0.06f, 0.94f, 0.94f);
            cardBorder = UiKit.Outline(UiKit.Panel("Border", card, Palette.Gold), UiKit.RadiusLg, 8);
            cardBorder.raycastTarget = false;

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
            title = UiKit.Display(UiKit.Label("Title", group, "축하해요!", 64, Palette.Gold, 0, 0.9f, 1, 0.955f));
            headline = UiKit.Display(UiKit.Label("Headline", group, "", 46, Palette.Text, 0.03f, 0.635f, 0.97f, 0.675f,
                TextAnchor.MiddleCenter, FontStyle.Normal, true, 26));
            var subRow = UiKit.Rect("Sub", group, 0.03f, 0.605f, 0.97f, 0.633f);
            roleIcon = UiKit.IconImage("Role", subRow, null, 0.2f, 0.05f, 0.24f, 0.95f);
            sub = UiKit.Label("Text", subRow, "", 30, Palette.TextDim, 0.25f, 0, 1f, 1, TextAnchor.MiddleLeft);

            // 능력치 (HP · 공격 · 방어)
            var stats = UiKit.RoundPanel("Stats", group, Palette.Panel, UiKit.RadiusLg, 0.067f, 0.36f, 0.933f, 0.588f);
            stats.raycastTarget = false;
            UiKit.Label("Header", stats.transform, "능력치", 30, Palette.Gold, 0.045f, 0.82f, 0.5f, 0.96f, TextAnchor.MiddleLeft);
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
            skillTitle = UiKit.Label("Label", skill.transform, "새 기술을 배웠어요!", 30, Palette.Gold, 0.17f, 0.64f, 0.97f, 0.9f, TextAnchor.MiddleLeft);
            skillName = UiKit.Display(UiKit.Label("Name", skill.transform, "", 44, Palette.Text, 0.17f, 0.36f, 0.97f, 0.66f, TextAnchor.MiddleLeft));
            skillDetail = UiKit.Label("Detail", skill.transform, "", 24, Palette.TextDim, 0.17f, 0.08f, 0.97f, 0.36f,
                TextAnchor.MiddleLeft, FontStyle.Normal, true, 16);

            var ok = UiKit.MakeButton("EvolutionOkButton", group, "좋아요!", Palette.Gold, 48, 0.022f, 0.0125f, 0.978f, 0.071f);
            UiKit.LabelOf(ok).color = Palette.OnAccent;
            ok.onClick.AddListener(Finish);
        }

        // 글자는 바로 채워 두고(건너뛰어도 같은 내용), 연출만 시간에 따라 보여준다
        public void Play(MonsterSpecies fromSpecies, MonsterSpecies toSpecies, int level, MonsterStats before, MonsterStats after,
            IReadOnlyList<SkillData> newSkills, IReadOnlyList<SkillData> lostSkills, Action finished)
        {
            from = fromSpecies;
            to = toSpecies;
            onFinished = finished;
            skip = false;
            ShowingResult = false;

            headline.text = $"{UiKit.WithJosa(from.DisplayName, "이", "가")} <color=#FFD140>{to.DisplayName}</color>{Josa(to.DisplayName)} 진화했다!";
            sub.text = $"Lv{level}  ·  {to.Role.DisplayName()}  ·  HP를 모두 회복했어요";
            var role = UiKit.Icon(InventoryView.RoleIconName(to.Role));
            roleIcon.sprite = role;
            roleIcon.enabled = role != null;
            SetStat(0, before.MaxHp, after.MaxHp);
            SetStat(1, before.Attack, after.Attack);
            SetStat(2, before.Defense, after.Defense);

            bool hasSkill = newSkills != null && newSkills.Count > 0;
            skillBox.SetActive(hasSkill);
            if (hasSkill)
            {
                var skill = newSkills[0];
                skillName.text = newSkills.Count > 1 ? $"{skill.DisplayName} 외 {newSkills.Count - 1}개" : skill.DisplayName;
                string replaced = lostSkills != null && lostSkills.Count == 1 ? $"  ({lostSkills[0].DisplayName} 대신)" : "";
                skillDetail.text = BattleScreen.SkillDetail(skill) + replaced;
                skillIconBack.color = BattleScreen.SkillColor(skill);
                var icon = UiKit.Icon(BattleScreen.SkillIconName(skill));
                skillIcon.sprite = icon;
                skillIcon.enabled = icon != null;
            }

            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(Run());
        }

        private static string Josa(string name) => UiKit.WithJosa(name, "으로", "로").Substring(name.Length);

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
            flash.color = Color.clear;
            ShowSpecies(from, CardSize, new Vector2(0.5f, 0.62f), false);
            message.text = $"어라…? {from.DisplayName}의 모습이…!";
            float t = 0f, duration = 1.4f * animationScale;
            while (t < duration && !skip)
            {
                float pulse = Mathf.Sin(t * 6f) * 0.03f;
                card.localScale = Vector3.one * (1f + pulse);
                glowImage.color = Tint(Palette.Gold, 0.18f + pulse * 2f);
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            // ② 빛: 빛줄기가 돌고 하얀 실루엣이 커졌다 작아졌다
            if (!skip)
            {
                message.text = $"빛이 {UiKit.WithJosa(from.DisplayName, "을", "를")} 감싸고 있어요…";
                raysImage.enabled = true;
                cardInitial.enabled = false;
                cardSprite.enabled = false;
                t = 0f;
                duration = 1.8f * animationScale;
                while (t < duration && !skip)
                {
                    float k = t / Mathf.Max(0.001f, duration);
                    cardImage.color = Color.Lerp(cardImage.color, new Color(1f, 0.97f, 0.88f), 0.15f);
                    card.localScale = Vector3.one * (1f + Mathf.Sin(t * (8f + 16f * k)) * 0.08f);
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

        private void ShowResult()
        {
            ShowingResult = true;
            messageBox.SetActive(false);
            resultGroup.SetActive(true);
            card.localScale = Vector3.one;
            ShowSpecies(to, ResultCardSize, new Vector2(0.5f, 0.786f), true);
            glowImage.color = Tint(Palette.Gold, 0.3f);
            raysImage.enabled = true;
            raysImage.color = Tint(new Color(1f, 0.95f, 0.77f), 0.12f);
        }

        private void ShowSpecies(MonsterSpecies species, float size, Vector2 anchor, bool border)
        {
            foreach (var rt in new[] { card, glow, rays })
                rt.anchorMin = rt.anchorMax = anchor;
            card.sizeDelta = new Vector2(size, size);
            cardImage.color = Color.Lerp(Palette.PanelLight, species.PlaceholderColor, 0.65f);
            cardInitial.text = species.DisplayName.Length > 0 ? species.DisplayName.Substring(0, 1) : "?";
            cardInitial.fontSize = Mathf.RoundToInt(size * 0.42f);
            cardSprite.sprite = species.Sprite;
            cardSprite.enabled = species.Sprite != null;
            cardInitial.enabled = species.Sprite == null;
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

        // 진화 화면을 닫을 때 연출도 같이 정리
        public void Stop()
        {
            StopAllCoroutines();
            onFinished = null;
            gameObject.SetActive(false);
        }
    }
}
