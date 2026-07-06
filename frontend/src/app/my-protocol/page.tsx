'use client';

import { Header } from '@/components/Header';
import { ContactCareTeamModal } from '@/components/protocol-portal/ContactCareTeamModal';
import { DayDetailModal } from '@/components/protocol-portal/DayDetailModal';
import { ProtocolOverviewHero } from '@/components/protocol-portal/ProtocolOverviewHero';
import { ProtocolStatGrid } from '@/components/protocol-portal/ProtocolStatGrid';
import { ProtocolTabBar, type ProtocolTabId } from '@/components/protocol-portal/ProtocolTabBar';
import { CalendarTab } from '@/components/protocol-portal/tabs/CalendarTab';
import { DashboardTab } from '@/components/protocol-portal/tabs/DashboardTab';
import { DietLifestyleTab } from '@/components/protocol-portal/tabs/DietLifestyleTab';
import { MonitoringLabsTab } from '@/components/protocol-portal/tabs/MonitoringLabsTab';
import { ProgressMilestonesTab } from '@/components/protocol-portal/tabs/ProgressMilestonesTab';
import { ResourcesTab } from '@/components/protocol-portal/tabs/ResourcesTab';
import { SupplementationTab } from '@/components/protocol-portal/tabs/SupplementationTab';
import { TierGate } from '@/components/protocol-portal/TierGate';
import { Toast } from '@/components/protocol-portal/Toast';
import { getMockProtocolPortal } from '@/lib/mock/protocolPortal';
import type { DaySchedule } from '@/lib/types';
import { Headset } from 'lucide-react';
import { useMemo, useState } from 'react';

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

/** Deterministic "Mon D" formatter for ISO date strings (avoids locale drift). */
function formatShortDate(iso: string): string {
  const [, month, day] = iso.split('-').map(Number);
  return `${MONTHS[(month ?? 1) - 1]} ${day}`;
}

