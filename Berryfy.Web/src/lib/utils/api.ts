import { cookies } from 'next/headers';

export interface ApiRequestOptions extends RequestInit {
  requireAuth?: boolean;
  isPublic?: boolean;
}

async function parseJsonResponse<T>(response: Response): Promise<T> {
  const contentType = response.headers.get('content-type');
  if (!contentType || !contentType.includes('application/json')) {
    throw new Error('Response is not JSON');
  }
  const text = await response.text();
  if (!text) {
    throw new Error('Empty response body');
  }
  return JSON.parse(text) as T;
}

export async function apiRequest<T>(
  url: string, 
  options: ApiRequestOptions = {}
): Promise<T> {
  const { requireAuth = false, isPublic = false, headers = {}, ...fetchOptions } = options;
  
  const cookieStore = await cookies();

  const buildHeaders = (token?: string): Record<string, string> => {
    const h: Record<string, string> = {
      'Content-Type': 'application/json',
      ...(headers as Record<string, string>),
    };
    if (token) h['Authorization'] = `Bearer ${token}`;
    return h;
  };

  const token = (requireAuth && !isPublic)
    ? cookieStore.get('auth_token')?.value
    : undefined;

  try {
    const response = await fetch(url, {
      ...fetchOptions,
      headers: buildHeaders(token),
      credentials: 'include',
    });

    if (!response.ok) {
      if (response.status === 401 && isPublic) {
        console.warn(`Authentication required for public endpoint: ${url}`);
        return {
          isSuccess: false,
          data: null,
          statusMessage: 'Authentication required'
        } as T;
      }
      
      let errorDetails = '';
      let parsedError: any = null;
      try {
        const errorText = await response.text();
        if (errorText) {
          try {
            parsedError = JSON.parse(errorText);
            errorDetails = parsedError.message || parsedError.statusMessage || parsedError.title || errorText;
          } catch {
            errorDetails = errorText;
          }
        }
      } catch {
      }
      
      // Handle 404 gracefully for GET requests
      if (response.status === 404 && (!options.method || options.method === 'GET')) {
        if (parsedError) {
          return parsedError as T;
        }
        return {
          isSuccess: false,
          statusCode: 404,
          statusMessage: errorDetails || 'Not found',
          data: null
        } as T;
      }
      
      const errorMessage = `HTTP error! status: ${response.status}${errorDetails ? ` - ${errorDetails}` : ''}`;
      console.error('API Request Error:', {
        url,
        status: response.status,
        statusText: response.statusText,
        errorDetails,
        method: fetchOptions.method ?? 'GET'
      });
      
      throw new Error(errorMessage);
    }

    return parseJsonResponse<T>(response);
  } catch (error) {
    console.error('API request failed:', error);
    throw error;
  }
}