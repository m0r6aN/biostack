'use client';

import { EmptyState } from '@/components/EmptyState';
import { ErrorState } from '@/components/ErrorState';
import { Header } from '@/components/Header';
import { LoadingSkeleton } from '@/components/LoadingState';
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
import { apiClient } from '@/lib/api';
import { useProfile } from '@/lib/context';
import type {
  DaySchedule,
  DietFramework,
  Milestone,
  MonitoringProtocol,
  ProtocolOverview,
  ProtocolStat,
  ProtocolTier,
  ResourceEntry,
  SupplementPlan,
  WeekDay,
} from '@/lib/types';
import { Headset } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useCallback, useEffect, useMemo, useState } from 'react';

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

const TIER_RANK: Record<ProtocolTier, number> = {
  observer: 0,
  operator: 1,
  commander: 2,
};

interface PortalViewData {
  overview: ProtocolOverview;
  stats: ProtocolStat[];
  today: DaySchedule;
  supplements: SupplementPlan;
  resources: ResourceEntry[];
  week: WeekDay[];
  diet: DietFramework | null;
  monitoring: MonitoringProtocol | null;
  milestones: Milestone[];
}

const EMPTY_DIET: DietFramework = {
  title: '',
  summary: '',
  targets: [],
  rationale: '',
  lifestyle: [],
};

const EMPTY_MONITORING: MonitoringProtocol = {
  baselineCompleted: '',
  recurringCadence: '',
  recurringLabs: [],
  adjustmentRules: [],
};

function formatShortDate(iso: string): string {
  const [, month, day] = iso.split('-').map(Number);
  return `${MONTHS[(month ?? 1) - 1]} ${day}`;
}

function normalizeTier(value: string): ProtocolTier {
  const normalized = value.toLowerCase();
  if (normalized === 'commander') return 'commander';
  if (normalized === 'operator') return 'operator';
  return 'observer';
}

function hasTier(current: ProtocolTier, required: ProtocolTier) {
  return TIER_RANK[current] >= TIER_RANK[required];
}

function shiftIsoDate(iso: string, days: number) {
  const date = new Date(`${iso}T00:00:00Z`);
  date.setUTCDate(date.getUTCDate() + days);
  return date.toISOString().slice(0, 10);
}

