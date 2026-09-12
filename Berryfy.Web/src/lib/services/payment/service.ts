import { Payment, CreatePayment, PaymentResponse, UpdatePaymentStatus, RefundPayment, PaymentSearchParams, PaymentStatus } from "../../../types/payment";
import { IPaymentService } from "./interface";
import { ResponseDto } from "../../../types/responseDto";
import { cookies } from 'next/headers';
import { apiRequest } from "../../utils/api";

const API_BASE_URL = process.env.API_BASE_PAYMENT;
const getDefaultHeaders = async () => {
  const cookieStore = await cookies();
  const authToken = cookieStore.get('auth_token')?.value;
  
  return {
    'Content-Type': 'application/json',
    'Authorization': authToken ? `Bearer ${authToken}` : ''
  };
};

export class PaymentService implements IPaymentService {
  async processPayment(paymentData: CreatePayment): Promise<PaymentResponse> {
      const url = `${API_BASE_URL}/process`; 
    
    try {
      console.log('ProcessPayment - Request URL:', url);
      
      const res: ResponseDto<PaymentResponse> = await apiRequest(url, {
        method: 'POST',
        requireAuth: true,
        body: JSON.stringify(paymentData)
      });
      
      if (!res.isSuccess) {
        throw new Error(`Failed to process payment: ${res.statusMessage}`);
      }
      
      const json = await res;
      
      if (!json.isSuccess || !json.data) {
        throw new Error(json.statusMessage || 'Failed to process payment');
      }
      
      return json.data;
    } catch (error) {
      console.error('PaymentService.processPayment error:', error);
      throw error; 
    }
  }

  async getPaymentById(id: number): Promise<Payment> {
      const res = await fetch(`${API_BASE_URL}/${id}`, {
      headers: await getDefaultHeaders(),
    });
    
    if (!res.ok) {
      const errorJson = await res.json().catch(() => null);
      throw new Error(errorJson?.statusMessage || `Failed to fetch payment: ${res.statusText}`);
    }
    
    const json: ResponseDto<Payment> = await res.json();
    if (!json.isSuccess || !json.data) {
      throw new Error(json.statusMessage || 'Failed to fetch payment');
    }
    return json.data;
  }

  async getPaymentByTransactionId(transactionId: string): Promise<Payment> {
      const res = await fetch(`${API_BASE_URL}/transaction/${transactionId}`, {
      credentials: 'include',
      headers: await getDefaultHeaders(),
    });
    
    if (!res.ok) {
      const errorJson = await res.json().catch(() => null);
      throw new Error(errorJson?.statusMessage || `Failed to fetch payment: ${res.statusText}`);
    }
    
    const json: ResponseDto<Payment> = await res.json();
    if (!json.isSuccess || !json.data) {
      throw new Error(json.statusMessage || 'Failed to fetch payment');
    }
    return json.data;
  }

  async getPaymentByOrderId(orderId: number): Promise<Payment> {
      const res = await fetch(`${API_BASE_URL}/order/${orderId}`, {
      credentials: 'include',
      headers: await getDefaultHeaders(),
    });
    
    if (!res.ok) {
      const errorJson = await res.json().catch(() => null);
      throw new Error(errorJson?.statusMessage || `Failed to fetch payment: ${res.statusText}`);
    }
    
    const json: ResponseDto<Payment> = await res.json();
    if (!json.isSuccess || !json.data) {
      throw new Error(json.statusMessage || 'Failed to fetch payment');
    }
    return json.data;
  }

  async getAllPayments(): Promise<Payment[]> {
      const res = await fetch(`${API_BASE_URL}`, {
      credentials: 'include',
      headers: await getDefaultHeaders(),
    });
    
    if (!res.ok) {
      const errorJson = await res.json().catch(() => null);
      throw new Error(errorJson?.statusMessage || `Failed to fetch payments: ${res.statusText}`);
    }
    
    const json: ResponseDto<Payment[]> = await res.json();
    if (!json.isSuccess || !json.data) {
      throw new Error(json.statusMessage || 'Failed to fetch payments');
    }
    return json.data;
  }

