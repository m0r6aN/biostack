import { ReviewDecisionProvider } from '@/lib/research/ReviewDecisionContext';

export default function ResearchLayout({ children }: { children: React.ReactNode }) {
  return <ReviewDecisionProvider>{children}</ReviewDecisionProvider>;
}
