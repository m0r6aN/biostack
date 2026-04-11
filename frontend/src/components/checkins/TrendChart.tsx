'use client';

import { CheckIn } from '@/lib/types';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';

interface TrendChartProps {
  checkIns: CheckIn[];
  metric: 'weight' | 'energy' | 'sleepQuality' | 'recovery';
  title: string;
}

export function TrendChart({ checkIns, metric, title }: TrendChartProps) {
  const sorted = [...checkIns]
    .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime())
    .slice(-30);

  const data = sorted.map((checkIn) => ({
    date: new Date(checkIn.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
    value:
      metric === 'weight'
        ? checkIn.weight
        : metric === 'energy'
          ? checkIn.energy
          : metric === 'sleepQuality'
            ? checkIn.sleepQuality
            : checkIn.recovery,
  }));

  const color = {
    weight: '#22C55E',
    energy: '#3B82F6',
    sleepQuality: '#F59E0B',
    recovery: '#8B5CF6',
  }[metric];

  return (
    <div className="p-6 bg-[#121923]/90 border border-white/[0.08] rounded-2xl shadow-[0_8px_24px_rgba(0,0,0,0.35)]">
      <h3 className="text-lg font-semibold text-white mb-4">{title}</h3>
      {data.length === 0 ? (
        <p className="text-sm text-white/50">No data available</p>
      ) : (
        <ResponsiveContainer width="100%" height={300}>
          <LineChart data={data}>
            <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.06)" />
            <XAxis dataKey="date" stroke="rgba(255,255,255,0.25)" style={{ fontSize: '12px' }} />
            <YAxis stroke="rgba(255,255,255,0.25)" style={{ fontSize: '12px' }} />
            <Tooltip
              contentStyle={{
                backgroundColor: '#121923',
                border: '1px solid rgba(255,255,255,0.1)',
                borderRadius: '12px',
              }}
              labelStyle={{ color: 'white' }}
            />
            <Legend />
            <Line
              type="monotone"
              dataKey="value"
              name={title}
              stroke={color}
              dot={false}
              strokeWidth={2}
              isAnimationActive={false}
            />
          </LineChart>
        </ResponsiveContainer>
      )}
    </div>
  );
}
