// Pure helpers and view types extracted from ProtocolAnalyzerExperience.tsx.
// No React/JSX — safe to import in server components and tests.

import { ApiError } from '@/lib/api';
import type {
  ProtocolAnalyzerInputType,
  ProtocolAnalyzerResult,
} from '@/lib/types';

// ── Types ────────────────────────────────────────────────────────────────────

export type OptimizedProtocolView = {
  label: string;
  protocol: ProtocolAnalyzerResult['protocol'];
  score: number;
  removed: string[];
};

export type AnalyzerFinding = {
  label: string;
  message: string;
  tone: 'positive' | 'caution' | 'neutral';
};

// ── Constants ────────────────────────────────────────────────────────────────

export const exampleProtocols = {
  healing: `GLOW Blend (GHK-cu, BPC-157, TB-500)
BPC-157 500mcg daily
TB-500 2mg twice weekly
8 weeks on, 8 weeks off`,
  fatLoss: `Semaglutide weekly
Tirzepatide weekly
NAD+ 100mg 2x weekly
8 weeks on, 8 weeks off`,
  longevity: `NAD+ 250mg daily
MOTS-C 5mg 3x weekly
CoQ10 daily
8 weeks on, 8 weeks off`,
};

// ── Score helpers ─────────────────────────────────────────────────────────────

export function scoreSummary(score: number): string {
  if (score >= 80) return 'Few overlaps detected and clear attribution across compounds.';
  if (score >= 60) return 'Workable structure with some redundancy or attribution gaps.';
  return 'Multiple overlaps or weak attribution issues detected in the parsed stack.';
}

export function getScoreLabel(score: number | undefined): string {
  if (score === undefined) return 'Not scored yet';
  if (score >= 85) return 'Excellent fit';
  if (score >= 70) return 'Strong fit';
  if (score >= 55) return 'Mixed fit';
  if (score >= 40) return 'Inefficient';
  return 'High concern';
}

export function getScoreBand(score: number | undefined): string {
  if (score === undefined) return 'unknown';
  if (score >= 85) return 'excellent_fit';
  if (score >= 70) return 'strong_fit';
  if (score >= 55) return 'mixed_fit';
  if (score >= 40) return 'inefficient';
  return 'high_concern';
}

export function getScoreInsight(
  result: ProtocolAnalyzerResult | null,
  optimized: OptimizedProtocolView | null,
  hasSelectedGoal: boolean,
): string {
  if (!result) {
    return 'Run an analysis to score the protocol.';
  }

  const redundancy = result.issues.find((issue) => issue.type === 'redundancy');
  const overlap = result.issues.find((issue) => issue.type === 'overlap');
  const excessive = result.issues.find((issue) => issue.type === 'excessive_compounds');
  const removal = result.counterfactuals?.bestRemoveOne?.[0];

  if (overlap?.compounds.length) {
    return `Overlap involving ${overlap.compounds.slice(0, 2).join(' and ')} is reducing protocol efficiency.`;
  }

  if (redundancy?.compounds.length) {
    return `Useful coverage is present, but redundancy around ${redundancy.compounds.slice(0, 2).join(' and ')} is lowering the score.`;
  }

  if (removal && removal.deltaScore > 0) {
    return `Without ${removal.removedCompound}, the internal model score shifts by ${formatDelta(removal.deltaScore)} points.`;
  }

  if (excessive) {
    return 'This stack may be harder to evaluate cleanly because several compounds are layered together.';
  }

  if (optimized && optimized.score > result.score) {
    return `BioStack found an alternative arrangement with an internal model delta of ${formatDelta(optimized.score - result.score)} points.`;
  }

  if (!hasSelectedGoal && result.score >= 55) {
    return 'This stack shows a mixed fit based on detected relationships and known overlap patterns. Select a goal to personalize the fit assessment.';
  }

  if (result.protocol.length <= 3 && result.score >= 55) {
    return 'This protocol is compact and reasonably aligned with the selected goal.';
  }

  return scoreSummary(result.score);
}

