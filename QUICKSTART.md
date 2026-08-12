# Quick start

`Dreamine.Gem.QuickStart` runs the public generic Demo profile as two separate
processes over actual TCP/HSMS. Start Equipment first, then Host.

The exact frozen scope label is `E30-0611 derived subset profile v1`. This is a
local executable example, not current-revision conformance or external
interoperability evidence.

## 1. Start the passive Equipment process

From the `Dreamine.Gem` repository root:

```powershell
dotnet run --project samples/Dreamine.Gem.QuickStart -- `
  --role equipment `
  --host 127.0.0.1 `
  --port 5000 `
  --session-id 37 `
  --timeout-seconds 45 `
  --evidence equipment-evidence.json
```

The Equipment role creates `E30DemoEquipmentProfile`, an isolated
`GemEquipmentContext`, and `E30EquipmentRouter`. It waits for Select and
communication establishment, performs explicit equipment-origin checks, and
serves the Host requests.

## 2. Start the active Host process

In a second terminal, with the same endpoint and nonzero Session ID:

```powershell
dotnet run --project samples/Dreamine.Gem.QuickStart -- `
  --role host `
  --host 127.0.0.1 `
  --port 5000 `
  --session-id 37 `
  --timeout-seconds 45 `
  --evidence host-evidence.json
```

The Host role uses `E30HostClient` and verifies typed success, timeout,
cancellation, raw ACK, W-bit, System Bytes correlation, and the selected
Session ID. A process returns `0` only after its bounded evidence checks pass;
configuration/runtime failure returns `1`, and bounded timeout/cancellation
returns `2`.

## Exercised dialogue directions

The run covers all 20 entries in the frozen manifest:

| Direction | Dialogues |
|---|---|
| Host request → Equipment response | S1F1/F2, S1F3/F4, S1F11/F12, S1F13/F14, S1F15/F16, S1F17/F18; S2F13/F14, S2F15/F16, S2F17/F18, S2F29/F30, S2F31/F32, S2F33/F34, S2F35/F36, S2F37/F38, S2F41/F42; S5F3/F4, S5F5/F6; S6F15/F16 |
| Equipment Primary → Host response | S5F1/F2, S6F11/F12 |

It also exercises equipment-initiated S1F1/F2 and S2F17/F18 as explicit
direction checks. `E30EquipmentRouter.EstablishCommunicationsAsync` separately
exposes equipment-initiated S1F13/F14. Do not infer that every manifest entry is
implemented in both directions.

## Result interpretation

| Result surface | Status |
|---|---|
| Frozen implementation surface | `IMPLEMENTED_UNVERIFIED` |
| Local unit and hand-built fixture evidence | `PASS` |
| This separate-process actual-TCP run, when both roles return `0` | `PASS` |
| External simulator or production equipment | `NOT_RUN` |
| E37.1 conformance | `BLOCKED_STANDARD` |

S6F19/F20 unknown-RPTID representation and S2F35 empty-list unlink/delete
semantics remain `BLOCKED_STANDARD`. S9F1, S9F9, S9F13, multi-block, and the
other capability families named in [Known limitations](KNOWN_LIMITATIONS.md)
remain `INTENTIONALLY_EXCLUDED` from frozen v1.

## WPF Workbench

The Workbench can select the same public Demo profile. Its alternative
educational fallback is Demo-only, not GEM, and is mutually exclusive with the
E30 router. See the [Workbench README](../../998.%20DEMO/000.%20Sample/010.%20Wpfs/Dreamine.SecsGem.Interop.Wpf/README.md).

For composition details, return to the [main README](README.md). For normative
disposition and evidence separation, see the
[requirements trace](docs/SEMI_REQUIREMENTS_TRACE.md).
