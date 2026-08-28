import type { NextRequest } from 'next/server';
import { NextResponse } from 'next/server';
import { isPublicRoutePath } from '@/lib/productContract';

const API_URL = (process.env.NEXT_PUBLIC_API_URL || process.env.API_URL || 'http://localhost:5050')
  .replace(/\/+$/, '');

type SessionContract = {
  authenticated: boolean;
};

function redirectToSignIn(req: NextRequest, error?: 'session-expired' | 'session-unavailable') {
  const signInUrl = new URL('/auth/signin', req.url);
  signInUrl.searchParams.set('callbackUrl', `${req.nextUrl.pathname}${req.nextUrl.search}`);
  if (error) {
    signInUrl.searchParams.set('error', error);
  }

  return NextResponse.redirect(signInUrl);
}

function copySessionCookie(upstream: Response, downstream: NextResponse) {
  const setCookie = upstream.headers.get('set-cookie');
  if (setCookie) {
    downstream.headers.set('set-cookie', setCookie);
  }
}

export default async function middleware(req: NextRequest) {
  const { pathname } = req.nextUrl;

  if (pathname === '/calculators') {
    return NextResponse.redirect(new URL('/tools', req.url), 308);
  }

  if (isPublicRoutePath(pathname)) {
    return NextResponse.next();
  }

  if (process.env.NODE_ENV === 'development' && pathname.startsWith('/admin')) {
    return NextResponse.next();
  }

  const sessionCookie = req.cookies.get('biostack_session')?.value;

  if (!sessionCookie) {
    return redirectToSignIn(req);
  }

  try {
    const sessionResponse = await fetch(`${API_URL}/api/v1/auth/session`, {
      headers: {
        cookie: `biostack_session=${sessionCookie}`,
      },
      cache: 'no-store',
    });

    if (!sessionResponse.ok) {
      const redirect = redirectToSignIn(
        req,
        sessionResponse.status === 401 || sessionResponse.status === 403
          ? 'session-expired'
          : 'session-unavailable',
      );
      copySessionCookie(sessionResponse, redirect);
      return redirect;
    }

    const session = (await sessionResponse.json()) as SessionContract;
    if (session.authenticated !== true) {
      const redirect = redirectToSignIn(req, 'session-expired');
      copySessionCookie(sessionResponse, redirect);
      return redirect;
    }

    const response = NextResponse.next();
    copySessionCookie(sessionResponse, response);
    return response;
  } catch {
    return redirectToSignIn(req, 'session-unavailable');
  }
}

export const config = {
  matcher: ['/((?!_next/static|_next/image|favicon.ico|public/).*)'],
};
