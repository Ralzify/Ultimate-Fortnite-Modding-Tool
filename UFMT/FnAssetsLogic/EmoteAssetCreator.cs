using Microsoft.UI.Xaml;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UAssetAPI;
using UAssetAPI.CustomVersions;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;
using UFMT.Core;
using UFMT.UI;
using UFMT.UnrealEngine;

namespace UFMT.FnAssets
{
    internal static class EmoteAssetCreator
    {
        internal static void CopyFilesFromUe(string contentFolderPath, DirectoryInfo cookedEmoteDirectory)
        {
            if (!Path.Exists(contentFolderPath)) Directory.CreateDirectory(contentFolderPath);
            foreach (DirectoryInfo subFolder in cookedEmoteDirectory.GetDirectories("*", SearchOption.AllDirectories))
            {
                string targetSubDir = subFolder.FullName.Replace(cookedEmoteDirectory.FullName, contentFolderPath);
                Log.Test($"target sub dir is {targetSubDir}");
                Directory.CreateDirectory(targetSubDir);

                foreach (FileInfo file in subFolder.GetFiles())
                {
                    file.CopyTo(Path.Combine(targetSubDir, file.Name), true);
                }
            }
            Log.Success($"Copied files from {cookedEmoteDirectory} to {contentFolderPath}");

        }

        internal static void CreateAnimationMontage(string OutputFnGameCurrentEmotePath, string animationName, float animationLength,
        FnVersion fnVersion, UeVersion ueVersion, string ueEmotesPackagePath, string codename)
        {
            try 
            {
                string montageName = $"{animationName}_M.uasset";
                string animationMontageUassetPath = Path.Combine(OutputFnGameCurrentEmotePath, "Animations", montageName);

                byte[] montageUassetBase64 = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "EmoteMontage.uasset");
                byte[] montageUexpBase64 = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "EmoteMontage.uexp");


                File.WriteAllBytes(animationMontageUassetPath, montageUassetBase64);
                File.WriteAllBytes(Path.ChangeExtension(animationMontageUassetPath, ".uexp"), montageUexpBase64);

                var animationMontageAsset = new UAsset(animationMontageUassetPath, ueVersion.UassetApiEngineVer);
                var exportData = animationMontageAsset.Exports;
                var export0 = (NormalExport)exportData[0];

                export0.ObjectName.Value.Value = Path.GetFileNameWithoutExtension(animationMontageUassetPath);

                var SequenceLength = (FloatPropertyData)export0["SequenceLength"];
                SequenceLength.Value = animationLength;

                var compositeSections = (ArrayPropertyData)export0["CompositeSections"];
                var defaultCompositeSection = (StructPropertyData)compositeSections.Value[0];
                var loopCompositeSection = (StructPropertyData)compositeSections.Value[1];
                var defaultCompositeSectionSegmentLength = (FloatPropertyData)defaultCompositeSection["SegmentLength"];
                var loopCompositeSectionSegmentLength = (FloatPropertyData)loopCompositeSection["SegmentLength"];
                defaultCompositeSectionSegmentLength.Value = animationLength;
                loopCompositeSectionSegmentLength.Value = animationLength;

                var slotAnimTracks = (ArrayPropertyData)export0["SlotAnimTracks"];
                var slotAnimTracks2 = (StructPropertyData)slotAnimTracks.Value[0];
                var animTrack = (StructPropertyData)slotAnimTracks2.Value[1];
                var animSegments = (ArrayPropertyData)animTrack.Value[0];
                var animSegments2 = (StructPropertyData)animSegments.Value[0];
                var animEndTime = (FloatPropertyData)animSegments2["AnimEndTime"];
                animEndTime.Value = animationLength;

                var notifies = (ArrayPropertyData)export0["Notifies"];

                var emoteSoundNotify = (StructPropertyData)notifies.Value[0];
                var emoteSoundNotifyDuration = (FloatPropertyData)emoteSoundNotify["Duration"];
                var emoteSoundNotifySegmentLength = (FloatPropertyData)emoteSoundNotify["SegmentLength"];
                emoteSoundNotifyDuration.Value = animationLength;
                emoteSoundNotifySegmentLength.Value = animationLength;
                var emoteSoundNotifyEndLink = (StructPropertyData)emoteSoundNotify["EndLink"];
                var emoteSoundNotifyEndLinkSegmentLength = (FloatPropertyData)emoteSoundNotifyEndLink["SegmentLength"];
                var emoteSoundNotifyEndLinkLinkValue = (FloatPropertyData)emoteSoundNotifyEndLink["LinkValue"];
                emoteSoundNotifyEndLinkSegmentLength.Value = animationLength;
                emoteSoundNotifyEndLinkLinkValue.Value = animationLength;

                var holsterWeaponNotify = (StructPropertyData)notifies.Value[1];
                var holsterWeaponNotifyDuration = (FloatPropertyData)holsterWeaponNotify["Duration"];
                var holsterWeaponNotifySegmentLength = (FloatPropertyData)holsterWeaponNotify["SegmentLength"];
                holsterWeaponNotifyDuration.Value = animationLength;
                holsterWeaponNotifySegmentLength.Value = animationLength;
                var holsterWeaponNotifyEndLink = (StructPropertyData)holsterWeaponNotify["EndLink"];
                var holsterWeaponNotifyEndLinkSegmentLength = (FloatPropertyData)holsterWeaponNotifyEndLink["SegmentLength"];
                var holsterWeaponNotifyEndLinkLinkValue = (FloatPropertyData)holsterWeaponNotifyEndLink["LinkValue"];
                holsterWeaponNotifyEndLinkSegmentLength.Value = animationLength;
                holsterWeaponNotifyEndLinkLinkValue.Value = animationLength;

                var importData = animationMontageAsset.Imports;
                importData[2].ObjectName.Value.Value = montageName;
                importData[9].ObjectName.Value.Value = $"{ueEmotesPackagePath}/{codename}/{animationName}";

                animationMontageAsset.Write(animationMontageUassetPath);
                Log.Success($"Succesfully edited {Path.GetFileNameWithoutExtension(animationMontageUassetPath)}");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}
