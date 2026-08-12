# Dreamine.Gem

Dreamine.Gem is a code-first GEM equipment/host implementation surface built
on the Dreamine SECS-II/HSMS stack.

[➡️ 한국어 문서 보기](README_KO.md)

## Status boundary

| Scope | Status | Evidence boundary |
|---|---|---|
| `E30-0611 derived subset profile v1` implementation surface | `IMPLEMENTED_UNVERIFIED` | Frozen source scope; not a conformance verdict. |
| Local unit and hand-built wire-fixture evidence | `PASS` | Structure, state, ACK, rejection, no-mutation, and correlation checks. |
| Local separate-process Host/Equipment actual-TCP evidence | `PASS` | The public QuickStart exercises every frozen dialogue definition and explicit equipment-origin directions. |
| External simulator or production equipment | `NOT_RUN` | Local evidence cannot promote an external or field result. |
| E37.1 conformance | `BLOCKED_STANDARD` | The required licensed revision is unavailable. |

The target sources are E30-0611 and E5-0813. This project does not claim
current-revision conformance, certification, or external interoperability.

## Public implementation surface

- `GemEquipmentProfileBuilder` freezes a validated, code-first equipment
  profile; each `CreateContext` call gets isolated mutable runtime state.
- `E30DemoEquipmentProfile.Create()` supplies a small generic public Demo
  profile without customer-equipment semantics.
- `E30HostClient` exposes typed calls and typed outcomes for the frozen host
  surface. `E30EquipmentRouter` owns exact equipment-side dispatch and explicit
  equipment-origin operations.
- `E30IdentifierPolicy` preserves independent CEID/RPTID/VID/SVID/DVID/ECID/
  ALID/DATAID integer formats. Raw ACK values remain observable.
- `HsmsGemTransport` adapts an application-owned `ISecsMessageSession`; the
  runtime does not own or dispose the supplied session.
- Existing domain services remain available for variables, constants, event/
  report snapshots, alarms, remote commands, process programs, clock, and
  bounded in-memory spooling. A domain service is not a wire-support claim.

## Frozen dialogue manifest

The exact profile name is `E30-0611 derived subset profile v1`. Its 20 normal
Primary/Secondary dialogue definitions are:

| Direction in the public Demo run | Dialogues |
|---|---|
| Host request → Equipment response | S1F1/F2, S1F3/F4, S1F11/F12, S1F13/F14, S1F15/F16, S1F17/F18; S2F13/F14, S2F15/F16, S2F17/F18, S2F29/F30, S2F31/F32, S2F33/F34, S2F35/F36, S2F37/F38, S2F41/F42; S5F3/F4, S5F5/F6; S6F15/F16 |
| Equipment Primary → Host response | S5F1/F2, S6F11/F12 |

The same actual-TCP run also checks equipment-initiated S1F1/F2 and S2F17/F18
through the router's typed operations. The router additionally exposes
equipment-initiated S1F13/F14. Direction is explicit; the 20-entry manifest
must not be interpreted as support for every dialogue in both directions.

## Code-first equipment composition

```csharp
var profile = E30DemoEquipmentProfile.Create();
var context = profile.CreateContext(new HsmsGemTransport(equipmentSession));

await using var router = new E30EquipmentRouter(
    equipmentSession,
    context,
    new E30EquipmentRouterOptions
    {
        CommandCompletionEvents = new Dictionary<string, ulong>
        {
            [E30DemoEquipmentProfile.StartCommand] =
                E30DemoEquipmentProfile.CommandCompletedEventId
        }
    });
```

The Host process uses the selected profile's identifier policy:

```csharp
using var host = new E30HostClient(
    hostSession,
    new E30IdentifierPolicy(profile.IdentifierFormats));

var result = await host.ReadStatusAsync(
    [E30DemoEquipmentProfile.EquipmentStateVariableId],
    cancellationToken);
```

Session connection, Select, dispatcher lifetime, and disconnection remain the
application's responsibility. See [Quick start](QUICKSTART.md) for the two-
process command lines.

## WPF Demo selection

The public WPF Workbench defaults to **E30-0611 derived subset profile v1
(Demo)** and uses `E30DemoEquipmentProfile.Create()` with
`E30EquipmentRouter`. Its alternative **Educational basic responder
(Demo-only, not GEM)** is a seven-pair teaching fallback. Selection is mutually
exclusive: the frozen router and the fallback responder are never registered
at the same time. See the [Workbench README](../../998.%20DEMO/000.%20Sample/010.%20Wpfs/Dreamine.SecsGem.Interop.Wpf/README.md).

## Explicit exclusions and blocked semantics

- S6F19/F20 for an unknown RPTID is `BLOCKED_STANDARD`.
- S2F35 empty outer-list and empty RPTID-list unlink/delete variants are
  `BLOCKED_STANDARD`; v1 terminates them without mutation.
- S9F1, S9F9, and S9F13 are `INTENTIONALLY_EXCLUDED` from frozen v1 because the
  current safe provider/router boundary does not expose the source context
  needed to construct them without invention.
- Multi-block S2F39/F40 and S6F5/F6, trace, limits, wire spooling, S7 process-
  program wire services, S10 terminal services, material handling, and enhanced
  remote command are `INTENTIONALLY_EXCLUDED`.

See [Known limitations](KNOWN_LIMITATIONS.md), the
[requirements trace](docs/SEMI_REQUIREMENTS_TRACE.md), and the
[public API review](docs/API_REVIEW.md).

## License

MIT.
