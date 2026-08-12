# GEM requirements trace

Review date: 2026-08-12

The implementation target is the exact frozen label
`E30-0611 derived subset profile v1`, derived from locally held E30-0611 and
E5-0813 source revisions. This trace is not a current-revision conformance,
certification, or external-interoperability statement.

## Status model

Normative disposition and evidence status are separate columns:

- **Normative disposition** says whether the available source closes the
  intended v1 semantics: Included in frozen v1, `BLOCKED_STANDARD`, or
  `INTENTIONALLY_EXCLUDED`.
- **Implementation status** is `IMPLEMENTED_UNVERIFIED` for the public frozen
  surface. It is not promoted by local tests.
- **Evidence status** is `PASS` only for the named local test surface,
  `NOT_RUN` for external/field work, and `BLOCKED_STANDARD` when required
  licensed material is unavailable.

## Source boundary

| Source | Available revision | Use in this profile | Evidence status |
|---|---|---|---|
| SEMI E30 | E30-0611 | GEM state/function source for the frozen derived subset | `PASS` — local source identification only |
| SEMI E5 | E5-0813 | SECS-II message structures and data items for the frozen subset | `PASS` — local source identification only |
| SEMI E37 | E37-0413 | Lower HSMS transport background; not an E37.1 substitute | `PASS` — local source identification only |
| SEMI E37.1 | Required revision unavailable | No conformance inference or guessed contract | `BLOCKED_STANDARD` |

Newer revisions may exist. Their changes are outside this evidence set, so the
profile must not be represented as “current standard,” “GEM compliant,” or
“certified.”

## Frozen included dialogue trace

All rows below are included in frozen v1, have implementation status
`IMPLEMENTED_UNVERIFIED`, and external/field evidence `NOT_RUN`.

| Function area | Frozen dialogue definitions | Direction exercised by public Demo | Local unit/fixture evidence | Local separate-process actual TCP |
|---|---|---|---|---|
| Communication and identity | S1F1/F2, S1F13/F14 | Host→Equipment for both; explicit Equipment→Host S1F1/F2; router also exposes Equipment→Host S1F13/F14 | `PASS` | `PASS` |
| Status variables | S1F3/F4, S1F11/F12 | Host→Equipment | `PASS` | `PASS` |
| Offline/online request | S1F15/F16, S1F17/F18 | Host→Equipment | `PASS` | `PASS` |
| Equipment constants | S2F13/F14, S2F15/F16, S2F29/F30 | Host→Equipment | `PASS` | `PASS` |
| Clock | S2F17/F18, S2F31/F32 | Host→Equipment; explicit Equipment→Host S2F17/F18 read | `PASS` | `PASS` |
| Dynamic event reports | S2F33/F34, S2F35/F36, S2F37/F38 | Host→Equipment, single-block supported forms only | `PASS` | `PASS` |
| Remote command | S2F41/F42 | Host→Equipment; acceptance is separate from configured completion CEID | `PASS` | `PASS` |
| Alarm management | S5F3/F4, S5F5/F6; S5F1/F2 | Host→Equipment configuration/query; Equipment→Host alarm report | `PASS` | `PASS` |
| Collection-event report | S6F15/F16; S6F11/F12 | Host→Equipment query; Equipment→Host event report | `PASS` | `PASS` |

The table names exactly 20 normal Primary/Secondary definitions in the public
manifest. Direction is explicit; inclusion does not mean each dialogue is
implemented in both directions.

## Fundamental error disposition

| Message | Normative disposition | Implementation status | Local evidence | External/field evidence | Rationale |
|---|---|---|---|---|---|
| S9F3, S9F5 | Included in frozen v1 | `IMPLEMENTED_UNVERIFIED` | `PASS` | `NOT_RUN` | Equipment exact-dispatch fallback distinguishes unknown Host stream/function and preserves the observable offending header. |
| S9F7 | Included in frozen v1 | `IMPLEMENTED_UNVERIFIED` | `PASS` | `NOT_RUN` | Observable known-dialogue structural failure produces illegal-data with the offending header. |
| S9F11 | Included in frozen v1 | `IMPLEMENTED_UNVERIFIED` | `PASS` | `NOT_RUN` | Body over the configured single-block boundary produces data-too-long with the offending header. |
| S9F1 | `INTENTIONALLY_EXCLUDED` | `IMPLEMENTED_UNVERIFIED` | `PASS` — exclusion/manifest assertion | `NOT_RUN` | Session mismatch is rejected below the router, which does not receive source-bound offending context. |
| S9F9 | `INTENTIONALLY_EXCLUDED` | `IMPLEMENTED_UNVERIFIED` | `PASS` — exclusion/manifest assertion | `NOT_RUN` | The safe transaction API reports timeout without exposing the original header/System Bytes required for SHEAD. |
| S9F13 | `INTENTIONALLY_EXCLUDED` | `IMPLEMENTED_UNVERIFIED` | `PASS` — exclusion/manifest assertion | `NOT_RUN` | Frozen v1 has no conversation state providing both required source values; no synthetic conversation is invented. |

