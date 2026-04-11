export const glass = {
  light: {
    background:     "rgba(255,255,255,0.04)",
    border:         "1px solid rgba(255,255,255,0.08)",
    backdropFilter: "blur(12px)",
  },
  medium: {
    background:     "rgba(255,255,255,0.055)",
    border:         "1px solid rgba(255,255,255,0.10)",
    backdropFilter: "blur(16px)",
  },
  strong: {
    background:     "rgba(255,255,255,0.07)",
    border:         "1px solid rgba(255,255,255,0.14)",
    backdropFilter: "blur(20px)",
  },
} as const;

export type GlassIntensity = keyof typeof glass;
