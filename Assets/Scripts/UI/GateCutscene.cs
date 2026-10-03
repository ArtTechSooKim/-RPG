using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WordRPG.UI
{
    // 길 열림 연출 화면 (Figma '길 열림 연출'): 영화처럼 위아래 검은 띠, 위 띠에 지역 이름, 아래에 금색 알림 띠,
    // 화면 전체 어둡게·밝게. 필드 HUD 위에 따로 캔버스를 띄우고, 띠와 어둠이 터치를 막는다 (FieldScreen.RevealGates)
    public class GateCutscene : MonoBehaviour
    {
        private const float BarHeight = 0.13f; // 띠 높이 (화면 비율)

        private RectTransform root;
        private Image fade;
        private RectTransform topBar, bottomBar;
        private Text place;
        private Image banner;
        private Text bannerText;

        public string BannerText => banner.gameObject.activeSelf ? bannerText.text : "";

        public static GateCutscene Create(Transform parent)
        {
            var go = new GameObject("GateCutscene", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var view = go.AddComponent<GateCutscene>();
            view.root = UiKit.Stretch("Root", go.transform);

            var top = UiKit.Panel("TopBar", view.root, Color.black, 0, 1, 1, 1);
            view.topBar = top.rectTransform;
            view.place = UiKit.Display(UiKit.OneLine(UiKit.Label("Place", top.transform, "", 46, Palette.Gold, 0, 0, 1, 0.7f)));
            var bottom = UiKit.Panel("BottomBar", view.root, Color.black, 0, 0, 1, 0);
            view.bottomBar = bottom.rectTransform;

            view.banner = UiKit.RoundPanel("Banner", view.root, Palette.Scrim, UiKit.RadiusLg, 0.06f, 0.19f, 0.94f, 0.27f);
            view.banner.raycastTarget = false;
            var outline = UiKit.Outline(UiKit.Panel("Border", view.banner.transform, Palette.Gold), UiKit.RadiusLg, 4);
            outline.raycastTarget = false;
            view.bannerText = UiKit.Display(UiKit.OneLine(UiKit.Label("Text", view.banner.transform, "", 52, Palette.Gold, 0, 0, 1, 1)));
            view.banner.gameObject.SetActive(false);

            view.fade = UiKit.Panel("Fade", view.root, new Color(0, 0, 0, 0));
            view.SetBars(0f);
            return view;
        }

        public IEnumerator Fade(float from, float to, float duration)
        {
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                fade.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t / duration));
                yield return null;
            }
            fade.color = new Color(0, 0, 0, to);
            fade.raycastTarget = to > 0f;
        }

        public void ShowLetterbox(string placeName)
        {
            place.text = placeName;
            SetBars(1f);
        }

        public void HideLetterbox()
        {
            SetBars(0f);
            banner.gameObject.SetActive(false);
        }

        public void ShowBanner(string text)
        {
            bannerText.text = text;
            banner.gameObject.SetActive(true);
            StartCoroutine(Pop(banner.rectTransform));
        }

        // 연기가 퍼지며 막힌 덤불이 사라진다 (월드 좌표 → 화면 위치)
        public IEnumerator PlaySmoke(Camera cam, Vector3 world, float animScale)
        {
            var anchor = UiKit.Rect("SmokeAnchor", root, 0, 0, 0, 0);
            anchor.SetSiblingIndex(0);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, cam.WorldToScreenPoint(world), null, out var local))
            {
                anchor.anchorMin = anchor.anchorMax = new Vector2(0.5f, 0.5f);
                anchor.anchoredPosition = local;
            }
            yield return BattleFx.Play(anchor, Fx.Smoke, 280f, animScale);
            if (anchor != null) Destroy(anchor.gameObject);
        }

        private void SetBars(float k)
        {
            topBar.anchorMin = new Vector2(0, 1f - BarHeight * k);
            bottomBar.anchorMax = new Vector2(1, BarHeight * k);
        }

        private static IEnumerator Pop(RectTransform rect)
        {
            for (float t = 0; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.25f;
                rect.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(k * Mathf.PI)) * Mathf.Lerp(0.6f, 1f, k);
                yield return null;
            }
            rect.localScale = Vector3.one;
        }
    }
}
