using System.Linq;

using Microsoft.CodeAnalysis;

namespace NoAsyncActions
{
	internal static class TypeSymbolExtensions
	{
		/// <summary>
		/// Determines if provided type symbol is async delegate. Returns null if type is not a delegate
		/// </summary>
		public static bool? IsAsyncDelegate( this ITypeSymbol typeSymbol, SemanticModel semanticModel, int position )
		{
			var invokeMethod = ( typeSymbol as INamedTypeSymbol )?.DelegateInvokeMethod;
			if ( invokeMethod is null )
				return null;
			return invokeMethod.ReturnType.OriginalDefinition.IsAwaitableNonDynamic( semanticModel, position );
		}

		// below is roslyn internals copy-paste, see https://stackoverflow.com/questions/44999586/how-to-identify-if-the-method-implementation-is-marked-as-async-can-be-called

		/// <summary>
		/// If the <paramref name="symbol"/> is a method symbol, returns <see langword="true"/> if the method's return type is "awaitable", but not if it's <see langword="dynamic"/>.
		/// If the <paramref name="symbol"/> is a type symbol, returns <see langword="true"/> if that type is "awaitable".
		/// An "awaitable" is any type that exposes a GetAwaiter method which returns a valid "awaiter". This GetAwaiter method may be an instance method or an extension method.
		/// </summary>
		private static bool IsAwaitableNonDynamic( this ITypeSymbol typeSymbol, SemanticModel semanticModel, int position )
		{
			// needs valid GetAwaiter
			var potentialGetAwaiters = semanticModel.LookupSymbols(
				position,
				container: typeSymbol,
				name: WellKnownMemberNames.GetAwaiter,
				includeReducedExtensionMethods: true
			);
			var getAwaiters = potentialGetAwaiters.OfType<IMethodSymbol>().Where( x => !x.Parameters.Any() );
			return getAwaiters.Any( x => VerifyGetAwaiter( x ) );
		}

		private static bool VerifyGetAwaiter( IMethodSymbol getAwaiter )
		{
			var returnType = getAwaiter.ReturnType;
			if ( returnType == null )
			{
				return false;
			}

			// bool IsCompleted { get }
			if ( !returnType.GetMembers().OfType<IPropertySymbol>().Any( p => p.Name == WellKnownMemberNames.IsCompleted && p.Type.SpecialType == SpecialType.System_Boolean && p.GetMethod != null ) )
			{
				return false;
			}

			var methods = returnType.GetMembers().OfType<IMethodSymbol>();

			// NOTE: (vladres) The current version of C# Spec, §7.7.7.3 'Runtime evaluation of await expressions', requires that
			// NOTE: the interface method INotifyCompletion.OnCompleted or ICriticalNotifyCompletion.UnsafeOnCompleted is invoked
			// NOTE: (rather than any OnCompleted method conforming to a certain pattern).
			// NOTE: Should this code be updated to match the spec?

			// void OnCompleted(Action) 
			// Actions are delegates, so we'll just check for delegates.
			if ( !methods.Any( x => x.Name == WellKnownMemberNames.OnCompleted && x.ReturnsVoid && x.Parameters.Length == 1 && x.Parameters.First().Type.TypeKind == TypeKind.Delegate ) )
			{
				return false;
			}

			// void GetResult() || T GetResult()
			return methods.Any( m => m.Name == WellKnownMemberNames.GetResult && !m.Parameters.Any() );
		}
	}
}
