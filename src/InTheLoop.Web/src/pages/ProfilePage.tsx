import { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { userApi } from '../services/userApi';
import '../index.css';

export default function ProfilePage() {
  const { user, token, refreshProfile } = useAuth();
  const [firstName, setFirstName] = useState(user?.firstName ?? '');
  const [lastName, setLastName] = useState(user?.lastName ?? '');
  const [avatarUrl, setAvatarUrl] = useState(user?.avatarUrl ?? '');
  const [saving, setSaving] = useState(false);
  const [avatarSaving, setAvatarSaving] = useState(false);
  const [message, setMessage] = useState('');
  const [messageType, setMessageType] = useState<'success' | 'error'>('success');

  if (!user) {
    return (
      <div className="container" style={{ paddingTop: 'var(--space-3xl)' }}>
        <div className="empty-state">
          <p>Please log in to view your profile.</p>
        </div>
      </div>
    );
  }

  const handleSave = async () => {
    if (!token) return;
    setSaving(true);
    setMessage('');
    try {
      await userApi.updateProfile(firstName, lastName);
      await refreshProfile();
      setMessage('Profile updated successfully!');
      setMessageType('success');
    } catch {
      setMessage('Failed to update profile.');
      setMessageType('error');
    } finally {
      setSaving(false);
    }
  };

  const handleAvatarSave = async () => {
    if (!token) return;
    setAvatarSaving(true);
    setMessage('');
    try {
      await userApi.updateAvatar(avatarUrl);
      await refreshProfile();
      setMessage('Avatar updated successfully!');
      setMessageType('success');
    } catch {
      setMessage('Failed to update avatar.');
      setMessageType('error');
    } finally {
      setAvatarSaving(false);
    }
  };

  const initials = `${user.firstName?.[0] || ''}${user.lastName?.[0] || ''}`.toUpperCase();

  return (
    <div className="container" style={{ paddingTop: 'var(--space-2xl)', paddingBottom: 'var(--space-3xl)', maxWidth: 600 }}>
      <h1 style={{ fontSize: 28, fontWeight: 800, marginBottom: 'var(--space-xl)', letterSpacing: '-0.5px' }}>
        Profile
      </h1>

      {/* Success/Error Message */}
      {message && (
        <div style={{
          padding: 'var(--space-md) var(--space-lg)',
          marginBottom: 'var(--space-xl)',
          backgroundColor: messageType === 'success' ? 'var(--color-success-bg)' : 'var(--color-error-bg)',
          color: messageType === 'success' ? 'var(--color-success)' : 'var(--color-error)',
          borderRadius: 'var(--radius-md)',
          fontSize: 14,
          fontWeight: 500,
        }}>
          {message}
        </div>
      )}

      {/* Avatar Section */}
      <div style={{
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--space-xl)',
        marginBottom: 'var(--space-2xl)',
        padding: 'var(--space-xl)',
        background: 'var(--color-surface)',
        borderRadius: 'var(--radius-lg)',
        border: '1px solid var(--color-border)',
      }}>
        <div className="avatar avatar-xl" style={{
          background: user.avatarUrl ? 'transparent' : 'var(--color-brand-gradient)',
          border: user.avatarUrl ? '3px solid var(--color-brand-light)' : 'none',
          overflow: 'hidden',
        }}>
          {user.avatarUrl ? (
            <img src={user.avatarUrl} alt="Avatar" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
          ) : (
            initials || '?'
          )}
        </div>

        <div style={{ flex: 1 }}>
          <label style={{ display: 'block', fontSize: 14, fontWeight: 600, marginBottom: 'var(--space-sm)', color: 'var(--color-text)' }}>
            Avatar URL
          </label>
          <div style={{ display: 'flex', gap: 'var(--space-sm)' }}>
            <input
              type="url"
              className="input"
              placeholder="https://example.com/avatar.jpg"
              value={avatarUrl}
              onChange={(e) => setAvatarUrl(e.target.value)}
              style={{ fontSize: 14 }}
            />
            <button
              onClick={handleAvatarSave}
              className="btn btn-primary"
              disabled={avatarSaving || !token || !avatarUrl}
            >
              {avatarSaving ? '...' : 'Update'}
            </button>
          </div>
        </div>
      </div>

      {/* Info Section */}
      <div style={{
        background: 'var(--color-surface)',
        borderRadius: 'var(--radius-lg)',
        border: '1px solid var(--color-border)',
        overflow: 'hidden',
      }}>
        {[
          { label: 'Email', value: user.email, editable: false },
          { label: 'First Name', value: firstName, editable: true, setter: setFirstName },
          { label: 'Last Name', value: lastName, editable: true, setter: setLastName },
          { label: 'Verified', value: user.isVerified ? '✅ Yes' : '❌ No', editable: false },
          { label: 'Member Since', value: new Date(user.createdAt).toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' }), editable: false },
        ].map((item, i) => (
          <div key={item.label} style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: 'var(--space-md) var(--space-xl)',
            borderBottom: i < 4 ? '1px solid var(--color-border-light)' : 'none',
            gap: 'var(--space-lg)',
          }}>
            <span style={{ fontSize: 14, fontWeight: 600, color: 'var(--color-text-secondary)', minWidth: 120 }}>
              {item.label}
            </span>
            {item.editable ? (
              <input
                type="text"
                className="input"
                value={item.value as string}
                onChange={(e) => (item.setter as (v: string) => void)(e.target.value)}
                style={{ maxWidth: 250, fontSize: 14 }}
              />
            ) : (
              <span style={{ fontSize: 14, color: 'var(--color-text)' }}>{item.value}</span>
            )}
          </div>
        ))}
      </div>

      {/* Save Button */}
      <button
        className="btn btn-primary btn-lg btn-block"
        onClick={handleSave}
        disabled={saving || !token}
        style={{ marginTop: 'var(--space-xl)' }}
      >
        {saving ? <span className="spinner" style={{ width: 18, height: 18, borderWidth: 2 }} /> : 'Save Changes'}
      </button>
    </div>
  );
}
