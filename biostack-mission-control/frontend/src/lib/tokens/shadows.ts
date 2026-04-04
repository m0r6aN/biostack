export const shadows = {
  card:            "0 8px 24px rgba(0,0,0,0.32)",
  elevated:        "0 12px 32px rgba(0,0,0,0.4)",
  glowGreenSoft:   "0 0 0 1px rgba(34,197,94,0.14), 0 8px 24px rgba(0,0,0,0.42)",
  glowGreenStrong: "0 0 0 1px rgba(34,197,94,0.22), 0 0 24px rgba(34,197,94,0.18)",
  insetSoft:       "inset 0 1px 0 rgba(255,255,255,0.06)",
} as const;

export type Shadow = typeof shadows;