export function getWhatThisMeans(result: ProtocolAnalyzerResult | null, optimized: OptimizedProtocolView | null): string {
  if (!result) {
    return '';
  }

  const issueCompounds = unique(result.issues.flatMap((issue) => issue.compounds).filter(Boolean));
  if (optimized && optimized.score > result.score && optimized.protocol.length < result.protocol.length) {
    return 'BioStack found an alternative arrangement with fewer overlapping signals on the internal model.';
  }

  if (issueCompounds.length > 1) {
    return `This stack may be harder to evaluate cleanly because ${issueCompounds.slice(0, 3).join(', ')} create overlapping signals.`;
  }

  if ((result.counterfactuals?.bestSwapOne?.length ?? 0) > 0) {
    return 'BioStack found an alternative arrangement worth comparing before committing this protocol to tracking.';
  }

  return 'This protocol appears relatively lean, but saving it lets you track whether the expected effects show up over time.';
}

export function recommendationCount(result: ProtocolAnalyzerResult): number {
  return (
    (result.counterfactuals?.bestRemoveOne?.length ?? 0) +
    (result.counterfactuals?.bestSwapOne?.length ?? 0) +
    (result.counterfactuals?.bestSimplifiedProtocol ? 1 : 0) +
    (result.counterfactuals?.goalAwareOptions?.length ?? 0)
  );
}

export function formatDelta(value: number): string {
  const rounded = Math.round(value);
  return rounded > 0 ? `+${rounded}` : `${rounded}`;
}

export function pickOptimizedProtocol(result: ProtocolAnalyzerResult | null): OptimizedProtocolView | null {
  if (!result) {
    return null;
  }

  // Only surface an "optimized" variant when it is strictly better than the
  // baseline. BioStack must not present a variant labeled as an improvement
  // when the score delta is zero or negative — that is what creates the
  // "Removed redundant compounds" misread on stacks the optimizer does not
  // actually understand well enough to improve.
  const counterfactuals = result.counterfactuals;
  const simplified = counterfactuals?.bestSimplifiedProtocol;
  if (simplified && simplified.score > result.score) {
    return {
      label: 'BioStack simplified arrangement',
      protocol: simplified.compounds,
      score: simplified.score,
      removed: simplified.removed,
    };
  }

  const goalAware = counterfactuals?.goalAwareOptions?.[0];
  if (goalAware && goalAware.score > result.score) {
    return {
      label: `BioStack arrangement for ${goalAware.goal}`,
      protocol: goalAware.compounds,
      score: goalAware.score,
      removed: result.protocol
        .filter((entry) => !goalAware.compounds.some((candidate) => candidate.compoundName.toLowerCase() === entry.compoundName.toLowerCase()))
        .map((entry) => entry.compoundName),
    };
  }

  return null;
}

export function currentRawInput(mode: ProtocolAnalyzerInputType, inputText: string, linkUrl: string, selectedFile: File | null): string {
  if (mode === 'Paste') {
    return inputText;
  }

  if (mode === 'Link') {
    return linkUrl;
  }

  return selectedFile ? `${selectedFile.name} (${selectedFile.type || 'unknown type'}, ${selectedFile.size} bytes)` : '';
}

export function formatAnalyzerError(error: unknown, mode: ProtocolAnalyzerInputType): string {
  const message = error instanceof Error ? error.message : 'Protocol analysis failed.';
  if (
    mode === 'CameraScan' &&
    (/ocr/i.test(message) || /image/i.test(message) || /read text/i.test(message) || /not configured/i.test(message))
  ) {
    return 'Scan is temporarily unavailable. Upload a PDF, spreadsheet, or paste text to analyze now.';
  }

  if (error instanceof ApiError) {
    if (error.status === 400 || error.status === 422) {
      return 'BioStack could not analyze that input yet. Check the protocol text and try again.';
    }

    if (error.status === 404) {
      return 'BioStack could not reach the intelligence route for this analysis. Your input is still safe. Try again in a moment.';
    }
  }

  if (/api error|failed to fetch|network|load failed|fetch/i.test(message)) {
    return 'BioStack could not reach the intelligence service. Your input is still safe. Try again in a moment.';
  }

  return message || 'Analysis is temporarily unavailable. Try again in a moment.';
}

