using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UFMT.Core;

namespace UFMT.UnrealEngine
{
    internal static class UnrealProcessRunner
    {
        private const string MeshCompatibilityArguments = "-ini:Engine:[SystemSettings]:Compat.MAX_GPUSKIN_BONES=75";
        private static string SkinPythonScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "PythonScripts", "UE_Import_Skin.py").Replace("\\", "/");
        private static string EmotePythonScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "PythonScripts", "UE_Import_Emote.py").Replace("\\", "/");
        internal static async Task<bool> LaunchUnreal(string unrealDataInJsonString, string ueProjectPath, string ueExecutablePath, string cosmeticType)
        {
            if (cosmeticType == "skin" && !UnrealPluginValidator.ValidatePhysicsImporter(ueProjectPath, ueExecutablePath))
                return false;

            string tempJsonPath = Path.Combine(Path.GetTempPath(), "ue_import_data.json");
            File.WriteAllText(tempJsonPath, unrealDataInJsonString, new System.Text.UTF8Encoding(false));
            Log.Test($"tempJsonPath is {tempJsonPath}");

            string scriptPath = cosmeticType == "skin" ? SkinPythonScriptPath : EmotePythonScriptPath;
            string arguments = $"\"{ueProjectPath}\" -run=PythonScriptCommandlet -script=\"{scriptPath}\" -NullRHI -NoWindow -unattended -stdout -FullStdOutLogOutput {MeshCompatibilityArguments}";

            Console.WriteLine($"Launching UE with args: {arguments}");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ueExecutablePath,
                Arguments = arguments,
                UseShellExecute = false, //This must be false for EnvironmentVariables to work
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false
            };

            startInfo.EnvironmentVariables["UFMT_JSON_PATH"] = tempJsonPath;
            Console.WriteLine("Launching unreal engine...");
            try
            {
                using var backup = cosmeticType == "skin"
                    ? UnrealSkinImportBackup.Prepare(ueProjectPath, unrealDataInJsonString) : null;
                bool succeeded = await RunProcess(startInfo, ueProjectPath, "import");
                if (succeeded) backup?.Complete();
                return succeeded;
            }
            catch (Exception ex)
            {
                Log.Error($"Could not prepare Unreal skin import: {ex.Message}. Export stopped.");
                return false;
            }
        }
        internal static async Task<bool> CookFiles(string ueProjectPath, string ueExecutablePath)
        {
            Console.WriteLine("Cooking newly created assets...");
            string arguments = $"\"{ueProjectPath}\" -run=Cook -TargetPlatform=WindowsNoEditor -unversioned -iterate -NullRHI -NoWindow -unattended -stdout -FullStdOutLogOutput {MeshCompatibilityArguments}";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ueExecutablePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (!await RunProcess(startInfo, ueProjectPath, "cook")) return false;
            Console.WriteLine("Cook done!");
            return true;
        }

        private static async Task<bool> RunProcess(ProcessStartInfo startInfo, string ueProjectPath, string operation)
        {
            try
            {
                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
                    Task<string> stderrTask = process.StandardError.ReadToEndAsync();
                    await Task.WhenAll(stdoutTask, stderrTask);
                    await process.WaitForExitAsync();
                    Console.WriteLine(stdoutTask.Result);
                    if (!string.IsNullOrWhiteSpace(stderrTask.Result)) Log.Error(stderrTask.Result);
                    if (process.ExitCode != 0)
                    {
                        Log.Error($"Unreal Engine {operation} failed with exit code {process.ExitCode}. Export stopped. Logs: {Path.Combine(Path.GetDirectoryName(ueProjectPath), "Saved", "Logs")}");
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Unreal Engine {operation} failed: {ex.Message}. Export stopped.");
                return false;
            }
        }
    }
}
