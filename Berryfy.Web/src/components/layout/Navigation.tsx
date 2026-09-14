import Link from 'next/link';
import MiniCart from '../cart/MiniCart';
import UserMenu from '../auth/UserMenu';
import { getCurrentUser } from '../../lib/actions/auth-actions';
import { Suspense } from 'react';

function MiniCartFallback() {
  return (
    <Link href="/cart" className="btn btn-outline-primary">
      <i className="bi bi-cart3"></i>
      <span className="d-none d-md-inline ms-2">Cart</span>
    </Link>
  );
}

export default async function Navigation() {
  const user = await getCurrentUser();

  return (
    <nav className="navbar navbar-expand-lg navbar-dark bg-primary">
      <div className="container">
        <Link className="navbar-brand fw-bold" href="/">
          <i className="bi bi-shop me-2"></i>
          Berryfy
        </Link>

        {/* Cart + user always visible on mobile (left of hamburger) */}
        <div className="d-flex d-lg-none gap-2 align-items-center ms-auto me-2">
          <Suspense fallback={<MiniCartFallback />}>
            <MiniCart />
          </Suspense>
          {user ? (
            <UserMenu user={user} />
          ) : (
            <Link href="/auth/login" className="btn btn-outline-light btn-sm">
              <i className="bi bi-box-arrow-in-right"></i>
            </Link>
          )}
        </div>

        <button
          className="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#navbarNav"
          aria-controls="navbarNav"
          aria-expanded="false"
          aria-label="Toggle navigation"
        >
          <span className="navbar-toggler-icon"></span>
        </button>

        {/* Collapsible section: nav links + desktop cart/user */}
        <div className="collapse navbar-collapse" id="navbarNav">
          <ul className="navbar-nav me-auto">
            <li className="nav-item">
              <Link className="nav-link" href="/">
                <i className="bi bi-house me-1"></i>
                Home
              </Link>
            </li>
            <li className="nav-item">
              <Link className="nav-link" href="/products">
                <i className="bi bi-box-seam me-1"></i>
                Products
              </Link>
            </li>
            <li className="nav-item">
              <Link className="nav-link" href="/categories">
                <i className="bi bi-grid me-1"></i>
                Categories
              </Link>
            </li>
          </ul>

          {/* Cart + user visible only on desktop inside collapse */}
          <div className="d-none d-lg-flex gap-2 align-items-center">
            <Suspense fallback={<MiniCartFallback />}>
              <MiniCart />
            </Suspense>
            
            {user ? (
              <UserMenu user={user} />
            ) : (
              <Link href="/auth/login" className="btn btn-outline-light">
                <i className="bi bi-box-arrow-in-right me-1"></i>
                Sign In
              </Link>
            )}
            <a href="https://github.com/abdulhamidshahade/berryfy" target="_blank" rel="noopener noreferrer" className="text-white-50 me-2">
              <i className="bi bi-github fs-4"></i>
            </a>
          </div>
        </div>
      </div>
    </nav>
  );
} 
