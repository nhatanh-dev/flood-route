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
        public string currentObjectiveText = "Đi cứu Nhà cần cứu 1.";

        [Header("Damage Settings")]
        public bool enableCollisionDamage = true;
        public int collisionDamage = 1;
        public float minDamageSpeed = 1.5f;
        public float damageCooldown = 1.0f;
        public float collisionDamageFeedbackDuration = 1.2f;

        private float lastDamageTime = -999f;
        private bool isGameOver = false;
        public bool IsGameOver => isGameOver;

        private Round1FirstPersonInteraction interactionScript;

        private void Start()
        {
            currentTimeRemaining = roundTimeSeconds;
            currentBoatDurability = maxBoatDurability;
            interactionScript = FindAnyObjectByType<Round1FirstPersonInteraction>();
        }

        private void Update()
        {
            if (isGameOver) return;

            if (currentTimeRemaining > 0)
            {
                currentTimeRemaining -= Time.deltaTime;
                if (currentTimeRemaining <= 0)
                {
                    currentTimeRemaining = 0;
                    TriggerFail("Hết thời gian!");
                }
            }
        }

        public void ApplyBoatDamage(int amount, string reason)
        {
            if (!enableCollisionDamage || isGameOver) return;

            if (Time.time - lastDamageTime < damageCooldown) return;

            lastDamageTime = Time.time;
            currentBoatDurability -= amount;
            if (currentBoatDurability < 0) currentBoatDurability = 0;

            if (interactionScript != null)
            {
                interactionScript.ShowFeedback($"Va chạm mạnh! Độ bền thuyền -{amount}.");
            }

            if (currentBoatDurability <= 0)
            {
                TriggerFail("Thuyền bị hỏng!");
            }
        }

        private void TriggerFail(string failReason)
        {
            if (isGameOver) return;
            isGameOver = true;

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
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            var boat = FindAnyObjectByType<Round1FirstPersonBoatController>();
            if (boat != null) boat.enabled = false;

            // Hide the red boundary warning canvas so it doesn't block the fail screen
            var warningUI = FindAnyObjectByType<Round1BoundaryWarningUI>();
            if (warningUI != null) warningUI.enabled = false;
            
            var warningCanvas = GameObject.Find("BoundaryWarningCanvas");
            if (warningCanvas != null) warningCanvas.SetActive(false);

            var refs = FindAnyObjectByType<Round1SceneReferences>();
            if (refs != null && refs.winLosePanel != null)
            {
                var canvas = refs.winLosePanel.GetComponentInParent<Canvas>(true);
                if (canvas != null) 
                {
                    canvas.gameObject.SetActive(true);
                    // Hide old turn-based UI elements that share this canvas
                    foreach (UnityEngine.Transform child in canvas.transform)
                    {
                        if (child != refs.winLosePanel.transform)
                        {
                            child.gameObject.SetActive(false);
                        }
                    }
                }

                refs.winLosePanel.SetActive(true);
                refs.winLosePanel.transform.SetAsLastSibling();

                if (refs.winLoseTitle != null) 
                {
                    refs.winLoseTitle.gameObject.SetActive(true);
                    refs.winLoseTitle.text = "THẤT BẠI";
                }
                if (refs.winLoseSub != null) 
                {
                    refs.winLoseSub.gameObject.SetActive(true);
                    refs.winLoseSub.text = failReason;
                }

                var btn = refs.winLosePanel.GetComponentInChildren<UnityEngine.UI.Button>(true);
                if (btn != null)
                {
                    btn.gameObject.SetActive(true);
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => {
                        Time.timeScale = 1f;
                        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
                    });
                }
            }
        }
    }
}
