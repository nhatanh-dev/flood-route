using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Round1
{
    public sealed class Round1SharedFrameworkAdapter : MonoBehaviour
    {
        [Serializable]
        private sealed class NodeVisualBinding
        {
            public Round1NodeId nodeId;
            public Transform markerRoot;
            public Renderer[] renderers;
            public Vector3 baseScale = Vector3.one;
        }

        [Serializable]
        private sealed class EdgeVisualBinding
        {
            public Round1NodeId from;
            public Round1NodeId to;
            public LineRenderer lineRenderer;
        }

        [Header("Round 1 Sources")]
        [SerializeField] private Round1SceneReferences sceneReferences;
        [SerializeField] private Round1NodeGraph nodeGraph;
        [SerializeField] private Round1BoatController boatController;
        [SerializeField] private Round1TurnController turnController;
        [SerializeField] private Round1RescueController rescueController;
        [SerializeField] private Round1GameStateController gameStateController;

        [Header("Shared UI")]
        [SerializeField] private RoundUIController sharedUi;
        [SerializeField] private RoundCompletionController completionController;
        [SerializeField] private GameObject legacyHudToDisable;

        [Header("Objective Markers")]
        [SerializeField] private RescueObjectiveCounter rescueCounterNhaBa;
        [SerializeField] private RescueObjectiveCounter rescueCounterNhaTu;
        [SerializeField] private GameObject nhaBaMarkerRoot;
        [SerializeField] private GameObject nhaTuMarkerRoot;
        [SerializeField] private GameObject shelterMarkerRoot;
        [SerializeField] private GameObject nhaBaPeopleRoot;
        [SerializeField] private GameObject nhaTuPeopleRoot;

        [Header("Visual Bindings")]
        [SerializeField] private NodeVisualBinding[] nodeVisuals = Array.Empty<NodeVisualBinding>();
        [SerializeField] private EdgeVisualBinding[] edgeVisuals = Array.Empty<EdgeVisualBinding>();

        [Header("Materials")]
        [SerializeField] private Material normalNodeMaterial;
        [SerializeField] private Material currentNodeMaterial;
        [SerializeField] private Material availableNodeMaterial;
        [SerializeField] private Material rescueNodeMaterial;
        [SerializeField] private Material shelterNodeMaterial;
        [SerializeField] private Material completedNodeMaterial;
        [SerializeField] private Material normalRouteMaterial;
        [SerializeField] private Material availableRouteMaterial;
        [SerializeField] private Material blockedRouteMaterial;

        [Header("Tuning")]
        [SerializeField] private float normalNodeScale = 0.7f;
        [SerializeField] private float currentNodeScale = 0.95f;
        [SerializeField] private float availableNodeScale = 0.82f;
        [SerializeField] private float routeLineWidth = 0.065f;
        [SerializeField] private float availableRouteLineWidth = 0.1f;
        [SerializeField] private Color normalRouteColor = new(0.42f, 0.86f, 0.85f, 0.52f);
        [SerializeField] private Color availableRouteColor = new(0.76f, 0.96f, 0.95f, 0.86f);
        [SerializeField] private Color blockedRouteColor = new(0.9f, 0.32f, 0.16f, 0.86f);

        private readonly HashSet<Round1NodeId> availableNodes = new();

        private void Awake()
        {
            EnsureReferences();
            CacheBaseScales();
            if (legacyHudToDisable != null)
            {
                legacyHudToDisable.SetActive(false);
            }
        }

        private void OnEnable()
        {
            EnsureReferences();
            Subscribe();
            RefreshAll();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            RefreshRouteState();
        }

        public void RefreshAll()
        {
            RefreshCounters();
            RefreshRouteState();
            RefreshUi();
            RefreshCompletionPanel();
        }

        private void HandleTurnChanged(int currentTurn)
        {
            RefreshAll();
        }

        private void HandleRescueChanged()
        {
            RefreshAll();
        }

        private void HandleResultChanged(Round1Result result)
        {
            RefreshAll();
        }

        private void RefreshCounters()
        {
            if (rescueController == null)
            {
                return;
            }

            if (rescueCounterNhaBa != null)
            {
                int savedAtNhaBa = rescueController.Saved >= rescueController.TotalCivilians
                    ? 2
                    : Mathf.Clamp(rescueController.Saved, 0, 2);
                rescueCounterNhaBa.SetCounts(2, rescueController.RemainingAtNhaBa, savedAtNhaBa);
            }

            if (rescueCounterNhaTu != null)
            {
                int savedAtNhaTu = rescueController.Saved >= rescueController.TotalCivilians
                    ? 1
                    : Mathf.Clamp(rescueController.Saved - 2, 0, 1);
                rescueCounterNhaTu.SetCounts(1, rescueController.RemainingAtNhaTu, savedAtNhaTu);
            }

            SetActive(nhaBaMarkerRoot, rescueController.RemainingAtNhaBa > 0);
            SetActive(nhaTuMarkerRoot, rescueController.RemainingAtNhaTu > 0);
            SetActive(nhaBaPeopleRoot, rescueController.RemainingAtNhaBa > 0);
            SetActive(nhaTuPeopleRoot, rescueController.RemainingAtNhaTu > 0);
            SetActive(shelterMarkerRoot, rescueController.Cargo > 0 || rescueController.Saved < rescueController.TotalCivilians);
        }

        private void RefreshRouteState()
        {
            if (boatController == null || nodeGraph == null)
            {
                return;
            }

            availableNodes.Clear();
            IReadOnlyList<Round1NodeId> adjacent = nodeGraph.GetAdjacentNodeIds(boatController.CurrentNode);
            for (int i = 0; i < adjacent.Count; i++)
            {
                Round1NodeId node = adjacent[i];
                if (nodeGraph.CanTraverse(boatController.CurrentNode, node))
                {
                    availableNodes.Add(node);
                }
            }

            for (int i = 0; i < nodeVisuals.Length; i++)
            {
                ApplyNodeVisual(nodeVisuals[i]);
            }

            for (int i = 0; i < edgeVisuals.Length; i++)
            {
                ApplyEdgeVisual(edgeVisuals[i]);
            }
        }

        private void ApplyNodeVisual(NodeVisualBinding binding)
        {
            if (binding == null || binding.markerRoot == null)
            {
                return;
            }

            Material material = normalNodeMaterial;
            float scale = normalNodeScale;

            if (boatController != null && binding.nodeId == boatController.CurrentNode)
            {
                material = currentNodeMaterial != null ? currentNodeMaterial : material;
                scale = currentNodeScale;
            }
            else if (availableNodes.Contains(binding.nodeId))
            {
                material = availableNodeMaterial != null ? availableNodeMaterial : material;
                scale = availableNodeScale;
            }
            else if (IsRescueNode(binding.nodeId))
            {
                material = rescueNodeMaterial != null ? rescueNodeMaterial : material;
            }
            else if (IsShelterNode(binding.nodeId))
            {
                material = shelterNodeMaterial != null ? shelterNodeMaterial : material;
            }

            bool completedRescue = rescueController != null
                && (binding.nodeId == Round1NodeId.NhaBa && rescueController.RemainingAtNhaBa <= 0
                    || binding.nodeId == Round1NodeId.NhaTu && rescueController.RemainingAtNhaTu <= 0);
            if (completedRescue)
            {
                material = completedNodeMaterial != null ? completedNodeMaterial : material;
                scale = normalNodeScale * 0.92f;
            }

            binding.markerRoot.localScale = binding.baseScale * scale;
            ApplyMaterial(binding.renderers, material);
        }

        private void ApplyEdgeVisual(EdgeVisualBinding binding)
        {
            if (binding == null || binding.lineRenderer == null)
            {
                return;
            }

            bool isAvailable = boatController != null
                && (binding.from == boatController.CurrentNode && availableNodes.Contains(binding.to)
                    || binding.to == boatController.CurrentNode && availableNodes.Contains(binding.from));
            bool isBlocked = nodeGraph != null && !nodeGraph.CanTraverse(binding.from, binding.to);

            binding.lineRenderer.widthMultiplier = isAvailable ? availableRouteLineWidth : routeLineWidth;

            if (isBlocked)
            {
                binding.lineRenderer.startColor = blockedRouteColor;
                binding.lineRenderer.endColor = blockedRouteColor;
                if (blockedRouteMaterial != null)
                {
                    binding.lineRenderer.sharedMaterial = blockedRouteMaterial;
                }
            }
            else if (isAvailable)
            {
                binding.lineRenderer.startColor = availableRouteColor;
                binding.lineRenderer.endColor = availableRouteColor;
                if (availableRouteMaterial != null)
                {
                    binding.lineRenderer.sharedMaterial = availableRouteMaterial;
                }
            }
            else
            {
                binding.lineRenderer.startColor = normalRouteColor;
                binding.lineRenderer.endColor = normalRouteColor;
                if (normalRouteMaterial != null)
                {
                    binding.lineRenderer.sharedMaterial = normalRouteMaterial;
                }
            }
        }

        private void RefreshUi()
        {
            if (sharedUi == null || turnController == null || rescueController == null)
            {
                return;
            }

            int remainingTurns = Mathf.Max(0, turnController.MaxTurns - turnController.CurrentTurn);
            sharedUi.SetHud(
                remainingTurns,
                rescueController.Cargo,
                rescueController.CargoCapacity,
                rescueController.Saved,
                rescueController.TotalCivilians);

            string objective = rescueController.Cargo > 0
                ? "Đưa người dân tới điểm trú an toàn."
                : "Cứu 3 người ở các nhà ngập nước.";
            sharedUi.SetObjective(objective);
            sharedUi.ShowMessage("Click điểm kế bên để di chuyển thuyền. Nhấn Q để chờ.");
        }

        private void RefreshCompletionPanel()
        {
            if (completionController == null || gameStateController == null || turnController == null || rescueController == null)
            {
                return;
            }

            int usedTurns = Mathf.Clamp(turnController.CurrentTurn, 0, turnController.MaxTurns);
            if (gameStateController.Result == Round1Result.Won)
            {
                completionController.ShowWin("Bạn đã cứu đủ 3 người dân.", usedTurns, turnController.MaxTurns);
            }
            else if (gameStateController.Result == Round1Result.Lost)
            {
                completionController.ShowLose("Bạn đã hết lượt trước khi cứu đủ người dân.", usedTurns, turnController.MaxTurns);
            }
            else
            {
                completionController.Hide();
            }
        }

        private void CacheBaseScales()
        {
            for (int i = 0; i < nodeVisuals.Length; i++)
            {
                NodeVisualBinding binding = nodeVisuals[i];
                if (binding != null && binding.markerRoot != null)
                {
                    binding.baseScale = binding.markerRoot.localScale.sqrMagnitude > 0.0001f
                        ? binding.markerRoot.localScale
                        : Vector3.one;
                }
            }
        }

        private void Subscribe()
        {
            if (turnController != null)
            {
                turnController.TurnChanged += HandleTurnChanged;
                turnController.ActionCompleted += RefreshAll;
                turnController.RoundReset += RefreshAll;
            }

            if (rescueController != null)
            {
                rescueController.RescueStateChanged += HandleRescueChanged;
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
                turnController.ActionCompleted -= RefreshAll;
                turnController.RoundReset -= RefreshAll;
            }

            if (rescueController != null)
            {
                rescueController.RescueStateChanged -= HandleRescueChanged;
            }

            if (gameStateController != null)
            {
                gameStateController.ResultChanged -= HandleResultChanged;
            }
        }

        private void EnsureReferences()
        {
            sceneReferences ??= FindAnyObjectByType<Round1SceneReferences>();
            nodeGraph ??= FindAnyObjectByType<Round1NodeGraph>();
            boatController ??= FindAnyObjectByType<Round1BoatController>();
            turnController ??= FindAnyObjectByType<Round1TurnController>();
            rescueController ??= FindAnyObjectByType<Round1RescueController>();
            gameStateController ??= FindAnyObjectByType<Round1GameStateController>();
        }

        private static bool IsRescueNode(Round1NodeId nodeId)
        {
            return nodeId == Round1NodeId.NhaBa || nodeId == Round1NodeId.NhaTu;
        }

        private static bool IsShelterNode(Round1NodeId nodeId)
        {
            return nodeId == Round1NodeId.BaiDinh || nodeId == Round1NodeId.GoCao;
        }

        private static void ApplyMaterial(Renderer[] renderers, Material material)
        {
            if (renderers == null || material == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].sharedMaterial = material;
                }
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