export default function MyProtocolPage() {
  const router = useRouter();
  const { currentProfileId } = useProfile();
  const [data, setData] = useState<PortalViewData | null>(null);
  const [tier, setTier] = useState<ProtocolTier>('observer');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<ProtocolTabId>('dashboard');
  const [activePhaseNumber, setActivePhaseNumber] = useState(1);
  const [selectedDay, setSelectedDay] = useState<DaySchedule | null>(null);
  const [contactOpen, setContactOpen] = useState(false);
  const [busy, setBusy] = useState<string | null>(null);
  const [toast, setToast] = useState<string | null>(null);

  const loadPortal = useCallback(async () => {
    if (!currentProfileId) {
      setData(null);
      setLoading(false);
      setError(null);
      return;
    }

    try {
      setLoading(true);
      const subscription = await apiClient.getCurrentSubscription();
      const currentTier = normalizeTier(subscription.tier);

      const [active, today, supplements, resources, operatorSections, monitoring] = await Promise.all([
        apiClient.getProtocolPortalActive(currentProfileId),
        apiClient.getProtocolPortalSchedule(currentProfileId),
        apiClient.getProtocolPortalSupplements(currentProfileId),
        apiClient.getProtocolPortalResources(currentProfileId),
        hasTier(currentTier, 'operator')
          ? Promise.all([
              apiClient.getProtocolPortalWeek(currentProfileId),
              apiClient.getProtocolPortalDiet(currentProfileId),
              apiClient.getProtocolPortalMilestones(currentProfileId),
            ])
          : Promise.resolve(null),
        hasTier(currentTier, 'commander')
          ? apiClient.getProtocolPortalMonitoring(currentProfileId)
          : Promise.resolve(null),
      ]);

      setTier(currentTier);
      setData({
        overview: active.overview,
        stats: active.stats,
        today,
        supplements,
        resources,
        week: operatorSections?.[0] ?? [],
        diet: operatorSections?.[1] ?? null,
        milestones: operatorSections?.[2] ?? [],
        monitoring,
      });
      setActivePhaseNumber(active.overview.currentPhase.number);
      setError(null);
    } catch {
      setData(null);
      setError('Your live protocol could not be loaded. Confirm the selected profile has an active protocol and try again.');
    } finally {
      setLoading(false);
    }
  }, [currentProfileId]);

  useEffect(() => {
    void loadPortal();
  }, [loadPortal]);

  const activePhase = useMemo(() => {
    if (!data) return null;
    return data.overview.phases.find((phase) => phase.number === activePhaseNumber) ?? data.overview.currentPhase;
  }, [activePhaseNumber, data]);

  const weekLabel = useMemo(() => {
    if (!data?.week.length) return '';
    const first = data.week[0].dateIso;
    const last = data.week[data.week.length - 1].dateIso;
    return `${formatShortDate(first)} – ${formatShortDate(last)}, ${last.split('-')[0]}`;
  }, [data?.week]);

  const doseHeadline = useMemo(() => {
    const dose = data?.stats[0];
    return dose ? `${dose.label.split(' ')[0]} ${dose.value} ${dose.unit ?? ''}`.trim() : '';
  }, [data?.stats]);

  async function handleLogDoses() {
    if (!currentProfileId || !data || busy) return;
    try {
      setBusy('doses');
      await apiClient.logProtocolDoses(currentProfileId, data.today.dateIso);
      const [active, today] = await Promise.all([
        apiClient.getProtocolPortalActive(currentProfileId),
        apiClient.getProtocolPortalSchedule(currentProfileId, data.today.dateIso),
      ]);
      setData((current) => current ? { ...current, stats: active.stats, today } : current);
      setToast('Today’s scheduled items were logged.');
    } catch {
      setToast('Today’s scheduled items could not be logged.');
    } finally {
      setBusy(null);
    }
  }

  async function handleSelectDay(dateIso: string) {
    if (!currentProfileId) return;
    try {
      setBusy('day');
      setSelectedDay(await apiClient.getProtocolPortalSchedule(currentProfileId, dateIso));
    } catch {
      setToast('That day’s schedule could not be loaded.');
    } finally {
      setBusy(null);
    }
  }

  async function handleShiftWeek(days: number) {
    const firstDay = data?.week[0]?.dateIso;
    if (!currentProfileId || !firstDay || busy) return;
    try {
      setBusy('week');
      const week = await apiClient.getProtocolPortalWeek(currentProfileId, shiftIsoDate(firstDay, days));
      setData((current) => current ? { ...current, week } : current);
    } catch {
      setToast('The adjacent week could not be loaded.');
    } finally {
      setBusy(null);
    }
  }

  async function handleSaveCareTeamNote(message: string) {
    if (!currentProfileId) return;
    try {
      await apiClient.sendCareTeamMessage(currentProfileId, message);
    } catch (cause) {
      setToast('The care-team note could not be saved.');
      throw cause;
    }
  }

  if (!currentProfileId) {
    return (
      <div className="w-full">
        <Header title="Your Protocol" />
        <EmptyState
          title="Create a profile to open your protocol"
          description="A profile keeps your compounds, schedules, check-ins, and protocol history together."
          action={{ label: 'Create profile', onClick: () => router.push('/profiles') }}
        />
      </div>
    );
  }

  if (loading) {
    return (
      <div className="w-full">
        <Header title="Your Protocol" />
        <div className="p-5 sm:p-8"><LoadingSkeleton /></div>
      </div>
    );
  }

  if (error || !data || !activePhase) {
    return (
      <div className="w-full">
        <Header title="Your Protocol" />
        <ErrorState message={error ?? 'Your protocol could not be loaded.'} onRetry={() => void loadPortal()} />
      </div>
    );
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
            <span className="hidden md:inline">Care Team Note</span>
          </button>
        }
      />

      <div className="mx-auto max-w-7xl space-y-8 p-5 sm:p-8">
        <ProtocolOverviewHero
          overview={data.overview}
          activePhase={activePhase}
          onSwitchPhase={setActivePhaseNumber}
        />

        <ProtocolStatGrid stats={data.stats} />

        <div className="space-y-6">
          <ProtocolTabBar active={activeTab} onChange={setActiveTab} />

          {activeTab === 'dashboard' && (
            <DashboardTab
              today={data.today}
              onViewCalendar={() => setActiveTab('calendar')}
              onViewLabs={() => setActiveTab('monitoring')}
              onLogDoses={() => void handleLogDoses()}
              onMessageCareTeam={() => setContactOpen(true)}
            />
          )}

          {activeTab === 'calendar' && (
            <TierGate requiredTier="operator" currentTier={tier}>
              <CalendarTab
                week={data.week}
                activePhase={activePhase}
                weekLabel={weekLabel}
                doseHeadline={doseHeadline}
                onSelectDay={(dateIso) => void handleSelectDay(dateIso)}
                onPreviousWeek={() => void handleShiftWeek(-7)}
                onNextWeek={() => void handleShiftWeek(7)}
              />
            </TierGate>
          )}

          {activeTab === 'diet' && (
            <TierGate requiredTier="operator" currentTier={tier}>
              <DietLifestyleTab diet={data.diet ?? EMPTY_DIET} />
            </TierGate>
          )}

          {activeTab === 'supplements' && <SupplementationTab supplements={data.supplements} />}

          {activeTab === 'monitoring' && (
            <TierGate requiredTier="commander" currentTier={tier}>
              <MonitoringLabsTab monitoring={data.monitoring ?? EMPTY_MONITORING} />
            </TierGate>
          )}

          {activeTab === 'progress' && (
            <TierGate requiredTier="operator" currentTier={tier}>
              <ProgressMilestonesTab milestones={data.milestones} />
            </TierGate>
          )}

          {activeTab === 'resources' && <ResourcesTab resources={data.resources} />}
        </div>
      </div>

      {selectedDay && <DayDetailModal day={selectedDay} onClose={() => setSelectedDay(null)} />}
      {contactOpen && (
        <ContactCareTeamModal
          onClose={() => setContactOpen(false)}
          onSubmit={handleSaveCareTeamNote}
        />
      )}
      {toast && <Toast message={toast} onDismiss={() => setToast(null)} />}
    </div>
  );
}
