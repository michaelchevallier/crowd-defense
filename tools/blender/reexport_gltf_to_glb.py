"""
Re-export .gltf -> .glb self-contained via Blender headless.

Usage (single file):
    blender --background --python tools/blender/reexport_gltf_to_glb.py -- <path_to.gltf>

Usage (batch via shell):
    for f in Assets/Models/Enemies/{goblin,knight,...}.gltf; do
        /Applications/Blender.app/Contents/MacOS/Blender --background \
            --python tools/blender/reexport_gltf_to_glb.py -- "$f"
    done

Reads a .gltf at <input_file>, imports the full scene into a clean Blender
session (factory settings), then exports to .glb with embedded textures.

Why .glb and not .gltf:
    Single-file binary container — UnityGLTF's auto-importer handles .glb
    more reliably than fragmented .gltf with base64 data: URIs (the original
    Quaternius .gltf used base64 buffer URIs but UnityGLTF skipped them at
    AssetDatabase load time; Blender re-encoding regenerates a clean
    standards-compliant .glb).

Output path = same as input but with .glb extension. Caller is responsible
for deleting the original .gltf afterwards if desired.
"""
import bpy
import sys
import os


def main():
    argv = sys.argv
    # Blender consumes everything before "--"; arguments after "--" are ours.
    if "--" not in argv:
        print("[reexport] ERROR no '--' separator found in argv", file=sys.stderr)
        sys.exit(1)
    user_args = argv[argv.index("--") + 1 :]
    if not user_args:
        print("[reexport] ERROR no input file provided", file=sys.stderr)
        sys.exit(1)

    input_file = user_args[0]
    if not os.path.isfile(input_file):
        print(f"[reexport] ERROR input not found: {input_file}", file=sys.stderr)
        sys.exit(1)

    # Compute output (same dir, .glb extension).
    output_file = os.path.splitext(input_file)[0] + ".glb"

    print(f"[reexport] input  = {input_file}")
    print(f"[reexport] output = {output_file}")

    # Clean Blender state.
    bpy.ops.wm.read_factory_settings(use_empty=True)

    # Import the source .gltf (full scene).
    bpy.ops.import_scene.gltf(filepath=input_file)

    # Export to .glb single-file with everything embedded.
    # export_format=GLB packs binary buffers + textures in one container.
    bpy.ops.export_scene.gltf(
        filepath=output_file,
        export_format="GLB",
        export_image_format="AUTO",
        export_animations=True,
        export_apply=False,
        export_yup=True,
    )

    if not os.path.isfile(output_file):
        print(f"[reexport] ERROR output not produced: {output_file}", file=sys.stderr)
        sys.exit(2)

    size_kb = os.path.getsize(output_file) / 1024
    print(f"[reexport] OK {output_file} ({size_kb:.1f} KB)")


if __name__ == "__main__":
    main()
