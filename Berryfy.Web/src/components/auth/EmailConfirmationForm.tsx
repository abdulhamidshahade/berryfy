import { confirmEmailAction } from '../../lib/actions/auth-actions';
import { redirect } from 'next/navigation';

interface EmailConfirmationFormProps {
  email: string;
  token: string;
}

export default function EmailConfirmationForm({ email, token }: EmailConfirmationFormProps) {
  async function handleConfirmation(formData: FormData) {
    'use server';
    
    const result = await confirmEmailAction(formData);
    
    if (result.success) {
      redirect('/auth/login?message=Email confirmed successfully! You can now sign in to your account.');
    } else {
      redirect(`/auth/confirm-email?email=${encodeURIComponent(email)}&token=${encodeURIComponent(token)}&error=${encodeURIComponent(result.error || 'Failed to confirm email')}`);
    }
  }

  return (
    <div className="card shadow">
      <div className="card-body p-4">
        <div className="text-center mb-4">
          <h2 className="card-title">
            <i className="bi bi-envelope-check me-2"></i>
            Confirm Your Email
          </h2>
          <p className="text-muted">
            Click the button below to confirm your email address and activate your account.
          </p>
        </div>

        <div className="alert alert-info mb-4">
          <i className="bi bi-info-circle me-2"></i>
          <strong>Email:</strong> {email}
        </div>

        <form action={handleConfirmation}>
          <input type="hidden" name="email" value={email} />
          <input type="hidden" name="token" value={token} />
          
          <button type="submit" className="btn btn-success w-100 mb-3">
            <i className="bi bi-check-circle me-2"></i>
            Confirm My Email
          </button>

          <div className="text-center">
            <p className="mb-0">
              Didn&apos;t receive the email?{' '}
              <a href={`/auth/resend-confirmation?email=${encodeURIComponent(email)}`} className="text-decoration-none">
                Resend confirmation email
              </a>
            </p>
          </div>
        </form>
      </div>
    </div>
  );
} 