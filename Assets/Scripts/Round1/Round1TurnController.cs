using System;
using System.Collections;
using UnityEngine;

namespace Round1
{
    public sealed class Round1TurnController : MonoBehaviour
    {
        [SerializeField] private Round1BoatController boatController;
        [SerializeField] private Round1DebrisController debrisController;
        [SerializeField] private Round1RescueController rescueController;
        [SerializeField] private Round1GameStateController gameStateController;
        [SerializeField] private int maxTurns = 9;

        private bool actionLocked;
        private Coroutine actionCoroutine;

        public int CurrentTurn { get; private set; }
        public int MaxTurns => maxTurns;
        public bool IsBusy => actionLocked
            || actionCoroutine != null
            || (boatController != null && boatController.IsMoving)
            || (debrisController != null && debrisController.IsMoving);
        public bool HasTurnsRemaining => CurrentTurn < MaxTurns;

        public event Action<int> TurnChanged;
        public event Action ActionCompleted;
        public event Action RoundReset;

        private void Awake()
        {
            InitializeRound();
        }

        private void OnDisable()
        {
            if (actionCoroutine != null)
            {
                StopCoroutine(actionCoroutine);
                actionCoroutine = null;
            }

            actionLocked = false;
        }

        public bool TryRequestMove(Round1NodeId targetNode)
        {
            if (!CanAcceptAction())
            {
                return false;
            }

            actionLocked = true;
            if (!boatController.TryMoveTo(targetNode))
            {
                actionLocked = false;
                return false;
            }

            actionCoroutine = StartCoroutine(CompleteMoveAction());
            return true;
        }

        public bool TryRequestWait()
        {
            if (!CanAcceptAction()
                || boatController.CurrentNode != Round1NodeId.BenPhu
                || !debrisController.IsRouteBlocked)
            {
                return false;
            }

            actionLocked = true;
            AdvanceTurnAndApplyDebris();
            actionCoroutine = StartCoroutine(WaitForDebrisThenUnlock());
            return true;
        }

        public void ResetRound()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnsureReferences();

            if (actionCoroutine != null)
            {
                StopCoroutine(actionCoroutine);
                actionCoroutine = null;
            }

            actionLocked = false;
            CurrentTurn = 0;
            boatController?.ResetToNodeForTesting(Round1NodeId.Base);
            rescueController?.ResetRescueState();
            TurnChanged?.Invoke(CurrentTurn);
            debrisController?.ApplyTurnState(0);
            RoundReset?.Invoke();
        }

        [ContextMenu("Reset Round To Turn 0")]
        private void ResetRoundToTurn0()
        {
            ResetRound();
        }

        [ContextMenu("Test Valid WAIT At BenPhu")]
        private void TestValidWaitAtBenPhu()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnsureReferences();

            if (actionCoroutine != null)
            {
                StopCoroutine(actionCoroutine);
                actionCoroutine = null;
            }

            actionCoroutine = StartCoroutine(TestValidWaitAtBenPhuRoutine());
        }

        private IEnumerator TestValidWaitAtBenPhuRoutine()
        {
            actionLocked = false;
            boatController.ResetToNodeForTesting(Round1NodeId.BenPhu);
            CurrentTurn = 2;
            TurnChanged?.Invoke(CurrentTurn);
            debrisController.ApplyTurnState(2);

            while (debrisController != null && debrisController.IsMoving)
            {
                yield return null;
            }

            actionCoroutine = null;
            TryRequestWait();
        }

        private void InitializeRound()
        {
            EnsureReferences();
            CurrentTurn = 0;
            actionLocked = false;
            debrisController?.ApplyTurnState(0);
        }

        private bool CanAcceptAction()
        {
            EnsureReferences();
            return Application.isPlaying
                && !IsBusy
                && HasTurnsRemaining
                && (gameStateController == null || !gameStateController.IsRoundFinished)
                && boatController != null
                && debrisController != null;
        }

        private IEnumerator CompleteMoveAction()
        {
            while (boatController != null && boatController.IsMoving)
            {
                yield return null;
            }

            AdvanceTurnAndApplyDebris();

            while (debrisController != null && debrisController.IsMoving)
            {
                yield return null;
            }

            actionCoroutine = null;
            ActionCompleted?.Invoke();
            actionLocked = false;
        }

        private IEnumerator WaitForDebrisThenUnlock()
        {
            while (debrisController != null && debrisController.IsMoving)
            {
                yield return null;
            }

            actionCoroutine = null;
            ActionCompleted?.Invoke();
            actionLocked = false;
        }

        private void AdvanceTurnAndApplyDebris()
        {
            CurrentTurn += 1;
            TurnChanged?.Invoke(CurrentTurn);
            debrisController.ApplyTurnState(CurrentTurn);
        }

        private void EnsureReferences()
        {
            boatController ??= FindAnyObjectByType<Round1BoatController>();
            debrisController ??= FindAnyObjectByType<Round1DebrisController>();
            rescueController ??= FindAnyObjectByType<Round1RescueController>();
            gameStateController ??= FindAnyObjectByType<Round1GameStateController>();
        }
    }
}
