using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UFMT.Core;
using UFMT.Helper;

namespace UFMT.AssetRegistry
{
    internal static class AssetRegistryBuilder
    {
        internal static void CreateAssetRegistry(string cookedAssetsPath, string ueVersionNumber, string outputFnGamePath, 
        string ueSkinsPackagePath, string ueEmotesPackagePath, string currentCosmeticPath)
        {
            Console.WriteLine("Creating AssetRegistry.bin!");
            // Just in case the user has the project folder named differently than the .uproject
            string cookedSkinsPath = Path.Combine(cookedAssetsPath, ueSkinsPackagePath.Substring(6, ueSkinsPackagePath.Length - 6).Replace("/", "\\"));
            string cookedEmotesPath = Path.Combine(cookedAssetsPath, ueEmotesPackagePath.Substring(6, ueEmotesPackagePath.Length - 6).Replace("/", "\\"));

            string[] customSkinFolders = Directory.GetDirectories(Path.Combine(cookedSkinsPath));
            string[] customEmoteFolders = Directory.GetDirectories(Path.Combine(cookedEmotesPath));
            List<string> cosmeticJsons = new();

            Console.WriteLine($"Searching for CIDs inside {cookedAssetsPath}...");
            foreach (string customSkinFolder in customSkinFolders)
            {
                string[] cid = Directory.GetFiles(customSkinFolder, "*.uasset");
                if (cid.Length > 0)
                {
                    string currentFoundCid = Path.GetFileNameWithoutExtension(cid[0]);

                    string json = System.Text.Encoding.UTF8.GetString(TemplateLoader.GetEmbeddedFile(ueVersionNumber, "RawUeAssets", "Cid.json"));
                    var root = JObject.Parse(json);
                    root["ObjectPath"] = root["ObjectPath"]!.Value<string>()!.Replace(
                        $"/Game/Athena/Items/Cosmetics/Characters/CID_Template.CID_Template",
                        $"/Game/Athena/Items/Cosmetics/Characters/{currentFoundCid}.{currentFoundCid}");
                    root["PackageName"] = root["PackageName"]!.Value<string>()!.Replace(
                        $"/Game/Athena/Items/Cosmetics/Characters/CID_Template",
                        $"/Game/Athena/Items/Cosmetics/Characters/{currentFoundCid}");
                    root["AssetName"] = root["PackageName"]!.Value<string>()!.Replace(
                        "CID_Template", currentFoundCid);
                    var tagAndValue = root["TagAndValue"]!.ToArray();
                    foreach (var tag in tagAndValue)
                    {
                        if (tag["Item1"]!.Value<string>() == "PrimaryAssetName")
                        {
                            tag["Item2"] = currentFoundCid;
                            break;
                        }
                    }
                    cosmeticJsons.Add(root.ToString(Formatting.Indented));
                    Console.WriteLine($"Added {currentFoundCid} to the AssetRegistry.bin!");
                }
                else
                {
                    Log.Warning($"no uasset files found inside {customSkinFolder}");
                }
            }

            Console.WriteLine($"Searching for EIDs inside {cookedAssetsPath}...");
            foreach (string customEmoteFolder in customEmoteFolders)
            {
                string[] eid = Directory.GetFiles(customEmoteFolder, "*.uasset");
                if (eid.Length > 0)
                {
                    string currentFoundEid = Path.GetFileNameWithoutExtension(eid[0]);

                    string json = System.Text.Encoding.UTF8.GetString(TemplateLoader.GetEmbeddedFile(ueVersionNumber, "RawUeAssets", "Eid.json"));
                    var root = JObject.Parse(json);
                    root["ObjectPath"] = root["ObjectPath"]!.Value<string>()!.Replace(
                        $"/Game/Athena/Items/Cosmetics/Dances/EID_Template.EID_Template",
                        $"/Game/Athena/Items/Cosmetics/Dances/{currentFoundEid}.{currentFoundEid}");
                    root["PackageName"] = root["PackageName"]!.Value<string>()!.Replace(
                        $"/Game/Athena/Items/Cosmetics/Dances/EID_Template",
                        $"/Game/Athena/Items/Cosmetics/Dances/{currentFoundEid}");
                    root["AssetName"] = root["PackageName"]!.Value<string>()!.Replace(
                        "EID_Template", currentFoundEid);
                    var tagAndValue = root["TagAndValue"]!.ToArray();
                    foreach (var tag in tagAndValue)
                    {
                        if (tag["Item1"]!.Value<string>() == "PrimaryAssetName")
                        {
                            tag["Item2"] = currentFoundEid;
                            break;
                        }
                    }
                    cosmeticJsons.Add(root.ToString(Formatting.Indented));
                    Console.WriteLine($"Added {currentFoundEid} to the AssetRegistry.bin!");
                }
                else
                {
                    Log.Warning($"no uasset files found inside {customEmoteFolder}");
                }
            }

            DeleteOldFnGamePath(currentCosmeticPath);

            if (!Directory.Exists(outputFnGamePath))
            {
                Directory.CreateDirectory(outputFnGamePath);
                Console.WriteLine($"Created {outputFnGamePath}!");
            }

            AssetRegistryHelper.Inject(TemplateLoader.GetEmbeddedFile(ueVersionNumber, "RawUeAssets", "AssetRegistry.bin"), cosmeticJsons.ToArray(), 
            Path.Combine(outputFnGamePath, "AssetRegistry312398E80AB6209B22CAA2EBAB2DB35B.bin"));
        }

        private static void DeleteOldFnGamePath(string skinPath)
        {
            string oldOutputPath = Path.Combine(skinPath, "Output", "FortniteGame");
            if (Directory.Exists(oldOutputPath)) Directory.Delete(oldOutputPath, true); //For OG users ;)
        }
    }
}
