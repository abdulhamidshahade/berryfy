import { UserRow } from './UserRow';
import {getCouponUsers} from '../../lib/actions/coupon-actions';
import Link from 'next/link';


export async function UsersList({ couponId, couponCode }: { couponId: number; couponCode: string }) {
  const users = await getCouponUsers(couponId);

  if (users.length === 0) {
    return (
      <div className="text-center py-5">
        <div className="mb-4">
          <i className="bi bi-people display-1 text-muted"></i>
        </div>
        <h3 className="text-muted">No Users Found</h3>
        <p className="text-muted mb-4">This coupon hasn&apos;t been assigned to any users yet.</p>
        <Link href={`/admin/coupons/${couponId}/add-user`} className="btn btn-primary">
          <i className="bi bi-person-plus me-2"></i>Add User to Coupon
        </Link>
      </div>
    );
  }

  return (
    <div className="table-responsive">
      <table className="table table-hover">
        <thead className="table-light">
          <tr>
            <th scope="col">
              <i className="bi bi-person me-1"></i>
              User
            </th>
            <th scope="col">
              <i className="bi bi-envelope me-1"></i>
              Email
            </th>
            <th scope="col">
              <i className="bi bi-check-circle me-1"></i>
              Usage Status
            </th>
            <th scope="col">
              <i className="bi bi-gear me-1"></i>
              Actions
            </th>
          </tr>
        </thead>
        <tbody>
          {users.map((user) => (
            <UserRow key={user.id} user={user} couponId={couponId} couponCode={couponCode} />
          ))}
        </tbody>
      </table>
    </div>
  );
}