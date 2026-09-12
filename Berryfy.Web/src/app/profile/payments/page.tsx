import Link from 'next/link';
import { getMyPayments } from '../../../lib/actions/payment-actions';
import { PaymentStatus, PaymentMethod, Payment } from '../../../types/payment';

interface UserPaymentsPageProps {
  searchParams: Promise<{
    page?: string;
  }>;
}

export default async function UserPaymentsPage({ searchParams }: UserPaymentsPageProps) {
  const params = await searchParams;
  const page = parseInt(params.page || '1');
  const pageSize = 10;

  const payments = await getMyPayments(page, pageSize);

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

  const getMethodIcon = (method: PaymentMethod) => {
    switch (method) {
      case PaymentMethod.CreditCard:
      case PaymentMethod.DebitCard:
        return 'bi-credit-card';
      case PaymentMethod.PayPal:
        return 'bi-paypal';
      case PaymentMethod.BankTransfer:
        return 'bi-bank';
      case PaymentMethod.DigitalWallet:
        return 'bi-wallet2';
      case PaymentMethod.Cryptocurrency:
        return 'bi-currency-bitcoin';
      case PaymentMethod.GiftCard:
        return 'bi-gift';
      case PaymentMethod.StoreCredit:
        return 'bi-credit-card-2-front';
      case PaymentMethod.CashOnDelivery:
        return 'bi-cash-coin';
      default:
        return 'bi-credit-card';
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
          <li className="breadcrumb-item active" aria-current="page">Payment History</li>
        </ol>
      </nav>

      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h1 className="h3 fw-bold mb-2">Payment History</h1>
          <p className="text-muted mb-0">View your payment transactions and receipts</p>
        </div>
        <div className="d-flex gap-2">
          <Link href="/profile" className="btn btn-outline-secondary">
            <i className="bi bi-arrow-left me-2"></i>
            Back to Profile
          </Link>
        </div>
      </div>

      <div className="row g-4 mb-4">
        <div className="col-md-4">
          <div className="card bg-success text-white">
            <div className="card-body">
              <div className="d-flex justify-content-between">
                <div>
                  <h6 className="card-title">Total Spent</h6>
                  <h4 className="mb-0">
                    ${payments
                      .filter((p: Payment) => p.status === PaymentStatus.Completed)
                      .reduce((sum: number, p: Payment) => sum + p.amount, 0)
                      .toFixed(2)}
                  </h4>
                </div>
                <i className="bi bi-currency-dollar display-6 opacity-75"></i>
              </div>
            </div>
          </div>
        </div>

        <div className="col-md-4">
          <div className="card bg-primary text-white">
            <div className="card-body">
              <div className="d-flex justify-content-between">
                <div>
                  <h6 className="card-title">Total Payments</h6>
                  <h4 className="mb-0">{payments.length}</h4>
                </div>
                <i className="bi bi-receipt display-6 opacity-75"></i>
              </div>
            </div>
          </div>
        </div>

        <div className="col-md-4">
          <div className="card bg-warning text-dark">
            <div className="card-body">
              <div className="d-flex justify-content-between">
                <div>
                  <h6 className="card-title">Successful</h6>
                  <h4 className="mb-0">
                    {payments.filter((p: Payment) => p.status === PaymentStatus.Completed).length}
                  </h4>
                </div>
                <i className="bi bi-check-circle display-6 opacity-75"></i>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="card">
        <div className="card-header">
          <h5 className="mb-0">
            <i className="bi bi-clock-history me-2"></i>
            Recent Transactions
          </h5>
        </div>

        <div className="card-body">
          {payments.length > 0 ? (
            <div className="table-responsive">
              <table className="table table-hover">
                <thead>
                  <tr>
                    <th>Transaction</th>
                    <th>Amount</th>
                    <th>Method</th>
                    <th>Status</th>
                    <th>Date</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {payments.map((payment: Payment) => (
                    <tr key={payment.id}>
                      <td>
                        <div className="d-flex flex-column">
                          <span className="fw-medium">{payment.transactionId}</span>
                          {payment.orderId && (
                            <small className="text-muted">
                              Order #{payment.orderId}
                            </small>
                          )}
                        </div>
                      </td>
                      <td>
                        <div className="d-flex flex-column">
                          <span className="fw-bold">${payment.amount.toFixed(2)}</span>
                          <small className="text-muted">{payment.currency}</small>
                        </div>
                      </td>
                      <td>
                        <div className="d-flex align-items-center">
                          <i className={`bi ${getMethodIcon(payment.method)} me-2 text-primary`}></i>
                          <div className="d-flex flex-column">
                            <span className="fw-medium">{getMethodText(payment.method)}</span>
                            {payment.cardLast4 && (
                              <small className="text-muted">****{payment.cardLast4}</small>
                            )}
                          </div>
                        </div>
                      </td>
                      <td>
                        <span className={`badge ${getStatusBadgeClass(payment.status)}`}>
                          {getStatusText(payment.status)}
                        </span>
                      </td>
                      <td>
                        <div className="d-flex flex-column">
                          <span>{new Date(payment.createdAt).toLocaleDateString()}</span>
                          <small className="text-muted">
                            {new Date(payment.createdAt).toLocaleTimeString()}
                          </small>
                        </div>
                      </td>
                      <td>
                        <div className="btn-group btn-group-sm" role="group">
                          <Link
                            href={`/profile/payments/${payment.id}`}
                            className="btn btn-outline-primary"
                            title="View Receipt"
                          >
                            <i className="bi bi-receipt"></i>
                          </Link>
                          
                          {payment.orderId && (
                            <Link
                              href={`/orders/${payment.orderId}`}
                              className="btn btn-outline-secondary"
                              title="View Order"
                            >
                              <i className="bi bi-box-seam"></i>
                            </Link>
                          )}
                          
                          {payment.status === PaymentStatus.Completed && (
                            <Link
                              href={`/api/receipts/${payment.id}/download`}
                              className="btn btn-outline-info"
                              title="Download Receipt"
                              target="_blank"
                              rel="noopener noreferrer"
                            >
                              <i className="bi bi-download"></i>
                            </Link>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="text-center py-5">
              <i className="bi bi-credit-card display-1 text-muted mb-3"></i>
              <h5 className="text-muted">No payment history</h5>
              <p className="text-muted mb-4">You haven&apos;t made any payments yet.</p>
              <Link href="/products" className="btn btn-primary">
                <i className="bi bi-shop me-2"></i>
                Start Shopping
              </Link>
            </div>
          )}
        </div>
      </div>

      <div className="row g-4 mt-4">
        <div className="col-md-6">
          <div className="card">
            <div className="card-body text-center">
              <i className="bi bi-question-circle display-4 text-primary mb-3"></i>
              <h5 className="card-title">Need Help?</h5>
              <p className="card-text text-muted">
                Have questions about a payment? Our support team is here to help.
              </p>
              <a href="mailto:support@berryfy.org" className="btn btn-outline-primary">
                <i className="bi bi-envelope me-2"></i>
                Contact Support
              </a>
            </div>
          </div>
        </div>

        <div className="col-md-6">
          <div className="card">
            <div className="card-body text-center">
              <i className="bi bi-shield-check display-4 text-success mb-3"></i>
              <h5 className="card-title">Secure Payments</h5>
              <p className="card-text text-muted">
                All your payments are protected with bank-level security and encryption.
              </p>
              <Link href="/security" className="btn btn-outline-success">
                <i className="bi bi-info-circle me-2"></i>
                Learn More
              </Link>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}