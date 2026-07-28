import { NextRequest, NextResponse } from 'next/server';

function resolveApiOrigin() {
  const configured =
    process.env.NEXT_PUBLIC_API_URL ||
    process.env.API_URL ||
    'http://localhost:5050';

  return configured.replace(/\/+$/, '');
}

export async function POST(request: NextRequest) {
  const apiOrigin = resolveApiOrigin();
  const targetUrl = `${apiOrigin}/api/v1/auth/start`;

  const contentType = request.headers.get('content-type') ?? 'application/json';
  const bodyText = await request.text();

  const upstream = await fetch(targetUrl, {
    method: 'POST',
    headers: {
      'content-type': contentType,
      cookie: request.headers.get('cookie') ?? '',
    },
    body: bodyText,
    cache: 'no-store',
  });

  const responseText = await upstream.text();

  const response = new NextResponse(responseText, {
    status: upstream.status,
    headers: {
      'content-type': upstream.headers.get('content-type') ?? 'application/json',
      'cache-control': 'no-store',
    },
  });

  const setCookie = upstream.headers.get('set-cookie');
  if (setCookie) {
    response.headers.set('set-cookie', setCookie);
  }

  return response;
}
