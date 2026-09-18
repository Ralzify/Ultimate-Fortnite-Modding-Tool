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
        private static Func<string, string, (string maleAnim, string femaleAnim)>[] AnimationSortMethods = { CheckForCMMandCMF, CheckForMAndFEndings,
        CheckForFemaleContaining, CheckForMaleCMMAndMissingCMF, CheckForFemaleCMFAndMissingCMM, CheckForMaleContaining, CheckForMaleMContainAndFemaleFInsteadOfMContain};

        internal static (bool success, string maleAnimationPsaName, string femaleAnimationPsaName) GetAnimationPsaData(string animationsFolderPath)
        {
            string maleAnimation = null;
            string femaleAnimation = null;

            string[] animationFiles = Directory.GetFiles(animationsFolderPath, "*.psa");
            if (animationFiles.Length > 2)
            {
                Log.Error($"Animations folder contains more than 2 .psa files!");
                return (false, maleAnimation, femaleAnimation);
            }

            if (animationFiles.Length == 0)
            {
                Log.Error($"Animations folder doesn't contain any .psa files!");
                return (false, maleAnimation, femaleAnimation);
            }

            string animation1 = Path.GetFileNameWithoutExtension(animationFiles[0]);

            if (animationFiles.Length == 1)
            {
                Console.WriteLine($"Only 1 .psa file found in Animations folder. Both genders will use the same animation.");
                maleAnimation = animation1;
                femaleAnimation = animation1;
                return (true, maleAnimation, femaleAnimation);
            }

            string animation2 = Path.GetFileNameWithoutExtension(animationFiles[1]);

            foreach (Func<string, string, (string maleAnim, string femaleAnim)> method in AnimationSortMethods)
            {
                (maleAnimation, femaleAnimation) = method(animation1, animation2);
                if (maleAnimation != null && femaleAnimation != null) return (true, maleAnimation, femaleAnimation);

                (maleAnimation, femaleAnimation) = method(animation2, animation1);
                if (maleAnimation != null && femaleAnimation != null) return (true, maleAnimation, femaleAnimation);
            }

            Log.Error($"Cannot determine male and female animations for '{animation1}' and '{animation2}'. Ensure animations were not renamed after export!");
            return (false, maleAnimation, femaleAnimation);
        }

        internal static (string maleAnimationJson, string femaleAnimationJson) GetAnimationJsonData(string maleAnimationPsa, string femaleAnimationPsa, string animationsPath) 
        {
            string maleJson = string.Empty;
            string femaleJson = string.Empty;
            if (File.Exists(Path.Combine(animationsPath, Path.ChangeExtension(maleAnimationPsa, ".json")))) maleJson = Path.ChangeExtension(maleAnimationPsa, ".json");
            if (File.Exists(Path.Combine(animationsPath, Path.ChangeExtension(femaleAnimationPsa, ".json")))) femaleJson = Path.ChangeExtension(femaleAnimationPsa, ".json");
            return (maleJson, femaleJson);
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

        private static (string maleAnimation, string femaleAnimation) CheckForCMMandCMF(string animation1, string animation2)
        {
            if ((animation1.Contains("_CMM_") || animation1.EndsWith("_CMM")) && (animation2.Contains("_CMF_") || animation2.EndsWith("_CMF")))
            {
                Console.WriteLine("Matched gender by suffix/tag: Male ('CMM'), Female ('CMF').");
                return (animation1, animation2);
            }
            return (null, null);
        }

        private static (string maleAnimation, string femaleAnimation) CheckForMAndFEndings(string animation1, string animation2)
        {
            if (animation1.EndsWith("_M") && animation2.EndsWith("_F"))
            {
                Console.WriteLine("Matched gender by file suffix: Male ('_M'), Female ('_F').");
                return (animation1, animation2);
            }
            return (null, null);
        }

        private static (string maleAnimation, string femaleAnimation) CheckForFemaleContaining(string animation1, string animation2)
        {
            if (!animation1.Contains("Female") && animation2.Contains("Female"))
            {
                Console.WriteLine("Matched gender by keyword: Female ('Female'), Male (no 'Female').");
                return (animation1, animation2);
            }
            return (null, null);
        }

        private static (string maleAnimation, string femaleAnimation) CheckForMaleCMMAndMissingCMF(string animation1, string animation2)
        {
            if ((animation2.Contains("_CMF_") || animation2.EndsWith("CMF")) && !animation1.Contains("_CMF_") && !animation1.EndsWith("CMF"))
            {
                Console.WriteLine("Matched gender by partial tag: Female ('CMF'), Male (missing 'CMF').");
                return (animation1, animation2);
            }
            return (null, null);
        }

        private static (string maleAnimation, string femaleAnimation) CheckForFemaleCMFAndMissingCMM(string animation1, string animation2)
        {
            if ((animation1.Contains("_CMM_") || animation1.EndsWith("CMM")) && !animation2.Contains("_CMM_") && !animation2.EndsWith("CMM"))
            {
                Console.WriteLine("Matched gender by partial tag: Male ('CMM'), Female (missing 'CMM').");
                return (animation1, animation2);
            }
            return (null, null);
        }

        private static (string maleAnimation, string femaleAnimation) CheckForMaleContaining(string animation1, string animation2)
        {
            if (!animation2.Contains("Male") && animation1.Contains("Male"))
            {
                Console.WriteLine("Matched gender by keyword: Male ('Male'), Female (no 'Male').");
                return (animation1, animation2);
            }
            return (null, null);
        }

        private static (string maleAnimation, string femaleAnimation) CheckForMaleMContainAndFemaleFInsteadOfMContain(string animation1, string animation2)
        {
            if (animation1.Contains("_M_") && !animation2.Contains("_M_") && animation2.Contains("_F_") && !animation1.Contains("_F_"))
            {
                Console.WriteLine("Matched gender by infix tag: Male ('_M_'), Female ('_F_').");
                return (animation1, animation2);
            }
            return (null, null);
        }
    }
}