  async getPaymentsByUserId(userId: number): Promise<Payment[]> {
      const res = await fetch(`${API_BASE_URL}/user/${userId}`, {
      credentials: 'include',
      headers: await getDefaultHeaders(),
    });
    
    if (!res.ok) {
      const errorJson = await res.json().catch(() => null);
      throw new Error(errorJson?.statusMessage || `Failed to fetch user payments: ${res.statusText}`);
    }
    
    const json: ResponseDto<Payment[]> = await res.json();
    if (!json.isSuccess || !json.data) {
      throw new Error(json.statusMessage || 'Failed to fetch user payments');
    }
    return json.data;
  }

  async getMyPayments(page: number = 1, pageSize: number = 10): Promise<Payment[]> {
      const res = await fetch(`${API_BASE_URL}/my-payments?page=${page}&pageSize=${pageSize}`, {
      credentials: 'include',
      headers: await getDefaultHeaders(),
    });
    
    if (!res.ok) {
      const errorJson = await res.json().catch(() => null);
      throw new Error(errorJson?.statusMessage || `Failed to fetch my payments: ${res.statusText}`);
    }
    
    const json: ResponseDto<Payment[]> = await res.json();
    if (!json.isSuccess || !json.data) {
      throw new Error(json.statusMessage || 'Failed to fetch my payments');
    }
    return json.data;
  }

  async getPaymentsByStatus(status: PaymentStatus): Promise<Payment[]> {
      const res = await fetch(`${API_BASE_URL}/status/${status}`, {
      credentials: 'include',
      headers: await getDefaultHeaders(),
    });
    
    if (!res.ok) {
      const errorJson = await res.json().catch(() => null);
      throw new Error(errorJson?.statusMessage || `Failed to fetch payments by status: ${res.statusText}`);
    }
    
    const json: ResponseDto<Payment[]> = await res.json();
    if (!json.isSuccess || !json.data) {
      throw new Error(json.statusMessage || 'Failed to fetch payments by status');
    }
    return json.data;
  }

  async getPaymentsByDateRange(startDate: string, endDate: string): Promise<Payment[]> {
      const res = await fetch(`${API_BASE_URL}/date-range?startDate=${startDate}&endDate=${endDate}`, {
      credentials: 'include',
      headers: await getDefaultHeaders(),
    });
    
    if (!res.ok) {
      const errorJson = await res.json().catch(() => null);
      throw new Error(errorJson?.statusMessage || `Failed to fetch payments by date range: ${res.statusText}`);
    }
    
    const json: ResponseDto<Payment[]> = await res.json();
    if (!json.isSuccess || !json.data) {
      throw new Error(json.statusMessage || 'Failed to fetch payments by date range');
    }
    return json.data;
  }

  async getPaginatedPayments(page: number = 1, pageSize: number = 50): Promise<Payment[]> {
      const res = await fetch(`${API_BASE_URL}/paginated?page=${page}&pageSize=${pageSize}`, {
      credentials: 'include',
      headers: await getDefaultHeaders(),
    });
    
    if (!res.ok) {
      const errorJson = await res.json().catch(() => null);
      throw new Error(errorJson?.statusMessage || `Failed to fetch paginated payments: ${res.statusText}`);
    }
    
    const json: ResponseDto<Payment[]> = await res.json();
    if (!json.isSuccess || !json.data) {
      throw new Error(json.statusMessage || 'Failed to fetch paginated payments');
    }
    return json.data;
  }

  async updatePaymentStatus(id: number, data: UpdatePaymentStatus): Promise<PaymentResponse> {
      const res = await fetch(`${API_BASE_URL}/${id}/status`, {
      method: 'PUT',
      credentials: 'include',
      headers: await getDefaultHeaders(),
      body: JSON.stringify(data)
    });
    
    if (!res.ok) {
      const errorJson = await res.json().catch(() => null);
      throw new Error(errorJson?.statusMessage || `Failed to update payment status: ${res.statusText}`);
    }
    
    const json: ResponseDto<PaymentResponse> = await res.json();
    if (!json.isSuccess || !json.data) {
      throw new Error(json.statusMessage || 'Failed to update payment status');
    }
    return json.data;
  }

