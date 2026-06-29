#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

public class FontDiscovery : MonoBehaviour
{
    [MenuItem("FloodRoute/List TMP Font Assets")]
    static void ListFonts()
    {
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        if (guids.Length == 0)
        {
            Debug.Log("[FontDiscovery] No TMP_FontAsset found in project.");
            return;
        }
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            Debug.Log($"[FontDiscovery] Found: '{fa.name}' at {path}");
        }
    }
}
#endif
