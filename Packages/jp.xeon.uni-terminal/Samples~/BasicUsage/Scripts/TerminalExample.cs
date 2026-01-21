using System.IO;
using System.Threading;
using UnityEngine;
using Xeon.UniTerminal;

namespace Xeon.UniTerminal.Samples
{
    /// <summary>
    /// Basic example demonstrating how to use UniTerminal.
    /// </summary>
    public class TerminalExample : MonoBehaviour
    {
        private Terminal terminal;
        private StringWriter stdout;
        private StringWriter stderr;

        [SerializeField]
        private string initialCommand = "echo Hello, UniTerminal!";

        private async void Start()
        {
            // Initialize Terminal with default settings
            terminal = new Terminal(
                workingDirectory: Application.dataPath,
                homeDirectory: Application.dataPath,
                registerBuiltInCommands: true
            );

            stdout = new StringWriter();
            stderr = new StringWriter();

            // Execute initial command
            await ExecuteCommand(initialCommand);
        }

        /// <summary>
        /// Execute a command and log the results.
        /// </summary>
        public async void ExecuteCommand(string command)
        {
            if (terminal == null || string.IsNullOrEmpty(command))
                return;

            // Clear previous output
            stdout.GetStringBuilder().Clear();
            stderr.GetStringBuilder().Clear();

            // Execute command
            var exitCode = await terminal.ExecuteAsync(command, stdout, stderr, CancellationToken.None);

            // Log results
            var output = stdout.ToString();
            var error = stderr.ToString();

            if (!string.IsNullOrEmpty(output))
            {
                Debug.Log($"[UniTerminal] Output:\n{output}");
            }

            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"[UniTerminal] Error:\n{error}");
            }

            Debug.Log($"[UniTerminal] Exit Code: {exitCode}");
        }

        /// <summary>
        /// Example: List files in current directory.
        /// </summary>
        [ContextMenu("Run: ls -la")]
        public void RunListCommand()
        {
            ExecuteCommand("ls -la");
        }

        /// <summary>
        /// Example: Show scene hierarchy.
        /// </summary>
        [ContextMenu("Run: hierarchy -r")]
        public void RunHierarchyCommand()
        {
            ExecuteCommand("hierarchy -r");
        }

        /// <summary>
        /// Example: Find GameObjects with specific tag.
        /// </summary>
        [ContextMenu("Run: go find -t MainCamera")]
        public void RunFindCommand()
        {
            ExecuteCommand("go find -t MainCamera");
        }

        /// <summary>
        /// Example: Pipeline command.
        /// </summary>
        [ContextMenu("Run: hierarchy -r | grep Camera")]
        public void RunPipelineCommand()
        {
            ExecuteCommand("hierarchy -r | grep --pattern=Camera");
        }
    }
}
