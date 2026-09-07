using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UFMT.Core;
using UFMT.FnAssets;
using Windows.ApplicationModel.Store;

namespace UFMT.FnAssetsLogic
{
    internal class EmoteFolderScanner
    {
        internal static (bool success, string maleAnimationPsaFileName, string femaleAnimationPsaFileName) GetAnimationsData(string animationsPath)
        {
            string maleAnimationFolderPath = Path.Combine(animationsPath, "Male");
            string femaleAnimationFolderPath = Path.Combine(animationsPath, "Female");

            string[] maleAnimationPsaFileNames = Directory.GetFiles(maleAnimationFolderPath, "*.psa");
            string[] femaleAnimationPsaFileNames = Directory.GetFiles(femaleAnimationFolderPath, "*.psa");

            if (maleAnimationPsaFileNames.Length > 1) 
            {
                Log.Error($"Multiple .psa files found in \"{maleAnimationFolderPath}\"!");
                return (false, null, null);
            }

            if (femaleAnimationPsaFileNames.Length > 1)
            {
                Log.Error($"Multiple .psa files found in \"{femaleAnimationFolderPath}\"!");
                return (false, null, null);
            }

            if (maleAnimationPsaFileNames.Length == 0 && femaleAnimationPsaFileNames.Length == 0)
            {
                Log.Error("No animations found for the emote (no .psa files found in Male or Female animation folders)!");
                return (false, null, null);
            }

            if (maleAnimationPsaFileNames.Length == 0) Log.Warning($"No .psa files found in \"{maleAnimationFolderPath}\"");

            Log.Success($"Found {Path.GetFileName(maleAnimationPsaFileNames[0])} in Male animation folder");
            Log.Success($"Found {Path.GetFileName(femaleAnimationPsaFileNames[0])} in Female animation folder");

            return (true, Path.GetFileName(maleAnimationPsaFileNames[0]), Path.GetFileName(femaleAnimationPsaFileNames[0]));
        }
    }
}
