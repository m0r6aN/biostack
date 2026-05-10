import type {
    EvidencePacket,
    PromotionManifestCandidate,
    ResearchReviewQueueItem,
    ResearchSummaryCompound,
    ReviewDecisionType,
    ReviewResolutionPlanItem,
} from '@/lib/research/types';

export const runtime = 'nodejs';

const DEFAULT_MODEL = 'gpt-5.5';

type SuggestionRequest = {
  compound: ResearchSummaryCompound;
  candidate: PromotionManifestCandidate;
  evidencePacket: EvidencePacket | null;
  planItems: ReviewResolutionPlanItem[];
  reviewQueueItems?: ResearchReviewQueueItem[];
};

type ReviewSuggestion = {
  decision: ReviewDecisionType;
  confidence: 'low' | 'moderate' | 'high';
  summary: string;
  rationale: string[];
  claimIdsToApprove: string[];
  reviewQueueItemIdsToResolve: string[];
  clearsSoftPromotionBlockers: boolean;
  draftNotes: string;
  safetyWarnings: string[];
  openQuestions: string[];
};

const REVIEW_SUGGESTION_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  required: [
    'decision',
    'confidence',
    'summary',
    'rationale',
    'claimIdsToApprove',
    'reviewQueueItemIdsToResolve',
    'clearsSoftPromotionBlockers',
    'draftNotes',
    'safetyWarnings',
    'openQuestions',
  ],
  properties: {
    decision: { type: 'string', enum: ['approve-for-promotion', 'approve-claims', 'resolve-review-items', 'archive-draft', 'request-changes', 'reject'] },
    confidence: { type: 'string', enum: ['low', 'moderate', 'high'] },
    summary: { type: 'string' },
    rationale: { type: 'array', items: { type: 'string' }, minItems: 1, maxItems: 6 },
    claimIdsToApprove: { type: 'array', items: { type: 'string' }, maxItems: 20 },
    reviewQueueItemIdsToResolve: { type: 'array', items: { type: 'string' }, maxItems: 20 },
    clearsSoftPromotionBlockers: { type: 'boolean' },
    draftNotes: { type: 'string' },
    safetyWarnings: { type: 'array', items: { type: 'string' }, maxItems: 6 },
    openQuestions: { type: 'array', items: { type: 'string' }, maxItems: 6 },
  },
};

const SYSTEM_PROMPT = `You are a cautious biomedical research review assistant for BioStack.
You help a human reviewer reason about whether a draft compound should be promoted, changed, rejected, archived, have queue items resolved, or have narrow source-scoped claims approved.
You are advisory only; the human reviewer owns the final decision.

Rules:
- Do not provide personalized medical advice, dosing instructions, or product recommendations.
- Distinguish evidence support from extraction confidence.
- Favor request-changes when evidence packets are partial, claims require human/regulatory review, or authoritative safety context is missing.
- Never recommend approve-for-promotion when hard blockers remain.
- Claim approval must be narrow and source-scoped; only return claim IDs present in the provided packet.
- Queue item resolution must be explicit; only return review queue item IDs present in the provided packet context.
- Clearing soft blockers is appropriate only for approve-for-promotion and only when the draft is genuinely promotion-ready.
- Peptides, hormones, GLP-1s, experimental drugs, gray-market compounds, and compounded products require extra caution.`;

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isSuggestionRequest(value: unknown): value is SuggestionRequest {
  if (!isRecord(value)) return false;
  return isRecord(value.compound) && isRecord(value.candidate) && ('evidencePacket' in value) && Array.isArray(value.planItems);
}

function compactContext(body: SuggestionRequest) {
  const sources = body.evidencePacket?.sources.map(source => ({
    sourceId: source.sourceId,
    authorityTier: source.authorityTier,
    sourceType: source.sourceType,
    title: source.title,
    publisher: source.publisher,
    doi: source.doi,
    pmid: source.pmid,
  })) ?? [];

  return {
    compound: body.compound,
    candidate: body.candidate,
    remediationPlan: body.planItems.map(item => ({
      itemId: item.itemId,
      severity: item.severity,
      resolutionType: item.resolutionType,
      issue: item.issue,
      recommendedAction: item.recommendedAction,
      relatedBlockers: item.relatedBlockers,
      relatedQualityFlags: item.relatedQualityFlags,
      relatedReviewQueueItemIds: item.relatedReviewQueueItemIds,
    })),
    reviewQueueItems: (body.reviewQueueItems ?? []).map(item => ({
      itemId: item.itemId,
      severity: item.severity,
      reason: item.reason,
      references: item.references,
    })),
    evidencePacket: body.evidencePacket ? {
      completeness: body.evidencePacket.ops.completeness,
      needsReview: body.evidencePacket.ops.needsReview,
      reviewReasons: body.evidencePacket.ops.reviewReasons,
      qualityFlags: body.evidencePacket.ops.qualityFlags,
      sources,
      claims: body.evidencePacket.claims.map(claim => ({
        claimId: claim.claimId,
        claimType: claim.claimType,
        statement: claim.statement,
        evidenceTier: claim.evidenceTier,
        confidence: claim.confidence,
        fieldAuthorityRequired: claim.fieldAuthorityRequired,
        sourceRefs: claim.sourceRefs,
        reviewFlags: claim.reviewFlags,
        context: claim.context,
        extractedEvidence: claim.extractedEvidence.slice(0, 3),
      })),
      conflicts: body.evidencePacket.conflicts,
    } : null,
  };
}

