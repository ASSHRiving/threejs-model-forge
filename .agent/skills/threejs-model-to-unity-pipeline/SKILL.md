---
name: threejs-model-to-unity-pipeline
description: Build interactive Three.js procedural 3D model generators across any domain (hard-surface, sci-fi, architecture, vehicles, nature, industrial props, or stylized items) with built-in GLB/OBJ and procedural PBR texture export for Unity URP and Blender.
---

# threejs-model-to-unity-pipeline

An end-to-end workflow for building interactive Three.js procedural 3D asset generators of any genre or category that export clean `.glb`, `.obj`, and procedural PBR `.png` textures ready for Unity Universal Render Pipeline (URP) and Blender.

## When to Use

- The user asks to generate any 3D model, prop, vehicle, architectural piece, mechanical assembly, organic asset, weapon, or visual showcase using Three.js/WebGL.
- The user wants an interactive WebGL model generator with one-click export buttons for `.glb`, `.obj`, or procedural PBR `.png` textures.
- The user wants to import Three.js-generated 3D models into Unity URP or Blender while preserving hierarchy, materials, vertex AO, and textures.

## Subject Selection & Creativity Rule

- **Strict Prompt Adherence**: Tailor the 3D model's subject, silhouette, and material palette strictly to the user's requested theme (e.g., sci-fi robotics, vehicles, modern/historical architecture, botanicals, industrial machinery, everyday objects, or fantasy gear). Never force fantasy, dungeon, or altar motifs unless requested.
- **Open-Ended Showcase Diversity**: If the user gives an open-ended prompt without specifying a subject (e.g., "surprise me" or "show your best modeling"), avoid repetitive default tropes like basic primitives or generic rotating-ring altars; instead, design an intricate, original centerpiece (such as a kinetic clockwork mechanism, a futuristic turbine/engine assembly, an architectural pavilion, a deep-sea submersible, or a detailed botanical terrarium).

## Steps

1. **Procedural Geometry, UV Integrity, Vertex AO, and Smart Merging**
   - **Scale & Pivot**: Construct the asset inside a dedicated root `THREE.Group` (`assetGroup`) aligned to Unity's `1 unit = 1 meter` scale, with the root pivot logically placed (bottom-centered at `y = 0` for grounded props/structures, center-of-mass for vehicles/aerial objects, or grip-centered at `(0, 0, 0)` for handheld items).
   - **Distortion-Free UVs**: Compute `V` coordinates along true cumulative Euclidean arc length for lofted profiles, curved pipes, and swept surfaces; apply triplanar box UVs for irregular rocks, terrain chunks, or organic surfaces.
   - **Native Vertex Color AO (`COLOR_0`)**: Bake crevice darkening and contact occlusion directly into the geometry's `color` vertex attribute (`vertexColors: true`) so `GLTFExporter` preserves `COLOR_0` for zero-texture-cost micro-shadows in Unity URP Lit.
   - **Smart Merging & Semantic Naming**: Normalize vertex attributes and merge same-material static sub-meshes via `BufferGeometryUtils.mergeGeometries` to minimize Draw Calls. Keep animated or interactive sub-parts (e.g., `Part_Rotor_01`, `Part_Piston_Main`, `Part_Core`) as distinct named child nodes, assign clean material names (`MAT_Primary_Metal`, `MAT_Secondary_Trim`, `MAT_Surface_Base`, `MAT_Emissive_Accent`), and optionally include toggleable low-poly convex collision proxies (`Collider_*`).

2. **Procedural PBR Materials, Sobel Height-to-Normal Baking, and ColorSpace**
   - Generate 1024×1024 surface textures programmatically via HTML5 `<canvas>` and wrap them in `THREE.CanvasTexture`.
   - **Height-to-Normal Conversion**: Run a 3×3 Sobel filter pass over grayscale height maps to bake true RGB tangent-space normal maps (`normalMap` / `Normal_Map.png`) rather than relying on `bumpMap`, ensuring `GLTFExporter` serializes valid `normalTexture` data for Unity URP.
   - **Strict ColorSpace Assignment**: Set `texture.colorSpace = THREE.SRGBColorSpace` for color/emission maps (`map`, `emissiveMap`) and `texture.colorSpace = THREE.NoColorSpace` (linear) for data maps (`normalMap`, `roughnessMap`, `metalnessMap`, `aoMap`) to match Unity URP without gamma distortion.

