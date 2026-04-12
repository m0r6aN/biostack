'use client';

import { Header } from '@/components/Header';
import { Button } from '@/components/ui/Button';
import { GlassCard } from '@/components/ui/GlassCard';
import { useEffect, useRef, useState } from 'react';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

interface SystemStats {
  profiles: number;
  knowledgeEntries: number;
  totalCompoundRecords: number;
  totalCheckIns: number;
}

export default function AdminPage() {
  const [stats, setStats] = useState<SystemStats | null>(null);
  const [jsonInput, setJsonInput] = useState('');
  const [isIngesting, setIsIngesting] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error', text: string } | null>(null);
  const devTokenRef = useRef<string | null>(null);

  useEffect(() => {
    acquireToken().then(fetchStats);
  }, []);

  /** In dev the backend issues a signed admin JWT on demand. No-op in production (endpoint absent). */
  async function acquireToken(): Promise<void> {
    if (devTokenRef.current) return;
    try {
      const res = await fetch(`${API_URL}/api/v1/auth/dev-token`, { method: 'POST' });
      if (res.ok) {
        const { token } = await res.json();
        devTokenRef.current = token;
      }
    } catch {
      // Endpoint absent in production — silently skip
    }
  }

  function authHeaders(): Record<string, string> {
    return devTokenRef.current
      ? { Authorization: `Bearer ${devTokenRef.current}` }
      : {};
  }

  const fetchStats = async () => {
    try {
      const res = await fetch(`${API_URL}/api/v1/admin/stats`, { headers: authHeaders() });
      if (res.ok) {
        setStats(await res.json());
      }
    } catch (err) {
      console.error('Failed to fetch stats', err);
    }
  };

  const handleIngest = async () => {
    setIsIngesting(true);
    setMessage(null);

    // Re-acquire token in case the page was open before the API came up.
    await acquireToken();

    let entries: unknown[];

    try {
      const parsed = JSON.parse(jsonInput);
      entries = Array.isArray(parsed) ? parsed : [parsed];
    } catch (err) {
      setMessage({ type: 'error', text: `Invalid JSON: ${err instanceof Error ? err.message : String(err)}` });
      setIsIngesting(false);
      return;
    }

    try {
      const res = await fetch(`${API_URL}/api/v1/admin/knowledge/ingest`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', ...authHeaders() },
        body: JSON.stringify(entries),
      });

      if (res.ok) {
        const result = await res.json();
        setMessage({ type: 'success', text: result.message });
        setJsonInput('');
        fetchStats();
      } else {
        const err = await res.text();
        setMessage({ type: 'error', text: `Ingest failed: ${err}` });
      }
    } catch (err) {
      setMessage({ type: 'error', text: `Ingest failed: ${err instanceof Error ? err.message : String(err)}` });
    } finally {
      setIsIngesting(false);
    }
  };

  return (
    <div className="flex-1 flex flex-col min-h-screen bg-[#0B0F14]">
      <Header title="System Administration" subtitle="Infrastructure & Knowledge Management" />

      <main className="flex-1 p-6 space-y-6 max-w-5xl mx-auto w-full">
        {/* Stats Dashboard */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <StatMiniCard label="Profiles" value={stats?.profiles ?? 0} color="blue" />
          <StatMiniCard label="Knowledge" value={stats?.knowledgeEntries ?? 0} color="emerald" />
          <StatMiniCard label="Recordings" value={stats?.totalCompoundRecords ?? 0} color="purple" />
          <StatMiniCard label="Logs" value={stats?.totalCheckIns ?? 0} color="orange" />
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* JSON Ingest Tool */}
          <GlassCard className="lg:col-span-2 p-6 flex flex-col space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="text-lg font-semibold text-white">Bulk Knowledge Ingest</h3>
                <p className="text-sm text-white/40">Paste compound JSON to upsert into the master knowledge base.</p>
              </div>
              <Button
                variant="primary"
                onClick={handleIngest}
                disabled={isIngesting || !jsonInput.trim()}
                className="bg-emerald-500 hover:bg-emerald-400"
              >
                {isIngesting ? 'Ingesting...' : 'Perform Upsert'}
              </Button>
            </div>

            <textarea
              value={jsonInput}
              onChange={(e) => setJsonInput(e.target.value)}
              placeholder="[ { 'canonicalName': 'BPC-157', ... }, ... ]"
              className="flex-1 min-h-[400px] bg-black/40 border border-white/10 rounded-xl p-4 text-xs font-mono text-emerald-400 focus:outline-none focus:border-emerald-500/50 transition-colors resize-none shadow-inner"
            />

            {message && (
              <div className={`p-4 rounded-xl border ${message.type === 'success' ? 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400' : 'bg-rose-500/10 border-rose-500/20 text-rose-400'}`}>
                <p className="text-sm font-medium">{message.type === 'success' ? '✔' : '✘'} {message.text}</p>
              </div>
            )}
          </GlassCard>

          {/* Quick Actions & System Info */}
          <div className="space-y-6">
            <GlassCard className="p-6 space-y-4">
              <h3 className="text-sm font-bold text-white/50 uppercase tracking-widest">Active Policies</h3>
              <ul className="space-y-3">
                <li className="flex items-center gap-3 text-sm text-white/60">
                  <span className="w-2 h-2 rounded-full bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.5)]" />
                  Local-First Persistence
                </li>
                <li className="flex items-center gap-3 text-sm text-white/60">
                  <span className="w-2 h-2 rounded-full bg-blue-500 shadow-[0_0_8px_rgba(59,130,246,0.5)]" />
                  Knowledge Immutability (Off)
                </li>
                <li className="flex items-center gap-3 text-sm text-white/60">
                  <span className="w-2 h-2 rounded-full bg-purple-500 shadow-[0_0_8px_rgba(168,85,247,0.5)]" />
                  Auto-Ingest Validator Active
                </li>
              </ul>
            </GlassCard>

            <div className="p-6 rounded-2xl border border-white/5 bg-white/[0.02]">
              <p className="text-xs text-white/30 leading-relaxed italic">
                Note: Bulk ingest will perform an **upsert** (update by name if exists, otherwise insert).
                Ensure canonical names match existing entries to avoid duplication.
              </p>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}

function StatMiniCard({ label, value, color }: { label: string, value: number, color: 'blue' | 'emerald' | 'purple' | 'orange' }) {
  const colorMap = {
    blue: 'text-blue-400 bg-blue-400/5 border-blue-400/20',
    emerald: 'text-emerald-400 bg-emerald-400/5 border-emerald-400/20',
    purple: 'text-purple-400 bg-purple-400/5 border-purple-400/20',
    orange: 'text-orange-400 bg-orange-400/5 border-orange-400/20',
  };

  return (
    <GlassCard className={`p-4 ${colorMap[color]}`}>
      <p className="text-[10px] uppercase font-bold tracking-widest opacity-60">{label}</p>
      <p className="text-2xl font-bold mt-1 text-white">{value}</p>
    </GlassCard>
  );
}
