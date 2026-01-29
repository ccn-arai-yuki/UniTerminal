using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Xeon.UniTerminal.Editor
{
    /// <summary>
    /// コマンドドキュメントを自動生成するエディタツール
    /// </summary>
    public class CommandDocumentationGenerator : EditorWindow
    {
        private string outputPath = "docs/ja/articles/commands";
        private bool generateEnglish = true;
        private bool generateJapanese = true;
        private Vector2 scrollPosition;
        private string previewText = "";
        private CommandDocConfig config;

        [MenuItem("Tools/UniTerminal/Generate Command Documentation")]
        public static void ShowWindow()
        {
            var window = GetWindow<CommandDocumentationGenerator>("Command Doc Generator");
            window.minSize = new Vector2(500, 400);
        }

        private void OnEnable()
        {
            LoadConfig();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("コマンドドキュメント生成", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            outputPath = EditorGUILayout.TextField("出力パス（日本語）", outputPath);
            generateJapanese = EditorGUILayout.Toggle("日本語ドキュメント生成", generateJapanese);
            generateEnglish = EditorGUILayout.Toggle("英語ドキュメント生成", generateEnglish);

            EditorGUILayout.Space();

            if (GUILayout.Button("設定ファイルを開く"))
                OpenConfigFile();

            if (GUILayout.Button("設定を再読み込み"))
                LoadConfig();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("プレビュー"))
                GeneratePreview();

            if (GUILayout.Button("ドキュメント生成"))
                GenerateDocumentation();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("プレビュー", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.TextArea(previewText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void LoadConfig()
        {
            var configPath = GetConfigPath();
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                config = JsonUtility.FromJson<CommandDocConfig>(json);
            }
            else
            {
                config = CreateDefaultConfig();
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            var configPath = GetConfigPath();
            var directory = Path.GetDirectoryName(configPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var json = JsonUtility.ToJson(config, true);
            File.WriteAllText(configPath, json);
            AssetDatabase.Refresh();
        }

        private string GetConfigPath()
        {
            return "Packages/jp.xeon.uni-terminal/Editor/Documentation/command_docs_config.json";
        }

        private void OpenConfigFile()
        {
            var configPath = GetConfigPath();
            if (!File.Exists(configPath))
            {
                config = CreateDefaultConfig();
                SaveConfig();
            }

            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(configPath);
            if (asset != null)
                AssetDatabase.OpenAsset(asset);
            else
                EditorUtility.RevealInFinder(configPath);
        }

        private CommandDocConfig CreateDefaultConfig()
        {
            return new CommandDocConfig
            {
                categories = new List<CategoryConfig>
                {
                    new CategoryConfig
                    {
                        id = "file-operations",
                        nameJa = "ファイル操作",
                        nameEn = "File Operations",
                        descriptionJa = "ファイルとディレクトリを操作するためのコマンドです。",
                        descriptionEn = "Commands for navigating and manipulating files and directories."
                    },
                    new CategoryConfig
                    {
                        id = "text-processing",
                        nameJa = "テキスト処理",
                        nameEn = "Text Processing",
                        descriptionJa = "テキスト出力の処理とフィルタリングのためのコマンドです。",
                        descriptionEn = "Commands for processing and filtering text output."
                    },
                    new CategoryConfig
                    {
                        id = "unity-commands",
                        nameJa = "Unityコマンド",
                        nameEn = "Unity Commands",
                        descriptionJa = "Unity GameObjects、Transform、Componentsを操作するためのコマンドです。",
                        descriptionEn = "Commands for manipulating Unity GameObjects, Transforms, and Components."
                    },
                    new CategoryConfig
                    {
                        id = "utilities",
                        nameJa = "ユーティリティ",
                        nameEn = "Utilities",
                        descriptionJa = "一般的なユーティリティコマンドです。",
                        descriptionEn = "General utility commands."
                    }
                },
                commands = new List<CommandExtraConfig>()
            };
        }

        private void GeneratePreview()
        {
            var commands = CollectCommandInfo();
            var sb = new StringBuilder();

            sb.AppendLine($"検出されたコマンド数: {commands.Count}");
            sb.AppendLine();

            foreach (var category in commands.GroupBy(c => c.Category))
            {
                sb.AppendLine($"## {category.Key}");
                foreach (var cmd in category)
                {
                    sb.AppendLine($"  - {cmd.Name}: {cmd.Description}");
                    sb.AppendLine($"    オプション数: {cmd.Options.Count}");
                }
                sb.AppendLine();
            }

            previewText = sb.ToString();
        }

        private void GenerateDocumentation()
        {
            var commands = CollectCommandInfo();

            if (generateJapanese)
            {
                GenerateDocumentationFiles(commands, "ja", outputPath);
            }

            if (generateEnglish)
            {
                var englishPath = outputPath.Replace("/ja/", "/");
                GenerateDocumentationFiles(commands, "en", englishPath);
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完了", "ドキュメントの生成が完了しました。", "OK");
        }

        private void GenerateDocumentationFiles(List<CommandInfo> commands, string lang, string basePath)
        {
            var grouped = commands.GroupBy(c => c.Category).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var kvp in grouped)
            {
                var categoryId = kvp.Key;
                var categoryCommands = kvp.Value;
                var categoryConfig = config.categories.FirstOrDefault(c => c.id == categoryId);

                var fileName = $"{categoryId}.md";
                var filePath = Path.Combine(Application.dataPath, "..", basePath, fileName);

                var content = GenerateCategoryDocument(categoryCommands, categoryConfig, lang);
                var directory = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(filePath, content, Encoding.UTF8);
                Debug.Log($"Generated: {filePath}");
            }

            GenerateIndexDocument(grouped, basePath, lang);
        }

        private void GenerateIndexDocument(
            Dictionary<string, List<CommandInfo>> grouped,
            string basePath,
            string lang)
        {
            var sb = new StringBuilder();
            var isJapanese = lang == "ja";

            sb.AppendLine(isJapanese ? "# 組み込みコマンド" : "# Built-in Commands");
            sb.AppendLine();
            sb.AppendLine(isJapanese
                ? "UniTerminalは、カテゴリ別に整理された包括的な組み込みコマンドセットを提供します。"
                : "UniTerminal provides a comprehensive set of built-in commands organized into categories.");
            sb.AppendLine();
            sb.AppendLine(isJapanese ? "## コマンドカテゴリ" : "## Command Categories");
            sb.AppendLine();
            sb.AppendLine(isJapanese
                ? "| カテゴリ | コマンド | 説明 |"
                : "| Category | Commands | Description |");
            sb.AppendLine("|----------|----------|------|");

            foreach (var kvp in grouped.OrderBy(x => GetCategoryOrder(x.Key)))
            {
                var categoryConfig = config.categories.FirstOrDefault(c => c.id == kvp.Key);
                var categoryName = isJapanese ? categoryConfig?.nameJa : categoryConfig?.nameEn;
                var categoryDesc = isJapanese ? categoryConfig?.descriptionJa : categoryConfig?.descriptionEn;
                var commandNames = string.Join(", ", kvp.Value.Select(c => $"`{c.Name}`"));

                sb.AppendLine($"| [{categoryName ?? kvp.Key}]({kvp.Key}.md) | {commandNames} | {categoryDesc} |");
            }

            sb.AppendLine();
            sb.AppendLine(isJapanese ? "## 終了コード" : "## Exit Codes");
            sb.AppendLine();
            sb.AppendLine(isJapanese ? "| コード | 説明 |" : "| Code | Description |");
            sb.AppendLine("|--------|------|");
            sb.AppendLine(isJapanese ? "| 0 | 成功 |" : "| 0 | Success |");
            sb.AppendLine(isJapanese ? "| 1 | 使用方法エラー |" : "| 1 | Usage error |");
            sb.AppendLine(isJapanese ? "| 2 | 実行時エラー |" : "| 2 | Runtime error |");

            var filePath = Path.Combine(Application.dataPath, "..", basePath, "index.md");
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"Generated: {filePath}");
        }

        private int GetCategoryOrder(string categoryId)
        {
            return categoryId switch
            {
                "file-operations" => 0,
                "text-processing" => 1,
                "utilities" => 2,
                "unity-commands" => 3,
                _ => 99
            };
        }

        private string GenerateCategoryDocument(
            List<CommandInfo> commands,
            CategoryConfig categoryConfig,
            string lang)
        {
            var sb = new StringBuilder();
            var isJapanese = lang == "ja";

            var title = isJapanese ? categoryConfig?.nameJa : categoryConfig?.nameEn;
            var description = isJapanese ? categoryConfig?.descriptionJa : categoryConfig?.descriptionEn;

            sb.AppendLine($"# {title}");
            sb.AppendLine();
            sb.AppendLine(description);
            sb.AppendLine();

            foreach (var cmd in commands.OrderBy(c => c.Name))
            {
                GenerateCommandSection(sb, cmd, lang);
            }

            return sb.ToString();
        }

        private void GenerateCommandSection(StringBuilder sb, CommandInfo cmd, string lang)
        {
            var isJapanese = lang == "ja";
            var extraConfig = config.commands.FirstOrDefault(c => c.name == cmd.Name);

            sb.AppendLine($"## {cmd.Name}");
            sb.AppendLine();
            sb.AppendLine(isJapanese ? extraConfig?.descriptionJa ?? cmd.Description : cmd.Description);
            sb.AppendLine();

            // 書式
            var synopsis = extraConfig?.synopsis ?? GenerateSynopsis(cmd);
            sb.AppendLine(isJapanese ? "### 書式" : "### Synopsis");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine(synopsis);
            sb.AppendLine("```");
            sb.AppendLine();

            // 説明
            if (!string.IsNullOrEmpty(extraConfig?.longDescriptionJa) && isJapanese)
            {
                sb.AppendLine("### 説明");
                sb.AppendLine();
                sb.AppendLine(extraConfig.longDescriptionJa);
                sb.AppendLine();
            }
            else if (!string.IsNullOrEmpty(extraConfig?.longDescriptionEn) && !isJapanese)
            {
                sb.AppendLine("### Description");
                sb.AppendLine();
                sb.AppendLine(extraConfig.longDescriptionEn);
                sb.AppendLine();
            }

            // オプション
            if (cmd.Options.Count > 0)
            {
                sb.AppendLine(isJapanese ? "### オプション" : "### Options");
                sb.AppendLine();
                sb.AppendLine(isJapanese
                    ? "| オプション | ロング形式 | 説明 |"
                    : "| Option | Long | Description |");
                sb.AppendLine("|--------|------|------|");

                foreach (var opt in cmd.Options)
                {
                    var shortOpt = string.IsNullOrEmpty(opt.ShortName) ? "" : $"`-{opt.ShortName}`";
                    var longOpt = $"`--{opt.LongName}`";
                    var desc = opt.Description;

                    if (opt.IsRequired)
                        desc = (isJapanese ? "**必須。** " : "**Required.** ") + desc;

                    sb.AppendLine($"| {shortOpt} | {longOpt} | {desc} |");
                }
                sb.AppendLine();
            }

            // 使用例
            var examples = isJapanese ? extraConfig?.examplesJa : extraConfig?.examplesEn;
            if (!string.IsNullOrEmpty(examples))
            {
                sb.AppendLine(isJapanese ? "### 使用例" : "### Examples");
                sb.AppendLine();
                sb.AppendLine("```bash");
                sb.AppendLine(examples.Replace("\\n", "\n"));
                sb.AppendLine("```");
                sb.AppendLine();
            }

            // 終了コード
            if (!string.IsNullOrEmpty(extraConfig?.exitCodes))
            {
                sb.AppendLine(isJapanese ? "### 終了コード" : "### Exit Codes");
                sb.AppendLine();
                sb.AppendLine(isJapanese ? "| コード | 説明 |" : "| Code | Description |");
                sb.AppendLine("|------|------|");
                sb.AppendLine(extraConfig.exitCodes.Replace("\\n", "\n"));
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        private string GenerateSynopsis(CommandInfo cmd)
        {
            var sb = new StringBuilder();
            sb.Append(cmd.Name);

            foreach (var opt in cmd.Options)
            {
                var optStr = string.IsNullOrEmpty(opt.ShortName)
                    ? $"--{opt.LongName}"
                    : $"-{opt.ShortName}";

                if (opt.Type != typeof(bool))
                    optStr += $" <{opt.LongName}>";

                if (opt.IsRequired)
                    sb.Append($" {optStr}");
                else
                    sb.Append($" [{optStr}]");
            }

            return sb.ToString();
        }

        private List<CommandInfo> CollectCommandInfo()
        {
            var result = new List<CommandInfo>();
            var commandTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => GetTypesSafe(a))
                .Where(t => t.GetCustomAttribute<CommandAttribute>() != null)
                .ToList();

            foreach (var type in commandTypes)
            {
                var cmdAttr = type.GetCustomAttribute<CommandAttribute>();
                var docAttr = type.GetCustomAttribute<CommandDocumentationAttribute>();

                var info = new CommandInfo
                {
                    Name = cmdAttr.CommandName,
                    Description = cmdAttr.Description,
                    Category = docAttr?.Category ?? InferCategory(type),
                    Options = CollectOptions(type)
                };

                result.Add(info);
            }

            return result;
        }

        private IEnumerable<Type> GetTypesSafe(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch
            {
                return Enumerable.Empty<Type>();
            }
        }

        private string InferCategory(Type type)
        {
            var ns = type.Namespace ?? "";

            if (ns.Contains("UnityCommands"))
                return "unity-commands";
            if (ns.Contains("BuiltInCommands"))
            {
                var name = type.Name.ToLower();
                if (name.Contains("echo") || name.Contains("grep"))
                    return "text-processing";
                if (name.Contains("help") || name.Contains("history") ||
                    name.Contains("clear") || name.Contains("log"))
                    return "utilities";
                return "file-operations";
            }

            return "utilities";
        }

        private List<OptionInfo> CollectOptions(Type type)
        {
            var result = new List<OptionInfo>();
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var member in members)
            {
                var optAttr = member.GetCustomAttribute<OptionAttribute>();
                if (optAttr == null)
                    continue;

                Type memberType = member switch
                {
                    FieldInfo fi => fi.FieldType,
                    PropertyInfo pi => pi.PropertyType,
                    _ => typeof(object)
                };

                result.Add(new OptionInfo
                {
                    LongName = optAttr.LongName,
                    ShortName = optAttr.ShortName,
                    Description = optAttr.Description,
                    IsRequired = optAttr.IsRequired,
                    Type = memberType
                });
            }

            return result;
        }
    }

    [Serializable]
    public class CommandDocConfig
    {
        public List<CategoryConfig> categories = new List<CategoryConfig>();
        public List<CommandExtraConfig> commands = new List<CommandExtraConfig>();
    }

    [Serializable]
    public class CategoryConfig
    {
        public string id;
        public string nameJa;
        public string nameEn;
        public string descriptionJa;
        public string descriptionEn;
    }

    [Serializable]
    public class CommandExtraConfig
    {
        public string name;
        public string synopsis;
        public string descriptionJa;
        public string longDescriptionJa;
        public string longDescriptionEn;
        public string examplesJa;
        public string examplesEn;
        public string exitCodes;
    }

    public class CommandInfo
    {
        public string Name;
        public string Description;
        public string Category;
        public List<OptionInfo> Options = new List<OptionInfo>();
    }

    public class OptionInfo
    {
        public string LongName;
        public string ShortName;
        public string Description;
        public bool IsRequired;
        public Type Type;
    }
}
