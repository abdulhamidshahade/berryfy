import { unstable_rethrow } from 'next/navigation';
import { notFound } from 'next/navigation';
import Link from 'next/link';
import { getProduct } from '../../../../../lib/actions/product-actions';
import { getInventoryHistory } from '../../../../../lib/actions/inventory-actions';
import { InventoryChangeType } from '../../../../../types/inventory';

interface PageProps {
  params: Promise<{
    id: string;
  }>;
}

function getChangeTypeIcon(changeType: InventoryChangeType): string {
  switch (changeType) {
    case InventoryChangeType.Restock:
      return 'bi-arrow-up-circle-fill text-success';
    case InventoryChangeType.Purchase:
      return 'bi-cart-fill text-primary';
    case InventoryChangeType.Return:
      return 'bi-arrow-clockwise text-info';
    case InventoryChangeType.StockAdjustment:
      return 'bi-gear-fill text-warning';
    case InventoryChangeType.Damaged:
      return 'bi-exclamation-triangle-fill text-danger';
    case InventoryChangeType.Reserved:
      return 'bi-lock-fill text-secondary';
    case InventoryChangeType.ReleaseReservation:
      return 'bi-unlock-fill text-secondary';
    case InventoryChangeType.InitialStock:
      return 'bi-plus-circle-fill text-primary';
    default:
      return 'bi-circle-fill text-muted';
  }
}

function getChangeTypeName(changeType: InventoryChangeType): string {
  switch (changeType) {
    case InventoryChangeType.Restock:
      return 'Restock';
    case InventoryChangeType.Purchase:
      return 'Purchase/Sale';
    case InventoryChangeType.Return:
      return 'Return';
    case InventoryChangeType.StockAdjustment:
      return 'Manual Adjustment';
    case InventoryChangeType.Damaged:
      return 'Damaged/Lost';
    case InventoryChangeType.Reserved:
      return 'Reserved';
    case InventoryChangeType.ReleaseReservation:
      return 'Released Reservation';
    case InventoryChangeType.InitialStock:
      return 'Initial Stock';
    default:
      return 'Unknown';
  }
}

function formatQuantityChange(quantity: number): { text: string; className: string } {
  if (quantity > 0) {
    return {
      text: `+${quantity}`,
      className: 'text-success fw-bold'
    };
  } else if (quantity < 0) {
    return {
      text: `${quantity}`,
      className: 'text-danger fw-bold'
    };
  } else {
    return {
      text: '0',
      className: 'text-muted'
    };
  }
}

export default async function InventoryHistoryPage({ params }: PageProps) {
  try {
    return await loadInventoryHistoryPage({ params });
  } catch (error) {
    unstable_rethrow(error);
    console.error('Error loading inventory history:', error);
    return (
      <div className="container-fluid">
        <div className="alert alert-danger" role="alert">
          <i className="bi bi-exclamation-triangle me-2"></i>
          <strong>Error!</strong> Unable to load inventory history. Please try again later.
        </div>
      </div>
    );
  }
}