export function sourceTypeLabel(result: ProtocolAnalyzerResult): string {
  if (result.inputType === 'Link') {
    return 'Link';
  }

  if (result.inputType === 'CameraScan') {
    return 'Image scan';
  }

  if (result.inputType === 'Paste') {
    return 'Pasted text';
  }

  const extension = result.sourceName?.split('.').pop()?.toUpperCase();
  return extension ? extension : 'File';
}

export function confidenceLabel(result: ProtocolAnalyzerResult): string {
  if (result.lowConfidenceExtraction) {
    return 'Low';
  }

  if (result.extractionWarnings.length > 0 || result.parserWarnings.length > 0) {
    return 'Medium';
  }

  return 'High';
}

export function formatDose(entry: ProtocolAnalyzerResult['protocol'][number]): string {
  if (entry.dose <= 0) {
    return '';
  }

  return `${entry.dose} ${entry.unit}`.trim();
}

export function unique(values: string[]): string[] {
  return Array.from(new Set(values.map((value) => value.trim()).filter(Boolean)));
}

export function goalText(optimized: OptimizedProtocolView): string {
  return optimized.label.replace(/^BioStack arrangement for /, '');
}

export function buildAnalyzerFindings(result: ProtocolAnalyzerResult | null): AnalyzerFinding[] {
  if (!result) return [];

  const findings: AnalyzerFinding[] = [];
  const explanation = result.scoreExplanation;

  if (explanation.synergy > 0) {
    findings.push({
      label: 'Positive signal',
      message: `Synergy +${Math.round(explanation.synergy)}`,
      tone: 'positive',
    });
  }

  if (explanation.redundancy < 0) {
    findings.push({
      label: 'Redundancy',
      message: `Redundancy ${Math.round(explanation.redundancy)}`,
      tone: 'caution',
    });
  }

  if (explanation.interference < 0) {
    findings.push({
      label: 'Interference',
      message: `Interference ${Math.round(explanation.interference)}`,
      tone: 'caution',
    });
  }

  for (const issue of result.issues) {
    findings.push({
      label: findingLabelForIssue(issue.type),
      message: issue.message,
      tone: issue.type === 'redundancy' || issue.type === 'overlap' ? 'caution' : 'neutral',
    });
  }

  if (result.unknownCompounds.length > 0) {
    findings.push({
      label: 'Unknown or partially parsed',
      message: `${result.unknownCompounds.length} compound${result.unknownCompounds.length === 1 ? '' : 's'} could not be fully normalized.`,
      tone: 'neutral',
    });
  }

  return findings;
}

export function findingLabelForIssue(type: string): string {
  switch (type) {
    case 'redundancy':
      return 'Redundancy';
    case 'overlap':
      return 'Caution';
    case 'inefficiency':
      return 'Caution';
    case 'excessive_compounds':
      return 'Caution';
    default:
      return 'Unknown or partially parsed';
  }
}

export function buildParserWarnings(result: ProtocolAnalyzerResult | null): string[] {
  if (!result) return [];
  const warnings: string[] = [];
  if (result.unknownCompounds.length > 0) {
    warnings.push(`${result.unknownCompounds.length} compound${result.unknownCompounds.length === 1 ? '' : 's'} could not be fully normalized.`);
  }
  if (result.decomposedBlends.length > 0) {
    warnings.push(`${result.decomposedBlends.length} blend${result.decomposedBlends.length === 1 ? ' was' : 's were'} expanded into individual compounds.`);
  }
  if (result.protocol.some((entry) => !entry.frequency || entry.dose === 0)) {
    warnings.push('One or more items were only partially extracted from the source text.');
  }
  return warnings;
}
