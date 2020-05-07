# NoAsyncActions
This is a Roslyn analyzer preventing implicit async void delegate usage.


[![Build status](https://ci.appveyor.com/api/projects/status/45y0l3khsdkpufjn?svg=true)](https://ci.appveyor.com/project/bessgeor/noasyncactions)
[![NoAsyncActions nuget package version](https://buildstats.info/nuget/NoAsyncActions?includePreReleases=true)](https://www.nuget.org/packages/NoAsyncActions)
[![NoAsyncActions myget dev package version](https://buildstats.info/myget/noasyncactions/NoAsyncActions?includePreReleases=true)](https://www.myget.org/feed/noasyncactions/package/nuget/NoAsyncActions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Only delegate-accepting method (in Roslyn sense of "method", eg. including constructors, delegate invocations and so on) calls are currently supported. Example of diagnostic-reporting code:
```csharp
namespace Whatever
{
	class C
	{
		void Call(System.Action<int> a) {}
		void Test() => Call(async _ => {});
	}
}
```

See [tests](https://github.com/bessgeor/NoAsyncActions/blob/master/NoAsyncActions.Test/Tests.fs) for more details on supported analysis.

Best fit to use with
`CS1998 Async method lacks 'await' operators and will run synchronously`
and/or `CS4014 Because this call is not awaited, execution of the current method continues before the call is completed`
configured to error severity (if no WarningsAsErrors used).
