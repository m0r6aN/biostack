'use client';

import { WeightUnitToggle } from '@/components/ui/WeightUnitToggle';
import { useSettings } from '@/lib/settings';
import { CreateCheckInRequest, GoalDefinition } from '@/lib/types';
import { lbsToKg } from '@/lib/utils';
import { useMemo, useState } from 'react';

interface CheckInFormProps {
  personId: string;
  profileGoals?: GoalDefinition[];
  onSubmit: (data: CreateCheckInRequest) => Promise<void>;
  isLoading?: boolean;
}

function RatingSlider({ label, value, onChange, invert = false }: { label: string; value: number; onChange: (v: number) => void, invert?: boolean }) {
  // invert = true means lower is better (e.g. pain)
  const isGood = invert ? value <= 3 : value >= 7;
  const isBad = invert ? value >= 7 : value <= 3;
  
  return (
    <div className="space-y-3 group">
      <div className="flex items-center justify-between">
        <label className="text-xs font-black text-white/50 uppercase tracking-widest">{label}</label>
        <span className={`text-sm font-black tabular-nums transition-colors ${isGood ? 'text-emerald-400' : isBad ? 'text-red-400' : 'text-amber-400'}`}>
          {value}/10
        </span>
      </div>
      <div className="relative h-2 flex items-center">
        {/* Progress Fill Background (Subtle) */}
        <div 
          className="absolute h-full bg-emerald-500/10 rounded-full transition-all duration-300 pointer-events-none"
          style={{ width: `${value * 10}%` }}
        />
        <input
          type="range"
          min="0"
          max="10"
          value={value}
          onChange={(e) => onChange(parseInt(e.target.value))}
          className="w-full h-full bg-white/10 border border-white/10 rounded-full appearance-none cursor-pointer accent-emerald-500 hover:bg-white/20 transition-all"
        />
      </div>
    </div>
  );
}

