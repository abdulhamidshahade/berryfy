import { unstable_rethrow } from 'next/navigation';
import Link from 'next/link';
import { notFound } from 'next/navigation';
import { getPaymentById } from '../../../../lib/actions/payment-actions';
import { getOrderByPaymentId } from '../../../../lib/actions/order-actions';
import { PaymentStatus, PaymentMethod } from '../../../../types/payment';
import { OrderStatus } from '../../../../types/order';

interface ReceiptPageProps {
  params: Promise<{
    id: string;
  }>;
}

export default async function ReceiptPage({ params }: ReceiptPageProps) {
  try {
    return await loadReceiptPage({ params });
  } catch (error) {
    unstable_rethrow(error);
    console.error('Error loading payment:', error);
    notFound();
  }
}

async function loadReceiptPage({ params }: ReceiptPageProps) {
  const { id } = await params;
  const paymentId = parseInt(id);

  if (isNaN(paymentId)) {
    notFound();
  }

  
    const payment = await getPaymentById(paymentId);
    if (!payment) {
      notFound();
    }

    let order = null;
    if (payment.orderId) {
      try {
        order = await getOrderByPaymentId(paymentId);
      } catch (error) {
        console.warn('Order not found for payment:', paymentId);
      }
    }

    const getStatusBadgeClass = (status: PaymentStatus) => {
      switch (status) {
        case PaymentStatus.Completed:
          return 'bg-success';
        case PaymentStatus.Processing:
          return 'bg-warning text-dark';
        case PaymentStatus.Failed:
          return 'bg-danger';
        case PaymentStatus.Cancelled:
          return 'bg-secondary';
        case PaymentStatus.Refunded:
          return 'bg-info';
        case PaymentStatus.PartiallyRefunded:
          return 'bg-primary';
        default:
          return 'bg-light text-dark';
      }
    };

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

    return (
      <div className="container py-4">
        <nav aria-label="breadcrumb" className="mb-4">
          <ol className="breadcrumb">
            <li className="breadcrumb-item">
              <Link href="/" className="text-decoration-none">
                <i className="bi bi-house-fill me-2"></i>
                Home
              </Link>
            </li>
            <li className="breadcrumb-item">
              <Link href="/profile" className="text-decoration-none">Profile</Link>
            </li>
            <li className="breadcrumb-item">
              <Link href="/profile/payments" className="text-decoration-none">Payment History</Link>
            </li>
            <li className="breadcrumb-item active" aria-current="page">Receipt</li>
          </ol>
        </nav>

        <div className="d-flex justify-content-between align-items-center mb-4">
          <div>
            <h1 className="h3 fw-bold mb-2">Payment Receipt</h1>
            <p className="text-muted mb-0">Transaction ID: {payment.transactionId}</p>
          </div>
          <div className="d-flex gap-2">
            <Link href="/profile/payments" className="btn btn-outline-secondary">
              <i className="bi bi-arrow-left me-2"></i>
              Back to Payments
            </Link>
            {payment.status === PaymentStatus.Completed && (
              <Link
                href={`/api/receipts/${payment.id}/download`}
                className="btn btn-primary"
                target="_blank"
                rel="noopener noreferrer"
              >
                <i className="bi bi-download me-2"></i>
                Download PDF
              </Link>
            )}
          </div>
        </div>

        <div className="row">
          <div className="col-lg-8">
            <div className="card mb-4">
              <div className="card-header bg-primary text-white">
                <h5 className="mb-0">
                  <i className="bi bi-receipt me-2"></i>
                  Receipt Details
                </h5>
              </div>
              <div className="card-body">
                <div className="row mb-4">
                  <div className="col-md-6">
                    <h6 className="text-muted mb-2">Transaction Information</h6>
                    <p className="mb-1"><strong>Transaction ID:</strong> {payment.transactionId}</p>
                    <p className="mb-1"><strong>Date:</strong> {new Date(payment.createdAt).toLocaleDateString()}</p>
                    <p className="mb-1"><strong>Time:</strong> {new Date(payment.createdAt).toLocaleTimeString()}</p>
                    {payment.completedAt && (
                      <p className="mb-1"><strong>Completed:</strong> {new Date(payment.completedAt).toLocaleDateString()}</p>
                    )}
                  </div>
                  <div className="col-md-6">
                    <h6 className="text-muted mb-2">Payment Method</h6>
                    <p className="mb-1"><strong>Method:</strong> {getMethodText(payment.method)}</p>
                    <p className="mb-1"><strong>Provider:</strong> {payment.provider}</p>
                    {payment.cardLast4 && (
                      <p className="mb-1"><strong>Card:</strong> **** **** **** {payment.cardLast4}</p>
                    )}
                    {payment.cardBrand && (
                      <p className="mb-1"><strong>Brand:</strong> {payment.cardBrand}</p>
                    )}
                  </div>
                </div>

                {payment.payerName && (
                  <div className="row mb-4">
                    <div className="col-12">
                      <h6 className="text-muted mb-2">Billing Information</h6>
                      <p className="mb-1"><strong>Name:</strong> {payment.payerName}</p>
                      {payment.payerEmail && (
                        <p className="mb-1"><strong>Email:</strong> {payment.payerEmail}</p>
                      )}
                      {payment.billingAddress1 && (
                        <div>
                          <p className="mb-1"><strong>Address:</strong></p>
                          <p className="mb-0">{payment.billingAddress1}</p>
                          {payment.billingAddress2 && <p className="mb-0">{payment.billingAddress2}</p>}
                          <p className="mb-0">
                            {payment.billingCity}, {payment.billingState} {payment.billingPostalCode}
                          </p>
                          <p className="mb-0">{payment.billingCountry}</p>
                        </div>
                      )}
                    </div>
                  </div>
                )}

                <div className="border-top pt-4">
                  <div className="row">
                    <div className="col-md-6">
                      <h6 className="text-muted mb-2">Amount Details</h6>
                      <div className="d-flex justify-content-between mb-2">
                        <span>Subtotal:</span>
                        <span>${payment.amount.toFixed(2)}</span>
                      </div>
                      <div className="d-flex justify-content-between mb-2">
                        <span>Processing Fee:</span>
                        <span>${payment.processingFee.toFixed(2)}</span>
                      </div>
                      <div className="d-flex justify-content-between border-top pt-2">
                        <strong>Total Paid:</strong>
                        <strong>${payment.amount.toFixed(2)}</strong>
                      </div>
                      <div className="d-flex justify-content-between">
                        <span className="text-muted">Net Amount:</span>
                        <span className="text-muted">${payment.netAmount.toFixed(2)}</span>
                      </div>
                    </div>
                    <div className="col-md-6">
                      <h6 className="text-muted mb-2">Status</h6>
                      <span className={`badge ${getStatusBadgeClass(payment.status)} fs-6`}>
                        {getStatusText(payment.status)}
                      </span>
                      {payment.notes && (
                        <div className="mt-3">
                          <h6 className="text-muted mb-2">Notes</h6>
                          <p className="mb-0">{payment.notes}</p>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {order && (
              <div className="card">
                <div className="card-header bg-info text-white">
                  <h5 className="mb-0">
                    <i className="bi bi-box-seam me-2"></i>
                    Order Details
                  </h5>
                </div>
                <div className="card-body">
                  <div className="row mb-3">
                    <div className="col-md-6">
                      <p className="mb-1"><strong>Order #:</strong> {order.referenceNumber || order.id}</p>
                      <p className="mb-1"><strong>Status:</strong> {Object.keys(OrderStatus)[order.status]}</p>
                    </div>
                    <div className="col-md-6">
                      <p className="mb-1"><strong>Customer:</strong> {order.customerEmail}</p>
                      {order.customerPhone && (
                        <p className="mb-1"><strong>Phone:</strong> {order.customerPhone}</p>
                      )}
                    </div>
                  </div>

                  {order.orderItems && order.orderItems.length > 0 && (
                    <div className="table-responsive">
                      <table className="table table-sm">
                        <thead>
                          <tr>
                            <th>Product</th>
                            <th>Quantity</th>
                            <th>Price</th>
                            <th>Total</th>
                          </tr>
                        </thead>
                        <tbody>
                          {order.orderItems.map(item => (
                            <tr key={item.id}>
                              <td>{item.productName}</td>
                              <td>{item.quantity}</td>
                              <td>${item.unitPrice.toFixed(2)}</td>
                              <td>${item.totalPrice.toFixed(2)}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}

                  <div className="d-flex justify-content-end">
                    <Link
                      href={`/orders/${order.id}`}
                      className="btn btn-outline-info"
                    >
                      <i className="bi bi-eye me-2"></i>
                      View Full Order
                    </Link>
                  </div>
                </div>
              </div>
            )}
          </div>

          <div className="col-lg-4">
            <div className="card">
              <div className="card-header">
                <h5 className="mb-0">Quick Actions</h5>
              </div>
              <div className="card-body">
                <div className="d-grid gap-2">
                  {payment.status === PaymentStatus.Completed && (
                    <Link
                      href={`/api/receipts/${payment.id}/download`}
                      className="btn btn-primary"
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      <i className="bi bi-download me-2"></i>
                      Download Receipt
                    </Link>
                  )}
                  
                  {order && (
                    <Link
                      href={`/orders/${order.id}`}
                      className="btn btn-outline-secondary"
                    >
                      <i className="bi bi-box-seam me-2"></i>
                      View Order
                    </Link>
                  )}
                  
                  <Link
                    href="/profile/payments"
                    className="btn btn-outline-primary"
                  >
                    <i className="bi bi-arrow-left me-2"></i>
                    Back to Payments
                  </Link>
                </div>
              </div>
            </div>

            <div className="card mt-4">
              <div className="card-header">
                <h5 className="mb-0">Need Help?</h5>
              </div>
              <div className="card-body">
                <p className="card-text mb-3">
                  Have questions about this payment? Our support team is here to help.
                </p>
                <div className="d-grid gap-2">
                  <a href="mailto:support@berryfy.org" className="btn btn-outline-primary btn-sm">
                    <i className="bi bi-envelope me-2"></i>
                    Contact Support
                  </a>
                  <Link href="/help" className="btn btn-outline-secondary btn-sm">
                    <i className="bi bi-question-circle me-2"></i>
                    Help Center
                  </Link>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  
} 