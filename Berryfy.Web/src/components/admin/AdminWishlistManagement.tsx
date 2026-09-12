import { 
  adminDeleteWishlist, 
  adminClearWishlist, 
  adminToggleWishlistVisibility, 
  exportWishlistData 
} from '../../lib/actions/admin-wishlist-actions';
import { WishlistDto, WishlistPriority, WishlistPriorityLabels, GlobalWishlistStatsDto } from '../../types/wishlist';

interface AdminWishlistManagementProps {
  wishlists: WishlistDto[];
  stats: GlobalWishlistStatsDto;
  currentUser: any;
  showModal?: string;
  selectedWishlistId?: string;
  success?: string;
  error?: string;
}

export default function AdminWishlistManagement({ 
  wishlists,
  stats,
  currentUser,
  showModal,
  selectedWishlistId,
  success,
  error
}: AdminWishlistManagementProps) {

  const formatDate = (date: string) => {
    return new Date(date).toLocaleDateString();
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  };

  const getPriorityBadge = (priority: number) => {
    switch (priority) {
      case WishlistPriority.High:
        return 'bg-danger';
      case WishlistPriority.Medium:
        return 'bg-warning text-dark';
      default:
        return 'bg-secondary';
    }
  };

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h1 className="h3 mb-1">
            <i className="bi bi-heart-fill me-2"></i>
            Wishlist Management
          </h1>
          <p className="text-muted mb-0">
            Monitor and manage all user wishlists across the platform
          </p>
        </div>
        <div className="d-flex gap-2">
          <a href="/admin/wishlists?modal=export" className="btn btn-outline-primary">
            <i className="bi bi-download me-1"></i>
            Export Data
          </a>
          <a href="/admin/wishlists" className="btn btn-secondary">
            <i className="bi bi-arrow-clockwise me-1"></i>
            Refresh
          </a>
        </div>
      </div>

      {success && (
        <div className="alert alert-success alert-dismissible fade show" role="alert">
          <i className="bi bi-check-circle me-2"></i>
          {decodeURIComponent(success)}
          <a href="/admin/wishlists" className="btn-close"></a>
        </div>
      )}

      {error && (
        <div className="alert alert-danger alert-dismissible fade show" role="alert">
          <i className="bi bi-exclamation-triangle me-2"></i>
          {decodeURIComponent(error)}
          <a href="/admin/wishlists" className="btn-close"></a>
        </div>
      )}

      <div className="row mb-4">
        <div className="col-lg-3 col-md-6 mb-3">
          <div className="card text-center border-primary">
            <div className="card-body">
              <div className="display-4 text-primary">
                <i className="bi bi-people"></i>
              </div>
              <h5 className="card-title">Total Users</h5>
              <p className="card-text display-6">{stats.totalUsers}</p>
              <small className="text-muted">
                Avg: {stats.averageWishlistsPerUser} wishlists/user
              </small>
            </div>
          </div>
        </div>
        <div className="col-lg-3 col-md-6 mb-3">
          <div className="card text-center border-success">
            <div className="card-body">
              <div className="display-4 text-success">
                <i className="bi bi-list-ul"></i>
              </div>
              <h5 className="card-title">Total Wishlists</h5>
              <p className="card-text display-6">{stats.totalWishlists}</p>
              <small className="text-muted">
                {stats.publicWishlists} public, {stats.privateWishlists} private
              </small>
            </div>
          </div>
        </div>
        <div className="col-lg-3 col-md-6 mb-3">
          <div className="card text-center border-warning">
            <div className="card-body">
              <div className="display-4 text-warning">
                <i className="bi bi-bag-heart"></i>
              </div>
              <h5 className="card-title">Total Items</h5>
              <p className="card-text display-6">{stats.totalItems}</p>
              <small className="text-muted">
                Avg: {stats.averageItemsPerWishlist} items/wishlist
              </small>
            </div>
          </div>
        </div>
        <div className="col-lg-3 col-md-6 mb-3">
          <div className="card text-center border-info">
            <div className="card-body">
              <div className="display-4 text-info">
                <i className="bi bi-currency-dollar"></i>
              </div>
              <h5 className="card-title">Total Value</h5>
              <p className="card-text display-6">{formatCurrency(stats.totalValue)}</p>
              <small className="text-muted">
                Across all wishlists
              </small>
            </div>
          </div>
        </div>
      </div>

      <div className="card mb-4">
        <div className="card-header">
          <h5 className="card-title mb-0">
            <i className="bi bi-activity me-2"></i>
            Recent Activity (Last 5 Days)
          </h5>
        </div>
        <div className="card-body">
          <div className="table-responsive">
            <table className="table table-sm">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>New Wishlists</th>
                  <th>New Items</th>
                </tr>
              </thead>
              <tbody>
                {stats.recentActivity.map((activity, index) => (
                  <tr key={index}>
                    <td>{formatDate(activity.date)}</td>
                    <td>
                      <span className="badge bg-success">{activity.newWishlists}</span>
                    </td>
                    <td>
                      <span className="badge bg-info">{activity.newItems}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <div className="card">
        <div className="card-header d-flex justify-content-between align-items-start align-items-sm-center flex-wrap gap-2">
          <h5 className="card-title mb-0">
            <i className="bi bi-table me-2"></i>
            All Wishlists ({wishlists.length})
          </h5>
          <div className="d-flex gap-2">
            <select className="form-select form-select-sm" style={{ width: 'auto' }}>
              <option value="">All Users</option>
              <option value="public">Public Only</option>
              <option value="private">Private Only</option>
            </select>
          </div>
        </div>
        <div className="card-body p-0">
          {wishlists.length === 0 ? (
            <div className="text-center py-5">
              <div className="display-1 text-muted mb-3">
                <i className="bi bi-heart"></i>
              </div>
              <h3>No Wishlists Found</h3>
              <p className="text-muted">No wishlists have been created yet.</p>
            </div>
          ) : (
            <div className="table-responsive">
              <table className="table table-hover mb-0">
                <thead className="table-light">
                  <tr>
                    <th>Wishlist</th>
                    <th>Owner</th>
                    <th>Items</th>
                    <th>Value</th>
                    <th>Visibility</th>
                    <th>Created</th>
                    <th>Updated</th>
                    <th style={{ width: '150px' }}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {wishlists.map((wishlist) => (
                    <tr key={wishlist.id}>
                      <td>
                        <div className="d-flex align-items-center">
                          <div>
                            <div className="fw-medium">{wishlist.name}</div>
                            {wishlist.isDefault && (
                              <span className="badge bg-primary">Default</span>
                            )}
                          </div>
                        </div>
                      </td>
                      <td>
                        <div className="text-muted small">
                          ID: {wishlist.userId}
                        </div>
                      </td>
                      <td>
                        <span className="badge bg-info">{wishlist.itemCount}</span>
                      </td>
                      <td>
                        <span className="fw-medium">{formatCurrency(wishlist.totalValue)}</span>
                      </td>
                      <td>
                        <form action={adminToggleWishlistVisibility} style={{ display: 'inline' }}>
                          <input type="hidden" name="wishlistId" value={wishlist.id} />
                          <input type="hidden" name="isPublic" value={wishlist.isPublic ? 'false' : 'true'} />
                          <button 
                            type="submit" 
                            className={`btn btn-sm ${wishlist.isPublic ? 'btn-success' : 'btn-outline-secondary'}`}
                            title={wishlist.isPublic ? 'Make Private' : 'Make Public'}
                          >
                            <i className={`bi ${wishlist.isPublic ? 'bi-eye-fill' : 'bi-eye-slash'}`}></i>
                            {wishlist.isPublic ? ' Public' : ' Private'}
                          </button>
                        </form>
                      </td>
                      <td>
                        <small>{formatDate(wishlist.createdDate)}</small>
                      </td>
                      <td>
                        <small>{formatDate(wishlist.updatedDate)}</small>
                      </td>
                      <td>
                        <div className="btn-group" role="group">
                          <a 
                            href={`/admin/wishlists?modal=view&id=${wishlist.id}`} 
                            className="btn btn-sm btn-outline-primary"
                            title="View Details"
                          >
                            <i className="bi bi-eye"></i>
                          </a>
                          {wishlist.itemCount > 0 && (
                            <a 
                              href={`/admin/wishlists?modal=clear&id=${wishlist.id}`} 
                              className="btn btn-sm btn-outline-warning"
                              title="Clear Items"
                            >
                              <i className="bi bi-trash3"></i>
                            </a>
                          )}
                          {!wishlist.isDefault && (
                            <a 
                              href={`/admin/wishlists?modal=delete&id=${wishlist.id}`} 
                              className="btn btn-sm btn-outline-danger"
                              title="Delete Wishlist"
                            >
                              <i className="bi bi-trash"></i>
                            </a>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      {showModal === 'export' && <ExportDataOverlay />}
      {showModal === 'view' && selectedWishlistId && (
        <ViewWishlistOverlay wishlist={wishlists.find(w => w.id === parseInt(selectedWishlistId))} />
      )}
      {showModal === 'clear' && selectedWishlistId && (
        <AdminClearWishlistOverlay wishlist={wishlists.find(w => w.id === parseInt(selectedWishlistId))} />
      )}
      {showModal === 'delete' && selectedWishlistId && (
        <AdminDeleteWishlistOverlay wishlist={wishlists.find(w => w.id === parseInt(selectedWishlistId))} />
      )}
    </>
  );
}

function ExportDataOverlay() {
  return (
    <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">
              <i className="bi bi-download me-2"></i>
              Export Wishlist Data
            </h5>
            <a href="/admin/wishlists" className="btn-close"></a>
          </div>
          <div className="modal-body">
            <div className="alert alert-info">
              <i className="bi bi-info-circle me-2"></i>
              This will export all wishlist data including user information, items, and statistics.
            </div>
            
            <div className="d-grid gap-2">
              <a 
                href="/admin/wishlists/export?format=json" 
                className="btn btn-outline-primary"
                target="_blank"
              >
                <i className="bi bi-filetype-json me-2"></i>
                Download as JSON
              </a>
              <a 
                href="/admin/wishlists/export?format=csv" 
                className="btn btn-outline-success"
                target="_blank"
              >
                <i className="bi bi-filetype-csv me-2"></i>
                Download as CSV
              </a>
              <a 
                href="/admin/wishlists/export?format=xlsx" 
                className="btn btn-outline-warning"
                target="_blank"
              >
                <i className="bi bi-file-earmark-excel me-2"></i>
                Download as Excel
              </a>
            </div>
          </div>
          <div className="modal-footer">
            <a href="/admin/wishlists" className="btn btn-secondary">Close</a>
          </div>
        </div>
      </div>
    </div>
  );
}

function ViewWishlistOverlay({ wishlist }: { wishlist?: WishlistDto }) {
  if (!wishlist) return null;

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  };

  const getPriorityBadge = (priority: number) => {
    switch (priority) {
      case WishlistPriority.High:
        return 'bg-danger';
      case WishlistPriority.Medium:
        return 'bg-warning text-dark';
      default:
        return 'bg-secondary';
    }
  };

  return (
    <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
      <div className="modal-dialog modal-xl">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">
              <i className="bi bi-eye me-2"></i>
              Wishlist Details: {wishlist.name}
            </h5>
            <a href="/admin/wishlists" className="btn-close"></a>
          </div>
          <div className="modal-body">
            <div className="row mb-4">
              <div className="col-md-6">
                <table className="table table-borderless">
                  <tbody>
                    <tr>
                      <th>Owner ID:</th>
                      <td>{wishlist.userId}</td>
                    </tr>
                    <tr>
                      <th>Visibility:</th>
                      <td>
                        <span className={`badge ${wishlist.isPublic ? 'bg-success' : 'bg-secondary'}`}>
                          {wishlist.isPublic ? 'Public' : 'Private'}
                        </span>
                      </td>
                    </tr>
                    <tr>
                      <th>Is Default:</th>
                      <td>
                        {wishlist.isDefault ? (
                          <span className="badge bg-primary">Yes</span>
                        ) : (
                          <span className="text-muted">No</span>
                        )}
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
              <div className="col-md-6">
                <table className="table table-borderless">
                  <tbody>
                    <tr>
                      <th>Items:</th>
                      <td>{wishlist.itemCount}</td>
                    </tr>
                    <tr>
                      <th>Total Value:</th>
                      <td>{formatCurrency(wishlist.totalValue)}</td>
                    </tr>
                    <tr>
                      <th>Created:</th>
                      <td>{new Date(wishlist.createdDate).toLocaleString()}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            {wishlist.items.length > 0 && (
              <div>
                <h6>Items ({wishlist.items.length})</h6>
                <div className="table-responsive" style={{ maxHeight: '300px', overflowY: 'auto' }}>
                  <table className="table table-sm table-hover">
                    <thead className="table-light sticky-top">
                      <tr>
                        <th>Product</th>
                        <th>Price</th>
                        <th>Priority</th>
                        <th>Added</th>
                        <th>Notes</th>
                      </tr>
                    </thead>
                    <tbody>
                      {wishlist.items.map((item) => (
                        <tr key={item.id}>
                          <td>
                            <div className="d-flex align-items-center">
                              <img 
                                src={item.product.imageUrl || '/placeholder-product.jpg'} 
                                alt={item.product.name}
                                className="rounded me-2"
                                style={{ width: '40px', height: '40px', objectFit: 'cover' }}
                              />
                              <div>
                                <div className="fw-medium">{item.product.name}</div>
                                <small className="text-muted">ID: {item.productId}</small>
                              </div>
                            </div>
                          </td>
                          <td>{formatCurrency(item.product.price)}</td>
                          <td>
                            <span className={`badge ${getPriorityBadge(item.priority)}`}>
                              {WishlistPriorityLabels[item.priority as WishlistPriority]}
                            </span>
                          </td>
                          <td>
                            <small>{new Date(item.addedDate).toLocaleDateString()}</small>
                          </td>
                          <td>
                            {item.notes && (
                              <small className="text-muted">{item.notes}</small>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </div>
          <div className="modal-footer">
            <a href="/admin/wishlists" className="btn btn-secondary">Close</a>
          </div>
        </div>
      </div>
    </div>
  );
}

function AdminClearWishlistOverlay({ wishlist }: { wishlist?: WishlistDto }) {
  if (!wishlist) return null;

  return (
    <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">
              <i className="bi bi-trash3 text-warning me-2"></i>
              Clear Wishlist Items
            </h5>
            <a href="/admin/wishlists" className="btn-close"></a>
          </div>
          <div className="modal-body">
            <div className="alert alert-warning">
              <i className="bi bi-exclamation-triangle-fill me-2"></i>
              <strong>Warning!</strong> This action cannot be undone.
            </div>
            
            <p>Are you sure you want to remove all items from <strong>&quot;{wishlist.name}&quot;</strong>?</p>
            
            <div className="alert alert-info">
              <i className="bi bi-info-circle me-2"></i>
              This will remove all <strong>{wishlist.itemCount} items</strong> from the user&apos;s wishlist.
            </div>
          </div>
          <div className="modal-footer">
            <a href="/admin/wishlists" className="btn btn-secondary">Cancel</a>
                            <form action={adminClearWishlist} style={{ display: 'inline' }}>
              <input type="hidden" name="wishlistId" value={wishlist.id} />
              <button type="submit" className="btn btn-warning">
                <i className="bi bi-trash3 me-1"></i>
                Clear All Items
              </button>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
}

function AdminDeleteWishlistOverlay({ wishlist }: { wishlist?: WishlistDto }) {
  if (!wishlist) return null;

  return (
    <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">
              <i className="bi bi-exclamation-triangle text-danger me-2"></i>
              Delete Wishlist
            </h5>
            <a href="/admin/wishlists" className="btn-close"></a>
          </div>
          <form action={adminDeleteWishlist}>
            <input type="hidden" name="wishlistId" value={wishlist.id} />
            <div className="modal-body">
              <div className="alert alert-danger">
                <i className="bi bi-exclamation-triangle-fill me-2"></i>
                <strong>Danger!</strong> This action cannot be undone.
              </div>
              
              <p>Are you sure you want to permanently delete <strong>&quot;{wishlist.name}&quot;</strong>?</p>
              
              {wishlist.itemCount > 0 && (
                <div className="alert alert-warning">
                  <i className="bi bi-info-circle me-2"></i>
                  This wishlist contains <strong>{wishlist.itemCount} items</strong> that will be permanently removed.
                </div>
              )}

              <div className="mb-3">
                <label htmlFor="confirmDelete" className="form-label">
                  Type <strong>DELETE</strong> to confirm:
                </label>
                <input
                  type="text"
                  className="form-control"
                  id="confirmDelete"
                  name="confirmDelete"
                  placeholder="DELETE"
                  required
                />
              </div>
            </div>
            <div className="modal-footer">
              <a href="/admin/wishlists" className="btn btn-secondary">Cancel</a>
              <button type="submit" className="btn btn-danger">
                <i className="bi bi-trash me-1"></i>
                Delete Wishlist
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
} 