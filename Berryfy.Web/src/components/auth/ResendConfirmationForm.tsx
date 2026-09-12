import { resendConfirmationAction } from '../../lib/actions/auth-actions';
import { redirect } from 'next/navigation';

interface ResendConfirmationFormProps {
  email?: string;
  redirectTo?: string;
}

export default function ResendConfirmationForm({ email, redirectTo }: ResendConfirmationFormProps) {
  async function handleResendConfirmation(formData: FormData) {
    'use server';
    
    const result = await resendConfirmationAction(formData);
    const submittedEmail = (formData.get('email') as string) || '';
    const submittedRedirectTo = formData.get('redirectTo') as string | null;
    
    if (result.success) {
      let url =
        '/auth/confirm-email?email=' +
        encodeURIComponent(submittedEmail) +
        '&message=' +
        encodeURIComponent('If your email exists in our system, a new 6-digit confirmation code has been sent.');

      if (submittedRedirectTo && submittedRedirectTo.startsWith('/') && !submittedRedirectTo.startsWith('//')) {
        url += '&redirectTo=' + encodeURIComponent(submittedRedirectTo);
      }

      redirect(url);
    } else {
      let url =
        '/auth/resend-confirmation?email=' +
        encodeURIComponent(submittedEmail) +
        '&error=' +
        encodeURIComponent(result.error || 'Failed to send confirmation code');

      if (submittedRedirectTo && submittedRedirectTo.startsWith('/') && !submittedRedirectTo.startsWith('//')) {
        url += '&redirectTo=' + encodeURIComponent(submittedRedirectTo);
      }

      redirect(url);
    }
  }

  return (
    <div className="card shadow">
      <div className="card-body p-4">
        <div className="text-center mb-4">
          <h2 className="card-title">
            <i className="bi bi-envelope-arrow-up me-2"></i>
            Resend Confirmation Email
          </h2>
          <p className="text-muted">
            Enter your email address and we&apos;ll send you a new 6-digit confirmation code.
          </p>
        </div>

        <form action={handleResendConfirmation}>
          <input type="hidden" name="redirectTo" value={redirectTo || ''} />

          <div className="mb-3">
            <label htmlFor="email" className="form-label">
              <i className="bi bi-envelope me-1"></i>
              Email Address
            </label>
            <input
              type="email"
              className="form-control"
              id="email"
              name="email"
              placeholder="Enter your email address"
              defaultValue={email}
              required
            />
          </div>

          <button type="submit" className="btn btn-primary w-100 mb-3">
            <i className="bi bi-send me-2"></i>
            Send confirmation code
          </button>

          <div className="text-center">
            {email && (
              <p className="mb-2">
                Already have a code?{' '}
                <a
                  href={`/auth/confirm-email?email=${encodeURIComponent(email)}${
                    redirectTo && redirectTo.startsWith('/') && !redirectTo.startsWith('//')
                      ? `&redirectTo=${encodeURIComponent(redirectTo)}`
                      : ''
                  }`}
                  className="text-decoration-none"
                >
                  Enter verification code
                </a>
              </p>
            )}
            <p className="mb-0">
              Already confirmed your email?{' '}
              <a href="/auth/login" className="text-decoration-none">
                Sign in here
              </a>
            </p>
          </div>
        </form>
      </div>
    </div>
  );
} 
