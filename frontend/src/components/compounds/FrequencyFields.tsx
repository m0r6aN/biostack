'use client';

import {
  FREQUENCY_KINDS,
  MAX_TIMES_OF_DAY,
  WEEKDAYS,
  type FrequencyFormErrors,
  type FrequencyFormValues,
} from '@/lib/userFrequency';

interface FrequencyFieldsProps {
  idPrefix: string;
  values: FrequencyFormValues;
  errors?: FrequencyFormErrors;
  onChange: (next: FrequencyFormValues) => void;
  /** The user's older free-text frequency, shown as a prompt to pick a structured one. */
  legacyText?: string | null;
}

const INPUT_CLASS =
  'w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/50 transition-all';

/**
 * How often the user takes the compound, picked by the user. The choice starts blank and is never
 * suggested, defaulted or inferred from free text: BioStack does not choose a schedule for anyone.
 */
export function FrequencyFields({ idPrefix, values, errors = {}, onChange, legacyText }: FrequencyFieldsProps) {
  const set = (patch: Partial<FrequencyFormValues>) => onChange({ ...values, ...patch });
  const kind = values.kind;
  const showStartAndTimes = kind !== '' && kind !== 'as_needed';

  const toggleWeekday = (day: number) =>
    set({ weekdays: values.weekdays.includes(day) ? values.weekdays.filter((d) => d !== day) : [...values.weekdays, day] });

  return (
    <fieldset className="space-y-3 rounded-xl border border-white/[0.08] p-4">
      <legend className="px-2 text-sm font-medium text-white/70">Your schedule (optional)</legend>

      {legacyText && !kind && (
        <p
          role="status"
          className="rounded-lg border border-amber-300/20 bg-amber-400/[0.06] p-3 text-xs leading-relaxed text-amber-100/85"
        >
          You described this as “{legacyText}”. Pick how often below so your schedule shows it only on the days it is
          due. Until you do, it keeps showing every day.
        </p>
      )}

      <div>
        <label htmlFor={`${idPrefix}-frequency-kind`} className="block text-sm font-medium text-white/70 mb-2">
          How often
        </label>
        <select
          id={`${idPrefix}-frequency-kind`}
          value={kind}
          onChange={(e) => set({ kind: e.target.value })}
          aria-invalid={errors.kind ? true : undefined}
          className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white focus:outline-none focus:border-emerald-500/50 transition-all font-medium"
        >
          <option value="">Choose how often</option>
          {FREQUENCY_KINDS.map((option) => (
            <option key={option.value} value={option.value}>{option.label}</option>
          ))}
        </select>
        {errors.kind && <p role="alert" className="mt-2 text-xs text-red-300">{errors.kind}</p>}
      </div>

      {kind === 'every_n_days' && (
        <div>
          <label htmlFor={`${idPrefix}-frequency-interval`} className="block text-sm font-medium text-white/70 mb-2">
            Every how many days
          </label>
          <input
            id={`${idPrefix}-frequency-interval`}
            type="number"
            inputMode="numeric"
            step={1}
            value={values.intervalDays}
            onChange={(e) => set({ intervalDays: e.target.value })}
            aria-invalid={errors.intervalDays ? true : undefined}
            className={INPUT_CLASS}
          />
          {errors.intervalDays && <p role="alert" className="mt-2 text-xs text-red-300">{errors.intervalDays}</p>}
        </div>
      )}

      {kind === 'weekdays' && (
        <div role="group" aria-labelledby={`${idPrefix}-frequency-weekdays-label`}>
          <p id={`${idPrefix}-frequency-weekdays-label`} className="block text-sm font-medium text-white/70 mb-2">
            Days of the week
          </p>
          <div className="flex flex-wrap gap-2">
            {WEEKDAYS.map((day) => {
              const checked = values.weekdays.includes(day.value);
              return (
                <label
                  key={day.value}
                  className={`flex cursor-pointer items-center gap-2 rounded-lg border px-3 py-2 text-sm ${
                    checked ? 'border-emerald-400/60 bg-emerald-400/10 text-white' : 'border-white/10 text-white/70'
                  }`}
                >
                  <input
                    type="checkbox"
                    checked={checked}
                    onChange={() => toggleWeekday(day.value)}
                    aria-label={day.long}
                    className="h-4 w-4 accent-emerald-400"
                  />
                  <span aria-hidden="true">{day.short}</span>
                </label>
              );
            })}
          </div>
          {errors.weekdays && <p role="alert" className="mt-2 text-xs text-red-300">{errors.weekdays}</p>}
        </div>
      )}

      {kind === 'times_per_week' && (
        <div>
          <label htmlFor={`${idPrefix}-frequency-per-week`} className="block text-sm font-medium text-white/70 mb-2">
            Times per week
          </label>
          <input
            id={`${idPrefix}-frequency-per-week`}
            type="number"
            inputMode="numeric"
            step={1}
            value={values.timesPerWeek}
            onChange={(e) => set({ timesPerWeek: e.target.value })}
            aria-invalid={errors.timesPerWeek ? true : undefined}
            className={INPUT_CLASS}
          />
          <p className="mt-2 text-xs text-white/55">
            This is shown as a weekly target, not on fixed days. Choose “Specific weekdays” to pick the days.
          </p>
          {errors.timesPerWeek && <p role="alert" className="mt-2 text-xs text-red-300">{errors.timesPerWeek}</p>}
        </div>
      )}

      {showStartAndTimes && (
        <>
          <div>
            <label htmlFor={`${idPrefix}-frequency-start`} className="block text-sm font-medium text-white/70 mb-2">
              Schedule start date
            </label>
            <input
              id={`${idPrefix}-frequency-start`}
              type="date"
              value={values.startDate}
              onChange={(e) => set({ startDate: e.target.value })}
              aria-invalid={errors.startDate ? true : undefined}
              className={INPUT_CLASS}
            />
            {errors.startDate && <p role="alert" className="mt-2 text-xs text-red-300">{errors.startDate}</p>}
          </div>

          <div role="group" aria-labelledby={`${idPrefix}-frequency-times-label`}>
            <p id={`${idPrefix}-frequency-times-label`} className="block text-sm font-medium text-white/70 mb-2">
              Times of day
            </p>
            <div className="space-y-2">
              {values.times.map((time, index) => (
                <div key={index} className="flex items-center gap-2">
                  <input
                    type="time"
                    aria-label={`Time of day ${index + 1}`}
                    value={time}
                    onChange={(e) => set({ times: values.times.map((t, i) => (i === index ? e.target.value : t)) })}
                    className={INPUT_CLASS}
                  />
                  <button
                    type="button"
                    onClick={() => set({ times: values.times.filter((_, i) => i !== index) })}
                    aria-label={`Remove time of day ${index + 1}`}
                    className="rounded-lg border border-white/10 px-3 py-2 text-xs text-white/70 hover:border-white/20"
                  >
                    Remove
                  </button>
                </div>
              ))}
            </div>
            {values.times.length < MAX_TIMES_OF_DAY && (
              <button
                type="button"
                onClick={() => set({ times: [...values.times, ''] })}
                className="mt-2 rounded-lg border border-white/10 px-3 py-2 text-xs font-medium text-white/70 hover:border-white/20"
              >
                Add a time of day
              </button>
            )}
            {errors.times && <p role="alert" className="mt-2 text-xs text-red-300">{errors.times}</p>}
          </div>
        </>
      )}
    </fieldset>
  );
}
