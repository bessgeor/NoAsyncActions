using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NoAsyncActions
{
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class NoAsyncActionsAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "NoAsyncActions";

		private static readonly string _title = "Implicit async void behavior is restricted";
		private static readonly string _messageFormat = "Don't pass async delegate as action. Eighter allow accepting func of task or use explicit Task.Run in delegate body.";
		private static readonly string _description = "";
		private const string _category = "Reliability";

		private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor( DiagnosticId, _title, _messageFormat, _category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: _description );

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create( _rule );

		public override void Initialize( AnalysisContext context )
		{
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction( x => AnalyzeSyntax( x ), SyntaxKind.InvocationExpression );
		}

		private static void AnalyzeSyntax( SyntaxNodeAnalysisContext context )
		{
			var node = (InvocationExpressionSyntax) context.Node;
			var args = node.ArgumentList.Arguments;
			if ( args.Count == 0 )
				return;
			var symbolInfo = context.SemanticModel.GetSymbolInfo( node.Expression );
			if ( !( symbolInfo.Symbol is IMethodSymbol callSymbol ) )
				return;

			var zipped =
				callSymbol.Parameters
				.Select(p => p.Type)
				.Zip( args, ( p, a ) => (p, a) )
				.Where( t => t.p.IsAsyncDelegate(context.SemanticModel, position: node.GetLocation().SourceSpan.Start) == false )
				.Select( t => (aStx: t.a, aSmb: context.SemanticModel.GetSymbolInfo( t.a.Expression, context.CancellationToken ).Symbol) )
				.Where( t => !(t.aSmb is null))
			;

			foreach ( var (argSyntax, argSymbol) in zipped )
			{
				if ( argSymbol is IMethodSymbol methodSymbol && methodSymbol.IsAsync )
					Report();

				void Report() =>
					context.ReportDiagnostic( Diagnostic.Create( _rule, argSyntax.GetLocation() ) );
			}
		}
	}
}
