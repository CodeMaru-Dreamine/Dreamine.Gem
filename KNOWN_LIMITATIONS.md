# Known limitations

This file describes `E30-0611 derived subset profile v1`. The target sources
are E30-0611 and E5-0813; no item below is a current-revision conformance,
certification, or field-interoperability claim.

## Evidence limits

| Surface | Status | Limit |
|---|---|---|
| Frozen implementation surface | `IMPLEMENTED_UNVERIFIED` | Public code exists, but external verification remains outstanding. |
| Local unit, hand-built fixture, and actual-TCP evidence | `PASS` | Local automated evidence only. |
| External simulator and production equipment | `NOT_RUN` | No external result is inferred from local execution. |
| E37.1 conformance | `BLOCKED_STANDARD` | The required licensed revision is unavailable. |

## Wire scope

- The manifest contains exactly 20 normal dialogue definitions. The direction
  exercised by the public sample is listed in [Quick start](QUICKSTART.md).
- `E30HostClient` and `E30EquipmentRouter` expose typed outcomes, but raw nonzero
  ACK values remain peer rejection data; they are not normalized into success.
- Remote-command acceptance and completion are separate. An accepted HCACK does
  not prove execution completion; the configured completion CEID is required.
- S9F3, S9F5, S9F7, and S9F11 are limited to errors observable with the original
  offending-header context at the equipment router.

## Blocked standard semantics

- Unknown-RPTID response representation for S6F19/F20 is `BLOCKED_STANDARD`.
- The meaning of an empty S2F35 outer link list or an empty RPTID list as
  unlink/delete is `BLOCKED_STANDARD`. Frozen v1 sends F0 and performs no
  mutation for those variants.

## Intentionally excluded from frozen v1

- S9F1, S9F9, and S9F13 are `INTENTIONALLY_EXCLUDED`. The safe provider/router
  boundary does not expose the required source-bound context, and v1 does not
  fabricate it.
- Multi-block S2F39/F40 and S6F5/F6 are `INTENTIONALLY_EXCLUDED`; the router
  enforces a configured single-block body limit.
- Trace, limits monitoring, wire spooling, S7 process-program wire services,
  S10 terminal services, material handling, and S2F49/F50 enhanced remote
  command are `INTENTIONALLY_EXCLUDED`.

## Runtime and application limits

- The provider-neutral transport does not own the supplied
  `ISecsMessageSession`. Applications own connection, Select, dispatch,
  disconnect, and disposal.
- Each `GemEquipmentProfile.CreateContext` call isolates mutable state; sharing
  one context between equipment identities is an application error.
- The command worker is bounded. Queue saturation, timeout, cancellation, or a
  missing completion-event mapping is reported as rejection/failure and is not
  converted into a fabricated peer acknowledgement.
- Domain spooling is bounded and in-memory; it is not durable across process
  failure. Event snapshots stabilize definitions, links, and ordered results,
  but independent external readers are not one physical atomic acquisition.
- The WPF educational fallback is Demo-only, not GEM, and is mutually exclusive
  with the frozen E30 router.

See the [requirements trace](docs/SEMI_REQUIREMENTS_TRACE.md) for separate
normative disposition, implementation status, and evidence status.
