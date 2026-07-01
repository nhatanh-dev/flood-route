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
