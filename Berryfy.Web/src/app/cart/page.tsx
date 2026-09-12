import { unstable_rethrow } from 'next/navigation';
import { getCart, getOrCreateCart } from '../../lib/actions/cart-actions';
import { getProducts } from '../../lib/actions/product-actions';
import CartItem from '../../components/cart/CartItem';
import CartSummary from '../../components/cart/CartSummary';
import CouponSection from '../../components/cart/CouponSection';
import ClearCartButton from '../../components/cart/ClearCartButton';
import CartStatusIndicator, { CartStatusMessage } from '../../components/cart/CartStatus';
import { ProductDto } from '../../types/product';
import { CartStatus } from '../../types/cart';
import Link from 'next/link';

export const dynamic = 'force-dynamic';

export default async function CartPage({
  searchParams,
}: {
  searchParams: Promise<{ confirm_clear?: string; error?: string; couponError?: string }>;
}) {
  try {
    return await loadCartPage({
  searchParams,
});
  } catch (error) {
    unstable_rethrow(error);
    console.error('Error loading cart page:', error);
    return (
      <div className="container py-5">
        <div className="text-center">
          <i className="bi bi-exclamation-triangle fs-1 text-danger mb-3 d-block"></i>
          <h2>Error Loading Cart</h2>
          <p className="text-muted">
            {error instanceof Error ? error.message : 'An unexpected error occurred'}
          </p>
          <div className="d-flex gap-2 justify-content-center">
            <Link href="/products" className="btn btn-primary">
              <i className="bi bi-arrow-left me-2"></i>
              Continue Shopping
            </Link>
            <Link href="/cart" className="btn btn-outline-secondary">
              <i className="bi bi-arrow-clockwise me-2"></i>
              Refresh Page
            </Link>
          </div>
        </div>
      </div>
    );
  }
}

async function loadCartPage({
  searchParams,
}: {
  searchParams: Promise<{ confirm_clear?: string; error?: string; couponError?: string }>;
}) {

  
    const cart = await getCart();
    const allProducts = await getProducts();

    if (!cart) {
      return (
        <div className="container py-5">
          <div className="text-center">
            <i className="bi bi-cart-x fs-1 text-muted mb-3 d-block"></i>
            <h2>Unable to load cart</h2>
            <p className="text-muted">Please try refreshing the page</p>
            <Link href="/products" className="btn btn-primary">
              <i className="bi bi-arrow-left me-2"></i>
              Continue Shopping
            </Link>
          </div>
        </div>
      );
    }

    const productMap = new Map<number, ProductDto>();
    allProducts.forEach(product => {
      productMap.set(product.id, product);
    });

    const search = await searchParams;
    const hasItems = cart.cartItems && cart.cartItems.length > 0;
    const showClearConfirm = search.confirm_clear === 'true';
    const errorMessage = search.error;
    const couponError = search.couponError ? decodeURIComponent(search.couponError) : undefined;
    const isActiveCart = cart.status === CartStatus.Active;
    const isPendingPayment = cart.status === CartStatus.PendingPayment;
    const isEditable = isActiveCart || isPendingPayment; // Can edit both Active and PendingPayment carts

    return (
      <div className="container py-4">
        <div className="row">
          <div className="col-12">
            <nav aria-label="breadcrumb">
              <ol className="breadcrumb">
                <li className="breadcrumb-item">
                  <Link href="/">Home</Link>
                </li>
                <li className="breadcrumb-item">
                  <Link href="/products">Products</Link>
                </li>
                <li className="breadcrumb-item active" aria-current="page">
                  Shopping Cart
                </li>
              </ol>
            </nav>
          </div>
        </div>

        {errorMessage && (
          <div className="row mb-4">
            <div className="col-12">
              <div className="alert alert-warning alert-dismissible fade show" role="alert">
                <i className="bi bi-exclamation-triangle me-2"></i>
                {decodeURIComponent(errorMessage)}
                <Link 
                  href="/cart" 
                  className="btn-close" 
                  aria-label="Close"
                ></Link>
              </div>
            </div>
          </div>
        )}

        <CartStatusMessage status={cart.status} className="mb-4" />

        <div className="row">
          <div className="col-12">
            <div className="d-flex justify-content-between align-items-start align-items-sm-center flex-column flex-sm-row gap-3 mb-4">
              <h1 className="mb-0 d-flex align-items-center flex-wrap gap-2">
                <i className="bi bi-cart3"></i>
                Shopping Cart
                {hasItems && (
                  <span className="badge bg-primary">
                    {cart.cartItems.reduce((sum, item) => sum + item.quantity, 0)} items
                  </span>
                )}
                <CartStatusIndicator status={cart.status} />
              </h1>
              <Link href="/products" className="btn btn-outline-primary flex-shrink-0">
                <i className="bi bi-arrow-left me-2"></i>
                Continue Shopping
              </Link>
            </div>
          </div>
        </div>

        {showClearConfirm && hasItems && isActiveCart && (
          <div className="row mb-4">
            <div className="col-12">
              <ClearCartButton showConfirm={true} />
            </div>
          </div>
        )}

        {hasItems ? (
          <div className="row">
            <div className="col-lg-8">
              <div className="mb-4">
                {cart.cartItems.map((item) => {
                  const product = productMap.get(item.productId);
                  if (!product) {
                    return (
                      <div key={item.id} className="card mb-3">
                        <div className="card-body">
                          <div className="alert alert-warning mb-0">
                            <i className="bi bi-exclamation-triangle me-2"></i>
                            Product not found (ID: {item.productId})
                          </div>
                        </div>
                      </div>
                    );
                  }
                  return (
                    <CartItem
                      key={item.id}
                      item={item}
                      product={product}
                      disabled={!isEditable}
                    />
                  );
                })}
              </div>

              {isEditable && (
                <CouponSection
                  appliedCoupons={cart.cartCoupons || []}
                  couponError={couponError}
                />
              )}
            </div>

            <div className="col-lg-4">
              <CartSummary cart={cart} />
              
              {!isEditable && (
                <div className="card mt-3">
                  <div className="card-body text-center">
                    <i className="bi bi-info-circle text-muted fs-4 mb-2 d-block"></i>
                    <p className="text-muted mb-0">
                      This cart has been completed and cannot be modified.
                    </p>
                  </div>
                </div>
              )}
            </div>
          </div>
        ) : (
          <div className="row">
            <div className="col-12">
              <div className="text-center py-5">
                <i className="bi bi-cart-x display-1 text-muted mb-4 d-block"></i>
                <h2 className="mb-3">Your cart is empty</h2>
                <p className="text-muted mb-4">
                  Looks like you haven&apos;t added any items to your cart yet.
                </p>
                <Link href="/products" className="btn btn-primary btn-lg">
                  <i className="bi bi-bag-plus me-2"></i>
                  Start Shopping
                </Link>
              </div>
            </div>
          </div>
        )}

        <div className="row mt-4">
          <div className="col-12">
            <div className="d-flex justify-content-center">
              <Link href="/products" className="btn btn-link">
                <i className="bi bi-arrow-left me-2"></i>
                Back to Products
              </Link>
            </div>
          </div>
        </div>
      </div>
    );
  
} 