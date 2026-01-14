using System.Collections.Generic;
using NUnit.Framework;
using Xeon.UniTerminal.Binding;
using Xeon.UniTerminal.BuiltInCommands;
using Xeon.UniTerminal.Parsing;

namespace Xeon.UniTerminal.Tests
{
    // Binderテスト用のテストコマンド
    public enum TestMode
    {
        Fast,
        Slow,
        Normal
    }

    [Command("select", "Test select command")]
    public class SelectCommand : ICommand
    {
        [Option("verbose", "v", Description = "Verbose output")]
        public bool Verbose;

        [Option("count", "c", Description = "Count")]
        public int Count;

        [Option("targets", "t", Description = "Target list")]
        public List<string> Targets;

        [Option("mode", "m", Description = "Mode")]
        public TestMode Mode;

        public string CommandName => "select";
        public string Description => "Test select command";

        public System.Threading.Tasks.Task<ExitCode> ExecuteAsync(CommandContext context, System.Threading.CancellationToken ct)
        {
            return System.Threading.Tasks.Task.FromResult(ExitCode.Success);
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            yield break;
        }
    }

    [Command("grep", "Test grep command")]
    public class TestGrepCommand : ICommand
    {
        [Option("pattern", "p", isRequired: true, Description = "Pattern to search")]
        public string Pattern;

        [Option("ignorecase", "i", Description = "Ignore case")]
        public bool IgnoreCase;

        public string CommandName => "grep";
        public string Description => "Test grep command";

        public System.Threading.Tasks.Task<ExitCode> ExecuteAsync(CommandContext context, System.Threading.CancellationToken ct)
        {
            return System.Threading.Tasks.Task.FromResult(ExitCode.Success);
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            yield break;
        }
    }

    [Command("cmd", "Test command with options")]
    public class CmdCommand : ICommand
    {
        [Option("a", "a")]
        public bool A;

        [Option("b", "b")]
        public bool B;

        [Option("c", "c")]
        public bool C;

        [Option("name", "n")]
        public string Name;

        [Option("count", "")]
        public int Count;

        public string CommandName => "cmd";
        public string Description => "Test command";

        public System.Threading.Tasks.Task<ExitCode> ExecuteAsync(CommandContext context, System.Threading.CancellationToken ct)
        {
            return System.Threading.Tasks.Task.FromResult(ExitCode.Success);
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            yield break;
        }
    }

    public class BinderTests
    {
        private CommandRegistry registry;
        private Binder binder;
        private Parser parser;

        [SetUp]
        public void SetUp()
        {
            registry = new CommandRegistry();
            registry.RegisterCommand<SelectCommand>();
            registry.RegisterCommand<TestGrepCommand>();
            registry.RegisterCommand<CmdCommand>();
            binder = new Binder(registry);
            parser = new Parser();
        }

        // BND-010 不明なオプション（ロング）
        [Test]
        public void Bind_UnknownLongOption_ThrowsBindException()
        {
            var parsed = parser.Parse("select --unknown=1");
            var ex = Assert.Throws<BindException>(() => binder.Bind(parsed.Pipeline));
            Assert.AreEqual("select", ex.CommandName);
            Assert.AreEqual(ExitCode.UsageError, ex.ExitCode);
        }

        // BND-020 必須オプション不足
        [Test]
        public void Bind_RequiredOptionMissing_ThrowsBindException()
        {
            var parsed = parser.Parse("grep");
            var ex = Assert.Throws<BindException>(() => binder.Bind(parsed.Pipeline));
            Assert.AreEqual("grep", ex.CommandName);
            Assert.AreEqual(ExitCode.UsageError, ex.ExitCode);
        }

        // BND-030 値付きbool
        [Test]
        public void Bind_BoolWithValue_ThrowsBindException()
        {
            var parsed = parser.Parse("select --verbose=true");
            var ex = Assert.Throws<BindException>(() => binder.Bind(parsed.Pipeline));
            Assert.AreEqual(ExitCode.UsageError, ex.ExitCode);
        }

        // BND-040 結合ショートオプション
        [Test]
        public void Bind_BundledShortOptions_SetsAllTrue()
        {
            var parsed = parser.Parse("cmd -abc");
            var bound = binder.Bind(parsed.Pipeline);

            var cmd = (CmdCommand)bound.Commands[0].Command;
            Assert.IsTrue(cmd.A);
            Assert.IsTrue(cmd.B);
            Assert.IsTrue(cmd.C);
        }

        // BND-051 -n 10は許可
        [Test]
        public void Bind_ShortOptionWithSpaceValue_ParsedCorrectly()
        {
            var parsed = parser.Parse("cmd -n hello");
            var bound = binder.Bind(parsed.Pipeline);

            var cmd = (CmdCommand)bound.Commands[0].Command;
            Assert.AreEqual("hello", cmd.Name);
        }

