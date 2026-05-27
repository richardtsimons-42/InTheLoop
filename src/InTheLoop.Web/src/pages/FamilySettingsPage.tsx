import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { familiesApi } from '../services/api';
import { useAuth } from '../context/AuthContext';
import '../index.css';

interface Family {
  id: number;
  name: string;
  description: string | null;
  coverPhotoUrl: string | null;
  ownerName: string;
  memberCount: number;
  createdAt: string;
  userRole: string;
}

interface FamilyMember {
  id: number;
  userId: string;
  name: string;
  avatarUrl: string | null;
  role: string;
  joinedAt: string;
}

export default function FamilySettingsPage() {
  const { familyId } = useParams<{ familyId: string }>();
  const navigate = useNavigate();
  const { currentUserId } = useAuth();
  const [family, setFamily] = useState<Family | null>(null);
  const [members, setMembers] = useState<FamilyMember[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [inviteEmail, setInviteEmail] = useState('');
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [coverPhotoUrl, setCoverPhotoUrl] = useState('');
  const [message, setMessage] = useState('');
  const [messageType, setMessageType] = useState<'success' | 'error'>('success');

  const isOwnerOrAdmin = family?.userRole === 'admin';
  const isOwner = family?.userRole === 'admin' && currentUserId !== null;

  useEffect(() => { loadFamily(); }, [familyId]);

  const loadFamily = async () => {
    if (!familyId) return;
    try {
      const response = await familiesApi.getMyFamilies();
      const familyList = response.data as Family[];
      const foundFamily = familyList.find(f => f.id === parseInt(familyId!));
      if (foundFamily) {
        setFamily(foundFamily);
        setName(foundFamily.name);
        setDescription(foundFamily.description || '');
        setCoverPhotoUrl(foundFamily.coverPhotoUrl || '');
      }

      // Load members
      const membersRes = await familiesApi.getFamilyMembers(parseInt(familyId!));
      setMembers(membersRes.data || []);
    } catch (error) {
      console.error('Failed to load family:', error);
    } finally {
      setLoading(false);
    }
  };

  const showMessage = (msg: string, type: 'success' | 'error') => {
    setMessage(msg);
    setMessageType(type);
    setTimeout(() => setMessage(''), 4000);
  };

  const handleSave = async () => {
    if (!familyId) return;
    setSaving(true);
    try {
      await familiesApi.updateFamily(parseInt(familyId), name, description || undefined, coverPhotoUrl || undefined);
      showMessage('Settings saved successfully!', 'success');
      loadFamily();
    } catch (error) {
      console.error('Failed to save family settings:', error);
      showMessage('Failed to save settings', 'error');
    } finally {
      setSaving(false);
    }
  };

  const handleInvite = async () => {
    if (!familyId || !inviteEmail.trim()) return;
    try {
      await familiesApi.inviteMember(parseInt(familyId), inviteEmail);
      showMessage('Invitation sent!', 'success');
      setInviteEmail('');
      loadFamily();
    } catch (error: any) {
      const msg = error.response?.data?.message || 'Failed to invite member';
      showMessage(msg, 'error');
    }
  };

  const handlePromote = async (userId: string) => {
    if (!familyId) return;
    try {
      await familiesApi.promoteMember(parseInt(familyId), userId);
      showMessage('Member promoted to co-owner!', 'success');
      loadFamily();
    } catch (error: any) {
      showMessage(error.response?.data?.message || 'Failed to promote', 'error');
    }
  };

  const handleDemote = async (userId: string) => {
    if (!familyId) return;
    try {
      await familiesApi.demoteMember(parseInt(familyId), userId);
      showMessage('Member demoted', 'success');
      loadFamily();
    } catch (error: any) {
      showMessage(error.response?.data?.message || 'Failed to demote', 'error');
    }
  };

  const handleRemove = async (userId: string) => {
    if (!familyId) return;
    if (!confirm('Remove this member from the family?')) return;
    try {
      await familiesApi.removeMember(parseInt(familyId), userId);
      showMessage('Member removed', 'success');
      loadFamily();
    } catch (error: any) {
      showMessage(error.response?.data?.message || 'Failed to remove', 'error');
    }
  };

  const getRoleBadge = (role: string) => {
    if (role === 'admin') return <span className="badge" style={{ background: 'var(--color-warning)', color: '#000' }}>👑 Co-owner</span>;
    return <span className="badge" style={{ background: 'var(--color-border-light)', color: 'var(--color-text-secondary)' }}>Member</span>;
  };

  if (loading) {
    return (
      <div style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <span className="spinner" style={{ width: 32, height: 32 }} />
      </div>
    );
  }

  if (!family) {
    return (
      <div className="container" style={{ paddingTop: 'var(--space-3xl)' }}>
        <div className="empty-state">
          <div className="empty-state-title">Family not found</div>
          <Link to="/families" className="btn btn-primary" style={{ marginTop: 'var(--space-lg)' }}>
            Back to Families
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="container" style={{ paddingTop: 'var(--space-2xl)', paddingBottom: 'var(--space-3xl)', maxWidth: 700 }}>
      {/* Back link */}
      <Link to={`/families/${familyId}`} style={{
        display: 'inline-flex', alignItems: 'center', gap: 'var(--space-sm)',
        fontSize: 14, color: 'var(--color-text-secondary)', marginBottom: 'var(--space-xl)',
      }}>
        ← Back to Feed
      </Link>

      <h1 style={{ fontSize: 28, fontWeight: 800, marginBottom: 'var(--space-xl)', letterSpacing: '-0.5px' }}>
        Family Settings
      </h1>

      {/* Message */}
      {message && (
        <div style={{
          padding: 'var(--space-md) var(--space-lg)', marginBottom: 'var(--space-xl)',
          backgroundColor: messageType === 'success' ? 'var(--color-success-bg)' : 'var(--color-error-bg)',
          color: messageType === 'success' ? 'var(--color-success)' : 'var(--color-error)',
          borderRadius: 'var(--radius-md)', fontSize: 14, fontWeight: 500,
        }}>
          {message}
        </div>
      )}

      {/* Family Info Card */}
      <div className="card" style={{ marginBottom: 'var(--space-xl)', padding: 'var(--space-xl)' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-md)', marginBottom: 'var(--space-md)' }}>
          <div style={{
            width: 48, height: 48, borderRadius: 'var(--radius-lg)',
            background: 'var(--color-brand-gradient)',
            display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 24,
          }}>👨‍👩‍👧</div>
          <div>
            <h2 style={{ fontSize: 20, fontWeight: 700, margin: 0 }}>{family.name}</h2>
            <p style={{ fontSize: 14, color: 'var(--color-text-secondary)', margin: 0 }}>
              {family.memberCount} members · Owner: {family.ownerName}
            </p>
          </div>
        </div>
      </div>

      {/* Settings Form */}
      {isOwnerOrAdmin && (
        <div style={{ marginBottom: 'var(--space-xl)' }}>
          <h3 style={{ fontSize: 18, fontWeight: 700, marginBottom: 'var(--space-md)' }}>Edit Family</h3>
          <form onSubmit={e => { e.preventDefault(); handleSave(); }}>
            <div style={{
              background: 'var(--color-surface)', borderRadius: 'var(--radius-lg)',
              border: '1px solid var(--color-border)', overflow: 'hidden',
            }}>
              <div style={{ padding: 'var(--space-md) var(--space-xl)', borderBottom: '1px solid var(--color-border-light)' }}>
                <label style={{ display: 'block', fontSize: 14, fontWeight: 600, marginBottom: 'var(--space-sm)' }}>Family Name</label>
                <input type="text" className="input" value={name} onChange={e => setName(e.target.value)} style={{ fontSize: 14 }} />
              </div>
              <div style={{ padding: 'var(--space-md) var(--space-xl)', borderBottom: '1px solid var(--color-border-light)' }}>
                <label style={{ display: 'block', fontSize: 14, fontWeight: 600, marginBottom: 'var(--space-sm)' }}>Description</label>
                <textarea className="input" value={description} onChange={e => setDescription(e.target.value)} rows={3} style={{ fontSize: 14, resize: 'vertical' }} />
              </div>
              <div style={{ padding: 'var(--space-md) var(--space-xl)' }}>
                <label style={{ display: 'block', fontSize: 14, fontWeight: 600, marginBottom: 'var(--space-sm)' }}>Cover Photo URL</label>
                <input type="text" className="input" value={coverPhotoUrl} onChange={e => setCoverPhotoUrl(e.target.value)} placeholder="https://example.com/photo.jpg" style={{ fontSize: 14 }} />
              </div>
            </div>
            <button type="submit" className="btn btn-primary btn-lg btn-block" disabled={saving} style={{ marginTop: 'var(--space-xl)' }}>
              {saving ? <span className="spinner" style={{ width: 18, height: 18, borderWidth: 2 }} /> : 'Save Settings'}
            </button>
          </form>
        </div>
      )}

      {/* Invite Member */}
      {isOwnerOrAdmin && (
        <div style={{ marginBottom: 'var(--space-xl)' }}>
          <h3 style={{ fontSize: 18, fontWeight: 700, marginBottom: 'var(--space-md)' }}>Invite Member</h3>
          <div style={{ display: 'flex', gap: 'var(--space-sm)' }}>
            <input
              type="email"
              className="input"
              placeholder="Email address"
              value={inviteEmail}
              onChange={e => setInviteEmail(e.target.value)}
              style={{ flex: 1 }}
            />
            <button onClick={handleInvite} className="btn btn-primary" disabled={!inviteEmail.trim()}>Invite</button>
          </div>
        </div>
      )}

      {/* Members List */}
      <div>
        <h3 style={{ fontSize: 18, fontWeight: 700, marginBottom: 'var(--space-md)' }}>Members ({members.length})</h3>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-sm)' }}>
          {members.map(member => (
            <div key={member.id} style={{
              display: 'flex', alignItems: 'center', gap: 'var(--space-md)',
              padding: 'var(--space-md) var(--space-lg)',
              background: 'var(--color-surface)', borderRadius: 'var(--radius-md)',
              border: '1px solid var(--color-border)',
            }}>
              <div className="avatar avatar-sm" style={{ background: 'var(--color-brand-gradient)', flexShrink: 0 }}>
                {member.name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)}
              </div>
              <div style={{ flex: 1 }}>
                <div style={{ fontWeight: 600, fontSize: 14 }}>{member.name}</div>
                <div style={{ fontSize: 12, color: 'var(--color-text-tertiary)' }}>{member.role === 'admin' ? 'Co-owner' : 'Member'}</div>
              </div>
              {getRoleBadge(member.role)}
              {/* Owner-only management actions */}
              {isOwner && member.userId !== currentUserId && (
                <div style={{ display: 'flex', gap: 'var(--space-xs)' }}>
                  {member.role === 'member' ? (
                    <button onClick={() => handlePromote(member.userId)} className="btn btn-ghost" style={{ fontSize: 12, padding: 'var(--space-xs) var(--space-sm)' }}>
                      👑 Promote
                    </button>
                  ) : (
                    <button onClick={() => handleDemote(member.userId)} className="btn btn-ghost" style={{ fontSize: 12, padding: 'var(--space-xs) var(--space-sm)', color: 'var(--color-warning)' }}>
                      ⬇️ Demote
                    </button>
                  )}
                  <button onClick={() => handleRemove(member.userId)} className="btn btn-ghost" style={{ fontSize: 12, padding: 'var(--space-xs) var(--space-sm)', color: 'var(--color-error)' }}>
                    🗑️
                  </button>
                </div>
              )}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
