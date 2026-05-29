# PR 2 Transcript Provider Seam TODO

- [x] Add transcript provider contracts (interface + result records) in Application boundary.
- [x] Add deterministic fake transcript provider in tests.
- [x] Add static TB500 transcript fixture under test fixtures.
- [x] Add focused transcript provider tests:
  - [x] queued TB500 source maps to transcript resolution input
  - [x] fake provider resolves static fixture
  - [x] deterministic content + metadata assertions
  - [x] unknown reference deterministic failure
  - [x] no canonical knowledge writes
  - [x] no extraction/candidate/safety outputs
  - [x] no network-dependent paths
- [x] Run focused test slices and collect exact pass/fail/skip counts.
- [ ] Produce final PR report with required confirmations.