        // BND-060 enum大文字小文字区別なし
        [Test]
        public void Bind_EnumCaseInsensitive_Converts()
        {
            var parsed = parser.Parse("select --mode=FAST");
            var bound = binder.Bind(parsed.Pipeline);

            var cmd = (SelectCommand)bound.Commands[0].Command;
            Assert.AreEqual(TestMode.Fast, cmd.Mode);
        }

        // BND-070 数値変換失敗
        [Test]
        public void Bind_NumericConversionFailure_ThrowsBindException()
        {
            var parsed = parser.Parse("select --count=abc");
            Assert.Throws<BindException>(() => binder.Bind(parsed.Pipeline));
        }

        // BND-080 リスト分割
        [Test]
        public void Bind_ListSplit_SplitsByComma()
        {
            var parsed = parser.Parse("select --targets=a,b,c");
            var bound = binder.Bind(parsed.Pipeline);

            var cmd = (SelectCommand)bound.Commands[0].Command;
            Assert.AreEqual(3, cmd.Targets.Count);
            Assert.AreEqual("a", cmd.Targets[0]);
            Assert.AreEqual("b", cmd.Targets[1]);
            Assert.AreEqual("c", cmd.Targets[2]);
        }

        // BND-081 リストクォート保護
        [Test]
        public void Bind_ListQuoteProtection_TreatsAsOneElement()
        {
            var parsed = parser.Parse("select --targets=\"a,b\"");
            var bound = binder.Bind(parsed.Pipeline);

            var cmd = (SelectCommand)bound.Commands[0].Command;
            Assert.AreEqual(1, cmd.Targets.Count);
            Assert.AreEqual("a,b", cmd.Targets[0]);
        }

        // BND-082 リスト空要素
        [Test]
        public void Bind_ListEmptyValue_CreatesListWithEmptyString()
        {
            var parsed = parser.Parse("select --targets=");
            var bound = binder.Bind(parsed.Pipeline);

            var cmd = (SelectCommand)bound.Commands[0].Command;
            Assert.AreEqual(1, cmd.Targets.Count);
            Assert.AreEqual("", cmd.Targets[0]);
        }

        // BND-083 リスト重複不許可
        [Test]
        public void Bind_ListDuplicate_ThrowsBindException()
        {
            var parsed = parser.Parse("select --targets=a --targets=b");
            Assert.Throws<BindException>(() => binder.Bind(parsed.Pipeline));
        }

        // BND-090 --はオプションパースを防止
        [Test]
        public void Bind_AfterEndOfOptions_TreatedAsPositional()
        {
            var parsed = parser.Parse("cmd -- --name=value");
            var bound = binder.Bind(parsed.Pipeline);

            var cmd = (CmdCommand)bound.Commands[0].Command;
            Assert.IsNull(cmd.Name);
            Assert.AreEqual(1, bound.Commands[0].PositionalArguments.Count);
            Assert.AreEqual("--name=value", bound.Commands[0].PositionalArguments[0]);
        }

        // ERR-030 コマンドが見つからない
        [Test]
        public void Bind_CommandNotFound_ThrowsBindException()
        {
            var parsed = parser.Parse("noSuchCmd");
            var ex = Assert.Throws<BindException>(() => binder.Bind(parsed.Pipeline));
            Assert.AreEqual("noSuchCmd", ex.CommandName);
            Assert.AreEqual(ExitCode.UsageError, ex.ExitCode);
        }

        // 追加テスト
        [Test]
        public void Bind_LongOptionWithSpaceValue_ParsedCorrectly()
        {
            var parsed = parser.Parse("grep --pattern hello");
            var bound = binder.Bind(parsed.Pipeline);

            var cmd = (TestGrepCommand)bound.Commands[0].Command;
            Assert.AreEqual("hello", cmd.Pattern);
        }

        [Test]
        public void Bind_MultiplePipelineCommands_AllBound()
        {
            var parsed = parser.Parse("grep --pattern=foo | cmd -a");
            var bound = binder.Bind(parsed.Pipeline);

            Assert.AreEqual(2, bound.Commands.Count);
            Assert.AreEqual("grep", bound.Commands[0].Command.CommandName);
            Assert.AreEqual("cmd", bound.Commands[1].Command.CommandName);
        }

        [Test]
        public void Bind_ScalarDuplicate_LastWins()
        {
            var parsed = parser.Parse("cmd --count=1 --count=2");
            var bound = binder.Bind(parsed.Pipeline);

            var cmd = (CmdCommand)bound.Commands[0].Command;
            Assert.AreEqual(2, cmd.Count);
        }

        [Test]
        public void Bind_BoolDuplicate_StaysTrue()
        {
            var parsed = parser.Parse("cmd -a -a");
            var bound = binder.Bind(parsed.Pipeline);

            var cmd = (CmdCommand)bound.Commands[0].Command;
            Assert.IsTrue(cmd.A);
        }

        [Test]
        public void Bind_UnknownShortOption_ThrowsBindException()
        {
            var parsed = parser.Parse("select -x");
            Assert.Throws<BindException>(() => binder.Bind(parsed.Pipeline));
        }
    }
}
