import {
  updateGeneralSettings,
  updateEcommerceSettings,
  updateSecuritySettings,
  updateNotificationSettings,
  updateBackupSettings,
  clearCache,
  backupDatabase,
  runDiagnostics,
  resetToDefaults,
  exportSettings,
  importSettings
} from '../../lib/actions/settings-action'

interface SystemSettings {
  siteName: string;
  siteDescription: string;
  contactEmail: string;
  maintenanceMode: boolean;
  allowRegistrations: boolean;
  defaultCurrency: string;
  taxRate: number;
  shippingCost: number;
  freeShippingThreshold: number;
  emailNotifications: boolean;
  smsNotifications: boolean;
  orderNotifications: boolean;
  stockAlerts: boolean;
  backupFrequency: string;
  sessionTimeout: number;
  maxLoginAttempts: number;
  passwordMinLength: number;
  requireSpecialChars: boolean;
  enableTwoFactor: boolean;
  enableSocialLogin: boolean;
  enableGuestCheckout: boolean;
  enableReviews: boolean;
  enableWishlist: boolean;
  enableCompareProducts: boolean;
  defaultLanguage: string;
  timezone: string;
  dateFormat: string;
  numberFormat: string;
}

interface SettingsManagementProps {
  settings: SystemSettings;
  currentUser: any;
  showModal?: string;
  success?: string;
  error?: string;
}

