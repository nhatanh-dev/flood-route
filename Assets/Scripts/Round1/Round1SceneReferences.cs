using TMPro;
using UnityEngine;

namespace Round1
{
    public sealed class Round1SceneReferences : MonoBehaviour
    {
        [Header("Nodes")]
        public Transform r1Base;
        public Transform r1Kenh;
        public Transform r1Cho;
        public Transform r1GoCao;
        public Transform r1BaiDinh;
        public Transform r1BenPhu;
        public Transform r1CauTre;
        public Transform r1NhaBa;
        public Transform r1DuongTre;
        public Transform r1NhaTu;

        [Header("Boat")]
        public Transform playerBoatRoot;

        [Header("HUD")]
        public TMP_Text statusText;
        public TMP_Text feedbackText;
        public GameObject winLosePanel;
        public TMP_Text winLoseTitle;
        public TMP_Text winLoseSub;
        
        [HideInInspector] public GameObject winPanel;
        [HideInInspector] public GameObject failPanel;

        private void Awake()
        {
            if (winLosePanel != null)
            {
                failPanel = winLosePanel;
                failPanel.name = "R1_FailPanel";
            }

            if (winPanel == null)
            {
                var all = Resources.FindObjectsOfTypeAll<Transform>();
                foreach (var t in all)
                {
                    if (t.name == "R1_WinPanel" && t.gameObject.scene.isLoaded)
                    {
                        winPanel = t.gameObject;
                        break;
                    }
                }
            }
        }

        [Header("Civilian Visuals")]
        public Transform civilianR1NhaBa1;
        public Transform civilianR1NhaBa2;
        public Transform civilianR1NhaTu1;

        [Header("Debris X1")]
        public Transform x1DebrisVisualRoot;
        public Transform x1Waterline;
        public Renderer benPhuCauTreRouteRenderer;

        [Header("Debris X1 Waypoints")]
        public Transform x1Turn0AwayWaypoint;
        public Transform x1Turn1ApproachWaypoint;
        public Transform x1Turn2BlockWaypoint;
        public Transform x1Turn3RoofRestWaypoint;
    }
}
