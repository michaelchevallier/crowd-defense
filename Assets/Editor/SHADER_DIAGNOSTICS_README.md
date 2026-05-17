# Toon Material & Shader Diagnostics

## Overview

Editor scripts to audit, diagnose, and repair broken shader references in Crowd Defense materials. URP renders materials with invalid shaders as magenta — these tools help identify and fix them.

## Tools

Access via **Tools > CrowdDefense** menu:

### 1. Audit Materials — Find Broken Shaders
- Scans `Assets/Materials/` for materials with null or error shaders
- Logs a list to Console
- No side effects (read-only)

### 2. Fix Materials — Reimport All
- Force-reimports all materials to rebuild shader references
- Calls `AssetDatabase.SaveAssets()` + `AssetDatabase.Refresh()`
- Fixes orphaned shader pointers if they still exist in the project

### 3. Toon Materials Diagnostics (Window)
- Opens an interactive Editor window
- Scan materials and see issues in a scrollable UI
- Button to apply fixes without Console logs

### 4. Audit Scenes — Find Magenta Objects
- Opens each scene in `Assets/Scenes/`
- Finds all `Renderer` components using broken shaders
- Logs with GameObject pinning (click to select in hierarchy)

### 5. Debug — List All Shader GUIDs
- Dumps all shader file → GUID mappings
- Useful to manually trace a broken reference

### 6. Debug — Validate All Material Shader References
- Checks each material's `m_Shader: {guid:...}` line
- Identifies orphaned GUIDs (pointing to deleted shaders)
- Reports valid broken references (null shaders) separately

## Audit Results (2026-05-17)

✓ **All materials have valid shader pointers** — no broken references detected.

- 20 materials scanned (Assets/Materials/*.mat + Skybox/*.mat)
- All shader GUIDs resolve to existing shader files
- Skybox materials use Unity built-in shader GUID (expected)
- No magenta indicators found in materials or scenes

## If Magenta Still Appears

1. Check **Project Settings > Graphics** — URP quality settings
2. Run "Audit Scenes — Find Magenta Objects" — logs all problematic GameObjects
3. Verify the material's shader is correctly compiled (no HLSL errors)
4. Check if the issue is a runtime-instantiated material without a valid shader

## References

- [Unity URP Material Troubleshooting](https://docs.unity3d.com/6000.0/Documentation/Manual/urp-materials-introduction.html)
- ToonCelShading.shader → main Toon shader (guid: 584e6eabab2446508202051ef07b5673)
- Toon_Lit.shader → cel-shaded Lambert variant (guid: b8f1330d2b2e743e9bfa8a86a72e777a)
