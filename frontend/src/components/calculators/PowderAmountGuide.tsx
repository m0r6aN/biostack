const powderLabels = ['5 mg', '10 mg', '20 mg', '30 mg', '60 mg'];

export function PowderAmountGuide() {
  return (
    <div className="rounded-xl border border-emerald-300/20 bg-[#07110d] p-5 text-white shadow-2xl">
      <div className="grid gap-5 sm:grid-cols-[0.95fr_1.05fr] sm:items-center">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.2em] text-emerald-200/70">
            Powder amount
          </p>
          <p className="mt-2 text-2xl font-semibold tracking-tight text-white">
            Use the amount printed on the vial label.
          </p>
          <p className="mt-2 text-sm leading-6 text-white/62">
            The calculator uses the dry compound amount before mixing, not the liquid volume added later.
          </p>
        </div>

        <div
          role="img"
          aria-label="Powder amount vial guide showing example vial label values from 5 mg to 60 mg"
          className="mx-auto w-full max-w-[260px]"
        >
          <svg viewBox="0 0 240 300" className="h-auto w-full" aria-hidden="true">
            <defs>
              <linearGradient id="vial-glass" x1="0" x2="1" y1="0" y2="1">
                <stop offset="0%" stopColor="#D7F7FF" stopOpacity="0.62" />
                <stop offset="48%" stopColor="#86A8B7" stopOpacity="0.16" />
                <stop offset="100%" stopColor="#FFFFFF" stopOpacity="0.34" />
              </linearGradient>
              <linearGradient id="vial-label" x1="0" x2="1">
                <stop offset="0%" stopColor="#111A22" />
                <stop offset="100%" stopColor="#0B1117" />
              </linearGradient>
            </defs>

            <rect x="84" y="18" width="72" height="36" rx="10" fill="#A8B0B7" />
            <rect x="76" y="46" width="88" height="20" rx="6" fill="#D4D9DD" />
            <path
              d="M70 64h100c7 16 11 35 11 58v110c0 30-24 54-54 54h-14c-30 0-54-24-54-54V122c0-23 4-42 11-58Z"
              fill="url(#vial-glass)"
              stroke="#D6E3E8"
              strokeWidth="4"
            />
            <path d="M72 121h96c8 0 14 6 14 14v86H58v-86c0-8 6-14 14-14Z" fill="url(#vial-label)" />
            <path d="M78 74c-7 18-10 35-10 56v96c0 22 15 41 36 47" fill="none" stroke="#FFFFFF" strokeOpacity="0.42" strokeWidth="7" strokeLinecap="round" />
            <path d="M151 74c10 21 14 39 14 62v86" fill="none" stroke="#FFFFFF" strokeOpacity="0.18" strokeWidth="5" strokeLinecap="round" />

            <text x="120" y="151" textAnchor="middle" fontSize="15" fontWeight="800" fill="#4ADE80">
              BioStack
            </text>
            <text x="120" y="181" textAnchor="middle" fontSize="28" fontWeight="800" fill="#FFFFFF">
              Peptide
            </text>

            <g>
              {powderLabels.map((label, index) => (
                <text
                  key={label}
                  x="120"
                  y="224"
                  textAnchor="middle"
                  fontSize="30"
                  fontWeight="900"
                  fill="#FFFFFF"
                  className="powder-amount-label"
                  style={{ animationDelay: `${index * 1.5}s` }}
                >
                  {label}
                </text>
              ))}
            </g>
          </svg>

          <div className="sr-only">
            {powderLabels.map((label) => (
              <span key={label}>{label}</span>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
