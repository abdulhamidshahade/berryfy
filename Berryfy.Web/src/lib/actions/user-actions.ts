'use server'

import { revalidatePath } from 'next/cache';
import { redirect } from 'next/navigation';
import { UserService } from '../services/user/service';
import { RoleManagementService } from '../services/roleManagement/service';
import { IUserService } from '../services/user/interface';
import { CreateUserData, UpdateUserData } from '../../types/user';
import { getCurrentUser } from './auth-actions';

const userService: IUserService = new UserService();

async function checkAdminAccess() {
  const user = await getCurrentUser();
  
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
  
  return user;
}

export async function viewUserDetails(userId: number) {
  await checkAdminAccess();
  try {
    const user = await userService.getById(userId);
    return { success: true, data: user };
  } catch (error) {
    console.error('Error fetching user details:', error);
    return { success: false, error: 'Failed to fetch user details' };
  }
}


export async function promoteUserToAdmin(formData: FormData) {
  await checkAdminAccess();
  
  const userId = parseInt(formData.get('userId') as string);
  const roleName = formData.get('roleName') as string;

  if (!userId || !roleName) {
    redirect('/admin/users?error=' + encodeURIComponent('User ID and role name are required'));
  }

  try {
    const response = await RoleManagementService.assignRoleToUser({ userId, roleName });
    
    if (response.isSuccess) {
      revalidatePath('/admin/users');
      redirect('/admin/users?success=' + encodeURIComponent(`User promoted to ${roleName} successfully`));
    } else {
      redirect('/admin/users?error=' + encodeURIComponent(response.statusMessage || 'Failed to promote user'));
    }
  } catch (error) {
    console.error('Error promoting user:', error);
    redirect('/admin/users?error=' + encodeURIComponent('An error occurred while promoting the user'));
  }
}

export async function demoteAdminUser(formData: FormData) {
  const currentUser = await checkAdminAccess();
  
  const userId = parseInt(formData.get('userId') as string);
  const roleName = formData.get('roleName') as string;

  if (!userId || !roleName) {
    redirect('/admin/users?error=' + encodeURIComponent('User ID and role name are required'));
  }

  if (userId === currentUser.id) {
    redirect('/admin/users?error=' + encodeURIComponent('You cannot demote yourself'));
  }

  if (roleName === 'SuperAdmin') {
    try {
      const superAdmins = await RoleManagementService.getUsersInRole('SuperAdmin');
      if (superAdmins.isSuccess && superAdmins.data && superAdmins.data.length <= 1) {
        redirect('/admin/users?error=' + encodeURIComponent('Cannot remove the last SuperAdmin'));
      }
    } catch (error) {
      console.error('Error checking SuperAdmin count:', error);
      redirect('/admin/users?error=' + encodeURIComponent('Error validating admin removal'));
    }
  }

  try {
    const response = await RoleManagementService.removeRoleFromUser({ userId, roleName });
    
    if (response.isSuccess) {
      revalidatePath('/admin/users');
      redirect('/admin/users?success=' + encodeURIComponent(`${roleName} role removed successfully`));
    } else {
      redirect('/admin/users?error=' + encodeURIComponent(response.statusMessage || 'Failed to remove admin role'));
    }
  } catch (error) {
    console.error('Error demoting user:', error);
    redirect('/admin/users?error=' + encodeURIComponent('An error occurred while removing admin role'));
  }
}

export async function updateUserRoles(formData: FormData) {
  await checkAdminAccess();
  
  const userId = parseInt(formData.get('userId') as string);
  const selectedRoles = formData.getAll('roles') as string[];

  if (!userId) {
    redirect('/admin/users?error=' + encodeURIComponent('User ID is required'));
  }

  try {
    const currentRoles = await RoleManagementService.getUserRoles(userId);
    
    if (!currentRoles.isSuccess) {
      redirect('/admin/users?error=' + encodeURIComponent('Failed to fetch current user roles'));
    }

    const currentRoleNames = currentRoles.data || [];
    
    for (const role of currentRoleNames) {
      if (!selectedRoles.includes(role)) {
        await RoleManagementService.removeRoleFromUser({ userId, roleName: role });
      }
    }
    
    for (const role of selectedRoles) {
      if (!currentRoleNames.includes(role)) {
        await RoleManagementService.assignRoleToUser({ userId, roleName: role });
      }
    }

    revalidatePath('/admin/users');
    redirect('/admin/users?success=' + encodeURIComponent('User roles updated successfully'));
  } catch (error) {
    console.error('Error updating user roles:', error);
    redirect('/admin/users?error=' + encodeURIComponent('An error occurred while updating user roles'));
  }
}

export async function lockUserAccount(formData: FormData) {
  await checkAdminAccess();
  
  const userId = parseInt(formData.get('userId') as string);
  const lockoutEnd = formData.get('lockoutEnd') as string;

  try {
    const result = await userService.lockAccount(userId, lockoutEnd);
    if (result) {
      revalidatePath('/admin/users');
      redirect('/admin/users?success=' + encodeURIComponent('User account locked successfully'));
    }
    redirect('/admin/users?error=' + encodeURIComponent('Failed to lock user account'));
  } catch (error) {
    console.error('Error locking user account:', error);
    redirect('/admin/users?error=' + encodeURIComponent('An error occurred while locking the account'));
  }
}

