import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';
import { cookieIsSecureForRequest } from '@/lib/cookie-is-secure-request';

function needsRefresh(token: string): boolean {
  try {
    const payload = JSON.parse(Buffer.from(token.split('.')[1], 'base64url').toString());
    return typeof payload.exp !== 'number' || payload.exp * 1000 <= Date.now() + 30_000;
  } catch {
    return true;
  }
}

export async function proxy(request: NextRequest) {
  const secure = cookieIsSecureForRequest(request);
  const updates = new Map<string, { value: string; maxAge: number }>();
  const setCookie = (name: string, value: string, maxAge: number) => {
    updates.set(name, { value, maxAge });
    if (maxAge === 0) request.cookies.delete(name);
    else request.cookies.set(name, value);
  };

  let token = request.cookies.get('auth_token')?.value;
  const refreshToken = request.cookies.get('refresh_token')?.value;
  if (!token || needsRefresh(token)) {
    token = undefined;
    if (refreshToken) {
      try {
        const res = await fetch(`${process.env.API_BASE_AUTH}/refresh-token`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ token: refreshToken }),
          cache: 'no-store',
        });
        if (res.ok) {
          const result = await res.json();
          if (result.isSuccess && result.data?.token && result.data?.refreshToken) {
            token = result.data.token;
            setCookie('auth_token', token!, 2 * 60 * 60);
            setCookie('refresh_token', result.data.refreshToken, 7 * 24 * 60 * 60);
            if (result.data.user) {
              setCookie('user_info', JSON.stringify(result.data.user), 7 * 24 * 60 * 60);
            }
          } else {
            setCookie('refresh_token', '', 0);
          }
        } else if (res.status === 400 || res.status === 401 || res.status === 403) {
          setCookie('refresh_token', '', 0);
        }
      } catch {
      }
    }
    if (!token) {
      setCookie('auth_token', '', 0);
      setCookie('user_info', '', 0);
    }
  }

  if (token) {
    if (request.cookies.has('cart_session')) setCookie('cart_session', '', 0);
  } else if (!request.cookies.has('cart_session')) {
    setCookie('cart_session', crypto.randomUUID(), 30 * 24 * 60 * 60);
  }

  const { pathname, search } = request.nextUrl;
  const protectedRoutes = ['/profile', '/orders', '/wishlist', '/admin', '/checkout', '/payment'];
  const isProtected = protectedRoutes.some(route => pathname === route || pathname.startsWith(`${route}/`));
  let response: NextResponse;
  if (isProtected && !token) {
    const loginUrl = new URL('/auth/login', request.url);
    loginUrl.searchParams.set('redirectTo', pathname + search);
    response = NextResponse.redirect(loginUrl);
  } else {
    response = NextResponse.next({ request: { headers: new Headers(request.headers) } });
  }
  for (const [name, { value, maxAge }] of updates) {
    response.cookies.set(name, value, { path: '/', httpOnly: true, sameSite: 'lax', secure, maxAge });
  }
  return response;
}

export const config = {
  matcher: ['/((?!_next/static|_next/image|favicon.ico|uploads/|.*\\.(?:svg|png|jpg|jpeg|gif|webp|ico|css|js|woff2?)$).*)'],
};
