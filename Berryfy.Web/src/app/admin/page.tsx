import { unstable_rethrow } from 'next/navigation';
﻿import Link from 'next/link';
import { getProducts } from '../../lib/actions/product-actions';
import { IOrderService } from '../../lib/services/order/interface';
import { OrderService } from '../../lib/services/order/service';
import { getStatusColor, getStatusText } from './orders/page';
import { OrderStatus } from '../../types/order';
import { IUserService } from '../../lib/services/user/interface';
import { UserService } from '../../lib/services/user/service';
export const dynamic = 'force-dynamic';
export default async function AdminDashboard() {
  try {
    return await loadAdminDashboard();
  } catch (error) {
    unstable_rethrow(error);
    console.error('Error loading admin dashboard:', error);
    return (
      <div className="container-fluid">
        <div className="text-center py-5">
          <i className="bi bi-exclamation-triangle fs-1 text-danger mb-3 d-block"></i>
          <h2>Error Loading Dashboard</h2>
          <p className="text-muted">
            {error instanceof Error ? error.message : 'An unexpected error occurred'}
          </p>
          <Link 
            href="/admin" 
            className="btn btn-primary"
          >
            <i className="bi bi-arrow-clockwise me-2"></i>
            Reload Dashboard
          </Link>
        </div>
      </div>
    );
  }
}

