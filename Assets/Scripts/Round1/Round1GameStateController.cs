using System;
using UnityEngine;

namespace Round1
{
    public enum Round1Result
    {
        Playing,
        Won,
        Lost
    }

    public sealed class Round1GameStateController : MonoBehaviour
    {
        [SerializeField] private Round1TurnController turnController;
        [SerializeField] private Round1RescueController rescueController;

        public Round1Result Result { get; private set; } = Round1Result.Playing;
        public bool IsRoundFinished => Result != Round1Result.Playing;
        public bool HasWon => Result == Round1Result.Won;
        public bool HasLost => Result == Round1Result.Lost;

        public event Action<Round1Result> ResultChanged;

        private void Awake()
        {
            EnsureReferences();
            Result = Round1Result.Playing;
        }

        private void OnEnable()
        {
            EnsureReferences();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void RequestRetry()
        {
            if (!Application.isPlaying || !IsRoundFinished)
            {
                return;
            }

            SetResult(Round1Result.Playing);
            turnController?.ResetRound();
        }

        private void HandleRescueStateChanged()
        {
            EvaluateWin();
        }

        private void HandleTurnChanged(int currentTurn)
        {
            EvaluateWin();
        }

        private void HandleActionCompleted()
        {
            EvaluateRoundEnd();
        }

        private void HandleRoundReset()
        {
            SetResult(Round1Result.Playing);
        }

        private void EvaluateRoundEnd()
        {
            if (EvaluateWin())
            {
                return;
            }

            if (turnController == null || rescueController == null)
            {
                return;
            }

            if (turnController.CurrentTurn >= turnController.MaxTurns
                && rescueController.Saved < rescueController.TotalCivilians)
            {
                SetResult(Round1Result.Lost);
            }
        }

        private bool EvaluateWin()
        {
            if (rescueController == null)
            {
                return false;
            }

            if (rescueController.Saved >= rescueController.TotalCivilians)
            {
                SetResult(Round1Result.Won);
                return true;
            }

            return false;
        }

        private void SetResult(Round1Result result)
        {
            if (Result == result)
            {
                return;
            }

            Result = result;
            ResultChanged?.Invoke(Result);
        }

        private void Subscribe()
        {
            if (turnController != null)
            {
                turnController.TurnChanged += HandleTurnChanged;
                turnController.ActionCompleted += HandleActionCompleted;
                turnController.RoundReset += HandleRoundReset;
            }

            if (rescueController != null)
            {
                rescueController.RescueStateChanged += HandleRescueStateChanged;
            }
        }

        private void Unsubscribe()
        {
            if (turnController != null)
            {
                turnController.TurnChanged -= HandleTurnChanged;
                turnController.ActionCompleted -= HandleActionCompleted;
                turnController.RoundReset -= HandleRoundReset;
            }

            if (rescueController != null)
            {
                rescueController.RescueStateChanged -= HandleRescueStateChanged;
            }
        }

        private void EnsureReferences()
        {
            turnController ??= FindAnyObjectByType<Round1TurnController>();
            rescueController ??= FindAnyObjectByType<Round1RescueController>();
        }
    }
}