  async refundPayment(id: number, data: RefundPayment): Promise<PaymentResponse> {
      const res = await fetch(`${API_BASE_URL}/${id}/refund`, {
      method: 'POST',
      credentials: 'include',
      headers: await getDefaultHeaders(),
      body: JSON.stringify(data)
    });
    
    if (!res.ok) {
      const errorJson = await res.json().catch(() => null);
      throw new Error(errorJson?.statusMessage || `Failed to refund payment: ${res.statusText}`);
    }
    
    const json: ResponseDto<PaymentResponse> = await res.json();
    if (!json.isSuccess || !json.data) {
      throw new Error(json.statusMessage || 'Failed to refund payment');
    }
    return json.data;
  }

  async deletePayment(id: number): Promise<boolean> {
      const res = await fetch(`${API_BASE_URL}/${id}`, {
      method: 'DELETE',
      credentials: 'include',
      headers: await getDefaultHeaders()
    });
    
    if (!res.ok) {
      const errorJson = await res.json().catch(() => null);
      throw new Error(errorJson?.statusMessage || `Failed to delete payment: ${res.statusText}`);
    }
    
    const json: ResponseDto<boolean> = await res.json();
    return json.isSuccess && json.data;
  }

  async getTotalPaymentCount(): Promise<number> {
      const res = await fetch(`${API_BASE_URL}/stats/count`, {
      credentials: 'include',
      headers: await getDefaultHeaders(),
    });
    
    if (!res.ok) {
      const errorJson = await res.json().catch(() => null);
      throw new Error(errorJson?.statusMessage || `Failed to fetch payment count: ${res.statusText}`);
    }
    
    const json: ResponseDto<number> = await res.json();
    if (!json.isSuccess || json.data === undefined) {
      throw new Error(json.statusMessage || 'Failed to fetch payment count');
    }
    return json.data;
  }

  async getTotalAmountByDateRange(startDate: string, endDate: string): Promise<number> {
      const res = await fetch(`${API_BASE_URL}/stats/amount/total?startDate=${startDate}&endDate=${endDate}`, {
      credentials: 'include',
      headers: await getDefaultHeaders(),
    });
    
    if (!res.ok) {
      const errorJson = await res.json().catch(() => null);
      throw new Error(errorJson?.statusMessage || `Failed to fetch total amount: ${res.statusText}`);
    }
    
    const json: ResponseDto<number> = await res.json();
    if (!json.isSuccess || json.data === undefined) {
      throw new Error(json.statusMessage || 'Failed to fetch total amount');
    }
    return json.data;
  }

  async searchPayments(params: PaymentSearchParams): Promise<Payment[]> {
    const searchParams = new URLSearchParams();
    
    if (params.searchTerm) searchParams.append('searchTerm', params.searchTerm);
    if (params.page) searchParams.append('page', params.page.toString());
    if (params.pageSize) searchParams.append('pageSize', params.pageSize.toString());
    
      const res = await fetch(`${API_BASE_URL}/search?${searchParams.toString()}`, {
      credentials: 'include',
      headers: await getDefaultHeaders(),
    });
    
    if (!res.ok) {
      const errorJson = await res.json().catch(() => null);
      throw new Error(errorJson?.statusMessage || `Failed to search payments: ${res.statusText}`);
    }
    
    const json: ResponseDto<Payment[]> = await res.json();
    if (!json.isSuccess || !json.data) {
      throw new Error(json.statusMessage || 'Failed to search payments');
    }
    return json.data;
  }

  async verifyPaymentWithProvider(transactionId: string): Promise<PaymentResponse> {
      const res = await fetch(`${API_BASE_URL}/${transactionId}/verify`, {
      method: 'POST',
      credentials: 'include',
      headers: await getDefaultHeaders()
    });
    
    if (!res.ok) {
      const errorJson = await res.json().catch(() => null);
      throw new Error(errorJson?.statusMessage || `Failed to verify payment: ${res.statusText}`);
    }
    
    const json: ResponseDto<PaymentResponse> = await res.json();
    if (!json.isSuccess || !json.data) {
      throw new Error(json.statusMessage || 'Failed to verify payment');
    }
    return json.data;
  }
} 
