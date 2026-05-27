import { useState, useEffect } from 'react';
import { familiesApi } from '../services/api';
import { Link } from 'react-router-dom';
import InviteMember from '../components/InviteMember';
import '../index.css';

interface Family {
  id: number;
  name: string;
  description: string | null;
  ownerName: string;
  memberCount: number;
  coverPhotoUrl: string | null;
  createdAt: string;
  userRole: string;
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

  const formatJoinedDate = (dateStr: string) => {
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  };

  const getRoleBadge = (role: string) => {
    if (role === 'admin') return <span className="badge" style={{ background: 'var(--color-warning)', color: '#000', marginLeft: 'var(--space-xs)' }}>👑 Co-owner</span>;
    return null;
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

  return (
    <div className="container-wide" style={{ paddingTop: 'var(--space-2xl)', paddingBottom: 'var(--space-3xl)' }}>
      {/* Header */}
      <div style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        marginBottom: 'var(--space-2xl)',
        flexWrap: 'wrap',
        gap: 'var(--space-lg)',
      }}>
        <div>
          <h1 style={{ fontSize: 28, fontWeight: 800, letterSpacing: '-0.5px', marginBottom: 'var(--space-xs)' }}>
            My Families
          </h1>
          <p style={{ color: 'var(--color-text-secondary)', fontSize: 15 }}>
            Manage your family groups
          </p>
        </div>
        <Link to="/families/new">
          <button className="btn btn-primary">
            <span style={{ fontSize: 18, lineHeight: 1 }}>+</span> New Family
          </button>
        </Link>
      </div>

      {/* Family Cards */}
      {families.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-icon">👨‍👩‍👧</div>
          <div className="empty-state-title">No families yet</div>
          <p style={{ marginBottom: 'var(--space-xl)' }}>Create a family to start sharing moments together</p>
          <Link to="/families/new">
            <button className="btn btn-primary btn-lg">Create Your First Family</button>
          </Link>
        </div>
      ) : (
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))',
          gap: 'var(--space-lg)',
        }}>
          {families.map(family => (
            <div key={family.id} className="card fade-in" style={{ padding: 0, overflow: 'hidden' }}>
              {/* Cover */}
              <div style={{
                height: 100,
                background: 'var(--color-brand-gradient)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontSize: 40,
              }}>
                👨‍👩‍👧
              </div>

              <div style={{ padding: 'var(--space-lg) var(--space-xl) var(--space-xl)' }}>
                <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', marginBottom: 'var(--space-sm)' }}>
                  <h3 style={{ fontSize: 18, fontWeight: 700, letterSpacing: '-0.3px' }}>{family.name}</h3>
                  <span className="badge badge-brand">{family.memberCount} members</span>
                </div>

                <p style={{
                  color: 'var(--color-text-secondary)',
                  fontSize: 14,
                  marginBottom: 'var(--space-md)',
                  lineHeight: 1.5,
                  minHeight: 40,
                }}>
                  {family.description || 'No description'}
                </p>

                <p style={{ fontSize: 12, color: 'var(--color-text-tertiary)', marginBottom: 'var(--space-lg)' }}>
                  Owner: {family.ownerName} · Joined {formatJoinedDate(family.createdAt)}
                  {getRoleBadge(family.userRole)}
                </p>

                <div style={{ display: 'flex', gap: 'var(--space-sm)' }}>
                  <Link to={`/families/${family.id}`} style={{ flex: 1 }}>
                    <button className="btn btn-primary btn-block">Enter Family</button>
                  </Link>
                  <Link to={`/families/${family.id}/settings`} style={{ flex: '0 0 auto' }}>
                    <button className="btn btn-secondary" style={{ padding: 'var(--space-sm) var(--space-lg)' }}>⚙️</button>
                  </Link>
                </div>

                <div style={{ marginTop: 'var(--space-lg)', paddingTop: 'var(--space-lg)', borderTop: '1px solid var(--color-border-light)' }}>
                  <InviteMember familyId={family.id} onInvite={fetchFamilies} />
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
