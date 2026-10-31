'use client';

import {
  DOSE_FREQUENCY_MAX_LENGTH,
  DOSE_TIME_MAX_LENGTH,
  DOSE_UNITS,
  type DoseFormErrors,
  type DoseFormValues,
} from '@/lib/userDose';

interface DoseFieldsProps {
  idPrefix: string;
  values: DoseFormValues;
  errors?: DoseFormErrors;
  onChange: (next: DoseFormValues) => void;
}

const INPUT_CLASS =
  'w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/50 focus:outline-none focus:border-emerald-500/50 transition-all';

/**
 * The dose the user takes, in their own words. Every input starts blank and is never prefilled from
 * the calculator, the library or any default: BioStack does not suggest, compute or check doses.
 */
export function DoseFields({ idPrefix, values, errors = {}, onChange }: DoseFieldsProps) {
  const set = (patch: Partial<DoseFormValues>) => onChange({ ...values, ...patch });

  return (
    <fieldset className="space-y-3 rounded-xl border border-white/[0.08] p-4">
      <legend className="px-2 text-sm font-medium text-white/70">Your dose (optional)</legend>
      <p id={`${idPrefix}-dose-help`} className="text-xs leading-relaxed text-white/55">
        Enter what you take, in your own words. BioStack never suggests, fills in or checks a dose.
      </p>
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label htmlFor={`${idPrefix}-dose-amount`} className="block text-sm font-medium text-white/70 mb-2">
            Dose amount
          </label>
          <input
            id={`${idPrefix}-dose-amount`}
            type="number"
            inputMode="decimal"
            step="any"
            value={values.amount}
            onChange={(e) => set({ amount: e.target.value })}
            aria-describedby={`${idPrefix}-dose-help`}
            aria-invalid={errors.amount ? true : undefined}
            className={INPUT_CLASS}
          />
          {errors.amount && <p role="alert" className="mt-2 text-xs text-red-300">{errors.amount}</p>}
        </div>
        <div>
          <label htmlFor={`${idPrefix}-dose-unit`} className="block text-sm font-medium text-white/70 mb-2">
            Dose unit
          </label>
          <select
            id={`${idPrefix}-dose-unit`}
            value={values.unit}
            onChange={(e) => set({ unit: e.target.value })}
            aria-invalid={errors.unit ? true : undefined}
            className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white focus:outline-none focus:border-emerald-500/50 transition-all font-medium"
          >
            <option value="">Unit</option>
            {DOSE_UNITS.map((unit) => (
              <option key={unit} value={unit}>{unit}</option>
            ))}
          </select>
          {errors.unit && <p role="alert" className="mt-2 text-xs text-red-300">{errors.unit}</p>}
        </div>
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label htmlFor={`${idPrefix}-dose-frequency`} className="block text-sm font-medium text-white/70 mb-2">
            Dose frequency
          </label>
          <input
            id={`${idPrefix}-dose-frequency`}
            type="text"
            maxLength={DOSE_FREQUENCY_MAX_LENGTH}
            value={values.frequency}
            onChange={(e) => set({ frequency: e.target.value })}
            aria-invalid={errors.frequency ? true : undefined}
            className={INPUT_CLASS}
          />
          {errors.frequency && <p role="alert" className="mt-2 text-xs text-red-300">{errors.frequency}</p>}
        </div>
        <div>
          <label htmlFor={`${idPrefix}-dose-time`} className="block text-sm font-medium text-white/70 mb-2">
            Dose time or schedule
          </label>
          <input
            id={`${idPrefix}-dose-time`}
            type="text"
            maxLength={DOSE_TIME_MAX_LENGTH}
            value={values.time}
            onChange={(e) => set({ time: e.target.value })}
            aria-invalid={errors.time ? true : undefined}
            className={INPUT_CLASS}
          />
          {errors.time && <p role="alert" className="mt-2 text-xs text-red-300">{errors.time}</p>}
        </div>
      </div>
    </fieldset>
  );
}
