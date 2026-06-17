import type { ProtocolPortalData } from '@/lib/types';

/**
 * Mock data for the client-facing protocol portal (/my-protocol).
 *
 * This mirrors the static design template at frontend/templates/protocol_template.html.
 * It is the single source the PR B page renders from. Backend-compatible API
 * client methods exist for the later live-data integration PR.
 */

const MORNING_FOUNDATIONS =
  'L-Carnitine 3g, Phosphatidylcholine 3g, Vitamin E, Selenium, NAC, B-Complex, Zinc';

export const MOCK_PROTOCOL_PORTAL: ProtocolPortalData = {
  overview: {
    protocolName: 'Your Personalized Protocol',
    objective: 'Metabolic Optimization + Longevity + Tissue Repair',
    status: 'active',
    startedOnUtc: '2026-01-12T00:00:00.000Z',
    clientName: 'Alex Rivera',
    clientAvatarUrl: null,
    currentPhase: { number: 1, label: 'Phase 1', currentWeek: 3, totalWeeks: 12 },
    phases: [
      { number: 1, label: 'Phase 1', currentWeek: 3, totalWeeks: 12 },
      { number: 2, label: 'Phase 2', currentWeek: 1, totalWeeks: 8 },
    ],
  },

  stats: [
    {
      label: 'Retatrutide dose',
      value: '2',
      unit: 'mg / week',
      caption: 'Stable · Next increase pending labs',
      accent: 'emerald',
    },
    {
      label: 'Next labs due',
      value: 'Feb 18',
      caption: '5 days remaining',
      accent: 'amber',
    },
    {
      label: 'Adherence (last 7 days)',
      value: '94%',
      caption: 'Excellent',
      accent: 'emerald',
    },
    {
      label: 'Weight trend',
      value: '-6.4',
      unit: 'lbs',
      caption: 'Since start · 3.2 lbs this week',
      accent: 'emerald',
    },
  ],

  today: {
    dateIso: '2026-02-10',
    title: "Today's Schedule",
    subtitle: 'Monday, February 10, 2026 · Week 3, Phase 1',
    items: [
      {
        time: '08:00 AM',
        name: 'Retatrutide',
        detail: '2 mg subcutaneous',
        icon: 'syringe',
        accent: 'emerald',
        status: 'completed',
      },
      {
        time: '08:15 AM',
        name: 'MOTS-c',
        detail: '5 mg subcutaneous',
        icon: 'flask',
        accent: 'violet',
        status: 'upcoming',
      },
      {
        time: '08:00 AM & 08:00 PM',
        name: 'BPC-157',
        detail: '250 mcg',
        icon: 'syringe',
        accent: 'blue',
        status: 'upcoming',
      },
    ],
  },

  week: [
    { dateIso: '2026-02-09', dayLabel: '9', weekdayLabel: 'Sun', isToday: false, itemCount: 6 },
    { dateIso: '2026-02-10', dayLabel: '10', weekdayLabel: 'Mon', isToday: true, itemCount: 8 },
    { dateIso: '2026-02-11', dayLabel: '11', weekdayLabel: 'Tue', isToday: false, itemCount: 6 },
    { dateIso: '2026-02-12', dayLabel: '12', weekdayLabel: 'Wed', isToday: false, itemCount: 6 },
    { dateIso: '2026-02-13', dayLabel: '13', weekdayLabel: 'Thu', isToday: false, itemCount: 9, tag: 'MOTS-c' },
    { dateIso: '2026-02-14', dayLabel: '14', weekdayLabel: 'Fri', isToday: false, itemCount: 6 },
    { dateIso: '2026-02-15', dayLabel: '15', weekdayLabel: 'Sat', isToday: false, itemCount: 6 },
  ],

  daySchedules: {
    '2026-02-10': {
      dateIso: '2026-02-10',
      title: 'Monday, February 10',
      subtitle: 'Week 3 · Phase 1',
      items: [
        { time: '07:45 AM', name: 'Morning Foundations', detail: MORNING_FOUNDATIONS },
        { time: '08:00 AM', name: 'Retatrutide', detail: '2 mg subcutaneous' },
        { time: '08:15 AM', name: 'MOTS-c', detail: '5 mg subcutaneous' },
        { time: '08:30 AM', name: 'GHK-Cu', detail: '1 mg subcutaneous' },
        { time: '08:30 AM', name: 'BPC-157', detail: '250 mcg (AM dose)' },
        { time: 'With Breakfast & Lunch', name: 'TUDCA', detail: '500 mg with each large meal (total 1,000 mg)' },
        { time: '08:00 PM', name: 'BPC-157', detail: '250 mcg (PM dose)' },
        { time: '09:00 PM', name: 'Evening Stack', detail: 'Magnesium Glycinate 500 mg' },
      ],
    },
    '2026-02-13': {
      dateIso: '2026-02-13',
      title: 'Thursday, February 13',
      subtitle: 'Week 3 · Phase 1 · MOTS-c Day',
      items: [
        { time: '07:45 AM', name: 'Morning Foundations', detail: MORNING_FOUNDATIONS },
        { time: '08:00 AM', name: 'Retatrutide', detail: '2 mg subcutaneous' },
        { time: '08:15 AM', name: 'MOTS-c', detail: '5 mg subcutaneous' },
        { time: '08:30 AM', name: 'GHK-Cu', detail: '1 mg subcutaneous' },
        { time: '08:30 AM', name: 'BPC-157', detail: '250 mcg' },
        { time: '08:45 AM', name: 'TB-500', detail: '2.5 mg subcutaneous (maintenance)' },
        { time: 'With Breakfast & Lunch', name: 'TUDCA', detail: '500 mg with each large meal' },
        { time: '08:00 PM', name: 'BPC-157', detail: '250 mcg' },
        { time: '09:00 PM', name: 'Evening Stack', detail: 'Magnesium Glycinate 500 mg' },
      ],
    },
  },

  diet: {
    title: 'Diet & Lifestyle Framework',
    summary: 'High-protein, anti-inflammatory, nutrient-dense foundation',
    targets: [
      { label: 'Protein', value: '1.8–2.2 g/kg ideal body weight' },
      { label: 'Fiber', value: '35–45 g' },
      { label: 'Hydration', value: '3.5–4.5 L' },
      { label: 'Alcohol', value: 'Minimize / Eliminate', caution: true },
    ],
    rationale:
      'Preserves lean mass during Retatrutide-driven fat loss, supports liver detoxification, reduces inflammation (enhancing MOTS-c and NAD+ effectiveness), and provides stable energy.',
    lifestyle: [
      'Resistance training 3–4× per week',
      'Zone 2 cardio (supports natural MOTS-c production)',
      '7–9 hours quality sleep nightly',
      'Stress management & circadian alignment',
    ],
  },

  supplements: {
    title: 'Daily Supplementation Guide',
    summary: 'Foundational support — always active',
    entries: [
      { name: 'L-Carnitine', dose: '3 g daily', note: 'Fatty acid transport · Synergizes with MOTS-c' },
      { name: 'Phosphatidylcholine', dose: '3 g daily', note: 'Cell membrane integrity & liver fat export' },
      { name: 'Vitamin E', dose: '400 IU daily', note: 'Protects mitochondrial & liver membranes' },
      { name: 'Selenium', dose: '200 mcg daily', note: 'Glutathione peroxidase support' },
      {
        name: 'TUDCA',
        dose: '500 mg with 2 largest meals (1,000 mg total)',
        note: 'Most important for Retatrutide biliary protection',
        emphasis: true,
      },
      { name: 'NAC', dose: '1,000 mg daily', note: 'Glutathione replenishment & oxidative stress reduction' },
      { name: 'Methyl B-Complex + B12 + Folate', dose: 'daily', note: 'Liver methylation & detoxification' },
      { name: 'Magnesium Glycinate', dose: '500 mg (evening)', note: 'Sleep quality + enzymatic cofactor' },
      { name: 'Zinc', dose: '30 mg daily', note: 'Liver detoxification enzyme support' },
    ],
    additional: ['Omega-3 (2–4 g)', 'Creatine 5 g', 'CoQ10 200–300 mg'],
  },

  monitoring: {
    baselineCompleted:
      'Full CMP, lipid panel, HbA1c, HOMA-IR, GGT, ferritin, Vitamin D, liver ultrasound with elastography.',
    recurringCadence: 'Every 5–6 weeks',
    recurringLabs: [
      'Fasting insulin + glucose + HOMA-IR',
      'GGT, ALT, AST',
      'Triglycerides & TG/HDL ratio',
    ],
    adjustmentRules: [
      { trigger: 'GGT rising', action: 'Cut Retatrutide dose by 50%. Reassess in 5–6 weeks.' },
      {
        trigger: 'TG/HDL ratio worsening',
        action: 'Increase L-Carnitine to 4 g + protein. Cut Retatrutide by 50%.',
      },
    ],
  },

  milestones: [
    {
      order: 1,
      period: 'Weeks 1–4 (Current)',
      detail: 'Appetite regulation, early fat loss (3–8%), stable liver markers, improved recovery.',
      current: true,
    },
    {
      order: 2,
      period: 'Weeks 5–12',
      detail:
        'Significant fat loss, visible body recomposition, muscle preservation, enhanced insulin sensitivity.',
    },
    {
      order: 3,
      period: 'Month 4+',
      detail:
        'Optimized body composition, sustainable metabolic health, foundational longevity support established.',
    },
  ],

  resources: [
    {
      heading: 'About BRP (Emerging Compound)',
      body: 'BRP (BRINP2-related peptide) is a 12-amino-acid peptide discovered in 2025 that suppresses appetite via hypothalamic pathways distinct from GLP-1 agonists. Early data showed strong fat loss with minimal nausea or muscle loss. It is currently advancing toward human trials and may become a valuable future option in this protocol.',
    },
    {
      heading: 'Protocol Philosophy',
      body: 'This system is deliberately layered and governed. Each compound either protects the system (liver support) or amplifies the others (MOTS-c + Retatrutide synergy, TB-500 + BPC-157 for muscle preservation during fat loss, Epitalon for circadian/metabolic foundation).',
    },
  ],
};

/** Returns the PR B mock portal payload. */
export function getMockProtocolPortal(): ProtocolPortalData {
  return MOCK_PROTOCOL_PORTAL;
}
