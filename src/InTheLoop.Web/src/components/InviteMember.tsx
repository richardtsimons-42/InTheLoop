import { useState } from 'react';
import { familiesApi } from '../services/api';

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
    <div style={{ marginTop: 16, padding: 16, border: '1px solid #eee', borderRadius: 8 }}>
      <h3 style={{ margin: '0 0 12px 0' }}>Invite Family Member</h3>
      {success && (
        <div style={{ padding: 8, marginBottom: 12, backgroundColor: '#d4edda', color: '#155724', borderRadius: 4 }}>
          Member invited successfully!
        </div>
      )}
      {error && (
        <div style={{ padding: 8, marginBottom: 12, backgroundColor: '#f8d7da', color: '#721c24', borderRadius: 4 }}>
          {error}
        </div>
      )}
      <form onSubmit={handleInvite}>
        <div style={{ display: 'flex', gap: 8 }}>
          <input
            type="email"
            placeholder="Enter email address"
            value={email}
            onChange={e => setEmail(e.target.value)}
            style={{
              flex: 1,
              padding: '8px 12px',
              border: '1px solid #ddd',
              borderRadius: 4,
              fontSize: 14,
            }}
          />
          <button
            type="submit"
            disabled={loading || !email.trim()}
            style={{
              padding: '8px 16px',
              backgroundColor: '#3498db',
              color: 'white',
              border: 'none',
              borderRadius: 4,
              cursor: loading || !email.trim() ? 'not-allowed' : 'pointer',
              fontWeight: 600,
              fontSize: 14,
            }}
          >
            {loading ? 'Inviting...' : 'Invite'}
          </button>
        </div>
      </form>
    </div>
  );
}
