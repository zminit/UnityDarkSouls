---
id: kd_4ca1a839-8251-46ac-984e-0c76e87e8af0
type: memory
path: unity-project-understanding/art-player-assets.md
title: art-player-assets
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1780407548996
updatedAt: 1780459331477
---

# art-player-assets

## Summary
Player art assets, material assignments, and URP pipeline dependency after resource normalization.

<!-- locus:body:start -->
- Active scene `Assets/Scenes/MainScene.unity` uses `Player/ModelRoot/PlayerModel/H_Ace_01` with a SkinnedMeshRenderer.
- Player model prefab is `Assets/Art/Prefabs/Characters/Player/PF_Player_Model.prefab`, depending on model `Assets/Art/Characters/Player/CH_Player_Body_LOD0.FBX`.
- Player Ace texture assets are under `Assets/Art/Textures/Characters/Player/Ace/`, e.g. `TEX_Player_Ace_Cloth00_BC.tga`, `_N`, `_R`; `Cloth01`, `Cloth02`, `Eye_BC`, `Hair_BC`, `Skin_BC/N/R`, `Weapon_BC/N/R`.
- Weapon Ace textures are under `Assets/Art/Textures/Weapons/Ace/`, e.g. `TEX_Weapon_Ace_Bow_BC/N/R` and `TEX_Weapon_Ace_Arrow_BC/N/R`.
- Player Ace body material assets are under `Assets/Art/Materials/Characters/Player/Ace/`. `H_Ace_01` uses six explicit material slots: `MAT_Player_Ace_Hair_Opaque.mat`, `MAT_Player_Ace_Skin_Opaque.mat`, `MAT_Player_Ace_Eye_Transparent.mat`, `MAT_Player_Ace_Cloth00_Opaque.mat`, `MAT_Player_Ace_Cloth01_Opaque.mat`, and `MAT_Player_Ace_Cloth02_Opaque.mat`.
- These Player Ace materials use `Universal Render Pipeline/Lit`; the project must have URP enabled via `Assets/_LocalResources/ArtPacks/Environment/StylizedEnvironement/UniversalRenderPipelineAsset.asset` in `ProjectSettings/GraphicsSettings.asset` and the active quality level, otherwise the model renders pink.
<!-- locus:body:end -->
