import bpy
import sys
import os

psa_path = sys.argv[-2]
export_path = sys.argv[-1]

armature = bpy.data.objects.get("Armature")

if armature:
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)

    bpy.ops.psa.import_file(filepath=psa_path)
    bpy.context.view_layer.update()

    if bpy.data.actions:
        if not armature.animation_data:
            armature.animation_data_create()

        action = bpy.data.actions[-1]
        armature.animation_data.action = action

        bpy.context.scene.frame_end = int(action.frame_range[1])

    bpy.ops.better_export.fbx(
        filepath=export_path,
        my_scale=0.01,
        use_optimize_for_game_engine = False,
        use_edge_crease = False,
    )
    
else:
    log_dir = os.path.join(os.getenv("LOCALAPPDATA"), "UFMT")
    os.makedirs(log_dir, exist_ok=True)
    with open(os.path.join(log_dir, "python_psa_to_fbx_error.txt"), "w") as f:
        f.write("Error: Armature not found in the template .blend file!")