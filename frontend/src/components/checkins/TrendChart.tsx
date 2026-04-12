'use client';

import { CheckIn } from '@/lib/types';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, ReferenceLine } from 'recharts';

export interface TrendMarker {
  date: string;
  label: string;
  type: 'compound' | 'phase';
}

interface TrendChartProps {
  checkIns: CheckIn[];
  metric: 'weight' | 'energy' | 'sleepQuality' | 'recovery';
  title: string;
  markers?: TrendMarker[];
}

export function TrendChart({ checkIns, metric, title, markers = [] }: TrendChartProps) {
  const sorted = [...checkIns]
    .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime())
    .slice(-30);

  const data = sorted.map((checkIn) => ({
    date: formatChartDate(checkIn.date),
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
            {markers.map((marker) => (
              <ReferenceLine
                key={`${marker.type}-${marker.date}-${marker.label}`}
                x={formatChartDate(marker.date)}
                stroke={marker.type === 'compound' ? '#22C55E' : '#F59E0B'}
                strokeDasharray="4 4"
                label={{ value: marker.label, fill: 'rgba(255,255,255,0.45)', fontSize: 10 }}
              />
            ))}
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
      {markers.length > 0 && (
        <div className="mt-3 flex flex-wrap gap-2 text-[11px] text-white/45">
          {markers.slice(0, 6).map((marker) => (
            <span key={`${marker.type}-chip-${marker.date}-${marker.label}`} className="rounded-md border border-white/[0.08] bg-white/[0.03] px-2 py-1">
              {marker.label}
            </span>
          ))}
        </div>
      )}
    </div>
  );
}

function formatChartDate(date: string) {
  return new Date(date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}
