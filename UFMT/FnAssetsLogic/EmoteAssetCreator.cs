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

            var sequenceLength = (FloatPropertyData)export0["SequenceLength"];
            sequenceLength.Value = animationLength;

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

        internal static void CreateSoundCues(string OutputFnGameCurrentEmotePath, FnVersion fnVersion, UeVersion ueVersion, string ueEmotesPackagePath, string codename)
        {
            string soundCueName = $"SC_EmoteMusic_{codename}.uasset";
            string soundCue3PName = $"SC_EmoteMusic3P_{codename}.uasset";
            string soundCueUassetPath = Path.Combine(OutputFnGameCurrentEmotePath, "Sound", soundCueName);
            string soundCue3PUassetPath = Path.Combine(OutputFnGameCurrentEmotePath, "Sound", soundCue3PName);

            byte[] soundCueUassetBase64 = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "EmoteSoundCue.uasset");
            byte[] soundCueUexpBase64 = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "EmoteSoundCue.uexp");
            byte[] soundCue3PUassetBase64 = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "EmoteSoundCue3P.uasset");
            byte[] soundCue3PUexpBase64 = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "EmoteSoundCue3P.uexp");


            File.WriteAllBytes(soundCueUassetPath, soundCueUassetBase64);
            File.WriteAllBytes(Path.ChangeExtension(soundCueUassetPath, ".uexp"), soundCueUexpBase64);
            File.WriteAllBytes(soundCue3PUassetPath, soundCue3PUassetBase64);
            File.WriteAllBytes(Path.ChangeExtension(soundCue3PUassetPath, ".uexp"), soundCue3PUexpBase64);

            float soundWaveLength =
            ((FloatPropertyData)((NormalExport)new UAsset(Path.Combine(OutputFnGameCurrentEmotePath, "Sound", $"{codename}_Sound.uasset"), ueVersion.UassetApiEngineVer).Exports[0])["Duration"]).Value;

            var soundCueAsset = new UAsset(soundCueUassetPath, ueVersion.UassetApiEngineVer);
            var exportData = soundCueAsset.Exports;
            var export0 = (NormalExport)exportData[0];

            export0.ObjectName.Value.Value = Path.GetFileNameWithoutExtension(soundCueName);
            var duration = (FloatPropertyData)export0["Duration"];
            duration.Value = soundWaveLength;

            var export1 = (NormalExport)exportData[1];
            var soundWaveAssetPtr = (SoftObjectPropertyData)export1["SoundWaveAssetPtr"];
            soundWaveAssetPtr.Value.AssetPath.AssetName.Value.Value = $"{ueEmotesPackagePath}/{codename}/{codename}_Sound.{codename}_Sound";

            var importData = soundCueAsset.Imports;
            importData[4].ObjectName.Value.Value = $"{ueEmotesPackagePath}/{codename}/{codename}_Sound";
            importData[10].ObjectName.Value.Value = $"{codename}_Sound";
            soundCueAsset.Write(soundCueUassetPath);

            var soundCue3PAsset = new UAsset(soundCue3PUassetPath, ueVersion.UassetApiEngineVer);
            exportData = soundCue3PAsset.Exports;
            export0 = (NormalExport)exportData[0];
            export0.ObjectName.Value.Value = Path.GetFileNameWithoutExtension(soundCue3PName);
            duration = (FloatPropertyData)export0["Duration"];
            duration.Value = soundWaveLength;

            export1 = (NormalExport)exportData[1];
            soundWaveAssetPtr = (SoftObjectPropertyData)export1["SoundWaveAssetPtr"];
            soundWaveAssetPtr.Value.AssetPath.AssetName.Value.Value = $"{ueEmotesPackagePath}/{codename}/{codename}_Sound.{codename}_Sound";

            importData = soundCue3PAsset.Imports;
            importData[5].ObjectName.Value.Value = $"{ueEmotesPackagePath}/{codename}/{codename}_Sound";
            importData[12].ObjectName.Value.Value = $"{codename}_Sound";
            soundCue3PAsset.Write(soundCue3PUassetPath);
        }
    }
}
