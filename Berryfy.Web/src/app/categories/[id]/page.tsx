import { Suspense } from 'react';
import { CategoryService } from "../../../lib/services/category/service";
import { ICategoryService } from "../../../lib/services/category/interface";
import { CategoryDto } from "../../../types/category";
import ProductCard from "../../../components/product/ProductCard";
import { notFound } from 'next/navigation';

const categoryService: ICategoryService = new CategoryService();

interface CategoryDetailsPageProps {
    params: Promise<{
        id: string;
    }>;
}

function ProductsLoadingSkeleton() {
    return (
        <div className="row row-cols-1 row-cols-md-2 row-cols-lg-3 row-cols-xl-4 g-4">
            {Array.from({ length: 8 }, (_, i) => (
                <div key={i} className="col">
                    <div className="card h-100">
                        <div className="card-img-top bg-light" style={{ height: '200px' }}>
                            <div className="d-flex align-items-center justify-content-center h-100">
                                <div className="spinner-border text-primary" role="status">
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
                                <span className="placeholder col-8"></span>
                            </div>
                        </div>
                    </div>
                </div>
            ))}
        </div>
    );
}

async function CategoryProducts({ category }: { category: CategoryDto }) {
    const activeProducts = category.products.filter(product => product.isActive);

    if (activeProducts.length === 0) {
        return (
            <div className="text-center py-5">
                <div className="mb-4">
                    <i className="bi bi-box-seam display-1 text-muted"></i>
                </div>
                <h3 className="text-muted">No products available</h3>
                <p className="text-muted mb-4">This category doesn&apos;t have any products available at the moment.</p>
                <a href="/categories" className="btn btn-primary me-2">
                    <i className="bi bi-grid-3x3-gap me-2"></i>Browse Categories
                </a>
                <a href="/products" className="btn btn-outline-primary">
                    <i className="bi bi-shop me-2"></i>All Products
                </a>
            </div>
        );
    }

    return (
        <div className="row row-cols-1 row-cols-md-2 row-cols-lg-3 row-cols-xl-4 g-4">
            {activeProducts.map((product) => (
                <ProductCard
                    key={product.id}
                    product={product}
                    showAdminActions={false}
                />
            ))}
        </div>
    );
}

function CategoryHeader({ category }: { category: CategoryDto }) {
    return (
        <div className="bg-primary text-white py-4 py-md-5 mb-4 mb-md-5">
            <div className="container">
                <div className="row align-items-center">
                    <div className="col-lg-8">
                        <div className="d-flex align-items-center flex-column flex-sm-row mb-3 text-center text-sm-start gap-3">
                            <img
                                src={category.imageUrl}
                                alt={category.name}
                                className="rounded-circle border border-white border-3 flex-shrink-0"
                                style={{ width: '72px', height: '72px', objectFit: 'cover' }}
                            />
                            <div>
                                <h1 className="h2 h1-md fw-bold mb-2">{category.name}</h1>
                                <div className="badge bg-light text-primary fs-6 px-3 py-2">
                                    {category.products.filter(p => p.isActive).length} Products Available
                                </div>
                            </div>
                        </div>
                        <p className="lead mb-0 text-center text-sm-start">{category.description}</p>
                    </div>
                    <div className="col-lg-4 text-end d-none d-lg-block">
                        <i className="bi bi-grid-3x3-gap-fill display-1 opacity-50"></i>
                    </div>
                </div>
            </div>
        </div>
    );
}

function CategoryFilters({ category }: { category: CategoryDto }) {
    const productCount = category.products.filter(p => p.isActive).length;

    return (
        <div className="card shadow-sm mb-4">
            <div className="card-body">
                <div className="row align-items-center">
                    <div className="col-md-6">
                        <h5 className="mb-0">
                            <i className="bi bi-funnel me-2"></i>
                            Showing {productCount} {productCount === 1 ? 'Product' : 'Products'}
                        </h5>
                    </div>
                    <div className="col-md-6">
                        <div className="d-flex gap-2 justify-content-md-end">
                            <select className="form-select form-select-sm" style={{ width: 'auto' }}>
                                <option value="name">Sort by Name</option>
                                <option value="price-low">Price: Low to High</option>
                                <option value="price-high">Price: High to Low</option>
                                <option value="newest">Newest First</option>
                            </select>
                            <div className="btn-group" role="group">
                                <button type="button" className="btn btn-outline-secondary btn-sm active">
                                    <i className="bi bi-grid-3x3-gap"></i>
                                </button>
                                <button type="button" className="btn btn-outline-secondary btn-sm">
                                    <i className="bi bi-list"></i>
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

function Breadcrumb({ category }: { category: CategoryDto }) {
    return (
        <nav aria-label="breadcrumb" className="mb-4">
            <ol className="breadcrumb">
                <li className="breadcrumb-item">
                    <a href="/" className="text-decoration-none">
                        <i className="bi bi-house-door me-1"></i>Home
                    </a>
                </li>
                <li className="breadcrumb-item">
                    <a href="/categories" className="text-decoration-none">
                        <i className="bi bi-grid-3x3-gap me-1"></i>Categories
                    </a>
                </li>
                <li className="breadcrumb-item active" aria-current="page">
                    <i className="bi bi-tag me-1"></i>{category.name}
                </li>
            </ol>
        </nav>
    );
}

export default async function CategoryDetailsPage({ params }: CategoryDetailsPageProps) {
    let category: CategoryDto;
    var resolvedParams = await params;
    try {
        const categoryId = parseInt(resolvedParams.id);
        if (isNaN(categoryId)) {
            notFound();
        }

        category = await categoryService.getById(categoryId);
    } catch (error) {
        console.error("Failed to load category:", error);
        notFound();
    }

    return (
        <>
            <CategoryHeader category={category} />

            <div className="container">
                <Breadcrumb category={category} />

                <CategoryFilters category={category} />

                <Suspense fallback={<ProductsLoadingSkeleton />}>
                    <CategoryProducts category={category} />
                </Suspense>

                <div className="text-center py-5">
                    <div className="row">
                        <div className="col-md-8 mx-auto">
                            <h4 className="mb-3">Discover More Categories</h4>
                            <p className="text-muted mb-4">
                                Explore our other categories to find exactly what you&apos;re looking for.
                            </p>
                            <a href="/categories" className="btn btn-outline-primary me-2">
                                <i className="bi bi-grid-3x3-gap me-2"></i>Browse All Categories
                            </a>
                            <a href="/products" className="btn btn-outline-secondary">
                                <i className="bi bi-shop me-2"></i>View All Products
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </>
    );
}