async function loadInventoryHistoryPage({ params }: PageProps) {
  var resolvedSearchParams = await params;
  const productId = parseInt(resolvedSearchParams.id);
  
  if (isNaN(productId)) {
    notFound();
  }

  
    const [product, history] = await Promise.all([
      getProduct(productId),
      getInventoryHistory(productId, 100)
    ]);

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
                  <i className="bi bi-clock-history me-1"></i>History: {product.name}
                </li>
              </ol>
            </nav>

            <div className="card mb-4">
              <div className="card-header bg-primary text-white">
                <div className="row align-items-center">
                  <div className="col">
                    <h4 className="mb-0">
                      <i className="bi bi-clock-history me-2"></i>
                      Inventory History: {product.name}
                    </h4>
                  </div>
                  <div className="col-auto">
                    <Link 
                      href={`/admin/products/${product.id}/edit`}
                      className="btn btn-light btn-sm"
                    >
                      <i className="bi bi-pencil-square me-1"></i>
                      Edit Product
                    </Link>
                  </div>
                </div>
              </div>
              <div className="card-body">
                <div className="row">
                  <div className="col-md-2">
                    {product.imageUrl ? (
                      <img 
                        src={product.imageUrl} 
                        alt={product.name}
                        className="img-fluid rounded"
                        style={{ maxHeight: '100px', objectFit: 'cover' }}
                      />
                    ) : (
                      <div 
                        className="bg-light rounded d-flex align-items-center justify-content-center"
                        style={{ height: '100px' }}
                      >
                        <i className="bi bi-image text-muted fs-1"></i>
                      </div>
                    )}
                  </div>
                  <div className="col-md-10">
                    <div className="row">
                      <div className="col-md-3">
                        <strong>SKU:</strong><br />
                        <code>{product.sku}</code>
                      </div>
                      <div className="col-md-3">
                        <strong>Current Stock:</strong><br />
                        <span className="badge bg-primary fs-6">{product.stockQuantity}</span>
                      </div>
                      <div className="col-md-3">
                        <strong>Reserved:</strong><br />
                        <span className="badge bg-secondary fs-6">{product.reservedStock}</span>
                      </div>
                      <div className="col-md-3">
                        <strong>Available:</strong><br />
                        <span className="badge bg-success fs-6">{product.stockQuantity - product.reservedStock}</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div className="card">
              <div className="card-header">
                <h5 className="mb-0">
                  <i className="bi bi-list-ul me-2"></i>
                  Transaction History
                </h5>
              </div>
              <div className="card-body">
                {history && history.length > 0 ? (
                  <div className="table-responsive">
                    <table className="table table-striped table-hover">
                      <thead className="table-dark">
                        <tr>
                          <th>Date</th>
                          <th>Type</th>
                          <th>Quantity Change</th>
                          <th>Stock After</th>
                          <th>Reference</th>
                          <th>Notes</th>
                        </tr>
                      </thead>
                      <tbody>
                        {history.map((log) => {
                          const quantityChange = formatQuantityChange(log.quantityChanged);
                          return (
                            <tr key={log.id}>
                              <td>
                                <div>
                                  <strong>{new Date(log.createdAt).toLocaleDateString()}</strong>
                                  <br />
                                  <small className="text-muted">
                                    {new Date(log.createdAt).toLocaleTimeString()}
                                  </small>
                                </div>
                              </td>
                              <td>
                                <div className="d-flex align-items-center">
                                  <i className={`${getChangeTypeIcon(log.changeType)} me-2`}></i>
                                  <span>{getChangeTypeName(log.changeType)}</span>
                                </div>
                              </td>
                              <td>
                                <span className={quantityChange.className}>
                                  {quantityChange.text}
                                </span>
                              </td>
                              <td>
                                <strong>{log.currentStockQuantity}</strong>
                              </td>
                              <td>
                                {log.referenceType && log.referenceId > 0 ? (
                                  <div>
                                    <small className="text-muted">{log.referenceType}</small>
                                    <br />
                                    <code>#{log.referenceId}</code>
                                  </div>
                                ) : (
                                  <span className="text-muted">-</span>
                                )}
                              </td>
                              <td>
                                {log.notes ? (
                                  <span className="text-muted">{log.notes}</span>
                                ) : (
                                  <span className="text-muted">-</span>
                                )}
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <div className="text-center py-5">
                    <i className="bi bi-clock-history text-muted" style={{ fontSize: '3rem' }}></i>
                    <h5 className="text-muted mt-3">No inventory history found</h5>
                    <p className="text-muted">
                      Inventory transactions will appear here once stock changes are made.
                    </p>
                    <Link 
                      href="/admin/inventory"
                      className="btn btn-primary"
                    >
                      <i className="bi bi-arrow-left me-2"></i>
                      Back to Inventory
                    </Link>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  
} 