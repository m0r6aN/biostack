import { redirect } from 'next/navigation';

export default function MapPage() {
  redirect('/start?mode=existing');
}
