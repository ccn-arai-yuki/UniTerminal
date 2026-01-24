using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Xeon.UniTerminal.UnityCommands
{
    /// <summary>
    /// GameObjectのTransformを操作するコマンド。
    /// </summary>
    [Command("transform", "Manipulate GameObject Transform")]
    public class TransformCommand : ICommand
    {
        #region Options

        [Option("position", "p", Description = "Set world position (x,y,z)")]
        public string Position;

        [Option("local-position", "P", Description = "Set local position (x,y,z)")]
        public string LocalPosition;

        [Option("rotation", "r", Description = "Set world rotation in euler angles (x,y,z)")]
        public string Rotation;

        [Option("local-rotation", "R", Description = "Set local rotation in euler angles (x,y,z)")]
        public string LocalRotation;

        [Option("scale", "s", Description = "Set local scale (x,y,z)")]
        public string Scale;

        [Option("parent", "", Description = "Set parent object (use '/' or 'null' to unparent)")]
        public string Parent;

        [Option("world", "w", Description = "Maintain world position when changing parent")]
        public bool WorldPositionStays = true;

        #endregion

        #region ICommand

        public string CommandName => "transform";
        public string Description => "Manipulate GameObject Transform";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            if (context.PositionalArguments.Count == 0)
            {
                await context.Stderr.WriteLineAsync("transform: missing path argument", ct);
                await context.Stderr.WriteLineAsync("Usage: transform <path> [options]", ct);
                return ExitCode.UsageError;
            }

            var path = context.PositionalArguments[0];
            var go = GameObjectPath.Resolve(path);

            if (go == null)
            {
                await context.Stderr.WriteLineAsync($"transform: '{path}': GameObject not found", ct);
                return ExitCode.RuntimeError;
            }

            var transform = go.transform;
            bool modified = false;

            await context.Stdout.WriteLineAsync($"Transform: {go.name}", ct);

            // 各オプションを順次適用
            var result = await ApplyPositionOptions(context, transform, ct);
            if (result.exitCode != ExitCode.Success) return result.exitCode;
            modified |= result.modified;

            result = await ApplyRotationOptions(context, transform, ct);
            if (result.exitCode != ExitCode.Success) return result.exitCode;
            modified |= result.modified;

            result = await ApplyScaleOption(context, transform, ct);
            if (result.exitCode != ExitCode.Success) return result.exitCode;
            modified |= result.modified;

            result = await ApplyParentOption(context, transform, ct);
            if (result.exitCode != ExitCode.Success) return result.exitCode;
            modified |= result.modified;

            // 変更なしの場合は情報を表示
            if (!modified)
                await DisplayTransformInfoAsync(context, go, ct);

            return ExitCode.Success;
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            var token = context.CurrentToken ?? "";

            if (!token.StartsWith("-"))
                return GameObjectPath.GetCompletions(token);

            return Array.Empty<string>();
        }

        #endregion

        #region Apply Options

        private async Task<(ExitCode exitCode, bool modified)> ApplyPositionOptions(
            CommandContext context, Transform transform, CancellationToken ct)
        {
            bool modified = false;

            // ワールド位置設定
            if (!string.IsNullOrEmpty(Position))
            {
                if (!TryParseVector3(Position, out var pos))
                {
                    await context.Stderr.WriteLineAsync($"transform: invalid position: '{Position}'", ct);
                    return (ExitCode.UsageError, false);
                }

                var oldPos = transform.position;
                transform.position = pos;
                await context.Stdout.WriteLineAsync($"  Position: {FormatVector3(oldPos)} -> {FormatVector3(transform.position)}", ct);
                modified = true;
            }

            // ローカル位置設定
            if (!string.IsNullOrEmpty(LocalPosition))
            {
                if (!TryParseVector3(LocalPosition, out var pos))
                {
                    await context.Stderr.WriteLineAsync($"transform: invalid local-position: '{LocalPosition}'", ct);
                    return (ExitCode.UsageError, false);
                }

                var oldPos = transform.localPosition;
                transform.localPosition = pos;
                await context.Stdout.WriteLineAsync($"  Local Position: {FormatVector3(oldPos)} -> {FormatVector3(transform.localPosition)}", ct);
                modified = true;
            }

            return (ExitCode.Success, modified);
        }

        private async Task<(ExitCode exitCode, bool modified)> ApplyRotationOptions(
            CommandContext context, Transform transform, CancellationToken ct)
        {
            bool modified = false;

            // ワールド回転設定
            if (!string.IsNullOrEmpty(Rotation))
            {
                if (!TryParseVector3(Rotation, out var rot))
                {
                    await context.Stderr.WriteLineAsync($"transform: invalid rotation: '{Rotation}'", ct);
                    return (ExitCode.UsageError, false);
                }

                var oldRot = transform.eulerAngles;
                transform.eulerAngles = rot;
                await context.Stdout.WriteLineAsync($"  Rotation: {FormatVector3(oldRot)} -> {FormatVector3(transform.eulerAngles)}", ct);
                modified = true;
            }

            // ローカル回転設定
            if (!string.IsNullOrEmpty(LocalRotation))
            {
                if (!TryParseVector3(LocalRotation, out var rot))
                {
                    await context.Stderr.WriteLineAsync($"transform: invalid local-rotation: '{LocalRotation}'", ct);
                    return (ExitCode.UsageError, false);
                }

                var oldRot = transform.localEulerAngles;
                transform.localEulerAngles = rot;
                await context.Stdout.WriteLineAsync($"  Local Rotation: {FormatVector3(oldRot)} -> {FormatVector3(transform.localEulerAngles)}", ct);
                modified = true;
            }

            return (ExitCode.Success, modified);
        }

        private async Task<(ExitCode exitCode, bool modified)> ApplyScaleOption(
            CommandContext context, Transform transform, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(Scale))
                return (ExitCode.Success, false);

            if (!TryParseVector3(Scale, out var scale))
            {
                await context.Stderr.WriteLineAsync($"transform: invalid scale: '{Scale}'", ct);
                return (ExitCode.UsageError, false);
            }

            var oldScale = transform.localScale;
            transform.localScale = scale;
            await context.Stdout.WriteLineAsync($"  Scale: {FormatVector3(oldScale)} -> {FormatVector3(transform.localScale)}", ct);
            return (ExitCode.Success, true);
        }

        private async Task<(ExitCode exitCode, bool modified)> ApplyParentOption(
            CommandContext context, Transform transform, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(Parent))
                return (ExitCode.Success, false);

            string oldParentPath = transform.parent != null
                ? GameObjectPath.GetPath(transform.parent.gameObject)
                : "(none)";

            // 親を解除
            if (Parent == "/" || Parent.ToLower() == "null" || Parent.ToLower() == "none")
            {
                transform.SetParent(null, WorldPositionStays);
                await context.Stdout.WriteLineAsync($"  Parent: {oldParentPath} -> (none)", ct);
                return (ExitCode.Success, true);
            }

            // 親を解決
            var parentGo = GameObjectPath.Resolve(Parent);
            if (parentGo == null)
            {
                await context.Stderr.WriteLineAsync($"transform: '{Parent}': Parent not found", ct);
                return (ExitCode.RuntimeError, false);
            }

            // 循環参照チェック
            if (parentGo.transform == transform || parentGo.transform.IsChildOf(transform))
            {
                await context.Stderr.WriteLineAsync("transform: cannot set parent to self or descendant", ct);
                return (ExitCode.RuntimeError, false);
            }

            transform.SetParent(parentGo.transform, WorldPositionStays);
            string newParentPath = GameObjectPath.GetPath(parentGo);
            await context.Stdout.WriteLineAsync($"  Parent: {oldParentPath} -> {newParentPath}", ct);
            return (ExitCode.Success, true);
        }

        #endregion

        #region Display

        private async Task DisplayTransformInfoAsync(CommandContext context, GameObject go, CancellationToken ct)
        {
            var t = go.transform;

            await context.Stdout.WriteLineAsync($"  World Position:  {FormatVector3(t.position)}", ct);
            await context.Stdout.WriteLineAsync($"  Local Position:  {FormatVector3(t.localPosition)}", ct);
            await context.Stdout.WriteLineAsync($"  World Rotation:  {FormatVector3(t.eulerAngles)}", ct);
            await context.Stdout.WriteLineAsync($"  Local Rotation:  {FormatVector3(t.localEulerAngles)}", ct);
            await context.Stdout.WriteLineAsync($"  Local Scale:     {FormatVector3(t.localScale)}", ct);

            string parentPath = t.parent != null ? GameObjectPath.GetPath(t.parent.gameObject) : "(none)";
            await context.Stdout.WriteLineAsync($"  Parent:          {parentPath}", ct);
            await context.Stdout.WriteLineAsync($"  Children:        {t.childCount}", ct);
            await context.Stdout.WriteLineAsync($"  Sibling Index:   {t.GetSiblingIndex()}", ct);
        }

        #endregion

        #region Utility

        private bool TryParseVector3(string input, out Vector3 result)
        {
            result = Vector3.zero;
            if (string.IsNullOrEmpty(input))
                return false;

            var parts = input.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                return parts.Length switch
                {
                    1 => ParseSingle(parts[0], out result),
                    2 => ParseTwo(parts, out result),
                    3 => ParseThree(parts, out result),
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }

        private static bool ParseSingle(string value, out Vector3 result)
        {
            var single = float.Parse(value);
            result = new Vector3(single, single, single);
            return true;
        }

        private static bool ParseTwo(string[] parts, out Vector3 result)
        {
            result = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), 0);
            return true;
        }

        private static bool ParseThree(string[] parts, out Vector3 result)
        {
            result = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
            return true;
        }

        private static string FormatVector3(Vector3 v) => $"({v.x:F2}, {v.y:F2}, {v.z:F2})";

        #endregion
    }
}
