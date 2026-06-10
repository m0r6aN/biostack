'use client';

import { TriangleAlert } from 'lucide-react';
import type { MonitoringProtocol } from '@/lib/types';
import { GlassCard } from '@/components/ui/GlassCard';

export function MonitoringLabsTab({ monitoring }: { monitoring: MonitoringProtocol }) {
  return (
    <GlassCard variant="base" className="p-6 sm:p-8">
      <h3 className="text-2xl font-semibold text-white">Monitoring &amp; Lab Protocol</h3>

      <div className="mt-6">
        <h4 className="mb-2 font-semibold text-white">Baseline (Completed)</h4>
        <p className="text-sm text-white/55">{monitoring.baselineCompleted}</p>
      </div>

      <div className="mt-6">
        <h4 className="mb-3 font-semibold text-white">Recurring Labs ({monitoring.recurringCadence})</h4>
        <div className="grid grid-cols-1 gap-3 text-sm sm:grid-cols-2 md:grid-cols-3">
          {monitoring.recurringLabs.map((lab) => (
            <div key={lab} className="rounded-xl bg-white/[0.03] px-4 py-3 text-white/75">
              {lab}
            </div>
          ))}
        </div>
      </div>

      <AdjustmentRulesCallout rules={monitoring.adjustmentRules} />
    </GlassCard>
  );
}

function AdjustmentRulesCallout({ rules }: { rules: MonitoringProtocol['adjustmentRules'] }) {
  return (
    <div className="mt-8 rounded-2xl border border-amber-400/20 bg-amber-500/[0.07] p-5">
      <div className="mb-2 flex items-center gap-2 font-semibold text-amber-200">
        <TriangleAlert className="h-4 w-4" />
        <span>Adjustment Rules</span>
      </div>
      <ul className="space-y-1.5 text-sm text-amber-100/80">
        {rules.map((rule) => (
          <li key={rule.trigger}>
            · <strong className="font-semibold">{rule.trigger}</strong> → {rule.action}
          </li>
        ))}
      </ul>
    </div>
  );
}
