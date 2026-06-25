using System.Collections.Generic;
using UnityEngine;

namespace Round1
{
    public class R1RouteGraph : MonoBehaviour
    {
        [SerializeReference] public R1RouteNode startNode;
        [SerializeReference] public R1RouteNode currentNode;
        [SerializeReference] public R1RouteNode selectedTargetNode;

        [SerializeReference] public List<R1RouteNode> allNodes = new List<R1RouteNode>();

        public int remainingTurns = 9;
        
        public int currentRouteStepIndex = 0;

        public bool IsAdjacent(R1RouteNode from, R1RouteNode to)
        {
            if (from == null || to == null) return false;
            return from.adjacentNodes.Contains(to);
        }

        public List<R1RouteNode> GetAdjacentNodes(R1RouteNode node)
        {
            if (node == null) return new List<R1RouteNode>();
            return node.adjacentNodes;
        }

        public bool SetSelectedTarget(R1RouteNode targetNode)
        {
            if (targetNode == null) return false;

            if (IsAdjacent(currentNode, targetNode))
            {
                if (remainingTurns > 0)
                {
                    remainingTurns--;
                }
                selectedTargetNode = targetNode;
                currentRouteStepIndex = 0;
                return true;
            }

            selectedTargetNode = null;
            return false;
        }

        public void ArriveAtSelectedTarget()
        {
            if (selectedTargetNode != null && IsAdjacent(currentNode, selectedTargetNode))
            {
                currentNode = selectedTargetNode;
                selectedTargetNode = null;
            }
        }
    }
}
