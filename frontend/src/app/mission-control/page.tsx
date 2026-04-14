import { redirect } from 'next/navigation';

export default function LegacyProtocolConsoleRedirectPage() {
  redirect('/protocol-console');
}
