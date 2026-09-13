import { redirect } from 'next/navigation';
import { getCurrentUser } from '../../../lib/actions/auth-actions';
import { getSystemSettings } from '../../../lib/actions/settings-action'
import SettingsManagement from '../../../components/admin/SettingsManagement';

interface SettingsPageProps {
  searchParams: Promise<{
    modal?: string;
    success?: string;
    error?: string;
  }>;
}

export default async function SettingsPage({ searchParams }: SettingsPageProps) {
  const user = await getCurrentUser();
  var resolvedSearchParams = await searchParams;
  if (!user) {
    redirect('/auth/login?redirectTo=/admin/settings');
  }

  const userRoles = user.roles || [];
  const hasAdminRole = userRoles.some(role => 
    role === 'Admin' || role === 'SuperAdmin'
  );

  if (!hasAdminRole) {
    redirect('/?error=' + encodeURIComponent('You do not have permission to access this page'));
  }

  const settings = await getSystemSettings();

  return (
    <div className="p-4">
      <div className="alert alert-warning" role="status">
        Settings are a temporary preview and reset when the server restarts.
        Security, pricing, notification, and feature options do not change the store yet.
        Export a copy to retain your choices. Cache refresh and readiness checks run real operations;
        database backups require separate configuration.
      </div>
      <div className="row">
        <div className="col-12">
          <SettingsManagement 
            settings={settings}
            currentUser={user}
            showModal={resolvedSearchParams.modal}
            success={resolvedSearchParams.success}
            error={resolvedSearchParams.error}
          />
        </div>
      </div>
    </div>
  );
}

export const metadata = {
  title: 'Settings | Admin Dashboard | Berryfy',
  description: 'Manage system settings and configuration for Berryfy e-commerce platform.',
};