## Blocked semantics

| Capability | Normative disposition | Implementation status | Local evidence | External/field evidence | Safe behavior |
|---|---|---|---|---|---|
| S6F19/F20 unknown-RPTID response representation | `BLOCKED_STANDARD` | `IMPLEMENTED_UNVERIFIED` | `PASS` — manifest assertion | `NOT_RUN` | No response shape is guessed. |
| S2F35 empty outer list or empty RPTID-list unlink/delete | `BLOCKED_STANDARD` | `IMPLEMENTED_UNVERIFIED` | `PASS` — F0/no-mutation path | `NOT_RUN` | Transaction terminates with F0 and staged state is not mutated. |
| E37.1 conformance | `BLOCKED_STANDARD` | `IMPLEMENTED_UNVERIFIED` | `BLOCKED_STANDARD` | `NOT_RUN` | Required licensed revision is unavailable. |

The domain service may support explicit unlink/delete operations. That does not
close the blocked S2F35 wire encoding semantics.

## Intentionally excluded capability families

| Capability family | Normative disposition | Implementation status | Local evidence | External/field evidence |
|---|---|---|---|---|
| Multi-block S2F39/F40 and S6F5/F6 | `INTENTIONALLY_EXCLUDED` | `IMPLEMENTED_UNVERIFIED` | `PASS` — single-block boundary/rejection tests | `NOT_RUN` |
| Trace S2F23/F24 and S6F1/F2 | `INTENTIONALLY_EXCLUDED` | `IMPLEMENTED_UNVERIFIED` | `PASS` — manifest assertion | `NOT_RUN` |
| Limits S2F45/F46/F47/F48 | `INTENTIONALLY_EXCLUDED` | `IMPLEMENTED_UNVERIFIED` | `PASS` — manifest assertion | `NOT_RUN` |
| Wire spooling S2F43/F44 and S6F23/F24 | `INTENTIONALLY_EXCLUDED` | `IMPLEMENTED_UNVERIFIED` | `PASS` — manifest assertion | `NOT_RUN` |
| S7 process-program wire, S10 terminal, material handling, S2F49/F50 enhanced remote command | `INTENTIONALLY_EXCLUDED` | `IMPLEMENTED_UNVERIFIED` | `PASS` — manifest assertion | `NOT_RUN` |

In-memory process-program and spool domain services do not change these wire
dispositions.

## Local evidence surfaces

| Evidence surface | Status | What it proves | What it does not prove |
|---|---|---|---|
| `Dreamine.Gem.Abstractions.Tests` profile-model tests | `PASS` | Immutable snapshots, identifier policy, typed definition validation | Wire interoperability |
| `Dreamine.Gem.Tests` unit/state/service tests | `PASS` | State gates, atomic mutation, command completion policy, isolation | External peer behavior |
| Hand-built E5 wire fixtures | `PASS` | Exact local structures, identifier formats, ALED/ACK parsing, F0/S9 headers | Current-revision conformance |
| In-process actual-TCP router loopback | `PASS` | HSMS/SECS correlation and frozen dialogue behavior on the Dreamine stack | Independent implementation interoperability |
| Separate Host/Equipment QuickStart processes | `PASS` | Public sample composition and all frozen dialogue definitions over local TCP | Simulator, production equipment, certification |
| WPF profile selector and mutual-exclusion tests | `PASS` | Demo profile/fallback selection contract | Human visual approval or external interoperability |
| External simulator | `NOT_RUN` | No evidence yet | Nothing may be inferred from local PASS rows |
| Production equipment / field | `NOT_RUN` | No evidence yet | Nothing may be inferred from local PASS rows |

This trace intentionally records no aggregate test count. Fresh counts and
environment details belong in the central release evidence report rather than
in the normative disposition table.
