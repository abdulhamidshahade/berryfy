'use client';

import { useState } from 'react';
import { loginFormSubmitAction } from '../../lib/actions/auth-actions';

interface LoginFormProps {
  redirectTo?: string;
}

const SUPER_ADMIN_EMAIL = 'abdulhamidshahade@berryfy.org';
const SUPER_ADMIN_PASSWORD = 'lkjflajljlka@32lkjA32';

export default function LoginForm({ redirectTo = '/' }: LoginFormProps) {
  const [mode, setMode] = useState<'user' | 'superAdmin'>('user');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const selectUser = () => {
    setMode('user');
    setEmail('');
    setPassword('');
  };

  const selectSuperAdmin = () => {
    setMode('superAdmin');
    setEmail(SUPER_ADMIN_EMAIL);
    setPassword(SUPER_ADMIN_PASSWORD);
  };

  return (
    <div className="card shadow">
      <div className="card-body p-4">
        <div className="text-center mb-4">
          <h2 className="card-title">
            <i className="bi bi-person-circle me-2"></i>
            Sign In
          </h2>
          <p className="text-muted">Welcome back! Please sign in to your account.</p>
        </div>

        <div className="mb-4">
          <label className="form-label small text-muted mb-2 d-block text-center">
            Sign in as
          </label>
          <div className="btn-group w-100" role="group" aria-label="Account type">
            <button
              type="button"
              className={`btn btn-outline-primary ${mode === 'user' ? 'active' : ''}`}
              onClick={selectUser}
            >
              <i className="bi bi-person me-1"></i>
              User
            </button>
            <button
              type="button"
              className={`btn btn-outline-primary ${mode === 'superAdmin' ? 'active' : ''}`}
              onClick={selectSuperAdmin}
            >
              <i className="bi bi-shield-lock me-1"></i>
              Super Admin
            </button>
          </div>
          {mode === 'superAdmin' && (
            <p className="small text-muted mt-2 mb-0 text-center">
              Credentials are filled in for you—press <strong>Sign In</strong> to continue.
            </p>
          )}
        </div>

        <form action={loginFormSubmitAction}>
          <input type="hidden" name="redirectTo" value={redirectTo || '/'} />

          <div className="mb-3">
            <label htmlFor="email" className="form-label">
              <i className="bi bi-envelope me-1"></i>
              Email Address
            </label>
            <input
              type="email"
              className="form-control"
              id="email"
              name="email"
              placeholder="Enter your email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoComplete="email"
            />
          </div>

          <div className="mb-3">
            <label htmlFor="password" className="form-label">
              <i className="bi bi-lock me-1"></i>
              Password
            </label>
            <input
              type="password"
              className="form-control"
              id="password"
              name="password"
              placeholder="Enter your password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              autoComplete="current-password"
            />
          </div>

          <div className="mb-3 d-flex justify-content-between align-items-start align-items-sm-center flex-wrap gap-2">
            <div className="form-check">
              <input
                type="checkbox"
                className="form-check-input"
                id="rememberMe"
                name="rememberMe"
              />
              <label className="form-check-label" htmlFor="rememberMe">
                Remember me
              </label>
            </div>
            <div>
              <a href="/auth/forgot-password" className="text-decoration-none small">
                Forgot Password?
              </a>
            </div>
          </div>

          <button type="submit" className="btn btn-primary w-100 mb-3">
            <i className="bi bi-box-arrow-in-right me-2"></i>
            Sign In
          </button>

          <div className="text-center">
            <p className="mb-0">
              Don&apos;t have an account?{' '}
              <a
                href={`/auth/register${redirectTo && redirectTo !== '/' ? `?redirectTo=${encodeURIComponent(redirectTo)}` : ''}`}
                className="text-decoration-none"
              >
                Sign up here
              </a>
            </p>
          </div>
        </form>
      </div>
    </div>
  );
}
