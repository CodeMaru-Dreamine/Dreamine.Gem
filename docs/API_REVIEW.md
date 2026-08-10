# Public API review

Review date: 2026-08-10. Baseline: 1.0.0 source. See [PUBLIC_API.md](PUBLIC_API.md).

## Result

- Dependencies are acyclic: implementation depends on GEM/SECS abstractions and the SECS runtime; abstractions never point back.
- Mutable registries are private and snapshots are returned through read-only contracts.
- Remote-command parameters are now copied before an asynchronous handler is invoked. Caller mutation can no longer race command validation/execution.
- Spool delivery now tolerates a callback that re-enters and purges the spool without corrupting the queue.
- No public signature or binary surface changed.

## Next-version proposals

| Classification | Proposal | Reason |
|---|---|---|
| Source- and binary-breaking | Introduce a typed spool enqueue result | A `bool` is insufficient to distinguish accepted, overwritten, disabled, and rejected outcomes. |
| Source- and binary-breaking | Add explicit async lifecycle/ownership to the runtime contract | Transport ownership is currently caller-managed and documented only. |
| Non-breaking candidate | Add a capability query for implemented wire mappings | Domain services currently exceed the implemented S1 wire adapter scope. |

Thread-safe registries protect their own state. User callbacks execute outside state locks where practical and receive immutable snapshots; callback side effects remain the consumer's responsibility.
