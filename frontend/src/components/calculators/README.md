# Unused calculator components: do not wire back in

Nothing under `src/app` imports `UnifiedDosingCalculator.tsx`, `ReconstitutionCalc.tsx`, `VolumeCalc.tsx`,
`ConversionCalc.tsx` or `PowderAmountGuide.tsx`. Only unit tests render them. The live calculators are
`src/components/tools/ToolsDecisionSurface.tsx`, which uses `SyringeDrawVisualizer.tsx` and `VialVisualizer.tsx` from this folder.

Do not use the unused components on a page. They show daily and weekly totals and a dose-plan frame, which the
live surface deliberately dropped: BioStack never suggests, prefills or computes a dose, and calculators only convert
amounts the user types. They are kept only until they can be deleted with the owner's OK.
