'use client';

import { useCallback, useEffect, useState } from 'react';
import {
  ReactFlow,
  Background,
  Controls,
  useNodesState,
  useEdgesState,
  type Node,
  type Edge,
  Handle,
  Position,
  getBezierPath,
  EdgeLabelRenderer,
  ReactFlowProvider,
  type NodeTypes,
  type EdgeTypes,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import { cn } from '@/lib/utils';
import type { ReasoningGraph, GraphNode, GraphEdge } from '@/lib/types';

interface ReasoningGraphViewerProps {
  graph: ReasoningGraph;
  className?: string;
}

// ── Node kind styles ───────────────────────────────────────────────────────

type NodeKind = GraphNode['kind'];
type EdgeRelation = GraphEdge['relation'];

const NODE_KIND_STYLES: Record<NodeKind, string> = {
  claim:         'bg-blue-500/10 border-blue-400/30 text-blue-300',
  assumption:    'bg-slate-500/10 border-slate-400/30 text-slate-300',
  risk:          'bg-red-500/10 border-red-400/30 text-red-300',
  decision:      'bg-emerald-500/10 border-emerald-400/30 text-emerald-300',
  mitigation:    'bg-teal-500/10 border-teal-400/30 text-teal-300',
  contradiction: 'bg-orange-500/10 border-orange-400/30 text-orange-300',
};

const EDGE_STYLES: Record<EdgeRelation, React.CSSProperties> = {
  contradicts:  { stroke: '#ef4444', strokeDasharray: '6 3' },
  supports:     { stroke: '#22c55e' },
  depends_on:   { stroke: '#94a3b8' },
  mitigates:    { stroke: '#14b8a6' },
  derives_from: { stroke: '#64748b', strokeDasharray: '3 3' },
};

// ── Custom node ────────────────────────────────────────────────────────────

interface KindNodeData extends Record<string, unknown> {
  kind: NodeKind;
  label: string;
  roleOrigin?: string;
  evidenceRefs?: string[];
}

function KindNode({ data, selected }: { data: KindNodeData; selected?: boolean }) {
  const styleClass = NODE_KIND_STYLES[data.kind] ?? NODE_KIND_STYLES.claim;
  return (
    <div
      className={cn(
        'relative px-3 py-2.5 rounded-xl border text-center cursor-pointer transition-all min-w-[120px] max-w-[180px]',
        styleClass,
        selected && 'ring-1 ring-white/30 shadow-lg',
      )}
    >
      <Handle type="target" position={Position.Top} className="!w-1.5 !h-1.5 !bg-white/20 !border-0" />
      <span className="block text-[9px] font-semibold uppercase tracking-widest opacity-50 mb-0.5">
        {data.kind}
      </span>
      <p className="text-[11px] font-medium leading-tight line-clamp-3">{data.label}</p>
      {data.roleOrigin && (
        <span className="mt-1 inline-block text-[9px] opacity-50 font-mono">{data.roleOrigin}</span>
      )}
      <Handle type="source" position={Position.Bottom} className="!w-1.5 !h-1.5 !bg-white/20 !border-0" />
    </div>
  );
}

// ── Custom edge ────────────────────────────────────────────────────────────

interface RelationEdgeData extends Record<string, unknown> {
  relation: EdgeRelation;
}

function RelationEdge({
  id, sourceX, sourceY, targetX, targetY, sourcePosition, targetPosition, data,
}: {
  id: string;
  sourceX: number; sourceY: number;
  targetX: number; targetY: number;
  sourcePosition: Position; targetPosition: Position;
  data?: RelationEdgeData;
  style?: React.CSSProperties;
}) {
  const [edgePath, labelX, labelY] = getBezierPath({
    sourceX, sourceY, sourcePosition, targetX, targetY, targetPosition,
  });
  const edgeStyle = data?.relation ? EDGE_STYLES[data.relation] : {};

  return (
    <>
      <path
        id={id}
        className="react-flow__edge-path"
        d={edgePath}
        style={{ ...edgeStyle, fill: 'none', strokeWidth: 1.5 }}
      />
      <EdgeLabelRenderer>
        <div
          style={{
            transform: `translate(-50%, -50%) translate(${labelX}px,${labelY}px)`,
            pointerEvents: 'all',
          }}
          className="absolute nodrag nopan"
        >
          {data?.relation && (
            <span className="text-[9px] text-white/25 bg-[#0a0a0c] px-1.5 py-0.5 rounded-full border border-white/8 font-mono">
              {data.relation}
            </span>
          )}
        </div>
      </EdgeLabelRenderer>
    </>
  );
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const nodeTypes: NodeTypes = { kindNode: KindNode as any };
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const edgeTypes: EdgeTypes = { relationEdge: RelationEdge as any };

// ── Layout helper ──────────────────────────────────────────────────────────

const COL_WIDTH = 210;
const ROW_HEIGHT = 120;
const COLS_PER_ROW = 4;

function buildFlowElements(
  graphNodes: GraphNode[],
  graphEdges: GraphEdge[],
  visibleKinds: Set<NodeKind>,
  visibleRelations: Set<EdgeRelation>,
): { nodes: Node<KindNodeData>[]; edges: Edge<RelationEdgeData>[] } {
  const decisionNodes = graphNodes.filter(n => n.kind === 'decision');
  const otherNodes = graphNodes.filter(n => n.kind !== 'decision');

  const positioned: Node<KindNodeData>[] = [];

  decisionNodes.forEach((n, i) => {
    const x = (i - decisionNodes.length / 2) * COL_WIDTH + COL_WIDTH / 2;
    positioned.push({
      id: n.id,
      type: 'kindNode',
      position: { x, y: 0 },
      data: { kind: n.kind, label: n.label, roleOrigin: n.roleOrigin, evidenceRefs: n.evidenceRefs },
      hidden: !visibleKinds.has(n.kind),
    });
  });

  otherNodes.forEach((n, i) => {
    const row = Math.floor(i / COLS_PER_ROW);
    const col = i % COLS_PER_ROW;
    const rowCount = Math.min(COLS_PER_ROW, otherNodes.length - row * COLS_PER_ROW);
    const offsetX = ((COLS_PER_ROW - rowCount) / 2) * COL_WIDTH;
    positioned.push({
      id: n.id,
      type: 'kindNode',
      position: { x: offsetX + col * COL_WIDTH, y: ROW_HEIGHT * (row + 1) + 40 },
      data: { kind: n.kind, label: n.label, roleOrigin: n.roleOrigin, evidenceRefs: n.evidenceRefs },
      hidden: !visibleKinds.has(n.kind),
    });
  });

  const visibleIds = new Set(positioned.filter(n => !n.hidden).map(n => n.id));

  const edges: Edge<RelationEdgeData>[] = graphEdges
    .filter(e => visibleRelations.has(e.relation) && visibleIds.has(e.source) && visibleIds.has(e.target))
    .map(e => ({
      id: `${e.source}--${e.relation}--${e.target}`,
      source: e.source,
      target: e.target,
      type: 'relationEdge',
      data: { relation: e.relation },
    }));

  return { nodes: positioned, edges };
}

// ── Node detail panel ──────────────────────────────────────────────────────

function NodeDetailPanel({ node, onClose }: { node: GraphNode; onClose: () => void }) {
  const styleClass = NODE_KIND_STYLES[node.kind] ?? NODE_KIND_STYLES.claim;
  return (
    <div className="absolute top-3 right-3 z-20 w-64 rounded-xl border border-white/10 bg-[#0a0a0c]/95 backdrop-blur p-4 shadow-2xl">
      <div className="flex items-start justify-between gap-2 mb-3">
        <span className={cn('text-[10px] font-semibold px-2 py-0.5 rounded-full border', styleClass)}>
          {node.kind}
        </span>
        <button
          onClick={onClose}
          className="text-white/30 hover:text-white/60 text-sm leading-none mt-0.5"
        >
          ✕
        </button>
      </div>
      <p className="text-[12px] text-white/80 leading-relaxed mb-2">{node.label}</p>
      {node.roleOrigin && (
        <p className="text-[11px] text-white/40 font-mono">Origin: {node.roleOrigin}</p>
      )}
      {node.evidenceRefs && node.evidenceRefs.length > 0 && (
        <div className="mt-2">
          <p className="text-[10px] text-white/30 mb-1 uppercase tracking-wider">Evidence</p>
          <div className="flex flex-wrap gap-1">
            {node.evidenceRefs.map((ref, i) => (
              <span
                key={i}
                className="text-[9px] font-mono text-white/40 bg-white/5 px-1.5 py-0.5 rounded border border-white/8"
              >
                {ref}
              </span>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

// ── Filter constants ───────────────────────────────────────────────────────

const ALL_KINDS: NodeKind[] = ['claim', 'assumption', 'risk', 'decision', 'mitigation', 'contradiction'];
const ALL_RELATIONS: EdgeRelation[] = ['supports', 'contradicts', 'depends_on', 'mitigates', 'derives_from'];

// ── Inner component ────────────────────────────────────────────────────────

function ReasoningGraphViewerInner({ graph, className }: ReasoningGraphViewerProps) {
  const hasDecisionNode = graph.nodes.some(n => n.kind === 'decision');
  const hasDerivesFrom = graph.edges.some(e => e.relation === 'derives_from');

  const [visibleKinds, setVisibleKinds] = useState<Set<NodeKind>>(new Set(ALL_KINDS));
  const [visibleRelations, setVisibleRelations] = useState<Set<EdgeRelation>>(new Set(ALL_RELATIONS));
  const [selectedNode, setSelectedNode] = useState<GraphNode | null>(null);

  const { nodes: initNodes, edges: initEdges } = buildFlowElements(
    graph.nodes, graph.edges, visibleKinds, visibleRelations,
  );

  const [nodes, setNodes, onNodesChange] = useNodesState<Node<KindNodeData>>(initNodes);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge<RelationEdgeData>>(initEdges);

  useEffect(() => {
    const { nodes: n, edges: e } = buildFlowElements(graph.nodes, graph.edges, visibleKinds, visibleRelations);
    setNodes(n);
    setEdges(e);
  }, [visibleKinds, visibleRelations, graph.nodes, graph.edges, setNodes, setEdges]);

  const toggleKind = useCallback((kind: NodeKind) => {
    setVisibleKinds(prev => {
      const next = new Set(prev);
      next.has(kind) ? next.delete(kind) : next.add(kind);
      return next;
    });
  }, []);

  const toggleRelation = useCallback((rel: EdgeRelation) => {
    setVisibleRelations(prev => {
      const next = new Set(prev);
      next.has(rel) ? next.delete(rel) : next.add(rel);
      return next;
    });
  }, []);

  const handleNodeClick = useCallback((_: React.MouseEvent, node: Node) => {
    const data = node.data as KindNodeData;
    setSelectedNode({
      id: node.id,
      kind: data.kind,
      label: data.label,
      roleOrigin: data.roleOrigin,
      evidenceRefs: data.evidenceRefs,
    });
  }, []);

  if (!hasDecisionNode || !hasDerivesFrom) {
    return (
      <div className={cn('rounded-2xl border border-red-500/30 bg-red-500/5 p-6', className)}>
        <p className="text-sm font-semibold text-red-300 mb-2">Lineage integrity error</p>
        <p className="text-xs text-red-300/70 leading-relaxed">
          This reasoning graph has no traceable path to an originating intent node.
          Rendering blocked per Keon canon.
        </p>
      </div>
    );
  }

  return (
    <div className={cn('rounded-2xl border border-white/10 bg-[#0a0a0c] overflow-hidden', className)}>
      {/* Header */}
      <div className="px-5 py-3 border-b border-white/8 flex items-center gap-2.5">
        <span className="text-[11px] font-mono text-white/30 uppercase tracking-widest">KE-8</span>
        <span className="text-sm font-semibold text-white/80">Reasoning Graph</span>
        <span className="text-[10px] text-white/30 bg-white/5 border border-white/8 px-2 py-0.5 rounded-full font-mono ml-auto">
          {graph.graphId}
        </span>
      </div>

      {/* Node-kind filter */}
      <div className="flex flex-wrap items-center gap-1.5 px-5 py-2.5 border-b border-white/8">
        <span className="text-[10px] text-white/20 font-mono uppercase tracking-widest mr-1">Nodes</span>
        {ALL_KINDS.map(kind => (
          <button
            key={kind}
            onClick={() => toggleKind(kind)}
            className={cn(
              'text-[10px] font-semibold px-2 py-0.5 rounded-full border transition-all',
              visibleKinds.has(kind)
                ? NODE_KIND_STYLES[kind]
                : 'border-white/8 text-white/20 bg-transparent hover:bg-white/5',
            )}
          >
            {kind}
          </button>
        ))}
      </div>

      {/* Edge-relation filter */}
      <div className="flex flex-wrap items-center gap-1.5 px-5 py-2.5 border-b border-white/8">
        <span className="text-[10px] text-white/20 font-mono uppercase tracking-widest mr-1">Edges</span>
        {ALL_RELATIONS.map(rel => (
          <button
            key={rel}
            onClick={() => toggleRelation(rel)}
            className={cn(
              'text-[10px] font-medium px-2 py-0.5 rounded-full border transition-all',
              visibleRelations.has(rel)
                ? 'border-white/20 text-white/60 bg-white/8'
                : 'border-white/8 text-white/20 bg-transparent hover:bg-white/5',
            )}
          >
            {rel}
          </button>
        ))}
      </div>

      {/* Graph canvas */}
      <div className="relative h-[500px]">
        <ReactFlow
          nodes={nodes}
          edges={edges}
          onNodesChange={onNodesChange}
          onEdgesChange={onEdgesChange}
          onNodeClick={handleNodeClick}
          nodeTypes={nodeTypes}
          edgeTypes={edgeTypes}
          fitView
          fitViewOptions={{ padding: 0.3 }}
          minZoom={0.2}
          maxZoom={2}
          proOptions={{ hideAttribution: true }}
          style={{ background: 'transparent' }}
        >
          <Background color="rgba(255,255,255,0.03)" gap={24} />
          <Controls className="!bg-[#0F141B] !border-white/10 !rounded-xl" />
        </ReactFlow>

        {selectedNode && (
          <NodeDetailPanel node={selectedNode} onClose={() => setSelectedNode(null)} />
        )}
      </div>
    </div>
  );
}

// ── Public export ──────────────────────────────────────────────────────────

export function ReasoningGraphViewer(props: ReasoningGraphViewerProps) {
  return (
    <ReactFlowProvider>
      <ReasoningGraphViewerInner {...props} />
    </ReactFlowProvider>
  );
}
