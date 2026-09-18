using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UFMT.Core;
using UFMT.UI;

namespace UFMT.FnAssetsLogic
{
    internal static class EmoteValidator
    {
        internal static bool ValidateAfterPathChange(string currentEmoteFolderPath, EmoteData currentEmote)
        {
            if (currentEmote == null)
            {
                Log.Error("Current emote was null when trying to validate it!");
                return false;
            }

            if (currentEmoteFolderPath == string.Empty)
            {
                Log.Error("Current emote path is empty!");
                return false;
            }

            if (!Directory.Exists(currentEmoteFolderPath))
            {
                Log.Error($"\"{currentEmoteFolderPath}\" does not exist or is not a directory!");
                return false;
            }

            string codename = Path.GetFileName(currentEmoteFolderPath);
            if (codename.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '_'))
            {
                Log.Error($"{codename} contains invalid characters; only basic English letters (A-Z), numbers, and underscores are allowed.");
                return false;
            }

            string sourcePath = Path.Combine(currentEmoteFolderPath, "Source");
            if (!Directory.Exists(sourcePath))
            {
                Log.Error($"Cannot find Source folder inside \"{currentEmoteFolderPath}\"");
                return false;
            }

            string animationsPath = Path.Combine(sourcePath, "Animations");
            if (!Directory.Exists(animationsPath))
            {
                Log.Error($"Cannot find Animations folder inside \"{sourcePath}\"");
                return false;
            }

            string iconsPath = Path.Combine(sourcePath, "Icons");
            if (!Directory.Exists(iconsPath))
            {
                Log.Error($"Cannot find Icons folder inside \"{sourcePath}\"");
                return false;
            }

            string soundPath = Path.Combine(sourcePath, "Sound");
            if (!Directory.Exists(soundPath))
            {
                Log.Error($"Cannot find Sound folder inside \"{sourcePath}\"");
                return false;
            }

            currentEmote.Path = currentEmoteFolderPath;
            currentEmote.SourcePath = sourcePath;
            currentEmote.AnimationsPath = animationsPath;
            currentEmote.IconsPath = iconsPath;
            currentEmote.SoundPath = soundPath;
            currentEmote.Codename = codename;
            return true;
        }
    }
}
