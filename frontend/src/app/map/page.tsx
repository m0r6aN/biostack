import { redirect } from 'next/navigation';
import { canonicalRoutes } from '@/lib/productContract';

export default function MapPage() {
  redirect(canonicalRoutes.analyzer);
}
