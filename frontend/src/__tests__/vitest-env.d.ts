// Type-only: makes the full `tsc --noEmit -p tsconfig.json` (which includes tests) see the vitest
// globals and the jest-dom matchers that vitest.config.ts + setup.ts already provide at runtime.
// Not part of the production build (tsconfig.build.json excludes src/__tests__).
/// <reference types="vitest/globals" />
import '@testing-library/jest-dom/vitest';
