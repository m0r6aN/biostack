import { cn } from '@/lib/utils';

interface SafetyHierarchyProps {
  deterministic: React.ReactNode;
  commentary?: React.ReactNode;
  className?: string;
}

/**
 * Enforces the doctrine that deterministic safety findings always render
 * before AI/derived commentary. Commentary is visually subordinated.
 * CC-2: never let commentary precede deterministic content in the same parent.
 */
export function SafetyHierarchy({ deterministic, commentary, className }: SafetyHierarchyProps) {
  return (
    <div className={cn('space-y-4', className)}>
      {/* Slot 1: Deterministic — always first */}
      <div role="region" aria-label="Deterministic findings">{deterministic}</div>

      {/* Slot 2: Commentary — always second, visually subordinated */}
      {commentary && (
        <div
          role="region"
          aria-label="Intelligence commentary"
          className="border-t border-white/5 pt-4"
        >
          <p className="text-[9px] font-bold text-white/20 uppercase tracking-widest mb-3">
            Intelligence Commentary · Observational only
          </p>
          {commentary}
        </div>
      )}
    </div>
  );
}