async function loadAdminDashboard() {
  
    const products = await getProducts();

    const totalProducts = products.length;
    const activeProducts = products.filter(p => p.isActive).length;
    const lowStockProducts = products.filter(p => p.stockQuantity <= p.lowStockThreshold).length;
    const outOfStockProducts = products.filter(p => p.stockQuantity <= p.reservedStock).length;

    const orderService: IOrderService = new OrderService();

    const orders = await orderService.getAllOrders();

    const userService:IUserService = new UserService();

    const compLetedOrders = orders.filter(o => o.status === OrderStatus.Completed);
    const totalSales = compLetedOrders.reduce((sum, order) => sum + order.total, 0);

    return (
      <div className="container-fluid">
        <div className="row mb-4">
          <div className="col-12">
            <div className="d-flex justify-content-between align-items-start align-items-sm-center flex-wrap gap-2">
              <div>
                <h1 className="h3 mb-1">Dashboard Overview</h1>
                <p className="text-muted mb-0">Welcome back! Here&apos;s what&apos;s happening in your store.</p>
              </div>
              <div className="text-end">
                <small className="text-muted d-block">Last updated</small>
                <strong>{new Date().toLocaleString()}</strong>
              </div>
            </div>
          </div>
        </div>

        <div className="row mb-4">
          <div className="col-md-3">
            <div className="card bg-primary text-white">
              <div className="card-body">
                <div className="d-flex justify-content-between align-items-start align-items-sm-center flex-wrap gap-2">
                  <div>
                    <h4 className="mb-1">{totalSales}</h4>
                    <small className="opacity-75">Total Sales</small>
                  </div>
                  <i className="bi bi-currency-dollar fs-1 opacity-75"></i>
                </div>
                <div className="mt-2">
                  <small className="opacity-75">
                    <i className="bi bi-arrow-up me-1"></i>
                    12% vs last month (for testing)
                  </small>
                </div>
              </div>
            </div>
          </div>

          <div className="col-md-3">
            <div className="card bg-success text-white">
              <div className="card-body">
                <div className="d-flex justify-content-between align-items-start align-items-sm-center flex-wrap gap-2">
                  <div>
                    <h4 className="mb-1">{orders.length}</h4>
                    <small className="opacity-75">Total Orders</small>
                  </div>
                  <i className="bi bi-receipt fs-1 opacity-75"></i>
                </div>
                <div className="mt-2">
                  <small className="opacity-75">
                    <i className="bi bi-arrow-up me-1"></i>
                    8% vs last month (for testing)
                  </small>
                </div>
              </div>
            </div>
          </div>

          <div className="col-md-3">
            <div className="card bg-info text-white">
              <div className="card-body">
                <div className="d-flex justify-content-between align-items-start align-items-sm-center flex-wrap gap-2">
                  <div>
                    <h4 className="mb-1">{totalProducts}</h4>
                    <small className="opacity-75">Total Products</small>
                  </div>
                  <i className="bi bi-box fs-1 opacity-75"></i>
                </div>
                <div className="mt-2">
                  <small className="opacity-75">
                    {activeProducts} active products
                  </small>
                </div>
              </div>
            </div>
          </div>

          <div className="col-md-3">
            <div className="card bg-warning text-white">
              <div className="card-body">
                <div className="d-flex justify-content-between align-items-start align-items-sm-center flex-wrap gap-2">
                  <div>
                    <h4 className="mb-1">{(await userService.getAll()).length}</h4>
                    <small className="opacity-75">Total Customers</small>
                  </div>
                  <i className="bi bi-people fs-1 opacity-75"></i>
                </div>
                <div className="mt-2">
                  <small className="opacity-75">
                    <i className="bi bi-arrow-up me-1"></i>
                    15% vs last month (for testing)
                  </small>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="row mb-4">
          <div className="col-md-6">
            <div className="card">
              <div className="card-header">
                <h5 className="mb-0">
                  <i className="bi bi-exclamation-triangle text-warning me-2"></i>
                  Inventory Alerts
                </h5>
              </div>
              <div className="card-body">
                {lowStockProducts > 0 || outOfStockProducts > 0 ? (
                  <div>
                    {outOfStockProducts > 0 && (
                      <div className="alert alert-danger py-2 mb-2">
                        <i className="bi bi-x-circle me-2"></i>
                        <strong>{outOfStockProducts}</strong> products are out of stock
                        <Link href="/admin/inventory" className="btn btn-sm btn-outline-danger ms-2">
                          View All
                        </Link>
                      </div>
                    )}
                    {lowStockProducts > 0 && (
                      <div className="alert alert-warning py-2 mb-2">
                        <i className="bi bi-exclamation-triangle me-2"></i>
                        <strong>{lowStockProducts}</strong> products are low in stock
                        <Link href="/admin/inventory" className="btn btn-sm btn-outline-warning ms-2">
                          Manage
                        </Link>
                      </div>
                    )}
                  </div>
                ) : (
                  <div className="text-center text-success py-3">
                    <i className="bi bi-check-circle fs-2 d-block mb-2"></i>
                    <p className="mb-0">All products are in good stock!</p>
                  </div>
                )}
              </div>
            </div>
          </div>

          <div className="col-md-6">
            <div className="card">
              <div className="card-header">
                <h5 className="mb-0">
                  <i className="bi bi-lightning text-primary me-2"></i>
                  Quick Actions
                </h5>
              </div>
              <div className="card-body">
                <div className="d-grid gap-2">
                  <Link href="/admin/products/create" className="btn btn-outline-primary">
                    <i className="bi bi-plus-circle me-2"></i>
                    Add New Product
                  </Link>
                  <Link href="/admin/coupons/create" className="btn btn-outline-success">
                    <i className="bi bi-tag me-2"></i>
                    Create Coupon
                  </Link>
                  <Link href="/admin/orders" className="btn btn-outline-info">
                    <i className="bi bi-receipt me-2"></i>
                    View Orders
                  </Link>
                  <Link href="/admin/reports" className="btn btn-outline-secondary">
                    <i className="bi bi-file-earmark-text me-2"></i>
                    Generate Report
                  </Link>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="row">
          <div className="col-md-6">
            <div className="card">
              <div className="card-header d-flex justify-content-between align-items-start align-items-sm-center flex-wrap gap-2">
                <h5 className="mb-0">Recent Orders</h5>
                <Link href="/admin/orders" className="btn btn-sm btn-outline-primary">
                  View All
                </Link>
              </div>
              <div className="card-body p-0">
                <div className="table-responsive">
                  <table className="table table-hover mb-0">
                    <thead className="table-light">
                      <tr>
                        <th>Order #</th>
                        <th>Customer</th>
                        <th>Amount</th>
                        <th>Status</th>
                      </tr>
                    </thead>
                    <tbody>
                      {orders.map(order => (
                        <tr key={order.id}>
                          <td>{order.referenceNumber}</td>
                          <td>{order.userId}</td>
                          <td>${order.total.toFixed(2)}</td>
                          <td>
                            <span
                              className="badge"
                              style={{ backgroundColor: getStatusColor(order.status), color: '#fff' }}
                            >
                              {getStatusText(order.status)}
                            </span>
                          </td>
                        </tr>
                      ))}

                    </tbody>
                  </table>
                </div>
              </div>

            </div>
          </div>

          <div className="col-md-6">
            <div className="card">
              <div className="card-header">
                <h5 className="mb-0">Sales Overview</h5>
              </div>
              <div className="card-body">
                <div className="text-center py-5">
                  <i className="bi bi-bar-chart fs-1 text-muted mb-3 d-block"></i>
                  <h6>Sales Chart</h6>
                  <p className="text-muted mb-3">Interactive sales chart will be displayed here</p>
                  <small className="text-muted">Chart component integration coming soon</small>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="row mt-4">
          <div className="col-12">
            <div className="card">
              <div className="card-header">
                <h5 className="mb-0">
                  <i className="bi bi-gear text-success me-2"></i>
                  System Status
                </h5>
              </div>
              <div className="card-body">
                <div className="row">
                  <div className="col-md-3">
                    <div className="d-flex align-items-center">
                      <i className="bi bi-check-circle text-success fs-4 me-2"></i>
                      <div>
                        <strong>Database</strong>
                        <small className="d-block text-muted">Connected</small>
                      </div>
                    </div>
                  </div>
                  <div className="col-md-3">
                    <div className="d-flex align-items-center">
                      <i className="bi bi-check-circle text-success fs-4 me-2"></i>
                      <div>
                        <strong>API Services</strong>
                        <small className="d-block text-muted">Online</small>
                      </div>
                    </div>
                  </div>
                  <div className="col-md-3">
                    <div className="d-flex align-items-center">
                      <i className="bi bi-check-circle text-success fs-4 me-2"></i>
                      <div>
                        <strong>Cache</strong>
                        <small className="d-block text-muted">Operational</small>
                      </div>
                    </div>
                  </div>
                  <div className="col-md-3">
                    <div className="d-flex align-items-center">
                      <i className="bi bi-check-circle text-success fs-4 me-2"></i>
                      <div>
                        <strong>Storage</strong>
                        <small className="d-block text-muted">Available</small>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  
}