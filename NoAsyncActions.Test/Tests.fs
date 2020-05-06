namespace NoAsyncActions
module Tests =
  open HellBrick.Diagnostics.Assertions
  open NoAsyncActions
  open Swensen.Unquote
  open Xunit

  let verifier = AnalyzerVerifier.UseAnalyzer<NoAsyncActionsAnalyzer>()

  let shouldHaveNoDiagnostics source =
    verifier
      .Source(source)
      .ShouldHaveNoDiagnostics()

  let shouldHaveSingleNoAsyncActionsAnalyzerDiagnostic source =
    verifier
      .Source(source)
      .ShouldHaveDiagnostics
        (fun d ->
          test <@ d.Length = 1 @>
          test <@ d.[0].Id = NoAsyncActionsAnalyzer.DiagnosticId @>)

  [<Fact>]
  let ``Diagnostic is not reported on parameterless async lambda passed as Func of Task`` () =
    shouldHaveNoDiagnostics "
namespace Whatever
{
	class C
	{
		void Call(Func<Task> a) {}
		void Test() => Call(async () => {});
	}
}"

  [<Fact>]
  let ``Diagnostic is not reported on parameterless sync lambda passed as Action`` () =
    shouldHaveNoDiagnostics "
namespace Whatever
{
	class C
	{
		void Call(System.Action a) {}
		void Test() => Call(() => {});
	}
}"

  [<Fact>]
  let ``Diagnostic is reported on parameterless async lambda passed as Action`` () =
    shouldHaveSingleNoAsyncActionsAnalyzerDiagnostic "
namespace Whatever
{
	class C
	{
		void Call(System.Action a) {}
		void Test() => Call(async () => {});
	}
}"

  [<Fact>]
  let ``Diagnostic is reported on simple async lambda passed as single-parameter Action`` () =
    shouldHaveSingleNoAsyncActionsAnalyzerDiagnostic "
namespace Whatever
{
	class C
	{
		void Call(System.Action<int> a) {}
		void Test() => Call(async _ => {});
	}
}"

  [<Fact>]
  let ``Diagnostic is reported on parenthesized async lambda passed as single-parameter Action`` () =
    shouldHaveSingleNoAsyncActionsAnalyzerDiagnostic "
namespace Whatever
{
	class C
	{
		void Call(System.Action<int> a) {}
		void Test() => Call(async (_) => {});
	}
}"

  [<Fact>]
  let ``Diagnostic is reported on async lambda passed as multi-parameter Action`` () =
    shouldHaveSingleNoAsyncActionsAnalyzerDiagnostic "
namespace Whatever
{
	class C
	{
		void Call(System.Action<int, float, string> a) {}
		void Test() => Call(async (a, b, c) => {});
	}
}"

  [<Fact>]
  let ``Diagnostic is reported on parameterless async lambda passed as Action alongside other parameters`` () =
    shouldHaveSingleNoAsyncActionsAnalyzerDiagnostic "
namespace Whatever
{
	class C
	{
		void Call(int a, System.Action b, float c, string d) {}
		void Test() => Call(1, async () => {}, 2, \"3\");
	}
}"

  [<Fact>]
  let ``Diagnostic is reported on parameterless async lambda passed as Action alongside other valid lambda parameters`` () =
    shouldHaveSingleNoAsyncActionsAnalyzerDiagnostic "
namespace Whatever
{
	using System;

	class C
	{
		void Call(Action a, Action b, Func<System.Threading.Tasks.Task> d) {}
		void Test() => Call(() => {}, async () => {}, async () => {});
	}
}"
