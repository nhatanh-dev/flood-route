# Asset Store Trees Dependency

The tree assets used from Unity Asset Store come from:

- Package: `Unity Terrain - URP Demo Scene`
- Local import folder: `Assets/TerrainDemoScene_URP`
- Package Manager source: `My Assets`

This package is intentionally ignored by Git because the full import is very large.
If trees are missing locally, re-import the package from Package Manager.

Main tree prefabs after re-import:

- `Assets/TerrainDemoScene_URP/Prefabs/Trees/Conifer/Conifer.prefab`
- `Assets/TerrainDemoScene_URP/Prefabs/Trees/Cypress/Cypress_Forest_Desktop.prefab`
- `Assets/TerrainDemoScene_URP/Prefabs/Trees/Pines/Pine_A/Pine_A.prefab`
- `Assets/TerrainDemoScene_URP/Prefabs/Trees/Pines/Pine_B/Pine_B.prefab`
- `Assets/TerrainDemoScene_URP/Prefabs/Trees/Pines/Pine_C/Pine_C.prefab`
- `Assets/TerrainDemoScene_URP/Prefabs/Trees/Pines/Pine_D/Pine_D.prefab`

Do not commit the whole `Assets/TerrainDemoScene_URP` folder unless the team decides
to move large art dependencies to Git LFS.
