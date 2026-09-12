import { Role, UserWithRoles } from '../../types/roleManagement';
import { assignRoleToUserAction, removeRoleFromUserAction } from '../../lib/actions/roleManagement';

interface UsersTableProps {
  users: UserWithRoles[];
  roles: Role[];
  searchQuery: string;
  currentPage: number;
  totalPages: number;
  totalUsers: number;
  selectedUserId?: number;
  selectedRole?: string;
  searchParams: {
    tab?: string;
    search?: string;
    page?: string;
    expanded?: string;
    edit?: string;
    expand?: string;
    user?: string;
    error?: string;
    success?: string;
    role?: string;
  };
}

export default function UsersTable({ 
  users, 
  roles, 
  searchQuery, 
  currentPage, 
  totalPages, 
  totalUsers,
  selectedUserId,
  selectedRole,
  searchParams
}: UsersTableProps) {
  const createPageUrl = (page: number) => {
    const params = new URLSearchParams();
    params.set('tab', 'users');
    params.set('page', page.toString());
    if (searchQuery) {
      params.set('search', searchQuery);
    }
    if (selectedRole) {
      params.set('role', selectedRole);
    }
    if (selectedUserId) {
      params.set('user', selectedUserId.toString());
    }
    return `/admin/role-management?${params.toString()}#users-table`;
  };

  const createUserUrl = (userId?: number, additionalParams: Record<string, string> = {}) => {
    const params = new URLSearchParams();
    params.set('tab', 'users');
    if (searchQuery) {
      params.set('search', searchQuery);
    }
    if (currentPage > 1) {
      params.set('page', currentPage.toString());
    }
    if (selectedRole) {
      params.set('role', selectedRole);
    }
    if (userId) {
      params.set('user', userId.toString());
    }
    Object.entries(additionalParams).forEach(([key, value]) => {
      if (value) {
        params.set(key, value);
      }
    });
    return `/admin/role-management?${params.toString()}#users-table`;
  };

  const getUserStatusColor = (user: UserWithRoles) => {
    if (user.lockoutEnd && new Date(user.lockoutEnd) > new Date()) {
      return 'bg-danger text-white';
    }
    if (!user.emailConfirmed) {
      return 'bg-warning text-dark';
    }
    return 'bg-success text-white';
  };

  const getUserStatus = (user: UserWithRoles) => {
    if (user.lockoutEnd && new Date(user.lockoutEnd) > new Date()) {
      return 'Locked';
    }
    if (!user.emailConfirmed) {
      return 'Unconfirmed';
    }
    return 'Active';
  };

  const getRoleBadgeClass = (role: string) => {
    if (role === 'SuperAdmin') return 'badge bg-danger';
    if (role === 'Admin') return 'badge bg-warning text-dark';
    if (role === 'User') return 'badge bg-primary';
    return 'badge bg-secondary';
  };

  const getAvailableRolesForUser = (user: UserWithRoles) => {
    return roles.filter(role => !user.roles.includes(role.name));
  };

  const canModifyUser = (user: UserWithRoles) => {
    return !user.roles.includes('SuperAdmin');
  };

  return (
    <div className="container-fluid p-4">
      <div className="d-flex flex-column flex-sm-row justify-content-between align-items-start align-items-sm-center mb-4">
        <div className="flex-grow-1">
          <h3 className="h4 text-dark mb-1">
            <svg className="me-2" width="20" height="20" fill="none" viewBox="0 0 24 24" strokeWidth="1.5" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 718.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z" />
            </svg>
            Users Management
          </h3>
          <p className="text-muted small mb-0">
            Manage user roles and permissions. Showing {users.length} of {totalUsers} users.
            {searchQuery && ` Filtered by "${searchQuery}".`}
            {selectedRole && ` Role filter: "${selectedRole}".`}
          </p>
        </div>
        
        <div className="mt-3 mt-sm-0">
          <div className="d-flex gap-2">
            <form action="/admin/role-management#users-table" method="get">
              <input type="hidden" name="tab" value="users" />
              {searchQuery && <input type="hidden" name="search" value={searchQuery} />}
              {selectedUserId && <input type="hidden" name="user" value={selectedUserId} />}
              <div className="d-flex gap-1">
                <select
                  name="role"
                  className="form-select form-select-sm"
                  defaultValue={selectedRole || ''}
                >
                  <option value="">All Roles</option>
                  {roles.map((role) => (
                    <option key={role.id} value={role.name}>
                      {role.name}
                    </option>
                  ))}
                </select>
                <button
                  type="submit"
                  className="btn btn-outline-secondary btn-sm"
                  title="Apply role filter"
                >
                  <svg width="14" height="14" fill="currentColor" viewBox="0 0 16 16">
                    <path d="M6 10.5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 0 1h-3a.5.5 0 0 1-.5-.5zm-2-3a.5.5 0 0 1 .5-.5h7a.5.5 0 0 1 0 1h-7a.5.5 0 0 1-.5-.5zm-2-3a.5.5 0 0 1 .5-.5h11a.5.5 0 0 1 0 1h-11a.5.5 0 0 1-.5-.5z"/>
                  </svg>
                </button>
              </div>
            </form>

            <form action="/admin/role-management#users-table" method="get">
              <input type="hidden" name="tab" value="users" />
              {selectedRole && <input type="hidden" name="role" value={selectedRole} />}
              {selectedUserId && <input type="hidden" name="user" value={selectedUserId} />}
              <div className="input-group">
                <input
                  type="text"
                  name="search"
                  defaultValue={searchQuery}
                  className="form-control form-control-sm"
                  placeholder="Search users..."
                />
                <button
                  type="submit"
                  className="btn btn-outline-secondary btn-sm"
                  aria-label="Search users"
                >
                  <svg className="bi" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
                    <path d="M11.742 10.344a6.5 6.5 0 1 0-1.397 1.398h-.001c.03.04.062.078.098.115l3.85 3.85a1 1 0 0 0 1.415-1.414l-3.85-3.85a1.007 1.007 0 0 0-.115-.1zM12 6.5a5.5 5.5 0 1 1-11 0 5.5 5.5 0 0 1 11 0z"/>
                  </svg>
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>

      {(searchQuery || selectedRole) && (
        <div className="mb-3">
          <div className="d-flex align-items-center gap-2">
            <span className="small text-muted">Active filters:</span>
            {searchQuery && (
              <span className="badge bg-light text-dark">
                Search: &quot;{searchQuery}&quot;
                <a href={createUserUrl(selectedUserId, { role: selectedRole || '' })} className="text-decoration-none ms-1">×</a>
              </span>
            )}
            {selectedRole && (
              <span className="badge bg-light text-dark">
                Role: {selectedRole}
                <a href={createUserUrl(selectedUserId, { search: searchQuery })} className="text-decoration-none ms-1">×</a>
              </span>
            )}
            <a href="/admin/role-management?tab=users#users-table" className="btn btn-sm btn-outline-secondary">
              Clear all
            </a>
          </div>
        </div>
      )}

      <div id="users-table" className="card shadow-sm">
        <div className="table-responsive">
          <table className="table table-hover mb-0">
            <thead className="table-light">
              <tr>
                <th scope="col" className="fw-semibold text-uppercase small text-muted">
                  User
                </th>
                <th scope="col" className="fw-semibold text-uppercase small text-muted">
                  Status
                </th>
                <th scope="col" className="fw-semibold text-uppercase small text-muted">
                  Current Roles
                </th>
                <th scope="col" className="fw-semibold text-uppercase small text-muted">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody>
              {users.length === 0 ? (
                <tr>
                  <td colSpan={4} className="text-center py-5 text-muted">
                    <div className="d-flex flex-column align-items-center">
                      <svg className="mb-3 text-muted" width="48" height="48" fill="none" viewBox="0 0 24 24" strokeWidth="1.5" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 718.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z" />
                      </svg>
                      <h6 className="text-muted">No users found</h6>
                      <p className="small text-muted mb-0">
                        {searchQuery || selectedRole ? 'No users match your search criteria.' : 'No users available in the system.'}
                      </p>
                      {(searchQuery || selectedRole) && (
                        <a href="/admin/role-management?tab=users#users-table" className="btn btn-sm btn-outline-secondary mt-2">
                          Clear filters
                        </a>
                      )}
                    </div>
                  </td>
                </tr>
              ) : (
                users.map((user) => {
                  const availableRoles = getAvailableRolesForUser(user);
                  const isExpanded = selectedUserId === user.id;
                  const userCanBeModified = canModifyUser(user);
                  
                  return (
                    <>
                      <tr key={user.id} className={isExpanded ? 'table-primary bg-opacity-10' : ''}>
                        <td className="py-3">
                          <div className="d-flex align-items-center">
                            <div className="rounded-circle bg-primary bg-opacity-15 d-flex align-items-center justify-content-center me-3" style={{ width: '40px', height: '40px' }}>
                              <span className="fw-semibold text-primary small">
                                {user.firstName?.[0] || user.userName[0]}
                                {user.lastName?.[0] || ''}
                              </span>
                            </div>
                            <div>
                              <div className="fw-semibold text-dark">
                                {user.firstName && user.lastName 
                                  ? `${user.firstName} ${user.lastName}` 
                                  : user.userName
                                }
                              </div>
                              <div className="text-muted small">{user.email}</div>
                              <div className="text-muted small">ID: {user.id}</div>
                            </div>
                          </div>
                        </td>
                        <td className="py-3">
                          <span className={`badge ${getUserStatusColor(user)} small`}>
                            {getUserStatus(user)}
                          </span>
                          {user.accessFailedCount > 0 && (
                            <div className="text-warning small mt-1">
                              {user.accessFailedCount} failed attempts
                            </div>
                          )}
                        </td>
                        <td className="py-3">
                          <div className="d-flex flex-wrap gap-1">
                            {user.roles.length === 0 ? (
                              <span className="badge bg-light text-muted">No roles assigned</span>
                            ) : (
                              user.roles.map(role => (
                                <span key={role} className={getRoleBadgeClass(role)}>
                                  {role}
                                </span>
                              ))
                            )}
                          </div>
                        </td>
                        <td className="py-3">
                          <div className="d-flex gap-2">
                            {userCanBeModified ? (
                              <>
                                <a
                                  href={createUserUrl(isExpanded ? undefined : user.id)}
                                  className={`btn btn-sm ${isExpanded ? 'btn-outline-secondary' : 'btn-outline-primary'}`}
                                  title={isExpanded ? 'Collapse' : 'Manage roles'}
                                >
                                  <svg className="me-1" width="14" height="14" fill="currentColor" viewBox="0 0 16 16">
                                    {isExpanded ? (
                                      <path d="M1.646 4.646a.5.5 0 0 1 .708 0L8 10.293l5.646-5.647a.5.5 0 0 1 .708.708l-6 6a.5.5 0 0 1-.708 0l-6-6a.5.5 0 0 1 0-.708z"/>
                                    ) : (
                                      <path d="M8 4a.5.5 0 0 1 .5.5v3h3a.5.5 0 0 1 0 1h-3v3a.5.5 0 0 1-1 0v-3h-3a.5.5 0 0 1 0-1h3v-3A.5.5 0 0 1 8 4z"/>
                                    )}
                                  </svg>
                                  {isExpanded ? 'Collapse' : 'Manage'}
                                </a>
                              </>
                            ) : (
                              <span className="badge bg-danger small">
                                <svg className="me-1" width="12" height="12" fill="currentColor" viewBox="0 0 16 16">
                                  <path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"/>
                                  <path d="M4.646 4.646a.5.5 0 0 1 .708 0L8 7.293l2.646-2.647a.5.5 0 0 1 .708.708L8.707 8l2.647 2.646a.5.5 0 0 1-.708.708L8 8.707l-2.646 2.647a.5.5 0 0 1-.708-.708L7.293 8 4.646 5.354a.5.5 0 0 1 0-.708z"/>
                                </svg>
                                Protected
                              </span>
                            )}
                          </div>
                        </td>
                      </tr>

                      {isExpanded && userCanBeModified && (
                        <tr className="table-primary bg-opacity-5">
                          <td colSpan={4} className="py-4">
                            <div className="row g-4">
                              <div className="col-md-6">
                                <h6 className="fw-semibold text-dark mb-3">Current Roles</h6>
                                {user.roles.length === 0 ? (
                                  <p className="text-muted small">No roles assigned to this user.</p>
                                ) : (
                                  <div className="d-flex flex-column gap-2">
                                    {user.roles.map(role => (
                                      <div key={role} className="d-flex justify-content-between align-items-center p-2 border rounded">
                                        <span className={getRoleBadgeClass(role)}>{role}</span>
                                        {role !== 'SuperAdmin' && (
                                          <form action={removeRoleFromUserAction} className="d-inline">
                                            <input type="hidden" name="userId" value={user.id} />
                                            <input type="hidden" name="roleName" value={role} />
                                            <input type="hidden" name="redirectUrl" value={createUserUrl(user.id)} />
                                            <button
                                              type="submit"
                                              className="btn btn-sm btn-outline-danger"
                                              title={`Remove ${role} role`}
                                            >
                                              <svg width="12" height="12" fill="currentColor" viewBox="0 0 16 16">
                                                <path d="M4.646 4.646a.5.5 0 0 1 .708 0L8 7.293l2.646-2.647a.5.5 0 0 1 .708.708L8.707 8l2.647 2.646a.5.5 0 0 1-.708.708L8 8.707l-2.646 2.647a.5.5 0 0 1-.708-.708L7.293 8 4.646 5.354a.5.5 0 0 1 0-.708z"/>
                                              </svg>
                                            </button>
                                          </form>
                                        )}
                                      </div>
                                    ))}
                                  </div>
                                )}
                              </div>

                              <div className="col-md-6">
                                <h6 className="fw-semibold text-dark mb-3">Assign New Role</h6>
                                {availableRoles.length === 0 ? (
                                  <p className="text-muted small">All available roles have been assigned to this user.</p>
                                ) : (
                                  <form action={assignRoleToUserAction}>
                                    <input type="hidden" name="userId" value={user.id} />
                                    <input type="hidden" name="redirectUrl" value={createUserUrl(user.id)} />
                                    <div className="d-flex gap-2">
                                      <select
                                        name="roleName"
                                        className="form-select form-select-sm"
                                        required
                                      >
                                        <option value="">Select a role...</option>
                                        {availableRoles
                                          .filter(role => role.name !== 'SuperAdmin')
                                          .map((role) => (
                                            <option key={role.id} value={role.name}>
                                              {role.name}
                                              {['Admin', 'User'].includes(role.name) && ' (System Role)'}
                                            </option>
                                          ))
                                        }
                                      </select>
                                      <button
                                        type="submit"
                                        className="btn btn-sm btn-success"
                                        title="Assign role"
                                      >
                                        <svg className="me-1" width="12" height="12" fill="currentColor" viewBox="0 0 16 16">
                                          <path d="M8 4a.5.5 0 0 1 .5.5v3h3a.5.5 0 0 1 0 1h-3v3a.5.5 0 0 1-1 0v-3h-3a.5.5 0 0 1 0-1h3v-3A.5.5 0 0 1 8 4z"/>
                                        </svg>
                                        Assign
                                      </button>
                                    </div>
                                  </form>
                                )}
                              </div>
                            </div>
                          </td>
                        </tr>
                      )}
                    </>
                  );
                })
              )}
            </tbody>
          </table>
        </div>

        {totalPages > 1 && (
          <div className="card-footer">
            <div className="d-flex justify-content-between align-items-start align-items-sm-center flex-wrap gap-2">
              <div className="text-muted small">
                Page {currentPage} of {totalPages} ({totalUsers} total users)
              </div>
              <nav aria-label="Users pagination">
                <ul className="pagination pagination-sm mb-0">
                  <li className={`page-item ${currentPage <= 1 ? 'disabled' : ''}`}>
                    <a
                      className="page-link"
                      href={currentPage > 1 ? createPageUrl(currentPage - 1) : '#'}
                      aria-label="Previous"
                    >
                      <span aria-hidden="true">&laquo;</span>
                    </a>
                  </li>

                  {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                    let pageNum;
                    if (totalPages <= 5) {
                      pageNum = i + 1;
                    } else if (currentPage <= 3) {
                      pageNum = i + 1;
                    } else if (currentPage >= totalPages - 2) {
                      pageNum = totalPages - 4 + i;
                    } else {
                      pageNum = currentPage - 2 + i;
                    }

                    return (
                      <li key={pageNum} className={`page-item ${currentPage === pageNum ? 'active' : ''}`}>
                        <a
                          className="page-link"
                          href={createPageUrl(pageNum)}
                        >
                          {pageNum}
                        </a>
                      </li>
                    );
                  })}

                  <li className={`page-item ${currentPage >= totalPages ? 'disabled' : ''}`}>
                    <a
                      className="page-link"
                      href={currentPage < totalPages ? createPageUrl(currentPage + 1) : '#'}
                      aria-label="Next"
                    >
                      <span aria-hidden="true">&raquo;</span>
                    </a>
                  </li>
                </ul>
              </nav>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}