function extractResponseText(payload: unknown): string | null {
  if (!isRecord(payload)) return null;
  if (typeof payload.output_text === 'string') return payload.output_text;

  const output = payload.output;
  if (!Array.isArray(output)) return null;
  const pieces: string[] = [];
  for (const item of output) {
    if (!isRecord(item) || !Array.isArray(item.content)) continue;
    for (const content of item.content) {
      if (isRecord(content) && typeof content.text === 'string') pieces.push(content.text);
    }
  }
  return pieces.length > 0 ? pieces.join('\n') : null;
}

function parseSuggestion(text: string): ReviewSuggestion | null {
  try {
    const parsed = JSON.parse(text.trim()) as unknown;
    if (!isRecord(parsed)) return null;
    if (!['approve-for-promotion', 'approve-claims', 'resolve-review-items', 'archive-draft', 'request-changes', 'reject'].includes(String(parsed.decision))) return null;
    return {
      decision: parsed.decision as ReviewDecisionType,
      confidence: ['low', 'moderate', 'high'].includes(String(parsed.confidence)) ? parsed.confidence as ReviewSuggestion['confidence'] : 'low',
      summary: typeof parsed.summary === 'string' ? parsed.summary : '',
      rationale: Array.isArray(parsed.rationale) ? parsed.rationale.filter(item => typeof item === 'string') : [],
      claimIdsToApprove: Array.isArray(parsed.claimIdsToApprove) ? parsed.claimIdsToApprove.filter(item => typeof item === 'string') : [],
      reviewQueueItemIdsToResolve: Array.isArray(parsed.reviewQueueItemIdsToResolve) ? parsed.reviewQueueItemIdsToResolve.filter(item => typeof item === 'string') : [],
      clearsSoftPromotionBlockers: parsed.clearsSoftPromotionBlockers === true,
      draftNotes: typeof parsed.draftNotes === 'string' ? parsed.draftNotes : '',
      safetyWarnings: Array.isArray(parsed.safetyWarnings) ? parsed.safetyWarnings.filter(item => typeof item === 'string') : [],
      openQuestions: Array.isArray(parsed.openQuestions) ? parsed.openQuestions.filter(item => typeof item === 'string') : [],
    };
  } catch {
    return null;
  }
}

function normalizeSuggestion(body: SuggestionRequest, suggestion: ReviewSuggestion): ReviewSuggestion {
  const hardBlockers = body.candidate.blockers.filter(blocker => blocker.startsWith('blocked:'));
  const allowedClaimIds = new Set(body.evidencePacket?.claims.map(claim => claim.claimId) ?? []);
  const allowedQueueItemIds = new Set((body.reviewQueueItems ?? []).map(item => item.itemId));
  const claimIdsToApprove = suggestion.claimIdsToApprove.filter(claimId => allowedClaimIds.has(claimId));
  const reviewQueueItemIdsToResolve = suggestion.reviewQueueItemIdsToResolve.filter(itemId => allowedQueueItemIds.has(itemId));
  let decision = suggestion.decision;
  let clearsSoftPromotionBlockers = suggestion.clearsSoftPromotionBlockers;
  const rationale = [...suggestion.rationale];

  if (hardBlockers.length > 0 && decision === 'approve-for-promotion') {
    decision = 'request-changes';
    clearsSoftPromotionBlockers = false;
    rationale.unshift('AI recommendation adjusted: hard blockers cannot be cleared from the review form.');
  }

  if (decision !== 'approve-for-promotion') clearsSoftPromotionBlockers = false;
  if (decision === 'resolve-review-items' && reviewQueueItemIdsToResolve.length === 0) {
    decision = 'request-changes';
    rationale.unshift('AI recommendation adjusted: no valid active review queue item IDs were returned to resolve.');
  }

  return { ...suggestion, decision, claimIdsToApprove, reviewQueueItemIdsToResolve, clearsSoftPromotionBlockers, rationale };
}

export async function POST(request: Request) {
  if (process.env.NODE_ENV === 'production' && process.env.RESEARCH_AI_SUGGEST_ENABLED !== 'true') {
    return Response.json({ error: 'Not Found' }, { status: 404 });
  }

  const apiKey = process.env.OPENAI_API_KEY;
  if (!apiKey) {
    return Response.json({ error: 'OPENAI_API_KEY is not configured.' }, { status: 503 });
  }

  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return Response.json({ error: 'Invalid JSON body.' }, { status: 400 });
  }

  if (!isSuggestionRequest(body)) {
    return Response.json({ error: 'Invalid suggestion request.' }, { status: 400 });
  }

  const model = process.env.OPENAI_REVIEW_MODEL ?? DEFAULT_MODEL;
  const openAiResponse = await fetch('https://api.openai.com/v1/responses', {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${apiKey}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      model,
      input: [
        { role: 'system', content: SYSTEM_PROMPT },
        { role: 'user', content: JSON.stringify(compactContext(body)) },
      ],
      text: {
        format: {
          type: 'json_schema',
          name: 'review_decision_suggestion',
          schema: REVIEW_SUGGESTION_SCHEMA,
          strict: true,
        },
      },
    }),
  });

  if (!openAiResponse.ok) {
    const message = await openAiResponse.text().catch(() => '');
    return Response.json({ error: 'OpenAI suggestion request failed.', detail: message.slice(0, 500) }, { status: 502 });
  }

  const text = extractResponseText(await openAiResponse.json());
  const suggestion = text ? parseSuggestion(text) : null;
  if (!suggestion) {
    return Response.json({ error: 'OpenAI returned an unreadable suggestion.' }, { status: 502 });
  }

  return Response.json({ suggestion: normalizeSuggestion(body, suggestion), modelUsed: model });
}