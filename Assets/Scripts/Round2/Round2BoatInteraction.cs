using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class Round2BoatInteraction : MonoBehaviour
{
    [Header("Configuration")]
    public float maxInteractSpeed = 0.35f;
    
    [Header("References")]
    public Round2.Round2FirstPersonBoatController boatMovement;
    public Round2RealtimeRoundController roundController;
    public TextMeshProUGUI txtInteractionPrompt;

    private Round2RescueZone currentZone = null;

    private void Start()
    {
        if (boatMovement == null) boatMovement = GetComponentInParent<Round2.Round2FirstPersonBoatController>();
        if (roundController == null) roundController = FindObjectOfType<Round2RealtimeRoundController>();
        
        if (txtInteractionPrompt != null)
        {
            txtInteractionPrompt.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Round2RescueZone zone = other.GetComponent<Round2RescueZone>();
        if (zone != null)
        {
            currentZone = zone;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Round2RescueZone zone = other.GetComponent<Round2RescueZone>();
        if (zone != null && currentZone == zone)
        {
            currentZone = null;
            if (txtInteractionPrompt != null)
            {
                txtInteractionPrompt.gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (roundController == null || !roundController.IsPlaying())
        {
            if (txtInteractionPrompt != null && txtInteractionPrompt.gameObject.activeSelf)
            {
                txtInteractionPrompt.gameObject.SetActive(false);
            }
            return;
        }

        if (currentZone != null)
        {
            bool speedOk = (boatMovement != null && boatMovement.CurrentSpeedAbs <= maxInteractSpeed);
            
            if (txtInteractionPrompt != null)
            {
                txtInteractionPrompt.gameObject.SetActive(true);

                if (currentZone.zoneType == Round2ZoneType.Rescue)
                {
                    if (currentZone.civiliansAvailable <= 0)
                    {
                        txtInteractionPrompt.text = "Không còn người cần cứu ở đây.";
                        txtInteractionPrompt.color = Color.gray;
                    }
                    else if (!speedOk)
                    {
                        txtInteractionPrompt.text = "Dừng thuyền lại để cứu người.";
                        txtInteractionPrompt.color = Color.red;
                    }
                    else
                    {
                        txtInteractionPrompt.text = "Nhấn E để cứu người.";
                        txtInteractionPrompt.color = Color.yellow;
                    }
                }
                else if (currentZone.zoneType == Round2ZoneType.Dropoff)
                {
                    if (roundController.currentCargo == 0)
                    {
                        txtInteractionPrompt.text = "Chưa có người dân trên thuyền.";
                        txtInteractionPrompt.color = Color.gray;
                    }
                    else if (!speedOk)
                    {
                        txtInteractionPrompt.text = "Dừng thuyền lại để thả người.";
                        txtInteractionPrompt.color = Color.red;
                    }
                    else
                    {
                        txtInteractionPrompt.text = "Nhấn E để thả người.";
                        txtInteractionPrompt.color = Color.green;
                    }
                }
            }

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame)
            {
                TryInteract(speedOk);
            }
        }
        else
        {
            if (txtInteractionPrompt != null && txtInteractionPrompt.gameObject.activeSelf)
            {
                txtInteractionPrompt.gameObject.SetActive(false);
            }
        }
    }

    private void TryInteract(bool speedOk)
    {
        if (!speedOk)
        {
            roundController.ShowFeedback(currentZone.zoneType == Round2ZoneType.Rescue ? "Dừng thuyền lại để cứu người." : "Dừng thuyền lại để thả người.");
            return;
        }

        if (currentZone.zoneType == Round2ZoneType.Rescue)
        {
            if (currentZone.civiliansAvailable <= 0)
            {
                roundController.ShowFeedback("Không còn người cần cứu ở đây.");
                return;
            }

            int freeCapacity = roundController.boatCapacity - roundController.currentCargo;
            if (freeCapacity <= 0)
            {
                roundController.ShowFeedback("Thuyền đã đầy. Hãy đưa người dân đến điểm trú.");
                return;
            }

            int toRescue = Mathf.Min(freeCapacity, currentZone.civiliansAvailable);
            currentZone.civiliansAvailable -= toRescue;
            currentZone.OnRescued(toRescue);
            roundController.AddCargo(toRescue);
            
            roundController.ShowFeedback($"Đã cứu {toRescue} người dân.");

            if (roundController.currentCargo > 0)
            {
                roundController.SetObjective("Mục tiêu: Đưa người dân đến điểm trú.");
            }
            else
            {
                roundController.SetObjective("Mục tiêu: Tìm nhà có tín hiệu cầu cứu.");
            }
        }
        else if (currentZone.zoneType == Round2ZoneType.Dropoff)
        {
            if (roundController.currentCargo <= 0)
            {
                roundController.ShowFeedback("Chưa có người dân trên thuyền.");
                return;
            }

            int delivered = roundController.currentCargo;
            roundController.DeliverCargo(delivered);
            
            roundController.ShowFeedback($"Đã đưa {delivered} người dân đến nơi an toàn.");

            if (roundController.civiliansSafe < roundController.totalCivilians)
            {
                roundController.SetObjective("Mục tiêu: Tiếp tục tìm người mắc kẹt.");
            }
            else
            {
                // TriggerWin is called by DeliverCargo if all safe
                roundController.SetObjective("Hoàn thành cứu hộ!");
            }
        }
    }
}
