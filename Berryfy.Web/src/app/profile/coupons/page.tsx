import { Suspense } from 'react';
import Link from 'next/link';
import { getCurrentUser } from '../../../lib/actions/auth-actions';
import { getUserCoupons, hasUserUsedCoupon } from '../../../lib/actions/coupon-actions';
import { 
  CouponTypeDisplay, 
  CouponValueDisplay, 
  CouponMinimumAmountDisplay 
} from '../../../components/coupon/CouponDisplayComponents';
import { CouponDto } from '../../../types/coupon';
import { redirect } from 'next/navigation';

function CouponsLoadingSkeleton() {
  return (
    <div className="row">
      {Array.from({ length: 3 }, (_, i) => (
        <div key={i} className="col-md-6 col-lg-4 mb-4">
          <div className="card h-100">
            <div className="card-body">
              <div className="placeholder-glow">
                <span className="placeholder col-6 mb-2"></span>
                <span className="placeholder col-4 mb-3"></span>
                <span className="placeholder col-8 mb-2"></span>
                <span className="placeholder col-5"></span>
              </div>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}

async function CouponCard({ coupon, userId }: { coupon: CouponDto; userId: number }) {
  const hasUsed = await hasUserUsedCoupon(userId, coupon.code);

  return (
    <div key={coupon.id} className="col-md-6 col-lg-4 mb-4">
      <div className={`card h-100 ${!coupon.isActive ? 'border-secondary opacity-75' : hasUsed ? 'border-warning' : 'border-primary shadow-sm'}`}>
        <div className="card-header bg-light d-flex justify-content-between align-items-start align-items-sm-center flex-wrap gap-2">
          <div className="d-flex align-items-center">
            <i className="bi bi-ticket-detailed me-2 text-primary"></i>
            <strong className="text-primary">{coupon.code}</strong>
          </div>
          <div>
            {!coupon.isActive ? (
              <span className="badge bg-secondary">
                <i className="bi bi-x-circle me-1"></i>
                Inactive
              </span>
            ) : hasUsed ? (
              <span className="badge bg-warning text-dark">
                <i className="bi bi-check-circle me-1"></i>
                Used
              </span>
            ) : (
              <span className="badge bg-success">
                <i className="bi bi-circle me-1"></i>
                Available
              </span>
            )}
          </div>
        </div>
        <div className="card-body">
          <div className="d-flex justify-content-between align-items-center mb-3">
            <CouponTypeDisplay type={coupon.type} />
            <div className="text-end">
              <div className="h4 mb-0 text-success">
                <CouponValueDisplay coupon={coupon} />
              </div>
              <small className="text-muted">Discount</small>
            </div>
          </div>

          <p className="card-text text-muted mb-3">
            {coupon.description}
          </p>

          {coupon.minimumOrderAmount > 0 && (
            <div className="mb-3">
              <small className="text-muted">
                <i className="bi bi-cart me-1"></i>
                Minimum order: <strong><CouponMinimumAmountDisplay minimumAmount={coupon.minimumOrderAmount} /></strong>
              </small>
            </div>
          )}

          {coupon.isForNewUsersOnly && (
            <div className="mb-3">
              <span className="badge bg-info text-dark">
                <i className="bi bi-person-plus me-1"></i>
                New Users Only
              </span>
            </div>
          )}
        </div>
        <div className="card-footer bg-transparent">
          {!coupon.isActive ? (
            <div className="text-center text-muted">
              <i className="bi bi-x-circle me-1"></i>
              This coupon is not active
            </div>
          ) : hasUsed ? (
            <div className="text-center text-warning">
              <i className="bi bi-check-circle me-1"></i>
              You&apos;ve already used this coupon
            </div>
          ) : (
            <div className="d-grid">
              <Link href={{
                pathname: '/cart',
  query: { code: coupon.code }
              }} className="btn btn-outline-primary">
                <i className="bi bi-cart me-1"></i>
                Use in Cart
              </Link>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

async function UserCouponsList() {
  const userCoupons = await getUserCoupons() as CouponDto[];
  const currentUser = await getCurrentUser();

  if (!currentUser) {
    redirect('/auth/login?redirectTo=/profile/coupons');
  }

  if (userCoupons.length === 0) {
    return (
      <div className="text-center py-5">
        <div className="mb-4">
          <i className="bi bi-ticket-detailed display-1 text-muted"></i>
        </div>
        <h3 className="text-muted">No Coupons Available</h3>
        <p className="text-muted mb-4">You don&apos;t have any coupons yet. Keep shopping to earn discounts!</p>
        <Link href="/" className="btn btn-primary">
          <i className="bi bi-shop me-2"></i>Continue Shopping
        </Link>
      </div>
    );
  }

  const activeCoupons = userCoupons.filter(coupon => coupon.isActive);
  const inactiveCoupons = userCoupons.filter(coupon => !coupon.isActive);

  return (
    <div>
      {activeCoupons.length > 0 && (
        <>
          <div className="d-flex align-items-center mb-3">
            <h5 className="mb-0 text-success">
              <i className="bi bi-check-circle me-2"></i>
              Active Coupons ({activeCoupons.length})
            </h5>
          </div>
          <div className="row">
            {activeCoupons.map((coupon) => (
              <CouponCard key={coupon.id} coupon={coupon} userId={currentUser.id} />
            ))}
          </div>
        </>
      )}

      {inactiveCoupons.length > 0 && (
        <>
          <div className="d-flex align-items-center mb-3 mt-4">
            <h5 className="mb-0 text-muted">
              <i className="bi bi-x-circle me-2"></i>
              Inactive Coupons ({inactiveCoupons.length})
            </h5>
          </div>
          <div className="row">
            {inactiveCoupons.map((coupon) => (
              <CouponCard key={coupon.id} coupon={coupon} userId={currentUser.id} />
            ))}
          </div>
        </>
      )}
    </div>
  );
}

export default async function UserCouponsPage() {
  const user = await getCurrentUser();

  if (!user) {
    redirect('/auth/login?redirectTo=/profile/coupons');
  }

  return (
    <div className="container py-5">
      <div className="row">
        <div className="col-12">
          <nav aria-label="breadcrumb" className="mb-4">
            <ol className="breadcrumb">
              <li className="breadcrumb-item">
                <Link href="/" className="text-decoration-none">
                  <i className="bi bi-house-door me-1"></i>Home
                </Link>
              </li>
              <li className="breadcrumb-item">
                <Link href="/profile" className="text-decoration-none">
                  <i className="bi bi-person-circle me-1"></i>Profile
                </Link>
              </li>
              <li className="breadcrumb-item active" aria-current="page">
                <i className="bi bi-ticket-detailed me-1"></i>My Coupons
              </li>
            </ol>
          </nav>

          <div className="d-flex justify-content-between align-items-center mb-4">
            <div>
              <h1 className="h2 mb-1">
                <i className="bi bi-ticket-detailed me-2"></i>My Coupons
              </h1>
              <p className="text-muted mb-0">View and manage your available discount coupons</p>
            </div>
            <Link href="/profile" className="btn btn-outline-secondary">
              <i className="bi bi-arrow-left me-1"></i>Back to Profile
            </Link>
          </div>

          <div className="card shadow-sm">
            <div className="card-header bg-light">
              <h5 className="card-title mb-0">
                <i className="bi bi-list-ul me-2"></i>Available Coupons
              </h5>
            </div>
            <div className="card-body">
              <Suspense fallback={<CouponsLoadingSkeleton />}>
                <UserCouponsList />
              </Suspense>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export const metadata = {
  title: 'My Coupons - Berryfy',
  description: 'View and manage your available discount coupons',
}; 