export default function MyProtocolPage() {
  // PR B intentionally renders from the mock fixture. The backend-compatible
  // API client methods are available for a later live-data integration PR.
  const data = useMemo(() => getMockProtocolPortal(), []);

  const [activeTab, setActiveTab] = useState<ProtocolTabId>('dashboard');
  const [activePhaseNumber, setActivePhaseNumber] = useState(data.overview.currentPhase.number);
  const [selectedDayIso, setSelectedDayIso] = useState<string | null>(null);
  const [contactOpen, setContactOpen] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  const activePhase =
    data.overview.phases.find((phase) => phase.number === activePhaseNumber) ??
    data.overview.currentPhase;

  const weekLabel = useMemo(() => {
    if (data.week.length === 0) return '';
    const first = data.week[0].dateIso;
    const last = data.week[data.week.length - 1].dateIso;
    const year = last.split('-')[0];
    return `${formatShortDate(first)} – ${formatShortDate(last)}, ${year}`;
  }, [data.week]);

  const doseHeadline = useMemo(() => {
    const dose = data.stats[0];
    return dose ? `${dose.label.split(' ')[0]} ${dose.value} ${dose.unit ?? ''}`.trim() : '';
  }, [data.stats]);

  const selectedDay: DaySchedule | null = selectedDayIso
    ? (data.daySchedules[selectedDayIso] ?? buildFallbackDay(data, selectedDayIso))
    : null;

  function handleSwitchPhase(phaseNumber: number) {
    setActivePhaseNumber(phaseNumber);
    const phase = data.overview.phases.find((p) => p.number === phaseNumber);
    setToast(`${phase?.label ?? 'Phase'} is previewed from the PR B mock fixture.`);
  }

  return (
    <div className="w-full">
      <Header
        title="Your Protocol"
        subtitle={data.overview.objective}
        actions={
          <button
            type="button"
            onClick={() => setContactOpen(true)}
            className="flex items-center gap-2 rounded-xl border border-white/[0.1] px-3 py-1.5 text-sm font-medium text-white/75 transition-colors hover:border-white/20"
          >
            <Headset className="h-3.5 w-3.5" />
            <span className="hidden md:inline">Contact Care Team</span>
          </button>
        }
      />

      <div className="mx-auto max-w-7xl space-y-8 p-5 sm:p-8">
        <div className="rounded-2xl border border-amber-400/20 bg-amber-500/[0.08] px-4 py-3 text-sm text-amber-100/85">
          Preview fixture: /my-protocol is mock-backed in this PR. Values shown here are UI review data, not production protocol records, and the tier badges below are not yet enforced — all tabs render regardless of plan.
        </div>

        <ProtocolOverviewHero
          overview={data.overview}
          activePhase={activePhase}
          onSwitchPhase={handleSwitchPhase}
        />

        <ProtocolStatGrid stats={data.stats} />

        <div className="space-y-6">
          <ProtocolTabBar active={activeTab} onChange={setActiveTab} />

          {activeTab === 'dashboard' && (
            <DashboardTab
              today={data.today}
              onViewCalendar={() => setActiveTab('calendar')}
              onViewLabs={() => setActiveTab('monitoring')}
              onLogDoses={() => setToast('Preview action only — no dose log was sent.')}
              onMessageCareTeam={() => setContactOpen(true)}
            />
          )}

          {activeTab === 'calendar' && (
            <TierGate requiredTier="operator">
              <CalendarTab
                week={data.week}
                activePhase={activePhase}
                weekLabel={weekLabel}
                doseHeadline={doseHeadline}
                onSelectDay={setSelectedDayIso}
                onPreviousWeek={() => setToast('Adjacent weeks are not live-wired in this PR B preview.')}
                onNextWeek={() => setToast('Adjacent weeks are not live-wired in this PR B preview.')}
              />
            </TierGate>
          )}

          {activeTab === 'diet' && (
            <TierGate requiredTier="operator">
              <DietLifestyleTab diet={data.diet} />
            </TierGate>
          )}

          {activeTab === 'supplements' && <SupplementationTab supplements={data.supplements} />}

          {activeTab === 'monitoring' && (
            <TierGate requiredTier="commander">
              <MonitoringLabsTab monitoring={data.monitoring} />
            </TierGate>
          )}

          {activeTab === 'progress' && (
            <TierGate requiredTier="operator">
              <ProgressMilestonesTab milestones={data.milestones} />
            </TierGate>
          )}

          {activeTab === 'resources' && <ResourcesTab resources={data.resources} />}
        </div>
      </div>

      {selectedDay && <DayDetailModal day={selectedDay} onClose={() => setSelectedDayIso(null)} />}
      {contactOpen && <ContactCareTeamModal onClose={() => setContactOpen(false)} />}
      {toast && <Toast message={toast} onDismiss={() => setToast(null)} />}
    </div>
  );
}

/** Synthesizes a generic day schedule for days without explicit data. */
function buildFallbackDay(
  data: ReturnType<typeof getMockProtocolPortal>,
  iso: string
): DaySchedule {
  const day = data.week.find((d) => d.dateIso === iso);
  const weekday = day?.weekdayLabel ?? '';
  return {
    dateIso: iso,
    title: `${weekday} ${formatShortDate(iso)}`.trim(),
    subtitle: `Week ${data.overview.currentPhase.currentWeek} · ${data.overview.currentPhase.label}`,
    items: [
      { time: '07:45 AM', name: 'Morning Foundations', detail: 'L-Carnitine, PC, Vitamin E, Selenium, NAC, B-Complex, Zinc' },
      { time: '08:30 AM', name: 'GHK-Cu', detail: '1 mg subcutaneous' },
      { time: '08:30 AM', name: 'BPC-157', detail: '250 mcg' },
      { time: 'With Breakfast & Lunch', name: 'TUDCA', detail: '500 mg per large meal' },
      { time: '08:00 PM', name: 'BPC-157', detail: '250 mcg' },
      { time: '09:00 PM', name: 'Magnesium Glycinate', detail: '500 mg' },
    ],
  };
}
