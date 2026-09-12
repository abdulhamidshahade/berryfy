import { OrderStatus } from '../../types/order';
import { 
  updateOrderStatus, 
  cancelOrder, 
  refundOrder, 
  markOrderAsPaid, 
  processOrder 
} from '../../lib/actions/order-actions';

interface OrderActionModalProps {
  orderId: number;
  currentStatus: OrderStatus;
  redirectTo?: string;
}

interface OrderActionModalBaseProps {
  orderId: number;
  redirectTo?: string;
}

export function StatusUpdateModal({ orderId, currentStatus, redirectTo }: OrderActionModalProps) {
  return (
    <div className="modal fade" id={`statusModal-${orderId}`} tabIndex={-1}>
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">Update Order Status</h5>
            <button type="button" className="btn-close" data-bs-dismiss="modal"></button>
          </div>
          <form action={updateOrderStatus}>
            <div className="modal-body">
              <input type="hidden" name="orderId" value={orderId} />
              {redirectTo && <input type="hidden" name="redirectTo" value={redirectTo} />}
              <div className="mb-3">
                <label htmlFor={`status-${orderId}`} className="form-label">New Status</label>
                <select 
                  className="form-select" 
                  id={`status-${orderId}`} 
                  name="newStatus" 
                  defaultValue={currentStatus}
                  required
                >
                  <option value={OrderStatus.Pending}>Pending</option>
                  <option value={OrderStatus.Processing}>Processing</option>
                  <option value={OrderStatus.Shipped}>Shipped</option>
                  <option value={OrderStatus.Delivered}>Delivered</option>
                  <option value={OrderStatus.Completed}>Completed</option>
                  <option value={OrderStatus.Cancelled}>Cancelled</option>
                  <option value={OrderStatus.Refunded}>Refunded</option>
                </select>
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" data-bs-dismiss="modal">
                Cancel
              </button>
              <button type="submit" className="btn btn-primary">
                Update Status
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

export function CancelOrderModal({ orderId, redirectTo }: OrderActionModalBaseProps) {
  return (
    <div className="modal fade" id={`cancelModal-${orderId}`} tabIndex={-1}>
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">Cancel Order</h5>
            <button type="button" className="btn-close" data-bs-dismiss="modal"></button>
          </div>
          <form action={cancelOrder}>
            <div className="modal-body">
              <input type="hidden" name="orderId" value={orderId} />
              {redirectTo && <input type="hidden" name="redirectTo" value={redirectTo} />}
              <div className="mb-3">
                <label htmlFor={`cancelReason-${orderId}`} className="form-label">Cancellation Reason</label>
                <textarea 
                  className="form-control" 
                  id={`cancelReason-${orderId}`} 
                  name="reason"
                  rows={3}
                  required
                  placeholder="Please provide a reason for cancelling this order..."
                ></textarea>
              </div>
              <div className="alert alert-warning">
                <i className="bi bi-exclamation-triangle me-2"></i>
                This action cannot be undone. The order will be marked as cancelled.
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" data-bs-dismiss="modal">
                Close
              </button>
              <button type="submit" className="btn btn-danger">
                Cancel Order
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

export function RefundOrderModal({ orderId, redirectTo }: OrderActionModalBaseProps) {
  return (
    <div className="modal fade" id={`refundModal-${orderId}`} tabIndex={-1}>
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">Refund Order</h5>
            <button type="button" className="btn-close" data-bs-dismiss="modal"></button>
          </div>
          <form action={refundOrder}>
            <div className="modal-body">
              <input type="hidden" name="orderId" value={orderId} />
              {redirectTo && <input type="hidden" name="redirectTo" value={redirectTo} />}
              <div className="mb-3">
                <label htmlFor={`refundReason-${orderId}`} className="form-label">Refund Reason</label>
                <textarea 
                  className="form-control" 
                  id={`refundReason-${orderId}`} 
                  name="reason"
                  rows={3}
                  required
                  placeholder="Please provide a reason for refunding this order..."
                ></textarea>
              </div>
              <div className="alert alert-info">
                <i className="bi bi-info-circle me-2"></i>
                This will process the refund and update the order status to &quot;Refunded&quot;.
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" data-bs-dismiss="modal">
                Close
              </button>
              <button type="submit" className="btn btn-warning">
                Process Refund
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

export function MarkPaidModal({ orderId, redirectTo }: OrderActionModalBaseProps) {
  return (
    <div className="modal fade" id={`markPaidModal-${orderId}`} tabIndex={-1}>
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">Mark Order as Paid</h5>
            <button type="button" className="btn-close" data-bs-dismiss="modal"></button>
          </div>
          <form action={markOrderAsPaid}>
            <div className="modal-body">
              <input type="hidden" name="orderId" value={orderId} />
              {redirectTo && <input type="hidden" name="redirectTo" value={redirectTo} />}
              <div className="mb-3">
                <label htmlFor={`paymentProvider-${orderId}`} className="form-label">Payment Provider</label>
                <select 
                  className="form-select" 
                  id={`paymentProvider-${orderId}`} 
                  name="paymentProvider"
                  required
                >
                  <option value="">Select payment provider</option>
                  <option value="stripe">Stripe</option>
                  <option value="paypal">PayPal</option>
                  <option value="bank_transfer">Bank Transfer</option>
                  <option value="cash">Cash</option>
                  <option value="other">Other</option>
                </select>
              </div>
              <div className="mb-3">
                <label htmlFor={`transactionId-${orderId}`} className="form-label">Transaction ID (Optional)</label>
                <input 
                  type="number" 
                  className="form-control" 
                  id={`transactionId-${orderId}`} 
                  name="paymentTransactionId"
                  placeholder="Enter transaction ID if available"
                />
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" data-bs-dismiss="modal">
                Close
              </button>
              <button type="submit" className="btn btn-success">
                Mark as Paid
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

export function ProcessOrderModal({ orderId }: { orderId: number }) {
  return (
    <div className="modal fade" id={`processModal-${orderId}`} tabIndex={-1}>
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">Process Order</h5>
            <button type="button" className="btn-close" data-bs-dismiss="modal"></button>
          </div>
          <div className="modal-body">
            <p>Are you sure you want to process this order?</p>
            <div className="alert alert-info">
              <i className="bi bi-info-circle me-2"></i>
              This will move the order to the next stage of processing.
            </div>
          </div>
          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" data-bs-dismiss="modal">
              Cancel
            </button>
            <form action={processOrder} style={{ display: 'inline' }}>
              <input type="hidden" name="orderId" value={orderId} />
              <button type="submit" className="btn btn-primary">
                Process Order
              </button>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
}