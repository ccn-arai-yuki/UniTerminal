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

            // ワールド位置設定
            if (!string.IsNullOrEmpty(Position))
            {
                if (TryParseVector3(Position, out var pos))
                {
                    var oldPos = transform.position;
                    transform.position = pos;
                    await context.Stdout.WriteLineAsync($"  Position: {FormatVector3(oldPos)} -> {FormatVector3(transform.position)}", ct);
                    modified = true;
                }
                else
                {
                    await context.Stderr.WriteLineAsync($"transform: invalid position: '{Position}'", ct);
                    return ExitCode.UsageError;
                }
            }

            // ローカル位置設定
            if (!string.IsNullOrEmpty(LocalPosition))
            {
                if (TryParseVector3(LocalPosition, out var pos))
                {
                    var oldPos = transform.localPosition;
                    transform.localPosition = pos;
                    await context.Stdout.WriteLineAsync($"  Local Position: {FormatVector3(oldPos)} -> {FormatVector3(transform.localPosition)}", ct);
                    modified = true;
                }
                else
                {
                    await context.Stderr.WriteLineAsync($"transform: invalid local-position: '{LocalPosition}'", ct);
                    return ExitCode.UsageError;
                }
            }

            // ワールド回転設定
            if (!string.IsNullOrEmpty(Rotation))
            {
                if (TryParseVector3(Rotation, out var rot))
                {
                    var oldRot = transform.eulerAngles;
                    transform.eulerAngles = rot;
                    await context.Stdout.WriteLineAsync($"  Rotation: {FormatVector3(oldRot)} -> {FormatVector3(transform.eulerAngles)}", ct);
                    modified = true;
                }
                else
                {
                    await context.Stderr.WriteLineAsync($"transform: invalid rotation: '{Rotation}'", ct);
                    return ExitCode.UsageError;
                }
            }

            // ローカル回転設定
            if (!string.IsNullOrEmpty(LocalRotation))
            {
                if (TryParseVector3(LocalRotation, out var rot))
                {
                    var oldRot = transform.localEulerAngles;
                    transform.localEulerAngles = rot;
                    await context.Stdout.WriteLineAsync($"  Local Rotation: {FormatVector3(oldRot)} -> {FormatVector3(transform.localEulerAngles)}", ct);
                    modified = true;
                }
                else
                {
                    await context.Stderr.WriteLineAsync($"transform: invalid local-rotation: '{LocalRotation}'", ct);
                    return ExitCode.UsageError;
                }
            }

            // スケール設定
            if (!string.IsNullOrEmpty(Scale))
            {
                if (TryParseVector3(Scale, out var scale))
                {
                    var oldScale = transform.localScale;
                    transform.localScale = scale;
                    await context.Stdout.WriteLineAsync($"  Scale: {FormatVector3(oldScale)} -> {FormatVector3(transform.localScale)}", ct);
                    modified = true;
                }
                else
                {
                    await context.Stderr.WriteLineAsync($"transform: invalid scale: '{Scale}'", ct);
                    return ExitCode.UsageError;
                }
            }

            // 親の変更
            if (!string.IsNullOrEmpty(Parent))
            {
                var result = await SetParentAsync(transform, context, ct);
                if (result != ExitCode.Success)
                    return result;
                modified = true;
            }

            // 変更なしの場合は情報を表示
            if (!modified)
            {
                await DisplayTransformInfoAsync(context, go, ct);
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// 親を設定します。
        /// </summary>
        private async Task<ExitCode> SetParentAsync(Transform transform, CommandContext context, CancellationToken ct)
        {
            Transform newParent = null;
            string oldParentPath = transform.parent != null ? GameObjectPath.GetPath(transform.parent.gameObject) : "(none)";

            // 親を解除
            if (Parent == "/" || Parent.ToLower() == "null" || Parent.ToLower() == "none")
            {
                transform.SetParent(null, WorldPositionStays);
                await context.Stdout.WriteLineAsync($"  Parent: {oldParentPath} -> (none)", ct);
                return ExitCode.Success;
            }

            // 親を解決
            var parentGo = GameObjectPath.Resolve(Parent);
            if (parentGo == null)
            {
                await context.Stderr.WriteLineAsync($"transform: '{Parent}': Parent not found", ct);
                return ExitCode.RuntimeError;
            }

            // 循環参照チェック
            if (parentGo.transform == transform || parentGo.transform.IsChildOf(transform))
            {
                await context.Stderr.WriteLineAsync("transform: cannot set parent to self or descendant", ct);
                return ExitCode.RuntimeError;
            }

            newParent = parentGo.transform;
            transform.SetParent(newParent, WorldPositionStays);

            string newParentPath = GameObjectPath.GetPath(newParent.gameObject);
            await context.Stdout.WriteLineAsync($"  Parent: {oldParentPath} -> {newParentPath}", ct);

            return ExitCode.Success;
        }

        /// <summary>
        /// Transform情報を表示します。
        /// </summary>
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

        /// <summary>
        /// Vector3文字列をパースします。
        /// </summary>
        private bool TryParseVector3(string input, out Vector3 result)
        {
            result = Vector3.zero;
            if (string.IsNullOrEmpty(input))
                return false;

            var parts = input.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                switch (parts.Length)
                {
                    case 1:
                        float single = float.Parse(parts[0]);
                        result = new Vector3(single, single, single);
                        return true;
                    case 2:
                        result = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), 0);
                        return true;
                    case 3:
                        result = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Vector3を文字列にフォーマットします。
        /// </summary>
        private string FormatVector3(Vector3 v)
        {
            return $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            var token = context.CurrentToken ?? "";

            // パス補完
            if (!token.StartsWith("-"))
            {
                foreach (var path in GameObjectPath.GetCompletions(token))
                {
                    yield return path;
                }
            }
        }
    }
}
