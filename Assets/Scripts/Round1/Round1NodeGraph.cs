using System;
using System.Collections.Generic;
using UnityEngine;

namespace Round1
{
    public sealed class Round1NodeGraph : MonoBehaviour
    {
        [SerializeField] private Round1SceneReferences sceneReferences;
        [SerializeField] private Round1DebrisController debrisController;

        private readonly Dictionary<Round1NodeId, Transform> nodeTransforms = new();
        private readonly Dictionary<Round1NodeId, Round1NodeId[]> adjacency = new();
        private bool initialized;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            sceneReferences ??= FindAnyObjectByType<Round1SceneReferences>();
            debrisController ??= FindAnyObjectByType<Round1DebrisController>();

            nodeTransforms.Clear();
            adjacency.Clear();

            if (sceneReferences != null)
            {
                nodeTransforms[Round1NodeId.Base] = sceneReferences.r1Base;
                nodeTransforms[Round1NodeId.Kenh] = sceneReferences.r1Kenh;
                nodeTransforms[Round1NodeId.Cho] = sceneReferences.r1Cho;
                nodeTransforms[Round1NodeId.GoCao] = sceneReferences.r1GoCao;
                nodeTransforms[Round1NodeId.BaiDinh] = sceneReferences.r1BaiDinh;
                nodeTransforms[Round1NodeId.BenPhu] = sceneReferences.r1BenPhu;
                nodeTransforms[Round1NodeId.CauTre] = sceneReferences.r1CauTre;
                nodeTransforms[Round1NodeId.NhaBa] = sceneReferences.r1NhaBa;
                nodeTransforms[Round1NodeId.DuongTre] = sceneReferences.r1DuongTre;
                nodeTransforms[Round1NodeId.NhaTu] = sceneReferences.r1NhaTu;
            }

            AddUndirectedEdge(Round1NodeId.Base, Round1NodeId.Kenh);
            AddUndirectedEdge(Round1NodeId.Kenh, Round1NodeId.Cho);
            AddUndirectedEdge(Round1NodeId.Kenh, Round1NodeId.BenPhu);
            AddUndirectedEdge(Round1NodeId.BenPhu, Round1NodeId.CauTre);
            AddUndirectedEdge(Round1NodeId.CauTre, Round1NodeId.Cho);
            AddUndirectedEdge(Round1NodeId.Cho, Round1NodeId.NhaBa);
            AddUndirectedEdge(Round1NodeId.Cho, Round1NodeId.GoCao);
            AddUndirectedEdge(Round1NodeId.NhaBa, Round1NodeId.DuongTre);
            AddUndirectedEdge(Round1NodeId.DuongTre, Round1NodeId.NhaTu);
            AddUndirectedEdge(Round1NodeId.NhaTu, Round1NodeId.BaiDinh);

            initialized = true;
        }

        public Transform GetNodeTransform(Round1NodeId id)
        {
            Initialize();
            return nodeTransforms.TryGetValue(id, out Transform nodeTransform) ? nodeTransform : null;
        }

        public bool AreAdjacent(Round1NodeId from, Round1NodeId to)
        {
            Initialize();
            if (!adjacency.TryGetValue(from, out Round1NodeId[] adjacentNodes))
            {
                return false;
            }

            return Array.IndexOf(adjacentNodes, to) >= 0;
        }

        public bool CanTraverse(Round1NodeId from, Round1NodeId to)
        {
            if (!AreAdjacent(from, to))
            {
                return false;
            }

            if (IsBenPhuCauTreEdge(from, to))
            {
                return debrisController == null || !debrisController.IsRouteBlocked;
            }

            return true;
        }

        public IReadOnlyList<Round1NodeId> GetAdjacentNodeIds(Round1NodeId id)
        {
            Initialize();
            return adjacency.TryGetValue(id, out Round1NodeId[] adjacentNodes)
                ? adjacentNodes
                : Array.Empty<Round1NodeId>();
        }

        private void AddUndirectedEdge(Round1NodeId first, Round1NodeId second)
        {
            AddDirectedEdge(first, second);
            AddDirectedEdge(second, first);
        }

        private void AddDirectedEdge(Round1NodeId from, Round1NodeId to)
        {
            if (!adjacency.TryGetValue(from, out Round1NodeId[] existing))
            {
                adjacency[from] = new[] { to };
                return;
            }

            Round1NodeId[] next = new Round1NodeId[existing.Length + 1];
            Array.Copy(existing, next, existing.Length);
            next[^1] = to;
            adjacency[from] = next;
        }

        private static bool IsBenPhuCauTreEdge(Round1NodeId from, Round1NodeId to)
        {
            return from == Round1NodeId.BenPhu && to == Round1NodeId.CauTre
                || from == Round1NodeId.CauTre && to == Round1NodeId.BenPhu;
        }
    }
}
