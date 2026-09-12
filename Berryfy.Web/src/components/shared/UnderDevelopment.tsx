import Link from 'next/link';

export default function UnderDevelopment() {
  return (
    <div className="container py-5">
      <div className="row justify-content-center">
        <div className="col-md-6 text-center">
          <div className="py-5">
            <div className="mb-4">
              <i className="bi bi-tools display-1 text-muted opacity-50"></i>
            </div>
            <h1 className="display-4 fw-bold text-warning mb-3">Under Development</h1>
            <h2 className="h3 text-dark mb-3">We&apos;re Working on Something Great</h2>
            <p className="text-muted mb-4">
              This page is currently under development. I&apos;m working hard 
              to bring you new features and improvements. Please check back soon!
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