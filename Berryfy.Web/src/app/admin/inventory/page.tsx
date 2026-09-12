import { unstable_rethrow } from 'next/navigation';
﻿import Link from 'next/link';
import { getProducts } from '../../../lib/actions/product-actions';
import { getInventoryHistory } from '../../../lib/actions/inventory-actions';
import InventoryManager from '../../../components/admin/InventoryManager';
import { InventoryLogDto } from '../../../types/inventory';

export const dynamic = 'force-dynamic';
interface ProductWithHistory {
  product: any;
  inventoryHistory: InventoryLogDto[];
}

export default async function AdminInventoryPage() {
  try {
    return await loadAdminInventoryPage();
  } catch (error) {
    unstable_rethrow(error);
    console.error('Error loading inventory page:', error);
    return (
      <div className="container-fluid">
        <div className="text-center py-5">
          <i className="bi bi-exclamation-triangle fs-1 text-danger mb-3 d-block"></i>
          <h2>Error Loading Inventory</h2>
          <p className="text-muted">
            {error instanceof Error ? error.message : 'An unexpected error occurred'}
          </p>
          <div className="d-flex gap-2 justify-content-center">
            <Link href="/admin" className="btn btn-secondary">
              <i className="bi bi-arrow-left me-2"></i>
              Back to Dashboard
            </Link>
            <button 
              onClick={() => window.location.reload()} 
              className="btn btn-primary"
            >
              <i className="bi bi-arrow-clockwise me-2"></i>
              Try Again
            </button>
          </div>
        </div>
      </div>
    );
  }
}

