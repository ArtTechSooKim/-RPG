using UnityEngine;
using UnityEngine.EventSystems;

namespace WordRPG.UI
{
    // 누르고 있는 동안 IsHeld = true. 가상 방향 패드용 (Button은 '뗄 때 한 번'이라 이동에 맞지 않음)
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool IsHeld { get; private set; }

        public void OnPointerDown(PointerEventData eventData) => IsHeld = true;
        public void OnPointerUp(PointerEventData eventData) => IsHeld = false;
        public void OnPointerExit(PointerEventData eventData) => IsHeld = false;

        private void OnDisable() => IsHeld = false;
    }
}
