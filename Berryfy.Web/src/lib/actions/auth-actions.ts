'use server';

import { cookies } from 'next/headers';
import { redirect } from 'next/navigation';
import { AuthService } from '../services/auth/service';
import { LoginRequest, RegisterRequest } from '../../types/auth';
import { User } from '../../types/user';
import { cookieIsSecure } from '../cookie-is-secure-server';
import { cache } from 'react';

export async function loginAction(formData: FormData) {
  const email = formData.get('email') as string;
  const password = formData.get('password') as string;

  if (!email || !password) {
    return {
      error: 'Email and password are required',
    };
  }

  try {
    const credentials: LoginRequest = { email, password };
    const response = await AuthService.login(credentials);

    if (response.isSuccess && response.data) {
      const cookieStore = await cookies();
      const secure = await cookieIsSecure();

      cookieStore.set('auth_token', response.data.token, {
        path: '/',
        httpOnly: true,
        secure,
        sameSite: 'lax',
        maxAge: 2 * 60 * 60, // matches JWT expiry (2 hours)
      });

      cookieStore.set('refresh_token', response.data.refreshToken, {
        path: '/',
        httpOnly: true,
        secure,
        sameSite: 'lax',
        maxAge: 7 * 24 * 60 * 60, // 7 days
      });

      cookieStore.set('user_info', JSON.stringify(response.data.user), {
        path: '/',
        httpOnly: true,
        secure,
        sameSite: 'lax',
        maxAge: 7 * 24 * 60 * 60,
      });

      return { success: true };
    } else {
      if (response.statusCode === 403 && response.statusMessage?.toLowerCase().includes('email not confirmed')) {
        return {
          error: response.statusMessage || 'Email not confirmed',
          emailNotConfirmed: true,
        };
      }
      
      return {
        error: response.statusMessage || 'Login failed',
      };
    }
  } catch (error) {
    console.error('Login error:', error);
    return {
      error: 'An unexpected error occurred',
    };
  }
}

/** Form action: signs in then redirects (used by client LoginForm). */
export async function loginFormSubmitAction(formData: FormData) {
  const redirectToRaw = formData.get('redirectTo') as string | null;
  const redirectTo =
    redirectToRaw && redirectToRaw.startsWith('/') && !redirectToRaw.startsWith('//')
      ? redirectToRaw
      : '/';

  const result = await loginAction(formData);

  if (result.success) {
    redirect(redirectTo);
  }

  if (result.emailNotConfirmed) {
    const email = formData.get('email') as string;
    const message = result.error || 'Email not confirmed. Enter the 6-digit code from your email, or resend a new code.';
    redirect(`/auth/confirm-email?email=${encodeURIComponent(email)}&error=${encodeURIComponent(message)}`);
  }

  redirect(`/auth/login?error=${encodeURIComponent(result.error || 'Login failed')}`);
}

export async function registerAction(formData: FormData) {
  const firstName = formData.get('firstName') as string;
  const lastName = formData.get('lastName') as string;
  const userName = formData.get('userName') as string;
  const email = formData.get('email') as string;
  const password = formData.get('password') as string;
  const confirmPassword = formData.get('confirmPassword') as string;

  if (!firstName || !lastName || !userName || !email || !password || !confirmPassword) {
    return {
      error: 'All fields are required',
    };
  }

  if (password !== confirmPassword) {
    return {
      error: 'Passwords do not match',
    };
  }

  try {
    const userData: RegisterRequest = {
      firstName,
      lastName,
      userName,
      email,
      password,
      confirmPassword,
    };

    const response = await AuthService.register(userData);

    if (response.isSuccess) {
      return { success: true };
    }

    return {
      error: response.statusMessage || response.errors?.[0] || 'Registration failed.',
    };
  } catch (error) {
    console.error('Registration error:', error);
    return {
      error: 'An unexpected error occurred',
    };
  }
}

