'use client';

import { GoalPicker } from '@/components/goals/GoalPicker';
import { WeightUnitToggle } from '@/components/ui/WeightUnitToggle';
import { useSettings } from '@/lib/settings';
import { CreateProfileRequest, PersonProfile } from '@/lib/types';
import { kgToLbs, lbsToKg } from '@/lib/utils';
import { useState } from 'react';

interface ProfileFormProps {
  initialData?: PersonProfile;
  onSubmit: (data: CreateProfileRequest & { selectedGoalIds?: string[] }) => Promise<void>;
  onCancel: () => void;
  isSubmitting?: boolean;
}

export function ProfileForm({ initialData, onSubmit, onCancel, isSubmitting }: ProfileFormProps) {
  const { settings } = useSettings();
  const [formData, setFormData] = useState({
    displayName: initialData?.displayName || '',
    sex: (initialData?.sex as any) || 'Male',
    age: initialData?.age,
    weight: initialData?.weight || 70,
    notes: initialData?.notes || '',
  });
  const [selectedGoalIds, setSelectedGoalIds] = useState<string[]>([]); // Note: We'd need to load existing goals if editing
  const [goalSummary, setGoalSummary] = useState(initialData?.goalSummary || '');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onSubmit({
      ...formData,
      goalSummary: goalSummary || undefined,
      selectedGoalIds: selectedGoalIds.length > 0 ? selectedGoalIds : undefined,
    });
  };

  return (
    <form onSubmit={handleSubmit} className="p-6 rounded-2xl border border-white/[0.08] bg-[#121923]/90 shadow-[0_8px_24px_rgba(0,0,0,0.35)] space-y-4">
      <div className="flex items-center justify-between mb-2">
        <h3 className="text-lg font-semibold text-white">
          {initialData ? 'Edit Profile' : 'New Profile'}
        </h3>
        <button
          type="button"
          onClick={onCancel}
          className="text-sm text-white/35 hover:text-white/60 transition-colors"
        >
          Cancel
        </button>
      </div>

      <div>
        <label className="block text-sm font-medium text-white/70 mb-2">Display Name</label>
        <input
          type="text"
          value={formData.displayName}
          onChange={(e) => setFormData({ ...formData, displayName: e.target.value })}
          required
          className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/50 focus:shadow-[0_0_0_1px_rgba(34,197,94,0.2)] transition-all"
        />
      </div>

      <div className="grid grid-cols-3 gap-4">
        <div>
          <label className="block text-sm font-medium text-white/70 mb-2">Sex</label>
          <select
            value={formData.sex}
            onChange={(e) => setFormData({ ...formData, sex: e.target.value as any })}
            className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/50 focus:shadow-[0_0_0_1px_rgba(34,197,94,0.2)] transition-all"
          >
            <option value="Male">Male</option>
            <option value="Female">Female</option>
            <option value="Other">Other</option>
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-white/70 mb-2">Age</label>
          <input
            type="number"
            value={formData.age || ''}
            onChange={(e) => setFormData({ ...formData, age: parseInt(e.target.value) || undefined })}
            placeholder="Years"
            className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/50 focus:shadow-[0_0_0_1px_rgba(34,197,94,0.2)] transition-all"
          />
        </div>
        <div>
          <div className="flex items-center justify-between mb-2">
            <label className="text-sm font-medium text-white/70">Weight</label>
            <WeightUnitToggle />
          </div>
          <input
            type="number"
            step="any"
            value={settings.weightUnit === 'imperial' ? kgToLbs(formData.weight) : formData.weight || ''}
            onChange={(e) => {
              const v = parseFloat(e.target.value);
              setFormData({ ...formData, weight: settings.weightUnit === 'imperial' ? lbsToKg(isNaN(v) ? 0 : v) : (isNaN(v) ? 0 : v) });
            }}
            className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/50 focus:shadow-[0_0_0_1px_rgba(34,197,94,0.2)] transition-all"
          />
        </div>
      </div>

      <GoalPicker
        selectedGoalIds={selectedGoalIds}
        onChange={setSelectedGoalIds}
        customGoalNote={goalSummary}
        onCustomGoalNoteChange={setGoalSummary}
      />

      <div>
        <label className="block text-sm font-medium text-white/70 mb-2">Notes</label>
        <textarea
          value={formData.notes}
          onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
          placeholder="Any additional notes..."
          rows={2}
          className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/50 focus:shadow-[0_0_0_1px_rgba(34,197,94,0.2)] transition-all"
        />
      </div>

      <button
        type="submit"
        disabled={isSubmitting}
        className="w-full px-5 py-3 bg-emerald-500 hover:bg-emerald-400 disabled:bg-emerald-500/50 disabled:cursor-not-allowed text-slate-950 rounded-xl font-medium transition-all"
      >
        {isSubmitting ? 'Saving...' : initialData ? 'Update Profile' : 'Create Profile'}
      </button>
    </form>
  );
}
