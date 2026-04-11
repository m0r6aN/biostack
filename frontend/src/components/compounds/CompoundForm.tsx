'use client';

import { apiClient } from '@/lib/api';
import { CompoundRecord, KnowledgeEntry } from '@/lib/types';
import { useEffect, useMemo, useState } from 'react';

interface CompoundFormProps {
  personId: string;
  onSubmit: (data: Omit<CompoundRecord, 'id'>) => Promise<void>;
  isLoading?: boolean;
}

export function CompoundForm({ personId, onSubmit, isLoading }: CompoundFormProps) {
  const [knowledgeBase, setKnowledgeBase] = useState<KnowledgeEntry[]>([]);
  const [formData, setFormData] = useState({
    name: '',
    category: '',
    goal: '',
    source: '',
    pricePaid: '' as string | number,
    startDate: new Date().toISOString().split('T')[0],
    endDate: '',
    status: 'Active' as const,
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
    return Array.from(goals).sort();
  }, [knowledgeBase, formData.category]);

  const filteredCompounds = useMemo(() => {
    if (!formData.category) return [];
    return knowledgeBase.filter(k => {
      const matchCategory = k.classification === formData.category;
      const matchGoal = !formData.goal || k.benefits?.includes(formData.goal);
      return matchCategory && matchGoal;
    });
  }, [knowledgeBase, formData.category, formData.goal]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await onSubmit({
        ...formData,
        personId,
        pricePaid: formData.pricePaid ? Number(formData.pricePaid) : undefined,
        endDate: formData.endDate || null,
      } as any);
      
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
    } catch (err) {
      console.error('Form submission error:', err);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="space-y-4">
        {/* 1. Category */}
        <div>
          <label className="block text-sm font-medium text-white/70 mb-2">1. Select a Category</label>
          <select
            value={formData.category}
            onChange={(e) => setFormData({ ...formData, category: e.target.value, goal: '', name: '' })}
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
          <label className="block text-sm font-medium text-white/70 mb-2">2. Select a Goal</label>
          <select
            value={formData.goal}
            onChange={(e) => setFormData({ ...formData, goal: e.target.value, name: '' })}
            disabled={!formData.category}
            className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white disabled:opacity-50 focus:outline-none focus:border-emerald-500/50 transition-all font-medium"
          >
            <option value="">{formData.category ? 'Select a goal (Optional)' : 'Select category first'}</option>
            {filteredGoals.map(goal => (
              <option key={goal} value={goal}>{goal.charAt(0).toUpperCase() + goal.slice(1)}</option>
            ))}
          </select>
        </div>

        {/* 3. Compound Selection */}
        <div>
          <label className="block text-sm font-medium text-white/70 mb-2">3. Select a Compound</label>
          <select
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
        </div>

        {/* 4. Optional: Search / Manual Entry */}
        <div>
          <label className="block text-sm font-medium text-white/70 mb-2">4. Optional: Manual Search/Entry</label>
          <input
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
            <label className="block text-sm font-medium text-white/70 mb-2">5a. Optional: Source</label>
            <input
              type="text"
              value={formData.source}
              onChange={(e) => setFormData({ ...formData, source: e.target.value })}
              placeholder="e.g., BioStack Labs"
              className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/50 transition-all"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-white/70 mb-2">5b. Optional: Price Paid</label>
            <div className="relative">
              <span className="absolute left-4 top-1/2 -translate-y-1/2 text-white/30">$</span>
              <input
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
          <label className="block text-sm font-medium text-white/70 mb-2">Start Date</label>
          <input
            type="date"
            value={formData.startDate}
            onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
            required
            className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white focus:outline-none focus:border-emerald-500/50 transition-all"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-white/70 mb-2">End Date (Optional)</label>
          <input
            type="date"
            value={formData.endDate}
            onChange={(e) => setFormData({ ...formData, endDate: e.target.value })}
            className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white focus:outline-none focus:border-emerald-500/50 transition-all"
          />
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium text-white/70 mb-2">Status</label>
        <select
          value={formData.status}
          onChange={(e) => setFormData({ ...formData, status: e.target.value as any })}
          className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white focus:outline-none focus:border-emerald-500/50 transition-all font-medium"
        >
          <option value="Active">Active</option>
          <option value="Paused">Paused</option>
          <option value="Completed">Completed</option>
        </select>
      </div>

      <div>
        <label className="block text-sm font-medium text-white/70 mb-2">Notes</label>
        <textarea
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
