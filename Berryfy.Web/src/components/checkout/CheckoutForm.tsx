import { createOrderForPayment } from '../../lib/actions/checkout-actions';
import { CartDto } from '../../types/cart';
import Link from 'next/link';

interface CheckoutFormProps {
  cart: CartDto;
  searchParams?: { [key: string]: string | string[] | undefined };
}

export default function CheckoutForm({ cart, searchParams }: CheckoutFormProps) {
  const itemCount = cart.cartItems.reduce((sum, item) => sum + item.quantity, 0);

  const formData = {
    firstName: (searchParams?.firstName as string) || '',
    lastName: (searchParams?.lastName as string) || '',
    email: (searchParams?.email as string) || '',
    phone: (searchParams?.phone as string) || '',
    address: (searchParams?.address as string) || '',
    address2: (searchParams?.address2 as string) || '',
    city: (searchParams?.city as string) || '',
    state: (searchParams?.state as string) || '',
    zipCode: (searchParams?.zipCode as string) || '',
    userId: (cart.userId as number) || undefined
  };

  const validation = {
    firstName: (searchParams?.error_firstName as string) || '',
    lastName: (searchParams?.error_lastName as string) || '',
    email: (searchParams?.error_email as string) || '',
    address: (searchParams?.error_address as string) || '',
    city: (searchParams?.error_city as string) || '',
    state: (searchParams?.error_state as string) || '',
    zipCode: (searchParams?.error_zipCode as string) || '',
  };

  return (
    <form action={createOrderForPayment}>
      <div className="row">
        <div className="col-lg-8">
          <div className="card mb-4">
            <div className="card-header">
              <h5 className="mb-0">
                <i className="bi bi-person me-2"></i>
                Billing Information
              </h5>
            </div>
            <div className="card-body">
              <div className="row">
                <div className="col-md-6 mb-3">
                  <label htmlFor="firstName" className="form-label">First Name</label>
                  <input 
                    type="text" 
                    className={`form-control ${validation.firstName ? 'is-invalid' : ''}`}
                    id="firstName" 
                    name="firstName" 
                    defaultValue={formData.firstName}
                    required 
                  />
                  {validation.firstName && (
                    <div className="invalid-feedback">{validation.firstName}</div>
                  )}
                </div>
                <div className="col-md-6 mb-3">
                  <label htmlFor="lastName" className="form-label">Last Name</label>
                  <input 
                    type="text" 
                    className={`form-control ${validation.lastName ? 'is-invalid' : ''}`}
                    id="lastName" 
                    name="lastName" 
                    defaultValue={formData.lastName}
                    required 
                  />
                  {validation.lastName && (
                    <div className="invalid-feedback">{validation.lastName}</div>
                  )}
                </div>
              </div>
              <div className="mb-3">
                <label htmlFor="email" className="form-label">Email</label>
                <input 
                  type="email" 
                  className={`form-control ${validation.email ? 'is-invalid' : ''}`}
                  id="email" 
                  name="email" 
                  defaultValue={formData.email}
                  required 
                />
                {validation.email && (
                  <div className="invalid-feedback">{validation.email}</div>
                )}
              </div>
              <div className="mb-3">
                <label htmlFor="phone" className="form-label">Phone (optional)</label>
                <input 
                  type="tel" 
                  className="form-control" 
                  id="phone" 
                  name="phone" 
                  defaultValue={formData.phone}
                />
              </div>
              <div className="mb-3">
                <label htmlFor="address" className="form-label">Address</label>
                <input 
                  type="text" 
                  className={`form-control ${validation.address ? 'is-invalid' : ''}`}
                  id="address" 
                  name="address" 
                  defaultValue={formData.address}
                  required 
                />
                {validation.address && (
                  <div className="invalid-feedback">{validation.address}</div>
                )}
              </div>
              <div className="mb-3">
                <label htmlFor="address2" className="form-label">Address 2 (optional)</label>
                <input 
                  type="text" 
                  className="form-control" 
                  id="address2" 
                  name="address2" 
                  defaultValue={formData.address2}
                />
              </div>
              <div className="row">
                <div className="col-md-5 mb-3">
                  <label htmlFor="city" className="form-label">City</label>
                  <input 
                    type="text" 
                    className={`form-control ${validation.city ? 'is-invalid' : ''}`}
                    id="city" 
                    name="city" 
                    defaultValue={formData.city}
                    required 
                  />
                  {validation.city && (
                    <div className="invalid-feedback">{validation.city}</div>
                  )}
                </div>
                <div className="col-md-3 mb-3">
                  <label htmlFor="state" className="form-label">State</label>
                  <input 
                    type="text" 
                    className={`form-control ${validation.state ? 'is-invalid' : ''}`}
                    id="state" 
                    name="state" 
                    defaultValue={formData.state}
                    required 
                  />
                  {validation.state && (
                    <div className="invalid-feedback">{validation.state}</div>
                  )}
                </div>
                <div className="col-md-4 mb-3">
                  <label htmlFor="zip" className="form-label">Zip Code</label>
                  <input 
                    type="text" 
                    className={`form-control ${validation.zipCode ? 'is-invalid' : ''}`}
                    id="zip" 
                    name="zip" 
                    defaultValue={formData.zipCode}
                    required 
                  />
                  {validation.zipCode && (
                    <div className="invalid-feedback">{validation.zipCode}</div>
                  )}
                </div>
              </div>
              <input type="hidden" name="country" value="US" />
            </div>
          </div>
        </div>

        <div className="col-lg-4">
          <div className="card sticky-top">
            <div className="card-header">
              <h5 className="mb-0">
                <i className="bi bi-receipt me-2"></i>
                Order Summary
              </h5>
            </div>
            <div className="card-body">
              <div className="d-flex justify-content-between mb-2">
                <span>Items ({itemCount}):</span>
                <span>${cart.subTotal.toFixed(2)}</span>
              </div>

              {cart.discountTotal > 0 && (
                <div className="d-flex justify-content-between mb-2">
                  <span className="text-success">Discounts:</span>
                  <span className="text-success">-${cart.discountTotal.toFixed(2)}</span>
                </div>
              )}

              <div className="d-flex justify-content-between mb-2">
                <span>Tax:</span>
                <span>${cart.taxAmount.toFixed(2)}</span>
              </div>

              <div className="d-flex justify-content-between mb-2">
                <span>Shipping:</span>
                <span>Free</span>
              </div>

              <hr />

              <div className="d-flex justify-content-between mb-3">
                <strong>Total:</strong>
                <strong className="text-primary fs-5">${cart.total.toFixed(2)}</strong>
              </div>

              <div className="d-grid gap-2">
                <button 
                  type="submit" 
                  className="btn btn-primary btn-lg"
                >
                  <i className="bi bi-arrow-right me-2"></i>
                  Order View
                </button>
                <Link href="/cart" className="btn btn-outline-secondary">
                  <i className="bi bi-arrow-left me-2"></i>
                  Back to Cart
                </Link>
              </div>

              {cart.discountTotal > 0 && (
                <div className="mt-3 p-2 bg-success bg-opacity-10 rounded">
                  <div className="text-center">
                    <i className="bi bi-piggy-bank-fill text-success me-1"></i>
                    <span className="text-success fw-bold">
                      You&apos;re saving ${cart.discountTotal.toFixed(2)}!
                    </span>
                  </div>
                </div>
              )}
            </div>
          </div>

          <div className="card mt-3">
            <div className="card-body text-center">
              <i className="bi bi-shield-check text-success fs-2 mb-2 d-block"></i>
              <h6>Secure Checkout</h6>
              <small className="text-muted">
                Your payment information is encrypted and secure
              </small>
            </div>
          </div>
        </div>
      </div>
    </form>
  );
} 