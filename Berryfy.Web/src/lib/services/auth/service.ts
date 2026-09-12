import { LoginRequest, RegisterRequest, AuthResponse, ApiResponse, AuthLog } from '../../../types/auth';
import { ResponseDto } from '../../../types/responseDto';
import { cookies } from 'next/headers';
import { User } from '../../../types/user';
import { apiRequest } from '../../utils/api';
import { cookieIsSecure } from '../../cookie-is-secure-server';

const API_BASE_URL = process.env.API_BASE_AUTH;

export class AuthService {
  private static async makeRequest<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<ApiResponse<T>> {
    const url = `${API_BASE_URL}${endpoint}`;
    
    const defaultHeaders = {
      'Content-Type': 'application/json',
      ...options.headers,
    };

    try {
      const response = await fetch(url, {
        ...options,
        cache: 'no-store',
        headers: defaultHeaders,
      });

      const data = await response.json();
      return data;
    } catch (error) {
      console.error('API request failed:', error);
      throw new Error('Network error occurred');
    }
  }

  static async login(credentials: LoginRequest): Promise<ResponseDto<AuthLog>> {
const cookieStore = await cookies();
    const token = cookieStore.get('cart_session')?.value;

    var response = await fetch(`${API_BASE_URL}/login`, {
      method: 'POST',
      body: JSON.stringify(credentials),
      headers: {
        'Content-Type': 'application/json',
        
                    'X-Session-Id': token ?? ''
                
      },
      
    });

    const jsonRes: ResponseDto<AuthLog> = await response.json();

    return jsonRes;
   

  }

  static async register(userData: RegisterRequest): Promise<ResponseDto<any>> {
    const cookieStore = await cookies();
    const sessionId = cookieStore.get('cart_session')?.value;

    const response = await fetch(`${API_BASE_URL}/register`, {
      method: 'POST',
      body: JSON.stringify(userData),
      headers: {
        'Content-Type': 'application/json',
        ...(sessionId ? { 'X-Session-Id': sessionId } : {}),
      },
    });

    return await response.json();
  }

  static async getCurrentUser(token: string): Promise<ApiResponse<User>> {
    return this.makeRequest<User>('/me', {
      method: 'GET',
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });
  }

  static async logout(token: string): Promise<AuthResponse> {
    const cookieStore = await cookies();
    const res: Promise<AuthResponse> = apiRequest(`${API_BASE_URL}/logout`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });
const newSessionId = crypto.randomUUID();
    const secure = await cookieIsSecure();
    cookieStore.set("cart_session", newSessionId, {
      path: "/",
      httpOnly: true,
      sameSite: "lax",
      secure,
      maxAge: 30 * 24 * 60 * 60,
    });

    return res;

  }

  static async checkEmailExists(email: string): Promise<ApiResponse<boolean>> {
    return this.makeRequest<boolean>(`/exists/email-address/${encodeURIComponent(email)}`, {
      method: 'GET',
    });
  }

  static async forgotPassword(data: { email: string }): Promise<ResponseDto<any>> {
    const response = await fetch(`${API_BASE_URL}/forgot-password`, {
      method: 'POST',
      body: JSON.stringify(data),
      headers: {
        'Content-Type': 'application/json',
      },
    });

    const jsonRes: ResponseDto<any> = await response.json();
    return jsonRes;
  }

  static async verifyPasswordResetCode(data: {
    email: string;
    code: string;
  }): Promise<ResponseDto<{ resetToken: string }>> {
    const response = await fetch(`${API_BASE_URL}/verify-password-reset-code`, {
      method: 'POST',
      body: JSON.stringify(data),
      headers: {
        'Content-Type': 'application/json',
      },
    });

    return await response.json();
  }

  static async resendPasswordReset(data: { email: string }): Promise<ResponseDto<any>> {
    const response = await fetch(`${API_BASE_URL}/resend-password-reset`, {
      method: 'POST',
      body: JSON.stringify(data),
      headers: {
        'Content-Type': 'application/json',
      },
    });

    return await response.json();
  }

  static async resetPassword(data: {
    email: string;
    token: string;
    newPassword: string;
    confirmPassword: string;
  }): Promise<ResponseDto<any>> {
    const response = await fetch(`${API_BASE_URL}/reset-password`, {
      method: 'POST',
      body: JSON.stringify(data),
      headers: {
        'Content-Type': 'application/json',
      },
    });

    const jsonRes: ResponseDto<any> = await response.json();
    return jsonRes;
  }

  static async confirmEmail(data: {
    email: string;
    code: string;
  }): Promise<ResponseDto<any>> {
    const response = await fetch(`${API_BASE_URL}/confirm-email`, {
      method: 'POST',
      body: JSON.stringify(data),
      headers: {
        'Content-Type': 'application/json',
      },
    });

    const jsonRes: ResponseDto<any> = await response.json();
    return jsonRes;
  }

  static async resendConfirmation(data: { email: string }): Promise<ResponseDto<any>> {
    const response = await fetch(`${API_BASE_URL}/resend-confirmation`, {
      method: 'POST',
      body: JSON.stringify(data),
      headers: {
        'Content-Type': 'application/json',
      },
    });

    const jsonRes: ResponseDto<any> = await response.json();
    return jsonRes;
  }

  static async updateProfile(
    token: string,
    data: { firstName: string; lastName: string; userName: string }
  ): Promise<ResponseDto<any>> {
    const response = await fetch(`${API_BASE_URL}/me`, {
      method: 'PUT',
      body: JSON.stringify(data),
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
    });
    return response.json();
  }

  static async changePassword(
    token: string,
    data: { currentPassword: string; newPassword: string; confirmNewPassword: string }
  ): Promise<ResponseDto<any>> {
    const response = await fetch(`${API_BASE_URL}/me/change-password`, {
      method: 'POST',
      body: JSON.stringify(data),
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
    });
    return response.json();
  }
}