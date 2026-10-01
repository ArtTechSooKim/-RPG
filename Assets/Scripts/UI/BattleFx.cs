using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordRPG.UI
{
    public enum Fx { Slash, Claw, Explosion, Heal, Shield, Smoke }

    // 전투 효과 애니메이션 (Ninja Adventure FX → Art/NinjaAdventure/Fx/{이름}.png, 정사각 프레임을 가로로 이어 붙인 시트).
    // 카드 위에 잠깐 겹쳐 그렸다가 사라진다. 그림이 없으면 아무것도 하지 않는다
    public static class BattleFx
    {
        private const string Folder = "Art/NinjaAdventure/Fx/";
        private const float FramesPerSecond = 14f;
        private static readonly Dictionary<Fx, Sprite[]> Cache = new Dictionary<Fx, Sprite[]>();

        public static Sprite[] Frames(Fx fx)
        {
            if (Cache.TryGetValue(fx, out var frames)) return frames;
            var sheet = Resources.Load<Texture2D>(Folder + fx.ToString().ToLowerInvariant());
            if (sheet == null || sheet.height == 0)
            {
                Cache[fx] = null;
                return null;
            }
            int size = sheet.height, count = sheet.width / size;
            frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = Sprite.Create(sheet, new Rect(i * size, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
            Cache[fx] = frames;
            return frames;
        }

        // target 가운데에 size 크기로 한 번 재생. animScale: 연출 시간 배율
        public static IEnumerator Play(RectTransform target, Fx fx, float size, float animScale)
        {
            var frames = Frames(fx);
            if (frames == null || frames.Length == 0 || target == null) yield break;

            var image = UiKit.IconImage($"Fx_{fx}", target, frames[0], 0.5f, 0.6f, 0.5f, 0.6f);
            image.rectTransform.sizeDelta = new Vector2(size, size);
            float frameTime = animScale / FramesPerSecond;
            foreach (var frame in frames)
            {
                if (image == null) yield break;
                image.sprite = frame;
                float end = Time.unscaledTime + frameTime;
                while (Time.unscaledTime < end) yield return null;
            }
            if (image != null) Object.Destroy(image.gameObject);
        }
    }

    // 몬스터 그림이 숨 쉬듯 위아래로 살짝 움직인다 (카드마다 박자를 다르게)
    public class IdleBob : MonoBehaviour
    {
        public float Amplitude = 6f;
        public float Speed = 2.2f;
        private float phase;
        private RectTransform rect;

        private void Awake()
        {
            rect = (RectTransform)transform;
            phase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            rect.anchoredPosition = new Vector2(0, Mathf.Sin(Time.unscaledTime * Speed + phase) * Amplitude);
        }
    }
}
