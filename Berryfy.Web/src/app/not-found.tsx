import Link from 'next/link';

export default function NotFound() {
  return (
    <div className="container py-5">
      <div className="row justify-content-center">
        <div className="col-md-6 text-center">
          <div className="py-5">
            <div className="mb-4">
              <i className="bi bi-search display-1 text-muted opacity-50"></i>
            </div>
            <h1 className="display-4 fw-bold text-primary mb-3">404</h1>
            <h2 className="h3 text-dark mb-3">Page Not Found</h2>
            <p className="text-muted mb-4">
              Sorry, we couldn&apos;t find the page you&apos;re looking for. 
              It might have been moved, deleted, or you entered the wrong URL.
            </p>
            <div className="d-flex gap-3 justify-content-center flex-wrap">
              <Link href="/" className="btn btn-primary btn-lg">
                <i className="bi bi-house me-2"></i>
                Go Home
              </Link>
              <Link href="/products" className="btn btn-outline-primary btn-lg">
                <i className="bi bi-shop me-2"></i>
                Browse Products
              </Link>
            </div>
            
            <div className="mt-5">
              <h5 className="text-dark mb-3">Popular Pages</h5>
              <div className="d-flex gap-3 justify-content-center flex-wrap">
                <Link href="/categories" className="btn btn-outline-secondary">
                  <i className="bi bi-grid me-1"></i>
                  Categories
                </Link>
                <Link href="/cart" className="btn btn-outline-secondary">
                  <i className="bi bi-cart me-1"></i>
                  Shopping Cart
                </Link>
                <Link href="/auth/login" className="btn btn-outline-secondary">
                  <i className="bi bi-person me-1"></i>
                  Sign In
                </Link>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export const metadata = {
  title: '404 - Page Not Found | Berryfy',
  description: 'The page you are looking for could not be found.',
};