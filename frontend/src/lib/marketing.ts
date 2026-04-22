export interface MarketingFaq {
  question: string;
  answer: string;
}

export interface PricingTier {
  name: string;
  monthly: string;
  annual: string;
  annualEffective: string;
  description: string;
  detail: string;
  ctaLabel: string;
  href: string;
  featured?: boolean;
  highlights: string[];
}

export const featuredFaqs: MarketingFaq[] = [
  {
    question: 'Is BioStack medical advice?',
    answer:
      'No. BioStack is an organizational and observational tool. It helps you track what you take, calculate doses accurately, and surface evidence from scientific literature. BioStack does not recommend compounds, suggest dosages, or provide clinical guidance.',
  },
  {
    question: 'What does BioStack actually do?',
    answer:
      'BioStack helps you paste a stack, surface overlap and conflict findings, calculate injection math, track daily check-ins across 15+ biomarkers, and compare how your stack changes over time.',
  },
  {
    question: 'Who is BioStack designed for?',
    answer:
      'BioStack is built for serious self-experimenters who manage complex stacks and need more than a spreadsheet. It is for users who already do the research and want a system that keeps up with the complexity.',
  },
  {
    question: 'What is the Reconstitution Calculator?',
    answer:
      'The Reconstitution Calculator handles the math for dissolving lyophilized powders into injectable solutions. Enter the compound amount in milligrams and your diluent volume in milliliters and BioStack outputs the concentration clearly and transparently.',
  },
  {
    question: 'What are evidence tiers?',
    answer:
      'Evidence tiers classify how well-supported a compound’s effects are in scientific literature. BioStack uses Strong, Moderate, Limited, and Mechanistic so users can read knowledge entries with the right expectations.',
  },
  {
    question: 'Is there a free plan?',
    answer:
      'Yes. Observer is free forever and includes one stack score, one headline finding, calculators, and basic knowledge access. Upgrade to Operator for scenarios, reasoning, and tracked stack history.',
  },
  {
    question: 'What is Pathway Overlap detection?',
    answer:
      'Pathway Overlap detection analyzes your stack and identifies when two or more compounds share the same biological pathways. BioStack surfaces findings and confidence levels so you can investigate further.',
  },
  {
    question: 'Where is my data stored?',
    answer:
      'Your BioStack data is stored securely and is private to your account. BioStack does not sell personal data or share stack information with third parties.',
  },
  {
    question: 'How is BioStack different from a spreadsheet?',
    answer:
      'Spreadsheets do not calculate reconstitution math, detect pathway overlaps, or align check-ins and compound events in a unified timeline. BioStack is built for stack intelligence rather than passive storage.',
  },
  {
    question: 'What daily metrics can I track?',
    answer:
      'BioStack supports check-ins across weight, sleep, energy, appetite, recovery, focus, thought clarity, skin quality, digestion, strength, endurance, joint pain, eyesight, mood, and side effects.',
  },
];

export const pricingTiers: PricingTier[] = [
  {
    name: 'Observer',
    monthly: '$0',
    annual: '$0/year',
    annualEffective: '$0/mo',
    description: 'Getting organized.',
    detail:
      'A simple place to track what you’re taking and stop relying on notes, memory, or scattered apps.',
    ctaLabel: 'Analyze My Stack',
    href: '/onboarding',
    highlights: [
      'Track one saved stack',
      'Basic knowledge library access',
      'Core check-ins',
      'Free calculators',
    ],
  },
  {
    name: 'Operator',
    monthly: '$12/mo',
    annual: '$96/year',
    annualEffective: '$8/mo effective',
    description: 'For people actively managing stacks, not just logging them.',
    detail:
      'Everything you need to track compounds, log results, and understand how your stack fits together.',
    ctaLabel: 'Choose Operator',
    href: '/auth/signin',
    featured: true,
    highlights: [
      'Unlimited compounds',
      'Full timeline history',
      'Reconstitution and volume calculators',
      'All check-in fields',
      'Tracked stack history and overlap insights',
    ],
  },
  {
    name: 'Commander',
    monthly: '$29/mo',
    annual: '$228/year',
    annualEffective: '$19/mo effective',
    description: 'For deeper analysis and pattern optimization.',
    detail:
      'Advanced insight tools for people managing more complex stacks over time.',
    ctaLabel: 'See Commander',
    href: '/auth/signin',
    highlights: [
      'Deep stack analysis',
      'Trend and pattern detection',
      'Cross-session comparison',
      'Side-effect pattern surfacing',
      'Priority support',
    ],
  },
];

export const landingFeatures = [
  'Compound tracking with precision structure',
  'Pathway overlap intelligence across your active stack',
  'Reconstitution, volume, and unit conversion math',
  'Evidence-tiered knowledge base entries',
  'Daily check-ins that turn subjectivity into analyzable signal',
  'Unified timeline correlation across compounds, phases, and check-ins',
];
