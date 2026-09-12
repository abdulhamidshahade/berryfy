import { unstable_rethrow } from 'next/navigation';
import Link from 'next/link';
import { notFound } from 'next/navigation';
import { OrderService } from '../../../lib/services/order/service';
import { OrderStatus } from '../../../types/order';

interface OrderDetailPageProps {
  params: Promise<{
    id: string;
  }>;
}

export default async function OrderDetailPage({ params }: OrderDetailPageProps) {
  try {
    return await loadOrderDetailPage({ params });
  } catch (error) {
    unstable_rethrow(error);
    console.error('Error loading order:', error);
    notFound();
  }
}

async function loadOrderDetailPage({ params }: OrderDetailPageProps) {
  const { id } = await params;
  const orderId = parseInt(id);

  if (isNaN(orderId)) {
    notFound();
  }

  
    const orderService = new OrderService();
    const order = await orderService.getById(orderId);
    if (!order) {
      notFound();
    }

    const getStatusBadgeClass = (status: OrderStatus) => {
      switch (status) {
        case OrderStatus.Completed:
          return 'bg-success';
        case OrderStatus.Processing:
          return 'bg-warning text-dark';
        case OrderStatus.Shipped:
          return 'bg-info';
        case OrderStatus.Delivered:
          return 'bg-primary';
        case OrderStatus.Cancelled:
          return 'bg-danger';
        case OrderStatus.Refunded:
          return 'bg-secondary';
        default:
          return 'bg-light text-dark';
      }
    };

    const getStatusText = (status: OrderStatus) => {
      switch (status) {
        case OrderStatus.Pending:
          return 'Pending';
        case OrderStatus.Processing:
          return 'Processing';
        case OrderStatus.Shipped:
          return 'Shipped';
        case OrderStatus.Delivered:
          return 'Delivered';
        case OrderStatus.Completed:
          return 'Completed';
        case OrderStatus.Cancelled:
          return 'Cancelled';
        case OrderStatus.Refunded:
          return 'Refunded';
        default:
          return 'Unknown';
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
              <Link href="/orders" className="text-decoration-none">My Orders</Link>
            </li>
            <li className="breadcrumb-item active" aria-current="page">Order Details</li>
          </ol>
        </nav>

        <div className="d-flex justify-content-between align-items-center mb-4">
          <div>
            <h1 className="h3 fw-bold mb-2">Order Details</h1>
            <p className="text-muted mb-0">
              Order #{order.referenceNumber || order.id} • Placed on {new Date(order.createdAt).toLocaleDateString()}
            </p>
          </div>
          <div className="d-flex gap-2">
            <Link href="/orders" className="btn btn-outline-secondary">
              <i className="bi bi-arrow-left me-2"></i>
              Back to Orders
            </Link>
            {order.isPaid && (
              <Link
                href={`/profile/payments?orderId=${order.id}`}
                className="btn btn-outline-primary"
              >
                <i className="bi bi-receipt me-2"></i>
                View Receipt
              </Link>
            )}
          </div>
        </div>

        <div className="row mb-4">
          <div className="col-12">
            <div className="card">
              <div className="card-body">
                <div className="row align-items-center">
                  <div className="col-md-6">
                    <h5 className="mb-0">Order Status</h5>
                    <span className={`badge ${getStatusBadgeClass(order.status)} fs-6 mt-2`}>
                      {getStatusText(order.status)}
                    </span>
                  </div>
                  <div className="col-md-6 text-md-end">
                    <h5 className="mb-0">Total Amount</h5>
                    <h4 className="text-primary mb-0 mt-2">${order.total.toFixed(2)}</h4>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="row">
          <div className="col-lg-8">
            <div className="card mb-4">
              <div className="card-header">
                <h5 className="mb-0">
                  <i className="bi bi-box-seam me-2"></i>
                  Order Items ({order.orderItems?.length || 0})
                </h5>
              </div>
              <div className="card-body">
                {order.orderItems && order.orderItems.length > 0 ? (
                  <div className="table-responsive">
                    <table className="table">
                      <thead>
                        <tr>
                          <th>Product</th>
                          <th>Quantity</th>
                          <th>Unit Price</th>
                          <th>Discount</th>
                          <th>Total</th>
                        </tr>
                      </thead>
                      <tbody>
                        {order.orderItems.map(item => (
                          <tr key={item.id}>
                            <td>
                              <div>
                                <h6 className="mb-0">{item.productName}</h6>
                                <small className="text-muted">Product ID: {item.productId}</small>
                              </div>
                            </td>
                            <td>{item.quantity}</td>
                            <td>${item.unitPrice.toFixed(2)}</td>
                            <td>
                              {item.discountAmount > 0 ? (
                                <span className="text-success">-${item.discountAmount.toFixed(2)}</span>
                              ) : (
                                <span className="text-muted">$0.00</span>
                              )}
                            </td>
                            <td><strong>${item.totalPrice.toFixed(2)}</strong></td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <div className="text-center py-4">
                    <i className="bi bi-box display-4 text-muted"></i>
                    <p className="text-muted mt-2">No items found in this order</p>
                  </div>
                )}
              </div>
            </div>

            
            <div className="card">
              <div className="card-header">
                <h5 className="mb-0">Order Summary</h5>
              </div>
              <div className="card-body">
                <div className="row">
                  <div className="col-md-6">
                    <div className="d-flex justify-content-between mb-2">
                      <span>Subtotal:</span>
                      <span>${order.subTotal.toFixed(2)}</span>
                    </div>
                    <div className="d-flex justify-content-between mb-2">
                      <span>Tax:</span>
                      <span>${order.taxAmount.toFixed(2)}</span>
                    </div>
                    <div className="d-flex justify-content-between mb-2">
                      <span>Shipping:</span>
                      <span>${order.shippingAmount.toFixed(2)}</span>
                    </div>
                    {order.discountTotal > 0 && (
                      <div className="d-flex justify-content-between mb-2 text-success">
                        <span>Discount:</span>
                        <span>-${order.discountTotal.toFixed(2)}</span>
                      </div>
                    )}
                    <hr />
                    <div className="d-flex justify-content-between">
                      <strong>Total:</strong>
                      <strong>${order.total.toFixed(2)}</strong>
                    </div>
                  </div>
                  <div className="col-md-6">
                    <h6 className="text-muted mb-2">Payment Status</h6>
                    <div className="mb-3">
                      {order.isPaid ? (
                        <span className="badge bg-success">Paid</span>
                      ) : (
                        <span className="badge bg-warning text-dark">Unpaid</span>
                      )}
                      {order.paidAt && (
                        <div className="mt-1">
                          <small className="text-muted">
                            Paid on {new Date(order.paidAt).toLocaleDateString()}
                          </small>
                        </div>
                      )}
                    </div>
                    {order.paymentProvider && (
                      <div>
                        <h6 className="text-muted mb-2">Payment Method</h6>
                        <p className="mb-0">{order.paymentProvider}</p>
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div className="col-lg-4">
            <div className="card mb-4">
              <div className="card-header">
                <h5 className="mb-0">Customer Information</h5>
              </div>
              <div className="card-body">
                <p className="mb-1"><strong>Email:</strong> {order.customerEmail}</p>
                {order.customerPhone && (
                  <p className="mb-1"><strong>Phone:</strong> {order.customerPhone}</p>
                )}
                <p className="mb-0"><strong>Order Date:</strong> {new Date(order.createdAt).toLocaleDateString()}</p>
              </div>
            </div>

            {order.shippingName && (
              <div className="card mb-4">
                <div className="card-header">
                  <h5 className="mb-0">Shipping Address</h5>
                </div>
                <div className="card-body">
                  <p className="mb-1"><strong>Name:</strong> {order.shippingName}</p>
                  {order.shippingAddress1 && (
                    <div className="mb-2">
                      <p className="mb-0">{order.shippingAddress1}</p>
                      {order.shippingAddress2 && <p className="mb-0">{order.shippingAddress2}</p>}
                      <p className="mb-0">
                        {order.shippingCity}, {order.shippingState} {order.shippingPostalCode}
                      </p>
                      <p className="mb-0">{order.shippingCountry}</p>
                    </div>
                  )}
                </div>
              </div>
            )}

            <div className="card">
              <div className="card-header">
                <h5 className="mb-0">Actions</h5>
              </div>
              <div className="card-body">
                <div className="d-grid gap-2">
                  {order.isPaid && (
                    <Link
                      href={`/profile/payments?orderId=${order.id}`}
                      className="btn btn-primary"
                    >
                      <i className="bi bi-receipt me-2"></i>
                      View Receipt
                    </Link>
                  )}
                  
                  <Link
                    href="/orders"
                    className="btn btn-outline-secondary"
                  >
                    <i className="bi bi-arrow-left me-2"></i>
                    Back to Orders
                  </Link>
                  
                  {order.status === OrderStatus.Pending && (
                    <Link
                      href={`/orders/${order.id}/cancel`}
                      className="btn btn-outline-danger"
                    >
                      <i className="bi bi-x-circle me-2"></i>
                      Cancel Order
                    </Link>
                  )}
                </div>
              </div>
            </div>

            <div className="card mt-4">
              <div className="card-header">
                <h5 className="mb-0">Need Help?</h5>
              </div>
              <div className="card-body">
                <p className="card-text mb-3">
                  Have questions about this order? We&apos;re here to help.
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