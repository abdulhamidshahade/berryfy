import { NextRequest, NextResponse } from 'next/server';
import { cookies } from 'next/headers';
import { AuthService } from '../../../../lib/services/auth/service';

export async function POST(request: NextRequest) {
  const cookieStore = await cookies();
  try {
    const token = cookieStore.get('auth_token')?.value;
    if (token) await AuthService.logout(token);
  } catch (error) {
    console.error('Logout error:', error);
  } finally {
    cookieStore.delete('auth_token');
    cookieStore.delete('refresh_token');
    cookieStore.delete('user_info');
  }
  return NextResponse.redirect(new URL('/', request.url), 303);
}
