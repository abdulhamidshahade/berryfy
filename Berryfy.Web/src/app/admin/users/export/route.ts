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
    const hasAdminRole = userRoles.some(role => 
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

    const formatDate = (dateString?: string) => {
      if (!dateString) return 'Never';
      return new Date(dateString).toLocaleDateString();
    };

    const getUserStatus = (user: UserWithRoles) => {
      if (user.lockoutEnd && new Date(user.lockoutEnd) > new Date()) {
        return 'Locked';
      }
      if (!user.emailConfirmed) {
        return 'Unverified';
      }
      return 'Active';
    };

    const getHighestRole = (userRoles: string[]) => {
      if (userRoles.some(role => role === 'SuperAdmin')) {
        return 'Super Admin';
      }
      if (userRoles.some(role => role === 'Admin')) {
        return 'Admin';
      }
      return 'User';
    };

    const csvContent = [
      ['ID', 'First Name', 'Last Name', 'Email', 'Username', 'Highest Role', 'All Roles', 'Status', 'Email Verified', 'Failed Attempts', 'Lockout End'].join(','),
      ...users.map(user => [
        user.id,
        `"${user.firstName || ''}"`,
        `"${user.lastName || ''}"`,
        `"${user.email}"`,
        `"${user.userName}"`,
        getHighestRole(user.roles),
        `"${user.roles.join(', ')}"`,
        getUserStatus(user),
        user.emailConfirmed ? 'Yes' : 'No',
        user.accessFailedCount,
        `"${formatDate(user.lockoutEnd)}"`
      ].join(','))
    ].join('\n');

    const response = new NextResponse(csvContent);
    response.headers.set('Content-Type', 'text/csv');
    response.headers.set('Content-Disposition', `attachment; filename="admin-users-${new Date().toISOString().split('T')[0]}.csv"`);
    
    return response;

  } catch (error) {
    console.error('Error exporting users:', error);
    return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
  }
}