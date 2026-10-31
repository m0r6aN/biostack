import { METHODOLOGY_TIERS } from '@/lib/evidenceTiers';

/**
 * Content for /support. Every answer restates behavior that exists in the product today and links to the place
 * where it happens. Wording on consent, export and deletion follows docs/INITIATIVES/consent-v2/consent-v2-approved.md
 * and the Account data page. Do not add dosing guidance, effects claims, or promises about response time here.
 * The email address is deliberately absent: the contact form is the support channel (see /contact).
 */

export type SupportLink = { href: string; label: string };

export type SupportEntry = {
  id: string;
  question: string;
  /** Plain-text paragraphs. Shown to readers and used verbatim in the FAQPage structured data. */
  answer: readonly string[];
  links?: readonly SupportLink[];
};

export type SupportGroup = {
  id: string;
  heading: string;
  entries: readonly SupportEntry[];
};

const tierNames = METHODOLOGY_TIERS.map((tier) => tier.label).join(', ');

export const SUPPORT_GROUPS: readonly SupportGroup[] = [
  {
    id: 'signing-in',
    heading: 'Signing in',
    entries: [
      {
        id: 'magic-link',
        question: 'How does signing in work?',
        answer: [
          'BioStack signs you in with an email link, so there is no password to remember. Enter your email on the sign-in page and we send you a one-time link. Open it and you are in.',
          'The link works once and expires after 15 minutes. If you see “expired or already used”, ask for a new one.',
          'Once you are signed in you can add a passkey from Account Security for one-tap sign-in on that device.',
        ],
        links: [{ href: '/auth/signin', label: 'Go to sign-in' }],
      },
      {
        id: 'no-email',
        question: 'The sign-in email did not arrive. What should I do?',
        answer: [
          'Check your spam or promotions folder and make sure you typed the address correctly. Links expire after 15 minutes, so ask for a new one if yours is old.',
          'If it still does not arrive, send us a message through the contact form, choose Support, and tell us which address you used. Do not send us the link itself.',
        ],
        links: [{ href: '/contact', label: 'Contact us' }],
      },
    ],
  },
  {
    id: 'consent-and-data',
    heading: 'Consent and your data',
    entries: [
      {
        id: 'consent',
        question: 'What am I agreeing to when I save data?',
        answer: [
          'Before you save anything, BioStack asks you to agree to a short statement. It covers the profile details, items, amounts, and logs you choose to save, which is health-related information, and you decide what goes in. We do not sell your data and we do not use it for ads.',
          'If you choose “Not now”, you can still read the public library. Nothing you saved is deleted unless you delete it. Anything that is not switched on yet is asked about again, with an on/off switch, before it starts for you.',
        ],
        links: [
          { href: '/onboarding/consent', label: 'Read the full consent text' },
          { href: '/privacy', label: 'Privacy' },
        ],
      },
      {
        id: 'export',
        question: 'How do I download my data?',
        answer: [
          'Open Your Data from the menu and choose Download JSON or Download CSV. JSON keeps the structure. CSV gives you one spreadsheet per list, in a zip file. Passwords, sign-in link secrets, and passkey keys are never included.',
        ],
        links: [{ href: '/account/data', label: 'Open Your Data' }],
      },
      {
        id: 'deletion',
        question: 'How do I delete my account?',
        answer: [
          'Open Your Data from the menu and find Delete your account. Type the phrase shown on the page, and we email you a one-time link. Open that link in the same browser within 15 minutes and confirm. Nothing is deleted until you confirm on that last page.',
          'Deleting is permanent. Your profiles, items, dose logs, check-ins, protocols, notes, alert settings, sign-in details, and sessions are removed from live systems. Backups age out within 35 days. A paid plan is cancelled, and our payment provider may keep its own billing records where tax rules require it. We keep a small record that a deletion happened, with a random id, the date, and a row count, and nothing in it identifies you.',
          'Download a copy first if you want one. We cannot bring deleted data back.',
        ],
        links: [{ href: '/account/data', label: 'Open Your Data' }],
      },
    ],
  },
  {
    id: 'what-biostack-does',
    heading: 'What BioStack does and does not do',
    entries: [
      {
        id: 'what-it-is',
        question: 'What is BioStack?',
        answer: [
          'BioStack is a public library of what the research says about peptides and similar compounds, graded by evidence strength. If you create an account you can also keep your own record of what you take, with logs, check-ins, and a timeline, and use tools such as the unit converter and the volume and reconstitution calculators.',
        ],
        links: [
          { href: '/how-it-works', label: 'How it works' },
          { href: '/knowledge', label: 'Browse the library' },
        ],
      },
      {
        id: 'no-dose-advice',
        question: 'Will BioStack tell me what to take or how much?',
        answer: [
          'No. BioStack never tells you what to take or how much, and it does not fill in, suggest, or work out an amount for you. Amounts come only from you. The calculators do arithmetic on numbers you enter, such as a concentration, and do not choose an amount.',
          'BioStack is not medical advice. It is not a doctor, a pharmacy, or an emergency service. For medical decisions, talk to a qualified clinician. If something feels wrong, contact a clinician or your local emergency services.',
        ],
        links: [{ href: '/safety', label: 'Safety boundary' }],
      },
      {
        id: 'custom-items',
        question: 'What happens when I add something BioStack does not recognize?',
        answer: [
          'In Everything I’m Taking you can add supplements, vitamins, research compounds, or anything else. BioStack checks what you typed against its library and may offer a match for you to confirm. If it cannot verify an item, it stays in your list exactly as you wrote it, marked Unverified. If BioStack offers a match, you choose whether to accept it.',
        ],
      },
    ],
  },
  {
    id: 'evidence',
    heading: 'Evidence and data errors',
    entries: [
      {
        id: 'evidence-tiers',
        question: 'What are evidence tiers?',
        answer: [
          `Each claim in the library is graded on seven tiers: ${tierNames}. A tier describes how strong the sources are for that specific claim. It does not say whether something is suitable for you.`,
        ],
        links: [{ href: '/knowledge/methodology', label: 'How we grade evidence' }],
      },
      {
        id: 'report-error',
        question: 'I think something in the library is wrong. How do I report it?',
        answer: [
          'Send us a message through the contact form and choose Feedback. Tell us the compound, the exact claim or number, and what you think is wrong. A source makes a report much easier to check, so include a PubMed ID, a DOI, or a link if you have one.',
        ],
        links: [{ href: '/contact', label: 'Report a data error' }],
      },
    ],
  },
  {
    id: 'billing',
    heading: 'Plans and billing',
    entries: [
      {
        id: 'billing-help',
        question: 'How do I change or cancel my plan?',
        answer: [
          'Signed-in members can open Billing and choose Manage Billing to manage your subscription. Payments are handled by Stripe. If Manage Billing does not open for your account, contact us, choose Billing, and tell us the email you sign in with.',
        ],
        links: [
          { href: '/pricing', label: 'Plans and pricing' },
          { href: '/billing', label: 'Billing' },
        ],
      },
    ],
  },
];

/** Support topics in the page order, flattened for structured data. */
export const SUPPORT_ENTRIES: readonly SupportEntry[] = SUPPORT_GROUPS.flatMap((group) => group.entries);
