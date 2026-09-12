import { notFound } from 'next/navigation';
import Link from 'next/link';
import { getPaymentById, deletePayment } from '../../../../../lib/actions/payment-actions';
import { PaymentStatus, PaymentMethod, Payment } from '../../../../../types/payment';

interface Props {
  params: Promise<{
    id: string;
  }>;
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
}

export default async function DeletePaymentPage({ params, searchParams }: Props) {
  const resolvedParams = await params;
  const paymentId = parseInt(resolvedParams.id);

  if (isNaN(paymentId)) {
    notFound();
  }

  let payment: Payment;
  try {
    payment = await getPaymentById(paymentId);
  } catch (error) {
    console.error("Failed to load payment:", error);
    notFound();
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

  async function handleDeletePayment(formData: FormData) {
    'use server'
    await deletePayment(formData);
  }

  return (
    <div className="container-fluid py-4">
      <div className="row mb-4">
        <div className="col-12">
          <div className="d-flex align-items-center">
            <Link
              href={`/admin/payments/${payment.id}`}
              className="btn btn-outline-secondary me-3"
              title="Back to Payment Details"
            >
              <i className="bi bi-arrow-left"></i>
            </Link>
            <div>
              <h1 className="h2 mb-1">
                <i className="bi bi-trash me-2 text-danger"></i>
                Delete Payment
              </h1>
              <p className="text-muted mb-0">
                Transaction ID: {payment.transactionId}
              </p>
            </div>
          </div>
        </div>
      </div>

      <div className="row justify-content-center">
        <div className="col-lg-8 col-xl-6">
          <div className="card shadow-sm border-0 mb-4">
            <div className="card-header bg-white py-3">
              <h5 className="card-title mb-0">
                <i className="bi bi-info-circle me-2"></i>
                Payment Information
              </h5>
            </div>
            <div className="card-body">
              <div className="row g-3">
                <div className="col-md-6">
                  <label className="form-label fw-medium">Amount</label>
                  <p className="form-control-plaintext h5 text-success">
                    ${payment.amount.toFixed(2)} {payment.currency}
                  </p>
                </div>
                
                <div className="col-md-6">
                  <label className="form-label fw-medium">Status</label>
                  <p className="form-control-plaintext">
                    <span className={`badge bg-${payment.status === PaymentStatus.Completed ? 'success' : 
                      payment.status === PaymentStatus.Failed ? 'danger' : 
                      payment.status === PaymentStatus.Processing ? 'warning' : 'secondary'}`}>
                      {getStatusText(payment.status)}
                    </span>
                  </p>
                </div>

                <div className="col-md-6">
                  <label className="form-label fw-medium">Payment Method</label>
                  <p className="form-control-plaintext">{getMethodText(payment.method)}</p>
                </div>

                <div className="col-md-6">
                  <label className="form-label fw-medium">Provider</label>
                  <p className="form-control-plaintext">{payment.provider}</p>
                </div>

                <div className="col-md-6">
                  <label className="form-label fw-medium">Customer</label>
                  <p className="form-control-plaintext">{payment.payerName || 'N/A'}</p>
                </div>

                <div className="col-md-6">
                  <label className="form-label fw-medium">Email</label>
                  <p className="form-control-plaintext">{payment.payerEmail || 'N/A'}</p>
                </div>

                <div className="col-md-6">
                  <label className="form-label fw-medium">Created At</label>
                  <p className="form-control-plaintext">
                    {new Date(payment.createdAt).toLocaleString()}
                  </p>
                </div>

                <div className="col-md-6">
                  <label className="form-label fw-medium">Last Updated</label>
                  <p className="form-control-plaintext">
                    {new Date(payment.updatedAt).toLocaleString()}
                  </p>
                </div>
              </div>
            </div>
          </div>

          <div className="card border-danger">
            <div className="card-header bg-danger text-white py-3">
              <h5 className="card-title mb-0">
                <i className="bi bi-exclamation-triangle me-2"></i>
                Delete Confirmation
              </h5>
            </div>
            <div className="card-body p-4">
              <div className="alert alert-danger" role="alert">
                <h6 className="alert-heading">
                  <i className="bi bi-exclamation-triangle-fill me-2"></i>
                  Warning: This action cannot be undone!
                </h6>
                <p className="mb-0">
                  You are about to permanently delete this payment record. This action will remove all payment data including transaction details, customer information, and billing details.
                </p>
              </div>

              <div className="mb-4">
                <h6 className="fw-medium mb-3">What will be deleted:</h6>
                <ul className="list-unstyled">
                  <li className="mb-2">
                    <i className="bi bi-check-circle text-danger me-2"></i>
                    Payment transaction record
                  </li>
                  <li className="mb-2">
                    <i className="bi bi-check-circle text-danger me-2"></i>
                    Customer billing information
                  </li>
                  <li className="mb-2">
                    <i className="bi bi-check-circle text-danger me-2"></i>
                    Payment method details
                  </li>
                  <li className="mb-2">
                    <i className="bi bi-check-circle text-danger me-2"></i>
                    Transaction history
                  </li>
                  <li className="mb-2">
                    <i className="bi bi-check-circle text-danger me-2"></i>
                    All associated metadata and notes
                  </li>
                </ul>
              </div>

              <form action={handleDeletePayment}>
                <input type="hidden" name="id" value={payment.id} />
                
                <div className="mb-4">
                  <label htmlFor="confirmation" className="form-label fw-medium">
                    Type &quot;DELETE&quot; to confirm <span className="text-danger">*</span>
                  </label>
                  <input 
                    type="text" 
                    name="confirmation" 
                    id="confirmation" 
                    className="form-control" 
                    placeholder="Type DELETE to confirm"
                    pattern="DELETE"
                    required
                  />
                  <div className="form-text">
                    Type &quot;DELETE&quot; (in uppercase) to confirm that you want to permanently delete this payment.
                  </div>
                </div>

                <div className="d-flex gap-2">
                  <button type="submit" className="btn btn-danger">
                    <i className="bi bi-trash me-2"></i>
                    Delete Payment
                  </button>
                  <Link
                    href={`/admin/payments/${payment.id}`}
                    className="btn btn-outline-secondary"
                  >
                    <i className="bi bi-x-circle me-2"></i>
                    Cancel
                  </Link>
                </div>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
} 