export async function forgotPasswordAction(formData: FormData) {
  const email = formData.get('email') as string;

  if (!email) {
    return {
      error: 'Email is required',
    };
  }

  try {
    const response = await AuthService.forgotPassword({ email });

    if (response.isSuccess) {
      return { success: true };
    } else {
      return {
        error: response.statusMessage || 'Failed to process forgot password request',
      };
    }
  } catch (error) {
    console.error('Forgot password error:', error);
    return {
      error: 'An unexpected error occurred',
    };
  }
}

export async function verifyPasswordResetCodeAction(formData: FormData) {
  const email = formData.get('email') as string;
  const code = formData.get('code') as string;

  if (!email || !code) {
    return {
      error: 'Email and verification code are required',
    };
  }

  try {
    const response = await AuthService.verifyPasswordResetCode({ email, code });

    if (response.isSuccess && response.data?.resetToken) {
      return { success: true as const, resetToken: response.data.resetToken };
    }

    return {
      error: response.statusMessage || 'Invalid or expired code. Please try again.',
    };
  } catch (error) {
    console.error('Verify password reset code error:', error);
    return {
      error: 'An unexpected error occurred',
    };
  }
}

export async function resendPasswordResetAction(formData: FormData) {
  const email = formData.get('email') as string;

  if (!email) {
    return {
      error: 'Email is required',
    };
  }

  try {
    const response = await AuthService.resendPasswordReset({ email });

    if (response.isSuccess) {
      return { success: true as const };
    }

    return {
      error: response.statusMessage || 'Failed to resend reset code',
    };
  } catch (error) {
    console.error('Resend password reset error:', error);
    return {
      error: 'An unexpected error occurred',
    };
  }
}

export async function resetPasswordAction(formData: FormData) {
  const email = formData.get('email') as string;
  const token = formData.get('token') as string;
  const newPassword = formData.get('newPassword') as string;
  const confirmPassword = formData.get('confirmPassword') as string;

  if (!email || !token || !newPassword || !confirmPassword) {
    return {
      error: 'All fields are required',
    };
  }

  if (newPassword !== confirmPassword) {
    return {
      error: 'Passwords do not match',
    };
  }

  try {
    const resetData = {
      email,
      token,
      newPassword,
      confirmPassword,
    };

    const response = await AuthService.resetPassword(resetData);

    if (response.isSuccess) {
      return { success: true, error: undefined, errors: undefined };
    } else {
      const errs = response.errors && response.errors.length > 0 ? response.errors : undefined;
      return {
        error: errs ? errs[0] : (response.statusMessage || 'Failed to reset password'),
        errors: errs,
        success: false,
      };
    }
  } catch (error) {
    console.error('Reset password error:', error);
    return {
      error: 'An unexpected error occurred. Please try again.',
      errors: undefined,
      success: false,
    };
  }
}

export async function confirmEmailAction(formData: FormData) {
  const email = formData.get('email') as string;
  const code = formData.get('code') as string;

  if (!email || !code) {
    return {
      error: 'Email and verification code are required',
    };
  }

  try {
    const confirmationData = {
      email,
      code,
    };

    const response = await AuthService.confirmEmail(confirmationData);

    if (response.isSuccess) {
      return { success: true };
    } else {
      return {
        error: response.statusMessage || 'Failed to confirm email',
      };
    }
  } catch (error) {
    console.error('Email confirmation error:', error);
    return {
      error: 'An unexpected error occurred',
    };
  }
}

export async function resendConfirmationAction(formData: FormData) {
  const email = formData.get('email') as string;

  if (!email) {
    return {
      error: 'Email is required',
    };
  }

  try {
    const response = await AuthService.resendConfirmation({ email });

    if (response.isSuccess) {
      return { success: true };
    } else {
      return {
        error: response.statusMessage || 'Failed to send confirmation code',
      };
    }
  } catch (error) {
    console.error('Resend confirmation error:', error);
    return {
      error: 'An unexpected error occurred',
    };
  }
}

export async function logoutAction() {
  const cookieStore = await cookies();
  const token = cookieStore.get('auth_token')?.value;

  if (token) {
    try {
      await AuthService.logout(token);
    } catch (error) {
      console.error('Logout API error:', error);
    }
  }

  cookieStore.delete('auth_token');
  cookieStore.delete('refresh_token');
  cookieStore.delete('user_info');

  redirect('/');
}

