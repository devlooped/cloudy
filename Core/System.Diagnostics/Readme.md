The System.Diagnostics API is evolving rapidly in the 5.0 timeframe 
to support more intuitive and streamline usage, driven in part from 
the OpenTelemetry effort that's going to be (partially?) subsumed 
in the Activity (and related) APIs.

See [dotnet/runtime#31373](https://github.com/dotnet/runtime/issues/31373)
and [dotnet/runtime#38419](https://github.com/dotnet/runtime/issues/38419).
and their associated design docs.

Since Azure Functions doesn't yet run on .NET 5.0, we bring the source 
instead so we can use the new bits and be ready when it does, without 
having to change implementation down the road.

Files are copied as-is from [dotnet/runtime](https://github.com/dotnet/runtime/tree/master/src/libraries/System.Diagnostics.DiagnosticSource/src/System/Diagnostics)
