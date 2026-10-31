'use client';

import { useEffect, useRef, useState } from 'react';
import { DOSE_UNITS } from '@/lib/userDose';
import type { ScheduleItem } from '@/lib/types';

interface DoseLogFormProps {
  item: ScheduleItem;
  disabled?: boolean;
  /** Resolves when the amount was logged; rejects (and keeps the inputs) when it was not. */
  onLog: (entry: { amount: number; unit: string; note: string }) => Promise<void>;
}

/**
 * Log the amount the user actually took. The amount and unit always start blank: BioStack never
 * suggests, prefills or computes a dose. The one shortcut is a button the user presses themselves that
 * copies the entry the user saved on this compound; nothing is copied unless they press it.
 */
export function DoseLogForm({ item, disabled, onLog }: DoseLogFormProps) {
  const [open, setOpen] = useState(false);
  const [amount, setAmount] = useState('');
  const [unit, setUnit] = useState('');
  const [note, setNote] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [announcement, setAnnouncement] = useState<string>('');
  const amountRef = useRef<HTMLInputElement>(null);
  const openButtonRef = useRef<HTMLButtonElement>(null);
  // Focus follows the disclosure: into the first field when the form opens, back to the opener when it closes
  // (otherwise focus is lost to <body> when the button unmounts - WCAG 2.4.3).
  const wasOpen = useRef(false);
  useEffect(() => {
    if (open) amountRef.current?.focus();
    else if (wasOpen.current) openButtonRef.current?.focus();
    wasOpen.current = open;
  }, [open]);

  const savedEntry =
    item.doseAmount != null && item.doseUnit ? { amount: item.doseAmount, unit: item.doseUnit } : null;

  function validate(): { amount: number; unit: string } | null {
    const parsed = Number(amount.trim());
    if (amount.trim() === '' || !Number.isFinite(parsed) || parsed <= 0) {
      setError('Enter the amount you took, as a number greater than zero.');
      return null;
    }
    if (!(DOSE_UNITS as readonly string[]).includes(unit)) {
      setError('Choose the unit.');
      return null;
    }
    setError(null);
    return { amount: parsed, unit };
  }

  async function submit() {
    const valid = validate();
    if (!valid) return;
    try {
      setSaving(true);
      await onLog({ ...valid, note: note.trim() });
      setAmount('');
      setUnit('');
      setNote('');
      setOpen(false);
      setAnnouncement(`Amount logged for ${item.name}.`);
    } catch (cause) {
      setError(cause instanceof Error && cause.message ? cause.message : 'That amount could not be logged.');
    } finally {
      setSaving(false);
    }
  }

  const idBase = `dose-log-${item.compoundId ?? item.name}`;
  const errorId = `${idBase}-error`;
  const live = <p role="status" aria-live="polite" className="sr-only">{announcement}</p>;

  if (!open) {
    return (
      <>
        <button
          ref={openButtonRef}
          type="button"
          disabled={disabled}
          onClick={() => { setAnnouncement(''); setOpen(true); }}
          className="mt-2 min-h-8 rounded-lg border border-white/[0.1] px-3 py-1.5 text-xs font-medium text-white/75 transition-colors hover:border-white/20 disabled:opacity-50"
        >
          Log amount taken <span className="sr-only">for {item.name}</span>
        </button>
        {live}
      </>
    );
  }

  return (
    <div className="mt-3 space-y-2 rounded-lg border border-white/[0.08] p-3" role="group" aria-label={`Log amount taken for ${item.name}`}>
      <div className="flex flex-wrap items-end gap-2">
        <div>
          <label htmlFor={`${idBase}-amount`} className="block text-xs font-medium text-white/65 mb-1">
            Amount taken
          </label>
          <input
            id={`${idBase}-amount`}
            ref={amountRef}
            type="number"
            inputMode="decimal"
            step="any"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            required
            aria-invalid={error ? true : undefined}
            aria-describedby={error ? errorId : undefined}
            className="w-28 rounded-lg border border-white/10 bg-[#0F141B] px-3 py-2 text-sm text-white focus:border-emerald-500/50 focus:outline-none"
          />
        </div>
        <div>
          <label htmlFor={`${idBase}-unit`} className="block text-xs font-medium text-white/65 mb-1">
            Unit taken
          </label>
          <select
            id={`${idBase}-unit`}
            value={unit}
            onChange={(e) => setUnit(e.target.value)}
            required
            aria-invalid={error ? true : undefined}
            aria-describedby={error ? errorId : undefined}
            className="rounded-lg border border-white/10 bg-[#0F141B] px-3 py-2 text-sm text-white focus:border-emerald-500/50 focus:outline-none"
          >
            <option value="">Unit</option>
            {DOSE_UNITS.map((option) => (
              <option key={option} value={option}>{option}</option>
            ))}
          </select>
        </div>
        {savedEntry && (
          <button
            type="button"
            onClick={() => {
              setAmount(String(savedEntry.amount));
              setUnit(savedEntry.unit);
              setError(null);
            }}
            className="rounded-lg border border-white/10 px-3 py-2 text-xs font-medium text-white/75 hover:border-white/20"
          >
            Same as my saved entry ({savedEntry.amount} {savedEntry.unit})
          </button>
        )}
      </div>
      <div>
        <label htmlFor={`${idBase}-note`} className="block text-xs font-medium text-white/65 mb-1">
          Note (optional)
        </label>
        <input
          id={`${idBase}-note`}
          type="text"
          maxLength={500}
          value={note}
          onChange={(e) => setNote(e.target.value)}
          className="w-full rounded-lg border border-white/10 bg-[#0F141B] px-3 py-2 text-sm text-white focus:border-emerald-500/50 focus:outline-none"
        />
      </div>
      {error && <p id={errorId} role="alert" className="text-xs text-red-300">{error}</p>}
      <div className="flex gap-2">
        <button
          type="button"
          disabled={saving || disabled}
          onClick={() => void submit()}
          className="rounded-lg bg-emerald-500 px-3 py-1.5 text-xs font-semibold text-slate-950 hover:bg-emerald-400 disabled:opacity-50"
        >
          Save amount
        </button>
        <button
          type="button"
          onClick={() => {
            setOpen(false);
            setError(null);
          }}
          className="rounded-lg border border-white/10 px-3 py-1.5 text-xs text-white/70 hover:border-white/20"
        >
          Cancel
        </button>
      </div>
    </div>
  );
}
