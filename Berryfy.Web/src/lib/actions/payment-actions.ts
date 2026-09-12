'use server'

import { revalidatePath } from 'next/cache';
import { redirect } from 'next/navigation';
import { PaymentService } from '../services/payment/service';
import { IPaymentService } from '../services/payment/interface';
import { CreatePayment, PaymentMethod, PaymentStatus } from '../../types/payment';
import { savePaymentBillingData } from '../utils/payment-storage';

const paymentService: IPaymentService = new PaymentService();

export async function processPayment(formData: FormData) {
  try {
    const orderId = formData.get("orderId") ? parseInt(formData.get("orderId") as string) : undefined;
    const method = parseInt(formData.get("method") as string) as PaymentMethod;
    const provider = formData.get("provider") as string;
    const amount = parseFloat(formData.get("amount") as string);
    const currency = (formData.get("currency") as string) || "USD";
    const payerEmail = formData.get("payerEmail") as string;
    const payerName = formData.get("payerName") as string;
    const billingAddress1 = formData.get("billingAddress1") as string;
    const billingAddress2 = formData.get("billingAddress2") as string;
    const billingCity = formData.get("billingCity") as string;
    const billingState = formData.get("billingState") as string;
    const billingPostalCode = formData.get("billingPostalCode") as string;
    const billingCountry = formData.get("billingCountry") as string;
    const cardLast4 = formData.get("cardLast4") as string;
    const cardBrand = formData.get("cardBrand") as string;

    if (isNaN(method) || method < 0 || method > 8) {
      throw new Error(`Invalid payment method: ${formData.get("method")}. Must be a valid PaymentMethod enum value (0-8).`);
    }

    if (!provider || provider.trim() === '') {
      throw new Error("Payment provider is required");
    }

    if (!payerEmail || !payerName || !billingAddress1 || !billingCity || !billingState || !billingPostalCode || !billingCountry) {
      const missingFields = [];
      if (!payerEmail) missingFields.push('payerEmail');
      if (!payerName) missingFields.push('payerName');
      if (!billingAddress1) missingFields.push('billingAddress1');
      if (!billingCity) missingFields.push('billingCity');
      if (!billingState) missingFields.push('billingState');
      if (!billingPostalCode) missingFields.push('billingPostalCode');
      if (!billingCountry) missingFields.push('billingCountry');
      throw new Error(`Missing required payment information: ${missingFields.join(', ')}`);
    }

    if (isNaN(amount) || amount <= 0) {
      throw new Error(`Invalid amount: ${formData.get("amount")}. Amount must be a number greater than 0`);
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(payerEmail)) {
      throw new Error(`Invalid email format: ${payerEmail}`);
    }

    const paymentData: CreatePayment = {
      orderId,
      method,
      provider,
      amount,
      currency,
      payerEmail,
      payerName,
      billingAddress1,
      billingAddress2: billingAddress2 || undefined,
      billingCity,
      billingState,
      billingPostalCode,
      billingCountry,
      cardLast4: cardLast4 || undefined,
      cardBrand: cardBrand || undefined
    };

    // Save payment billing data to cookies for future use (excluding card details for security)
    console.log('Saving payment billing data to cookies...');
    await savePaymentBillingData({
      payerName,
      payerEmail,
      billingAddress1,
      billingAddress2,
      billingCity,
      billingState,
      billingPostalCode,
      billingCountry
    });

    const result = await paymentService.processPayment(paymentData);

    if (result.success) {
      revalidatePath("/payment");
      revalidatePath("/orders");
      const params = new URLSearchParams({ orderId: String(orderId) });
      if (result.transactionId) params.set('transactionId', result.transactionId);
      redirect(`/payment/success?${params}`);
    } else {
      throw new Error(result.message || "Payment processing failed");
    }
  } catch (error) {
    if (error instanceof Error && error.message === "NEXT_REDIRECT") {
      throw error;
    }
    console.error("Error processing payment:", error);
    throw new Error(
      error instanceof Error ? error.message : "Failed to process payment"
    );
  }
}

export async function getMyPayments(page: number = 1, pageSize: number = 10) {
  try {
    return await paymentService.getMyPayments(page, pageSize);
  } catch (error) {
    console.error("Error getting my payments:", error);
    throw new Error("Failed to fetch payments");
  }
}

export async function getPaymentById(id: number) {
  try {
    return await paymentService.getPaymentById(id);
  } catch (error) {
    console.error("Error getting payment by ID:", error);
    throw new Error("Failed to fetch payment");
  }
}

export async function getPaymentByTransactionId(transactionId: string) {
  try {
    return await paymentService.getPaymentByTransactionId(transactionId);
  } catch (error) {
    console.error("Error getting payment by transaction ID:", error);
    throw new Error("Failed to fetch payment");
  }
}

