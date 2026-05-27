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
}

export default function FamilySettingsPage() {
  const { familyId } = useParams<{ familyId: string }>();
  const navigate = useNavigate();
  const { currentUserId } = useAuth();
  const [family, setFamily] = useState<Family | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [coverPhotoUrl, setCoverPhotoUrl] = useState('');
  const [message, setMessage] = useState('');
  const [messageType, setMessageType] = useState<'success' | 'error'>('success');

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
    } catch (error) {
      console.error('Failed to load family:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    if (!familyId) return;
    setSaving(true);
    setMessage('');
    try {
      await familiesApi.updateFamily(parseInt(familyId), name, description || undefined, coverPhotoUrl || undefined);
      setMessage('Settings saved successfully!');
      setMessageType('success');
      loadFamily();
    } catch (error) {
      console.error('Failed to save family settings:', error);
      setMessage('Failed to save settings');
      setMessageType('error');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
      }}>
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

  const isOwner = family.ownerName === 'You';

  return (
    <div className="container" style={{ paddingTop: 'var(--space-2xl)', paddingBottom: 'var(--space-3xl)', maxWidth: 600 }}>
      {/* Back link */}
      <Link to={`/families/${familyId}`} style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 'var(--space-sm)',
        fontSize: 14,
        color: 'var(--color-text-secondary)',
        marginBottom: 'var(--space-xl)',
      }}>
        ← Back to Feed
      </Link>

      <h1 style={{ fontSize: 28, fontWeight: 800, marginBottom: 'var(--space-xl)', letterSpacing: '-0.5px' }}>
        Family Settings
      </h1>

      {/* Info Card */}
      <div className="card" style={{ marginBottom: 'var(--space-xl)', padding: 'var(--space-xl)' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-md)', marginBottom: 'var(--space-md)' }}>
          <div style={{
            width: 48, height: 48, borderRadius: 'var(--radius-lg)',
            background: 'var(--color-brand-gradient)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            fontSize: 24,
          }}>👨‍👩‍👧</div>
          <div>
            <h2 style={{ fontSize: 20, fontWeight: 700, margin: 0 }}>{family.name}</h2>
            <p style={{ fontSize: 14, color: 'var(--color-text-secondary)', margin: 0 }}>
              {family.memberCount} members · Owner: {family.ownerName}
            </p>
          </div>
        </div>
      </div>

      {/* Message */}
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

      {/* Form */}
      <form onSubmit={e => { e.preventDefault(); handleSave(); }}>
        <div style={{
          background: 'var(--color-surface)',
          borderRadius: 'var(--radius-lg)',
          border: '1px solid var(--color-border)',
          overflow: 'hidden',
        }}>
          <div style={{ padding: 'var(--space-md) var(--space-xl)', borderBottom: '1px solid var(--color-border-light)' }}>
            <label style={{ display: 'block', fontSize: 14, fontWeight: 600, marginBottom: 'var(--space-sm)', color: 'var(--color-text)' }}>
              Family Name
            </label>
            <input
              type="text"
              className="input"
              value={name}
              onChange={e => setName(e.target.value)}
              disabled={!isOwner}
              style={{ fontSize: 14 }}
            />
          </div>

          <div style={{ padding: 'var(--space-md) var(--space-xl)', borderBottom: '1px solid var(--color-border-light)' }}>
            <label style={{ display: 'block', fontSize: 14, fontWeight: 600, marginBottom: 'var(--space-sm)', color: 'var(--color-text)' }}>
              Description
            </label>
            <textarea
              className="input"
              value={description}
              onChange={e => setDescription(e.target.value)}
              disabled={!isOwner}
              rows={4}
              style={{ fontSize: 14, resize: 'vertical' }}
            />
          </div>

          <div style={{ padding: 'var(--space-md) var(--space-xl)' }}>
            <label style={{ display: 'block', fontSize: 14, fontWeight: 600, marginBottom: 'var(--space-sm)', color: 'var(--color-text)' }}>
              Cover Photo URL
            </label>
            <input
              type="text"
              className="input"
              value={coverPhotoUrl}
              onChange={e => setCoverPhotoUrl(e.target.value)}
              disabled={!isOwner}
              placeholder="https://example.com/photo.jpg"
              style={{ fontSize: 14 }}
            />
          </div>
        </div>

        {isOwner && (
          <button
            type="submit"
            className="btn btn-primary btn-lg btn-block"
            disabled={saving}
            style={{ marginTop: 'var(--space-xl)' }}
          >
            {saving ? <span className="spinner" style={{ width: 18, height: 18, borderWidth: 2 }} /> : 'Save Settings'}
          </button>
        )}

        {!isOwner && (
          <p style={{
            marginTop: 'var(--space-lg)',
            textAlign: 'center',
            fontSize: 14,
            color: 'var(--color-text-tertiary)',
          }}>
            Only the family owner can edit settings
          </p>
        )}
      </form>
    </div>
  );
}
