using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Roni.Utility.Debugging
{
    public static class DebugExtensions
    {
        public static void ShowMessageDebug(Color color, string header, string message)
        {
#if UNITY_EDITOR
            string colorHex = UnityEngine.ColorUtility.ToHtmlStringRGB(color);
            // 리치 텍스트 형식으로 변환
            string richText = $"<color=#{colorHex}>[ {header} ]</color> <color=white>{message}</color>";
            Debug.Log(richText);
#endif
        }
        public static void ShowMessageDebugError(string header, string message)
        {
#if UNITY_EDITOR
            string colorHex = UnityEngine.ColorUtility.ToHtmlStringRGB(Color.red);
            // 리치 텍스트 형식으로 변환
            string richText = $"<color=#{colorHex}>[ {header} ]</color> <color=white>{message}</color>";
            Debug.LogError(richText);
#endif
        }
    }
}