3. **PMREM Studio Lighting, LOD Budget HUD, and Pre-Export Pose Reset**
   - Build a self-contained HTML/Three.js generator (`<script type="importmap">` with CDN `three` and `three/addons/`) featuring `OrbitControls`, a `RoomEnvironment` PMREM `scene.environment` (matching Unity URP Reflection Probes), and consolidated lighting (at most 1 dynamic `PointLight` paired with emissive glow meshes to prevent multi-light shader overdraw).
   - **Real-Time Asset Budget & LOD Toggle**: Provide a **Hero Showcase (~20k tris)** vs. **Game-Ready (~6k tris)** geometry detail toggle alongside a live readout of **Triangles**, **Vertices**, and **Exported Mesh / Draw Call count** for `assetGroup`.
   - **Pre-Export Pose Reset Hook**: On clicking `.glb` or `.obj` export, pause animations, reset all dynamic child transforms (`position`, `rotation`, `scale`) to their canonical rest pose, serialize only `assetGroup`, and resume preview animation.
   - **One-Click Exports**:
     - **Download `.glb` (`GLTFExporter`)**: Export `assetGroup` with `{ binary: true }` so meshes, hierarchy, vertex colors, PBR values, and baked textures pack into one binary file.
     - **Download `.obj` (`OBJExporter`)**: Export `.obj` geometry as a universal fallback.
     - **Download Procedural `.png` Textures**: Export generated canvas textures (Albedo, Normal, Roughness, Metalness, Emissive) as standalone `.png` files.

4. **Execution Environment & Unity URP Integration**
   - **Cross-Machine Model Repository Detection**: Determine the target `threejs-model-forge` repository root dynamically without hardcoding a single machine's absolute path:
     1. **Active Workspace Check**: First check whether the currently opened Active Workspace is `threejs-model-forge`. If so, output directly to `<workspace>/<asset-name>/`.
     2. **Cross-Machine Path Probing**: When running from another workspace, check via terminal or script in order whether `C:\Rich\Three.js\threejs-model-forge` or `%USERPROFILE%\Documents\GitHub\threejs-model-forge` exists (e.g., in PowerShell: `@('C:\Rich\Three.js\threejs-model-forge', "$env:USERPROFILE\Documents\GitHub\threejs-model-forge") | Where-Object { Test-Path $_ } | Select-Object -First 1`), and automatically use the existing local repository.
     3. **Output & Repo Registration**: Save every new generated model project under `<repo-root>/<asset-name>/` (with its own `Tools/ModelForge/<asset_name>_forge.html` and `Assets/Scripts/*.cs`), update `index.html` and `README.md` at `<repo-root>` with a matching entry, and stage/commit when requested. (If neither path exists, fall back to the active workspace or ask the user for the repository location.)
   - **Web Canvas vs. Local IDE (e.g., Antigravity / VS Code)**: If running in a local IDE agent without an embedded web canvas, write the standalone `.html` generator into `Tools/ModelForge/<asset_name>_forge.html`, launch it via a local HTTP server or browser preview so `Blob` file downloads work cleanly without `file://` restrictions, and save companion C# scripts directly into `Assets/Scripts/` when requested.
   - **Unity URP Prefab Setup**: Import `.glb` via Unity's `glTFast` package (or route through Blender to `.fbx` with `Path Mode: Copy` and `Embed Textures`). Assign exported `.png` maps to `Universal Render Pipeline/Lit` (`Base Map`, `Metallic Map`, `Normal Map` marked as `Normal map` Texture Type, and `Emission`), attach `MeshCollider` (Convex) using the `Collider_*` proxies, and add modular C# scripts for runtime motion or `MaterialPropertyBlock` emission pulsing.

## Gotchas

- Never export the entire `THREE.Scene` via `GLTFExporter`; always pass only the isolated `assetGroup` so preview lights, cameras, and floor grids are excluded from the Unity prefab.
- Never rely on `bumpMap` alone for exported assets—`GLTFExporter` does not convert `bumpMap` into a glTF `normalTexture`. Always bake grayscale heights into an RGB tangent-space `normalMap`.
- Never export mid-animation transforms; always execute the pre-export rest-pose reset hook before calling `GLTFExporter` so child pivots and rotations stay clean in Unity.
- Avoid `THREE.ShaderMaterial` for surface textures that need to be exported, as custom GLSL shaders do not serialize into `.glb` or `.obj`. Always bake procedural patterns into `<canvas>` textures on `THREE.MeshStandardMaterial`.
- Ensure all procedural geometry has valid UV coordinates, matching vertex attributes (`position`, `normal`, `uv`, `color`) prior to `BufferGeometryUtils.mergeGeometries`, and computed vertex normals (`geometry.computeVertexNormals()`) before exporting.
