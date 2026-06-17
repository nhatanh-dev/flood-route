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

            if (result == Round1Result.Won)
            {
                RefreshResultHud(
                    $"RESCUE COMPLETE!    SAVED: {rescueController.Saved}/{rescueController.TotalCivilians}    TURNS LEFT: {remainingTurns}");
                return;
            }

            if (result == Round1Result.Lost)
            {
                RefreshResultHud(
                    $"RESCUE FAILED    SAVED: {rescueController.Saved}/{rescueController.TotalCivilians}    TURNS LEFT: 0");
                return;
            }

            RefreshPlayingHud(remainingTurns);
        }

        private void RefreshPlayingHud(int remainingTurns)
        {
            statusText.text =
                $"TURNS LEFT: {remainingTurns}    CARGO: {rescueController.Cargo}/{rescueController.CargoCapacity}    SAVED: {rescueController.Saved}/{rescueController.TotalCivilians}\n" +
                "MOVE: WASD / ARROWS    WAIT: Q";
        }

        private void RefreshResultHud(string resultLine)
        {
            statusText.text = resultLine + "\nPRESS R TO RETRY";
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