export default function SettingsManagement({ 
  settings,
  currentUser,
  showModal,
  success,
  error
}: SettingsManagementProps) {

  const formatDate = (date: Date) => {
    return date.toLocaleDateString();
  };

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h1 className="h3 mb-1">
            <i className="bi bi-gear me-2"></i>
            System Settings
          </h1>
          <p className="text-muted mb-0">
            Configure and manage your Berryfy e-commerce platform settings
          </p>
        </div>
        <div className="btn-group" role="group">
          <a href="/admin/settings/export" className="btn btn-outline-primary">
            <i className="bi bi-download me-1"></i>
            Export
          </a>
          <a href="/admin/settings?modal=import" className="btn btn-outline-secondary">
            <i className="bi bi-upload me-1"></i>
            Import
          </a>
          <a href="/admin/settings?modal=reset" className="btn btn-outline-danger">
            <i className="bi bi-arrow-clockwise me-1"></i>
            Reset
          </a>
        </div>
      </div>

      {success && (
        <div className="alert alert-success alert-dismissible fade show" role="alert">
          <i className="bi bi-check-circle me-2"></i>
          {decodeURIComponent(success)}
          <a href="/admin/settings" className="btn-close"></a>
        </div>
      )}

      {error && (
        <div className="alert alert-danger alert-dismissible fade show" role="alert">
          <i className="bi bi-exclamation-triangle me-2"></i>
          {decodeURIComponent(error)}
          <a href="/admin/settings" className="btn-close"></a>
        </div>
      )}

      <div className="row">
        <div className="col-lg-8">
          <div className="card shadow-sm mb-4">
            <div className="card-header bg-primary text-white">
              <h5 className="card-title mb-0">
                <i className="bi bi-gear-fill me-2"></i>
                General Settings
              </h5>
            </div>
            <div className="card-body">
              <form action={updateGeneralSettings}>
                <div className="row">
                  <div className="col-md-6 mb-3">
                    <label htmlFor="siteName" className="form-label">Site Name *</label>
                    <input 
                      type="text" 
                      className="form-control" 
                      id="siteName" 
                      name="siteName"
                      defaultValue={settings.siteName}
                      required
                    />
                  </div>
                  <div className="col-md-6 mb-3">
                    <label htmlFor="contactEmail" className="form-label">Contact Email *</label>
                    <input 
                      type="email" 
                      className="form-control" 
                      id="contactEmail" 
                      name="contactEmail"
                      defaultValue={settings.contactEmail}
                      required
                    />
                  </div>
                </div>
                <div className="mb-3">
                  <label htmlFor="siteDescription" className="form-label">Site Description</label>
                  <textarea 
                    className="form-control" 
                    id="siteDescription" 
                    name="siteDescription"
                    rows={3}
                    defaultValue={settings.siteDescription}
                  ></textarea>
                </div>
                
                <div className="row">
                  <div className="col-md-6 mb-3">
                    <label htmlFor="defaultCurrency" className="form-label">Default Currency</label>
                    <select className="form-select" id="defaultCurrency" name="defaultCurrency" defaultValue={settings.defaultCurrency}>
                      <option value="USD">USD - US Dollar</option>
                      <option value="EUR">EUR - Euro</option>
                      <option value="GBP">GBP - British Pound</option>
                      <option value="CAD">CAD - Canadian Dollar</option>
                      <option value="AUD">AUD - Australian Dollar</option>
                      <option value="JPY">JPY - Japanese Yen</option>
                    </select>
                  </div>
                  <div className="col-md-6 mb-3">
                    <label htmlFor="taxRate" className="form-label">Tax Rate (%)</label>
                    <input 
                      type="number" 
                      className="form-control" 
                      id="taxRate" 
                      name="taxRate"
                      step="0.1"
                      min="0"
                      max="100"
                      defaultValue={settings.taxRate}
                    />
                  </div>
                </div>

                <div className="row">
                  <div className="col-md-6 mb-3">
                    <label htmlFor="defaultLanguage" className="form-label">Default Language</label>
                    <select className="form-select" id="defaultLanguage" name="defaultLanguage" defaultValue={settings.defaultLanguage}>
                      <option value="en">English</option>
                      <option value="es">Spanish</option>
                      <option value="fr">French</option>
                      <option value="de">German</option>
                      <option value="it">Italian</option>
                      <option value="pt">Portuguese</option>
                    </select>
                  </div>
                  <div className="col-md-6 mb-3">
                    <label htmlFor="timezone" className="form-label">Timezone</label>
                    <select className="form-select" id="timezone" name="timezone" defaultValue={settings.timezone}>
                      <option value="UTC">UTC</option>
                      <option value="America/New_York">Eastern Time</option>
                      <option value="America/Chicago">Central Time</option>
                      <option value="America/Denver">Mountain Time</option>
                      <option value="America/Los_Angeles">Pacific Time</option>
                      <option value="Europe/London">London</option>
                      <option value="Europe/Paris">Paris</option>
                    </select>
                  </div>
                </div>

                <div className="row">
                  <div className="col-md-6 mb-3">
                    <label htmlFor="dateFormat" className="form-label">Date Format</label>
                    <select className="form-select" id="dateFormat" name="dateFormat" defaultValue={settings.dateFormat}>
                      <option value="MM/DD/YYYY">MM/DD/YYYY</option>
                      <option value="DD/MM/YYYY">DD/MM/YYYY</option>
                      <option value="YYYY-MM-DD">YYYY-MM-DD</option>
                      <option value="DD-MM-YYYY">DD-MM-YYYY</option>
                    </select>
                  </div>
                  <div className="col-md-6 mb-3">
                    <label htmlFor="numberFormat" className="form-label">Number Format</label>
                    <select className="form-select" id="numberFormat" name="numberFormat" defaultValue={settings.numberFormat}>
                      <option value="en-US">1,234.56 (US)</option>
                      <option value="de-DE">1.234,56 (German)</option>
                      <option value="fr-FR">1 234,56 (French)</option>
                      <option value="en-IN">1,23,456.78 (Indian)</option>
                    </select>
                  </div>
                </div>

                <div className="text-end">
                  <button type="submit" className="btn btn-primary">
                    <i className="bi bi-save me-1"></i>
                    Save General Settings
                  </button>
                </div>
              </form>
            </div>
          </div>

          <div className="card shadow-sm mb-4">
            <div className="card-header bg-success text-white">
              <h5 className="card-title mb-0">
                <i className="bi bi-shop me-2"></i>
                E-commerce Settings
              </h5>
            </div>
            <div className="card-body">
              <form action={updateEcommerceSettings}>
                <div className="row">
                  <div className="col-md-6 mb-3">
                    <label htmlFor="shippingCost" className="form-label">Standard Shipping Cost</label>
                    <div className="input-group">
                      <span className="input-group-text">$</span>
                      <input 
                        type="number" 
                        className="form-control" 
                        id="shippingCost" 
                        name="shippingCost"
                        step="0.01"
                        min="0"
                        defaultValue={settings.shippingCost}
                      />
                    </div>
                  </div>
                  <div className="col-md-6 mb-3">
                    <label htmlFor="freeShippingThreshold" className="form-label">Free Shipping Threshold</label>
                    <div className="input-group">
                      <span className="input-group-text">$</span>
                      <input 
                        type="number" 
                        className="form-control" 
                        id="freeShippingThreshold" 
                        name="freeShippingThreshold"
                        step="0.01"
                        min="0"
                        defaultValue={settings.freeShippingThreshold}
                      />
                    </div>
                  </div>
                </div>

                <div className="mb-3">
                  <label className="form-label">E-commerce Features</label>
                  <div className="row">
                    <div className="col-md-6">
                      <div className="form-check form-switch mb-2">
                        <input 
                          className="form-check-input" 
                          type="checkbox" 
                          id="enableGuestCheckout"
                          name="enableGuestCheckout"
                          defaultChecked={settings.enableGuestCheckout}
                        />
                        <label className="form-check-label" htmlFor="enableGuestCheckout">
                          Enable Guest Checkout
                        </label>
                      </div>
                      <div className="form-check form-switch mb-2">
                        <input 
                          className="form-check-input" 
                          type="checkbox" 
                          id="enableReviews"
                          name="enableReviews"
                          defaultChecked={settings.enableReviews}
                        />
                        <label className="form-check-label" htmlFor="enableReviews">
                          Enable Product Reviews
                        </label>
                      </div>
                    </div>
                    <div className="col-md-6">
                      <div className="form-check form-switch mb-2">
                        <input 
                          className="form-check-input" 
                          type="checkbox" 
                          id="enableWishlist"
                          name="enableWishlist"
                          defaultChecked={settings.enableWishlist}
                        />
                        <label className="form-check-label" htmlFor="enableWishlist">
                          Enable Wishlist
                        </label>
                      </div>
                      <div className="form-check form-switch mb-2">
                        <input 
                          className="form-check-input" 
                          type="checkbox" 
                          id="enableCompareProducts"
                          name="enableCompareProducts"
                          defaultChecked={settings.enableCompareProducts}
                        />
                        <label className="form-check-label" htmlFor="enableCompareProducts">
                          Enable Product Compare
                        </label>
                      </div>
                    </div>
                  </div>
                </div>

                <div className="text-end">
                  <button type="submit" className="btn btn-success">
                    <i className="bi bi-save me-1"></i>
                    Save E-commerce Settings
                  </button>
                </div>
              </form>
            </div>
          </div>

          <div className="card shadow-sm mb-4">
            <div className="card-header bg-warning text-dark">
              <h5 className="card-title mb-0">
                <i className="bi bi-shield-lock me-2"></i>
                Security & Access Settings
              </h5>
            </div>
            <div className="card-body">
              <form action={updateSecuritySettings}>
                <div className="row">
                  <div className="col-md-6">
                    <div className="form-check form-switch mb-3">
                      <input 
                        className="form-check-input" 
                        type="checkbox" 
                        id="maintenanceMode"
                        name="maintenanceMode"
                        defaultChecked={settings.maintenanceMode}
                      />
                      <label className="form-check-label" htmlFor="maintenanceMode">
                        <strong>Maintenance Mode</strong>
                        <small className="d-block text-muted">
                          When enabled, the site will display a maintenance message to visitors
                        </small>
                      </label>
                    </div>
                    
                    <div className="form-check form-switch mb-3">
                      <input 
                        className="form-check-input" 
                        type="checkbox" 
                        id="allowRegistrations"
                        name="allowRegistrations"
                        defaultChecked={settings.allowRegistrations}
                      />
                      <label className="form-check-label" htmlFor="allowRegistrations">
                        <strong>Allow New Registrations</strong>
                        <small className="d-block text-muted">
                          Allow new users to register accounts on the website
                        </small>
                      </label>
                    </div>

                    <div className="form-check form-switch mb-3">
                      <input 
                        className="form-check-input" 
                        type="checkbox" 
                        id="enableTwoFactor"
                        name="enableTwoFactor"
                        defaultChecked={settings.enableTwoFactor}
                      />
                      <label className="form-check-label" htmlFor="enableTwoFactor">
                        <strong>Enable Two-Factor Authentication</strong>
                        <small className="d-block text-muted">
                          Require 2FA for admin users
                        </small>
                      </label>
                    </div>

                    <div className="form-check form-switch mb-3">
                      <input 
                        className="form-check-input" 
                        type="checkbox" 
                        id="enableSocialLogin"
                        name="enableSocialLogin"
                        defaultChecked={settings.enableSocialLogin}
                      />
                      <label className="form-check-label" htmlFor="enableSocialLogin">
                        <strong>Enable Social Login</strong>
                        <small className="d-block text-muted">
                          Allow login with Google, Facebook, etc.
                        </small>
                      </label>
                    </div>
                  </div>

                  <div className="col-md-6">
                    <div className="mb-3">
                      <label htmlFor="sessionTimeout" className="form-label">Session Timeout (minutes)</label>
                      <input 
                        type="number" 
                        className="form-control" 
                        id="sessionTimeout" 
                        name="sessionTimeout"
                        min="5"
                        max="480"
                        defaultValue={settings.sessionTimeout}
                      />
                      <div className="form-text">Between 5 and 480 minutes</div>
                    </div>

                    <div className="mb-3">
                      <label htmlFor="maxLoginAttempts" className="form-label">Max Login Attempts</label>
                      <input 
                        type="number" 
                        className="form-control" 
                        id="maxLoginAttempts" 
                        name="maxLoginAttempts"
                        min="3"
                        max="10"
                        defaultValue={settings.maxLoginAttempts}
                      />
                      <div className="form-text">Between 3 and 10 attempts</div>
                    </div>

                    <div className="mb-3">
                      <label htmlFor="passwordMinLength" className="form-label">Password Minimum Length</label>
                      <input 
                        type="number" 
                        className="form-control" 
                        id="passwordMinLength" 
                        name="passwordMinLength"
                        min="6"
                        max="20"
                        defaultValue={settings.passwordMinLength}
                      />
                      <div className="form-text">Between 6 and 20 characters</div>
                    </div>

                    <div className="form-check form-switch mb-3">
                      <input 
                        className="form-check-input" 
                        type="checkbox" 
                        id="requireSpecialChars"
                        name="requireSpecialChars"
                        defaultChecked={settings.requireSpecialChars}
                      />
                      <label className="form-check-label" htmlFor="requireSpecialChars">
                        <strong>Require Special Characters in Passwords</strong>
                      </label>
                    </div>
                  </div>
                </div>

                <div className="text-end">
                  <button type="submit" className="btn btn-warning">
                    <i className="bi bi-save me-1"></i>
                    Save Security Settings
                  </button>
                </div>
              </form>
            </div>
          </div>

          <div className="card shadow-sm mb-4">
            <div className="card-header bg-info text-white">
              <h5 className="card-title mb-0">
                <i className="bi bi-bell me-2"></i>
                Notification Settings
              </h5>
            </div>
            <div className="card-body">
              <form action={updateNotificationSettings}>
                <div className="row">
                  <div className="col-md-6">
                    <div className="form-check form-switch mb-3">
                      <input 
                        className="form-check-input" 
                        type="checkbox" 
                        id="emailNotifications"
                        name="emailNotifications"
                        defaultChecked={settings.emailNotifications}
                      />
                      <label className="form-check-label" htmlFor="emailNotifications">
                        <strong>Email Notifications</strong>
                        <small className="d-block text-muted">
                          Send general notifications via email
                        </small>
                      </label>
                    </div>

                    <div className="form-check form-switch mb-3">
                      <input 
                        className="form-check-input" 
                        type="checkbox" 
                        id="orderNotifications"
                        name="orderNotifications"
                        defaultChecked={settings.orderNotifications}
                      />
                      <label className="form-check-label" htmlFor="orderNotifications">
                        <strong>Order Notifications</strong>
                        <small className="d-block text-muted">
                          Notify about new orders and status updates
                        </small>
                      </label>
                    </div>
                  </div>

                  <div className="col-md-6">
                    <div className="form-check form-switch mb-3">
                      <input 
                        className="form-check-input" 
                        type="checkbox" 
                        id="smsNotifications"
                        name="smsNotifications"
                        defaultChecked={settings.smsNotifications}
                      />
                      <label className="form-check-label" htmlFor="smsNotifications">
                        <strong>SMS Notifications</strong>
                        <small className="d-block text-muted">
                          Send important notifications via SMS
                        </small>
                      </label>
                    </div>

                    <div className="form-check form-switch mb-3">
                      <input 
                        className="form-check-input" 
                        type="checkbox" 
                        id="stockAlerts"
                        name="stockAlerts"
                        defaultChecked={settings.stockAlerts}
                      />
                      <label className="form-check-label" htmlFor="stockAlerts">
                        <strong>Stock Alerts</strong>
                        <small className="d-block text-muted">
                          Notify when product stock is low
                        </small>
                      </label>
                    </div>
                  </div>
                </div>

                <div className="text-end">
                  <button type="submit" className="btn btn-info">
                    <i className="bi bi-save me-1"></i>
                    Save Notification Settings
                  </button>
                </div>
              </form>
            </div>
          </div>

          <div className="card shadow-sm">
            <div className="card-header bg-secondary text-white">
              <h5 className="card-title mb-0">
                <i className="bi bi-hdd me-2"></i>
                Backup Settings
              </h5>
            </div>
            <div className="card-body">
              <form action={updateBackupSettings}>
                <div className="mb-3">
                  <label htmlFor="backupFrequency" className="form-label">Automatic Backup Frequency</label>
                  <select className="form-select" id="backupFrequency" name="backupFrequency" defaultValue={settings.backupFrequency}>
                    <option value="manual">Manual Only</option>
                    <option value="daily">Daily</option>
                    <option value="weekly">Weekly</option>
                    <option value="monthly">Monthly</option>
                  </select>
                  <div className="form-text">
                    Automatic backups are performed at 2:00 AM server time
                  </div>
                </div>

                <div className="text-end">
                  <button type="submit" className="btn btn-secondary">
                    <i className="bi bi-save me-1"></i>
                    Save Backup Settings
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>

        <div className="col-lg-4">
          <div className="card shadow-sm mb-4">
            <div className="card-header bg-dark text-white">
              <h5 className="card-title mb-0">
                <i className="bi bi-info-circle me-2"></i>
                System Information
              </h5>
            </div>
            <div className="card-body">
              <div className="mb-3">
                <small className="text-muted">Version</small>
                <div className="fw-bold">Berryfy v1.0.0</div>
              </div>
              <div className="mb-3">
                <small className="text-muted">Last Updated</small>
                <div className="fw-bold">{formatDate(new Date())}</div>
              </div>
              <div className="mb-3">
                <small className="text-muted">Environment</small>
                <div className="fw-bold">
                  <span className="badge bg-success">Production</span>
                </div>
              </div>
              <div className="mb-3">
                <small className="text-muted">Database Status</small>
                <div className="fw-bold text-success">
                  <i className="bi bi-check-circle me-1"></i>
                  Connected
                </div>
              </div>
              <div className="mb-3">
                <small className="text-muted">Cache Status</small>
                <div className="fw-bold text-success">
                  <i className="bi bi-check-circle me-1"></i>
                  Active
                </div>
              </div>
              <div className="mb-3">
                <small className="text-muted">Current User</small>
                <div className="fw-bold">{currentUser.email}</div>
              </div>
            </div>
          </div>

          <div className="card shadow-sm">
            <div className="card-header bg-danger text-white">
              <h5 className="card-title mb-0">
                <i className="bi bi-tools me-2"></i>
                System Tools
              </h5>
            </div>
            <div className="card-body">
              <div className="d-grid gap-2">
                <form action={clearCache}>
                  <button type="submit" className="btn btn-outline-primary w-100">
                    <i className="bi bi-arrow-clockwise me-2"></i>
                    Clear Cache
                  </button>
                </form>

                <form action={backupDatabase}>
                  <button type="submit" className="btn btn-outline-warning w-100">
                    <i className="bi bi-download me-2"></i>
                    Backup Database
                  </button>
                </form>

                <a href="/admin/settings?modal=logs" className="btn btn-outline-info">
                  <i className="bi bi-file-text me-2"></i>
                  View System Logs
                </a>

                <form action={runDiagnostics}>
                  <button type="submit" className="btn btn-outline-danger w-100">
                    <i className="bi bi-exclamation-triangle me-2"></i>
                    System Diagnostics
                  </button>
                </form>
              </div>

              <hr />

              <div className="text-center">
                <small className="text-muted">Last backup:</small>
                <div className="fw-bold">Not available</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {showModal === 'reset' && <ResetSettingsOverlay />}
      {showModal === 'import' && <ImportSettingsOverlay />}
      {showModal === 'logs' && <SystemLogsOverlay />}
    </>
  );
}

