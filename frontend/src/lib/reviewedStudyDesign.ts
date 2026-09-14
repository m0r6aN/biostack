import explanations from './generated/reviewed-study-design.json';

export interface ReviewedStudyDesign {
  canonicalName: string;
  kind: 'dated-review-study-design';
  claimId: string;
  statement: string;
  citation: { displayLabel: string; sourceItemId: string; pmid: string; doi: string; url: string };
  searchCutoff: string;
  languageScope: string;
  limitations: string;
  acceptanceDecisionId: string;
  expiresAt: string | null;
}

export function getReviewedStudyDesign(name: string): ReviewedStudyDesign | undefined {
  // Only the separately checked public projection is imported into the client.
  // Expiry also hides an old deployed asset until the next deployment.
  return (explanations as ReviewedStudyDesign[]).find(item =>
    item.canonicalName.trim().toLowerCase() === name.trim().toLowerCase()
    && (item.expiresAt === null || Date.parse(item.expiresAt) > Date.now()));
}
