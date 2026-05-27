import { useState } from 'react';
import { familiesApi } from '../services/api';
import { useNavigate } from 'react-router-dom';
import '../index.css';

export default function CreateFamilyPage() {
  const navigate = useNavigate();
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    try {
      await familiesApi.createFamily(name, description || undefined);
      navigate('/families');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to create family');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{
      minHeight: '100vh',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      padding: 'var(--space-xl)',
      background: 'linear-gradient(135deg, #eef2ff 0%, #f8f9fc 50%, #faf5ff 100%)',
    }}>
      <div className="card fade-in" style={{
        maxWidth: 480,
        width: '100%',
        padding: 'var(--space-2xl)',
      }}>
        <div style={{ textAlign: 'center', marginBottom: 'var(--space-2xl)' }}>
          <div style={{
            width: 56,
            height: 56,
            borderRadius: 'var(--radius-lg)',
            background: 'var(--color-brand-gradient)',
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontSize: 28,
            color: 'white',
            marginBottom: 'var(--space-lg)',
          }}>👨‍👩‍👧</div>
          <h1 style={{ fontSize: 24, fontWeight: 800, letterSpacing: '-0.5px', marginBottom: 'var(--space-xs)' }}>
            Create a Family
          </h1>
          <p style={{ color: 'var(--color-text-secondary)', fontSize: 15 }}>
            Start your own family network
          </p>
        </div>

        {error && (
          <div style={{
            padding: 'var(--space-md) var(--space-lg)',
            marginBottom: 'var(--space-lg)',
            backgroundColor: 'var(--color-error-bg)',
            color: 'var(--color-error)',
            borderRadius: 'var(--radius-md)',
            fontSize: 14,
            fontWeight: 500,
          }}>
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div style={{ marginBottom: 'var(--space-lg)' }}>
            <label style={{ display: 'block', fontSize: 14, fontWeight: 600, marginBottom: 'var(--space-sm)', color: 'var(--color-text)' }}>
              Family Name
            </label>
            <input
              type="text"
              className="input"
              placeholder="The Smiths"
              value={name}
              onChange={e => setName(e.target.value)}
              required
            />
          </div>

          <div style={{ marginBottom: 'var(--space-xl)' }}>
            <label style={{ display: 'block', fontSize: 14, fontWeight: 600, marginBottom: 'var(--space-sm)', color: 'var(--color-text)' }}>
              Description <span style={{ fontWeight: 400, color: 'var(--color-text-tertiary)' }}>(optional)</span>
            </label>
            <textarea
              className="input"
              placeholder="What's your family about?"
              value={description}
              onChange={e => setDescription(e.target.value)}
              rows={3}
            />
          </div>

          <button
            type="submit"
            className="btn btn-primary btn-block btn-lg"
            disabled={loading}
          >
            {loading ? <span className="spinner" /> : 'Create Family'}
          </button>
        </form>
      </div>
    </div>
  );
}
