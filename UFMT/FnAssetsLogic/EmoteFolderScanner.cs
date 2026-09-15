using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using UFMT.Core;
using UFMT.FnAssets;
using Windows.ApplicationModel.Store;

namespace UFMT.FnAssetsLogic
{
    internal class EmoteFolderScanner
    {
        internal static (bool success, string maleAnimationPsaFileName, string femaleAnimationPsaFileName, string maleAnimationJsonFileName, 
        string femaleAnimationJsonFileName)GetAnimationsData(string animationsFolderPath)
        {
            string maleAnimationFolderPath = Path.Combine(animationsFolderPath, "Male");
            string femaleAnimationFolderPath = Path.Combine(animationsFolderPath, "Female");

            string[] maleAnimationPsaFilePaths = Directory.GetFiles(maleAnimationFolderPath, "*.psa");
            string[] femaleAnimationPsaFilePaths = Directory.GetFiles(femaleAnimationFolderPath, "*.psa");

            if (maleAnimationPsaFilePaths.Length > 1) 
            {
                Log.Error($"Multiple .psa files found in \"{maleAnimationFolderPath}\"!");
                return (false, null, null, null, null);
            }

            if (femaleAnimationPsaFilePaths.Length > 1)
            {
                Log.Error($"Multiple .psa files found in \"{femaleAnimationFolderPath}\"!");
                return (false, null, null, null, null);
            }

            if (maleAnimationPsaFilePaths.Length == 0 && femaleAnimationPsaFilePaths.Length == 0)
            {
                Log.Error("No animations found for the emote (no .psa files found in Male or Female animation folders)!");
                return (false, null, null, null, null);
            }

            if (maleAnimationPsaFilePaths.Length == 0) Log.Warning($"No .psa files found in \"{maleAnimationFolderPath}\"");

            Log.Success($"Found {Path.GetFileName(maleAnimationPsaFilePaths[0])} in Male animation folder");
            Log.Success($"Found {Path.GetFileName(femaleAnimationPsaFilePaths[0])} in Female animation folder");

            string[] maleAnimationJsonFilePaths = Directory.GetFiles(maleAnimationFolderPath, "*.json");
            string[] femaleAnimationJsonFilePaths = Directory.GetFiles(femaleAnimationFolderPath, "*.json");

            if (maleAnimationJsonFilePaths.Length > 1)
            {
                Log.Error($"Multiple .json files in \"{maleAnimationFolderPath}\"!\nMake sure there is only 1 .json animation!");
                return (false, null, null, null, null);
            }
            if (maleAnimationJsonFilePaths.Length != 0)
            {
                Log.Success($"Found {Path.GetFileName(maleAnimationJsonFilePaths[0])} in Male animation folder");
            }
            else
            {
                maleAnimationJsonFilePaths = [string.Empty];
            }
            if (femaleAnimationJsonFilePaths.Length > 1)
            {
                Log.Error($"Multiple .json files in \"{femaleAnimationFolderPath}\"!\nMake sure there is only 1 .json animation!");
                return (false, null, null, null, null);
            }
            if (femaleAnimationJsonFilePaths.Length != 0)
            {
                Log.Success($"Found {Path.GetFileName(femaleAnimationJsonFilePaths[0])} in Female animation folder");
            }
            else
            {
                femaleAnimationJsonFilePaths = [string.Empty];
            }

            return (true, Path.GetFileName(maleAnimationPsaFilePaths[0]), Path.GetFileName(femaleAnimationPsaFilePaths[0]), Path.GetFileName(maleAnimationJsonFilePaths[0]),
            Path.GetFileName(femaleAnimationJsonFilePaths[0]));
        }

        internal static (bool, string) GetSoundData(string SoundFolderPath)
        {
            string[] WavFilePaths = Directory.GetFiles(SoundFolderPath);
            if (WavFilePaths.Length > 1)
            {
                Log.Error($"Multiple .wav files found in \"{SoundFolderPath}\"!");
                return (false, null);
            }
            if (WavFilePaths.Length == 0)
            {
                Log.Error($"No .wav files found in \"{SoundFolderPath}\"!");
                return (false, null);
            }

            Log.Success($"Found {Path.GetFileName(WavFilePaths[0])} in Sound folder");
            return (true, Path.GetFileName(WavFilePaths[0]));
        }
    }
}
