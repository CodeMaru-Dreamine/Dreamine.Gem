# Quick start

The buildable domain-service sample registers a status variable and equipment constant, collects an event/report snapshot, sets and clears an alarm, and runs a validated remote command:

```powershell
dotnet run --project samples/Dreamine.Gem.QuickStart
```

For S1F13/S1F14 and S1F1/S1F2 wire handling, compose `GemRuntime` with `HsmsGemTransport` over a connected and selected `HsmsSession`; the caller retains ownership of that session. Domain services do not imply that every related GEM Stream/Function is mapped to wire messages.

This sample is Unit/Sample Tested only. External interoperability remains a separate manual result; see [KNOWN_LIMITATIONS.md](KNOWN_LIMITATIONS.md).
