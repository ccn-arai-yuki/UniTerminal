using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Xeon.UniTerminal.UnityCommands
{
    /// <summary>
    /// コンポーネントのプロパティを取得・設定するコマンド
    /// </summary>
    [Command("property", "Get or set component properties (list, get, set)")]
    public class PropertyCommand : ICommand
    {
        #region Options

        [Option("all", "a", Description = "Include private fields")]
        public bool IncludePrivate;

        [Option("serialized", "s", Description = "Show only SerializeField")]
        public bool SerializedOnly;

        [Option("namespace", "n", Description = "Component namespace")]
        public string Namespace;

        #endregion

        #region ICommand

        public string CommandName => "property";
        public string Description => "Get or set component properties";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            if (context.PositionalArguments.Count == 0)
                return await ShowUsageAsync(context, ct);

            var subCommand = context.PositionalArguments[0].ToLower();
            var args = context.PositionalArguments.Skip(1).ToList();

            return subCommand switch
            {
                "list" => await ListAsync(context, args, ct),
                "get" => await GetAsync(context, args, ct),
                "set" => await SetAsync(context, args, ct),
                _ => await UnknownSubCommandAsync(context, subCommand, ct)
            };
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            var token = context.CurrentToken ?? "";
            var tokens = context.InputLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (context.TokenIndex == 1)
                return GetSubCommandCompletions(token);

            if (context.TokenIndex == 2 && !token.StartsWith("-"))
                return GameObjectPath.GetCompletions(token);

            if (context.TokenIndex == 3 && !token.StartsWith("-"))
                return GetComponentNameCompletions(tokens, token);

            if (context.TokenIndex == 4 && !token.StartsWith("-"))
                return GetPropertyNameCompletions(tokens, token);

            return Enumerable.Empty<string>();
        }

        #endregion

        #region Subcommands

        private async Task<ExitCode> ListAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count < 2)
            {
                await context.Stderr.WriteLineAsync("property list: usage: property list <path> <component>", ct);
                return ExitCode.UsageError;
            }

            var result = ResolveComponent(args[0], args[1]);
            if (result.error != null)
            {
                await context.Stderr.WriteLineAsync(result.error, ct);
                return ExitCode.RuntimeError;
            }

            var type = result.comp.GetType();
            var bindingFlags = GetBindingFlags();

            await context.Stdout.WriteLineAsync($"Properties of {type.Name} on {GameObjectPath.GetPath(result.go)}:", ct);

            // フィールド
            await WriteFields(context, result.comp, type, bindingFlags, ct);

            // プロパティ
            await WriteProperties(context, result.comp, type, bindingFlags, ct);

            return ExitCode.Success;
        }

        private async Task<ExitCode> GetAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count < 3)
            {
                await context.Stderr.WriteLineAsync("property get: usage: property get <path> <component> <property>", ct);
                return ExitCode.UsageError;
            }

            var result = ResolveComponent(args[0], args[1]);
            if (result.error != null)
            {
                await context.Stderr.WriteLineAsync(result.error, ct);
                return ExitCode.RuntimeError;
            }

            var type = result.comp.GetType();
            var propertyNames = args[2].Split(',');
            var hasError = false;

            foreach (var propName in propertyNames)
            {
                ct.ThrowIfCancellationRequested();
                var memberResult = GetMemberValue(result.comp, type, propName.Trim());

                if (memberResult.error != null)
                {
                    await context.Stderr.WriteLineAsync(memberResult.error, ct);
                    hasError = true;
                    continue;
                }

                var valueStr = ValueConverter.Format(memberResult.value);
                var typeName = GetFriendlyTypeName(memberResult.memberType);
                await context.Stdout.WriteLineAsync($"{type.Name}.{propName.Trim()} = {valueStr} ({typeName})", ct);
            }

            return hasError ? ExitCode.RuntimeError : ExitCode.Success;
        }

        private async Task<ExitCode> SetAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count < 4)
            {
                await context.Stderr.WriteLineAsync("property set: usage: property set <path> <component> <property> <value>", ct);
                return ExitCode.UsageError;
            }

            var result = ResolveComponent(args[0], args[1]);
            if (result.error != null)
            {
                await context.Stderr.WriteLineAsync(result.error, ct);
                return ExitCode.RuntimeError;
            }

            var propName = args[2];
            var newValueStr = string.Join(" ", args.Skip(3)); // スペースを含む値に対応
            var type = result.comp.GetType();
            var (baseName, arrayIndex) = ParseArrayIndex(propName);

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(result.comp, $"Set {propName}");
#endif

            // フィールドを検索
            var field = type.GetField(baseName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
                return await SetFieldValueAsync(context, result.comp, type, field, baseName, arrayIndex, newValueStr, propName, ct);

            // プロパティを検索
            var prop = type.GetProperty(baseName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null)
                return await SetPropertyValueAsync(context, result.comp, type, prop, baseName, arrayIndex, newValueStr, propName, ct);

            await context.Stderr.WriteLineAsync($"property: '{baseName}': Property not found on {type.Name}", ct);
            return ExitCode.RuntimeError;
        }

        #endregion

        #region Set Helpers

        private async Task<ExitCode> SetFieldValueAsync(
            CommandContext context, Component comp, Type compType, FieldInfo field,
            string baseName, int? arrayIndex, string newValueStr, string propName, CancellationToken ct)
        {
            if (field.IsInitOnly)
            {
                await context.Stderr.WriteLineAsync($"property: '{baseName}' is read-only", ct);
                return ExitCode.RuntimeError;
            }

            if (arrayIndex.HasValue)
            {
                return await SetArrayElementAsync(context, comp, compType, field.GetValue(comp),
                    field.FieldType, baseName, arrayIndex.Value, newValueStr, ct);
            }

            var oldValue = field.GetValue(comp);

            try
            {
                var newValue = ValueConverter.Convert(newValueStr, field.FieldType);
                field.SetValue(comp, newValue);
                await context.Stdout.WriteLineAsync(
                    $"{compType.Name}.{propName}: {ValueConverter.Format(oldValue)} -> {ValueConverter.Format(newValue)}", ct);
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                await context.Stderr.WriteLineAsync(
                    $"property: Cannot convert '{newValueStr}' to {field.FieldType.Name}: {ex.Message}", ct);
                return ExitCode.UsageError;
            }
        }

        private async Task<ExitCode> SetPropertyValueAsync(
            CommandContext context, Component comp, Type compType, PropertyInfo prop,
            string baseName, int? arrayIndex, string newValueStr, string propName, CancellationToken ct)
        {
            if (arrayIndex.HasValue && prop.CanRead)
            {
                return await SetArrayElementAsync(context, comp, compType, prop.GetValue(comp),
                    prop.PropertyType, baseName, arrayIndex.Value, newValueStr, ct);
            }

            if (!prop.CanWrite)
            {
                await context.Stderr.WriteLineAsync($"property: '{baseName}' is read-only", ct);
                return ExitCode.RuntimeError;
            }

            var oldValue = prop.CanRead ? prop.GetValue(comp) : null;

            try
            {
                var newValue = ValueConverter.Convert(newValueStr, prop.PropertyType);
                prop.SetValue(comp, newValue);
                await context.Stdout.WriteLineAsync(
                    $"{compType.Name}.{propName}: {ValueConverter.Format(oldValue)} -> {ValueConverter.Format(newValue)}", ct);
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                await context.Stderr.WriteLineAsync(
                    $"property: Cannot convert '{newValueStr}' to {prop.PropertyType.Name}: {ex.Message}", ct);
                return ExitCode.UsageError;
            }
        }

        private async Task<ExitCode> SetArrayElementAsync(
            CommandContext context, Component comp, Type compType, object arrayObj, Type arrayType,
            string memberName, int index, string newValueStr, CancellationToken ct)
        {
            if (arrayObj == null)
            {
                await context.Stderr.WriteLineAsync($"property: '{memberName}' is null", ct);
                return ExitCode.RuntimeError;
            }

            // 配列の場合
            if (arrayObj is Array array)
                return await SetArrayValueAsync(context, compType, array, arrayType, memberName, index, newValueStr, ct);

            // IListの場合
            if (arrayObj is IList list)
                return await SetListValueAsync(context, compType, list, arrayType, memberName, index, newValueStr, ct);

            await context.Stderr.WriteLineAsync($"property: '{memberName}' is not an array or list", ct);
            return ExitCode.RuntimeError;
        }

        private async Task<ExitCode> SetArrayValueAsync(
            CommandContext context, Type compType, Array array, Type arrayType,
            string memberName, int index, string newValueStr, CancellationToken ct)
        {
            if (index < 0 || index >= array.Length)
            {
                await context.Stderr.WriteLineAsync($"property: Index {index} out of range for '{memberName}' (length: {array.Length})", ct);
                return ExitCode.RuntimeError;
            }

            var elementType = arrayType.GetElementType() ?? typeof(object);
            var oldValue = array.GetValue(index);

            try
            {
                var newValue = ValueConverter.Convert(newValueStr, elementType);
                array.SetValue(newValue, index);
                await context.Stdout.WriteLineAsync(
                    $"{compType.Name}.{memberName}[{index}]: {ValueConverter.Format(oldValue)} -> {ValueConverter.Format(newValue)}", ct);
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                await context.Stderr.WriteLineAsync(
                    $"property: Cannot convert '{newValueStr}' to {elementType.Name}: {ex.Message}", ct);
                return ExitCode.UsageError;
            }
        }

        private async Task<ExitCode> SetListValueAsync(
            CommandContext context, Type compType, IList list, Type listType,
            string memberName, int index, string newValueStr, CancellationToken ct)
        {
            if (index < 0 || index >= list.Count)
            {
                await context.Stderr.WriteLineAsync($"property: Index {index} out of range for '{memberName}' (count: {list.Count})", ct);
                return ExitCode.RuntimeError;
            }

            var elementType = listType.IsGenericType ? listType.GetGenericArguments()[0] : typeof(object);
            var oldValue = list[index];

            try
            {
                var newValue = ValueConverter.Convert(newValueStr, elementType);
                list[index] = newValue;
                await context.Stdout.WriteLineAsync(
                    $"{compType.Name}.{memberName}[{index}]: {ValueConverter.Format(oldValue)} -> {ValueConverter.Format(newValue)}", ct);
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                await context.Stderr.WriteLineAsync(
                    $"property: Cannot convert '{newValueStr}' to {elementType.Name}: {ex.Message}", ct);
                return ExitCode.UsageError;
            }
        }

        #endregion

        #region List Output Helpers

        private async Task WriteFields(
            CommandContext context, Component comp, Type type, BindingFlags bindingFlags, CancellationToken ct)
        {
            var fields = type.GetFields(bindingFlags)
                .Where(f => !SerializedOnly || HasSerializeField(f))
                .OrderBy(f => f.Name);

            foreach (var field in fields)
            {
                ct.ThrowIfCancellationRequested();
                await WriteFieldEntry(context, comp, field, ct);
            }
        }

        private async Task WriteProperties(
            CommandContext context, Component comp, Type type, BindingFlags bindingFlags, CancellationToken ct)
        {
            var properties = type.GetProperties(bindingFlags)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .OrderBy(p => p.Name);

            foreach (var prop in properties)
            {
                ct.ThrowIfCancellationRequested();
                await WritePropertyEntry(context, comp, prop, ct);
            }
        }

        private async Task WriteFieldEntry(CommandContext context, Component comp, FieldInfo field, CancellationToken ct)
        {
            var readOnly = field.IsInitOnly ? " [readonly]" : "";
            var typeName = GetFriendlyTypeName(field.FieldType);

            try
            {
                var value = field.GetValue(comp);
                var valueStr = ValueConverter.Format(value);
                await context.Stdout.WriteLineAsync($"  {field.Name,-24} {typeName,-14} {valueStr}{readOnly}", ct);
            }
            catch
            {
                await context.Stdout.WriteLineAsync($"  {field.Name,-24} {typeName,-14} (error){readOnly}", ct);
            }
        }

        private async Task WritePropertyEntry(CommandContext context, Component comp, PropertyInfo prop, CancellationToken ct)
        {
            var readOnly = !prop.CanWrite ? " [readonly]" : "";
            var typeName = GetFriendlyTypeName(prop.PropertyType);

            try
            {
                var value = prop.GetValue(comp);
                var valueStr = ValueConverter.Format(value);
                await context.Stdout.WriteLineAsync($"  {prop.Name,-24} {typeName,-14} {valueStr}{readOnly}", ct);
            }
            catch
            {
                await context.Stdout.WriteLineAsync($"  {prop.Name,-24} {typeName,-14} (error){readOnly}", ct);
            }
        }

        #endregion

        #region Resolution Helpers

        private (GameObject go, Component comp, string error) ResolveComponent(string path, string compName)
        {
            var go = GameObjectPath.Resolve(path);
            if (go == null)
                return (null, null, $"property: '{path}': GameObject not found");

            var type = TypeResolver.ResolveComponentType(compName, Namespace);
            if (type == null)
                return (go, null, $"property: '{compName}': Component type not found");

            var comp = go.GetComponent(type);
            if (comp == null)
                return (go, null, $"property: '{compName}' not found on {go.name}");

            return (go, comp, null);
        }

        private (object value, Type memberType, string error) GetMemberValue(Component comp, Type type, string memberName)
        {
            var (baseName, arrayIndex) = ParseArrayIndex(memberName);

            // フィールドを検索
            var field = type.GetField(baseName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                var value = field.GetValue(comp);
                if (arrayIndex.HasValue)
                    return GetArrayElement(value, field.FieldType, arrayIndex.Value, baseName);
                return (value, field.FieldType, null);
            }

            // プロパティを検索
            var prop = type.GetProperty(baseName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null && prop.CanRead)
            {
                try
                {
                    var value = prop.GetValue(comp);
                    if (arrayIndex.HasValue)
                        return GetArrayElement(value, prop.PropertyType, arrayIndex.Value, baseName);
                    return (value, prop.PropertyType, null);
                }
                catch (Exception ex)
                {
                    return (null, null, $"property: Cannot read '{memberName}': {ex.Message}");
                }
            }

            return (null, null, $"property: '{memberName}': Property not found on {type.Name}");
        }

        private (object value, Type memberType, string error) GetArrayElement(object arrayObj, Type arrayType, int index, string memberName)
        {
            if (arrayObj == null)
                return (null, null, $"property: '{memberName}' is null");

            // 配列の場合
            if (arrayObj is Array array)
            {
                if (index < 0 || index >= array.Length)
                    return (null, null, $"property: Index {index} out of range for '{memberName}' (length: {array.Length})");
                var elementType = arrayType.GetElementType() ?? typeof(object);
                return (array.GetValue(index), elementType, null);
            }

            // IListの場合
            if (arrayObj is IList list)
            {
                if (index < 0 || index >= list.Count)
                    return (null, null, $"property: Index {index} out of range for '{memberName}' (count: {list.Count})");
                var elementType = arrayType.IsGenericType ? arrayType.GetGenericArguments()[0] : typeof(object);
                return (list[index], elementType, null);
            }

            return (null, null, $"property: '{memberName}' is not an array or list");
        }

        #endregion

        #region Utility

        private BindingFlags GetBindingFlags()
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
            if (IncludePrivate)
                bindingFlags |= BindingFlags.NonPublic;
            return bindingFlags;
        }

        private (string baseName, int? index) ParseArrayIndex(string memberName)
        {
            var match = Regex.Match(memberName, @"^(.+)\[(\d+)\]$");
            if (match.Success)
                return (match.Groups[1].Value, int.Parse(match.Groups[2].Value));
            return (memberName, null);
        }

        private bool HasSerializeField(FieldInfo field)
        {
            if (field.GetCustomAttribute<SerializeField>() != null)
                return true;

            if (field.IsPublic && field.GetCustomAttribute<NonSerializedAttribute>() == null)
                return true;

            return false;
        }

        private string GetFriendlyTypeName(Type type)
        {
            return type.Name switch
            {
                "Int32" => "int",
                "Single" => "float",
                "Double" => "double",
                "Boolean" => "bool",
                "String" => "string",
                "Int64" => "long",
                "Byte" => "byte",
                "Int16" => "short",
                _ when type.IsEnum => "enum",
                _ => type.Name
            };
        }

        #endregion

        #region Output Helpers

        private async Task<ExitCode> ShowUsageAsync(CommandContext context, CancellationToken ct)
        {
            await context.Stderr.WriteLineAsync("property: missing subcommand", ct);
            await context.Stderr.WriteLineAsync("Usage: property <subcommand> <path> <component> [property] [value]", ct);
            await context.Stderr.WriteLineAsync("Subcommands: list, get, set", ct);
            return ExitCode.UsageError;
        }

        private async Task<ExitCode> UnknownSubCommandAsync(CommandContext context, string subCommand, CancellationToken ct)
        {
            await context.Stderr.WriteLineAsync($"property: unknown subcommand '{subCommand}'", ct);
            await context.Stderr.WriteLineAsync("Subcommands: list, get, set", ct);
            return ExitCode.UsageError;
        }

        #endregion

        #region Completion Helpers

        private static IEnumerable<string> GetSubCommandCompletions(string token)
        {
            var subCommands = new[] { "list", "get", "set" };
            return subCommands.Where(cmd => cmd.StartsWith(token, StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<string> GetComponentNameCompletions(string[] tokens, string token)
        {
            if (tokens.Length < 3)
            {
                yield break;
            }

            var go = GameObjectPath.Resolve(tokens[2]);
            if (go != null)
            {
                var components = go.GetComponents<Component>();
                var seenTypes = new HashSet<string>();
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    var typeName = comp.GetType().Name;
                    if (seenTypes.Add(typeName) && typeName.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                        yield return typeName;
                }
            }

            // よく使うコンポーネント名
            foreach (var compName in TypeResolver.GetCommonComponentNames())
            {
                if (compName.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                    yield return compName;
            }
        }

        private IEnumerable<string> GetPropertyNameCompletions(string[] tokens, string token)
        {
            if (tokens.Length < 4)
                return Enumerable.Empty<string>();

            var go = GameObjectPath.Resolve(tokens[2]);
            if (go == null)
                return Enumerable.Empty<string>();

            var compType = TypeResolver.ResolveComponentType(tokens[3]);
            if (compType == null)
                return Enumerable.Empty<string>();

            var comp = go.GetComponent(compType);
            if (comp == null)
                return Enumerable.Empty<string>();

            return GetMemberNames(comp.GetType(), token);
        }

        private IEnumerable<string> GetMemberNames(Type type, string prefix)
        {
            var bindingFlags = GetBindingFlags();

            // フィールド
            foreach (var field in type.GetFields(bindingFlags))
            {
                if (field.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    yield return field.Name;
            }

            // プロパティ
            foreach (var prop in type.GetProperties(bindingFlags))
            {
                if (prop.CanRead && prop.GetIndexParameters().Length == 0)
                {
                    if (prop.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        yield return prop.Name;
                }
            }
        }

        #endregion
    }
}
