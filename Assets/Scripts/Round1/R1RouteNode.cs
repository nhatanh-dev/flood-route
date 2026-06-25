using System.Collections.Generic;
using UnityEngine;

namespace Round1
{
    public enum R1NodeType
    {
        Start,
        Junction,
        Rescue,
        Shelter
    }

    [System.Serializable]
    public class R1RouteNode
    {
        public string nodeId;
        public string displayName;
        public R1NodeType nodeType;
        public int peopleCount;
        public bool isCollected;
        public bool isDelivered;
        public Transform worldAnchor;
        
        [Tooltip("Ordered sequence of intermediate waypoints to reach this node from the previous node.")]
        public List<Transform> pathWaypoints = new List<Transform>();

        [SerializeReference]
        public List<R1RouteNode> adjacentNodes = new List<R1RouteNode>();
    }
}
