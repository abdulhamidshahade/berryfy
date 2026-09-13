import { redirect } from 'next/navigation';
import { getCurrentUser } from '../../../lib/actions/auth-actions';
import { RoleManagementService } from '../../../lib/services/roleManagement/service';
import { UserWithRoles, Role } from '../../../types/roleManagement';
import UserManagement from '../../../components/admin/UserManagement';
import { getAllUsers } from '../../../lib/actions/coupon-actions';

interface UsersPageProps {
  searchParams: Promise<{
    search?: string;
    status?: string;
    modal?: string;
    id?: string;
    showAll?: string;
    success?: string;
    error?: string;
  }>;
}

export default async function UsersPage({ searchParams }: UsersPageProps) {
  const user = await getCurrentUser();
  var resolvedSearchParams = await searchParams;
  if (!user) {
    redirect('/auth/login?redirectTo=/admin/users');
  }

  const userRoles = user.roles || [];
  const hasAdminRole = userRoles.some((role: string) => 
    role === 'Admin' || role === 'SuperAdmin'
  );

  if (!hasAdminRole) {
    redirect('/?error=' + encodeURIComponent('You do not have permission to access this page'));
  }

  let users: UserWithRoles[] = [];
  let roles: Role[] = [];
  
  try {
    const [usersResponse, rolesResponse] = await Promise.all([
      RoleManagementService.getAllUsers(),
      RoleManagementService.getAllRoles()
    ]);
    users = usersResponse || [];
    roles = rolesResponse.data || [];
    
  } catch (error) {
    console.error('Failed to fetch data:', error);
  }

  return (
    <div className="p-4">
      {resolvedSearchParams.success && (
        <div className="alert alert-success alert-dismissible fade show" role="alert">
          <i className="bi bi-check-circle me-2"></i>
          {decodeURIComponent(resolvedSearchParams.success)}
          <a href="/admin/users" className="btn-close"></a>
        </div>
      )}
      
      {resolvedSearchParams.error && (
        <div className="alert alert-danger alert-dismissible fade show" role="alert">
          <i className="bi bi-exclamation-triangle me-2"></i>
          {decodeURIComponent(resolvedSearchParams.error)}
          <a href="/admin/users" className="btn-close"></a>
        </div>
      )}

      <div className="row">
        <div className="col-12">
          <UserManagement 
            users={users}
            roles={roles}
            currentUser={user}
            searchQuery={resolvedSearchParams.search}
            statusFilter={resolvedSearchParams.status}
            showModal={resolvedSearchParams.modal}
            selectedUserId={resolvedSearchParams.id}
            showAll={resolvedSearchParams.showAll}
          />
        </div>
      </div>
    </div>
  );
}

export const metadata = {
  title: 'Admin Users | Admin Dashboard | Berryfy',
  description: 'Manage super admin and admin users with elevated privileges.',
}; 