export async function unlockUserAccount(formData: FormData) {
  await checkAdminAccess();
  
  const userId = parseInt(formData.get('userId') as string);

  try {
    const result = await userService.unlockAccount(userId);
    if (result) {
      revalidatePath('/admin/users');
      redirect('/admin/users?success=' + encodeURIComponent('User account unlocked successfully'));
    }
    redirect('/admin/users?error=' + encodeURIComponent('Failed to unlock user account'));
  } catch (error) {
    console.error('Error unlocking user account:', error);
    redirect('/admin/users?error=' + encodeURIComponent('An error occurred while unlocking the account'));
  }
}

export async function resetUserPassword(formData: FormData) {
  await checkAdminAccess();
  
  const userId = parseInt(formData.get('userId') as string);
  const newPassword = formData.get('newPassword') as string;

  if (!newPassword || newPassword.length < 6) {
    redirect('/admin/users?error=' + encodeURIComponent('Password must be at least 6 characters long'));
  }

  try {
    const result = await userService.resetPassword(userId, newPassword);
    if (result) {
      revalidatePath('/admin/users');
      redirect('/admin/users?success=' + encodeURIComponent('User password reset successfully'));
    }
    redirect('/admin/users?error=' + encodeURIComponent('Failed to reset user password'));
  } catch (error) {
    console.error('Error resetting user password:', error);
    redirect('/admin/users?error=' + encodeURIComponent('An error occurred while resetting the password'));
  }
}

export async function verifyUserEmail(formData: FormData) {
  await checkAdminAccess();
  
  const userId = parseInt(formData.get('userId') as string);

  try {
    const result = await userService.verifyEmail(userId);
    if (result) {
      revalidatePath('/admin/users');
      redirect('/admin/users?success=' + encodeURIComponent('User email verified successfully'));
    }
    redirect('/admin/users?error=' + encodeURIComponent('Failed to verify user email'));
  } catch (error) {
    console.error('Error verifying user email:', error);
    redirect('/admin/users?error=' + encodeURIComponent('An error occurred while verifying the email'));
  }
}

export async function updateUser(formData: FormData) {
  await checkAdminAccess();
  
  const userId = parseInt(formData.get('userId') as string);
  const userData: UpdateUserData = {
    email: formData.get('email') as string,
    userName: formData.get('userName') as string,
    firstName: formData.get('firstName') as string || undefined,
    lastName: formData.get('lastName') as string || undefined,
    emailConfirmed: formData.get('emailConfirmed') === 'on'
  };

  try {
    const result = await userService.updateUser(userId, userData);
    if (result) {
      revalidatePath('/admin/users');
      redirect('/admin/users?success=' + encodeURIComponent('User updated successfully'));
    }
    redirect('/admin/users?error=' + encodeURIComponent('Failed to update user'));
  } catch (error) {
    console.error('Error updating user:', error);
    redirect('/admin/users?error=' + encodeURIComponent('An error occurred while updating the user'));
  }
}

export async function createAdminUser(formData: FormData) {
  await checkAdminAccess();
  
  const userData: CreateUserData = {
    email: formData.get('email') as string,
    userName: formData.get('userName') as string,
    firstName: formData.get('firstName') as string || undefined,
    lastName: formData.get('lastName') as string || undefined,
    password: formData.get('password') as string,
    emailConfirmed: formData.get('emailConfirmed') === 'on',
    roles: formData.getAll('roles') as string[] || ['User']
  };

  if (!userData.password || userData.password.length < 6) {
    redirect('/admin/users?error=' + encodeURIComponent('Password must be at least 6 characters long'));
  }

  try {
    const result = await userService.createUser(userData);
    if (result) {
      revalidatePath('/admin/users');
      redirect('/admin/users?success=' + encodeURIComponent('Admin user created successfully'));
    }
    redirect('/admin/users?error=' + encodeURIComponent('Failed to create admin user'));
  } catch (error) {
    console.error('Error creating admin user:', error);
    redirect('/admin/users?error=' + encodeURIComponent('An error occurred while creating the admin user'));
  }
}

export async function deleteUser(formData: FormData) {
  const currentUser = await checkAdminAccess();
  
  const userId = parseInt(formData.get('userId') as string);

  if (!userId) {
    redirect('/admin/users?error=' + encodeURIComponent('User ID is required'));
  }

  if (userId === currentUser.id) {
    redirect('/admin/users?error=' + encodeURIComponent('You cannot delete yourself'));
  }

  try {
    const result = await userService.deleteUser(userId);
    if (result) {
      revalidatePath('/admin/users');
      redirect('/admin/users?success=' + encodeURIComponent('User deleted successfully'));
    }
    redirect('/admin/users?error=' + encodeURIComponent('Failed to delete user'));
  } catch (error) {
    console.error('Error deleting user:', error);
    redirect('/admin/users?error=' + encodeURIComponent('An error occurred while deleting the user'));
  }
} 