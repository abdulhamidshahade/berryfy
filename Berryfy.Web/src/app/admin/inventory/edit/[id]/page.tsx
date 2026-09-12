import { notFound } from 'next/navigation';
import Link from 'next/link';
import { getProduct } from '../../../../../lib/actions/product-actions';
import { updateProduct } from '../../../../../lib/actions/product-actions';
import { getInventoryHistory } from '../../../../../lib/actions/inventory-actions';

interface PageProps {
  params: Promise<{
    id: string;
  }>;
}

async function handleUpdateProduct(formData: FormData) {
  'use server';
  await updateProduct(formData, formData.get('imageUrl') as string);
}

export default async function InventoryEditProductPage({ params }: PageProps) {
  var resolvedSearchParams = await params;
  
  const productId = parseInt(resolvedSearchParams.id);

  if (isNaN(productId)) {
    notFound();
  }

  const product = await getProduct(productId);

  if (!product) {
    notFound();
  }

  return (
    <div className="container-fluid">
      <div className="row">
        <div className="col-12">
          <nav aria-label="breadcrumb" className="mb-4">
            <ol className="breadcrumb">
              <li className="breadcrumb-item">
                <Link href="/admin" className="text-decoration-none">
                  <i className="bi bi-house-door me-1"></i>Admin
                </Link>
              </li>
              <li className="breadcrumb-item">
                <Link href="/admin/inventory" className="text-decoration-none">
                  <i className="bi bi-boxes me-1"></i>Inventory
                </Link>
              </li>
              <li className="breadcrumb-item active" aria-current="page">
                <i className="bi bi-pencil-square me-1"></i>Edit: {product.name}
              </li>
            </ol>
          </nav>

          <div className="row">
            <div className="col-lg-8">
              <div className="card shadow">
                <div className="card-header bg-primary text-white">
                  <h4 className="mb-0">
                    <i className="bi bi-pencil-square me-2"></i>
                    Edit Product Inventory
                  </h4>
                </div>
                <div className="card-body">
                  <form action={handleUpdateProduct}>
                    <input type="hidden" name="id" value={product.id} />

                    <div className="card mb-4">
                      <div className="card-header bg-light">
                        <h6 className="mb-0">Product Information</h6>
                      </div>
                      <div className="card-body">
                        <div className="row">
                          <div className="col-md-6">
                            <label className="form-label">Product Name</label>
                            <input
                              type="text"
                              className="form-control"
                              name="name"
                              defaultValue={product.name}
                              required
                            />
                          </div>
                          <div className="col-md-6">
                            <label className="form-label">SKU</label>
                            <input
                              type="text"
                              className="form-control"
                              name="sku"
                              defaultValue={product.sku}
                              required
                            />
                          </div>
                        </div>
                        <div className="mt-3">
                          <label className="form-label">Description</label>
                          <textarea
                            className="form-control"
                            name="description"
                            rows={3}
                            defaultValue={product.description}
                            required
                          ></textarea>
                        </div>
                      </div>
                    </div>

                    <div className="card mb-4">
                      <div className="card-header bg-warning text-dark">
                        <h6 className="mb-0">
                          <i className="bi bi-boxes me-2"></i>
                          Inventory Settings
                        </h6>
                      </div>
                      <div className="card-body">
                        <div className="row">
                          <div className="col-md-4">
                            <label htmlFor="stockQuantity" className="form-label">
                              <i className="bi bi-box-seam me-1"></i>Current Stock *
                            </label>
                            <input
                              type="number"
                              min="0"
                              className="form-control"
                              id="stockQuantity"
                              name="stockQuantity"
                              defaultValue={product.stockQuantity}
                              required
                            />
                            <div className="form-text">
                              Current total inventory count
                            </div>
                          </div>

                          <div className="col-md-4">
                            <label htmlFor="reservedStock" className="form-label">
                              <i className="bi bi-archive me-1"></i>Reserved Stock
                            </label>
                            <input
                              type="number"
                              min="0"
                              className="form-control"
                              id="reservedStock"
                              name="reservedStock"
                              defaultValue={product.reservedStock}
                            />
                            <div className="form-text">
                              Stock reserved for pending orders
                            </div>
                          </div>

                          <div className="col-md-4">
                            <label htmlFor="lowStockThreshold" className="form-label">
                              <i className="bi bi-exclamation-triangle me-1"></i>Low Stock Alert
                            </label>
                            <input
                              type="number"
                              min="0"
                              className="form-control"
                              id="lowStockThreshold"
                              name="lowStockThreshold"
                              defaultValue={product.lowStockThreshold}
                            />
                            <div className="form-text">
                              Alert when stock falls below this level
                            </div>
                          </div>
                        </div>

                        <div className="row mt-3">
                          <div className="col-12">
                            <div className="alert alert-info">
                              <i className="bi bi-info-circle me-2"></i>
                              <strong>Available Stock:</strong> {product.stockQuantity - product.reservedStock} units
                              <br />
                              <small>This is the actual stock available for new orders.</small>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>

                    <div className="card mb-4">
                      <div className="card-header bg-success text-white">
                        <h6 className="mb-0">
                          <i className="bi bi-currency-dollar me-2"></i>
                          Pricing
                        </h6>
                      </div>
                      <div className="card-body">
                        <div className="row">
                          <div className="col-md-6">
                            <label htmlFor="price" className="form-label">
                              <i className="bi bi-currency-dollar me-1"></i>Price *
                            </label>
                            <input
                              type="number"
                              step="0.01"
                              min="0"
                              className="form-control"
                              id="price"
                              name="price"
                              defaultValue={product.price}
                              required
                            />
                          </div>
                          <div className="col-md-6">
                            <label htmlFor="imageUrl" className="form-label">
                              <i className="bi bi-image me-1"></i>Image URL
                            </label>
                            <input
                              type="url"
                              className="form-control"
                              id="imageUrl"
                              name="imageUrl"
                              defaultValue={product.imageUrl || ''}
                            />
                          </div>
                        </div>
                      </div>
                    </div>

                    <div className="card mb-4">
                      <div className="card-header bg-secondary text-white">
                        <h6 className="mb-0">
                          <i className="bi bi-toggle-on me-2"></i>
                          Product Status
                        </h6>
                      </div>
                      <div className="card-body">
                        <div className="form-check">
                          <input
                            className="form-check-input"
                            type="checkbox"
                            id="isActive"
                            name="isActive"
                            defaultChecked={product.isActive}
                          />
                          <label className="form-check-label" htmlFor="isActive">
                            <strong>Product is Active</strong>
                            <br />
                            <small className="text-muted">
                              Inactive products won&apos;t appear in the store
                            </small>
                          </label>
                        </div>
                      </div>
                    </div>

                    <div className="d-flex gap-2 justify-content-end">
                      <Link
                        href="/admin/inventory"
                        className="btn btn-secondary"
                      >
                        <i className="bi bi-arrow-left me-2"></i>
                        Cancel
                      </Link>
                      <Link
                        href={`/admin/products/${product.id}/edit`}
                        className="btn btn-outline-primary"
                      >
                        <i className="bi bi-gear me-2"></i>
                        Full Edit
                      </Link>
                      <button type="submit" className="btn btn-primary">
                        <i className="bi bi-check-circle me-2"></i>
                        Update Product
                      </button>
                    </div>
                  </form>
                </div>
              </div>
            </div>

            <div className="col-lg-4">
              <div className="card mb-4">
                <div className="card-header">
                  <h6 className="mb-0">
                    <i className="bi bi-graph-up me-2"></i>
                    Current Stock Status
                  </h6>
                </div>
                <div className="card-body">
                  <div className="row text-center">
                    <div className="col-6">
                      <div className="border rounded p-3 mb-3">
                        <h4 className="text-primary mb-1">{product.stockQuantity}</h4>
                        <small className="text-muted">Total Stock</small>
                      </div>
                    </div>
                    <div className="col-6">
                      <div className="border rounded p-3 mb-3">
                        <h4 className="text-warning mb-1">{product.reservedStock}</h4>
                        <small className="text-muted">Reserved</small>
                      </div>
                    </div>
                    <div className="col-6">
                      <div className="border rounded p-3 mb-3">
                        <h4 className="text-success mb-1">{product.stockQuantity - product.reservedStock}</h4>
                        <small className="text-muted">Available</small>
                      </div>
                    </div>
                    <div className="col-6">
                      <div className="border rounded p-3 mb-3">
                        <h4 className="text-info mb-1">{product.lowStockThreshold}</h4>
                        <small className="text-muted">Alert Level</small>
                      </div>
                    </div>
                  </div>

                  {product.stockQuantity <= product.lowStockThreshold && (
                    <div className="alert alert-warning">
                      <i className="bi bi-exclamation-triangle me-2"></i>
                      <strong>Low Stock!</strong>
                    </div>
                  )}
                </div>
              </div>

              <div className="card">
                <div className="card-header">
                  <h6 className="mb-0">
                    <i className="bi bi-lightning me-2"></i>
                    Quick Actions
                  </h6>
                </div>
                <div className="card-body">
                  <div className="d-grid gap-2">
                    <Link
                      href={`/admin/inventory/history/${product.id}`}
                      className="btn btn-outline-primary"
                    >
                      <i className="bi bi-clock-history me-2"></i>
                      View Inventory History
                    </Link>
                    <button
                      type="button"
                      className="btn btn-outline-success"
                      data-bs-toggle="modal"
                      data-bs-target={`#inventoryModal${product.id}`}
                    >
                      <i className="bi bi-boxes me-2"></i>
                      Manage Inventory
                    </button>
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