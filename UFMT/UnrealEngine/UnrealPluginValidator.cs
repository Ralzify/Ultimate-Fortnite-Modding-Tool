using System;
using System.IO;
using System.Text.Json;
using UFMT.Core;

namespace UFMT.UnrealEngine
{
    internal static class UnrealPluginValidator
    {
        internal static bool ValidatePhysicsImporter(string ueProjectPath, string ueExecutablePath)
        {
            string pluginDirectory = Path.Combine(Path.GetDirectoryName(ueProjectPath), "Plugins", "PhysicsImporter");
            string pluginManifestPath = Path.Combine(pluginDirectory, "Binaries", "Win64", "UE4Editor.modules");
            string engineManifestPath = Path.Combine(Path.GetDirectoryName(ueExecutablePath), "UE4Editor.modules");

            try
            {
                if (!File.Exists(Path.Combine(pluginDirectory, "PhysicsImporter.uplugin")) || !File.Exists(pluginManifestPath))
                    throw new InvalidDataException($"PhysicsImporter is missing its plugin descriptor or compiled module manifest in {pluginDirectory}.");

                using JsonDocument engineManifest = JsonDocument.Parse(File.ReadAllText(engineManifestPath));
                using JsonDocument pluginManifest = JsonDocument.Parse(File.ReadAllText(pluginManifestPath));
                string engineBuildId = engineManifest.RootElement.GetProperty("BuildId").GetString();
                string pluginBuildId = pluginManifest.RootElement.GetProperty("BuildId").GetString();
                if (string.IsNullOrEmpty(engineBuildId) || !string.Equals(engineBuildId, pluginBuildId, StringComparison.Ordinal))
                    throw new InvalidDataException($"PhysicsImporter was built for a different Unreal Engine build. Engine BuildId: {engineBuildId}; plugin BuildId: {pluginBuildId}.");

                string moduleName = pluginManifest.RootElement.GetProperty("Modules").GetProperty("PhysicsImporter").GetString();
                if (string.IsNullOrEmpty(moduleName) || !File.Exists(Path.Combine(Path.GetDirectoryName(pluginManifestPath), moduleName)))
                    throw new InvalidDataException($"The compiled PhysicsImporter DLL is missing from {Path.GetDirectoryName(pluginManifestPath)}.");

                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is JsonException || ex is InvalidOperationException || ex is System.Collections.Generic.KeyNotFoundException)
            {
                Log.Error($"Unreal export stopped: {ex.Message}");
                Log.Error($"Install or rebuild PhysicsImporter for the engine at {ueExecutablePath}. Plugin folder: {pluginDirectory}");
                return false;
            }
        }
    }
}
