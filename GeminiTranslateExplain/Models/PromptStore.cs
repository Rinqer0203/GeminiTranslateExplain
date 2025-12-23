using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace GeminiTranslateExplain.Models
{
    public sealed class PromptStore
    {
        private const string PromptFileExtension = ".md";
        private const string DefaultPromptId = "default";
        private const string DefaultPromptName = "デフォルト";
        private const string PromptFolderName = "prompts";
        private const string PromptNameCommentPrefix = "<!-- prompt:name=";
        private const string PromptNameCommentSuffix = " -->";

        public static PromptStore Instance { get; } = new PromptStore();

        public ObservableCollection<PromptProfile> PromptProfiles { get; } = new();

        public string SelectedPromptId { get; private set; } = string.Empty;

        private PromptStore()
        {
            LoadPromptProfiles();
            SelectedPromptId = AppConfig.Instance.SelectedPromptId;
            EnsureSelectedProfile();
            SyncSelectedPromptIdToConfig();
        }

        public PromptProfile GetSelectedPromptProfile()
        {
            if (PromptProfiles.Count == 0)
            {
                var fallback = CreateFallbackProfile();
                PromptProfiles.Add(fallback);
                SelectedPromptId = fallback.Id;
                return fallback;
            }

            var selected = PromptProfiles.FirstOrDefault(p => p.Id == SelectedPromptId);
            if (selected != null)
                return selected;

            SelectedPromptId = PromptProfiles[0].Id;
            return PromptProfiles[0];
        }

        public void SetSelectedPromptProfile(PromptProfile profile)
        {
            if (profile == null)
                return;

            SelectedPromptId = profile.Id;
            SyncSelectedPromptIdToConfig();
        }

        public PromptProfile AddPromptProfile(string name)
        {
            var id = Guid.NewGuid().ToString("N");
            var profile = new PromptProfile
            {
                Id = id,
                Name = name,
                Instruction = string.Empty,
                FilePath = GetPromptFilePath(id)
            };

            PromptProfiles.Add(profile);
            SaveProfile(profile);
            return profile;
        }

        public bool RemovePromptProfile(PromptProfile profile)
        {
            if (profile == null)
                return false;

            PromptProfiles.Remove(profile);
            TryDeletePromptFile(profile);

            if (SelectedPromptId == profile.Id)
            {
                SelectedPromptId = PromptProfiles.Count > 0 ? PromptProfiles[0].Id : string.Empty;
                SyncSelectedPromptIdToConfig();
            }

            return true;
        }

        public void SaveAllProfiles()
        {
            foreach (var profile in PromptProfiles)
            {
                SaveProfile(profile);
            }
            SyncSelectedPromptIdToConfig();
        }

        public void SaveProfile(PromptProfile profile)
        {
            if (string.IsNullOrWhiteSpace(profile.FilePath))
                return;

            var directory = Path.GetDirectoryName(profile.FilePath);
            if (string.IsNullOrWhiteSpace(directory) || !EnsurePromptDirectory(directory))
                return;

            var content = BuildPromptFileContent(profile);
            try
            {
                File.WriteAllText(profile.FilePath, content, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                ShowFileError($"プロンプトファイルの保存に失敗しました。\n{profile.FilePath}", ex);
            }
        }

        private void LoadPromptProfiles()
        {
            var baseDir = AppContext.BaseDirectory;
            var promptDir = GetPromptDirectoryPath(baseDir);
            if (!EnsurePromptDirectory(promptDir))
                return;

            var files = Directory.GetFiles(promptDir, $"*{PromptFileExtension}");

            if (files.Length == 0)
            {
                var defaultProfile = CreateDefaultPromptFile();
                if (defaultProfile != null)
                    PromptProfiles.Add(defaultProfile);
                return;
            }

            foreach (var file in files.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var profile = LoadProfileFromFile(file);
                if (profile != null)
                    PromptProfiles.Add(profile);
            }
        }

        private void EnsureSelectedProfile()
        {
            if (string.IsNullOrWhiteSpace(SelectedPromptId))
            {
                if (PromptProfiles.Count > 0)
                {
                    SelectedPromptId = PromptProfiles[0].Id;
                    SyncSelectedPromptIdToConfig();
                }
                return;
            }

            if (PromptProfiles.Any(p => p.Id == SelectedPromptId))
                return;

            if (PromptProfiles.Count > 0)
            {
                SelectedPromptId = PromptProfiles[0].Id;
                SyncSelectedPromptIdToConfig();
            }
        }

        private PromptProfile? LoadProfileFromFile(string path)
        {
            string text;
            try
            {
                text = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                ShowFileError($"プロンプトファイルの読み込みに失敗しました。\n{path}", ex);
                return null;
            }

            var id = ExtractIdFromFilePath(path);
            if (string.IsNullOrWhiteSpace(id))
                return null;

            var name = id;
            var instruction = text;
            if (TryExtractNameAndInstruction(text, out var extractedName, out var extractedInstruction))
            {
                name = extractedName;
                instruction = extractedInstruction;
            }

            var profile = new PromptProfile
            {
                Id = id,
                Name = name,
                Instruction = instruction,
                FilePath = path
            };

            return profile;
        }

        private PromptProfile? CreateDefaultPromptFile()
        {
            var instruction = LoadDefaultPromptText();
            if (string.IsNullOrWhiteSpace(instruction))
                return null;

            var profile = new PromptProfile
            {
                Id = DefaultPromptId,
                Name = DefaultPromptName,
                Instruction = instruction,
                FilePath = GetPromptFilePath(DefaultPromptId)
            };

            SaveProfile(profile);

            if (!File.Exists(profile.FilePath))
            {
                profile.FilePath = string.Empty;
            }

            return profile;
        }

        private PromptProfile CreateFallbackProfile()
        {
            return new PromptProfile
            {
                Id = DefaultPromptId,
                Name = DefaultPromptName,
                Instruction = LoadDefaultPromptText(),
                FilePath = string.Empty
            };
        }

        private static string BuildPromptFileContent(PromptProfile profile)
        {
            var builder = new StringBuilder();
            builder.Append(PromptNameCommentPrefix);
            builder.Append(profile.Name);
            builder.AppendLine(PromptNameCommentSuffix);
            builder.AppendLine();
            builder.Append(profile.Instruction ?? string.Empty);
            return builder.ToString();
        }

        private static bool TryExtractNameAndInstruction(string text, out string name, out string instruction)
        {
            name = string.Empty;
            instruction = text;

            var normalized = text.Replace("\r\n", "\n");
            var lines = normalized.Split('\n');
            var index = 0;

            while (index < lines.Length)
            {
                var trimmed = lines[index].Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    index++;
                    continue;
                }

                if (TryParseNameLine(trimmed, out var parsedName))
                {
                    name = parsedName;
                    index++;
                    break;
                }

                return false;
            }

            if (string.IsNullOrWhiteSpace(name))
                return false;

            instruction = string.Join("\n", lines.Skip(index)).Replace("\n", Environment.NewLine);
            return true;
        }

        private static bool TryParseNameLine(string line, out string name)
        {
            name = string.Empty;
            var trimmed = line.Trim();
            if (!trimmed.StartsWith(PromptNameCommentPrefix, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!trimmed.EndsWith(PromptNameCommentSuffix, StringComparison.Ordinal))
                return false;

            var value = trimmed.Substring(PromptNameCommentPrefix.Length, trimmed.Length - PromptNameCommentPrefix.Length - PromptNameCommentSuffix.Length);
            value = value.Trim();
            if (string.IsNullOrWhiteSpace(value))
                return false;

            name = value;
            return true;
        }

        private static string ExtractIdFromFilePath(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        private static string GetPromptFilePath(string id)
        {
            var baseDir = AppContext.BaseDirectory;
            return Path.Combine(GetPromptDirectoryPath(baseDir), $"{id}{PromptFileExtension}");
        }

        private void SyncSelectedPromptIdToConfig()
        {
            AppConfig.Instance.SelectedPromptId = SelectedPromptId;
        }

        private static void TryDeletePromptFile(PromptProfile profile)
        {
            if (string.IsNullOrWhiteSpace(profile.FilePath))
                return;

            try
            {
                if (File.Exists(profile.FilePath))
                    File.Delete(profile.FilePath);
            }
            catch (Exception ex)
            {
                ShowFileError($"プロンプトファイルの削除に失敗しました。\n{profile.FilePath}", ex);
            }
        }

        private static string LoadDefaultPromptText()
        {
            var assembly = typeof(PromptStore).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith("DefaultPrompt.md", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(resourceName))
                return string.Empty;

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return string.Empty;

            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private static string GetPromptDirectoryPath(string baseDir)
        {
            return Path.Combine(baseDir, PromptFolderName);
        }

        private static bool EnsurePromptDirectory(string promptDir)
        {
            try
            {
                if (!Directory.Exists(promptDir))
                {
                    Directory.CreateDirectory(promptDir);
                }
                return true;
            }
            catch (Exception ex)
            {
                ShowFileError($"プロンプトフォルダの作成に失敗しました。\n{promptDir}", ex);
                return false;
            }
        }

        private static void ShowFileError(string message, Exception ex)
        {
            System.Windows.MessageBox.Show($"{message}\n\n{ex.Message}", "プロンプト管理エラー", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
