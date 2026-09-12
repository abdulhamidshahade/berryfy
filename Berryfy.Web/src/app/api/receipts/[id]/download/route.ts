import { NextRequest, NextResponse } from 'next/server';
import { getPaymentById } from '../../../../../lib/actions/payment-actions';
import { getOrderByPaymentId } from '../../../../../lib/actions/order-actions';
import { PaymentStatus, PaymentMethod } from '../../../../../types/payment';
import { OrderStatus } from '../../../../../types/order';
import { escapeHtml } from '../../../../../lib/utils/escape-html';

export async function GET(
  request: NextRequest,
  { params }: { params: Promise<{ id: string }> }
) {
  try {
    const { id } = await params;
    const paymentId = parseInt(id);

    if (isNaN(paymentId)) {
      return NextResponse.json(
        { error: 'Invalid payment ID' },
        { status: 400 }
      );
    }

    const payment = await getPaymentById(paymentId);
    if (!payment) {
      return NextResponse.json(
        { error: 'Payment not found' },
        { status: 404 }
      );
    }

    if (payment.status !== PaymentStatus.Completed) {
      return NextResponse.json(
        { error: 'Receipt not available for incomplete payments' },
        { status: 400 }
      );
    }

    let order = null;
    if (payment.orderId) {
      try {
        order = await getOrderByPaymentId(paymentId);
      } catch (error) {
        console.warn('Order not found for payment:', paymentId);
      }
    }


    const getStatusText = (status: PaymentStatus) => {
      switch (status) {
        case PaymentStatus.Pending:
          return 'Pending';
        case PaymentStatus.Processing:
          return 'Processing';
        case PaymentStatus.Completed:
          return 'Completed';
        case PaymentStatus.Failed:
          return 'Failed';
        case PaymentStatus.Cancelled:
          return 'Cancelled';
        case PaymentStatus.Refunded:
          return 'Refunded';
        case PaymentStatus.PartiallyRefunded:
          return 'Partially Refunded';
        case PaymentStatus.Disputed:
          return 'Disputed';
        case PaymentStatus.Expired:
          return 'Expired';
        default:
          return 'Unknown';
      }
    };

    const getMethodText = (method: PaymentMethod) => {
      switch (method) {
        case PaymentMethod.CreditCard:
          return 'Credit Card';
        case PaymentMethod.DebitCard:
          return 'Debit Card';
        case PaymentMethod.PayPal:
          return 'PayPal';
        case PaymentMethod.BankTransfer:
          return 'Bank Transfer';
        case PaymentMethod.DigitalWallet:
          return 'Digital Wallet';
        case PaymentMethod.Cryptocurrency:
          return 'Cryptocurrency';
        case PaymentMethod.GiftCard:
          return 'Gift Card';
        case PaymentMethod.StoreCredit:
          return 'Store Credit';
        case PaymentMethod.CashOnDelivery:
          return 'Cash on Delivery';
        default:
          return 'Other';
      }
    };

    const getOrderStatusText = (status: OrderStatus) => {
      return Object.keys(OrderStatus)[status] || 'Unknown';
    };

    const receiptHtml = `
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Receipt - ${escapeHtml(payment.transactionId)}</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            max-width: 800px;
            margin: 0 auto;
            padding: 20px;
            line-height: 1.6;
            color: #333;
        }
        .header {
            text-align: center;
            border-bottom: 2px solid #0066cc;
            padding-bottom: 20px;
            margin-bottom: 30px;
        }
        .company-name {
            font-size: 28px;
            font-weight: bold;
            color: #0066cc;
            margin-bottom: 10px;
        }
        .receipt-title {
            font-size: 20px;
            color: #666;
        }
        .receipt-info {
            background-color: #f8f9fa;
            padding: 20px;
            border-radius: 8px;
            margin-bottom: 30px;
        }
        .info-row {
            display: flex;
            justify-content: space-between;
            margin-bottom: 10px;
        }
        .info-label {
            font-weight: bold;
            color: #555;
        }
        .status-badge {
            display: inline-block;
            padding: 4px 12px;
            border-radius: 4px;
            color: white;
            font-size: 14px;
            font-weight: bold;
        }
        .status-completed {
            background-color: #28a745;
        }
        .section {
            margin-bottom: 30px;
        }
        .section-title {
            font-size: 18px;
            font-weight: bold;
            color: #0066cc;
            border-bottom: 1px solid #ddd;
            padding-bottom: 10px;
            margin-bottom: 15px;
        }
        .table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 20px;
        }
        .table th,
        .table td {
            padding: 12px;
            text-align: left;
            border-bottom: 1px solid #ddd;
        }
        .table th {
            background-color: #f8f9fa;
            font-weight: bold;
            color: #555;
        }
        .amount-summary {
            background-color: #f8f9fa;
            padding: 20px;
            border-radius: 8px;
        }
        .amount-row {
            display: flex;
            justify-content: space-between;
            margin-bottom: 10px;
        }
        .total-row {
            font-size: 18px;
            font-weight: bold;
            border-top: 2px solid #0066cc;
            padding-top: 10px;
            margin-top: 10px;
        }
        .footer {
            text-align: center;
            margin-top: 40px;
            padding-top: 20px;
            border-top: 1px solid #ddd;
            color: #666;
            font-size: 14px;
        }
        @media print {
            body {
                padding: 0;
            }
        }
    </style>
</head>
<body>
    <div class="header">
        <div class="company-name">Berryfy</div>
        <div class="receipt-title">Payment Receipt</div>
    </div>

    <div class="receipt-info">
        <div class="info-row">
            <span class="info-label">Transaction ID:</span>
            <span>${escapeHtml(payment.transactionId)}</span>
        </div>
        <div class="info-row">
            <span class="info-label">Date:</span>
            <span>${new Date(payment.createdAt).toLocaleDateString()} ${new Date(payment.createdAt).toLocaleTimeString()}</span>
        </div>
        <div class="info-row">
            <span class="info-label">Status:</span>
            <span class="status-badge status-completed">${getStatusText(payment.status)}</span>
        </div>
        ${payment.completedAt ? `
        <div class="info-row">
            <span class="info-label">Completed:</span>
            <span>${new Date(payment.completedAt).toLocaleDateString()}</span>
        </div>
        ` : ''}
    </div>

    <div class="section">
        <div class="section-title">Payment Information</div>
        <div class="info-row">
            <span class="info-label">Payment Method:</span>
            <span>${getMethodText(payment.method)}</span>
        </div>
        <div class="info-row">
            <span class="info-label">Provider:</span>
            <span>${escapeHtml(payment.provider)}</span>
        </div>
        ${payment.cardLast4 ? `
        <div class="info-row">
            <span class="info-label">Card:</span>
            <span>**** **** **** ${escapeHtml(payment.cardLast4)}</span>
        </div>
        ` : ''}
        ${payment.cardBrand ? `
        <div class="info-row">
            <span class="info-label">Card Brand:</span>
            <span>${escapeHtml(payment.cardBrand)}</span>
        </div>
        ` : ''}
    </div>

    ${payment.payerName ? `
    <div class="section">
        <div class="section-title">Billing Information</div>
        <div class="info-row">
            <span class="info-label">Name:</span>
            <span>${escapeHtml(payment.payerName)}</span>
        </div>
        ${payment.payerEmail ? `
        <div class="info-row">
            <span class="info-label">Email:</span>
            <span>${escapeHtml(payment.payerEmail)}</span>
        </div>
        ` : ''}
        ${payment.billingAddress1 ? `
        <div class="info-row">
            <span class="info-label">Address:</span>
            <span>
                ${escapeHtml(payment.billingAddress1)}${payment.billingAddress2 ? ', ' + escapeHtml(payment.billingAddress2) : ''}<br>
                ${escapeHtml(payment.billingCity)}, ${escapeHtml(payment.billingState)} ${escapeHtml(payment.billingPostalCode)}<br>
                ${escapeHtml(payment.billingCountry)}
            </span>
        </div>
        ` : ''}
    </div>
    ` : ''}

    ${order && order.orderItems && order.orderItems.length > 0 ? `
    <div class="section">
        <div class="section-title">Order Details</div>
        <div class="info-row">
            <span class="info-label">Order #:</span>
            <span>${escapeHtml(order.referenceNumber || order.id)}</span>
        </div>
        <div class="info-row">
            <span class="info-label">Order Status:</span>
            <span>${getOrderStatusText(order.status)}</span>
        </div>
        
        <table class="table">
            <thead>
                <tr>
                    <th>Product</th>
                    <th>Quantity</th>
                    <th>Unit Price</th>
                    <th>Total</th>
                </tr>
            </thead>
            <tbody>
                ${order.orderItems.map(item => `
                <tr>
                    <td>${escapeHtml(item.productName)}</td>
                    <td>${item.quantity}</td>
                    <td>$${item.unitPrice.toFixed(2)}</td>
                    <td>$${item.totalPrice.toFixed(2)}</td>
                </tr>
                `).join('')}
            </tbody>
        </table>
    </div>
    ` : ''}

    <div class="section">
        <div class="section-title">Payment Summary</div>
        <div class="amount-summary">
            <div class="amount-row">
                <span>Subtotal:</span>
                <span>$${payment.amount.toFixed(2)}</span>
            </div>
            <div class="amount-row">
                <span>Processing Fee:</span>
                <span>$${payment.processingFee.toFixed(2)}</span>
            </div>
            <div class="amount-row total-row">
                <span>Total Paid:</span>
                <span>$${payment.amount.toFixed(2)}</span>
            </div>
            <div class="amount-row">
                <span style="color: #666;">Net Amount:</span>
                <span style="color: #666;">$${payment.netAmount.toFixed(2)}</span>
            </div>
        </div>
    </div>

    ${payment.notes ? `
    <div class="section">
        <div class="section-title">Notes</div>
        <p>${escapeHtml(payment.notes)}</p>
    </div>
    ` : ''}

    <div class="footer">
        <p>Thank you for your business!</p>
        <p>If you have any questions about this receipt, please contact us at support@Berryfy.com</p>
        <p>Generated on ${new Date().toLocaleDateString()} at ${new Date().toLocaleTimeString()}</p>
    </div>
</body>
</html>
    `;

    //TODO use pdf library in future use!
    return new NextResponse(receiptHtml, {
      status: 200,
      headers: {
        'Content-Type': 'text/html; charset=utf-8',
        'Content-Disposition': `inline; filename="receipt-${paymentId}.html"`,
        'Cache-Control': 'private, no-store',
        'Content-Security-Policy': "sandbox; default-src 'none'; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'",
      },
    });

  } catch (error) {
    console.error('Error generating receipt:', error);
    return NextResponse.json(
      { error: 'Failed to generate receipt' },
      { status: 500 }
    );
  }
}
