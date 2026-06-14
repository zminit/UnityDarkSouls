---
id: kd_7b4ddb26-3d1c-4d70-b0a9-5a683295f9e8
type: memory
path: unity-project-understanding/environment-art-assets.md
title: environment-art-assets
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1781357892997
updatedAt: 1781415432558
---

# environment-art-assets

## Summary
Environment art assets migrated from StylizedEnvironement local pack into formal Assets/Art directories and used to dress MainScene outdoor environment.

<!-- locus:body:start -->
# Environment Art Assets

Migrated basic outdoor environment assets from `Assets/_LocalResources/ArtPacks/Environment/StylizedEnvironement` into formal `Assets/Art` directories using the project naming rules.

Formal runtime Prefabs:
- `Assets/Art/Environment/Buildings/PF_Environment_WindTurbine_A_Runtime.prefab`
- `Assets/Art/Environment/Foliage/PF_Environment_Tree_A_Runtime.prefab`
- `Assets/Art/Environment/Foliage/PF_Environment_Tree_B_Runtime.prefab`
- `Assets/Art/Environment/Foliage/PF_Environment_Bush_A_Runtime.prefab`
- `Assets/Art/Environment/Foliage/PF_Environment_GrassCluster_A_Runtime.prefab`
- `Assets/Art/Environment/Props/PF_Environment_Fence_A_Runtime.prefab`
- `Assets/Art/Environment/Props/PF_Environment_Rock_A_Runtime.prefab`
- `Assets/Art/Environment/Props/PF_Environment_Rock_B_Runtime.prefab`
- `Assets/Art/Environment/Props/PF_Environment_Well_A_Runtime.prefab`

Supporting formal asset folders:
- Meshes extracted to `Assets/Art/Models/Environment/ExtractedMeshes` to avoid copied FBX importer dependencies on `_LocalResources` materials/shaders.
- URP materials are under `Assets/Art/Materials/Environment`.
- Textures are under `Assets/Art/Textures/Environment`.
- Wind turbine helix animation/controller are under `Assets/Art/Animations/Environment`.

Validation on migration completion checked 63 assets under the formal environment/material/texture/animation folders and found `ASSETS_WITH_OLD_DEPS=0` for `_LocalResources` dependencies.

## MainScene Outdoor Dressing

`Assets/Scenes/MainScene.unity` now has an `Environment` root with child groups `Ground`, `Buildings`, `Foliage`, and `Props`.

Current dressing contents:
- `Environment/Ground/GroundPlane` uses `Assets/Art/Materials/Environment/Terrain/MAT_Environment_Grass_A_URP.mat` and scale `(14, 1, 14)`.
- `Environment/Buildings` has 1 wind turbine instance.
- `Environment/Foliage` has 4 Tree A, 4 Tree B, 8 Bush A, and 12 GrassCluster A instances.
- `Environment/Props` has 1 Well A, 12 Fence A, 3 Rock A, and 2 Rock B instances.
- Environment objects were marked static for batching/occlusion.

Scene dependency verification after dressing found no direct `_LocalResources` references from `Environment`. Remaining `_LocalResources` dependencies in `MainScene` come indirectly from `Assets/Setting/PlayerAnimationController.controller`, not from environment dressing.
<!-- locus:body:end -->
