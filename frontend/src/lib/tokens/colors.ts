export const brandColors = {
  bg: {
    primary:   "#0B0F14",
    secondary: "#0F141B",
    elevated:  "#121923",
    panel:     "#172033",
  },
  text: {
    primary:   "#F8FAFC",
    secondary: "rgba(248,250,252,0.72)",
    muted:     "rgba(248,250,252,0.48)",
    inverse:   "#0F172A",
  },
  brand: {
    green:     "#22C55E",
    greenSoft: "rgba(34,197,94,0.16)",
    greenGlow: "rgba(34,197,94,0.30)",
  },
  accent: {
    blue:  "#3B82F6",
    amber: "#F59E0B",
    red:   "#EF4444",
  },
  border: {
    subtle: "rgba(255,255,255,0.08)",
    strong: "rgba(255,255,255,0.14)",
  },
} as const;

export type BrandColor = typeof brandColors;
