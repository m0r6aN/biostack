'use client';

import { useEffect, useState } from 'react';
import { useProfile } from '@/lib/context';
import { apiClient } from '@/lib/api';
import { TimelineEvent } from '@/lib/types';
import { LoadingState, LoadingSkeleton } from '@/components/LoadingState';
import { ErrorState } from '@/components/ErrorState';
import { EmptyState } from '@/components/EmptyState';
import { TimelineEventCard } from '@/components/timeline/TimelineEventCard';
import { TimelineFilter } from '@/components/timeline/TimelineFilter';
import { Header } from '@/components/Header';

export default function TimelinePage() {
  const { currentProfileId } = useProfile();
  const [allEvents, setAllEvents] = useState<TimelineEvent[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeFilter, setActiveFilter] = useState('all');

  useEffect(() => {
    if (currentProfileId) {
      loadTimeline();
    }
  }, [currentProfileId]);

  const loadTimeline = async () => {
    try {
      setLoading(true);
      const data = await apiClient.getTimeline(currentProfileId!);
      setAllEvents(data);
    } catch (err) {
      setError('Failed to load timeline');
    } finally {
      setLoading(false);
    }
  };

  const filteredEvents = activeFilter === 'all'
    ? allEvents
    : allEvents.filter(e => e.eventType === activeFilter);

  const sortedEvents = [...filteredEvents].sort(
    (a, b) => new Date(b.occurredAtUtc).getTime() - new Date(a.occurredAtUtc).getTime()
  );

  if (!currentProfileId) {
    return (
      <div className="w-full">
        <Header title="Timeline" />
        <div className="p-8">
          <EmptyState
            title="No Profile Selected"
            description="Select a profile to view timeline"
            icon="📅"
          />
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="w-full">
        <Header title="Timeline" />
        <div className="p-8">
          <ErrorState message={error} onRetry={loadTimeline} />
        </div>
      </div>
    );
  }

  return (
    <div className="w-full">
      <Header title="Timeline" subtitle="Unified event stream" />

      <div className="p-8 max-w-4xl">
        {loading ? (
          <LoadingSkeleton />
        ) : (
          <>
            <div className="mb-6">
              <TimelineFilter
                activeFilter={activeFilter}
                onFilterChange={setActiveFilter}
              />
            </div>

            {sortedEvents.length === 0 ? (
              <EmptyState
                title="No Events"
                description={activeFilter === 'all' ? 'No timeline events yet' : 'No events matching this filter'}
                icon="📅"
              />
            ) : (
              <div className="pb-6">
                {sortedEvents.map((event) => (
                  <TimelineEventCard key={event.id} event={event} />
                ))}
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
