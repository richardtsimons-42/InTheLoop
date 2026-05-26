import { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { userApi } from '../services/userApi';

export default function ProfilePage() {
  const { user, token, refreshProfile } = useAuth();
  const [firstName, setFirstName] = useState(user?.firstName ?? '');
  const [lastName, setLastName] = useState(user?.lastName ?? '');
  const [avatarUrl, setAvatarUrl] = useState(user?.avatarUrl ?? '');
  const [saving, setSaving] = useState(false);
  const [avatarSaving, setAvatarSaving] = useState(false);
  const [message, setMessage] = useState('');

  if (!user) {
    return <div className="profile-page">Please log in to view your profile.</div>;
  }

  const handleSave = async () => {
    if (!token) return;
    setSaving(true);
    setMessage('');
    try {
      await userApi.updateProfile(firstName, lastName);
      await refreshProfile();
      setMessage('Profile updated!');
    } catch {
      setMessage('Failed to update profile.');
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
      setMessage('Avatar updated!');
    } catch {
      setMessage('Failed to update avatar.');
    } finally {
      setAvatarSaving(false);
    }
  };

  return (
    <div className="profile-page">
      <h1>Profile</h1>

      <div className="profile-avatar-section">
        <div className="profile-avatar">
          {user.avatarUrl ? (
            <img src={user.avatarUrl} alt="Avatar" className="avatar-image" />
          ) : (
            <div className="avatar-placeholder">
              {user.firstName[0]}{user.lastName[0]}
            </div>
          )}
        </div>
        <div className="avatar-input">
          <input
            type="url"
            placeholder="Avatar URL"
            value={avatarUrl}
            onChange={(e) => setAvatarUrl(e.target.value)}
          />
          <button onClick={handleAvatarSave} disabled={avatarSaving || !token}>
            {avatarSaving ? 'Saving...' : 'Update Avatar'}
          </button>
        </div>
      </div>

      <div className="profile-info">
        <div className="info-item">
          <label>Email</label>
          <span>{user.email}</span>
        </div>
        <div className="info-item">
          <label>First Name</label>
          <input
            type="text"
            value={firstName}
            onChange={(e) => setFirstName(e.target.value)}
          />
        </div>
        <div className="info-item">
          <label>Last Name</label>
          <input
            type="text"
            value={lastName}
            onChange={(e) => setLastName(e.target.value)}
          />
        </div>
        <div className="info-item">
          <label>Verified</label>
          <span>{user.isVerified ? 'Yes' : 'No'}</span>
        </div>
        <div className="info-item">
          <label>Member Since</label>
          <span>{new Date(user.createdAt).toLocaleDateString()}</span>
        </div>
      </div>

      <button className="save-profile-btn" onClick={handleSave} disabled={saving || !token}>
        {saving ? 'Saving...' : 'Save Changes'}
      </button>

      {message && <p className="profile-message">{message}</p>}
    </div>
  );
}
