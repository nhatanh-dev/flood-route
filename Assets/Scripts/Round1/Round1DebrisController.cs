using System.Collections;
using UnityEngine;

namespace Round1
{
    public sealed class Round1DebrisController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Round1SceneReferences sceneReferences;
        [SerializeField] private float moveDuration = 0.8f;
        [SerializeField] private float waypointPositionTolerance = 0.005f;
        [SerializeField] private float waypointRotationToleranceDegrees = 0.1f;
        [SerializeField] private Color blockedColor = new Color(1f, 0.6470588f, 0f, 1f);

        private MaterialPropertyBlock routePropertyBlock;
        private Coroutine movementCoroutine;
        private Color openColor = Color.cyan;
        private bool hasBaseColor;
        private bool hasColor;

        public bool IsRouteBlocked { get; private set; }
        public bool IsMoving => movementCoroutine != null;

        private void Awake()
        {
            Initialize();
        }

        private void OnDisable()
        {
            StopActiveMovement();
        }

        public void ApplyTurnState(int turn)
        {
            EnsureInitialized();

            if (turn <= 0)
            {
                StopActiveMovement();
                SnapDebrisTo(sceneReferences.x1Turn0AwayWaypoint);
                SetRouteBlocked(false);
                return;
            }

            if (turn == 1)
            {
                MoveDebrisTo(sceneReferences.x1Turn1ApproachWaypoint);
                SetRouteBlocked(false);
                return;
            }

            if (turn == 2)
            {
                MoveDebrisTo(sceneReferences.x1Turn2BlockWaypoint);
                SetRouteBlocked(true);
                return;
            }

            MoveDebrisTo(sceneReferences.x1Turn3RoofRestWaypoint);
            SetRouteBlocked(false);
        }

        [ContextMenu("Test Turn 0")]
        private void TestTurn0()
        {
            ApplyTurnState(0);
        }

        [ContextMenu("Test Turn 1")]
        private void TestTurn1()
        {
            ApplyTurnState(1);
        }

        [ContextMenu("Test Turn 2")]
        private void TestTurn2()
        {
            ApplyTurnState(2);
        }

        [ContextMenu("Test Turn 3")]
        private void TestTurn3()
        {
            ApplyTurnState(3);
        }

        private void Initialize()
        {
            routePropertyBlock ??= new MaterialPropertyBlock();
            EnsureInitialized();
            CaptureOpenRouteColor();
            StopActiveMovement();
            SnapDebrisTo(sceneReferences.x1Turn0AwayWaypoint);
            SetRouteBlocked(false);
        }

        private void EnsureInitialized()
        {
            if (sceneReferences == null)
            {
                sceneReferences = FindAnyObjectByType<Round1SceneReferences>();
            }
        }

        private void CaptureOpenRouteColor()
        {
            Renderer routeRenderer = sceneReferences != null ? sceneReferences.benPhuCauTreRouteRenderer : null;
            if (routeRenderer == null || routeRenderer.sharedMaterial == null)
            {
                hasBaseColor = false;
                hasColor = false;
                openColor = Color.cyan;
                return;
            }

            Material sharedMaterial = routeRenderer.sharedMaterial;
            hasBaseColor = sharedMaterial.HasProperty(BaseColorId);
            hasColor = sharedMaterial.HasProperty(ColorId);

            if (hasBaseColor)
            {
                openColor = sharedMaterial.GetColor(BaseColorId);
                return;
            }

            if (hasColor)
            {
                openColor = sharedMaterial.GetColor(ColorId);
                return;
            }

            openColor = Color.cyan;
        }

        private void MoveDebrisTo(Transform waypoint)
        {
            if (sceneReferences == null || sceneReferences.x1DebrisVisualRoot == null || waypoint == null)
            {
                return;
            }

            if (IsDebrisAtWaypoint(waypoint))
            {
                StopActiveMovement();
                SnapDebrisTo(waypoint);
                return;
            }

            StopActiveMovement();
            movementCoroutine = StartCoroutine(MoveDebrisRoutine(waypoint));
        }

        private bool IsDebrisAtWaypoint(Transform waypoint)
        {
            Transform debrisRoot = sceneReferences != null ? sceneReferences.x1DebrisVisualRoot : null;
            if (debrisRoot == null || waypoint == null)
            {
                return false;
            }

            float positionTolerance = Mathf.Max(0f, waypointPositionTolerance);
            float rotationTolerance = Mathf.Max(0f, waypointRotationToleranceDegrees);
            bool positionMatches = Vector3.Distance(debrisRoot.position, waypoint.position) <= positionTolerance;
            bool rotationMatches = Quaternion.Angle(debrisRoot.rotation, waypoint.rotation) <= rotationTolerance;
            return positionMatches && rotationMatches;
        }

        private IEnumerator MoveDebrisRoutine(Transform waypoint)
        {
            Transform debrisRoot = sceneReferences.x1DebrisVisualRoot;
            Vector3 startPosition = debrisRoot.position;
            Quaternion startRotation = debrisRoot.rotation;
            Vector3 targetPosition = waypoint.position;
            Quaternion targetRotation = waypoint.rotation;
            float duration = Mathf.Max(0.01f, moveDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                debrisRoot.SetPositionAndRotation(
                    Vector3.LerpUnclamped(startPosition, targetPosition, t),
                    Quaternion.SlerpUnclamped(startRotation, targetRotation, t));
                yield return null;
            }

            debrisRoot.SetPositionAndRotation(targetPosition, targetRotation);
            movementCoroutine = null;
        }

        private void SnapDebrisTo(Transform waypoint)
        {
            if (sceneReferences == null || sceneReferences.x1DebrisVisualRoot == null || waypoint == null)
            {
                return;
            }

            sceneReferences.x1DebrisVisualRoot.SetPositionAndRotation(waypoint.position, waypoint.rotation);
        }

        private void SetRouteBlocked(bool blocked)
        {
            IsRouteBlocked = blocked;

            Renderer routeRenderer = sceneReferences != null ? sceneReferences.benPhuCauTreRouteRenderer : null;
            if (routeRenderer == null)
            {
                return;
            }

            routeRenderer.GetPropertyBlock(routePropertyBlock);
            Color routeColor = blocked ? blockedColor : openColor;

            if (hasBaseColor)
            {
                routePropertyBlock.SetColor(BaseColorId, routeColor);
            }

            if (hasColor)
            {
                routePropertyBlock.SetColor(ColorId, routeColor);
            }

            routeRenderer.SetPropertyBlock(routePropertyBlock);
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
