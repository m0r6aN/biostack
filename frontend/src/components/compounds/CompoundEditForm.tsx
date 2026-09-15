'use client';

import { CompoundRecord } from '@/lib/types';
import { useId, useState } from 'react';

interface CompoundEditFormProps {
  compound: CompoundRecord;
  onSubmit: (data: Omit<CompoundRecord, 'id'>) => Promise<void>;
  onCancel: () => void;
  isLoading?: boolean;
}

const CATEGORY_OPTIONS = [
  { value: 'Unknown', label: 'Unknown' },
  { value: 'Compound', label: 'Compound' },
  { value: 'Sarm', label: 'SARM' },
  { value: 'Serm', label: 'SERM' },
  { value: 'Hormone', label: 'Hormone' },
  { value: 'Peptide', label: 'Peptides' },
  { value: 'Supplement', label: 'Supplements' },
  { value: 'Pharmaceutical', label: 'Pharmaceuticals' },
  { value: 'Nutraceutical', label: 'Nutraceuticals' },
  { value: 'Coenzyme', label: 'Coenzymes' },
  { value: 'Other', label: 'Other' },
];

function toDateInputValue(value: string | null | undefined): string {
  if (!value) return '';
  return value.slice(0, 10);
}

/**
 * A minimal, purpose-built editor for the fields the owner needs to fix on an
 * existing compound (most importantly: giving a nameless record a name).
 * Deliberately does not reuse the full CompoundForm goal/compound picker —
 * that wizard is for discovering a new compound, not correcting one already
 * on file, and keeping this separate avoids any overlap with in-flight work
 * on CompoundForm's goal-filter section.
 */
export function CompoundEditForm({ compound, onSubmit, onCancel, isLoading }: CompoundEditFormProps) {
  const formId = useId();
  const [name, setName] = useState(compound.name ?? '');
  const [category, setCategory] = useState(compound.category ?? '');
  const [notes, setNotes] = useState(compound.notes ?? '');
  const [startDate, setStartDate] = useState(toDateInputValue(compound.startDate));
  const [dateEdited, setDateEdited] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const handleKeyDown = (e: React.KeyboardEvent<HTMLFormElement>) => {
    if (e.key !== 'Enter') return;
    const target = e.target as HTMLElement;
    if (target.tagName === 'TEXTAREA') return;
    if (target instanceof HTMLButtonElement) return;
    e.preventDefault();
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const trimmedName = name.trim();
    if (!trimmedName) {
      setFormError('Enter a compound name before saving.');
      return;
    }
    if (!category) {
      setFormError('Select a category before saving.');
      return;
    }
    if (dateEdited && !startDate) {
      setFormError('Choose a start date before saving the date change.');
      return;
    }
    setFormError(null);
    try {
      await onSubmit({
        personId: compound.personId,
        name: trimmedName,
        category,
        goal: compound.goal,
        source: compound.source,
        pricePaid: compound.pricePaid,
        // Preserve the original timestamp (or API null) unless the user edits the date.
        startDate: dateEdited ? new Date(`${startDate}T00:00:00Z`).toISOString() : compound.startDate,
        endDate: compound.endDate,
        status: compound.status,
        notes,
        sourceType: compound.sourceType,
      });
    } catch (err) {
      console.error('Compound edit error:', err);
    }
  };

  return (
    <form onSubmit={handleSubmit} onKeyDown={handleKeyDown} aria-label="Edit compound" className="space-y-4">
      {formError && <p role="alert" className="text-sm text-red-300">{formError}</p>}
      <div>
        <label htmlFor={`${formId}-edit-name`} className="block text-sm font-medium text-white/70 mb-2">Compound name</label>
        <input
          id={`${formId}-edit-name`}
          type="text"
          value={name}
          onChange={(e) => {
            setName(e.target.value);
            if (formError) setFormError(null);
          }}
          required
          className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/50 transition-all"
        />
      </div>
      <div>
        <label htmlFor={`${formId}-edit-category`} className="block text-sm font-medium text-white/70 mb-2">Category</label>
        <select
          id={`${formId}-edit-category`}
          value={category}
          onChange={(e) => setCategory(e.target.value)}
          required
          className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white focus:outline-none focus:border-emerald-500/50 transition-all font-medium"
        >
          <option value="">Select a category...</option>
          {CATEGORY_OPTIONS.map(opt => (
            <option key={opt.value} value={opt.value}>{opt.label}</option>
          ))}
        </select>
      </div>
      <div>
        <label htmlFor={`${formId}-edit-start-date`} className="block text-sm font-medium text-white/70 mb-2">Start Date</label>
        <input
          id={`${formId}-edit-start-date`}
          type="date"
          value={startDate}
          onChange={(e) => { setStartDate(e.target.value); setDateEdited(true); }}
          required={dateEdited}
          className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white focus:outline-none focus:border-emerald-500/50 transition-all"
        />
      </div>
      <div>
        <label htmlFor={`${formId}-edit-notes`} className="block text-sm font-medium text-white/70 mb-2">Notes</label>
        <textarea
          id={`${formId}-edit-notes`}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows={3}
          className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/50 transition-all"
        />
      </div>
      <div className="flex gap-3">
        <button
          type="submit"
          disabled={isLoading}
          className="flex-1 px-4 py-3 bg-emerald-500 hover:bg-emerald-400 disabled:bg-white/10 disabled:text-white/30 text-slate-950 rounded-xl font-semibold transition-all"
        >
          {isLoading ? 'Saving...' : 'Save Changes'}
        </button>
        <button
          type="button"
          onClick={onCancel}
          disabled={isLoading}
          className="px-4 py-3 bg-white/5 hover:bg-white/10 disabled:opacity-50 text-white/70 rounded-xl font-medium transition-all"
        >
          Cancel
        </button>
      </div>
    </form>
  );
}
