using TMPro;
using UnityEngine;

public class RescueProgressHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private string progressFormat = "Đã cứu: {0}/{1}";
    [SerializeField] private string incompleteHint = "Đưa về Điểm trú";
    [SerializeField] private string completeHint = "Hoàn thành";

    public void SetProgress(int rescuedPeople, int totalPeople)
    {
        rescuedPeople = Mathf.Clamp(rescuedPeople, 0, Mathf.Max(0, totalPeople));

        if (progressText != null)
        {
            progressText.text = string.Format(progressFormat, rescuedPeople, totalPeople);
        }

        if (hintText != null)
        {
            hintText.text = rescuedPeople >= totalPeople ? completeHint : incompleteHint;
        }
    }

    private void Awake()
    {
        if (progressText == null)
        {
            progressText = GetComponentInChildren<TMP_Text>(true);
        }
    }
}
