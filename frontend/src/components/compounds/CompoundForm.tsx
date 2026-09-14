'use client';

import { apiClient } from '@/lib/api';
import { compoundGoalDisplay } from '@/lib/compoundGoalLabels';
import { CompoundRecord, KnowledgeEntry } from '@/lib/types';
import { useEffect, useId, useMemo, useState } from 'react';

interface CompoundFormProps {
  personId: string;
  onSubmit: (data: Omit<CompoundRecord, 'id'>) => Promise<void>;
  isLoading?: boolean;
}

export function CompoundForm({ personId, onSubmit, isLoading }: CompoundFormProps) {
  const formId = useId();
  const [knowledgeBase, setKnowledgeBase] = useState<KnowledgeEntry[]>([]);
  const [showAllCompounds, setShowAllCompounds] = useState(false);
  const [formData, setFormData] = useState({
    name: '',
    category: '',
    goal: '',
    source: '',
    pricePaid: '' as string | number,
    startDate: new Date().toISOString().split('T')[0],
    endDate: '',
    status: 'Active',
    notes: '',
    sourceType: 'Manual',
  });

  useEffect(() => {
    const fetchKnowledge = async () => {
      try {
        const compounds = await apiClient.getAllKnowledgeCompounds();
        setKnowledgeBase(compounds);
      } catch (err) {
        console.error('Failed to fetch knowledge base:', err);
      }
    };
    fetchKnowledge();
  }, []);

  const filteredGoals = useMemo(() => {
    if (!formData.category) return [];
    const goals = new Set<string>();
    knowledgeBase
      .filter(k => k.classification === formData.category)
      .forEach(k => k.benefits?.forEach(b => goals.add(b)));
    return Array.from(goals)
      .map(value => ({ value, ...compoundGoalDisplay(value) }))
      .sort((a, b) => a.label.localeCompare(b.label));
  }, [knowledgeBase, formData.category]);

  const categoryCompounds = useMemo(() => knowledgeBase.filter(k => k.classification === formData.category), [knowledgeBase, formData.category]);

  const goalCompounds = useMemo(() => {
    if (!formData.category) return [];
    return knowledgeBase.filter(k => {
      const matchCategory = k.classification === formData.category;
      const matchGoal = !formData.goal || k.benefits?.includes(formData.goal);
      return matchCategory && matchGoal;
    });
  }, [knowledgeBase, formData.category, formData.goal]);
  const filteredCompounds = showAllCompounds ? categoryCompounds : goalCompounds;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await onSubmit({
        ...formData,
        personId,
        pricePaid: formData.pricePaid ? Number(formData.pricePaid) : undefined,
        startDate: new Date(`${formData.startDate}T00:00:00Z`).toISOString(),
        endDate: formData.endDate ? new Date(`${formData.endDate}T00:00:00Z`).toISOString() : null,
      });
      
      setFormData({
        name: '',
        category: '',
        goal: '',
        source: '',
        pricePaid: '',
        startDate: new Date().toISOString().split('T')[0],
        endDate: '',
        status: 'Active',
        notes: '',
        sourceType: 'Manual',
      });
      setShowAllCompounds(false);
    } catch (err) {
      console.error('Form submission error:', err);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="space-y-4">
        {/* 1. Category */}
        <div>
          <label htmlFor={`${formId}-category`} className="block text-sm font-medium text-white/70 mb-2">1. Select a Category</label>
          <select
            id={`${formId}-category`}
            value={formData.category}
            onChange={(e) => {
              setFormData({ ...formData, category: e.target.value, goal: '', name: '' });
              setShowAllCompounds(false);
            }}
            required
            className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white focus:outline-none focus:border-emerald-500/50 transition-all font-medium"
          >
            <option value="">Select a category...</option>
            <option value="Peptide">Peptides</option>
            <option value="Supplement">Supplements</option>
            <option value="Pharmaceutical">Pharmaceuticals</option>
            <option value="Nutraceutical">Nutraceuticals</option>
            <option value="Coenzyme">Coenzymes</option>
            <option value="Other">Other</option>
          </select>
        </div>

        {/* 2. Goal */}
        <div>
          <label htmlFor={`${formId}-goal`} className="block text-sm font-medium text-white/70 mb-2">2. Select a Goal</label>
          <select
            id={`${formId}-goal`}
            aria-describedby={formData.goal && compoundGoalDisplay(formData.goal).context ? `${formId}-goal-context` : undefined}
            value={formData.goal}
            onChange={(e) => {
              setFormData({ ...formData, goal: e.target.value, name: '' });
              setShowAllCompounds(false);
            }}
            disabled={!formData.category}
            className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white disabled:opacity-50 focus:outline-none focus:border-emerald-500/50 transition-all font-medium"
          >
            <option value="">{formData.category ? 'Select a goal (Optional)' : 'Select category first'}</option>
            {filteredGoals.map(goal => (
              <option key={goal.value} value={goal.value}>{goal.label.charAt(0).toUpperCase() + goal.label.slice(1)}</option>
            ))}
          </select>
          {formData.goal && compoundGoalDisplay(formData.goal).context && (
            <p id={`${formId}-goal-context`} className="mt-2 break-words text-xs leading-relaxed text-white/55">
              {compoundGoalDisplay(formData.goal).context}
            </p>
          )}
        </div>

        {/* 3. Compound Selection */}
        <div>
          <label htmlFor={`${formId}-compound`} className="block text-sm font-medium text-white/70 mb-2">3. Select a Compound</label>
          <select
            id={`${formId}-compound`}
            aria-describedby={formData.goal ? `${formId}-compound-results` : undefined}
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            disabled={!formData.category}
            className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white disabled:opacity-50 focus:outline-none focus:border-emerald-500/50 transition-all font-medium"
          >
            <option value="">{formData.category ? 'Select a compound...' : 'Select category first'}</option>
            {filteredCompounds.map(c => (
              <option key={c.canonicalName} value={c.canonicalName}>{c.canonicalName}</option>
            ))}
          </select>
          {formData.goal && (
            <div className="mt-2 space-y-2 text-xs leading-relaxed">
              <p id={`${formId}-compound-results`} className="text-white/55" role="status">
                Showing {filteredCompounds.length} of {categoryCompounds.length} compounds in this category.
                {' '}Goal tags are not a complete list of compounds or evidence of effectiveness.
              </p>
              {goalCompounds.length < categoryCompounds.length && (
                <button
                  type="button"
                  className="text-emerald-400 underline underline-offset-4 hover:text-emerald-300 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-emerald-400"
                  onClick={() => {
                    setShowAllCompounds(!showAllCompounds);
                    if (showAllCompounds && !goalCompounds.some(c => c.canonicalName === formData.name)) {
                      setFormData({ ...formData, name: '' });
                    }
                  }}
                >
                  {showAllCompounds ? 'Show goal matches only' : 'Show all compounds in this category'}
                </button>
              )}
            </div>
          )}
        </div>

        {/* 4. Optional: Search / Manual Entry */}
        <div>
          <label htmlFor={`${formId}-manual-name`} className="block text-sm font-medium text-white/70 mb-2">4. Optional: Manual Search/Entry</label>
          <input
            id={`${formId}-manual-name`}
            type="text"
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            placeholder="Search or enter custom compound name..."
            className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/50 transition-all"
          />
          <p className="mt-1 text-[10px] text-white/30 italic px-1">Tip: Use this if you can't find your compound in the list above.</p>
        </div>

        {/* 5. Optional: Source and Price */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label htmlFor={`${formId}-source`} className="block text-sm font-medium text-white/70 mb-2">5a. Optional: Source</label>
            <input
              id={`${formId}-source`}
              type="text"
              value={formData.source}
              onChange={(e) => setFormData({ ...formData, source: e.target.value })}
              placeholder="e.g., BioStack Labs"
              className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/50 transition-all"
            />
          </div>
          <div>
            <label htmlFor={`${formId}-price-paid`} className="block text-sm font-medium text-white/70 mb-2">5b. Optional: Price Paid</label>
            <div className="relative">
              <span className="absolute left-4 top-1/2 -translate-y-1/2 text-white/30">$</span>
              <input
                id={`${formId}-price-paid`}
                type="number"
                step="0.01"
                value={formData.pricePaid}
                onChange={(e) => setFormData({ ...formData, pricePaid: e.target.value })}
                placeholder="0.00"
                className="w-full pl-8 pr-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/50 transition-all"
              />
            </div>
          </div>
        </div>
      </div>

      <div className="h-px bg-white/5 my-2" />

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label htmlFor={`${formId}-start-date`} className="block text-sm font-medium text-white/70 mb-2">Start Date</label>
          <input
            id={`${formId}-start-date`}
            type="date"
            value={formData.startDate}
            onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
            required
            className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white focus:outline-none focus:border-emerald-500/50 transition-all"
          />
        </div>
        <div>
          <label htmlFor={`${formId}-end-date`} className="block text-sm font-medium text-white/70 mb-2">End Date (Optional)</label>
          <input
            id={`${formId}-end-date`}
            type="date"
            value={formData.endDate}
            onChange={(e) => setFormData({ ...formData, endDate: e.target.value })}
            className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white focus:outline-none focus:border-emerald-500/50 transition-all"
          />
        </div>
      </div>

      <div>
        <label htmlFor={`${formId}-status`} className="block text-sm font-medium text-white/70 mb-2">Status</label>
        <select
          id={`${formId}-status`}
          value={formData.status}
          onChange={(e) => setFormData({ ...formData, status: e.target.value })}
          className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white focus:outline-none focus:border-emerald-500/50 transition-all font-medium"
        >
          <option value="Active">Active</option>
          <option value="Paused">Paused</option>
          <option value="Completed">Completed</option>
        </select>
      </div>

      <div>
        <label htmlFor={`${formId}-notes`} className="block text-sm font-medium text-white/70 mb-2">Notes</label>
        <textarea
          id={`${formId}-notes`}
          value={formData.notes}
          onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
          placeholder="Any additional notes..."
          rows={3}
          className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/50 transition-all"
        />
      </div>

      <button
        type="submit"
        disabled={isLoading}
        className="w-full px-5 py-4 bg-emerald-500 hover:bg-emerald-400 disabled:bg-white/10 disabled:text-white/30 text-slate-950 rounded-xl font-bold text-lg shadow-lg shadow-emerald-500/20 active:scale-[0.98] transition-all"
      >
        {isLoading ? 'Adding...' : 'Add Compound'}
      </button>
    </form>
  );
}