function ResetSettingsOverlay() {
  return (
    <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">
              <i className="bi bi-exclamation-triangle text-danger me-2"></i>
              Reset Settings to Defaults
            </h5>
            <a href="/admin/settings" className="btn-close"></a>
          </div>
          <div className="modal-body">
            <div className="alert alert-danger">
              <i className="bi bi-exclamation-triangle-fill me-2"></i>
              <strong>Warning!</strong> This action will reset ALL settings to their default values.
            </div>
            
            <p>Are you sure you want to proceed? This action cannot be undone.</p>
            
            <div className="alert alert-info">
              <i className="bi bi-info-circle me-2"></i>
              <strong>What will be reset:</strong>
              <ul className="mb-0 mt-2">
                <li>General settings (site name, currency, etc.)</li>
                <li>E-commerce settings (shipping, features)</li>
                <li>Security settings (login attempts, passwords)</li>
                <li>Notification preferences</li>
                <li>Backup frequency</li>
              </ul>
            </div>
          </div>
          <div className="modal-footer">
            <a href="/admin/settings" className="btn btn-secondary">
              Cancel
            </a>
                          <form action={resetToDefaults} style={{ display: 'inline' }}>
              <button type="submit" className="btn btn-danger">
                <i className="bi bi-arrow-clockwise me-1"></i>
                Reset to Defaults
              </button>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
}

function ImportSettingsOverlay() {
  return (
    <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">
              <i className="bi bi-upload me-2"></i>
              Import Settings
            </h5>
            <a href="/admin/settings" className="btn-close"></a>
          </div>
          <form action={importSettings}>
            <div className="modal-body">
              <div className="mb-3">
                <label htmlFor="settingsFile" className="form-label">Settings File</label>
                <input
                  type="file"
                  className="form-control"
                  id="settingsFile"
                  name="settingsFile"
                  accept=".json"
                  required
                />
                <div className="form-text">
                  Upload a JSON file containing settings configuration. Only valid settings files are accepted.
                </div>
              </div>
              
              <div className="alert alert-warning">
                <i className="bi bi-exclamation-triangle me-2"></i>
                <strong>Note:</strong> Importing settings will overwrite existing configuration. Make sure to backup current settings first.
              </div>
            </div>
            <div className="modal-footer">
              <a href="/admin/settings" className="btn btn-secondary">
                Cancel
              </a>
              <button type="submit" className="btn btn-primary">
                <i className="bi bi-upload me-1"></i>
                Import Settings
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

function SystemLogsOverlay() {
  const sampleLogs = [
    { timestamp: '2024-01-15 10:30:25', level: 'INFO', message: 'User admin@Berryfy.com logged in successfully', category: 'Authentication' },
    { timestamp: '2024-01-15 10:28:15', level: 'INFO', message: 'Settings updated: General Settings', category: 'Settings' },
    { timestamp: '2024-01-15 10:25:42', level: 'INFO', message: 'Database backup completed successfully', category: 'Backup' },
    { timestamp: '2024-01-15 10:20:18', level: 'WARNING', message: 'Failed login attempt for user: test@example.com', category: 'Security' },
    { timestamp: '2024-01-15 10:15:33', level: 'INFO', message: 'Cache cleared successfully', category: 'System' },
    { timestamp: '2024-01-15 10:10:07', level: 'ERROR', message: 'Product image upload failed: Invalid file format', category: 'Upload' },
    { timestamp: '2024-01-15 10:05:22', level: 'INFO', message: 'New order received: #ORD-001234', category: 'Orders' },
    { timestamp: '2024-01-15 10:00:14', level: 'INFO', message: 'Automatic backup scheduled for 02:00 AM', category: 'Backup' }
  ];

  const getLevelBadge = (level: string) => {
    switch (level) {
      case 'ERROR': return 'bg-danger';
      case 'WARNING': return 'bg-warning text-dark';
      case 'INFO': return 'bg-info';
      default: return 'bg-secondary';
    }
  };

  return (
    <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
      <div className="modal-dialog modal-xl">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">
              <i className="bi bi-file-text me-2"></i>
              System Logs
            </h5>
            <a href="/admin/settings" className="btn-close"></a>
          </div>
          <div className="modal-body">
            <div className="table-responsive" style={{ maxHeight: '400px', overflowY: 'auto' }}>
              <table className="table table-sm">
                <thead className="table-dark sticky-top">
                  <tr>
                    <th>Timestamp</th>
                    <th>Level</th>
                    <th>Category</th>
                    <th>Message</th>
                  </tr>
                </thead>
                <tbody>
                  {sampleLogs.map((log, index) => (
                    <tr key={index}>
                      <td className="text-nowrap">{log.timestamp}</td>
                      <td>
                        <span className={`badge ${getLevelBadge(log.level)}`}>
                          {log.level}
                        </span>
                      </td>
                      <td>
                        <span className="badge bg-light text-dark">
                          {log.category}
                        </span>
                      </td>
                      <td>{log.message}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
          <div className="modal-footer">
            <a href="/admin/settings" className="btn btn-secondary">
              Close
            </a>
            <button className="btn btn-primary">
              <i className="bi bi-download me-1"></i>
              Download Full Logs
            </button>
          </div>
        </div>
      </div>
    </div>
  );
} 
