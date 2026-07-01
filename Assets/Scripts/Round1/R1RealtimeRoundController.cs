using UnityEngine;

namespace Round1
{
    public class R1RealtimeRoundController : MonoBehaviour
    {
        [Header("Round Timer")]
        public float roundTimeSeconds = 180f;
        public float currentTimeRemaining = 180f;

        [Header("Boat Durability")]
        public int maxBoatDurability = 3;
        public int currentBoatDurability = 3;

        [Header("Boat Cargo")]
        public int boatCapacity = 3;
        public int currentCargo = 0;

        [Header("Civilians Safe")]
        public int totalCivilians = 3;
        public int civiliansSafe = 0;

        [Header("Objectives")]
        public string currentObjectiveText = "Mục tiêu: Tìm nhà có tín hiệu cầu cứu.";

        [Header("Rescue State")]
        public bool rescuedA = false;
        public bool rescuedB = false;
        public bool finished = false;

        public bool TryRescueA(int amount)
        {
            if (rescuedA || currentCargo + amount > boatCapacity) return false;
            rescuedA = true;
            currentCargo += amount;
            // Changed from specific house to general find objective if there's still space and people
            currentObjectiveText = "Mục tiêu: Tiếp tục tìm người mắc kẹt.";
            return true;
        }

        public bool TryRescueB(int amount)
        {
            if (rescuedB || currentCargo + amount > boatCapacity) return false;
            rescuedB = true;
            currentCargo += amount;
            currentObjectiveText = "Mục tiêu: Đưa người dân đến điểm trú.";
            return true;
        }

        public bool TryDropOff()
        {
            if (currentCargo <= 0) return false;
            civiliansSafe += currentCargo;
            currentCargo = 0;
            if (rescuedA && rescuedB)
            {
                finished = true;
                currentObjectiveText = "An toàn.";
                TriggerWin();
            }
            else
            {
                currentObjectiveText = "Mục tiêu: Tiếp tục tìm người mắc kẹt.";
            }
            return true;
        }

        [Header("Damage Settings")]
        public float collisionCooldown = 1f;
        private float lastCollisionTime = -1f;
        public AudioClip collisionSound;
        public bool enableCollisionDamage = true;
        public int collisionDamage = 1;
        public float minDamageSpeed = 1.5f;
        public float damageCooldown = 1.0f;
        public float collisionDamageFeedbackDuration = 1.2f;

        public enum Round1EndState
        {
            Playing,
            Win,
            Fail
        }

        private float lastDamageTime = -999f;
        public float LastDamageTime => lastDamageTime;
        private Round1EndState endState = Round1EndState.Playing;
        public bool IsGameOver => endState != Round1EndState.Playing;

        private Round1FirstPersonInteraction interactionScript;

        private void Start()
        {
            currentTimeRemaining = roundTimeSeconds;
            currentBoatDurability = maxBoatDurability;
            currentObjectiveText = "Mục tiêu: Tìm nhà có tín hiệu cầu cứu."; // Ensure it's set correctly
            interactionScript = FindAnyObjectByType<Round1FirstPersonInteraction>();
        }

        private void Update()
        {
            if (endState != Round1EndState.Playing)
            {
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
                {
                    Time.timeScale = 1f;
                    UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
                }
                return;
            }

            if (currentTimeRemaining > 0)
            {
                currentTimeRemaining -= Time.deltaTime;
                if (currentTimeRemaining <= 0)
                {
                    currentTimeRemaining = 0f;
                    TriggerFail("Hết thời gian!", "Hết thời gian cứu hộ.");
                }
            }
        }

        public void ApplyBoatDamage(int amount, string reason)
        {
            if (!enableCollisionDamage || endState != Round1EndState.Playing) return;

            if (Time.time - lastDamageTime < damageCooldown) return;

            lastDamageTime = Time.time;
            currentBoatDurability -= amount;
            if (currentBoatDurability < 0) currentBoatDurability = 0;

            if (interactionScript != null)
            {
                interactionScript.ShowFeedback($"Va chạm mạnh! Độ bền -{amount}.");
            }
            if (collisionSound != null)
            {
                var cam = UnityEngine.Camera.main;
                UnityEngine.AudioSource.PlayClipAtPoint(collisionSound, cam != null ? cam.transform.position : transform.position, 1.0f);
            }

            if (currentBoatDurability <= 0)
            {
                currentBoatDurability = 0;
                TriggerFail("Thuyền đã bị hỏng!", "Thuyền đã bị hỏng trong quá trình cứu hộ.");
            }
        }

        private void TriggerFail(string failReason, string failDetail)
        {
            if (endState != Round1EndState.Playing) return;
            endState = Round1EndState.Fail;

            // Force HUD update one last time before locking, then hide it
            if (interactionScript != null)
            {
                var method = interactionScript.GetType().GetMethod("RefreshHUD", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null) method.Invoke(interactionScript, null);
                
                var hideMethod = interactionScript.GetType().GetMethod("HideGameplayHUD", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (hideMethod != null) hideMethod.Invoke(interactionScript, null);
            }

            Time.timeScale = 0f;
            
            // Unlock Cursor so player can click Retry
            // Unlock Cursor so player can click Retry
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            var boat = FindAnyObjectByType<Round1FirstPersonBoatController>();
            if (boat != null) boat.enabled = false;

            // Hide the red boundary warning canvas so it doesn't block the fail screen
            var warningUI = FindAnyObjectByType<Round1BoundaryWarningUI>();
            if (warningUI != null) warningUI.enabled = false;
            
            var warningCanvas = GameObject.Find("BoundaryWarningCanvas");
            if (warningCanvas != null) warningCanvas.SetActive(false);

            var endgameUI = FindAnyObjectByType<Round1EndgameUIController>();
            if (endgameUI != null)
            {
                endgameUI.ShowLose(failReason, failDetail);
            }
        }

        private void TriggerWin()
        {
            if (endState != Round1EndState.Playing) return;
            endState = Round1EndState.Win;

            if (interactionScript != null)
            {
                var method = interactionScript.GetType().GetMethod("RefreshHUD", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null) method.Invoke(interactionScript, null);
                
                var hideMethod = interactionScript.GetType().GetMethod("HideGameplayHUD", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (hideMethod != null) hideMethod.Invoke(interactionScript, null);
            }

            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            var boat = FindAnyObjectByType<Round1FirstPersonBoatController>();
            if (boat != null) boat.enabled = false;

            var endgameUI = FindAnyObjectByType<Round1EndgameUIController>();
            if (endgameUI != null)
            {
                endgameUI.ShowWin();
            }
        }
    }
}
