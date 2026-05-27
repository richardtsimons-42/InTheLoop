import { useState } from 'react';
import { familiesApi } from '../services/api';
import '../index.css';

interface InviteMemberProps {
  familyId: number;
  onInvite: () => void;
}

export default function InviteMember({ familyId, onInvite }: InviteMemberProps) {
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  const handleInvite = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.trim()) return;

    setLoading(true);
    setError('');
    setSuccess(false);

    try {
      await familiesApi.inviteMember(familyId, email);
      setSuccess(true);
      setEmail('');
      onInvite();
      setTimeout(() => setSuccess(false), 3000);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to invite member');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ marginTop: 'var(--space-lg)', paddingTop: 'var(--space-lg)', borderTop: '1px solid var(--color-border-light)' }}>
      {success && (
        <div style={{
          padding: 'var(--space-sm) var(--space-md)',
          marginBottom: 'var(--space-md)',
          backgroundColor: 'var(--color-success-bg)',
          color: 'var(--color-success)',
          borderRadius: 'var(--radius-md)',
          fontSize: 13,
          fontWeight: 500,
        }}>
          ✅ Member invited successfully!
        </div>
      )}
      {error && (
        <div style={{
          padding: 'var(--space-sm) var(--space-md)',
          marginBottom: 'var(--space-md)',
          backgroundColor: 'var(--color-error-bg)',
          color: 'var(--color-error)',
          borderRadius: 'var(--radius-md)',
          fontSize: 13,
          fontWeight: 500,
        }}>
          {error}
        </div>
      )}
      <form onSubmit={handleInvite}>
        <div style={{ display: 'flex', gap: 'var(--space-sm)' }}>
          <input
            type="email"
            className="input"
            placeholder="Enter email address"
            value={email}
            onChange={e => setEmail(e.target.value)}
            style={{ fontSize: 13, padding: 'var(--space-sm) var(--space-md)' }}
          />
          <button
            type="submit"
            className="btn btn-primary btn-sm"
            disabled={loading || !email.trim()}
          >
            {loading ? '...' : 'Invite'}
          </button>
        </div>
      </form>
    </div>
  );
}
