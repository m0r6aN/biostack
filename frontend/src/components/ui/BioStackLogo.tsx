import { cn } from '@/lib/utils';

// ─── Types ────────────────────────────────────────────────────────────────────

export type BioStackLogoVariant = 'icon' | 'horizontal' | 'stacked';
export type BioStackTheme      = 'dark' | 'light';
export type BioStackSize       = 'sm' | 'md' | 'lg';

export interface BioStackLogoProps {
  /** Layout variant: icon mark only, icon + wordmark side-by-side, or icon + wordmark stacked */
  variant?: BioStackLogoVariant;
  /** Color theme — dark (default) or light */
  theme?: BioStackTheme;
  /** Enables the signal pulse animation. Use for hero sections / active analysis states. */
  animated?: boolean;
  /** Adds a static base glow to the signal. Use for hero/empty states. */
  glow?: boolean;
  /** Enables the layer-activation loading animation. Overrides `animated`. */
  loading?: boolean;
  /** Enables the hover-lift interaction. Use for clickable/link logo contexts. */
  hoverable?: boolean;
  /** Icon size */
  size?: BioStackSize;
  className?: string;
  wordmarkClassName?: string;
}

// ─── Constants ────────────────────────────────────────────────────────────────

const ICON_PX: Record<BioStackSize, number> = {
  sm: 28,
  md: 40,
  lg: 56,
};

const WORDMARK_CLASS: Record<BioStackSize, string> = {
  sm: 'text-base',
  md: 'text-xl',
  lg: 'text-3xl',
};

// SVG viewBox: 160 × 160
//
// Three isometric rhombus plates stacked front-to-back:
//   Plate 1 (top / front)  — filled, highest opacity
//   Plate 2 (middle)       — filled, medium opacity
//   Plate 3 (bottom / back)— outline only, for depth
//
// EKG waveform runs at y ≈ 64, crossing all three plates.
//
// Render order (SVG painter model): plate 3 → plate 2 → plate 1 → signal
// so the signal always reads on top.

const PLATE_1 = 'M 80,18 L 148,50 L 80,70 L 12,50 Z';
const PLATE_2 = 'M 80,58 L 145,88 L 80,108 L 15,88 Z';
const PLATE_3 = 'M 80,96 L 140,122 L 80,142 L 20,122 Z';

// EKG: flat baseline → P wave → QRS complex → T wave → flat
const SIGNAL = 'M 24,64 L 40,64 L 50,51 L 58,77 L 70,24 L 85,64 L 97,52 L 110,64 L 136,64';

const PLATE_FILL: Record<BioStackTheme, { p1: string; p2: string }> = {
  dark:  { p1: '#2A3D54', p2: '#1E2D3E' },
  light: { p1: '#94A3B8', p2: '#7B8FA0' },
};

const PLATE_STROKE: Record<BioStackTheme, string> = {
  dark:  '#2A3D54',
  light: '#8094A8',
};

const WORDMARK_COLOR: Record<BioStackTheme, string> = {
  dark:  '#F8FAFC',
  light: '#0F172A',
};

const SIGNAL_COLOR = '#22C55E';

// ─── Component ────────────────────────────────────────────────────────────────

export function BioStackLogo({
  variant         = 'horizontal',
  theme           = 'dark',
  animated        = false,
  glow            = false,
  loading         = false,
  hoverable       = false,
  size            = 'md',
  className,
  wordmarkClassName,
}: BioStackLogoProps) {
  const iconPx     = ICON_PX[size];
  const fills      = PLATE_FILL[theme];
  const stroke     = PLATE_STROKE[theme];
  const isPulsing  = animated && !loading;
  const isLoading  = loading;

  return (
    <div
      className={cn(
        'inline-flex items-center',
        variant === 'stacked' ? 'flex-col gap-2' : 'gap-2.5',
        hoverable  && 'biostack-hover',
        isLoading  && 'biostack-loading',
        className
      )}
    >
      <svg
        width={iconPx}
        height={iconPx}
        viewBox="0 0 160 160"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
        role="img"
        aria-label="BioStack"
        className="overflow-visible shrink-0"
      >
        {/* ── Plate 3 — bottom / back, outline only ────────────────────── */}
        <path
          d={PLATE_3}
          fill="none"
          stroke={stroke}
          strokeWidth="2"
          opacity="0.60"
          className="biostack-plate-3"
        />

        {/* ── Plate 2 — middle ─────────────────────────────────────────── */}
        <path
          d={PLATE_2}
          fill={fills.p2}
          stroke={stroke}
          strokeWidth="1.5"
          opacity="0.86"
          className="biostack-plate-2"
        />

        {/* ── Plate 1 — top / front ────────────────────────────────────── */}
        <path
          d={PLATE_1}
          fill={fills.p1}
          stroke={stroke}
          strokeWidth="1.5"
          opacity="0.96"
          className="biostack-plate-1"
        />

        {/* ── EKG signal ───────────────────────────────────────────────── */}
        <path
          d={SIGNAL}
          fill="none"
          stroke={SIGNAL_COLOR}
          strokeWidth="4.5"
          strokeLinecap="round"
          strokeLinejoin="round"
          className={cn(
            'biostack-signal-path',
            isPulsing && 'biostack-signal',
            glow && !isPulsing && !isLoading && 'biostack-glow-static'
          )}
        />
      </svg>

      {/* ── Wordmark ─────────────────────────────────────────────────────── */}
      {variant !== 'icon' && (
        <span
          className={cn(
            'font-semibold tracking-tight select-none leading-none',
            WORDMARK_CLASS[size],
            wordmarkClassName
          )}
          style={{ color: WORDMARK_COLOR[theme] }}
        >
          Bio<span style={{ color: SIGNAL_COLOR }}>Stack</span>
        </span>
      )}
    </div>
  );
}
