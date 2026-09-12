import Link from 'next/link';
import { getPaymentByTransactionId } from '../../../lib/actions/payment-actions';

interface PaymentSuccessPageProps {
  searchParams: Promise<{
    transactionId?: string;
    orderId?: string;
  }>;
}

export default async function PaymentSuccessPage({ searchParams }: PaymentSuccessPageProps) {
  const params = await searchParams;
  const transactionId = params.transactionId;
  const orderId = params.orderId;

  let payment = null;
  if (transactionId) {
    try {
      payment = await getPaymentByTransactionId(transactionId);
    } catch (error) {
      console.error('Error fetching payment:', error);
    }
  }

  return (
    <div className="min-vh-100 bg-light d-flex align-items-center">
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-lg-8 col-xl-6">
            <div className="card shadow-lg border-0">
              <div className="card-body text-center p-5">
                 <div className="mb-4">
                   <div className="d-inline-flex align-items-center justify-content-center bg-success bg-opacity-10 rounded-circle" style={{width: '80px', height: '80px'}}>
                     <i className="bi bi-check-circle-fill text-success" style={{fontSize: '3rem'}}></i>
                   </div>
                 </div>

                <h1 className="display-6 fw-bold text-dark mb-3">
                  Thank you for purchasing!
                </h1>
                
                <p className="text-muted mb-4 fs-5">
                  Your payment has been processed successfully. We appreciate your business!
                </p>

                {payment && (
                  <div className="bg-light rounded-3 p-4 mb-4">
                    <h5 className="fw-semibold mb-3">Payment Details</h5>
                    <div className="row g-3 text-start">
                      <div className="col-sm-6">
                        <small className="text-muted d-block">Transaction ID</small>
                        <span className="fw-medium">{payment.transactionId}</span>
                      </div>
                      <div className="col-sm-6">
                        <small className="text-muted d-block">Amount Paid</small>
                        <span className="fw-medium">${payment.amount.toFixed(2)} {payment.currency}</span>
                      </div>
                      <div className="col-sm-6">
                        <small className="text-muted d-block">Payment Method</small>
                        <span className="fw-medium">
                          {payment.method === 0 ? 'Credit Card' : 
                           payment.method === 1 ? 'Debit Card' : 
                           payment.method === 2 ? 'PayPal' : 'Other'}
                          {payment.cardLast4 && ` ending in ${payment.cardLast4}`}
                        </span>
                      </div>
                      <div className="col-sm-6">
                        <small className="text-muted d-block">Payment Date</small>
                        <span className="fw-medium">
                          {new Date(payment.completedAt || payment.createdAt).toLocaleDateString()}
                        </span>
                      </div>
                    </div>
                  </div>
                )}

                {orderId && (
                  <div className="bg-primary bg-opacity-10 rounded-3 p-4 mb-4">
                    <h5 className="fw-semibold mb-2 text-primary">Order Confirmation</h5>
                    <p className="mb-2">Your order has been confirmed!</p>
                    <p className="mb-0">
                      <strong>Order ID:</strong> {orderId}
                    </p>
                  </div>
                )}

                <div className="border rounded-3 p-4 mb-4 text-start">
                  <h5 className="fw-semibold mb-3">What happens next?</h5>
                  <div className="d-flex align-items-start mb-3">
                    <div className="bg-primary bg-opacity-10 rounded-circle d-flex align-items-center justify-content-center me-3" style={{width: '32px', height: '32px', minWidth: '32px'}}>
                      <span className="fw-bold text-primary small">1</span>
                    </div>
                    <div>
                      <h6 className="mb-1">Confirmation Email</h6>
                      <p className="text-muted mb-0 small">You&apos;ll receive a confirmation email with your receipt and order details.</p>
                    </div>
                  </div>
                  <div className="d-flex align-items-start mb-3">
                    <div className="bg-primary bg-opacity-10 rounded-circle d-flex align-items-center justify-content-center me-3" style={{width: '32px', height: '32px', minWidth: '32px'}}>
                      <span className="fw-bold text-primary small">2</span>
                    </div>
                    <div>
                      <h6 className="mb-1">Order Processing</h6>
                      <p className="text-muted mb-0 small">Your order will be processed and prepared for shipment.</p>
                    </div>
                  </div>
                  <div className="d-flex align-items-start">
                    <div className="bg-primary bg-opacity-10 rounded-circle d-flex align-items-center justify-content-center me-3" style={{width: '32px', height: '32px', minWidth: '32px'}}>
                      <span className="fw-bold text-primary small">3</span>
                    </div>
                    <div>
                      <h6 className="mb-1">Shipping Updates</h6>
                      <p className="text-muted mb-0 small">You&apos;ll receive tracking information once your order ships.</p>
                    </div>
                  </div>
                </div>

                <div className="d-grid gap-3">
                  {orderId && (
                    <Link href={`/orders`} className="btn btn-primary btn-lg d-flex align-items-center justify-content-center">
                      <i className="bi bi-receipt me-2"></i>
                      View Order Details
                      <i className="bi bi-arrow-right ms-2"></i>
                    </Link>
                  )}
                  
                  <div className="row g-2">
                    <div className="col-sm-6">
                      <Link href="/products" className="btn btn-outline-primary w-100">
                        Continue Shopping
                      </Link>
                    </div>
                    <div className="col-sm-6">
                      <Link href="/profile" className="btn btn-outline-secondary w-100">
                        View Account
                      </Link>
                    </div>
                  </div>
                </div>

                <div className="mt-4 pt-4 border-top text-muted">
                  <p className="mb-2 small">
                    <strong>Need help?</strong>
                  </p>
                  <p className="mb-0 small">
                    Contact our support team at{' '}
                    <a href="mailto:support@berryfy.com" className="text-decoration-none">
                      support@berryfy.com
                    </a>
                    {' '}or visit our{' '}
                    <Link href="/help" className="text-decoration-none">
                      Help Center
                    </Link>
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
} 