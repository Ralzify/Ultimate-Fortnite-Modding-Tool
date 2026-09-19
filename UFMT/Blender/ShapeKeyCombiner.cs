using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using UFMT.Core;

namespace UFMT.Blender
{
    internal static class ShapeKeyCombiner
    {
        private static string CombineShapeKeysScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "PythonScripts", "Blender_CombineShapeKeys.py");
        internal static async Task<bool> CombineShapeKeys(string fbxFilePath)
        {
            if (!File.Exists(CombineShapeKeysScript))
            {
                Log.Error($"Failed to find python script! \"{CombineShapeKeysScript}\" does not exist or is not a python file!");
                return false;
            }

            if (!File.Exists(App.Settings.BlenderPath))
            {
                Log.Error($"Failed to find blender-launcher.exe! \"{App.Settings.BlenderPath}\" does not exist or is not a blender executable file!");
                return false;
            }

            if (!File.Exists(fbxFilePath))
            {
                Log.Error($"Failed to find character part head fbx! \"{fbxFilePath}\" does not exist or is not an .fbx file!");
                return false;
            }

            string blenderPath = App.Settings.BlenderPath;
            if (string.Equals(Path.GetFileName(blenderPath), "blender-launcher.exe", StringComparison.OrdinalIgnoreCase))
            {
                string consolePath = Path.Combine(Path.GetDirectoryName(blenderPath), "blender.exe");
                if (File.Exists(consolePath)) blenderPath = consolePath;
            }

            ProcessStartInfo psi = new ProcessStartInfo(blenderPath, $"-b --python-exit-code 1 --python \"{CombineShapeKeysScript}\" -- \"{fbxFilePath}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process blender = Process.Start(psi))
            {
                Task<string> stdoutTask = blender.StandardOutput.ReadToEndAsync();
                Task<string> stderrTask = blender.StandardError.ReadToEndAsync();
                await blender.WaitForExitAsync();
                string stdout = await stdoutTask;
                string stderr = await stderrTask;

                if (blender.ExitCode != 0)
                {
                    Log.Error($"Blender shape key combination failed for {fbxFilePath} with exit code {blender.ExitCode}:\n{stdout}\n{stderr}");
                    return false;
                }

                if (!File.Exists(fbxFilePath))
                {
                    Log.Error($"Failed to combine shape keys for {Path.GetFileName(fbxFilePath)}.");
                    Log.Error("Make sure you are using the correct Blender version and have all required extensions enabled!");
                    return false;
                }

                foreach (string line in stdout.Split('\n'))
                {
                    if (line.StartsWith("Skipping shape key combination:") || line.StartsWith("Combined "))
                        Console.WriteLine(line.TrimEnd());
                }
            }

            return true;
        }
    }
}
