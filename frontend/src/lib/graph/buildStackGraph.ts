import { INTERACTION_TOKENS } from '@/styles/tokens';
import type { InteractionIntelligence, CompoundRecord } from '@/lib/types';

export interface StackGraphNode {
  id: string;
  data: {
    label: string;
    compound: CompoundRecord | null;
    evidenceTier?: string;
    regulatoryBoundary?: string;
    findingCount: number;
    status: string;
  };
  position: { x: number; y: number };
  type: 'compoundNode';
}

export interface StackGraphEdge {
  id: string;
  source: string;
  target: string;
  data: {
    interactionType: string;
    confidence: number;
    sharedPathways: string[];
    reason: string;
    hintBacked: boolean;
    label: string;
  };
  animated: boolean;
  style: { stroke: string; strokeWidth: number; strokeDasharray?: string };
  type: 'interactionEdge';
}

export interface StackGraphData {
  nodes: StackGraphNode[];
  edges: StackGraphEdge[];
}

// Layout: arrange compounds in a circle or grid
function circleLayout(count: number, radius = 200): Array<{ x: number; y: number }> {
  if (count === 0) return [];
  if (count === 1) return [{ x: 300, y: 300 }];
  return Array.from({ length: count }, (_, i) => {
    const angle = (i / count) * 2 * Math.PI - Math.PI / 2;
    return {
      x: 300 + radius * Math.cos(angle),
      y: 300 + radius * Math.sin(angle),
    };
  });
}

function slugify(name: string) {
  return name.toLowerCase().replace(/[^a-z0-9]/g, '-');
}

export function buildStackGraph(
  intelligence: InteractionIntelligence | null,
  compounds: CompoundRecord[],
): StackGraphData {
  if (!intelligence) return { nodes: [], edges: [] };

  const activeCompounds = compounds.filter((c) => c.status === 'Active');
  const positions = circleLayout(activeCompounds.length);

  // Count findings per compound
  const findingCounts: Record<string, number> = {};
  for (const finding of intelligence.topFindings ?? []) {
    for (const name of finding.compounds ?? []) {
      findingCounts[name] = (findingCounts[name] ?? 0) + 1;
    }
  }

  const nodes: StackGraphNode[] = activeCompounds.map((c, i) => ({
    id: slugify(c.name),
    data: {
      label: c.name,
      compound: c,
      evidenceTier: undefined,
      regulatoryBoundary: undefined,
      findingCount: findingCounts[c.name] ?? 0,
      status: c.status,
    },
    position: positions[i] ?? { x: i * 120, y: 0 },
    type: 'compoundNode' as const,
  }));

  const edges: StackGraphEdge[] = (intelligence.interactions ?? []).map((interaction, i) => {
    const token = INTERACTION_TOKENS[interaction.type] ?? INTERACTION_TOKENS.Neutral;
    const thickness = Math.max(1, Math.round(interaction.confidence * 3));
    const dashed = !interaction.hintBacked;

    return {
      id: `e-${slugify(interaction.compoundA)}-${slugify(interaction.compoundB)}-${i}`,
      source: slugify(interaction.compoundA),
      target: slugify(interaction.compoundB),
      data: {
        interactionType: interaction.type,
        confidence: interaction.confidence,
        sharedPathways: interaction.sharedPathways ?? [],
        reason: interaction.reason,
        hintBacked: interaction.hintBacked,
        label: `${interaction.sharedPathways?.length ?? 0} pathways`,
      },
      animated: interaction.type === 'Synergistic',
      style: {
        stroke: token.edgeColor,
        strokeWidth: thickness,
        ...(dashed ? { strokeDasharray: '5,4' } : {}),
      },
      type: 'interactionEdge' as const,
    };
  });

  return { nodes, edges };
}

// Filter helpers for the UI controls
export type GraphFilter = {
  types: string[];
  minConfidence: number;
  concernsOnly: boolean;
  synergiesOnly: boolean;
};

export function applyGraphFilter(data: StackGraphData, filter: GraphFilter): StackGraphData {
  let edges = data.edges;

  if (filter.types.length > 0) {
    edges = edges.filter((e) => filter.types.includes(e.data.interactionType));
  }
  if (filter.minConfidence > 0) {
    edges = edges.filter((e) => e.data.confidence >= filter.minConfidence);
  }
  if (filter.concernsOnly) {
    edges = edges.filter((e) => ['Redundant', 'Interfering'].includes(e.data.interactionType));
  }
  if (filter.synergiesOnly) {
    edges = edges.filter((e) => ['Synergistic', 'Complementary'].includes(e.data.interactionType));
  }

  const usedNodeIds = new Set(edges.flatMap((e) => [e.source, e.target]));
  const nodes = data.nodes.filter((n) => usedNodeIds.has(n.id) || data.nodes.length <= 1);

  return { nodes, edges };
}
