using TMPro;
using UnityEngine;

namespace Round1
{
    public sealed class Round1HudController : MonoBehaviour
    {
        [SerializeField] private Round1SceneReferences sceneReferences;
        [SerializeField] private Round1TurnController turnController;
        [SerializeField] private Round1RescueController rescueController;
        [SerializeField] private Round1GameStateController gameStateController;

        private TMP_Text statusText;

        private void Awake()
        {
            // In the First-Person prototype scene, the Round1FirstPersonInteraction handles all HUD
            // updates directly using the shared named TMP objects. Disable this controller to avoid conflict.
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("FirstPerson"))
            {
                enabled = false;
                return;
            }
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();
            Subscribe();
            UpdateHud();
        }

        private void Start()
        {
            EnsureReferences();
            UpdateHud();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void HandleTurnChanged(int currentTurn)
        {
            UpdateHud();
        }

        private void HandleRescueStateChanged()
        {
            UpdateHud();
        }

        private void HandleResultChanged(Round1Result result)
        {
            UpdateHud();
        }

        private void HandleRoundReset()
        {
            UpdateHud();
        }

        private void UpdateHud()
        {
            EnsureReferences();

            if (statusText == null || turnController == null || rescueController == null)
            {
                return;
            }

            Round1Result result = gameStateController != null
                ? gameStateController.Result
                : Round1Result.Playing;

            int remainingTurns = Mathf.Max(0, turnController.MaxTurns - turnController.CurrentTurn);

            if (sceneReferences != null && sceneReferences.winLosePanel != null)
            {
                sceneReferences.winLosePanel.SetActive(result != Round1Result.Playing);
            }

            if (result == Round1Result.Won)
            {
                if (sceneReferences != null && sceneReferences.winLoseTitle != null)
                {
                    sceneReferences.winLoseTitle.text = "HOÀN THÀNH!";
                    sceneReferences.winLoseSub.text = "Đã đưa người dân tới điểm trú.";
                }
                RefreshResultHud(remainingTurns, true);
                return;
            }

            if (result == Round1Result.Lost)
            {
                if (sceneReferences != null && sceneReferences.winLoseTitle != null)
                {
                    sceneReferences.winLoseTitle.text = "HẾT LƯỢT!";
                    sceneReferences.winLoseSub.text = "Hãy thử lại.";
                }
                RefreshResultHud(remainingTurns, false);
                return;
            }

            RefreshPlayingHud(remainingTurns);
        }

        private void RefreshPlayingHud(int remainingTurns)
        {
            var boat = FindAnyObjectByType<Round1BoatController>();
            bool atBenPhu = boat != null && boat.CurrentNode == Round1NodeId.BenPhu;
            var debris = FindAnyObjectByType<Round1DebrisController>();
            bool debrisBlocked = debris != null && debris.IsRouteBlocked;
            
            bool isFP = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("FirstPerson");
            
            string objective = "Cứu 3 người ở các nhà ngập nước.";
            if (rescueController.Cargo > 0 || rescueController.Saved > 0)
            {
                objective = "Đưa người dân tới Điểm trú.";
            }
            if (!isFP && atBenPhu && debrisBlocked)
            {
                objective = "Nhấn Q để chờ nước cuốn rác trôi.";
            }
            
            if (isFP)
            {
                statusText.text =
                    $"Lượt còn lại: Không giới hạn    Trên thuyền: {rescueController.Cargo}/{rescueController.CargoCapacity}    Đã cứu: {rescueController.Saved}/{rescueController.TotalCivilians}\n" +
                    $"Nhiệm vụ: {objective}\n" +
                    (!string.IsNullOrEmpty(interactionPrompt) ? $"{interactionPrompt}\n" : "") +
                    "Điều khiển: WASD để lái. Chuột để nhìn.";
            }
            else
            {
                statusText.text =
                    $"Lượt còn lại: {remainingTurns}    Trên thuyền: {rescueController.Cargo}/{rescueController.CargoCapacity}    Đã cứu: {rescueController.Saved}/{rescueController.TotalCivilians}\n" +
                    $"Nhiệm vụ: {objective}\n" +
                    "Điều khiển: Click điểm kế bên để đi. Nhấn Q để chờ.";
            }
        }
        
        private string interactionPrompt = "";
        public void SetInteractionPrompt(string prompt)
        {
            interactionPrompt = prompt;
            UpdateHud();
        }

        private void RefreshResultHud(int remainingTurns, bool isWin)
        {
            statusText.text = $"Lượt còn lại: {remainingTurns}    Trên thuyền: {rescueController.Cargo}/{rescueController.CargoCapacity}    Đã cứu: {rescueController.Saved}/{rescueController.TotalCivilians}\n" +
                $"Kết quả: {(isWin ? "Hoàn thành! Đã đưa người dân tới điểm trú." : "Hết lượt! Hãy thử lại.")}\nPRESS R TO RETRY";
        }

        private void Subscribe()
        {
            if (turnController != null)
            {
                turnController.TurnChanged += HandleTurnChanged;
                turnController.RoundReset += HandleRoundReset;
            }

            if (rescueController != null)
            {
                rescueController.RescueStateChanged += HandleRescueStateChanged;
            }

            if (gameStateController != null)
            {
                gameStateController.ResultChanged += HandleResultChanged;
            }
        }

        private void Unsubscribe()
        {
            if (turnController != null)
            {
                turnController.TurnChanged -= HandleTurnChanged;
                turnController.RoundReset -= HandleRoundReset;
            }

            if (rescueController != null)
            {
                rescueController.RescueStateChanged -= HandleRescueStateChanged;
            }

            if (gameStateController != null)
            {
                gameStateController.ResultChanged -= HandleResultChanged;
            }
        }

        private void EnsureReferences()
        {
            sceneReferences ??= FindAnyObjectByType<Round1SceneReferences>();
            turnController ??= FindAnyObjectByType<Round1TurnController>();
            rescueController ??= FindAnyObjectByType<Round1RescueController>();
            gameStateController ??= FindAnyObjectByType<Round1GameStateController>();
            statusText ??= sceneReferences != null ? sceneReferences.statusText : null;
        }
    }
}
