import { Order, OrderStatus } from "../../../types/order";
import { IOrderService } from "./interface";
import { ResponseDto } from "../../../types/responseDto";
import { Payment } from '../../../types/payment';
import { apiRequest } from "../../utils/api";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || 'https://localhost:7105/api';
const API_BASE_ORDER = `${API_BASE_URL}/Orders`;


export class OrderService implements IOrderService {
  

  async getById(orderId: number): Promise<Order> {
    const res: ResponseDto<Order> = await apiRequest(`${API_BASE_ORDER}/${orderId}`, {
 
      requireAuth: true,
    });
    
    if (!res.isSuccess) {
      throw new Error(`Failed to fetch order: ${res.statusMessage}`);
    }
    
    const order: Order = await res.data;
    if (!res.isSuccess || !res.data) {
      throw new Error(res.statusMessage || 'Failed to fetch order');
    }
    return order;
  }

  async getOrderByPaymentId(paymentId: number): Promise<Order> {
    const res: ResponseDto<Payment> = await apiRequest(`${API_BASE_URL}/payments/${paymentId}`, {
      requireAuth: true,
    });
    
    if (!res.isSuccess) {

      throw new Error(`Failed to fetch order by payment ID: ${res.statusMessage}`);
    }
    
    if (!res.data?.orderId) {
      throw new Error('This payment has no associated order');
    }
    return this.getById(res.data.orderId);
  }

  async getUserOrders(userId: number, page: number = 1, pageSize: number = 10): Promise<Order[]> {
    const res: ResponseDto<Order[]> = await apiRequest(`${API_BASE_ORDER}/user/${userId}?page=${page}&pageSize=${pageSize}`, {
      requireAuth: true,
    });
    
    if (!res.isSuccess) {

      throw new Error(`Failed to fetch user orders: ${res.statusMessage}`);
    }
    
    const orders: Order[] = await res.data;
    if (!res.isSuccess || !res.data) {
      throw new Error(res.statusMessage || 'Failed to fetch user orders');
    }
    return orders;
  }

  async getOrdersByStatus(status: OrderStatus, page: number = 1, pageSize: number = 10): Promise<Order[]> {
    const res: ResponseDto<Order[]> = await apiRequest(`${API_BASE_ORDER}/status/${status}?page=${page}&pageSize=${pageSize}`, {
      requireAuth: true,
    });
    
    if (!res.isSuccess) {

      throw new Error(`Failed to fetch orders by status: ${res.statusMessage}`);
    }
    
    const orders: Order[] = await res.data;
    if (!res.isSuccess || !res.data) {
      throw new Error(res.statusMessage || 'Failed to fetch orders by status');
    }
    return orders;
  }

  async updateOrderStatus(orderId: number, status: number): Promise<boolean> {
    const res: ResponseDto<boolean> = await apiRequest(`${API_BASE_ORDER}/${orderId}/update-status`, {
      method: 'PUT',
      requireAuth: true,
      body: JSON.stringify({ newStatus: status })
    });
    
    if (!res.isSuccess) {

      throw new Error(`Failed to update order status: ${res.statusMessage}`);
    }
    
    const result: boolean =  res.isSuccess;
    return result;
  }

  async processOrder(orderId: number): Promise<boolean> {
    const res: ResponseDto<boolean> = await apiRequest(`${API_BASE_ORDER}/${orderId}/process`, {
      method: 'PUT',
      requireAuth: true,

    });
    
    if (!res.isSuccess) {

      throw new Error(`Failed to process order: ${res.statusMessage}`);
    }
    
    const result: boolean =  res.isSuccess;
    return result;
  }

  async getAllOrders(page: number = 1, pageSize: number = 50): Promise<Order[]> {
    const res: ResponseDto<Order[]> = await apiRequest(`${API_BASE_ORDER}/admin/all?page=${page}&pageSize=${pageSize}`, {
      requireAuth: true,
    });
    
    if (!res.isSuccess) {

      throw new Error(`Failed to fetch all orders: ${res.statusMessage}`);
    }
    
    const orders: Order[] = await res.data;
    if (!res.isSuccess || !res.data) {
      throw new Error(res.statusMessage || 'Failed to fetch all orders');
    }
    return orders;
  }

  async cancelOrder(orderId: number, reason: string): Promise<boolean> {
    const res: ResponseDto<boolean> = await apiRequest(`${API_BASE_ORDER}/${orderId}/cancel`, {
      method: 'PUT',
      requireAuth: true,
      body: JSON.stringify({ reason })
    });
    
    if (!res.isSuccess) {
      throw new Error(`Failed to cancel order: ${res.statusMessage}`);
    }
    
    const result: boolean = res.isSuccess;
    return result;
  }

  async refundOrder(orderId: number, reason: string): Promise<boolean> {
    const res: ResponseDto<boolean> = await apiRequest(`${API_BASE_ORDER}/${orderId}/refund`, {
      method: 'PUT',
      requireAuth: true,
      body: JSON.stringify({ reason })
    });
    
    if (!res.isSuccess) {

      throw new Error(`Failed to refund order: ${res.statusMessage}`);
    }
    
    return res.isSuccess;
  }

  async markOrderAsPaid(orderId: number, paymentTransactionId: number, paymentProvider: string): Promise<boolean> {
    const res: ResponseDto<boolean> = await apiRequest(`${API_BASE_ORDER}/${orderId}/mark-paid`, {
      method: 'PUT',
      requireAuth: true,

      body: JSON.stringify({ 
        paymentTransactionId, 
        paymentProvider 
      })
    });
    
    if (!res.isSuccess) {

      throw new Error(`Failed to mark order as paid: ${res.statusMessage}`);
    }
    
    const json: boolean = await res.isSuccess;
    return json;
  }
}
