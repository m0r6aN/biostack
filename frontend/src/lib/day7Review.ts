import { CheckIn, Day7Review, Day7ReviewTrend } from './types';

const WINDOW_DAYS = 7;
export const MINIMUM_DAY7_CHECKINS = 5;
const TREND_THRESHOLD = 0.75;

type MetricPoint = {
  date: string;
  value: number;
};

export function buildDay7Review(checkIns: CheckIn[]): Day7Review {
  const window = getLatestWindow(checkIns);
  const coveredDays = new Set(window.map((checkIn) => toDayKey(checkIn.date))).size;

  if (coveredDays < MINIMUM_DAY7_CHECKINS) {
    return {
      isEarned: false,
      coveredDays,
      requiredDays: MINIMUM_DAY7_CHECKINS,
      sleepTrend: 'insufficient_data',
      energyTrend: 'insufficient_data',
      recoveryTrend: 'insufficient_data',
      trendSummary: 'Not enough check-ins yet to form a 7-day review.',
      signalStrength: 'weak',
      alignmentWithExpected: 'unclear',
      nextStep: 'track_longer',
      confidenceNote: `Record at least ${MINIMUM_DAY7_CHECKINS} check-ins across a 7-day window before reviewing patterns.`,
    };
  }

  const sleepTrend = calculateTrend(window.map((checkIn) => ({ date: checkIn.date, value: checkIn.sleepQuality })));
  const energyTrend = calculateTrend(window.map((checkIn) => ({ date: checkIn.date, value: checkIn.energy })));
  const recoveryTrend = calculateTrend(window.map((checkIn) => ({ date: checkIn.date, value: checkIn.recovery })));
  const trends = [sleepTrend, energyTrend, recoveryTrend];
  const signalStrength = resolveSignalStrength(trends);

  return {
    isEarned: true,
    coveredDays,
    requiredDays: MINIMUM_DAY7_CHECKINS,
    sleepTrend,
    energyTrend,
    recoveryTrend,
    trendSummary: summarizeTrends(sleepTrend, energyTrend, recoveryTrend),
    signalStrength,
    alignmentWithExpected: 'unclear',
    nextStep: resolveNextStep(trends, signalStrength),
    confidenceNote: 'This review compares simple direction across recent check-ins and stays observational.',
  };
}

function getLatestWindow(checkIns: CheckIn[]): CheckIn[] {
  const ordered = [...checkIns].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
  const latest = ordered[0];

  if (!latest) {
    return [];
  }

  const latestDay = startOfDay(latest.date);
  const windowStart = new Date(latestDay);
  windowStart.setDate(latestDay.getDate() - (WINDOW_DAYS - 1));

  return ordered
    .filter((checkIn) => {
      const day = startOfDay(checkIn.date);
      return day >= windowStart && day <= latestDay;
    })
    .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
}

function calculateTrend(points: MetricPoint[]): Day7ReviewTrend {
  if (points.length < MINIMUM_DAY7_CHECKINS) {
    return 'insufficient_data';
  }

  const ordered = [...points].sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
  const midpoint = Math.floor(ordered.length / 2);
  const earlyAverage = average(ordered.slice(0, midpoint).map((point) => point.value));
  const laterAverage = average(ordered.slice(midpoint).map((point) => point.value));
  const delta = laterAverage - earlyAverage;

  if (delta >= TREND_THRESHOLD) {
    return 'improving';
  }

  if (delta <= -TREND_THRESHOLD) {
    return 'declining';
  }

  return 'flat';
}

function summarizeTrends(
  sleepTrend: Day7ReviewTrend,
  energyTrend: Day7ReviewTrend,
  recoveryTrend: Day7ReviewTrend
) {
  const trends = [sleepTrend, energyTrend, recoveryTrend];
  const improving = trends.filter((trend) => trend === 'improving').length;
  const declining = trends.filter((trend) => trend === 'declining').length;
  const flat = trends.filter((trend) => trend === 'flat').length;

  if (improving >= 2 && declining === 0) {
    return 'Sleep, energy, and recovery are mostly moving upward across recent check-ins.';
  }

  if (declining >= 2 && improving === 0) {
    return 'Sleep, energy, and recovery are mostly moving downward across recent check-ins.';
  }

  if (flat >= 2 && improving === 0 && declining === 0) {
    return 'Sleep, energy, and recovery look mostly steady across recent check-ins.';
  }

  return 'Recent check-ins show a mixed pattern without one dominant direction.';
}

function resolveSignalStrength(trends: Day7ReviewTrend[]): Day7Review['signalStrength'] {
  const improving = trends.filter((trend) => trend === 'improving').length;
  const declining = trends.filter((trend) => trend === 'declining').length;
  const moving = improving + declining;

  if ((improving >= 2 && declining === 0) || (declining >= 2 && improving === 0)) {
    return 'clear';
  }

  if (moving === 1 || (moving === 2 && improving !== declining)) {
    return 'moderate';
  }

  return 'weak';
}

function resolveNextStep(
  trends: Day7ReviewTrend[],
  signalStrength: Day7Review['signalStrength']
): Day7Review['nextStep'] {
  if (signalStrength === 'weak') {
    return 'track_longer';
  }

  const improving = trends.filter((trend) => trend === 'improving').length;
  const declining = trends.filter((trend) => trend === 'declining').length;

  return declining > improving ? 'reassess' : 'continue';
}

function average(values: number[]) {
  return values.reduce((total, value) => total + value, 0) / values.length;
}

function startOfDay(date: string) {
  const day = new Date(date);
  day.setHours(0, 0, 0, 0);
  return day;
}

function toDayKey(date: string) {
  return startOfDay(date).toISOString().slice(0, 10);
}
