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
            // ��ġ �ؽ�Ʈ �������� ��ȯ
            string richText = $"<color=#{colorHex}>[ {header} ]</color> <color=white>{message}</color>";
            Debug.Log(richText);
#endif
        }
        public static void ShowMessageDebugError(string header, string message)
        {
#if UNITY_EDITOR
            string colorHex = UnityEngine.ColorUtility.ToHtmlStringRGB(Color.red);
            // ��ġ �ؽ�Ʈ �������� ��ȯ
            string richText = $"<color=#{colorHex}>[ {header} ]</color> <color=white>{message}</color>";
            Debug.LogError(richText);
#endif
        }
    }
}