export async function getPaymentByOrderId(orderId: number) {
  try {
    return await paymentService.getPaymentByOrderId(orderId);
  } catch (error) {
    console.error("Error getting payment by order ID:", error);
    throw new Error("Failed to fetch payment");
  }
}


export async function getAllPayments() {
  try {
    return await paymentService.getAllPayments();
  } catch (error) {
    console.error("Error getting all payments:", error);
    throw new Error("Failed to fetch payments");
  }
}

export async function getPaginatedPayments(page: number = 1, pageSize: number = 50) {
  try {
    return await paymentService.getPaginatedPayments(page, pageSize);
  } catch (error) {
    console.error("Error getting paginated payments:", error);
    throw new Error("Failed to fetch payments");
  }
}

export async function getPaymentsByStatus(status: PaymentStatus) {
  try {
    return await paymentService.getPaymentsByStatus(status);
  } catch (error) {
    console.error("Error getting payments by status:", error);
    throw new Error("Failed to fetch payments by status");
  }
}

export async function getPaymentsByDateRange(startDate: string, endDate: string) {
  try {
    return await paymentService.getPaymentsByDateRange(startDate, endDate);
  } catch (error) {
    console.error("Error getting payments by date range:", error);
    throw new Error("Failed to fetch payments by date range");
  }
}

export async function updatePaymentStatus(formData: FormData) {
  try {
    const id = parseInt(formData.get("id") as string);
    const status = parseInt(formData.get("status") as string) as PaymentStatus;
    const notes = formData.get("notes") as string;

    const result = await paymentService.updatePaymentStatus(id, { status, notes: notes || undefined });

    if (result.success) {
      revalidatePath("/admin/payments");
      return result;
    } else {
      throw new Error(result.message || "Failed to update payment status");
    }
  } catch (error) {
    console.error("Error updating payment status:", error);
    throw new Error(
      error instanceof Error ? error.message : "Failed to update payment status"
    );
  }
}

export async function refundPayment(formData: FormData) {
  try {
    const id = parseInt(formData.get("id") as string);
    const refundAmount = formData.get("refundAmount") ? parseFloat(formData.get("refundAmount") as string) : undefined;
    const reason = formData.get("reason") as string;

    const result = await paymentService.refundPayment(id, { refundAmount, reason: reason || undefined });

    if (result.success) {
      revalidatePath("/admin/payments");
      return result;
    } else {
      throw new Error(result.message || "Failed to refund payment");
    }
  } catch (error) {
    console.error("Error refunding payment:", error);
    throw new Error(
      error instanceof Error ? error.message : "Failed to refund payment"
    );
  }
}

export async function deletePayment(formData: FormData) {
  try {
    const id = parseInt(formData.get("id") as string);

    const success = await paymentService.deletePayment(id);

    if (success) {
      revalidatePath("/admin/payments");
      return { success: true, message: "Payment deleted successfully" };
    } else {
      throw new Error("Failed to delete payment");
    }
  } catch (error) {
    console.error("Error deleting payment:", error);
    throw new Error(
      error instanceof Error ? error.message : "Failed to delete payment"
    );
  }
}

export async function searchPayments(formData: FormData) {
  try {
    const searchTerm = formData.get("searchTerm") as string;
    const page = formData.get("page") ? parseInt(formData.get("page") as string) : 1;
    const pageSize = formData.get("pageSize") ? parseInt(formData.get("pageSize") as string) : 50;

    return await paymentService.searchPayments({ searchTerm, page, pageSize });
  } catch (error) {
    console.error("Error searching payments:", error);
    throw new Error("Failed to search payments");
  }
}

export async function verifyPaymentWithProvider(transactionId: string) {
  try {
    const result = await paymentService.verifyPaymentWithProvider(transactionId);

    if (result.success) {
      revalidatePath("/admin/payments");
      return result;
    } else {
      throw new Error(result.message || "Failed to verify payment");
    }
  } catch (error) {
    console.error("Error verifying payment:", error);
    throw new Error(
      error instanceof Error ? error.message : "Failed to verify payment"
    );
  }
}

export async function getTotalPaymentCount() {
  try {
    return await paymentService.getTotalPaymentCount();
  } catch (error) {
    console.error("Error getting total payment count:", error);
    throw new Error("Failed to fetch payment count");
  }
}

export async function getTotalAmountByDateRange(startDate: string, endDate: string) {
  try {
    return await paymentService.getTotalAmountByDateRange(startDate, endDate);
  } catch (error) {
    console.error("Error getting total amount by date range:", error);
    throw new Error("Failed to fetch total amount");
  }
}
