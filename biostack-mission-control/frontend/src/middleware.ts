import { auth } from '@/lib/auth';
import type { NextRequest } from 'next/server';
import { NextResponse } from 'next/server';

// Public routes that do NOT require sign-in
const PUBLIC_PREFIX_ROUTES = [
  '/auth/signin',
  '/api/auth',
  '/pricing',
  '/faq',
  '/tools',
  '/terms',
  '/privacy',
  '/robots.txt',
  '/sitemap.xml',
];

export default auth(function middleware(req: NextRequest & { auth?: unknown }) {
  const { pathname } = req.nextUrl;

  if (pathname === '/') {
    return NextResponse.next();
  }

  if (PUBLIC_PREFIX_ROUTES.some((p) => pathname.startsWith(p))) {
    return NextResponse.next();
  }

  const session = (req as unknown as { auth?: { user?: { role?: number } } }).auth;

  if (!session?.user) {
    const signInUrl = new URL('/auth/signin', req.url);
    signInUrl.searchParams.set('callbackUrl', req.url);
    return NextResponse.redirect(signInUrl);
  }

  // Admin routes — require role === 1
  if (pathname.startsWith('/admin') && session.user.role !== 1) {
    return NextResponse.redirect(new URL('/mission-control', req.url));
  }

  return NextResponse.next();
});

export const config = {
  matcher: ['/((?!_next/static|_next/image|favicon.ico|public/).*)'],
};
