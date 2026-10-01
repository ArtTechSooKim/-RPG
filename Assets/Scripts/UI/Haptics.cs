using UnityEngine;

namespace WordRPG.UI
{
    // 휴대폰 진동. 에디터·PC에서는 아무것도 하지 않는다
    public static class Haptics
    {
        public static void Vibrate()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
