using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using UFMT.Core;

namespace UFMT.UnrealEngine
{
    internal sealed class UnrealSkinImportBackup : IDisposable
    {
        private readonly string backupRoot;
        private readonly List<(string Original, string Backup)> staged = new();
        private bool completed;

        private UnrealSkinImportBackup(string backupRoot) => this.backupRoot = backupRoot;

        internal static UnrealSkinImportBackup Prepare(string projectPath, string json)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            string package = document.RootElement.GetProperty("UeSkinsPackagePath").GetString();
            string codename = document.RootElement.GetProperty("Codename").GetString();
            string cid = document.RootElement.GetProperty("CID").GetString();
            if (package == null || !package.StartsWith("/Game/", StringComparison.Ordinal))
                throw new InvalidDataException("Skin package path must start with /Game/.");

            string[] components = package.Substring(6).Split('/');
            foreach (string component in components) ValidateComponent(component);
            ValidateComponent(codename);
            ValidateComponent(cid);

            string projectRoot = Path.GetDirectoryName(Path.GetFullPath(projectPath));
            string contentRoot = Path.Combine(projectRoot, "Content");
            string skinRoot = Path.GetFullPath(Path.Combine(contentRoot, Path.Combine(components), codename));
            RequireChild(contentRoot, skinRoot);
            string backupRoot = Path.Combine(projectRoot, "Saved", "UFMT", "ImportBackups", Guid.NewGuid().ToString("N"));
            RequireChild(projectRoot, backupRoot);

            var targets = new List<string>();
            foreach (string folder in new[] { "Meshes", "Materials", "Textures" })
                targets.Add(Path.Combine(skinRoot, folder));
            foreach (string extension in new[] { ".uasset", ".uexp", ".ubulk" })
                targets.Add(Path.Combine(skinRoot, cid + extension));

            RejectLinks(backupRoot);
            foreach (string target in targets)
            {
                RequireChild(skinRoot, target);
                RejectLinks(target);
                if (Directory.Exists(target)) RejectTreeLinks(target);
            }

            var backup = new UnrealSkinImportBackup(backupRoot);
            try
            {
                foreach (string target in targets)
                {
                    string saved = Path.Combine(backupRoot, "Previous", Path.GetFileName(target));
                    MoveIfExists(target, saved);
                    backup.staged.Add((target, saved));
                }
                Console.WriteLine($"Prepared skin assets for import. Recovery backup: {backupRoot}");
                return backup;
            }
            catch
            {
                backup.Dispose();
                throw;
            }
        }

        internal void Complete()
        {
            completed = true;
            try
            {
                if (Directory.Exists(backupRoot))
                {
                    RejectLinks(backupRoot);
                    RejectTreeLinks(backupRoot);
                    Directory.Delete(backupRoot, recursive: true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Import succeeded; previous assets retained at {backupRoot}: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (completed) return;
            completed = true;
            foreach (var entry in staged)
            {
                try
                {
                    RejectLinks(entry.Original);
                    RejectLinks(entry.Backup);
                    string failed = Path.Combine(backupRoot, "Failed", Path.GetFileName(entry.Original));
                    RejectLinks(failed);
                    MoveIfExists(entry.Original, failed);
                    MoveIfExists(entry.Backup, entry.Original);
                }
                catch (Exception ex)
                {
                    Log.Error($"Could not restore {entry.Original}: {ex.Message}. Recovery files: {backupRoot}");
                }
            }
            Console.WriteLine($"Skin import failed; previous assets restored where possible. Recovery files: {backupRoot}");
        }

        private static void ValidateComponent(string component)
        {
            if (string.IsNullOrEmpty(component) || !Regex.IsMatch(component, @"\A[\p{L}\p{N}_]+\z") ||
                Regex.IsMatch(component, @"\A(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])\z", RegexOptions.IgnoreCase))
                throw new InvalidDataException($"Invalid Unreal asset path component: '{component}'.");
        }

        private static void RequireChild(string parent, string child)
        {
            string prefix = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parent)) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(child).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Skin import path is outside its expected directory.");
        }

        private static void RejectLinks(string path)
        {
            for (string current = path; current != null; current = Path.GetDirectoryName(current))
                if (Path.Exists(current) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException($"Skin import cannot move assets through a linked path: {current}");
        }

        private static void RejectTreeLinks(string directory)
        {
            foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
            {
                FileAttributes attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException($"Skin import backup contains a linked path: {entry}");
                if ((attributes & FileAttributes.Directory) != 0) RejectTreeLinks(entry);
            }
        }

        private static void MoveIfExists(string source, string destination)
        {
            if (!Path.Exists(source)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            if (Directory.Exists(source)) Directory.Move(source, destination);
            else File.Move(source, destination);
        }
    }
}
