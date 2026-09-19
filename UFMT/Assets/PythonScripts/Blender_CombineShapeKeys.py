import sys
import os
import traceback
import bpy

def log_error(msg):
    local_app_data = os.getenv('LOCALAPPDATA') or os.path.expanduser('~')
    log_dir = os.path.join(local_app_data, 'UFMT')
    os.makedirs(log_dir, exist_ok=True)
    log_file_path = os.path.join(log_dir, 'python_combineshapekeys_log.txt')
    with open(log_file_path, "a", encoding="utf-8") as f:
        f.write(msg + "\n")


def main():
    argv = sys.argv
    if "--" in argv:
        args = argv[argv.index("--") + 1:]
    else:
        args = []

    if len(args) < 1:
        raise ValueError("Missing FBX file argument.")

    fbx_file_path = args[0]

    # Clean scene without resetting factory add-ons
    if bpy.ops.object.select_all.poll():
        bpy.ops.object.select_all(action="SELECT")
        bpy.ops.object.delete()

    # Import FBX
    # bpy.ops creates wrappers even for unregistered operators, so hasattr is
    # not a reliable availability check on older Blender versions.
    try:
        bpy.ops.wm.fbx_import.get_rna_type()
        import_fbx = bpy.ops.wm.fbx_import
    except AttributeError:
        import_fbx = bpy.ops.import_scene.fbx
    if 'FINISHED' not in import_fbx(filepath=fbx_file_path):
        raise RuntimeError(f"Failed to import FBX: {fbx_file_path}")

    meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    if not meshes:
        raise RuntimeError(f"No mesh found in: {fbx_file_path}")

    meshes_with_shape_keys = [obj for obj in meshes if obj.data.shape_keys]
    if not meshes_with_shape_keys:
        print(f"Skipping shape key combination: no shape keys in {fbx_file_path}. FBX left unchanged.", flush=True)
        return

    shape_key_combinations = {
        "C_glabella_down_pose": {"browDownL": 0.75, "browDownR": 0.75, "browLateralL": 1.0, "browLateralR": 1.0},
        "tongue_up_pose": {"tongueUp": 1.0},
        "phoneme_mbp_pose": {"mouthLowerLipTowardsTeethL": 0.5, "mouthLowerLipTowardsTeethR": 0.5, "mouthUpperLipTowardsTeethL": 0.5, "mouthUpperLipTowardsTeethR": 0.5},
        "L_upper_lip_lower_pose": {"mouthUpperLipTowardsTeethL": 1.0},
        "R_blink_pose": {"eyeBlinkR": 1.0},
        "R_wide_pose": {"eyeWidenR": 1.0},
        "look_right_pose": {"eyeLookRightL": 1.0, "eyeLookRightR": 1.0},
        "jaw_up_pose": {"jawChinRaiseDL": 0.5, "jawChinRaiseDR": 0.5},
        "L_squeeze_pose": {"eyeCheekRaiseL": 1.0, "noseWrinkleL": 0.5},
        "R_brow_up_pose": {"browRaiseInR": 1.0, "browRaiseOuterR": 0.5},
        "look_left_pose": {"eyeLookLeftL": 1.0, "eyeLookLeftR": 1.0},
        "tongue_back_pose": {"tongueIn": 1.0},
        "R_squint_inner_pose": {"eyeSquintInnerR": 1.0},
        "look_up_pose": {"eyeLookUpL": 1.0, "eyeLookUpR": 1.0},
        "phoneme_fv_pose": {"jawChinRaiseUL": 0.3, "jawChinRaiseUR": 0.3, "mouthCornerWideL": 1.0, "mouthCornerWideR": 1.0, "mouthLowerLipBiteL": 1.0, "mouthLowerLipBiteR": 1.0, "mouthLowerLipTowardsTeethL": 0.2, "mouthLowerLipTowardsTeethR": 0.2},
        "jaw_back_pose": {"jawBack": 1.0},
        "R_smile_pose": {"mouthCornerPullR": 0.65, "mouthCornerWideR": 1.0, "mouthUpperLipTowardsTeethR": 0.5},
        "R_lip_corner_narrow_pose": {"mouthCornerNarrowR": 1.0},
        "eye_convergence_pose": {"eyeLookLeftR": 0.6, "eyeLookRightL": 0.6},
        "R_brow_down_pose": {"browDownR": 1.0},
        "R_frown_pose": {"mouthCornerWideR": 1.0, "mouthLowerLipTowardsTeethR": 1.0, "mouthStretchR": 1.0},
        "L_lower_lip_down_pose": {"mouthLowerLipDepressL": 1.0, "mouthLowerLipTowardsTeethR": 0.5},
        "L_blink_pose": {"eyeBlinkL": 1.0},
        "L_lip_corner_narrow_pose": {"mouthCornerNarrowR": 1.0},
        "look_down_pose": {"eyeWidenR": 0.5, "eyeWidenL": 0.5, "eyeSquintInnerL": 1.0, "eyeSquintInnerR": 1.0, "eyeLookDownL": 1.0, "eyeLookDownR": 1.0},
        "R_upper_lip_raiser_pose": {"mouthUpperLipRaiseR": 1.0},
        "phoneme_oo_pose": {"jawOpen": 0.25, "mouthCornerNarrowL": 0.5, "mouthCornerNarrowR": 0.5, "mouthLipsPurseDL": 1.0, "mouthLipsPurseDR": 1.0, "mouthLipsPurseUL": 1.0, "mouthLipsPurseUR": 1.0},
        "R_brow_squeeze_in_pose": {"browDownR": 0.3, "browLateralR": 1.0},
        "jaw_left_pose": {"jawLeft": 1.0},
        "L_lip_corner_wide_pose": {"mouthCornerWideL": 1.0},
        "C_nose_wrinkler_pose": {"noseWrinkleL": 0.5, "noseWrinkleR": 0.5, "browDownL": 0.5, "browDownR": 0.5, "browLateralL": 1.0, "browLateralR": 1.0, "noseNasolabialDeepenL": 1.0, "noseNasolabialDeepenR": 1.0},
        "R_lower_lip_up_pose": {"mouthLowerLipTowardsTeethR": 1.0},
        "jaw_open_pose": {"jawOpen": 1.0},
        "L_brow_up_pose": {"browRaiseInL": 1.0, "browRaiseOuterL": 0.5},
        "R_lip_corner_wide_pose": {"mouthCornerWideR": 1.0},
        "L_lower_lip_up_pose": {"mouthLowerLipTowardsTeethL": 1.0},
        "R_lower_lip_down_pose": {"mouthLowerLipDepressR": 1.0, "mouthLowerLipTowardsTeethL": 0.5},
        "L_upper_lip_raiser_pose": {"mouthUpperLipRaiseL": 1.0},
        "R_cheek_up_pose": {"eyeCheekRaiseR": 0.75},
        "L_brow_down_pose": {"browDownL": 1.0},
        "jaw_forward_pose": {"jawFwd": 1.0},
        "L_squint_inner_pose": {"eyeSquintInnerL": 1.0},
        "tongue_down_pose": {"tongueDown": 1.0},
        "R_upper_lip_lower_pose": {"mouthUpperLipTowardsTeethR": 1.0},
        "L_brow_squeeze_in_pose": {"browDownL": 0.3, "browLateralL": 1.0},
        "jaw_right_pose": {"jawRight": 1.0},
        "mouth_down_pose": {"mouthDown": 1.0},
        "C_glabella_up_pose": {"browRaiseInL": 0.5, "browRaiseInR": 0.5},
        "phoneme_ch_pose": {"mouthLowerLipDepressL": 0.5, "mouthLowerLipDepressR": 0.5, "mouthUpperLipRaiseL": 0.6, "mouthUpperLipRaiseR": 0.6, "mouthCornerNarrowL": 1.0, "mouthCornerNarrowR": 1.0},
        "L_frown_pose": {"mouthCornerWideL": 1.0, "mouthLowerLipTowardsTeethL": 1.0, "mouthStretchL": 1.0},
        "R_squeeze_pose": {"eyeCheekRaiseR": 1.0, "noseWrinkleR": 0.5},
        "tongue_forward_pose": {"tongueOut": 1.0},
        "L_upperLidDown_pose": {"eyeCheekRaiseL": 1.0, "eyeLookDownL": 1.0, "eyeLookUpL": 1.0, "eyeSquintInnerL": 0.5},
        "R_upperLidDown_pose": {"eyeCheekRaiseR": 1.0, "eyeLookDownR": 1.0, "eyeLookUpR": 1.0, "eyeSquintInnerR": 0.5},
        "L_wide_pose": {"eyeWidenL": 1.0},
        "L_smile_pose": {"mouthCornerPullL": 0.65, "mouthCornerWideL": 1.0, "mouthUpperLipTowardsTeethL": 0.5},
        "mouth_up_pose": {"mouthUp": 1.0},
        "L_cheek_up_pose": {"eyeCheekRaiseL": 0.75}
    }

    def combine_shape_keys(target_obj, new_shape_key_name, shape_key_data):
        shape_keys = target_obj.data.shape_keys
        if new_shape_key_name in shape_keys.key_blocks:
            return False

        source_keys = [(shape_keys.key_blocks[name], value)
                       for name, value in shape_key_data.items()
                       if name in shape_keys.key_blocks]
        if not source_keys:
            return False

        basis = shape_keys.reference_key
        new_shape = target_obj.shape_key_add(name=new_shape_key_name, from_mix=False)
        new_shape.interpolation = 'KEY_LINEAR'
        new_shape.value = 0.0

        for key_block, value in source_keys:
            for i in range(len(target_obj.data.vertices)):
                new_shape.data[i].co += (key_block.data[i].co - basis.data[i].co) * value
        return True

    combined_count = 0
    for obj in meshes_with_shape_keys:
        for new_shape_key, data in shape_key_combinations.items():
            if combine_shape_keys(obj, new_shape_key, data):
                combined_count += 1

    if not combined_count:
        print(f"Skipping shape key combination: no new legacy facial poses needed in {fbx_file_path}. FBX left unchanged.", flush=True)
        return

    # Export using Better FBX Exporter
    result = bpy.ops.better_export.fbx(
        filepath=fbx_file_path,
        use_optimize_for_game_engine=False,
        use_edge_crease=False,
    )
    if 'FINISHED' not in result:
        raise RuntimeError(f"Failed to export combined shape keys: {fbx_file_path}")
    print(f"Combined {combined_count} legacy facial shape keys in {fbx_file_path}.", flush=True)


if __name__ == '__main__':
    try:
        main()
    except Exception:
        err_trace = traceback.format_exc()
        print(err_trace, file=sys.stderr, flush=True)
        try:
            log_error(f"--- ERROR OCCURRED --- \n{err_trace}\n")
        except OSError as log_exception:
            print(f"Could not write Blender error log: {log_exception}", file=sys.stderr, flush=True)
        sys.exit(1)
