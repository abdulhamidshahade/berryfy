import Link from 'next/link';
import { getCoupons, addCouponToNewUsersAction } from '../../lib/actions/coupon-actions';
import { User } from '../../types/user';
import { CouponDto } from '../../types/coupon';
import { getAllUsers } from '../../lib/actions/coupon-actions';

export async function AddCouponToNewUsersForm() {
  let users: User[] = [];
  let coupons: CouponDto[] = [];
  
  try {
    [users, coupons] = await Promise.all([
      getAllUsers(),
      getCoupons()
    ]);
  } catch (error) {
    console.error('Error loading data:', error);
  }

  const newUsers = users.filter(user => user.firstName === "new");
  coupons = coupons.filter(c => c.isForNewUsersOnly);

  if (coupons.length === 0) {
    return (
      <div className="text-center py-5">
        <div className="mb-4">
          <i className="bi bi-exclamation-triangle display-1 text-warning"></i>
        </div>
        <h3 className="text-muted">No Coupons Available</h3>
        <p className="text-muted">There are no coupons available to assign to new users.</p>
      </div>
    );
  }

  return (
    <form action={addCouponToNewUsersAction} className="needs-validation" noValidate>
      <div className="row">
        <div className="col-12">
          <div className="mb-4">
            <label htmlFor="couponId" className="form-label">
              <i className="bi bi-tag me-1"></i>
              Select Coupon <span className="text-danger">*</span>
            </label>
            <select 
              className="form-select form-select-lg" 
              id="couponId" 
              name="couponId" 
              required
            >
              <option value="">Choose a coupon...</option>
              {coupons.map((coupon: CouponDto) => (
                <option key={coupon.id} value={coupon.id}>
                  {coupon.code} - {coupon.description}
                </option>
              ))}
            </select>
            <div className="invalid-feedback">
              Please select a coupon.
            </div>
          </div>
        </div>
      </div>

      <div className="card mb-4">
        <div className="card-header bg-primary text-white">
          <h5 className="card-title mb-0">
            <i className="bi bi-person-plus me-2"></i>New Users Overview
          </h5>
        </div>
        <div className="card-body">
          <div className="row">
            <div className="col-md-4">
              <div className="text-center">
                <h3 className="text-primary">{newUsers.length}</h3>
                <p className="text-muted mb-0">New Users</p>
              </div>
            </div>
            <div className="col-md-4">
              <div className="text-center">
                <h3 className="text-secondary">{users.length}</h3>
                <p className="text-muted mb-0">Total Users</p>
              </div>
            </div>
            <div className="col-md-4">
              <div className="text-center">
                <h3 className="text-info">{users.length - newUsers.length}</h3>
                <p className="text-muted mb-0">Existing Users</p>
              </div>
            </div>
          </div>

          {newUsers.length > 0 ? (
            <div className="mt-4">
              <h6>New users that will receive this coupon:</h6>
              <div style={{ maxHeight: '200px', overflowY: 'auto' }} className="border rounded p-2">
                <div className="row g-2">
                  {newUsers.slice(0, 20).map((user: User) => (
                    <div key={user.id} className="col-md-6">
                      <div className="d-flex align-items-center">
                        <i className="bi bi-person-plus text-primary me-2"></i>
                        <div>
                          <small className="fw-semibold">{user.firstName} {user.lastName}</small>
                          <br />
                          <small className="text-muted">{user.email}</small>
                        </div>
                      </div>
                    </div>
                  ))}
                  {newUsers.length > 20 && (
                    <div className="col-12">
                      <small className="text-muted">... and {newUsers.length - 20} more new users</small>
                    </div>
                  )}
                </div>
              </div>
            </div>
          ) : (
            <div className="mt-4">
              <div className="alert alert-info">
                <i className="bi bi-info-circle me-2"></i>
                <strong>No New Users Found:</strong> There are currently no new users in the system. 
                This coupon will be set up for automatic distribution to future new users.
              </div>
            </div>
          )}
        </div>
      </div>

      <div className="alert alert-info">
        <i className="bi bi-info-circle me-2"></i>
        <strong>Note:</strong> This action will add the selected coupon to all new users 
        {newUsers.length > 0 ? ` (currently ${newUsers.length} users)` : ''}. 
        New users are identified as users with &quot;new&quot; in their first name. 
        Users who already have this coupon will be skipped.
      </div>

      <div className="d-flex gap-2">
        <button type="submit" className="btn btn-primary btn-lg">
          <i className="bi bi-person-plus me-2"></i>
          Add Coupon to New Users
          {newUsers.length > 0 && ` (${newUsers.length})`}
        </button>
        <Link 
          href="/admin/coupons" 
          className="btn btn-outline-secondary btn-lg"
        >
          <i className="bi bi-arrow-left me-1"></i>
          Cancel
        </Link>
      </div>
    </form>
  );
}