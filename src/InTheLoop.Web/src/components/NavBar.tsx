import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function NavBar() {
  const { user, logout } = useAuth();

  return (
    <nav style={{ background: '#2c3e50', padding: '12px 24px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
      <Link to="/families" style={{ color: 'white', textDecoration: 'none', fontSize: 20, fontWeight: 'bold' }}>
        InTheLoop
      </Link>
      <div style={{ display: 'flex', gap: 16, alignItems: 'center' }}>
        <Link to="/families" style={{ color: 'white', textDecoration: 'none' }}>Families</Link>
        {user && <span style={{ color: 'white' }}>{user.firstName} {user.lastName}</span>}
        <button onClick={logout} style={{ background: '#e74c3c', color: 'white', border: 'none', padding: '6px 12px', borderRadius: 4, cursor: 'pointer' }}>
          Logout
        </button>
      </div>
    </nav>
  );
}
