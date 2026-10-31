'use client';

import type { ResourceEntry } from '@/lib/types';
import { GlassCard } from '@/components/ui/GlassCard';

export function ResourcesTab({ resources }: { resources: ResourceEntry[] }) {
  return (
    <GlassCard variant="base" className="p-6 sm:p-8">
      <h2 className="mb-4 text-2xl font-semibold text-white">Resources &amp; Additional Information</h2>

      <div className="space-y-6">
        {resources.map((resource) => (
          <div key={resource.heading}>
            <h3 className="font-semibold text-white">{resource.heading}</h3>
            <p className="mt-1 text-sm leading-6 text-white/55">{resource.body}</p>
          </div>
        ))}
      </div>
    </GlassCard>
  );
}