export function CheckInForm({ personId, profileGoals = [], onSubmit, isLoading }: CheckInFormProps) {
  const { settings } = useSettings();
  const [formData, setFormData] = useState({
    date: new Date().toISOString().split('T')[0],
    weight: 0,
    sleepQuality: 7,
    energy: 7,
    appetite: 5,
    recovery: 7,
    focus: 7,
    thoughtClarity: 7,
    skinQuality: 7,
    digestiveHealth: 7,
    strength: 5,
    endurance: 5,
    jointPain: 0,
    eyesight: 7,
    sideEffects: '',
    photoUrls: [] as string[],
    giSymptoms: 'none',
    mood: 'neutral',
    notes: '',
  });

  const [newPhotoUrl, setNewPhotoUrl] = useState('');

  const activeCategories = useMemo(() => {
    return new Set(profileGoals.map(g => g.category));
  }, [profileGoals]);

  const showMetric = (category: string) => activeCategories.has(category) || activeCategories.size === 0;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const weightKg = settings.weightUnit === 'imperial'
        ? lbsToKg(formData.weight)
        : formData.weight;

      const submissionData: CreateCheckInRequest = {
        ...formData,
        weight: weightKg,
        // Only submit relevant categories to keep data clean
        focus: showMetric('cognitive') ? formData.focus : undefined,
        thoughtClarity: showMetric('cognitive') ? formData.thoughtClarity : undefined,
        skinQuality: showMetric('skin') || showMetric('longevity') ? formData.skinQuality : undefined,
        digestiveHealth: showMetric('organ') ? formData.digestiveHealth : undefined,
        strength: showMetric('performance') ? formData.strength : undefined,
        endurance: showMetric('performance') ? formData.endurance : undefined,
        jointPain: showMetric('recovery') ? formData.jointPain : undefined,
        eyesight: showMetric('longevity') ? formData.eyesight : undefined,
      };

      await onSubmit(submissionData);
      
      setFormData({
        date: new Date().toISOString().split('T')[0],
        weight: 0,
        sleepQuality: 7,
        energy: 7,
        appetite: 5,
        recovery: 7,
        focus: 7,
        thoughtClarity: 7,
        skinQuality: 7,
        digestiveHealth: 7,
        strength: 5,
        endurance: 5,
        jointPain: 0,
        eyesight: 7,
        sideEffects: '',
        photoUrls: [],
        giSymptoms: 'none',
        mood: 'neutral',
        notes: '',
      });
    } catch (err) {
      console.error('Form submission error:', err);
    }
  };

  const addPhotoUrl = () => {
    if (newPhotoUrl && !formData.photoUrls.includes(newPhotoUrl)) {
      setFormData({ ...formData, photoUrls: [...formData.photoUrls, newPhotoUrl] });
      setNewPhotoUrl('');
    }
  };

  const removePhotoUrl = (url: string) => {
    setFormData({ ...formData, photoUrls: formData.photoUrls.filter(u => u !== url) });
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500">
      {/* Core Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 p-6 rounded-2xl bg-white/[0.02] border border-white/[0.05]">
        <div className="space-y-4">
          <h3 className="text-sm font-semibold text-white/50 uppercase tracking-wider mb-4">Basics</h3>
          <div>
            <label className="block text-xs font-medium text-white/40 mb-2 uppercase">Date</label>
            <input
              type="date"
              value={formData.date}
              onChange={(e) => setFormData({ ...formData, date: e.target.value })}
              required
              className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white focus:outline-none focus:border-emerald-500/50 focus:shadow-[0_0_0_1px_rgba(34,197,94,0.1)] transition-all"
            />
          </div>

          <div>
            <div className="flex items-center justify-between mb-2">
              <label className="text-xs font-medium text-white/40 uppercase">Weight</label>
              <WeightUnitToggle />
            </div>
            <input
              type="number"
              step="any"
              value={formData.weight || ''}
              onChange={(e) => { const v = parseFloat(e.target.value); setFormData({ ...formData, weight: isNaN(v) ? 0 : v }); }}
              required
              placeholder={settings.weightUnit === 'imperial' ? 'e.g. 170' : 'e.g. 77'}
              className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/20 focus:outline-none focus:border-emerald-500/50 transition-all font-mono"
            />
          </div>
        </div>

        <div className="space-y-4">
          <h3 className="text-sm font-semibold text-white/50 uppercase tracking-wider mb-4">Vitality</h3>
          <RatingSlider label="Energy" value={formData.energy} onChange={(v) => setFormData({ ...formData, energy: v })} />
          <RatingSlider label="Sleep" value={formData.sleepQuality} onChange={(v) => setFormData({ ...formData, sleepQuality: v })} />
          <RatingSlider label="Appetite" value={formData.appetite} onChange={(v) => setFormData({ ...formData, appetite: v })} />
          <RatingSlider label="Recovery" value={formData.recovery} onChange={(v) => setFormData({ ...formData, recovery: v })} />
        </div>
      </div>

      {/* Goal-Specific Metrics */}
      <div className="p-6 rounded-2xl bg-white/[0.02] border border-white/[0.05]">
        <h3 className="text-sm font-semibold text-emerald-400 uppercase tracking-wider mb-6 flex items-center gap-2">
          <div className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
          Goal-Specific Metrics
        </h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
          {showMetric('cognitive') && (
            <>
              <RatingSlider label="Mental Focus" value={formData.focus} onChange={(v) => setFormData({ ...formData, focus: v })} />
              <RatingSlider label="Thought Clarity" value={formData.thoughtClarity} onChange={(v) => setFormData({ ...formData, thoughtClarity: v })} />
            </>
          )}
          {(showMetric('skin') || showMetric('longevity')) && (
            <RatingSlider label="Skin Quality" value={formData.skinQuality} onChange={(v) => setFormData({ ...formData, skinQuality: v })} />
          )}
          {showMetric('organ') && (
            <RatingSlider label="Digestive Health" value={formData.digestiveHealth} onChange={(v) => setFormData({ ...formData, digestiveHealth: v })} />
          )}
          {showMetric('performance') && (
            <>
              <RatingSlider label="Strength Output" value={formData.strength} onChange={(v) => setFormData({ ...formData, strength: v })} />
              <RatingSlider label="Endurance" value={formData.endurance} onChange={(v) => setFormData({ ...formData, endurance: v })} />
            </>
          )}
          {showMetric('recovery') && (
            <RatingSlider label="Joint/Muscle Pain" value={formData.jointPain} onChange={(v) => setFormData({ ...formData, jointPain: v })} invert />
          )}
          {showMetric('longevity') && (
            <RatingSlider label="Visual Clarity" value={formData.eyesight} onChange={(v) => setFormData({ ...formData, eyesight: v })} />
          )}
        </div>
      </div>

      {/* Subjective & Qualitative */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-8 p-6 rounded-2xl bg-white/[0.02] border border-white/[0.05]">
        <div className="space-y-6">
          <div>
            <label className="block text-xs font-medium text-white/40 mb-2 uppercase">Overall Mood</label>
            <div className="grid grid-cols-2 gap-2">
              {['negative', 'neutral', 'positive', 'excellent'].map((m) => (
                <button
                  key={m}
                  type="button"
                  onClick={() => setFormData({ ...formData, mood: m })}
                  className={`px-3 py-2 text-xs rounded-xl border transition-all capitalize ${
                    formData.mood === m
                      ? 'bg-emerald-500/10 border-emerald-500/50 text-emerald-400'
                      : 'bg-white/5 border-white/10 text-white/40 hover:text-white/60 hover:border-white/20'
                  }`}
                >
                  {m}
                </button>
              ))}
            </div>
          </div>
          <div>
            <label className="block text-xs font-medium text-white/40 mb-2 uppercase">Side Effects / Issues</label>
            <textarea
              value={formData.sideEffects}
              onChange={(e) => setFormData({ ...formData, sideEffects: e.target.value })}
              placeholder="Report any negative reactions..."
              rows={3}
              className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/10 focus:outline-none focus:border-red-500/30 transition-all text-sm"
            />
          </div>
        </div>

        <div>
          <label className="block text-xs font-medium text-white/40 mb-2 uppercase">Narrative Notes</label>
          <textarea
            value={formData.notes}
            onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
            placeholder="Tell us more about how you feel today..."
            rows={7}
            className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/10 focus:outline-none focus:border-emerald-500/50 transition-all text-sm"
          />
        </div>
      </div>

      {/* Visual Tracking */}
      <div className="p-6 rounded-2xl bg-white/[0.02] border border-white/[0.05]">
        <label className="block text-xs font-medium text-white/40 mb-4 uppercase flex items-center gap-2">
          Visual Progress
        </label>
        
        <div className="flex gap-2 mb-6">
          <input
            type="text"
            value={newPhotoUrl}
            onChange={(e) => setNewPhotoUrl(e.target.value)}
            placeholder="Paste evidence image URL..."
            className="flex-1 px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/10 focus:outline-none focus:border-emerald-500/50 transition-all text-sm"
          />
          <button
            type="button"
            onClick={addPhotoUrl}
            className="px-6 py-3 bg-white/5 hover:bg-emerald-500 hover:text-slate-950 text-white rounded-xl font-bold transition-all text-sm"
          >
            Add
          </button>
        </div>

        <div className="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-6 gap-4">
          {formData.photoUrls.map((url, i) => (
            <div key={i} className="group relative aspect-square rounded-2xl overflow-hidden bg-white/5 border border-white/10 shadow-2xl">
              <img src={url} alt={`Evidence ${i}`} className="w-full h-full object-cover transition-transform duration-500 group-hover:scale-110" />
              <div className="absolute inset-0 bg-gradient-to-t from-black/60 to-transparent opacity-0 group-hover:opacity-100 transition-opacity" />
              <button
                type="button"
                onClick={() => removePhotoUrl(url)}
                className="absolute top-2 right-2 p-2 bg-red-500 text-white rounded-full opacity-0 group-hover:opacity-100 transition-all hover:scale-110 shadow-lg"
              >
                <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M6 18L18 6M6 6l12 12" /></svg>
              </button>
            </div>
          ))}
        </div>
      </div>

      <button
        type="submit"
        disabled={isLoading}
        className="w-full px-5 py-5 bg-emerald-500 hover:bg-emerald-400 disabled:bg-white/5 disabled:text-white/10 text-slate-950 rounded-2xl font-black text-xl shadow-[0_12px_40px_rgb(16,185,129,0.25)] hover:shadow-[0_12px_40px_rgb(16,185,129,0.35)] hover:-translate-y-0.5 active:translate-y-0 transition-all group"
      >
        {isLoading ? (
          <div className="flex items-center justify-center gap-3">
            <div className="w-5 h-5 border-2 border-slate-950 border-t-transparent rounded-full animate-spin" />
            Synchronizing...
          </div>
        ) : (
          <div className="flex items-center justify-center gap-2">
            Commit Daily Record
            <svg className="w-6 h-6 transition-transform group-hover:translate-x-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7l5 5m0 0l-5 5m5-5H6" />
            </svg>
          </div>
        )}
      </button>
    </form>
  );
}
