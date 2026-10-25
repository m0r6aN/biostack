import { renderCard } from '@/lib/cardResponse';
import { fetchAllEntries, findClaim } from '@/lib/claimLibrary';

// Growth Phase 3: claim card ("THE CLAIM / THE RECEIPTS"). Same sizes and cache key as the
// compound card; an old ?v renders a "Superseded" ribbon with the current verdict.
export const revalidate = 3600;

export async function GET(request: Request, { params }: { params: Promise<{ claimId: string }> }) {
  const { claimId } = await params;
  const hit = findClaim(await fetchAllEntries(), claimId);
  return renderCard({ kind: 'claim', entry: hit?.entry ?? null, claim: hit?.claim, request, fallbackName: 'Claim' });
}
