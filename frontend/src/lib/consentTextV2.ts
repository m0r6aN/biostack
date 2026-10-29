/**
 * The consent text for bio-observational-v2, word for word as approved by Clint on 2026-10-28.
 * Source of truth: docs/INITIATIVES/consent-v2/consent-v2-approved.md (section 2).
 * consentTextV2.test.ts fails if this file and that document drift apart, so change the text only
 * by getting a new approval and updating both.
 */

export const CONSENT_VERSION_V2 = 'bio-observational-v2';

export const CONSENT_TITLE_NEW = 'Before you save data';
export const CONSENT_TITLE_RETURNING = 'We updated how BioStack works with your data';

export const CONSENT_SHORT_VERSION: readonly string[] = [
  'BioStack keeps the profile, items, amounts, and logs you choose to save, so you can track what you take. That is health-related information, and you decide what goes in.',
  'BioStack never tells you what to take or how much. Amounts come only from you. It is not medical advice.',
  "We don't sell your data and we don't use it for ads. You can export everything or delete your account any time from the Account page.",
  'Some things below are not switched on yet. Before any of them start for you, we ask again with an on/off switch.',
];

export const CONSENT_BUTTON_AGREE = 'I agree and continue';
export const CONSENT_BUTTON_DECLINE = 'Not now';

/** A bullet may start with an emphasized lead ("Export:" or an italic feature name). */
export type ConsentBullet = { lead?: string; leadStyle?: 'strong' | 'em'; text: string };

export type ConsentDetailSection = {
  heading: string;
  bullets?: readonly ConsentBullet[];
  paragraph?: string;
};

export const CONSENT_DETAILS: readonly ConsentDetailSection[] = [
  {
    heading: '1. What you save',
    bullets: [
      { text: 'Your profile details, goals, and check-ins.' },
      { text: "Everything I'm Taking: the supplements, vitamins, research compounds, or anything else you add. This includes custom items you type yourself that BioStack can't verify. We store them as you wrote them." },
      { text: "Amounts, units, schedules, and the dose logs you enter (\"I took this, at this time\"). These are your own numbers. BioStack doesn't prescribe, suggest, or work out a dose for you, and nothing it shows should be read as one." },
      { text: 'Notes you write and the protocols you build.' },
    ],
  },
  {
    heading: '2. Who can see it',
    bullets: [
      { text: 'You. Nobody else can see your saved items or logs unless you choose to share them.' },
      { text: 'Provider summary: when you copy or print a summary for your clinician, that happens on your device, when you ask. It only leaves BioStack when you send it.' },
      { text: 'Services that run BioStack for you (hosting, email delivery, payments) handle only what they need to do their job. Payments see your email and billing details, not your items or logs.' },
      { text: "We don't sell your data. We don't share it for advertising." },
    ],
  },
  {
    heading: '3. Coming later (not on yet; we ask again first)',
    bullets: [
      { lead: 'Your own catalog and notes.', leadStyle: 'em', text: "Looking up the items you add against BioStack's library, and keeping personal notes on them." },
      { lead: 'Public-source lookups.', leadStyle: 'em', text: 'When you turn this on, the name you typed for an item may be sent to public databases (NIH/NLM DSLD, PubMed, ClinicalTrials.gov, openFDA, PubChem, RxNorm, and optionally Open Food Facts) to find information about it. Your name, email, and amounts are not sent. You can mark any item "keep private" and its name is never sent.' },
      { lead: 'Anonymous research requests.', leadStyle: 'em', text: "If an item has no information yet, you can ask us to look into it. The request is just the item name, with nothing linking it to you. We can't tell who asked." },
      { lead: 'Reminders.', leadStyle: 'em', text: 'Optional reminders by web push and by email. By default the text on your lock screen and in the email subject is generic ("BioStack reminder") with no item names. You can change this.' },
    ],
  },
  {
    heading: '4. Your controls',
    bullets: [
      { lead: 'Export:', leadStyle: 'strong', text: 'download everything you saved (JSON or CSV) from the Account page.' },
      { lead: 'Delete:', leadStyle: 'strong', text: 'delete your account and your saved data from the Account page. Deleted data is removed from live systems right away. Backups age out within 35 days. We keep a small record that a deletion happened that contains nothing that identifies you. Payment records may be kept by our payment provider where tax rules require it.' },
      { lead: 'Say no:', leadStyle: 'strong', text: 'choose "Not now". You can still read the public library. Your saved data is not deleted unless you delete it.' },
      { lead: 'Change your mind:', leadStyle: 'strong', text: 'you can turn off any later feature, and delete your account, any time.' },
    ],
  },
  {
    heading: '5. What BioStack is not',
    paragraph: "BioStack is an information tool. It isn't a doctor, pharmacy, or emergency service. If something feels wrong, contact a clinician or emergency services.",
  },
];

export const CONSENT_WHAT_CHANGED_HEADING = 'What changed';
export const CONSENT_WHAT_CHANGED =
  "Since you last agreed, we added: logging doses you took, the Everything I'm Taking list (including custom items), export and delete tools, and a plain statement of what is coming later. Nothing new is switched on for you by agreeing.";

/** The plain text of one bullet as a reader sees it. */
export function bulletText(bullet: ConsentBullet): string {
  return bullet.lead ? `${bullet.lead} ${bullet.text}` : bullet.text;
}
