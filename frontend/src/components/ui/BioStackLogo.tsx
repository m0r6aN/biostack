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

const PLATE_1 = 'M 80,18 L 148,50 L 80,70 L 12,50 Z';
const PLATE_2 = 'M 80,58 L 145,88 L 80,108 L 15,88 Z';
const PLATE_3 = 'M 80,96 L 140,122 L 80,142 L 20,122 Z';
const SIGNAL = 'M 24,64 L 40,64 L 50,51 L 58,77 L 70,24 L 85,64 L 97,52 L 110,64 L 136,64';
const PATHWAY_ARC = 'M 58,112 C 72,92 92,86 105,66 C 115,50 112,36 100,26';
const TOP_HIGHLIGHT = 'M 20,50 L 80,22 L 140,50';

const WORDMARK_COLOR: Record<BioStackTheme, string> = {
  dark:  '#F8FAFC',
  light: '#0F172A',
};

const SIGNAL_COLOR = '#22C55E';
const SIGNAL_GRADIENT_ID = 'biostackSignal';
const NODE_GRADIENT_ID = 'biostackNode';
const PLATE_TOP_GRADIENT_ID = 'biostackPlateTop';
const PLATE_MID_GRADIENT_ID = 'biostackPlateMid';
const SOFT_GLOW_ID = 'biostackSoftGlow';
const PLATE_SHADOW_ID = 'biostackPlateShadow';

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
        <defs>
          <linearGradient id={PLATE_TOP_GRADIENT_ID} x1="12" y1="18" x2="148" y2="70" gradientUnits="userSpaceOnUse">
            <stop offset="0" stopColor={theme === 'dark' ? '#31465F' : '#D6E1EA'} />
            <stop offset="0.52" stopColor={theme === 'dark' ? '#273A51' : '#ADC0CF'} />
            <stop offset="1" stopColor={theme === 'dark' ? '#1A2838' : '#7F96AA'} />
          </linearGradient>

          <linearGradient id={PLATE_MID_GRADIENT_ID} x1="15" y1="58" x2="145" y2="108" gradientUnits="userSpaceOnUse">
            <stop offset="0" stopColor={theme === 'dark' ? '#26384E' : '#B9CBD8'} />
            <stop offset="0.55" stopColor={theme === 'dark' ? '#1E2D3E' : '#91A8BA'} />
            <stop offset="1" stopColor={theme === 'dark' ? '#172333' : '#6F8498'} />
          </linearGradient>

          <linearGradient id={SIGNAL_GRADIENT_ID} x1="24" y1="24" x2="136" y2="77" gradientUnits="userSpaceOnUse">
            <stop offset="0" stopColor="#22D3EE" />
            <stop offset="0.48" stopColor="#2DD4BF" />
            <stop offset="1" stopColor="#22C55E" />
          </linearGradient>

          <linearGradient id={NODE_GRADIENT_ID} x1="54" y1="34" x2="108" y2="120" gradientUnits="userSpaceOnUse">
            <stop offset="0" stopColor="#67E8F9" />
            <stop offset="0.55" stopColor="#2DD4BF" />
            <stop offset="1" stopColor="#60A5FA" />
          </linearGradient>

          <filter id={SOFT_GLOW_ID} x="-40%" y="-40%" width="180%" height="180%">
            <feGaussianBlur stdDeviation="3" result="blur" />
            <feColorMatrix
              in="blur"
              type="matrix"
              values="
                0 0 0 0 0.13
                0 0 0 0 0.83
                0 0 0 0 0.93
                0 0 0 0.38 0"
              result="glow"
            />
            <feMerge>
              <feMergeNode in="glow" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>

          <filter id={PLATE_SHADOW_ID} x="-20%" y="-20%" width="140%" height="140%">
            <feDropShadow dx="0" dy="8" stdDeviation="8" floodColor="#020617" floodOpacity="0.38" />
          </filter>
        </defs>

        <path
          d={PLATE_3}
          fill="none"
          stroke={theme === 'dark' ? '#2A3D54' : '#8094A8'}
          strokeWidth="2"
          opacity="0.58"
          className="biostack-plate-3"
        />

        <path
          d={PLATE_2}
          fill={`url(#${PLATE_MID_GRADIENT_ID})`}
          stroke={theme === 'dark' ? '#38516D' : '#8094A8'}
          strokeWidth="1.5"
          opacity="0.88"
          filter={`url(#${PLATE_SHADOW_ID})`}
          className="biostack-plate-2"
        />

        <path
          d={PLATE_1}
          fill={`url(#${PLATE_TOP_GRADIENT_ID})`}
          stroke={theme === 'dark' ? '#4A6888' : '#9DB3C6'}
          strokeWidth="1.5"
          opacity="0.98"
          filter={`url(#${PLATE_SHADOW_ID})`}
          className="biostack-plate-1"
        />

        <path
          d={TOP_HIGHLIGHT}
          fill="none"
          stroke="#67E8F9"
          strokeWidth="1.25"
          strokeLinecap="round"
          opacity="0.38"
          className="biostack-top-highlight"
        />

        <path
          d={SIGNAL}
          fill="none"
          stroke={`url(#${SIGNAL_GRADIENT_ID})`}
          strokeWidth="4.5"
          strokeLinecap="round"
          strokeLinejoin="round"
          filter={`url(#${SOFT_GLOW_ID})`}
          className={cn(
            'biostack-signal-path',
            isPulsing && 'biostack-signal',
            glow && !isPulsing && !isLoading && 'biostack-glow-static'
          )}
        />

        <path
          d={PATHWAY_ARC}
          fill="none"
          stroke="#22D3EE"
          strokeWidth="2"
          strokeLinecap="round"
          strokeDasharray="2 7"
          opacity="0.52"
          className={cn('biostack-pathway-arc', isPulsing && 'biostack-pathway-pulse')}
        />

        <circle cx="60" cy="110" r="4.5" fill={`url(#${NODE_GRADIENT_ID})`} opacity="0.95" className="biostack-node biostack-node-1" />
        <circle cx="78" cy="96" r="3.8" fill={`url(#${NODE_GRADIENT_ID})`} opacity="0.92" className="biostack-node biostack-node-2" />
        <circle cx="96" cy="76" r="3.5" fill={`url(#${NODE_GRADIENT_ID})`} opacity="0.88" className="biostack-node biostack-node-3" />
        <circle cx="108" cy="54" r="3.2" fill={`url(#${NODE_GRADIENT_ID})`} opacity="0.82" className="biostack-node biostack-node-4" />

        <circle
          cx="70"
          cy="24"
          r="5.5"
          fill="#67E8F9"
          opacity="0.98"
          filter={`url(#${SOFT_GLOW_ID})`}
          className={cn('biostack-anchor-node', isPulsing && 'biostack-anchor-pulse')}
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
