'use client';

import { Header } from '@/components/Header';
import { GlassCard } from '@/components/ui/GlassCard';
import { getApiBaseUrl } from '@/lib/apiBase';
import { useEffect, useRef, useState } from 'react';

const API_URL = getApiBaseUrl();

interface SystemStats {
  profiles: number;
  knowledgeEntries: number;
  totalCompoundRecords: number;
  totalCheckIns: number;
}

export default function AdminPage() {
  const [stats, setStats] = useState<SystemStats | null>(null);
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

  return (
    <div className="flex-1 flex flex-col min-h-screen bg-[#0B0F14]">
      <Header title="System Administration" subtitle="Infrastructure & Knowledge Management" />

      <main className="flex-1 p-6 space-y-6 max-w-5xl mx-auto w-full">
        {/* Stats overview */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <StatMiniCard label="Profiles" value={stats?.profiles ?? 0} color="blue" />
          <StatMiniCard label="Knowledge" value={stats?.knowledgeEntries ?? 0} color="emerald" />
          <StatMiniCard label="Recordings" value={stats?.totalCompoundRecords ?? 0} color="purple" />
          <StatMiniCard label="Logs" value={stats?.totalCheckIns ?? 0} color="orange" />
        </div>

        <GlassCard className="p-6 space-y-4">
          <h3 className="text-sm font-bold text-white/50 uppercase tracking-widest">Knowledge Governance</h3>
          <ul className="space-y-3">
            <li className="flex items-center gap-3 text-sm text-white/60">
              <span className="w-2 h-2 rounded-full bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.5)]" />
              Canonical bulk ingest disabled
            </li>
            <li className="flex items-center gap-3 text-sm text-white/60">
              <span className="w-2 h-2 rounded-full bg-blue-500 shadow-[0_0_8px_rgba(59,130,246,0.5)]" />
              Source-registry authorization required
            </li>
            <li className="flex items-center gap-3 text-sm text-white/60">
              <span className="w-2 h-2 rounded-full bg-purple-500 shadow-[0_0_8px_rgba(168,85,247,0.5)]" />
              Provenance and human review required before promotion
            </li>
          </ul>
        </GlassCard>
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