const getVerifiedCurrentUser = cache(async (): Promise<User | null> => {
  const cookieStore = await cookies();
  const token = cookieStore.get('auth_token')?.value;

  if (!token) {
    return null;
  }

  try {
    const response = await AuthService.getCurrentUser(token);
    if (!response.isSuccess || !response.data) return null;
    return { ...response.data, roles: response.data.roles ?? [] };
  } catch (error) {
    console.error('Error getting current user:', error);
    return null;
  }
});

export async function getAuthToken(): Promise<string | null> {
  const cookieStore = await cookies();
  return cookieStore.get('auth_token')?.value || null;
}

export async function updateProfileAction(formData: FormData) {
  const firstName = formData.get('firstName') as string;
  const lastName = formData.get('lastName') as string;
  const userName = formData.get('userName') as string;

  if (!firstName || !lastName || !userName) {
    return { error: 'All fields are required' };
  }

  const cookieStore = await cookies();
  const token = cookieStore.get('auth_token')?.value;
  if (!token) return { error: 'Not authenticated' };

  try {
    const response = await AuthService.updateProfile(token, { firstName, lastName, userName });
    if (response.isSuccess) {
      const userInfo = cookieStore.get('user_info')?.value;
      if (userInfo) {
        const user = JSON.parse(userInfo) as User;
        user.firstName = firstName;
        user.lastName = lastName;
        user.userName = userName;
        const secure = await cookieIsSecure();
        cookieStore.set('user_info', JSON.stringify(user), {
          path: '/',
          httpOnly: true,
          secure,
          sameSite: 'lax',
          maxAge: 24 * 60 * 60,
        });
      }
      return { success: true };
    }
    return { error: response.statusMessage || 'Failed to update profile' };
  } catch {
    return { error: 'An unexpected error occurred' };
  }
}

export async function refreshTokenAction(): Promise<boolean> {
  const cookieStore = await cookies();
  const refreshToken = cookieStore.get('refresh_token')?.value;

  if (!refreshToken) return false;

  try {
    const response = await fetch(`${process.env.API_BASE_AUTH}/refresh-token`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ token: refreshToken }),
    });

    if (!response.ok) return false;

    const result = await response.json();

    if (!result.isSuccess || !result.data?.token) return false;

    const secure = await cookieIsSecure();

    cookieStore.set('auth_token', result.data.token, {
      path: '/',
      httpOnly: true,
      secure,
      sameSite: 'lax',
      maxAge: 2 * 60 * 60,
    });

    if (result.data.refreshToken) {
      cookieStore.set('refresh_token', result.data.refreshToken, {
        path: '/',
        httpOnly: true,
        secure,
        sameSite: 'lax',
        maxAge: 7 * 24 * 60 * 60,
      });
    }

    if (result.data.user) {
      cookieStore.set('user_info', JSON.stringify(result.data.user), {
        path: '/',
        httpOnly: true,
        secure,
        sameSite: 'lax',
        maxAge: 7 * 24 * 60 * 60,
      });
    }

    return true;
  } catch {
    return false;
  }
}

export async function changePasswordAction(formData: FormData) {
  const currentPassword = formData.get('currentPassword') as string;
  const newPassword = formData.get('newPassword') as string;
  const confirmNewPassword = formData.get('confirmNewPassword') as string;

  if (!currentPassword || !newPassword || !confirmNewPassword) {
    return { error: 'All fields are required' };
  }

  if (newPassword !== confirmNewPassword) {
    return { error: 'New password and confirmation do not match' };
  }

  const cookieStore = await cookies();
  const token = cookieStore.get('auth_token')?.value;
  if (!token) return { error: 'Not authenticated' };

  try {
    const response = await AuthService.changePassword(token, {
      currentPassword,
      newPassword,
      confirmNewPassword,
    });
    if (response.isSuccess) return { success: true };
    return { error: response.statusMessage || 'Failed to change password' };
  } catch {
    return { error: 'An unexpected error occurred' };
  }
}

export async function getCurrentUser(): Promise<User | null> {
  return getVerifiedCurrentUser();
}
