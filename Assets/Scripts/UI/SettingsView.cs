using System;
using UnityEngine;
using UnityEngine.UI;
using WordRPG.Game;

namespace WordRPG.UI
{
    // 설정 (Figma '설정' 화면): 소리(배경 음악·효과음), 진동, 저장 데이터 지우기(+확인 창), 정보.
    // 값을 바꿀 때마다 onSettingsChanged로 저장. 저장 데이터 지우기는 확인 창에서 [지우기]를 눌러야 onDeleteSave
    public class SettingsView
    {
        public GameObject Root { get; private set; }
        public bool IsOpen => Root != null && Root.activeSelf;
        public ConfirmDialog Dialog { get; private set; }

        private Slider music, sfx;
        private SwitchView vibration;
        private GameSettings settings;
        private Action onSettingsChanged;
        private Action onDeleteSave;

        public static SettingsView Create(Transform parent)
        {
            var view = new SettingsView();
            var root = UiKit.Stretch("SettingsView", parent);
            view.Root = root.gameObject;
            view.Root.AddComponent<Image>().color = Palette.Background;

            UiKit.Display(UiKit.Label("Title", root, "설정", 56, Palette.Gold, 0, 0.93f, 1, 0.99f));

            // 소리
            var sound = Section(root, "소리", 0.745f, 0.915f);
            Row(sound, "music", "배경 음악", null, 0.47f, 0.71f);
            view.music = UiKit.MakeSlider("MusicSlider", sound, 0.44f, 0.47f, 0.965f, 0.71f);
            view.music.onValueChanged.AddListener(v => view.Change(s => s.MusicVolume = v));
            Row(sound, "sound", "효과음", null, 0.15f, 0.39f);
            view.sfx = UiKit.MakeSlider("SfxSlider", sound, 0.44f, 0.15f, 0.965f, 0.39f);
            view.sfx.onValueChanged.AddListener(v => view.Change(s => s.SfxVolume = v));

            // 게임
            var game = Section(root, "게임", 0.6f, 0.73f);
            Row(game, null, "진동", "틀렸을 때 짧게 떨려요", 0.1f, 0.66f);
            view.vibration = SwitchView.Create("VibrationSwitch", game, 0.84f, 0.24f, 0.965f, 0.54f);

            // 저장
            var save = Section(root, "저장", 0.39f, 0.585f);
            Row(save, null, "자동 저장", "답할 때마다, 전투가 끝날 때마다 저절로 저장돼요", 0.44f, 0.8f);
            var delete = UiKit.MakeButton("DeleteSaveButton", save, "저장 데이터 지우기", Palette.Confirm, 42, 0.035f, 0.08f, 0.965f, 0.38f);
            delete.onClick.AddListener(view.AskDelete);

            // 정보
            var info = Section(root, "정보", 0.255f, 0.375f);
            UiKit.Label("Version", info, $"버전 {Application.version} (MVP)", 28, Palette.TextDim, 0.035f, 0.4f, 0.965f, 0.66f,
                TextAnchor.MiddleLeft);
            UiKit.Label("Fonts", info, "글꼴  Jua · Noto Sans KR — SIL Open Font License 1.1", 28, Palette.TextDim,
                0.035f, 0.1f, 0.965f, 0.38f, TextAnchor.MiddleLeft, FontStyle.Normal, true, 18);

            var close = UiKit.MakeButton("SettingsCloseButton", root, "닫기", Palette.Neutral, 44, 0.05f, 0.015f, 0.95f, 0.095f);
            close.onClick.AddListener(view.Hide);

            view.Dialog = ConfirmDialog.Create(root);
            view.Root.SetActive(false);
            return view;
        }

        // 둥근 판 + 금색 소제목
        private static RectTransform Section(RectTransform root, string title, float minY, float maxY)
        {
            var panel = UiKit.RoundPanel($"Section_{title}", root, Palette.Panel, UiKit.RadiusLg, 0.022f, minY, 0.978f, maxY);
            float header = Mathf.Min(0.3f, 60f / ((maxY - minY) * 1920f));
            UiKit.Label("Header", panel.transform, title, 30, Palette.Gold, 0.035f, 1f - header - 0.04f, 0.6f, 0.98f,
                TextAnchor.MiddleLeft);
            return panel.rectTransform;
        }

        // [아이콘] 이름 (+ 작은 설명)
        private static void Row(RectTransform section, string icon, string label, string sub, float minY, float maxY)
        {
            float x = 0.035f;
            if (icon != null)
            {
                UiKit.IconImage($"Icon_{label}", section, UiKit.Icon(icon), x, minY + 0.02f, x + 0.05f, maxY - 0.02f);
                x += 0.065f;
            }
            if (sub == null)
            {
                UiKit.Label($"Label_{label}", section, label, 34, Palette.Text, x, minY, 0.42f, maxY, TextAnchor.MiddleLeft, FontStyle.Bold);
                return;
            }
            float mid = minY + (maxY - minY) * 0.45f;
            UiKit.Label($"Label_{label}", section, label, 34, Palette.Text, x, mid, 0.8f, maxY, TextAnchor.LowerLeft, FontStyle.Bold);
            UiKit.Label($"Sub_{label}", section, sub, 24, Palette.TextDim, x, minY, 0.8f, mid, TextAnchor.UpperLeft,
                FontStyle.Normal, true, 18);
        }

        public void Show(GameSettings gameSettings, Action settingsChanged, Action deleteSave)
        {
            settings = gameSettings ?? new GameSettings();
            onSettingsChanged = settingsChanged;
            onDeleteSave = deleteSave;
            music.SetValueWithoutNotify(settings.MusicVolume);
            sfx.SetValueWithoutNotify(settings.SfxVolume);
            vibration.Bind(settings.Vibration, on => Change(s => s.Vibration = on));
            Dialog.Hide();
            Root.transform.SetAsLastSibling();
            Root.SetActive(true);
        }

        public void Hide()
        {
            Dialog.Hide();
            Root.SetActive(false);
        }

        private void Change(Action<GameSettings> apply)
        {
            if (settings == null) return;
            apply(settings);
            Sound.ApplyVolumes(settings);
            onSettingsChanged?.Invoke();
        }

        private void AskDelete()
        {
            Dialog.Show("저장 데이터를 지울까요?",
                "발견한 단어, 몬스터, 아이템이 모두 사라지고\n처음부터 다시 시작해요. 되돌릴 수 없어요.",
                "지우기", true, () =>
                {
                    var delete = onDeleteSave;
                    Hide();
                    delete?.Invoke();
                });
        }
    }
}
