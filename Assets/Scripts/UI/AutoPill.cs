using UnityEngine;
using UnityEngine.UI;

namespace WordRPG.UI
{
    // 둥근 9-slice 이미지의 모서리를 높이의 절반으로 맞춘다 → 크기가 바뀌어도 항상 알약 모양
    [RequireComponent(typeof(Image))]
    public class AutoPill : MonoBehaviour
    {
        private const float SpriteRadius = 34f; // UiKit.RoundedSprite(32)의 border

        private Image image;

        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            if (image == null) image = GetComponent<Image>();
            float height = ((RectTransform)transform).rect.height;
            if (height > 0f) image.pixelsPerUnitMultiplier = SpriteRadius * 2f / height;
        }
    }
}
