import { redirect } from 'next/navigation';
import { getCurrentUser } from '../../../lib/actions/auth-actions';
import { RoleManagementService } from '../../../lib/services/roleManagement/service';
import { UserWithRoles } from '../../../types/roleManagement';
import CustomerManagement from '../../../components/admin/CustomerManagement'

interface CustomersPageProps {
  searchParams: Promise<{
    search?: string;
    status?: string;
    modal?: string;
    id?: string;
  }>;
}

export default async function CustomersPage({ searchParams }: CustomersPageProps) {
  const user = await getCurrentUser();

  var resolvedSearchParams = await searchParams;  
  if (!user) {
    redirect('/auth/login?redirectTo=/admin/customers');
  }

  const userRoles = user.roles || [];
  const hasAdminRole = userRoles.some((role:string) => 
    role === 'Admin' || role === 'SuperAdmin'
  );

  if (!hasAdminRole) {
    redirect('/?error=' + encodeURIComponent('You do not have permission to access this page'));
  }

  let users: UserWithRoles[] = [];
  try {
    const response = await RoleManagementService.getAllUsers();
    users = response || [];
  } catch (error) {
    console.error('Failed to fetch users:', error);
  }

  const customers = users.filter((userItem: UserWithRoles) => {
    const userRoles = userItem.roles || [];
    return !userRoles.some((role: string) => 
      role === 'Admin' || role === 'SuperAdmin'
    );
  });

  return (
    <div className="p-4">
      <div className="row">
        <div className="col-12">
          <CustomerManagement 
            customers={customers}
            searchQuery={resolvedSearchParams.search}
            statusFilter={resolvedSearchParams.status}
            showModal={resolvedSearchParams.modal}
            selectedCustomerId={resolvedSearchParams.id}
          />
        </div>
      </div>
    </div>
  );
}

export const metadata = {
  title: 'Customers | Admin Dashboard | Berryfy',
  description: 'Manage customer accounts and view customer information.',
}; 