import { 
  createWishlist, 
  updateWishlist, 
  deleteWishlist, 
  removeFromWishlist,
  bulkRemoveItems,
  clearWishlist,
  shareWishlist,
  duplicateWishlist,
  updateWishlistItem
} from '../../lib/actions/wishlist-actions';
import { WishlistDto, WishlistPriority, WishlistPriorityLabels } from '../../types/wishlist';

interface WishlistManagementProps {
  wishlists: WishlistDto[];
  currentUser: any;
  showModal?: string;
  selectedWishlistId?: string;
  selectedProductId?: string;
  success?: string;
  error?: string;
}

export default function WishlistManagement({ 
  wishlists,
  currentUser,
  showModal,
  selectedWishlistId,
  selectedProductId,
  success,
  error
}: WishlistManagementProps) {

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

  const totalItems = wishlists.reduce((sum, wishlist) => sum + wishlist.itemCount, 0);
  const totalValue = wishlists.reduce((sum, wishlist) => sum + wishlist.totalValue, 0);

  return (
    <>
      {/* Header */}
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h1 className="h3 mb-1">
            <i className="bi bi-heart me-2"></i>
            My Wishlists
          </h1>
          <p className="text-muted mb-0">
            Manage your saved products and wish lists
          </p>
        </div>
        <div>
          <a href="/profile/wishlist?modal=create" className="btn btn-primary">
            <i className="bi bi-plus-lg me-1"></i>
            Create Wishlist
          </a>
        </div>
      </div>

      {/* Alert Messages */}
      {success && (
        <div className="alert alert-success alert-dismissible fade show" role="alert">
          <i className="bi bi-check-circle me-2"></i>
          {decodeURIComponent(success)}
          <a href="/profile/wishlist" className="btn-close"></a>
        </div>
      )}

      {error && (
        <div className="alert alert-danger alert-dismissible fade show" role="alert">
          <i className="bi bi-exclamation-triangle me-2"></i>
          {decodeURIComponent(error)}
          <a href="/profile/wishlist" className="btn-close"></a>
        </div>
      )}

      {/* Summary Cards */}
      <div className="row mb-4">
        <div className="col-md-4">
          <div className="card text-center border-primary">
            <div className="card-body">
              <div className="display-4 text-primary">
                <i className="bi bi-list-ul"></i>
              </div>
              <h5 className="card-title">Total Wishlists</h5>
              <p className="card-text display-6">{wishlists.length}</p>
            </div>
          </div>
        </div>
        <div className="col-md-4">
          <div className="card text-center border-success">
            <div className="card-body">
              <div className="display-4 text-success">
                <i className="bi bi-bag-heart"></i>
              </div>
              <h5 className="card-title">Total Items</h5>
              <p className="card-text display-6">{totalItems}</p>
            </div>
          </div>
        </div>
        <div className="col-md-4">
          <div className="card text-center border-warning">
            <div className="card-body">
              <div className="display-4 text-warning">
                <i className="bi bi-currency-dollar"></i>
              </div>
              <h5 className="card-title">Total Value</h5>
              <p className="card-text display-6">{formatCurrency(totalValue)}</p>
            </div>
          </div>
        </div>
      </div>

      {/* Wishlists */}
      {wishlists.length === 0 ? (
        <div className="text-center py-5">
          <div className="display-1 text-muted mb-3">
            <i className="bi bi-heart"></i>
          </div>
          <h3>No Wishlists Yet</h3>
          <p className="text-muted mb-4">Create your first wishlist to start saving your favorite products.</p>
          <a href="/profile/wishlist?modal=create" className="btn btn-primary btn-lg">
            <i className="bi bi-plus-lg me-2"></i>
            Create Your First Wishlist
          </a>
        </div>
      ) : (
        <div className="row">
          {wishlists.map((wishlist) => (
            <div key={wishlist.id} className="col-lg-6 col-xl-4 mb-4">
              <div className={`card h-100 ${wishlist.isDefault ? 'border-primary' : ''}`}>
                <div className="card-header d-flex justify-content-between align-items-start align-items-sm-center flex-wrap gap-2">
                  <div className="d-flex align-items-center">
                    <h5 className="card-title mb-0">
                      {wishlist.name}
                      {wishlist.isDefault && (
                        <span className="badge bg-primary ms-2">Default</span>
                      )}
                      {wishlist.isPublic && (
                        <span className="badge bg-success ms-2">Public</span>
                      )}
                    </h5>
                  </div>
                  <div className="dropdown">
                    <button className="btn btn-sm btn-outline-secondary dropdown-toggle" type="button" data-bs-toggle="dropdown">
                      <i className="bi bi-three-dots"></i>
                    </button>
                    <ul className="dropdown-menu">
                      <li>
                        <a className="dropdown-item" href={`/profile/wishlist?modal=edit&id=${wishlist.id}`}>
                          <i className="bi bi-pencil me-2"></i>Edit
                        </a>
                      </li>
                      <li>
                        <a className="dropdown-item" href={`/profile/wishlist?modal=duplicate&id=${wishlist.id}`}>
                          <i className="bi bi-files me-2"></i>Duplicate
                        </a>
                      </li>
                      <li>
                        <a className="dropdown-item" href={`/profile/wishlist?modal=share&id=${wishlist.id}`}>
                          <i className="bi bi-share me-2"></i>Share Settings
                        </a>
                      </li>
                      {wishlist.itemCount > 0 && (
                        <li>
                          <a className="dropdown-item text-warning" href={`/profile/wishlist?modal=clear&id=${wishlist.id}`}>
                            <i className="bi bi-trash3 me-2"></i>Clear All Items
                          </a>
                        </li>
                      )}
                      {!wishlist.isDefault && (
                        <li>
                          <a className="dropdown-item text-danger" href={`/profile/wishlist?modal=delete&id=${wishlist.id}`}>
                            <i className="bi bi-trash me-2"></i>Delete Wishlist
                          </a>
                        </li>
                      )}
                    </ul>
                  </div>
                </div>
                
                <div className="card-body">
                  <div className="d-flex justify-content-between text-muted mb-3">
                    <small>
                      <i className="bi bi-bag me-1"></i>
                      {wishlist.itemCount} items
                    </small>
                    <small>
                      <i className="bi bi-currency-dollar me-1"></i>
                      {formatCurrency(wishlist.totalValue)}
                    </small>
                  </div>

                  {wishlist.items.length === 0 ? (
                    <div className="text-center py-3">
                      <i className="bi bi-heart display-4 text-muted"></i>
                      <p className="text-muted mt-2">No items in this wishlist</p>
                    </div>
                  ) : (
                    <div className="wishlist-items">
                      {wishlist.items.slice(0, 3).map((item) => (
                        <div key={item.id} className="d-flex align-items-center mb-2 p-2 bg-light rounded">
                          <img 
                            src={item.product.imageUrl || '/placeholder-product.jpg'} 
                            alt={item.product.name}
                            className="rounded me-2"
                            style={{ width: '40px', height: '40px', objectFit: 'contain' }}
                          />
                          <div className="flex-grow-1">
                            <div className="fw-medium small">{item.product.name}</div>
                            <div className="text-muted small">
                              {formatCurrency(item.product.price)}
                              <span className={`badge ms-2 ${getPriorityBadge(item.priority)}`}>
                                {WishlistPriorityLabels[item.priority as WishlistPriority]}
                              </span>
                            </div>
                          </div>
                          <form action={removeFromWishlist} style={{ display: 'inline' }}>
                            <input type="hidden" name="wishlistId" value={wishlist.id} />
                            <input type="hidden" name="productId" value={item.productId} />
                            <button type="submit" className="btn btn-sm btn-outline-danger" title="Remove">
                              <i className="bi bi-x"></i>
                            </button>
                          </form>
                        </div>
                      ))}
                      
                      {wishlist.items.length > 3 && (
                        <div className="text-center mt-2">
                          <small className="text-muted">+{wishlist.items.length - 3} more items</small>
                        </div>
                      )}
                    </div>
                  )}
                </div>

                <div className="card-footer text-muted d-flex justify-content-between align-items-start align-items-sm-center flex-wrap gap-2">
                  <small>
                    Updated {formatDate(wishlist.updatedDate)}
                  </small>
                  {wishlist.itemCount > 0 && (
                    <a href={`/profile/wishlist?modal=bulk&id=${wishlist.id}`} className="btn btn-sm btn-outline-primary">
                      Manage Items
                    </a>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Modal Overlays */}
      {showModal === 'create' && <CreateWishlistOverlay />}
      {showModal === 'edit' && selectedWishlistId && (
        <EditWishlistOverlay wishlist={wishlists.find(w => w.id === parseInt(selectedWishlistId))} />
      )}
      {showModal === 'delete' && selectedWishlistId && (
        <DeleteWishlistOverlay wishlist={wishlists.find(w => w.id === parseInt(selectedWishlistId))} />
      )}
      {showModal === 'duplicate' && selectedWishlistId && (
        <DuplicateWishlistOverlay wishlist={wishlists.find(w => w.id === parseInt(selectedWishlistId))} />
      )}
      {showModal === 'share' && selectedWishlistId && (
        <ShareWishlistOverlay wishlist={wishlists.find(w => w.id === parseInt(selectedWishlistId))} />
      )}
      {showModal === 'clear' && selectedWishlistId && (
        <ClearWishlistOverlay wishlist={wishlists.find(w => w.id === parseInt(selectedWishlistId))} />
      )}
      {showModal === 'bulk' && selectedWishlistId && (
        <BulkManageOverlay wishlist={wishlists.find(w => w.id === parseInt(selectedWishlistId))} />
      )}
    </>
  );
}

// Modal Components
function CreateWishlistOverlay() {
  return (
    <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">
              <i className="bi bi-plus-lg me-2"></i>
              Create New Wishlist
            </h5>
            <a href="/profile/wishlist" className="btn-close"></a>
          </div>
          <form action={createWishlist}>
            <div className="modal-body">
              <div className="mb-3">
                <label htmlFor="name" className="form-label">Wishlist Name *</label>
                <input
                  type="text"
                  className="form-control"
                  id="name"
                  name="name"
                  placeholder="e.g., Birthday Gifts, Holiday Shopping"
                  required
                  maxLength={100}
                />
              </div>
              
              <div className="form-check">
                <input
                  className="form-check-input"
                  type="checkbox"
                  id="isPublic"
                  name="isPublic"
                />
                <label className="form-check-label" htmlFor="isPublic">
                  Make this wishlist public
                  <small className="d-block text-muted">
                    Others will be able to view your wishlist
                  </small>
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <a href="/profile/wishlist" className="btn btn-secondary">Cancel</a>
              <button type="submit" className="btn btn-primary">
                <i className="bi bi-plus-lg me-1"></i>
                Create Wishlist
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

function EditWishlistOverlay({ wishlist }: { wishlist?: WishlistDto }) {
  if (!wishlist) return null;

  return (
    <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">
              <i className="bi bi-pencil me-2"></i>
              Edit Wishlist
            </h5>
            <a href="/profile/wishlist" className="btn-close"></a>
          </div>
          <form action={updateWishlist}>
            <input type="hidden" name="id" value={wishlist.id} />
            <div className="modal-body">
              <div className="mb-3">
                <label htmlFor="name" className="form-label">Wishlist Name *</label>
                <input
                  type="text"
                  className="form-control"
                  id="name"
                  name="name"
                  defaultValue={wishlist.name}
                  required
                  maxLength={100}
                />
              </div>
              
              <div className="form-check">
                <input
                  className="form-check-input"
                  type="checkbox"
                  id="isPublic"
                  name="isPublic"
                  defaultChecked={wishlist.isPublic}
                />
                <label className="form-check-label" htmlFor="isPublic">
                  Make this wishlist public
                  <small className="d-block text-muted">
                    Others will be able to view your wishlist
                  </small>
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <a href="/profile/wishlist" className="btn btn-secondary">Cancel</a>
              <button type="submit" className="btn btn-primary">
                <i className="bi bi-check2 me-1"></i>
                Update Wishlist
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

function DeleteWishlistOverlay({ wishlist }: { wishlist?: WishlistDto }) {
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
            <a href="/profile/wishlist" className="btn-close"></a>
          </div>
          <div className="modal-body">
            <div className="alert alert-danger">
              <i className="bi bi-exclamation-triangle-fill me-2"></i>
              <strong>Warning!</strong> This action cannot be undone.
            </div>
            
            <p>Are you sure you want to delete the wishlist <strong>&quot;{wishlist.name}&quot;</strong>?</p>
            
            {wishlist.itemCount > 0 && (
              <div className="alert alert-warning">
                <i className="bi bi-info-circle me-2"></i>
                This wishlist contains <strong>{wishlist.itemCount} items</strong> that will be permanently removed.
              </div>
            )}
          </div>
          <div className="modal-footer">
            <a href="/profile/wishlist" className="btn btn-secondary">Cancel</a>
                            <form action={deleteWishlist} style={{ display: 'inline' }}>
              <input type="hidden" name="id" value={wishlist.id} />
              <button type="submit" className="btn btn-danger">
                <i className="bi bi-trash me-1"></i>
                Delete Wishlist
              </button>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
}

function DuplicateWishlistOverlay({ wishlist }: { wishlist?: WishlistDto }) {
  if (!wishlist) return null;

  return (
    <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">
              <i className="bi bi-files me-2"></i>
              Duplicate Wishlist
            </h5>
            <a href="/profile/wishlist" className="btn-close"></a>
          </div>
          <form action={duplicateWishlist}>
            <input type="hidden" name="wishlistId" value={wishlist.id} />
            <div className="modal-body">
              <p>Create a copy of <strong>&quot;{wishlist.name}&quot;</strong> with all its items.</p>
              
              <div className="mb-3">
                <label htmlFor="newName" className="form-label">New Wishlist Name *</label>
                <input
                  type="text"
                  className="form-control"
                  id="newName"
                  name="newName"
                  defaultValue={`${wishlist.name} (Copy)`}
                  required
                  maxLength={100}
                />
              </div>
              
              <div className="alert alert-info">
                <i className="bi bi-info-circle me-2"></i>
                The new wishlist will contain all <strong>{wishlist.itemCount} items</strong> from the original wishlist.
              </div>
            </div>
            <div className="modal-footer">
              <a href="/profile/wishlist" className="btn btn-secondary">Cancel</a>
              <button type="submit" className="btn btn-primary">
                <i className="bi bi-files me-1"></i>
                Duplicate Wishlist
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

function ShareWishlistOverlay({ wishlist }: { wishlist?: WishlistDto }) {
  if (!wishlist) return null;

  return (
    <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">
              <i className="bi bi-share me-2"></i>
              Share Settings
            </h5>
            <a href="/profile/wishlist" className="btn-close"></a>
          </div>
          <div className="modal-body">
            <p>Manage sharing settings for <strong>&quot;{wishlist.name}&quot;</strong>.</p>
            
            <div className="d-flex justify-content-between align-items-center p-3 border rounded">
              <div>
                <strong>Public Visibility</strong>
                <div className="text-muted small">
                  {wishlist.isPublic ? 'Others can view this wishlist' : 'Only you can view this wishlist'}
                </div>
              </div>
              <div>
                <form action={shareWishlist} style={{ display: 'inline' }}>
                  <input type="hidden" name="wishlistId" value={wishlist.id} />
                  <input type="hidden" name="isPublic" value={wishlist.isPublic ? 'false' : 'true'} />
                  <button type="submit" className={`btn ${wishlist.isPublic ? 'btn-outline-danger' : 'btn-outline-success'}`}>
                    {wishlist.isPublic ? 'Make Private' : 'Make Public'}
                  </button>
                </form>
              </div>
            </div>
            
            {wishlist.isPublic && (
              <div className="alert alert-info mt-3">
                <i className="bi bi-info-circle me-2"></i>
                Your wishlist is currently public and can be viewed by others.
              </div>
            )}
          </div>
          <div className="modal-footer">
            <a href="/profile/wishlist" className="btn btn-secondary">Close</a>
          </div>
        </div>
      </div>
    </div>
  );
}

function ClearWishlistOverlay({ wishlist }: { wishlist?: WishlistDto }) {
  if (!wishlist) return null;

  return (
    <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">
              <i className="bi bi-trash3 text-warning me-2"></i>
              Clear Wishlist
            </h5>
            <a href="/profile/wishlist" className="btn-close"></a>
          </div>
          <div className="modal-body">
            <div className="alert alert-warning">
              <i className="bi bi-exclamation-triangle-fill me-2"></i>
              <strong>Warning!</strong> This action cannot be undone.
            </div>
            
            <p>Are you sure you want to remove all items from <strong>&quot;{wishlist.name}&quot;</strong>?</p>
            
            <div className="alert alert-info">
              <i className="bi bi-info-circle me-2"></i>
              This will remove all <strong>{wishlist.itemCount} items</strong> from the wishlist. The wishlist itself will remain.
            </div>
          </div>
          <div className="modal-footer">
            <a href="/profile/wishlist" className="btn btn-secondary">Cancel</a>
                            <form action={clearWishlist} style={{ display: 'inline' }}>
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

function BulkManageOverlay({ wishlist }: { wishlist?: WishlistDto }) {
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
              <i className="bi bi-list-check me-2"></i>
              Manage &quot;{wishlist.name}&quot; Items
            </h5>
            <a href="/profile/wishlist" className="btn-close"></a>
          </div>
          <div className="modal-body">
                          <form action={bulkRemoveItems}>
              <input type="hidden" name="wishlistId" value={wishlist.id} />
              
              <div className="d-flex justify-content-between align-items-center mb-3">
                <div>
                  <strong>{wishlist.itemCount} items</strong> in this wishlist
                </div>
                <div>
                  <button type="submit" className="btn btn-outline-danger">
                    <i className="bi bi-trash me-1"></i>
                    Remove Selected
                  </button>
                </div>
              </div>
              
              <div className="table-responsive" style={{ maxHeight: '400px', overflowY: 'auto' }}>
                <table className="table table-hover">
                  <thead className="table-light sticky-top">
                    <tr>
                      <th style={{ width: '50px' }}>
                        <input 
                          type="checkbox" 
                          className="form-check-input"
                          title="Select All"
                        />
                      </th>
                      <th>Product</th>
                      <th>Price</th>
                      <th>Priority</th>
                      <th>Added</th>
                      <th style={{ width: '100px' }}>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {wishlist.items.map((item) => (
                      <tr key={item.id}>
                        <td>
                          <input 
                            type="checkbox" 
                            className="form-check-input"
                            name="selectedItems"
                            value={item.productId}
                          />
                        </td>
                        <td>
                          <div className="d-flex align-items-center">
                            <img 
                              src={item.product.imageUrl || '/placeholder-product.jpg'} 
                              alt={item.product.name}
                              className="rounded me-2"
                              style={{ width: '50px', height: '50px', objectFit: 'cover' }}
                            />
                            <div>
                              <div className="fw-medium">{item.product.name}</div>
                              {item.notes && (
                                <small className="text-muted">{item.notes}</small>
                              )}
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
                          <div className="btn-group" role="group">
                            <a href={`/products/${item.productId}`} className="btn btn-sm btn-outline-primary" title="View Product">
                              <i className="bi bi-eye"></i>
                            </a>
                            <a href={`/profile/wishlist?modal=editItem&wishlistId=${wishlist.id}&productId=${item.productId}`} className="btn btn-sm btn-outline-secondary" title="Edit">
                              <i className="bi bi-pencil"></i>
                            </a>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </form>
          </div>
          <div className="modal-footer">
            <a href="/profile/wishlist" className="btn btn-secondary">Close</a>
          </div>
        </div>
      </div>
    </div>
  );
} 