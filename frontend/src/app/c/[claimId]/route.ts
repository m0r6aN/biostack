import { NextResponse } from 'next/server';
import { fetchAllEntries, findClaim } from '@/lib/claimLibrary';

// Short claim URL used on cards when the full path would overflow the 64-char footer.
export async function GET(request: Request, { params }: { params: Promise<{ claimId: string }> }) {
  const { claimId } = await params;
  const hit = findClaim(await fetchAllEntries(), claimId);
  const target = hit ? `/knowledge/${hit.slug}/claims/${hit.claim.id}` : '/knowledge';
  return NextResponse.redirect(new URL(target, request.url), 301);
}
