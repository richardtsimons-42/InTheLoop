import { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { useNavigate, Link } from 'react-router-dom';
import '../index.css';

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    try {
      await login(email, password);
      navigate('/feed');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Invalid email or password');
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
        maxWidth: 420,
        width: '100%',
        padding: 'var(--space-2xl)',
      }}>
        {/* Logo */}
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
          }}>◉</div>
          <h1 style={{ fontSize: 24, fontWeight: 800, letterSpacing: '-0.5px', marginBottom: 'var(--space-xs)' }}>
            InTheLoop
          </h1>
          <p style={{ color: 'var(--color-text-secondary)', fontSize: 15 }}>
            Sign in to your family network
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
              Email
            </label>
            <input
              type="email"
              className="input"
              placeholder="you@example.com"
              value={email}
              onChange={e => setEmail(e.target.value)}
              required
            />
          </div>

          <div style={{ marginBottom: 'var(--space-xl)' }}>
            <label style={{ display: 'block', fontSize: 14, fontWeight: 600, marginBottom: 'var(--space-sm)', color: 'var(--color-text)' }}>
              Password
            </label>
            <input
              type="password"
              className="input"
              placeholder="••••••••"
              value={password}
              onChange={e => setPassword(e.target.value)}
              required
            />
          </div>

          <button
            type="submit"
            className="btn btn-primary btn-block btn-lg"
            disabled={loading}
          >
            {loading ? <span className="spinner" /> : 'Sign In'}
          </button>
        </form>

        <p style={{
          textAlign: 'center',
          marginTop: 'var(--space-xl)',
          fontSize: 14,
          color: 'var(--color-text-secondary)',
        }}>
          Don't have an account?{' '}
          <Link to="/register" style={{ fontWeight: 600 }}>Create one</Link>
        </p>
      </div>
    </div>
  );
}
