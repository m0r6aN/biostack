'use client';

import {
  Apple,
  BookOpen,
  Calendar,
  Gauge,
  LayoutDashboard,
  LineChart,
  Pill,
  type LucideIcon,
} from 'lucide-react';
import { cn } from '@/lib/utils';

export type ProtocolTabId =
  | 'dashboard'
  | 'calendar'
  | 'diet'
  | 'supplements'
  | 'monitoring'
  | 'progress'
  | 'resources';

interface TabDef {
  id: ProtocolTabId;
  label: string;
  icon: LucideIcon;
}

export const PROTOCOL_TABS: TabDef[] = [
  { id: 'dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { id: 'calendar', label: 'Calendar & Schedule', icon: Calendar },
  { id: 'diet', label: 'Diet & Lifestyle', icon: Apple },
  { id: 'supplements', label: 'Supplementation', icon: Pill },
  { id: 'monitoring', label: 'Monitoring & Labs', icon: LineChart },
  { id: 'progress', label: 'Progress & Milestones', icon: Gauge },
  { id: 'resources', label: 'Resources', icon: BookOpen },
];

interface ProtocolTabBarProps {
  active: ProtocolTabId;
  onChange: (id: ProtocolTabId) => void;
}

export function ProtocolTabBar({ active, onChange }: ProtocolTabBarProps) {
  return (
    <div
      role="tablist"
      aria-label="Protocol sections"
      className="flex gap-1 overflow-x-auto border-b border-white/[0.08] pb-px"
    >
      {PROTOCOL_TABS.map((tab) => {
        const Icon = tab.icon;
        const isActive = tab.id === active;
        return (
          <button
            key={tab.id}
            type="button"
            role="tab"
            aria-selected={isActive}
            onClick={() => onChange(tab.id)}
            className={cn(
              'flex items-center gap-2 whitespace-nowrap border-b-2 px-4 py-3 text-sm font-medium transition-colors',
              isActive
                ? 'border-emerald-400 text-white'
                : 'border-transparent text-white/45 hover:text-white/80'
            )}
          >
            <Icon className="h-4 w-4 shrink-0" />
            <span>{tab.label}</span>
          </button>
        );
      })}
    </div>
  );
}
