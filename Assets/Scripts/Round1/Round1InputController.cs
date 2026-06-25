using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Round1
{
    public sealed class Round1InputController : MonoBehaviour
    {
        [SerializeField] private Round1TurnController turnController;
        [SerializeField] private Round1BoatController boatController;
        [SerializeField] private Round1NodeGraph nodeGraph;
        [SerializeField] private Round1DebrisController debrisController;
        [SerializeField] private Round1GameStateController gameStateController;
        [SerializeField] private Round1IntroController introController;
        [SerializeField] private float orthogonalTolerance = 0.05f;

        private void Awake()
        {
            EnsureReferences();
        }

        private void Update()
        {
            EnsureReferences();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (introController != null && introController.IsIntroActive)
            {
                return;
            }

            if (gameStateController != null && gameStateController.IsRoundFinished)
            {
                if (keyboard.rKey.wasPressedThisFrame)
                {
                    gameStateController.RequestRetry();
                }

                return;
            }

            if (ShouldIgnoreInput())
            {
                return;
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                if (turnController.TryRequestWait())
                {
                    if (boatController.CurrentNode == Round1NodeId.BenPhu)
                    {
                        ShowFeedback("Đã chờ nước cuốn rác trôi.");
                    }
                    else
                    {
                        ShowFeedback("Đã chờ 1 lượt.");
                    }
                }
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                HandleMouseClick(mouse.position.ReadValue());
                return;
            }
        }

        private void HandleMouseClick(Vector2 mousePosition)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;
            
            Ray ray = mainCam.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Transform hitTransform = hit.collider.transform;
                Round1NodeId? clickedNodeId = nodeGraph.GetNodeIdFromTransform(hitTransform);
                if (clickedNodeId.HasValue)
                {
                    Round1NodeId currentNode = boatController.CurrentNode;
                    if (clickedNodeId.Value == currentNode) return;
                    
                    if (nodeGraph.AreAdjacent(currentNode, clickedNodeId.Value))
                    {
                        if (nodeGraph.CanTraverse(currentNode, clickedNodeId.Value))
                        {
                            turnController.TryRequestMove(clickedNodeId.Value);
                        }
                        else
                        {
                            ShowFeedback("Đường đang bị chặn.");
                        }
                    }
                    else
                    {
                        ShowFeedback("Chỉ có thể đi tới node kề.");
                    }
                }
            }
        }
        
        private void ShowFeedback(string msg)
        {
            var sceneRefs = FindAnyObjectByType<Round1SceneReferences>();
            if (sceneRefs != null && sceneRefs.feedbackText != null)
            {
                sceneRefs.feedbackText.text = msg;
                StartCoroutine(ClearFeedbackRoutine(sceneRefs.feedbackText));
            }
        }
        
        private System.Collections.IEnumerator ClearFeedbackRoutine(TMPro.TMP_Text text)
        {
            yield return new WaitForSeconds(2f);
            if (text != null) text.text = "";
        }

        private bool ShouldIgnoreInput()
        {
            return turnController == null
                || boatController == null
                || nodeGraph == null
                || debrisController == null
                || (introController != null && introController.IsIntroActive)
                || (gameStateController != null && gameStateController.IsRoundFinished)
                || turnController.IsBusy
                || boatController.IsMoving
                || debrisController.IsMoving
                || !turnController.HasTurnsRemaining;
        }

        private void TryMoveInDirection(Vector3 direction)
        {
            Round1NodeId currentNode = boatController.CurrentNode;
            Transform currentTransform = nodeGraph.GetNodeTransform(currentNode);
            if (currentTransform == null)
            {
                return;
            }

            IReadOnlyList<Round1NodeId> adjacentNodes = nodeGraph.GetAdjacentNodeIds(currentNode);
            for (int i = 0; i < adjacentNodes.Count; i++)
            {
                Round1NodeId candidateNode = adjacentNodes[i];
                Transform candidateTransform = nodeGraph.GetNodeTransform(candidateNode);
                if (candidateTransform == null)
                {
                    continue;
                }

                Vector3 delta = candidateTransform.position - currentTransform.position;
                if (IsInRequestedDirection(delta, direction))
                {
                    turnController.TryRequestMove(candidateNode);
                    return;
                }
            }
        }

        private bool IsInRequestedDirection(Vector3 delta, Vector3 direction)
        {
            float tolerance = Mathf.Max(0f, orthogonalTolerance);

            if (direction == Vector3.forward)
            {
                return delta.z > tolerance && Mathf.Abs(delta.x) <= tolerance;
            }

            if (direction == Vector3.back)
            {
                return delta.z < -tolerance && Mathf.Abs(delta.x) <= tolerance;
            }

            if (direction == Vector3.right)
            {
                return delta.x > tolerance && Mathf.Abs(delta.z) <= tolerance;
            }

            return delta.x < -tolerance && Mathf.Abs(delta.z) <= tolerance;
        }

        private void EnsureReferences()
        {
            turnController ??= FindAnyObjectByType<Round1TurnController>();
            boatController ??= FindAnyObjectByType<Round1BoatController>();
            nodeGraph ??= FindAnyObjectByType<Round1NodeGraph>();
            debrisController ??= FindAnyObjectByType<Round1DebrisController>();
            gameStateController ??= FindAnyObjectByType<Round1GameStateController>();
            introController ??= FindAnyObjectByType<Round1IntroController>();
        }
    }
}
