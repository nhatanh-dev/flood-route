# Flood Route Shared Round Template

This folder contains reusable systems extracted from `Round2_FinalPolish`.

## Template Scene

- `Assets/Scenes/RoundTemplate.unity`

Scene structure:

- `RoundRoot`
- `Environment`
- `RouteSystem`
  - `GameplayNodes`
  - `GameplayEdges`
  - `RouteVisuals`
- `Objectives`
  - `RescueTargets`
  - `Shelters`
  - `ObjectiveMarkers`
- `DebrisSystem`
  - `DecorativeDebris`
  - `BlockingDebris`
  - `FlowControlNodes`
- `Boat`
- `UI`
- `Managers`

Use this as the skeleton for new rounds. Do not duplicate the full Round 2 scene unless you specifically need a one-off art pass.

## Shared Prefabs

Route:

- `Prefabs/Route/PF_Shared_RouteNode.prefab`
- `Prefabs/Route/PF_Shared_RouteEdge.prefab`

Objectives:

- `Prefabs/Objectives/PF_Shared_RescueTargetMarker.prefab`
- `Prefabs/Objectives/PF_Shared_ShelterMarker.prefab`
- `Prefabs/Objectives/PF_Shared_RescueBadge_ScreenMarker.prefab`
- `Prefabs/Objectives/PF_Shared_RescuePeople_RoofPlaceholders.prefab`

Debris / Q interaction:

- `Prefabs/Debris/PF_Shared_DebrisSystem.prefab`
- `Prefabs/Debris/PF_Shared_DebrisCluster_Blocking.prefab`
- `Prefabs/Debris/PF_Shared_FlowControlNode.prefab`

UI:

- `Prefabs/UI/PF_Shared_RoundGameUI_Canvas.prefab`
- `Prefabs/UI/PF_Shared_CompletionPanel.prefab`

## Shared Scripts

Core route/movement:

- `RouteNode`
- `RouteEdge`
- `RouteGraphManager`
- `BoatRouteMover`
- `NodeClickHandler`
- `RouteVisualManager`

Objectives:

- `ObjectiveStateController`
- `RescueObjectiveCounter`
- `RescueCountMarkerFollower`
- `RescuePeopleVisualController`

Debris:

- `DebrisClearInteraction`
- `FlowControlNode`

UI:

- `RoundUIController`
- `RoundCompletionController`
- `RoundTemplateReferences`

## Round 2 Binding

`Round2_FinalPolish` now contains:

- `R2_Debug_And_Validation/Round2_SharedFramework_Binding`

This object has `RoundTemplateReferences` assigned to the current Round 2 route, objective, debris, boat, UI, and manager objects. It is a non-gameplay reference map for future refactors.

## What Stays Round-Specific

Round-specific content should remain outside shared prefabs:

- node IDs and route topology
- turn limit and route rules
- pickup/shelter IDs
- round-specific houses, landmarks, props, and map layout
- round-specific debris timing/rules
- intro copy and objective copy

## Suggested Round 1 Refactor Path

1. Open Round 1 and create the same template groups.
2. Replace old debug nodes with `PF_Shared_RouteNode` visuals while preserving existing node IDs.
3. Replace old line visuals with `PF_Shared_RouteEdge` style.
4. Add objective/shelter marker prefabs and wire them to Round 1 pickup/dropoff logic.
5. Use `RoundUIController` and `RoundCompletionController` for HUD/result panel consistency.
6. Only after visuals are stable, adapt gameplay controllers toward shared interfaces.
