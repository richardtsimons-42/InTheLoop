import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { familiesApi } from '../services/api';
import { useAuth } from '../context/AuthContext';

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

  useEffect(() => {
    loadFamily();
  }, [familyId]);

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
    try {
      const updatedFamily = await familiesApi.updateFamily(
        parseInt(familyId),
        name,
        description || undefined,
        coverPhotoUrl || undefined
      );
      setFamily(updatedFamily.data);
      alert('Family settings saved successfully!');
    } catch (error) {
      console.error('Failed to save family settings:', error);
      alert('Failed to save family settings');
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <div style={{ padding: 40, textAlign: 'center' }}>Loading...</div>;
  if (!family) return <div style={{ padding: 40, textAlign: 'center' }}>Family not found</div>;

  const isOwner = family.ownerName === 'You';

  return (
    <div style={{ maxWidth: 600, margin: '40px auto', padding: 20 }}>
      <h1>Family Settings</h1>
      
      <div style={{ marginBottom: 20, padding: 16, border: '1px solid #ddd', borderRadius: 8 }}>
        <h3>{family.name}</h3>
        <p>{family.memberCount} members</p>
        <p>Owner: {family.ownerName}</p>
      </div>

      <form onSubmit={e => { e.preventDefault(); handleSave(); }}>
        <div style={{ marginBottom: 16 }}>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 600 }}>
            Family Name
          </label>
          <input
            type="text"
            value={name}
            onChange={e => setName(e.target.value)}
            disabled={!isOwner}
            style={{
              width: '100%',
              padding: '12px 16px',
              border: '1px solid #ddd',
              borderRadius: 8,
              fontSize: 14,
              boxSizing: 'border-box',
            }}
          />
        </div>

        <div style={{ marginBottom: 16 }}>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 600 }}>
            Description
          </label>
          <textarea
            value={description}
            onChange={e => setDescription(e.target.value)}
            disabled={!isOwner}
            rows={4}
            style={{
              width: '100%',
              padding: '12px 16px',
              border: '1px solid #ddd',
              borderRadius: 8,
              fontSize: 14,
              boxSizing: 'border-box',
              resize: 'vertical',
            }}
          />
        </div>

        <div style={{ marginBottom: 16 }}>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 600 }}>
            Cover Photo URL
          </label>
          <input
            type="text"
            value={coverPhotoUrl}
            onChange={e => setCoverPhotoUrl(e.target.value)}
            disabled={!isOwner}
            placeholder="https://example.com/photo.jpg"
            style={{
              width: '100%',
              padding: '12px 16px',
              border: '1px solid #ddd',
              borderRadius: 8,
              fontSize: 14,
              boxSizing: 'border-box',
            }}
          />
        </div>

        {isOwner && (
          <button
            type="submit"
            disabled={saving}
            style={{
              padding: '12px 24px',
              backgroundColor: '#3498db',
              color: 'white',
              border: 'none',
              borderRadius: 8,
              cursor: saving ? 'not-allowed' : 'pointer',
              fontWeight: 600,
              fontSize: 14,
            }}
          >
            {saving ? 'Saving...' : 'Save Settings'}
          </button>
        )}
      </form>

      <div style={{ marginTop: 20 }}>
        <Link to={`/families/${familyId}`}>
          <button style={{ marginRight: 10 }}>
            ← Back to Feed
          </button>
        </Link>
      </div>
    </div>
  );
}
