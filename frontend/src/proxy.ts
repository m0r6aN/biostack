import type { NextRequest } from 'next/server';
import { NextResponse } from 'next/server';

const PUBLIC_PREFIX_ROUTES = [
  '/auth',
  '/pricing',
  '/faq',
  '/onboarding',
  '/tools',
  '/terms',
  '/privacy',
  '/robots.txt',
  '/sitemap.xml',
];

export default function proxy(req: NextRequest) {
  const { pathname } = req.nextUrl;

  if (pathname === '/' || PUBLIC_PREFIX_ROUTES.some((p) => pathname.startsWith(p))) {
    return NextResponse.next();
  }

  if (process.env.NODE_ENV === 'development' && pathname.startsWith('/admin')) {
    return NextResponse.next();
  }

  const hasSessionCookie = Boolean(req.cookies.get('biostack_session')?.value);

  if (!hasSessionCookie) {
    const signInUrl = new URL('/auth/signin', req.url);
    signInUrl.searchParams.set('callbackUrl', `${req.nextUrl.pathname}${req.nextUrl.search}`);
    return NextResponse.redirect(signInUrl);
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/((?!_next/static|_next/image|favicon.ico|public/).*)'],
};
