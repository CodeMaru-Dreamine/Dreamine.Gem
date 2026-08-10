# Dreamine.Gem

Dreamine.Gem provides an initial, testable GEM equipment runtime built on the
Dreamine SECS-II/HSMS stack.

[➡️ 한국어 문서 보기](https://github.com/CodeMaru-Dreamine/Dreamine.Gem/blob/main/README_KO.md)

## Implemented boundary

- Communication, control, and equipment-processing state models
- Typed variable and equipment-constant registries
- Dynamic report links and collection-event snapshots
- Alarm state, remote-command, opaque process-program, clock, and bounded
  in-memory spool services
- Equipment-initiated and host-initiated S1F13/S1F14 communication
  establishment
- S1F1/S1F2 online identification
- `HsmsGemTransport`, an adapter over `Dreamine.Secs.Com.Hsms.HsmsSession`

All public domain values use immutable definitions and typed `SecsItem` values.
Time and cancellation are injectable at asynchronous boundaries. The runtime
does not own or dispose the supplied transport connection.

## Explicit limits

The normative local evidence for this first pass is SEMI E30-0611. The official
SEMI catalog identifies a newer revision, so this package does **not** claim
conformance, certification, or interoperability with the current revision.

Feature services are domain boundaries, not claims that every corresponding
SECS Stream/Function scenario is implemented. Wire handling is currently
limited to S1F13/S1F14 and S1F1/S1F2. Persistent spooling, trace collection,
limits monitoring, terminal services, E42 recipes, and E139 RaP are outside
this pass.

See [the requirements trace](./docs/SEMI_REQUIREMENTS_TRACE.md) and
[the sanitized local inventory](./docs/LOCAL_DOCUMENT_INVENTORY.md).

## Composition

```csharp
await using var session = new HsmsSession(options);
var transport = new HsmsGemTransport(session, options.SessionId);
var gem = new GemRuntime(transport, new GemEquipmentIdentity("MODEL", "1.0"));
```

Connection selection and lifecycle remain the caller's responsibility.

## License

MIT.
