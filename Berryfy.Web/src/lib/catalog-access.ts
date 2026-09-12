import { getCurrentUser } from './actions/auth-actions';

// Server Actions can be invoked independently of the page that renders their form.
export async function requireCatalogAdmin(): Promise<void> {
  const user = await getCurrentUser();
  if (!user?.roles?.some(role => role === 'Admin' || role === 'SuperAdmin')) {
    throw new Error('Administrator access is required');
  }
}
