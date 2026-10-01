using System;
using UnityEngine;
using UnityEngine.UI;

namespace WordRPG.UI
{
    // 켜기/끄기 스위치 (Figma 'Toggle'): 켜짐 = 초록 바탕 + 오른쪽 손잡이, 꺼짐 = 어두운 바탕 + 왼쪽 손잡이
    public class SwitchView
    {
        public Button Button { get; private set; }
        public bool IsOn { get; private set; }

        private Image track;
        private RectTransform knob;
        private Action<bool> onChanged;

        public static SwitchView Create(string name, Transform parent, float minX, float minY, float maxX, float maxY)
        {
            var view = new SwitchView();
            view.track = UiKit.Pill(UiKit.Panel(name, parent, Palette.Track, minX, minY, maxX, maxY));
            view.Button = UiKit.AddButton(view.track);
            var knobImage = UiKit.Pill(UiKit.Panel("Knob", view.track.transform, Palette.Text));
            knobImage.raycastTarget = false;
            view.knob = knobImage.rectTransform;
            var fitter = knobImage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            fitter.aspectRatio = 1f;
            view.Button.onClick.AddListener(() => view.Toggle());
            return view;
        }

        // 화면을 열 때 현재 값으로 맞춘다 (onChanged는 부르지 않음)
        public void Bind(bool on, Action<bool> changed)
        {
            onChanged = changed;
            Set(on);
        }

        private void Toggle()
        {
            Set(!IsOn);
            onChanged?.Invoke(IsOn);
        }

        private void Set(bool on)
        {
            IsOn = on;
            track.color = on ? Palette.Good : Palette.Track;
            float x = on ? 1f : 0f;
            knob.anchorMin = new Vector2(x, 0.11f);
            knob.anchorMax = new Vector2(x, 0.89f);
            knob.pivot = new Vector2(x, 0.5f);
            knob.anchoredPosition = new Vector2(on ? -8f : 8f, 0f);
        }
    }
}
