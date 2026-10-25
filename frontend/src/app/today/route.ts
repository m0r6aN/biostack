import { NextResponse } from 'next/server';
import { todayKey } from '@/lib/editorialCalendar';

// /today → the stable permalink for today's claim (rotates 6:00 AM ET).
export const dynamic = 'force-dynamic';

export function GET(request: Request) {
  return NextResponse.redirect(new URL(`/today/${todayKey()}`, request.url), 307);
}