async function loadAdminInventoryPage() {
  
    const products = await getProducts();

    const productsWithHistory: ProductWithHistory[] = await Promise.all(
      products.map(async (product) => {
        try {
          const inventoryHistory = await getInventoryHistory(product.id, 10);
          return { product, inventoryHistory: inventoryHistory || [] };
        } catch (error) {
          console.error(`Error fetching history for product ${product.id}:`, error);
          return { product, inventoryHistory: [] };
        }
      })
    );

    const lowStockProducts = products.filter(p => p.stockQuantity <= p.lowStockThreshold);
    const outOfStockProducts = products.filter(p => p.stockQuantity <= p.reservedStock);
    const totalStockValue = products.reduce((sum, p) => sum + (p.stockQuantity * p.price), 0);
    const totalReservedStock = products.reduce((sum, p) => sum + p.reservedStock, 0);

    return (
      <div className="container-fluid">
        <div className="row mb-4">
          <div className="col-12">
            <div className="d-flex justify-content-between align-items-start align-items-sm-center flex-wrap gap-2">
              <div>
                <h1 className="h3 mb-1">Inventory Management</h1>
                <p className="text-muted mb-0">Monitor and manage your product inventory</p>
              </div>
              <div className="d-flex gap-2">
                <Link href="/admin/inventory/adjustments" className="btn btn-outline-primary">
                  <i className="bi bi-arrow-up-down me-2"></i>
                  Stock Adjustments
                </Link>
                <Link href="/admin/inventory/logs" className="btn btn-outline-secondary">
                  <i className="bi bi-list-ul me-2"></i>
                  View Logs
                </Link>
              </div>
            </div>
          </div>
        </div>

        <div className="row mb-4">
          <div className="col-md-3">
            <div className="card border-primary">
              <div className="card-body text-center">
                <h4 className="text-primary mb-1">{products.reduce((sum, p) => sum + p.stockQuantity, 0)}</h4>
                <small className="text-muted">Total Units</small>
              </div>
            </div>
          </div>
          <div className="col-md-3">
            <div className="card border-warning">
              <div className="card-body text-center">
                <h4 className="text-warning mb-1">{totalReservedStock}</h4>
                <small className="text-muted">Reserved Stock</small>
              </div>
            </div>
          </div>
          <div className="col-md-3">
            <div className="card border-success">
              <div className="card-body text-center">
                <h4 className="text-success mb-1">${totalStockValue.toFixed(0)}</h4>
                <small className="text-muted">Stock Value</small>
              </div>
            </div>
          </div>
          <div className="col-md-3">
            <div className="card border-danger">
              <div className="card-body text-center">
                <h4 className="text-danger mb-1">{lowStockProducts.length}</h4>
                <small className="text-muted">Low Stock Alerts</small>
              </div>
            </div>
          </div>
        </div>

        {(lowStockProducts.length > 0 || outOfStockProducts.length > 0) && (
          <div className="row mb-4">
            <div className="col-12">
              <div className="card border-warning">
                <div className="card-header bg-warning bg-opacity-10">
                  <h5 className="mb-0 text-warning">
                    <i className="bi bi-exclamation-triangle me-2"></i>
                    Inventory Alerts
                  </h5>
                </div>
                <div className="card-body">
                  {outOfStockProducts.length > 0 && (
                    <div className="alert alert-danger mb-2">
                      <strong>{outOfStockProducts.length}</strong> products are completely out of stock!
                      <div className="mt-2">
                        {outOfStockProducts.slice(0, 3).map(product => (
                          <span key={product.id} className="badge bg-danger me-1">{product.name}</span>
                        ))}
                        {outOfStockProducts.length > 3 && <span className="text-muted">and {outOfStockProducts.length - 3} more...</span>}
                      </div>
                    </div>
                  )}
                  {lowStockProducts.length > 0 && (
                    <div className="alert alert-warning mb-0">
                      <strong>{lowStockProducts.length}</strong> products are running low on stock.
                      <div className="mt-2">
                        {lowStockProducts.slice(0, 5).map(product => (
                          <span key={product.id} className="badge bg-warning text-dark me-1">
                            {product.name} ({product.stockQuantity - product.reservedStock} left)
                          </span>
                        ))}
                        {lowStockProducts.length > 5 && <span className="text-muted">and {lowStockProducts.length - 5} more...</span>}
                      </div>
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        )}

        <div className="row mb-4">
          <div className="col-12">
            <div className="card">
              <div className="card-header d-flex justify-content-between align-items-start align-items-sm-center flex-wrap gap-2">
                <h5 className="mb-0">Product Inventory</h5>
                <div className="d-flex gap-2">
                  <div className="input-group" style={{ width: '300px' }}>
                    <span className="input-group-text">
                      <i className="bi bi-search"></i>
                    </span>
                    <input
                      type="text"
                      className="form-control"
                      placeholder="Search products..."
                    />
                  </div>
                  <select className="form-select" style={{ width: '150px' }}>
                    <option value="">All Status</option>
                    <option value="in-stock">In Stock</option>
                    <option value="low-stock">Low Stock</option>
                    <option value="out-of-stock">Out of Stock</option>
                  </select>
                </div>
              </div>
              <div className="card-body p-0">
                <div className="table-responsive">
                  <table className="table table-hover mb-0">
                    <thead className="table-light">
                      <tr>
                        <th>Product</th>
                        <th>SKU</th>
                        <th>Total Stock</th>
                        <th>Available</th>
                        <th>Reserved</th>
                        <th>Value</th>
                        <th>Status</th>
                        <th>Actions</th>
                      </tr>
                    </thead>
                    <tbody>
                      {products.map((product) => {
                        const availableStock = product.stockQuantity - product.reservedStock;
                        const stockStatus = product.stockQuantity <= product.lowStockThreshold
                          ? 'danger'
                          : product.stockQuantity <= product.lowStockThreshold * 2
                          ? 'warning'
                          : 'success';
                        
                        return (
                          <tr key={product.id}>
                            <td>
                              <div className="d-flex align-items-center">
                                {product.imageUrl ? (
                                  <img 
                                    src={product.imageUrl} 
                                    alt={product.name}
                                    className="rounded me-2"
                                    style={{ width: '40px', height: '40px', objectFit: 'cover' }}
                                  />
                                ) : (
                                  <div 
                                    className="bg-light rounded me-2 d-flex align-items-center justify-content-center"
                                    style={{ width: '40px', height: '40px' }}
                                  >
                                    <i className="bi bi-image text-muted"></i>
                                  </div>
                                )}
                                <div>
                                  <strong>{product.name}</strong>
                                  <br />
                                  <small className="text-muted">${product.price.toFixed(2)}</small>
                                </div>
                              </div>
                            </td>
                            <td>
                              <code>{product.sku}</code>
                            </td>
                            <td>
                              <strong>{product.stockQuantity}</strong>
                            </td>
                            <td>
                              <span className={`badge bg-${stockStatus}`}>
                                {availableStock}
                              </span>
                            </td>
                            <td>
                              {product.reservedStock > 0 ? (
                                <span className="badge bg-warning text-dark">{product.reservedStock}</span>
                              ) : (
                                <span className="text-muted">0</span>
                              )}
                            </td>
                            <td>
                              <strong>${(product.stockQuantity * product.price).toFixed(2)}</strong>
                            </td>
                            <td>
                              {availableStock <= 0 ? (
                                <span className="badge bg-danger">Out of Stock</span>
                              ) : product.stockQuantity <= product.lowStockThreshold ? (
                                <span className="badge bg-warning text-dark">Low Stock</span>
                              ) : (
                                <span className="badge bg-success">In Stock</span>
                              )}
                            </td>
                            <td>
                              <div className="btn-group btn-group-sm">
                                <button 
                                  className="btn btn-outline-primary"
                                  data-bs-toggle="modal"
                                  data-bs-target={`#inventoryModal${product.id}`}
                                  title="Manage Inventory"
                                >
                                  <i className="bi bi-boxes"></i>
                                </button>
                                <Link 
                                  href={`/admin/inventory/edit/${product.id}`}
                                  className="btn btn-outline-warning"
                                  title="Edit Inventory"
                                >
                                  <i className="bi bi-pencil-square"></i>
                                </Link>
                                <Link 
                                  href={`/admin/products/${product.id}/edit`}
                                  className="btn btn-outline-secondary"
                                  title="Edit Product"
                                >
                                  <i className="bi bi-pencil"></i>
                                </Link>
                              </div>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="row">
          <div className="col-12">
            <div className="card">
              <div className="card-header">
                <h6 className="mb-0">
                  <i className="bi bi-lightning me-2"></i>
                  Quick Actions
                </h6>
              </div>
              <div className="card-body">
                <div className="row">
                  <div className="col-md-3">
                    <button className="btn btn-outline-success w-100">
                      <i className="bi bi-plus-circle me-2"></i>
                      Bulk Restock
                    </button>
                  </div>
                  <div className="col-md-3">
                    <button className="btn btn-outline-warning w-100">
                      <i className="bi bi-exclamation-triangle me-2"></i>
                      Update Low Stock Alerts
                    </button>
                  </div>
                  <div className="col-md-3">
                    <button className="btn btn-outline-info w-100">
                      <i className="bi bi-download me-2"></i>
                      Export Inventory Report
                    </button>
                  </div>
                  <div className="col-md-3">
                    <button className="btn btn-outline-secondary w-100">
                      <i className="bi bi-arrow-clockwise me-2"></i>
                      Refresh All Stock
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {productsWithHistory.map(({ product, inventoryHistory }) => (
          <InventoryManager 
            key={product.id} 
            product={product} 
            inventoryHistory={inventoryHistory}
          />
        ))}
      </div>
    );
  
} 