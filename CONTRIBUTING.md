# Contributing

Preserve the split between provider-neutral abstractions, domain services, and wire adapters. A domain service must not be documented as a wire implementation unless a protocol test proves that path.

Run Release build/test/pack and the Quick Start sample. Add cancellation, timeout, reentrancy, and concurrent-access tests for callback or registry changes. Breaking proposals belong in `docs/API_REVIEW.md`; do not silently reinterpret public result values. Never commit licensed standards, customer/internal material, captures, secrets, or build output.

No per-repository GitHub Actions convention exists and clean standalone restore currently depends on coordinated sibling checkouts. Add CI only with an explicit multi-repository checkout/package-consumption design.
