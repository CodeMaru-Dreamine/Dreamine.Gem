# Public API review

Review date: 2026-08-12. Comparison baseline: the checked-in `1.0.0` source.
The final release-reflection inventory is generated separately in
[PUBLIC_API.md](PUBLIC_API.md).

## Result

The working public surface is additive; this is not a no-public-change pass.

- Code-first profile types were added: `GemEquipmentProfile`,
  `GemEquipmentContext`, and `GemEquipmentProfileBuilder`. `GemRuntime.Profile`
  exposes the applied immutable profile.
- `E30DemoEquipmentProfile` provides the generic public Demo profile shared by
  the QuickStart and WPF Workbench.
- Typed host/equipment endpoints were added through `E30HostClient`,
  `E30EquipmentRouter`, and `E30EquipmentRouterOptions`.
- The frozen protocol surface adds `E30DerivedSubsetManifest`, `E30Dialogues`,
  `E30IdentifierPolicy`, `E30CallResult<T>`, typed wire models,
  `E30WireCodec`, and `E30WireFormatException`.
- Existing domain services gained additive typed registration, atomic batch,
  snapshot, and event/report configuration members. Legacy Boolean and
  single-item members remain available.
- `HsmsGemTransport(ISecsMessageSession)` is additive. The existing
  `HsmsGemTransport(HsmsSession, SecsSessionId)` constructor remains available
  and retains its explicit Session ID behavior.
- Session ownership remains with the caller. Exact dispatcher registrations are
  disposed by the host client/router; neither endpoint disposes the session.

No final exported-type count is recorded here. `PUBLIC_API.md`, regenerated
from the frozen Release assemblies, is the inventory authority.

## Compatibility and behavior review

| Item | Disposition |
|---|---|
| Existing public signatures | No removal or signature replacement identified in the source diff; new overloads, members, and types are additive. |
| Collection exposure | Profile definitions and snapshots use read-only, copied collections; mutable runtime state stays context-local. |
| Identifier formats | CEID, RPTID, VID, SVID, DVID, ECID, ALID, and DATAID are independently policy-controlled. |
| Remote commands | Parameter definitions are typed; caller input is copied before asynchronous execution; acceptance is separate from completion. |
| Event/report configuration | Batch operations stage and validate before mutation; ordered report/VID snapshots are retained. |
| Equipment constants | Typed batch updates validate all entries before an atomic apply. |
| Callback isolation | Application callbacks run outside core state locks where practical; callback side effects remain application-owned. |

## Status and evidence

| Review surface | Status | Evidence boundary |
|---|---|---|
| Frozen `E30-0611 derived subset profile v1` public surface | `IMPLEMENTED_UNVERIFIED` | Source/API review; not external evidence. |
| Local unit and hand-built fixture evidence | `PASS` | Automated local execution. |
| Local separate-process actual-TCP evidence | `PASS` | Public Host/Equipment QuickStart only. |
| External simulator or production equipment | `NOT_RUN` | Not implied by API compatibility review. |
| E37.1 conformance | `BLOCKED_STANDARD` | Required licensed source is unavailable. |

## Version-pair risk

`Dreamine.Gem` and `Dreamine.Gem.Abstractions` still declare version `1.0.0`
while both working public surfaces contain additions. Reusing an earlier
`1.0.0` package identity can select stale binaries from a NuGet cache or pair a
new implementation assembly with an old abstractions assembly. Use one unique
candidate version across the pair, a clean package cache, and a local-feed-only
consumer smoke before any publication decision.

## Next-version proposals

| Classification | Proposal | Reason |
|---|---|---|
| Source- and binary-breaking | Introduce a typed spool enqueue result. | A Boolean cannot distinguish accepted, overwritten, disabled, and rejected outcomes. |
| Source- and binary-breaking | Add explicit async lifecycle/ownership to the runtime contract. | Transport ownership is currently caller-managed and documented rather than expressed by `IGemRuntime`. |
| Non-breaking candidate | Add a public capability query at the domain-service boundary. | A domain service must not imply that its wire family is in the frozen profile. |

The API review does not promote implementation status to conformance. See the
[requirements trace](SEMI_REQUIREMENTS_TRACE.md) and
[Known limitations](../KNOWN_LIMITATIONS.md).
