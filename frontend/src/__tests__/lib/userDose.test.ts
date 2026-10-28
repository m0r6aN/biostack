import {
  DOSE_UNITS,
  EMPTY_DOSE_FORM,
  doseFormFromCompound,
  doseFormToPayload,
  formatUserDose,
  formatUserDoseWithTime,
  validateDoseForm,
} from '@/lib/userDose';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';

describe('user-entered dose helpers', () => {
  it('offers exactly the agreed units', () => {
    expect([...DOSE_UNITS]).toEqual(['mcg', 'mg', 'g', 'IU', 'mL', 'units']);
  });

  it('starts blank and a blank form is valid and produces an all-null payload', () => {
    expect(EMPTY_DOSE_FORM).toEqual({ amount: '', unit: '', frequency: '', time: '' });
    expect(validateDoseForm(EMPTY_DOSE_FORM)).toEqual({});
    expect(doseFormToPayload(EMPTY_DOSE_FORM)).toEqual({
      doseAmount: null,
      doseUnit: null,
      doseFrequency: null,
      doseTime: null,
    });
  });

  it('accepts 250 mcg twice weekly exactly as entered', () => {
    const form = { amount: '250', unit: 'mcg', frequency: ' twice weekly ', time: '' };
    expect(validateDoseForm(form)).toEqual({});
    expect(doseFormToPayload(form)).toEqual({
      doseAmount: 250,
      doseUnit: 'mcg',
      doseFrequency: 'twice weekly',
      doseTime: null,
    });
  });

  it.each(['0', '-1', '-0.5', 'abc', 'Infinity', '1e999'])('rejects the amount %s as not a positive finite number', (amount) => {
    const errors = validateDoseForm({ amount, unit: 'mg', frequency: '', time: '' });
    expect(errors.amount).toBeTruthy();
  });

  it.each(['0.0001', '0.5', '100000', '1e6'])('accepts the amount %s: there is no recommended range', (amount) => {
    expect(validateDoseForm({ amount, unit: 'mcg', frequency: '', time: '' })).toEqual({});
  });

  it('requires an amount with a unit and a unit with an amount', () => {
    expect(validateDoseForm({ amount: '5', unit: '', frequency: '', time: '' }).unit).toBeTruthy();
    expect(validateDoseForm({ amount: '', unit: 'mg', frequency: '', time: '' }).amount).toBeTruthy();
  });

  it('rejects a unit outside the allowed set', () => {
    expect(validateDoseForm({ amount: '5', unit: 'tablets', frequency: '', time: '' }).unit).toBeTruthy();
  });

  it('caps the free-text frequency and time lengths', () => {
    const long = 'x'.repeat(101);
    expect(validateDoseForm({ amount: '', unit: '', frequency: long, time: '' }).frequency).toBeTruthy();
    expect(validateDoseForm({ amount: '', unit: '', frequency: '', time: long }).time).toBeTruthy();
  });

  it('formats only what the user entered', () => {
    expect(formatUserDose({ doseAmount: 250, doseUnit: 'mcg', doseFrequency: 'twice weekly' })).toBe('250 mcg · twice weekly');
    expect(formatUserDose({ doseAmount: 0.5, doseUnit: 'mL' })).toBe('0.5 mL');
    expect(formatUserDose({ doseFrequency: 'every other day' })).toBe('every other day');
    expect(formatUserDoseWithTime({ doseAmount: 250, doseUnit: 'mcg', doseFrequency: 'twice weekly', doseTime: 'mornings' })).toBe(
      '250 mcg · twice weekly · mornings',
    );
    expect(formatUserDose({})).toBeNull();
    expect(formatUserDose(null)).toBeNull();
    expect(formatUserDoseWithTime({ doseTime: 'evenings' })).toBe('evenings');
  });

  it('round-trips an existing record into the edit form, and stays blank for older records', () => {
    expect(doseFormFromCompound({ doseAmount: 250, doseUnit: 'mcg', doseFrequency: 'twice weekly', doseTime: null })).toEqual({
      amount: '250',
      unit: 'mcg',
      frequency: 'twice weekly',
      time: '',
    });
    expect(doseFormFromCompound({})).toEqual(EMPTY_DOSE_FORM);
  });

  it('never suggests or computes a dose: the helper module has no calculator, library or default-dose dependency', () => {
    const source = readFileSync(resolve(process.cwd(), 'src/lib/userDose.ts'), 'utf8')
      .replace(/\/\*[\s\S]*?\*\//g, '')
      .replace(/^\s*\/\/.*$/gm, '');
    const imports = source.match(/^import .*$/gm) ?? [];
    expect(imports).toEqual([]);
    for (const forbidden of ['calculator', 'recommendedDosage', 'standardDosageRange', 'maxReportedDose', 'typical', 'defaultDose']) {
      expect(source.toLowerCase()).not.toContain(forbidden.toLowerCase());
    }
  });
});
