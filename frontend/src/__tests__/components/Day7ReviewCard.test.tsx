import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Day7ReviewCard } from '@/components/checkins/Day7ReviewCard';
import { Day7Review } from '@/lib/types';

describe('Day7ReviewCard', () => {
  it('does not render a completed review before enough check-ins are present', () => {
    render(<Day7ReviewCard review={pendingReview} />);

    expect(screen.getByLabelText(/day 7 review pending/i)).toBeInTheDocument();
    expect(screen.getByText(/keep collecting observations/i)).toBeInTheDocument();
    expect(screen.queryByText(/recent signal check/i)).not.toBeInTheDocument();
  });

  it('renders a completed review when the Day-7 threshold is earned', () => {
    render(<Day7ReviewCard review={earnedReview} />);

    expect(screen.getByLabelText('Day 7 Review')).toBeInTheDocument();
    expect(screen.getByText(/recent signal check/i)).toBeInTheDocument();
    expect(screen.getByText(/mostly moving upward/i)).toBeInTheDocument();
    expect(screen.getByText(/continue observing this pattern/i)).toBeInTheDocument();
  });

  it('renders insufficient-data guidance as a first-class state', () => {
    render(<Day7ReviewCard review={pendingReview} />);

    expect(screen.getByText('3/5 check-ins')).toBeInTheDocument();
    expect(screen.getByText(/record at least 5 check-ins/i)).toBeInTheDocument();
  });
});

const pendingReview: Day7Review = {
  isEarned: false,
  coveredDays: 3,
  requiredDays: 5,
  sleepTrend: 'insufficient_data',
  energyTrend: 'insufficient_data',
  recoveryTrend: 'insufficient_data',
  trendSummary: 'Not enough check-ins yet to form a 7-day review.',
  signalStrength: 'weak',
  alignmentWithExpected: 'unclear',
  nextStep: 'track_longer',
  confidenceNote: 'Record at least 5 check-ins across a 7-day window before reviewing patterns.',
};

const earnedReview: Day7Review = {
  isEarned: true,
  coveredDays: 5,
  requiredDays: 5,
  sleepTrend: 'improving',
  energyTrend: 'improving',
  recoveryTrend: 'improving',
  trendSummary: 'Sleep, energy, and recovery are mostly moving upward across recent check-ins.',
  signalStrength: 'clear',
  alignmentWithExpected: 'unclear',
  nextStep: 'continue',
  confidenceNote: 'This review compares simple direction across recent check-ins and stays observational.',
};
