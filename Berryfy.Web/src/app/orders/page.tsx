import { getCurrentUser } from '../../lib/actions/auth-actions';
import { OrderService } from '../../lib/services/order/service';

export const dynamic = 'force-dynamic';
import { formatCurrency } from '../../lib/utils/formatCurrency';
import { OrderStatus } from '../../types/order';
import { format } from 'date-fns';
import { redirect } from 'next/navigation';
import Link from 'next/link';

export default async function OrdersPage() {
  const orderService = new OrderService();
  const user = await getCurrentUser();
  if (!user) {
    redirect('/auth/login?redirectTo=/orders');
  }
  const orders = await orderService.getUserOrders(user.id);

  const getStatusLabel = (status: OrderStatus): string => {
    switch (status) {
      case OrderStatus.Pending: return 'Pending';
      case OrderStatus.Processing: return 'Processing';
      case OrderStatus.Shipped: return 'Shipped';
      case OrderStatus.Delivered: return 'Delivered';
      case OrderStatus.Completed: return 'Completed';
      case OrderStatus.Cancelled: return 'Cancelled';
      case OrderStatus.Refunded: return 'Refunded';
      default: return 'Unknown';
    }
  };

  const getStatusColor = (status: OrderStatus): string => {
    switch (status) {
      case OrderStatus.Pending: return '#FFA500';    // Orange
      case OrderStatus.Processing: return '#3498db'; // Blue
      case OrderStatus.Shipped: return '#9b59b6';    // Purple
      case OrderStatus.Delivered: return '#2ecc71';  // Green
      case OrderStatus.Completed: return '#27ae60';  // Dark Green
      case OrderStatus.Cancelled: return '#e74c3c';  // Red
      case OrderStatus.Refunded: return '#95a5a6';   // Gray
      default: return '#95a5a6';                      // Gray
    }
  };

  if (orders.length === 0) {
    return (
      <div className="container py-4">
        <div className="text-center py-5">
          <i className="bi bi-bag-x display-1 text-muted mb-4 d-block"></i>
          <h2 className="mb-3">No Orders Found</h2>
          <p className="text-muted mb-4">
            You haven&apos;t placed any orders yet. Start shopping to see your order history here.
          </p>
          <Link href="/products" className="btn btn-primary btn-lg">
            <i className="bi bi-bag-plus me-2"></i>
            Start Shopping
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="container py-4">
      <div className="row">
        <div className="col-12">
          <div className="d-flex justify-content-between align-items-center mb-4">
            <h1 className="display-5 fw-bold mb-0">
              <i className="bi bi-bag-check me-2"></i>
              My Orders
            </h1>
            <span className="badge bg-primary fs-6">
              {orders.length} order{orders.length !== 1 ? 's' : ''}
            </span>
          </div>
        </div>
      </div>

      <div className="row g-4">
        {orders.map((order) => (
          <div key={order.id} className="col-12">
            <div className="card shadow-sm">
              <div className="card-body">
                <div className="d-flex justify-content-between align-items-start mb-3">
                  <div>
                    <h2 className="h4 fw-semibold mb-1">
                      Order #{order.referenceNumber || order.id}
                    </h2>
                    <p className="text-muted mb-0">
                      Placed on {format(new Date(order.createdAt), 'PPP')}
                    </p>
                    {order.isPaid && order.paidAt && (
                      <p className="text-success mb-0 small">
                        <i className="bi bi-check-circle-fill me-1"></i>
                        Paid on {format(new Date(order.paidAt), 'PPP')}
                      </p>
                    )}
                  </div>
                  <div className="text-end">
                    <span 
                      className="badge rounded-pill fw-normal mb-2"
                      style={{
                        backgroundColor: getStatusColor(order.status),
                        color: 'white'
                      }}
                    >
                      {getStatusLabel(order.status)}
                    </span>
                    <br />
                    {order.isPaid ? (
                      <span className="badge bg-success">Paid</span>
                    ) : (
                      <span className="badge bg-warning">Unpaid</span>
                    )}
                  </div>
                </div>
                
                <hr className="my-3" />
                
                <div className="mb-3">
                  {order.orderItems.map((item) => (
                    <div key={item.id} className="d-flex justify-content-between align-items-center mb-3">
                      <div>
                        <p className="fw-medium mb-1">{item.productName}</p>
                        <p className="text-muted mb-0 small">
                          Quantity: {item.quantity} × ${item.unitPrice.toFixed(2)}
                          {item.discountAmount > 0 && (
                            <span className="text-success ms-2">
                              (${item.discountAmount.toFixed(2)} discount)
                            </span>
                          )}
                        </p>
                      </div>
                      <p className="fw-medium mb-0">
                        ${item.totalPrice.toFixed(2)}
                      </p>
                    </div>
                  ))}
                </div>
                
                <hr className="my-3" />
                
                <div className="mb-3">
                  <div className="d-flex justify-content-between text-muted mb-2">
                    <span>Subtotal</span>
                    <span>{formatCurrency(order.subTotal)}</span>
                  </div>
                  {order.discountTotal > 0 && (
                    <div className="d-flex justify-content-between text-success mb-2">
                      <span>Discount</span>
                      <span>-{formatCurrency(order.discountTotal)}</span>
                    </div>
                  )}
                  <div className="d-flex justify-content-between text-muted mb-2">
                    <span>Shipping</span>
                    <span>{formatCurrency(order.shippingAmount)}</span>
                  </div>
                  <div className="d-flex justify-content-between text-muted mb-2">
                    <span>Tax</span>
                    <span>{formatCurrency(order.taxAmount)}</span>
                  </div>
                  <hr className="my-2" />
                  <div className="d-flex justify-content-between fw-bold fs-5">
                    <span>Total</span>
                    <span>{formatCurrency(order.total)}</span>
                  </div>
                </div>
                
                <hr className="my-3" />
                
                <div className="row">
                  <div className="col-md-6">
                    <h3 className="h6 fw-semibold mb-2">Customer Information</h3>
                    <div className="text-muted">
                      <p className="mb-1">
                        <i className="bi bi-envelope me-2"></i>
                        {order.customerEmail}
                      </p>
                      {order.customerPhone && (
                        <p className="mb-1">
                          <i className="bi bi-telephone me-2"></i>
                          {order.customerPhone}
                        </p>
                      )}
                    </div>
                  </div>
                  <div className="col-md-6">
                    <h3 className="h6 fw-semibold mb-2">Shipping Details</h3>
                    <div className="text-muted">
                      {order.shippingName && <p className="mb-1">{order.shippingName}</p>}
                      {order.shippingAddress1 && <p className="mb-1">{order.shippingAddress1}</p>}
                      {order.shippingAddress2 && <p className="mb-1">{order.shippingAddress2}</p>}
                      {(order.shippingCity || order.shippingState || order.shippingPostalCode) && (
                        <p className="mb-1">
                          {order.shippingCity}, {order.shippingState} {order.shippingPostalCode}
                        </p>
                      )}
                      {order.shippingCountry && <p className="mb-0">{order.shippingCountry}</p>}
                    </div>
                  </div>
                </div>

                {(order.paymentProvider || order.paymentTransactionId) && (
                  <>
                    <hr className="my-3" />
                    <div>
                      <h3 className="h6 fw-semibold mb-2">Payment Information</h3>
                      <div className="text-muted">
                        {order.paymentProvider && (
                          <p className="mb-1">
                            <i className="bi bi-credit-card me-2"></i>
                            Provider: {order.paymentProvider}
                          </p>
                        )}
                        {order.paymentTransactionId && (
                          <p className="mb-0">
                            <i className="bi bi-receipt me-2"></i>
                            Transaction ID: {order.paymentTransactionId}
                          </p>
                        )}
                      </div>
                    </div>
                  </>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>
      
      <div className="row mt-4">
        <div className="col-12">
          <div className="d-flex justify-content-center">
            <Link href="/products" className="btn btn-outline-primary">
              <i className="bi bi-arrow-left me-2"></i>
              Continue Shopping
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}