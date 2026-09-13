import { NextRequest, NextResponse } from 'next/server';
import { getCurrentUser } from '../../../../lib/actions/auth-actions';
import { RoleManagementService } from '../../../../lib/services/roleManagement/service';
import { UserWithRoles } from '../../../../types/roleManagement';

export async function GET(request: NextRequest) {
  try {
    const user = await getCurrentUser();
    
    if (!user) {
      return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
    }

    const userRoles = user.roles || [];
    const hasAdminRole = userRoles.some((role: string) => 
      role === 'Admin' || role === 'SuperAdmin'
    );

    if (!hasAdminRole) {
      return NextResponse.json({ error: 'Forbidden' }, { status: 403 });
    }

    let users: UserWithRoles[] = [];
    try {
      const response = await RoleManagementService.getAllUsers();
      users = response || [];
    } catch (error) {
      console.error('Failed to fetch users:', error);
      return NextResponse.json({ error: 'Failed to fetch users' }, { status: 500 });
    }

    const customers = users.filter((userItem: UserWithRoles) => {
      const userRoles = userItem.roles || [];
      return !userRoles.some((role: string) => 
        role === 'Admin' || role === 'SuperAdmin'
      );
    });

    const formatDate = (dateString?: string) => {
      if (!dateString) return 'Never';
      return new Date(dateString).toLocaleDateString();
    };

    const getUserStatus = (customer: UserWithRoles) => {
      if (customer.lockoutEnd && new Date(customer.lockoutEnd) > new Date()) {
        return 'Locked';
      }
      if (!customer.emailConfirmed) {
        return 'Unverified';
      }
      return 'Active';
    };

    const csvContent = [
      ['ID', 'First Name', 'Last Name', 'Email', 'Username', 'Status', 'Email Verified', 'Failed Attempts', 'Lockout End'].join(','),
      ...customers.map(customer => [
        customer.id,
        `"${customer.firstName || ''}"`,
        `"${customer.lastName || ''}"`,
        `"${customer.email}"`,
        `"${customer.userName}"`,
        getUserStatus(customer),
        customer.emailConfirmed ? 'Yes' : 'No',
        customer.accessFailedCount,
        `"${formatDate(customer.lockoutEnd)}"`
      ].join(','))
    ].join('\n');

    const response = new NextResponse(csvContent);
    response.headers.set('Content-Type', 'text/csv');
    response.headers.set('Content-Disposition', `attachment; filename="customers-${new Date().toISOString().split('T')[0]}.csv"`);
    
    return response;

  } catch (error) {
    console.error('Error exporting customers:', error);
    return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
  }
} 