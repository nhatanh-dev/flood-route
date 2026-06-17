using System;
using System.Collections;
using UnityEngine;

namespace Round1
{
    public sealed class Round1BoatController : MonoBehaviour
    {
        [SerializeField] private Round1SceneReferences sceneReferences;
        [SerializeField] private Round1NodeGraph nodeGraph;
        [SerializeField] private float moveDuration = 0.85f;
        [SerializeField] private Vector3 boatPositionOffset = new(0.6f, -0.1f, 0.6f);

        private Coroutine movementCoroutine;

        public Round1NodeId CurrentNode { get; private set; } = Round1NodeId.Base;
        public bool IsMoving => movementCoroutine != null;

        public event Action MovementStarted;
        public event Action<Round1NodeId> ArrivedAtNode;

        private void Awake()
        {
            Initialize();
        }

        private void OnDisable()
        {
            StopActiveMovement();
        }

        public bool TryMoveTo(Round1NodeId targetNode)
        {
            if (!Application.isPlaying || IsMoving)
            {
                return false;
            }

            EnsureReferences();

            if (nodeGraph == null || !nodeGraph.AreAdjacent(CurrentNode, targetNode))
            {
                return false;
            }

            if (!nodeGraph.CanTraverse(CurrentNode, targetNode))
            {
                return false;
            }

            Transform targetTransform = nodeGraph.GetNodeTransform(targetNode);
            Transform boatRoot = sceneReferences != null ? sceneReferences.playerBoatRoot : null;
            if (targetTransform == null || boatRoot == null)
            {
                return false;
            }

            movementCoroutine = StartCoroutine(MoveBoatRoutine(boatRoot, targetTransform, targetNode));
            MovementStarted?.Invoke();
            return true;
        }

        [ContextMenu("Reset To Base")]
        private void ResetToBaseContext()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ResetToNodeForTesting(Round1NodeId.Base);
        }

        [ContextMenu("Test Base To Kenh")]
        private void TestBaseToKenh()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ResetToNodeForTesting(Round1NodeId.Base);
            TryMoveTo(Round1NodeId.Kenh);
        }

        [ContextMenu("Test Kenh To Cho")]
        private void TestKenhToCho()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ResetToNodeForTesting(Round1NodeId.Kenh);
            TryMoveTo(Round1NodeId.Cho);
        }

        [ContextMenu("Test BenPhu To CauTre")]
        private void TestBenPhuToCauTre()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ResetToNodeForTesting(Round1NodeId.BenPhu);
            TryMoveTo(Round1NodeId.CauTre);
        }

        private void Initialize()
        {
            EnsureReferences();
            CurrentNode = Round1NodeId.Base;
            SnapBoatTo(Round1NodeId.Base);
        }

        private void EnsureReferences()
        {
            sceneReferences ??= FindAnyObjectByType<Round1SceneReferences>();
            nodeGraph ??= FindAnyObjectByType<Round1NodeGraph>();
            nodeGraph?.Initialize();
        }

        private IEnumerator MoveBoatRoutine(Transform boatRoot, Transform targetTransform, Round1NodeId targetNode)
        {
            Vector3 startPosition = boatRoot.position;
            Vector3 targetPosition = targetTransform.position + boatPositionOffset;
            float duration = Mathf.Max(0.01f, moveDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                boatRoot.position = Vector3.LerpUnclamped(startPosition, targetPosition, t);
                yield return null;
            }

            boatRoot.position = targetPosition;
            CurrentNode = targetNode;
            movementCoroutine = null;
            ArrivedAtNode?.Invoke(CurrentNode);
        }

        public void ResetToNodeForTesting(Round1NodeId nodeId)
        {
            StopActiveMovement();
            CurrentNode = nodeId;
            SnapBoatTo(nodeId);
        }

        private void SnapBoatTo(Round1NodeId nodeId)
        {
            EnsureReferences();

            Transform nodeTransform = nodeGraph != null ? nodeGraph.GetNodeTransform(nodeId) : null;
            Transform boatRoot = sceneReferences != null ? sceneReferences.playerBoatRoot : null;
            if (nodeTransform == null || boatRoot == null)
            {
                return;
            }

            boatRoot.position = nodeTransform.position + boatPositionOffset;
            boatRoot.rotation = Quaternion.identity;
        }

        private void StopActiveMovement()
        {
            if (movementCoroutine == null)
            {
                return;
            }

            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
    }
}
