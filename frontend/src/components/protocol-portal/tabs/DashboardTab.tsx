'use client';

import { ArrowRight, Check, MessageSquarePlus, TestTube } from 'lucide-react';
import type { DaySchedule, ScheduleItem } from '@/lib/types';
import { GlassCard } from '@/components/ui/GlassCard';
import { cn } from '@/lib/utils';
import { accentClasses } from '../accents';
import { ScheduleIcon } from '../icons';

interface DashboardTabProps {
  today: DaySchedule;
  onViewCalendar: () => void;
  onViewLabs: () => void;
  onLogDoses: () => void;
  onMessageCareTeam: () => void;
}

export function DashboardTab({
  today,
  onViewCalendar,
  onViewLabs,
  onLogDoses,
  onMessageCareTeam,
}: DashboardTabProps) {
  return (
    <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
      <GlassCard variant="base" className="p-6 lg:col-span-2">
        <div className="mb-4 flex items-center justify-between gap-4">
          <div>
            <h3 className="text-lg font-semibold text-white">{today.title}</h3>
            <p className="text-sm text-white/45">{today.subtitle}</p>
          </div>
          <button
            type="button"
            onClick={onViewCalendar}
            className="flex items-center gap-2 whitespace-nowrap rounded-xl border border-white/[0.1] px-4 py-2 text-xs font-medium text-white/70 transition-colors hover:border-white/20"
          >
            <span>Full Calendar</span>
            <ArrowRight className="h-3.5 w-3.5" />
          </button>
        </div>

        <div className="space-y-3">
          {today.items.map((item, index) => (
            <ScheduleItemRow key={`${item.name}-${index}`} item={item} />
          ))}
        </div>
      </GlassCard>

      <GlassCard variant="base" className="p-6">
        <h3 className="mb-4 text-lg font-semibold text-white">Quick Actions</h3>
        <div className="space-y-2">
          <button
            type="button"
            onClick={onLogDoses}
            className="flex w-full items-center justify-center gap-2 rounded-xl bg-emerald-500 px-4 py-3 text-sm font-semibold text-slate-950 transition-colors hover:bg-emerald-400"
          >
            <Check className="h-4 w-4" />
            <span>Log Today&rsquo;s Doses</span>
          </button>
          <SecondaryAction icon={<TestTube className="h-4 w-4" />} label="View Latest Labs" onClick={onViewLabs} />
          <SecondaryAction
            icon={<MessageSquarePlus className="h-4 w-4" />}
            label="Message Care Team"
            onClick={onMessageCareTeam}
          />
        </div>
      </GlassCard>
    </div>
  );
}

function ScheduleItemRow({ item }: { item: ScheduleItem }) {
  const accent = accentClasses(item.accent);

  return (
    <div className="flex items-center justify-between rounded-xl bg-white/[0.03] p-3">
      <div className="flex items-center gap-3">
        <ScheduleIcon iconKey={item.icon} className={cn('h-5 w-5 shrink-0', accent.text)} />
        <div>
          <div className="font-medium text-white">{item.name}</div>
          <div className="text-xs text-white/45">{item.detail}</div>
        </div>
      </div>
      <div className="text-right">
        <div className="font-mono text-sm font-medium text-white/80">{item.time}</div>
        {item.status === 'completed' && <div className="text-xs text-emerald-300">Completed</div>}
        {item.status === 'skipped' && <div className="text-xs text-red-300">Skipped</div>}
      </div>
    </div>
  );
}

function SecondaryAction({
  icon,
  label,
  onClick,
}: {
  icon: React.ReactNode;
  label: string;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="flex w-full items-center justify-center gap-2 rounded-xl border border-white/[0.1] px-4 py-3 text-sm font-medium text-white/75 transition-colors hover:border-white/20"
    >
      {icon}
      <span>{label}</span>
    </button>
  );
}
