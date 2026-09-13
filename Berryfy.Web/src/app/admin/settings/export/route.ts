import { NextRequest, NextResponse } from 'next/server';
import { getCurrentUser } from '../../../../lib/actions/auth-actions';
import { getSystemSettings } from '../../../../lib/actions/settings-action'

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

    const settings = await getSystemSettings();

    const exportData = {
      metadata: {
        exportedAt: new Date().toISOString(),
        exportedBy: user.email,
        version: '1.0.0',
        application: 'Berryfy'
      },
      settings: settings
    };

    const jsonContent = JSON.stringify(exportData, null, 2);

    const response = new NextResponse(jsonContent);
    response.headers.set('Content-Type', 'application/json');
    response.headers.set('Content-Disposition', `attachment; filename="Berryfy-settings-${new Date().toISOString().split('T')[0]}.json"`);
    
    return response;

  } catch (error) {
    console.error('Error exporting settings:', error);
    return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
  }
} 