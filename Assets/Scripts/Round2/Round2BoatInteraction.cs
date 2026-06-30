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
            SetPromptVisible(false);
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
                SetPromptVisible(false);
            }
        }
    }

    private void Update()
    {
        if (roundController == null || !roundController.IsPlaying())
        {
            SetPromptVisible(false);
            return;
        }

        if (currentZone != null)
        {
            bool speedOk = (boatMovement != null && boatMovement.CurrentSpeedAbs <= maxInteractSpeed);
            
            if (txtInteractionPrompt != null)
            {
                SetPromptVisible(true);

                if (currentZone.zoneType == Round2ZoneType.Rescue)
                {
                    if (currentZone.civiliansAvailable <= 0)
                    {
                        txtInteractionPrompt.text = "Không còn người cần cứu ở đây";
                        txtInteractionPrompt.color = new Color(0.72f, 0.75f, 0.71f);
                    }
                    else if (!speedOk)
                    {
                        txtInteractionPrompt.text = "Dừng thuyền lại để cứu người";
                        txtInteractionPrompt.color = new Color(0.82f, 0.58f, 0.45f);
                    }
                    else
                    {
                        txtInteractionPrompt.text = "Nhấn E để cứu người";
                        txtInteractionPrompt.color = new Color(0.94f, 0.91f, 0.82f);
                    }
                }
                else if (currentZone.zoneType == Round2ZoneType.Dropoff)
                {
                    if (roundController.currentCargo == 0)
                    {
                        txtInteractionPrompt.text = "Chưa có người dân trên thuyền";
                        txtInteractionPrompt.color = new Color(0.72f, 0.75f, 0.71f);
                    }
                    else if (!speedOk)
                    {
                        txtInteractionPrompt.text = "Dừng thuyền lại để thả người";
                        txtInteractionPrompt.color = new Color(0.82f, 0.58f, 0.45f);
                    }
                    else
                    {
                        txtInteractionPrompt.text = "Nhấn E để thả người";
                        txtInteractionPrompt.color = new Color(0.94f, 0.91f, 0.82f);
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
            SetPromptVisible(false);
        }
    }

    private void SetPromptVisible(bool visible)
    {
        if (txtInteractionPrompt == null) return;
        var root = txtInteractionPrompt.transform.parent;
        if (root != null && root.name == "PNL_R2_InteractionPrompt")
        {
            if (!txtInteractionPrompt.gameObject.activeSelf)
                txtInteractionPrompt.gameObject.SetActive(true);
            root.gameObject.SetActive(visible);
        }
        else
            txtInteractionPrompt.gameObject.SetActive(visible);
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
                roundController.ShowFeedback("Thuyền đã đầy, hãy đưa người dân đến điểm trú.");
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
                roundController.SetObjective("Mục tiêu: Đi cứu người dân.");
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
                roundController.SetObjective("Mục tiêu: Tiếp tục cứu người dân.");
            }
            else
            {
                // TriggerWin is called by DeliverCargo if all safe
                roundController.SetObjective("Hoàn thành cứu hộ!");
            }
        }
    }
}
