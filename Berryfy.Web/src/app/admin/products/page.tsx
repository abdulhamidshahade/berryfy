import { Suspense } from 'react';
import Link from 'next/link';
import { getPaginatedProducts } from '../../../lib/actions/product-actions';
import ProductCard from '../../../components/product/ProductCard';
import Pagination from '../../../components/shared/Pagination';
import { ProductFilterDto } from '../../../types/pagination';
import { ADMIN_CATALOG_MUTATIONS_DISABLED } from '../../../lib/config/admin-catalog';

export const dynamic = 'force-dynamic';
interface SearchParams {
  success?: string;
  error?: string;
  page?: string;
  search?: string;
  category?: string;
  sort?: string;
}

function SuccessAlert({ message }: { message: string }) {
  const messages = {
    created: 'Product created successfully!',
    updated: 'Product updated successfully!',
    deleted: 'Product deleted successfully!'
  };

  return (
    <div className="alert alert-success alert-dismissible fade show" role="alert">
      <i className="bi bi-check-circle-fill me-2"></i>
      {messages[message as keyof typeof messages] || message}
      <button type="button" className="btn-close" data-bs-dismiss="alert"></button>
    </div>
  );
}

function ErrorAlert({ message }: { message: string }) {
  return (
    <div className="alert alert-danger alert-dismissible fade show" role="alert">
      <i className="bi bi-exclamation-circle-fill me-2"></i>
      {message}
      <button type="button" className="btn-close" data-bs-dismiss="alert"></button>
    </div>
  );
}

function ProductsLoadingSkeleton() {
  return (
    <div className="row row-cols-1 row-cols-md-2 row-cols-lg-3 row-cols-xl-4 g-4">
      {Array.from({ length: 8 }, (_, i) => (
        <div key={i} className="col">
          <div className="card h-100">
            <div className="card-img-top bg-light" style={{ height: '200px' }}>
              <div className="d-flex align-items-center justify-content-center h-100">
                <div className="spinner-border text-muted" role="status">
                  <span className="visually-hidden">Loading...</span>
                </div>
              </div>
            </div>
            <div className="card-body">
              <div className="placeholder-glow">
                <span className="placeholder col-7"></span>
                <span className="placeholder col-4"></span>
                <span className="placeholder col-4"></span>
                <span className="placeholder col-6"></span>
              </div>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}

async function ProductsList({ filter }: { filter: ProductFilterDto }) {
  const paginationResult = await getPaginatedProducts(filter);

  if (paginationResult.data.length === 0) {
    return (
      <div className="text-center py-5">
        <div className="mb-4">
          <i className="bi bi-box-seam display-1 text-muted"></i>
        </div>
        <h3 className="text-muted">No products found</h3>
        <p className="text-muted mb-4">Start by creating your first product.</p>
        {ADMIN_CATALOG_MUTATIONS_DISABLED ? (
          <button type="button" disabled className="btn btn-primary" title="Creating products is disabled">
            <i className="bi bi-plus-circle me-2"></i>Create First Product
          </button>
        ) : (
          <Link href="/admin/products/create" className="btn btn-primary">
            <i className="bi bi-plus-circle me-2"></i>Create First Product
          </Link>
        )}
      </div>
    );
  }

  return (
    <>
      <div className="row row-cols-1 row-cols-md-2 row-cols-lg-3 row-cols-xl-4 g-4">
        {paginationResult.data.map((product) => (
          <ProductCard 
            key={product.id} 
            product={product} 
            showAdminActions={true}
            catalogMutationsDisabled={ADMIN_CATALOG_MUTATIONS_DISABLED}
          />
        ))}
      </div>
      
      <Pagination 
        pagination={paginationResult}
        currentPage={filter.pageNumber}
        baseUrl="/admin/products"
        searchParams={{
          ...(filter.category && { category: filter.category }),
          ...(filter.sortBy && filter.sortBy !== 'name' && { sort: filter.sortBy }),
          ...(filter.searchTerm && { search: filter.searchTerm })
        }}
      />
    </>
  );
}

function AdminSearchFilters({ 
  currentSearch, 
  currentPage 
}: { 
  currentSearch: string;
  currentPage: number;
}) {
  return (
    <div className="card shadow-sm mb-4">
      <div className="card-body">
        <form action="/admin/products" method="GET" className="d-flex">
          <input 
            type="hidden" 
            name="page" 
            value="1" 
          />
          <input
            type="text"
            name="search"
            className="form-control me-2"
            placeholder="Search products by name or description..."
            defaultValue={currentSearch}
          />
          <button type="submit" className="btn btn-primary">
            <i className="bi bi-search"></i>
          </button>
          {currentSearch && (
            <a href="/admin/products" className="btn btn-outline-secondary ms-2">
              <i className="bi bi-x-circle"></i>
            </a>
          )}
        </form>
      </div>
    </div>
  );
}

export default async function AdminProductsPage({
  searchParams,
}: {
  searchParams: Promise<SearchParams>
}) {
  const { success, error, page, search, category, sort } = await searchParams;

  const currentPage = Math.max(1, parseInt(page || '1'));
  const searchTerm = search || '';
  const categoryFilter = category || '';
  const sortBy = sort || 'name';
  
  const filter: ProductFilterDto = {
    pageNumber: currentPage,
    pageSize: 12,
    searchTerm: searchTerm || undefined,
    category: categoryFilter || undefined,
    sortBy: sortBy === 'name' ? undefined : sortBy,
    isActive: undefined,
    includeInactive: true
  };

  const paginationResult = await getPaginatedProducts(filter);

  return (
    <div className="container-fluid">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h1 className="h2 mb-1">
            <i className="bi bi-box-seam me-2"></i>Product Management
          </h1>
          <p className="text-muted mb-0">
            Manage your product inventory 
            {paginationResult.totalCount > 0 && (
              <span className="ms-2">({paginationResult.totalCount} total products)</span>
            )}
          </p>
        </div>
        {ADMIN_CATALOG_MUTATIONS_DISABLED ? (
          <button
            type="button"
            disabled
            className="btn btn-primary"
            title="Adding products is disabled"
          >
            <i className="bi bi-plus-circle me-2"></i>Add New Product
          </button>
        ) : (
          <Link href="/admin/products/create" className="btn btn-primary">
            <i className="bi bi-plus-circle me-2"></i>Add New Product
          </Link>
        )}
      </div>

      {success && <SuccessAlert message={success} />}
      {error && <ErrorAlert message={error} />}

      <AdminSearchFilters 
        currentSearch={searchTerm}
        currentPage={currentPage}
      />

      <div className="card shadow-sm">
        <div className="card-header bg-light">
          <div className="row align-items-center">
            <div className="col">
              <h5 className="card-title mb-0">
                <i className="bi bi-list-ul me-2"></i>Products Overview
                {searchTerm && (
                  <span className="text-muted ms-2">(Search: &quot;{searchTerm}&quot;)</span>
                )}
              </h5>
            </div>
            <div className="col-auto">
              <div className="btn-group" role="group">
                <button 
                  type="button" 
                  className="btn btn-outline-secondary btn-sm active"
                  title="Grid View"
                >
                  <i className="bi bi-grid-3x3-gap"></i>
                </button>
                <button 
                  type="button" 
                  className="btn btn-outline-secondary btn-sm"
                  title="List View"
                >
                  <i className="bi bi-list"></i>
                </button>
              </div>
            </div>
          </div>
        </div>
        <div className="card-body">
          <Suspense fallback={<ProductsLoadingSkeleton />}>
            <ProductsList filter={filter} />
          </Suspense>
        </div>
      </div>
    </div>
  );
} 