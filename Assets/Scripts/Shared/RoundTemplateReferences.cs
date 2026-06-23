using UnityEngine;

public sealed class RoundTemplateReferences : MonoBehaviour
{
    [Header("Round Identity")]
    [SerializeField] private string roundId = "ROUND_TEMPLATE";
    [SerializeField] private string displayName = "Round Template";

    [Header("Scene Groups")]
    [SerializeField] private Transform environmentRoot;
    [SerializeField] private Transform routeSystemRoot;
    [SerializeField] private Transform objectivesRoot;
    [SerializeField] private Transform debrisSystemRoot;
    [SerializeField] private Transform boatRoot;
    [SerializeField] private Transform uiRoot;
    [SerializeField] private Transform managersRoot;

    [Header("Route System")]
    [SerializeField] private Transform gameplayNodesRoot;
    [SerializeField] private Transform gameplayEdgesRoot;
    [SerializeField] private RouteGraphManager routeGraphManager;
    [SerializeField] private RouteVisualManager routeVisualManager;

    [Header("Objectives")]
    [SerializeField] private RescueObjectiveCounter rescueObjectiveCounter;
    [SerializeField] private RescueCountMarkerFollower rescueBadgeFollower;
    [SerializeField] private RescuePeopleVisualController rescuePeopleVisualController;
    [SerializeField] private Transform rescueTargetAnchor;
    [SerializeField] private Transform shelterTargetAnchor;

    [Header("Debris / Q Interaction")]
    [SerializeField] private DebrisClearInteraction debrisClearInteraction;
    [SerializeField] private FlowControlNode flowControlNode;

    [Header("Boat")]
    [SerializeField] private BoatRouteMover boatRouteMover;

    [Header("UI")]
    [SerializeField] private RoundUIController roundUiController;
    [SerializeField] private RoundCompletionController roundCompletionController;

    public string RoundId => roundId;
    public string DisplayName => displayName;
    public Transform EnvironmentRoot => environmentRoot;
    public Transform RouteSystemRoot => routeSystemRoot;
    public Transform ObjectivesRoot => objectivesRoot;
    public Transform DebrisSystemRoot => debrisSystemRoot;
    public Transform BoatRoot => boatRoot;
    public Transform UiRoot => uiRoot;
    public Transform ManagersRoot => managersRoot;
    public Transform GameplayNodesRoot => gameplayNodesRoot;
    public Transform GameplayEdgesRoot => gameplayEdgesRoot;
    public RouteGraphManager RouteGraphManager => routeGraphManager;
    public RouteVisualManager RouteVisualManager => routeVisualManager;
    public RescueObjectiveCounter RescueObjectiveCounter => rescueObjectiveCounter;
    public RescueCountMarkerFollower RescueBadgeFollower => rescueBadgeFollower;
    public RescuePeopleVisualController RescuePeopleVisualController => rescuePeopleVisualController;
    public Transform RescueTargetAnchor => rescueTargetAnchor;
    public Transform ShelterTargetAnchor => shelterTargetAnchor;
    public DebrisClearInteraction DebrisClearInteraction => debrisClearInteraction;
    public FlowControlNode FlowControlNode => flowControlNode;
    public BoatRouteMover BoatRouteMover => boatRouteMover;
    public RoundUIController RoundUiController => roundUiController;
    public RoundCompletionController RoundCompletionController => roundCompletionController;
}
