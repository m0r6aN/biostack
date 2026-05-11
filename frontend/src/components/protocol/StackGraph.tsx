'use client';

import { useCallback, useState, useEffect } from 'react';
import {
  ReactFlow,
  Background,
  Controls,
  MiniMap,
  useNodesState,
  useEdgesState,
  type NodeTypes,
  type EdgeTypes,
  type Node,
  type Edge,
  Handle,
  Position,
  EdgeLabelRenderer,
  getBezierPath,
  useReactFlow,
  ReactFlowProvider,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import { buildStackGraph, applyGraphFilter, type GraphFilter, type StackGraphNode, type StackGraphEdge } from '@/lib/graph/buildStackGraph';
import { EVIDENCE_TIER_TOKENS, INTERACTION_TOKENS } from '@/styles/tokens';
import { cn } from '@/lib/utils';
import { track } from '@/lib/telemetry';
import type { InteractionIntelligence, CompoundRecord, KnowledgeEntry } from '@/lib/types';
import { CompoundLink } from '@/components/knowledge/CompoundLink';

// ── Custom Node ────────────────────────────────────────────────────────────

function CompoundNode({ data, selected }: { data: StackGraphNode['data']; selected?: boolean }) {
  const tierToken = data.evidenceTier ? (EVIDENCE_TIER_TOKENS[data.evidenceTier] ?? EVIDENCE_TIER_TOKENS.unknown) : null;

  return (
    <div
      className={cn(
        'relative px-4 py-3 rounded-2xl border text-center cursor-pointer transition-all min-w-[110px]',
        selected
          ? 'border-emerald-400/50 bg-emerald-500/10 shadow-[0_0_20px_rgba(52,211,153,0.2)]'
          : 'border-white/10 bg-[#0F141B] hover:border-white/20 hover:bg-white/[0.03]',
      )}
    >
      <Handle type="target" position={Position.Top} className="!w-2 !h-2 !bg-white/20 !border-0" />

      {/* Finding count badge */}
      {data.findingCount > 0 && (
        <span className="absolute -top-2 -right-2 w-5 h-5 rounded-full bg-amber-500/20 border border-amber-400/30 text-amber-400 text-[9px] font-bold flex items-center justify-center">
          {data.findingCount}
        </span>
      )}

      <p className="text-[11px] font-semibold text-white/85 leading-tight">{data.label}</p>

      {tierToken && (
        <span className={cn('mt-1.5 inline-block text-[9px] font-medium px-1.5 py-0.5 rounded-full', tierToken.bg, tierToken.color)}>
          {tierToken.short}
        </span>
      )}

      <CompoundLink
        displayName={data.label}
        className="text-[9px] text-white/20 hover:text-white/50 mt-1 block"
      />

      <Handle type="source" position={Position.Bottom} className="!w-2 !h-2 !bg-white/20 !border-0" />
    </div>
  );
}

// ── Custom Edge ────────────────────────────────────────────────────────────

function InteractionEdge({
  id, sourceX, sourceY, targetX, targetY, sourcePosition, targetPosition,
  data, style, markerEnd,
}: any) {
  const [edgePath, labelX, labelY] = getBezierPath({ sourceX, sourceY, sourcePosition, targetX, targetY, targetPosition });

  return (
    <>
      <path id={id} className="react-flow__edge-path" d={edgePath} style={style} markerEnd={markerEnd} />
      <EdgeLabelRenderer>
        <div
          style={{ transform: `translate(-50%, -50%) translate(${labelX}px,${labelY}px)`, pointerEvents: 'all' }}
          className="absolute nodrag nopan"
        >
          {data?.label && (
            <span className="text-[9px] text-white/30 bg-[#0B0F14] px-1.5 py-0.5 rounded-full border border-white/8 font-mono">
              {data.label}
            </span>
          )}
        </div>
      </EdgeLabelRenderer>
    </>
  );
}

const nodeTypes: NodeTypes = { compoundNode: CompoundNode as any };
const edgeTypes: EdgeTypes = { interactionEdge: InteractionEdge };

// ── Main Graph ─────────────────────────────────────────────────────────────

interface StackGraphInnerProps {
  intelligence: InteractionIntelligence | null;
  compounds: CompoundRecord[];
  onNodeClick?: (compoundName: string) => void;
  onEdgeClick?: (edgeData: StackGraphEdge['data']) => void;
  variant?: 'full' | 'mini';
}

const DEFAULT_FILTER: GraphFilter = { types: [], minConfidence: 0, concernsOnly: false, synergiesOnly: false };

function StackGraphInner({ intelligence, compounds, onNodeClick, onEdgeClick, variant = 'full' }: StackGraphInnerProps) {
  const rawData = buildStackGraph(intelligence, compounds);
  const [filter, setFilter] = useState<GraphFilter>(DEFAULT_FILTER);
  const data = applyGraphFilter(rawData, filter);
  const [nodes, setNodes, onNodesChange] = useNodesState(data.nodes as any);
  const [edges, setEdges, onEdgesChange] = useEdgesState(data.edges as any);

  useEffect(() => {
    const filtered = applyGraphFilter(rawData, filter);
    setNodes(filtered.nodes as any);
    setEdges(filtered.edges as any);
  }, [filter, intelligence, compounds]);

  const handleNodeClick = useCallback((_: any, node: Node) => {
    const name = (node.data as StackGraphNode['data']).label;
    track({ name: 'stack_graph_node_click', compoundName: name });
    onNodeClick?.(name);
  }, [onNodeClick]);

  const handleEdgeClick = useCallback((_: any, edge: Edge) => {
    const edgeData = (edge as StackGraphEdge).data;
    track({ name: 'stack_graph_edge_click', interactionType: edgeData.interactionType });
    onEdgeClick?.(edgeData);
  }, [onEdgeClick]);

  if (rawData.nodes.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center h-full text-center p-8">
        <div className="w-16 h-16 rounded-2xl border border-white/5 bg-white/[0.02] flex items-center justify-center mb-4">
          <svg className="w-7 h-7 text-white/20" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="1.5">
            <path strokeLinecap="round" strokeLinejoin="round" d="M13.19 8.688a4.5 4.5 0 0 1 1.242 7.244l-4.5 4.5a4.5 4.5 0 0 1-6.364-6.364l1.757-1.757m13.35-.622 1.757-1.757a4.5 4.5 0 0 0-6.364-6.364l-4.5 4.5a4.5 4.5 0 0 0 1.242 7.244" />
          </svg>
        </div>
        <p className="text-sm font-semibold text-white/50">No active compounds</p>
        <p className="text-xs text-white/30 mt-1">Add compounds to visualize stack interactions.</p>
      </div>
    );
  }

  return (
    <div className="relative w-full h-full">
      {/* Filter bar (full variant only) */}
      {variant === 'full' && (
        <div className="absolute top-3 left-3 z-10 flex flex-wrap gap-2">
          {Object.entries(INTERACTION_TOKENS).map(([type, token]) => {
            const active = filter.types.includes(type);
            return (
              <button
                key={type}
                onClick={() => setFilter((f) => ({
                  ...f,
                  types: active ? f.types.filter((t) => t !== type) : [...f.types, type],
                }))}
                className={cn(
                  'text-[10px] font-semibold px-2.5 py-1 rounded-full border transition-all',
                  active ? `${token.bg} ${token.color} ${token.border}` : 'border-white/10 text-white/30 bg-white/[0.02] hover:bg-white/5',
                )}
              >
                {token.label}
              </button>
            );
          })}
          <button
            onClick={() => setFilter((f) => ({ ...f, concernsOnly: !f.concernsOnly, synergiesOnly: false }))}
            className={cn('text-[10px] font-semibold px-2.5 py-1 rounded-full border transition-all',
              filter.concernsOnly ? 'bg-rose-500/10 text-rose-300 border-rose-400/20' : 'border-white/10 text-white/30 bg-white/[0.02] hover:bg-white/5')}
          >
            Concerns only
          </button>
          <button
            onClick={() => setFilter((f) => ({ ...f, synergiesOnly: !f.synergiesOnly, concernsOnly: false }))}
            className={cn('text-[10px] font-semibold px-2.5 py-1 rounded-full border transition-all',
              filter.synergiesOnly ? 'bg-emerald-500/10 text-emerald-300 border-emerald-400/20' : 'border-white/10 text-white/30 bg-white/[0.02] hover:bg-white/5')}
          >
            Synergies only
          </button>
        </div>
      )}

      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onNodeClick={handleNodeClick}
        onEdgeClick={handleEdgeClick}
        nodeTypes={nodeTypes}
        edgeTypes={edgeTypes}
        fitView
        fitViewOptions={{ padding: 0.3 }}
        minZoom={0.3}
        maxZoom={2}
        proOptions={{ hideAttribution: true }}
        style={{ background: 'transparent' }}
      >
        <Background color="rgba(255,255,255,0.03)" gap={24} />
        {variant === 'full' && (
          <>
            <Controls className="!bg-[#0F141B] !border-white/10 !rounded-2xl" />
            <MiniMap
              style={{ background: '#0B0F14', border: '1px solid rgba(255,255,255,0.08)' }}
              nodeColor="#22c55e30"
              maskColor="rgba(0,0,0,0.6)"
            />
          </>
        )}
      </ReactFlow>
    </div>
  );
}

export function StackGraph(props: StackGraphInnerProps) {
  return (
    <ReactFlowProvider>
      <StackGraphInner {...props} />
    </ReactFlowProvider>
  );
}
