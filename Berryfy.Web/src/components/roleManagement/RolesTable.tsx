import { Role, UserWithRoles } from '../../types/roleManagement';
import { updateRoleAction, deleteRoleAction } from '../../lib/actions/roleManagement';

interface RolesTableProps {
  roles: Role[];
  users: UserWithRoles[];
  searchQuery: string;
  editingRoleId?: string;
  expandedRoleId?: string;
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

export default function RolesTable({ 
  roles, 
  users, 
  searchQuery,
  editingRoleId,
  expandedRoleId,
  searchParams
}: RolesTableProps) {
  const createRoleUrl = (roleId?: string, action?: string) => {
    const params = new URLSearchParams();
    params.set('tab', 'roles');
    if (searchQuery) {
      params.set('search', searchQuery);
    }
    if (action === 'edit' && roleId) {
      params.set('edit', roleId);
    } else if (action === 'expand' && roleId) {
      params.set('expand', roleId);
    }
    return `/admin/role-management?${params.toString()}#roles-table`;
  };

  const getRoleUsageCount = (roleName: string) => {
    return users.filter(user => user.roles.includes(roleName)).length;
  };

  const getRoleBadgeClass = (role: Role) => {
    if (role.name === 'SuperAdmin') return 'badge bg-danger';
    if (role.name === 'Admin') return 'badge bg-warning text-dark';
    if (role.name === 'User') return 'badge bg-primary';
    return 'badge bg-secondary';
  };

  const isSystemRole = (roleName: string) => {
    return ['SuperAdmin', 'Admin', 'User'].includes(roleName);
  };

  const canDeleteRole = (role: Role) => {
    return !isSystemRole(role.name) && getRoleUsageCount(role.name) === 0;
  };

  const canEditRole = (role: Role) => {
    return !isSystemRole(role.name);
  };

  return (
    <div className="container-fluid p-4">
      <div className="d-flex flex-column flex-sm-row justify-content-between align-items-start align-items-sm-center mb-4">
        <div className="flex-grow-1">
          <h3 className="h4 text-dark mb-1">
            <svg className="me-2" width="20" height="20" fill="none" viewBox="0 0 24 24" strokeWidth="1.5" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.623 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" />
            </svg>
            Roles Management
          </h3>
          <p className="text-muted small mb-0">
            Manage system roles and permissions. Showing {roles.length} roles.
            {searchQuery && ` Filtered by "${searchQuery}".`}
          </p>
        </div>
        
        <div className="mt-3 mt-sm-0">
          <form action="/admin/role-management#roles-table" method="get">
            <input type="hidden" name="tab" value="roles" />
            <div className="input-group">
              <input
                type="text"
                name="search"
                defaultValue={searchQuery}
                className="form-control form-control-sm"
                placeholder="Search roles..."
              />
              <button
                type="submit"
                className="btn btn-outline-secondary btn-sm"
                aria-label="Search roles"
              >
                <svg className="bi" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
                  <path d="M11.742 10.344a6.5 6.5 0 1 0-1.397 1.398h-.001c.03.04.062.078.098.115l3.85 3.85a1 1 0 0 0 1.415-1.414l-3.85-3.85a1.007 1.007 0 0 0-.115-.1zM12 6.5a5.5 5.5 0 1 1-11 0 5.5 5.5 0 0 1 11 0z"/>
                </svg>
              </button>
            </div>
          </form>
        </div>
      </div>

      {searchQuery && (
        <div className="mb-3">
          <div className="d-flex align-items-center gap-2">
            <span className="small text-muted">Active filter:</span>
            <span className="badge bg-light text-dark">
              Search: &quot;{searchQuery}&quot;
              <a href="/admin/role-management?tab=roles#roles-table" className="text-decoration-none ms-1">×</a>
            </span>
            <a href="/admin/role-management?tab=roles#roles-table" className="btn btn-sm btn-outline-secondary">
              Clear search
            </a>
          </div>
        </div>
      )}

      <div id="roles-table" className="card shadow-sm">
        <div className="table-responsive">
          <table className="table table-hover mb-0">
            <thead className="table-light">
              <tr>
                <th scope="col" className="fw-semibold text-uppercase small text-muted">
                  Role
                </th>
                <th scope="col" className="fw-semibold text-uppercase small text-muted">
                  Type
                </th>
                <th scope="col" className="fw-semibold text-uppercase small text-muted">
                  Users
                </th>
                <th scope="col" className="fw-semibold text-uppercase small text-muted">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody>
              {roles.length === 0 ? (
                <tr>
                  <td colSpan={4} className="text-center py-5 text-muted">
                    <div className="d-flex flex-column align-items-center">
                      <svg className="mb-3 text-muted" width="48" height="48" fill="none" viewBox="0 0 24 24" strokeWidth="1.5" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 713.598 6 11.99 11.99 0 713 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.623 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" />
                      </svg>
                      <h6 className="text-muted">No roles found</h6>
                      <p className="small text-muted mb-0">
                        {searchQuery ? 'No roles match your search criteria.' : 'No roles available in the system.'}
                      </p>
                      {searchQuery && (
                        <a href="/admin/role-management?tab=roles#roles-table" className="btn btn-sm btn-outline-secondary mt-2">
                          Clear search
                        </a>
                      )}
                    </div>
                  </td>
                </tr>
              ) : (
                roles.map((role) => {
                  const usageCount = getRoleUsageCount(role.name);
                  const isExpanded = expandedRoleId === role.id;
                  const isEditing = editingRoleId === role.id;
                  const roleCanBeEdited = canEditRole(role);
                  const roleCanBeDeleted = canDeleteRole(role);
                  
                  return (
                    <>
                      <tr key={role.id} className={isExpanded || isEditing ? 'table-primary bg-opacity-10' : ''}>
                        <td className="py-3">
                          <div className="d-flex align-items-center">
                            <div className="me-3">
                              <span className={getRoleBadgeClass(role)}>
                                {role.name}
                              </span>
                            </div>
                            <div>
                              <div className="fw-semibold text-dark">{role.name}</div>
                              <div className="text-muted small">ID: {role.id}</div>
                            </div>
                          </div>
                        </td>
                        <td className="py-3">
                          <span className={`badge ${isSystemRole(role.name) ? 'bg-info' : 'bg-success'} small`}>
                            {isSystemRole(role.name) ? 'System Role' : 'Custom Role'}
                          </span>
                        </td>
                        <td className="py-3">
                          <div className="d-flex align-items-center">
                            <span className="fw-medium text-dark me-2">{usageCount}</span>
                            <span className="text-muted small">
                              {usageCount === 1 ? 'user' : 'users'}
                            </span>
                            {usageCount > 0 && (
                              <a
                                href={`/admin/role-management?tab=users&role=${encodeURIComponent(role.name)}#users-table`}
                                className="btn btn-sm btn-outline-primary ms-2"
                                title="View users with this role"
                              >
                                <svg width="12" height="12" fill="currentColor" viewBox="0 0 16 16">
                                  <path d="M1 2.828c.885-.37 2.154-.769 3.388-.893 1.33-.134 2.458.063 3.112.752v9.746c-.935-.53-2.12-.603-3.213-.493-1.18.12-2.37.461-3.287.811V2.828zm7.5-.141c.654-.689 1.782-.886 3.112-.752 1.234.124 2.503.523 3.388.893v9.923c-.918-.35-2.107-.692-3.287-.81-1.094-.111-2.278-.039-3.213.492V2.687zM8 1.783C7.015.936 5.587.81 4.287.94c-1.514.153-3.042.672-3.994 1.105A.5.5 0 0 0 0 2.5v11a.5.5 0 0 0 .707.455c.882-.4 2.303-.881 3.68-1.02 1.409-.142 2.59.087 3.223.877a.5.5 0 0 0 .78 0c.633-.79 1.814-1.019 3.222-.877 1.378.139 2.8.62 3.681 1.02A.5.5 0 0 0 16 13.5v-11a.5.5 0 0 0-.293-.455c-.952-.433-2.48-.952-3.994-1.105C10.413.809 8.985.936 8 1.783z"/>
                                </svg>
                              </a>
                            )}
                          </div>
                        </td>
                        <td className="py-3">
                          <div className="d-flex gap-2">
                            {roleCanBeEdited ? (
                              <>
                                <a
                                  href={createRoleUrl(isExpanded ? undefined : role.id, 'expand')}
                                  className={`btn btn-sm ${isExpanded ? 'btn-outline-secondary' : 'btn-outline-info'}`}
                                  title={isExpanded ? 'Collapse' : 'View details'}
                                >
                                  <svg className="me-1" width="14" height="14" fill="currentColor" viewBox="0 0 16 16">
                                    {isExpanded ? (
                                      <path d="M1.646 4.646a.5.5 0 0 1 .708 0L8 10.293l5.646-5.647a.5.5 0 0 1 .708.708l-6 6a.5.5 0 0 1-.708 0l-6-6a.5.5 0 0 1 0-.708z"/>
                                    ) : (
                                      <path d="M1.5 1.5A.5.5 0 0 1 2 1h12a.5.5 0 0 1 .5.5v2a.5.5 0 0 1-.128.334L10 8.692V13.5a.5.5 0 0 1-.342.474l-3 1A.5.5 0 0 1 6 14.5V8.692L1.628 3.834A.5.5 0 0 1 1.5 3.5v-2z"/>
                                    )}
                                  </svg>
                                  {isExpanded ? 'Collapse' : 'Details'}
                                </a>
                                <a
                                  href={createRoleUrl(isEditing ? undefined : role.id, 'edit')}
                                  className={`btn btn-sm ${isEditing ? 'btn-outline-secondary' : 'btn-outline-warning'}`}
                                  title={isEditing ? 'Cancel edit' : 'Edit role'}
                                >
                                  <svg className="me-1" width="14" height="14" fill="currentColor" viewBox="0 0 16 16">
                                    {isEditing ? (
                                      <path d="M4.646 4.646a.5.5 0 0 1 .708 0L8 7.293l2.646-2.647a.5.5 0 0 1 .708.708L8.707 8l2.647 2.646a.5.5 0 0 1-.708.708L8 8.707l-2.646 2.647a.5.5 0 0 1-.708-.708L7.293 8 4.646 5.354a.5.5 0 0 1 0-.708z"/>
                                    ) : (
                                      <path d="M12.146.146a.5.5 0 0 1 .708 0l3 3a.5.5 0 0 1 0 .708L14.5 5.207l-3-3L12.146.146zm-.106 3.394L14.5 6l-3 3L5.5 9l.5-4 3.5-3.5zm-4.5 4.5L6.5 9l1 1 1-1-1-1z"/>
                                    )}
                                  </svg>
                                  {isEditing ? 'Cancel' : 'Edit'}
                                </a>
                                {roleCanBeDeleted && (
                                  <form action={deleteRoleAction} className="d-inline">
                                    <input type="hidden" name="roleName" value={role.name} />
                                    <input type="hidden" name="redirectUrl" value={createRoleUrl()} />
                                    <button
                                      type="submit"
                                      className="btn btn-sm btn-outline-danger"
                                      title={`Delete role "${role.name}" - This action cannot be undone`}
                                    >
                                      <svg className="me-1" width="12" height="12" fill="currentColor" viewBox="0 0 16 16">
                                        <path d="M5.5 5.5A.5.5 0 0 1 6 6v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5zm2.5 0a.5.5 0 0 1 .5.5v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5zm3 .5a.5.5 0 0 0-1 0v6a.5.5 0 0 0 1 0V6z"/>
                                        <path fillRule="evenodd" d="M14.5 3a1 1 0 0 1-1 1H13v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V4h-.5a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1H6a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1h3.5a1 1 0 0 1 1 1v1zM4.118 4 4 4.059V13a1 1 0 0 0 1 1h6a1 1 0 0 0 1-1V4.059L11.882 4H4.118zM2.5 3V2h11v1h-11z"/>
                                      </svg>
                                    </button>
                                  </form>
                                )}
                              </>
                            ) : (
                              <span className="badge bg-info small">
                                <svg className="me-1" width="12" height="12" fill="currentColor" viewBox="0 0 16 16">
                                  <path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"/>
                                  <path d="m8.93 6.588-2.29.287-.082.38.45.083c.294.07.352.176.288.469l-.738 3.468c-.194.897.105 1.319.808 1.319.545 0 1.178-.252 1.465-.598l.088-.416c-.2.176-.492.246-.686.246-.275 0-.375-.193-.304-.533L8.93 6.588zM9 4.5a1 1 0 1 1-2 0 1 1 0 0 1 2 0z"/>
                                </svg>
                                System Role
                              </span>
                            )}
                          </div>
                        </td>
                      </tr>

                      {isExpanded && (
                        <tr className="table-primary bg-opacity-5">
                          <td colSpan={4} className="py-4">
                            <div className="row g-4">
                              <div className="col-md-6">
                                <h6 className="fw-semibold text-dark mb-3">Role Information</h6>
                                <dl className="row small">
                                  <dt className="col-sm-4 text-muted">Role ID:</dt>
                                  <dd className="col-sm-8 text-dark">{role.id}</dd>
                                  <dt className="col-sm-4 text-muted">Role Name:</dt>
                                  <dd className="col-sm-8 text-dark">{role.name}</dd>
                                  <dt className="col-sm-4 text-muted">Normalized Name:</dt>
                                  <dd className="col-sm-8 text-dark">{role.normalizedName}</dd>
                                  <dt className="col-sm-4 text-muted">Type:</dt>
                                  <dd className="col-sm-8">
                                    <span className={`badge ${isSystemRole(role.name) ? 'bg-info' : 'bg-success'} small`}>
                                      {isSystemRole(role.name) ? 'System Role' : 'Custom Role'}
                                    </span>
                                  </dd>
                                  <dt className="col-sm-4 text-muted">Users Assigned:</dt>
                                  <dd className="col-sm-8 text-dark">{usageCount}</dd>
                                </dl>
                              </div>
                              <div className="col-md-6">
                                <h6 className="fw-semibold text-dark mb-3">Role Usage</h6>
                                {usageCount === 0 ? (
                                  <p className="text-muted small">No users are currently assigned to this role.</p>
                                ) : (
                                  <div>
                                    <p className="small text-muted mb-2">
                                      This role is assigned to {usageCount} {usageCount === 1 ? 'user' : 'users'}.
                                    </p>
                                    <a
                                      href={`/admin/role-management?tab=users&role=${encodeURIComponent(role.name)}#users-table`}
                                      className="btn btn-sm btn-outline-primary"
                                    >
                                      <svg className="me-1" width="14" height="14" fill="currentColor" viewBox="0 0 16 16">
                                        <path d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 718.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z"/>
                                      </svg>
                                      View Users with this Role
                                    </a>
                                  </div>
                                )}
                                
                                {roleCanBeEdited && (
                                  <div className="mt-3 pt-3 border-top">
                                    <h6 className="small fw-semibold text-dark mb-2">Management Actions</h6>
                                    <div className="d-flex gap-2">
                                      <a
                                        href={createRoleUrl(role.id, 'edit')}
                                        className="btn btn-sm btn-outline-warning"
                                      >
                                        <svg className="me-1" width="12" height="12" fill="currentColor" viewBox="0 0 16 16">
                                          <path d="M12.146.146a.5.5 0 0 1 .708 0l3 3a.5.5 0 0 1 0 .708L14.5 5.207l-3-3L12.146.146zm-.106 3.394L14.5 6l-3 3L5.5 9l.5-4 3.5-3.5zm-4.5 4.5L6.5 9l1 1 1-1-1-1z"/>
                                        </svg>
                                        Edit Role
                                      </a>
                                      {roleCanBeDeleted && (
                                        <form action={deleteRoleAction} className="d-inline">
                                          <input type="hidden" name="roleName" value={role.name} />
                                          <input type="hidden" name="redirectUrl" value={createRoleUrl()} />
                                          <button
                                            type="submit"
                                            className="btn btn-sm btn-outline-danger"
                                            title={`Delete role "${role.name}" - This action cannot be undone`}
                                          >
                                            <svg className="me-1" width="12" height="12" fill="currentColor" viewBox="0 0 16 16">
                                              <path d="M5.5 5.5A.5.5 0 0 1 6 6v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5zm2.5 0a.5.5 0 0 1 .5.5v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5zm3 .5a.5.5 0 0 0-1 0v6a.5.5 0 0 0 1 0V6z"/>
                                              <path fillRule="evenodd" d="M14.5 3a1 1 0 0 1-1 1H13v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V4h-.5a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1H6a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1h3.5a1 1 0 0 1 1 1v1zM4.118 4 4 4.059V13a1 1 0 0 0 1 1h6a1 1 0 0 0 1-1V4.059L11.882 4H4.118zM2.5 3V2h11v1h-11z"/>
                                            </svg>
                                            Delete Role
                                          </button>
                                        </form>
                                      )}
                                    </div>
                                  </div>
                                )}
                              </div>
                            </div>
                          </td>
                        </tr>
                      )}

                      {isEditing && roleCanBeEdited && (
                        <tr className="table-warning bg-opacity-5">
                          <td colSpan={4} className="py-4">
                            <div className="row g-4">
                              <div className="col-md-8">
                                <h6 className="fw-semibold text-dark mb-3">Edit Role</h6>
                                                                  <form action={updateRoleAction}>
                                  <input type="hidden" name="oldRoleName" value={role.name} />
                                  <div className="row g-3">
                                    <div className="col-md-6">
                                      <label htmlFor={`newRoleName-${role.id}`} className="form-label fw-medium">
                                        Role Name
                                        <span className="text-danger">*</span>
                                      </label>
                                      <input
                                        type="text"
                                        name="newRoleName"
                                        id={`newRoleName-${role.id}`}
                                        defaultValue={role.name}
                                        required
                                        maxLength={50}
                                        pattern="^[a-zA-Z][a-zA-Z0-9\s]*$"
                                        title="Role name must start with a letter and contain only letters, numbers, and spaces"
                                        className="form-control"
                                        placeholder="Enter new role name"
                                        autoComplete="off"
                                      />
                                      <div className="form-text">
                                        Role names must start with a letter and can contain letters, numbers, and spaces (max 50 characters).
                                      </div>
                                    </div>
                                    <div className="col-md-6 d-flex align-items-end">
                                      <div className="d-flex gap-2">
                                        <button
                                          type="submit"
                                          className="btn btn-success"
                                        >
                                          <svg className="me-1" width="14" height="14" fill="currentColor" viewBox="0 0 16 16">
                                            <path d="M13.854 3.646a.5.5 0 0 1 0 .708l-7 7a.5.5 0 0 1-.708 0l-3.5-3.5a.5.5 0 1 1 .708-.708L6.5 10.293l6.646-6.647a.5.5 0 0 1 .708 0z"/>
                                          </svg>
                                          Save Changes
                                        </button>
                                        <a
                                          href={createRoleUrl()}
                                          className="btn btn-outline-secondary"
                                        >
                                          Cancel
                                        </a>
                                      </div>
                                    </div>
                                  </div>
                                </form>
                              </div>
                              <div className="col-md-4">
                                <div className="alert alert-warning small">
                                  <strong>Warning:</strong> Changing the role name will affect all users who have this role assigned.
                                </div>
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
      </div>
    </div>
  );
}