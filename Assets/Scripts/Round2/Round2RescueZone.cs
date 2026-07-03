using System.Collections.Generic;
using UnityEngine;

public enum Round2ZoneType
{
    Rescue,
    Dropoff
}

public class Round2RescueZone : MonoBehaviour
{
    public Round2ZoneType zoneType = Round2ZoneType.Rescue;
    public string zoneName = "Khu vực cứu hộ";
    public int civiliansAvailable = 2;

    // ── Excluded name fragments ────────────────────────────────────────────────
    private static readonly string[] s_ExcludeNames =
        { "FloatingText", "BND_", "RescueVoiceCue", "Zone", "Anchor" };

    /// <summary>
    /// Returns up to <paramref name="maxCount"/> active civilian Transforms from
    /// this zone, prioritising objects with Animator / SkinnedMeshRenderer.
    /// Call this BEFORE OnRescued() so the objects are still active.
    /// </summary>
    public List<Transform> GetVisibleCivilianVisuals(int maxCount)
    {
        var result = new List<Transform>();
        if (maxCount <= 0) return result;

        foreach (Transform child in transform)
        {
            if (result.Count >= maxCount) break;
            if (!child.gameObject.activeSelf) continue;
            if (IsExcluded(child.name)) continue;

            // Accept if it has any render/animation component
            bool hasVisual = child.GetComponentInChildren<Animator>(true) != null
                          || child.GetComponentInChildren<SkinnedMeshRenderer>(true) != null
                          || child.GetComponentInChildren<MeshRenderer>(true) != null;

            if (hasVisual)
                result.Add(child);
        }
        return result;
    }

    private static bool IsExcluded(string name)
    {
        foreach (var fragment in s_ExcludeNames)
            if (name.Contains(fragment)) return true;
        return false;
    }

    public void OnRescued(int count)
    {
        int hidden = 0;
        foreach (Transform child in transform)
        {
            if (hidden >= count) break;
            
            // Bỏ qua Text nổi và các component hệ thống
            if (child.name == "FloatingText" || child.name.Contains("BND_") || !child.gameObject.activeSelf)
                continue;

            child.gameObject.SetActive(false);
            hidden++;
        }

        // Cập nhật hoặc ẩn dòng chữ nổi
        Transform txtObj = transform.Find("FloatingText");
        if (txtObj != null)
        {
            if (civiliansAvailable <= 0)
            {
                txtObj.gameObject.SetActive(false);
            }
            else
            {
                var tmp = txtObj.GetComponent<TMPro.TextMeshPro>();
                if (tmp != null)
                {
                    tmp.text = "Có người mắc kẹt gần đây.";
                }
            }
        }
    }
}
