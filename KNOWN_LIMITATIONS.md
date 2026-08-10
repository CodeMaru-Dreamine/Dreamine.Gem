# Known limitations

- Wire handling is limited to the implemented S1F13/S1F14 and S1F1/S1F2 paths. Domain services do not imply complete GEM message support.
- Spooling is bounded and in-memory; it is not durable across process failure.
- Event snapshots use a stable definition/link snapshot, but independent external variable readers are not one physical atomic sample.
- Trace collection, limit monitoring, terminal services, recipe wire services, persistent recovery, and certification are outside this release.
- External interoperability remains **Not Run / Waiting for User** unless separately executed and recorded.
