import { useState, useEffect } from 'react';
import { familiesApi } from '../services/api';
import { Link } from 'react-router-dom';
import InviteMember from '../components/InviteMember';

interface Family {
  id: number;
  name: string;
  description: string | null;
  ownerName: string;
  memberCount: number;
  coverPhotoUrl: string | null;
  createdAt: string;
}

export default function FamiliesPage() {
  const [families, setFamilies] = useState<Family[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchFamilies = async () => {
    try {
      const res = await familiesApi.getMyFamilies();
      setFamilies(res.data);
    } catch (error) {
      console.error('Failed to fetch families:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchFamilies();
  }, []);

  if (loading) return <div style={{ padding: 40, textAlign: 'center' }}>Loading...</div>;

  return (
    <div style={{ maxWidth: 600, margin: '40px auto', padding: 20 }}>
      <h1>My Families</h1>
      <Link to="/families/new" style={{ display: 'block', marginBottom: 20 }}>
        <button>Create New Family</button>
      </Link>
      {families.length === 0 ? (
        <p style={{ textAlign: 'center', color: '#666' }}>No families yet. Create one to get started!</p>
      ) : (
        families.map(family => (
          <div key={family.id} style={{ border: '1px solid #ddd', padding: 16, marginBottom: 12, borderRadius: 8 }}>
            <h3 style={{ margin: '0 0 8px 0' }}>{family.name}</h3>
            <p style={{ margin: '0 0 8px 0', color: '#666' }}>{family.description || 'No description'}</p>
            <p style={{ margin: '0 0 12px 0', fontSize: 14, color: '#999' }}>
              {family.memberCount} members • Owner: {family.ownerName}
            </p>
            <div style={{ display: 'flex', gap: 10, marginBottom: 12 }}>
              <Link to={`/families/${family.id}`}>
                <button>Enter Family</button>
              </Link>
              <Link to={`/families/${family.id}/settings`}>
                <button>Settings</button>
              </Link>
            </div>
            <InviteMember familyId={family.id} onInvite={fetchFamilies} />
          </div>
        ))
      )}
    </div>
  );
}
