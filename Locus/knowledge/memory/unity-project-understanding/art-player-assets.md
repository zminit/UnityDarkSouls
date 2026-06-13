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
updatedAt: 1781270996075
---

# art-player-assets

## Summary
Player art assets, material assignments, player Animator Controller lookup, Upper Body Layer locomotion mapping, abandoned prototype weapon setup cleanup state, Elden Sword extracted assets, and URP pipeline dependency after resource normalization.

<!-- locus:body:start -->
- Active scene `Assets/Scenes/MainScene.unity` uses `Player/ModelRoot/PlayerModel/H_Ace_01` with a SkinnedMeshRenderer.
- `Player/ModelRoot/PlayerModel` has the Animator component. Its controller is `Assets/Setting/PlayerAnimationController.controller`, and its avatar comes from `Assets/Art/Characters/Player/CH_Player_Body_LOD0.FBX`.
- Player model prefab is `Assets/Art/Prefabs/Characters/Player/PF_Player_Model.prefab`, depending on model `Assets/Art/Characters/Player/CH_Player_Body_LOD0.FBX`; current MainScene player weapon setup is on the scene instance, not verified as prefab-backed.
- Prototype scene weapon objects may remain from an abandoned prototype setup: hand socket `Player/ModelRoot/PlayerModel/Dummy_CP/Bip001/Bip001-Pelvis/Bip001-Spine/Bip001-Spine1/Bip001-Spine2/Bip001-R-Clavicle/Bip001-R-UpperArm/Bip001-R-Forearm/Bip001-R-Hand/RightHandWeaponSocket`, sheathe socket `Player/ModelRoot/PlayerModel/Dummy_CP/Bip001/Bip001-Prop1/D_R_WP/SheatheSocket`, and prototype weapon `Player/ModelRoot/PlayerModel/Dummy_CP/Bip001/Bip001-Prop1/D_R_WP/SheatheSocket/PrototypeKatana`.
- The abandoned prototype weapon runtime scripts were removed: `Assets/Scripts/Animation/PlayerAnimationEvents.cs`, `Assets/Scripts/Combat/WeaponController.cs`, and `Assets/Scripts/Combat/WeaponHitbox.cs`. Scene components were intentionally not removed, so old component slots may show Missing Script until the new weapon system is designed.
- Project layers include `PlayerWeapon` at layer 7 and `EnemyHurtbox` at layer 8. Physics matrix currently allows `PlayerWeapon` to collide with `EnemyHurtbox`; other named layers are ignored for `PlayerWeapon`.
- `PlayerAnimationController.controller` has `Base Layer` plus `Upper Body Layer`. `Upper Body Layer` uses `Assets/Art/Animations/Shared/Masks/AN_Shared_Player_UpperBody_Mask.mask`, default weight 1, `Override` blending, and contains `LocomotionWithWeapon`, `DrawSword`, and `SheatheSword` states.
- `Upper Body Layer/LocomotionWithWeapon` is a `FreeformDirectional2D` Blend Tree driven by `Horizontal` / `Vertical` with 17 children: idle at `(0,0)`, walk directions at radius 1, and run directions at radius 2.
- `LocomotionWithWeapon` uses Katana IP locomotion clips under `Assets/Art/Animations/Player/Locomotion/Katana/`. Forward walk/run use `AN_Player_Katana_Strafe_Walk_F_Loop_IP.anim` and `AN_Player_Katana_Strafe_Run_F_Loop_IP.anim` because exact `..._Walk_F_IP` and `..._Run_F_IP` assets are absent.
- Verify current Animation Events directly before editing combat/equip clips; event state is being actively redesigned and may change between tasks.
- Elden Sword FBX source is `Assets/Art/Weapons/WP_EldenSword_Package_LOD0.fbx`. Its four Mesh subassets were extracted to `Assets/Art/Weapons/3d66-Editable_Poly-22334717-001.asset` through `004.asset`, and its two Material subassets were extracted to `Assets/Art/Weapons/3d66-VRayMtl-22334717-023.mat` and `024.mat`.
- Player Ace texture assets are under `Assets/Art/Textures/Characters/Player/Ace/`, e.g. `TEX_Player_Ace_Cloth00_BC.tga`, `_N`, `_R`; `Cloth01`, `Cloth02`, `Eye_BC`, `Hair_BC`, `Skin_BC/N/R`, `Weapon_BC/N/R`.
- Weapon Ace textures are under `Assets/Art/Textures/Weapons/Ace/`, e.g. `TEX_Weapon_Ace_Bow_BC/N/R` and `TEX_Weapon_Ace_Arrow_BC/N/R`.
- Player Ace body material assets are under `Assets/Art/Materials/Characters/Player/Ace/`. `H_Ace_01` uses six explicit material slots: `MAT_Player_Ace_Hair_Opaque.mat`, `MAT_Player_Ace_Skin_Opaque.mat`, `MAT_Player_Ace_Eye_Transparent.mat`, `MAT_Player_Ace_Cloth00_Opaque.mat`, `MAT_Player_Ace_Cloth01_Opaque.mat`, and `MAT_Player_Ace_Cloth02_Opaque.mat`.
- These Player Ace materials use `Universal Render Pipeline/Lit`; the project must have URP enabled via `Assets/_LocalResources/ArtPacks/Environment/StylizedEnvironement/UniversalRenderPipelineAsset.asset` in `ProjectSettings/GraphicsSettings.asset` and the active quality level, otherwise the model renders pink.
<!-- locus:body:end -->
