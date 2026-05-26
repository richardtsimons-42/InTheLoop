import { useState, useEffect } from 'react';
import { familiesApi } from '../services/api';
import { Link } from 'react-router-dom';

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

  useEffect(() => {
    familiesApi.getMyFamilies().then(res => {
      setFamilies(res.data);
      setLoading(false);
    });
  }, []);

  if (loading) return <div>Loading...</div>;

  return (
    <div style={{ maxWidth: 600, margin: '40px auto', padding: 20 }}>
      <h1>My Families</h1>
      <Link to="/families/new" style={{ display: 'block', marginBottom: 20 }}>
        <button>Create New Family</button>
      </Link>
      {families.map(family => (
        <div key={family.id} style={{ border: '1px solid #ddd', padding: 16, marginBottom: 12, borderRadius: 8 }}>
          <h3>{family.name}</h3>
          <p>{family.description}</p>
          <p>{family.memberCount} members</p>
          <Link to={`/families/${family.id}`}>
            <button>Enter Family</button>
          </Link>
        </div>
      ))}
    </div>
  );
}
