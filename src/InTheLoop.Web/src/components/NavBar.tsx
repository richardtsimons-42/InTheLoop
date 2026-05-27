import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import '../index.css';

export default function NavBar() {
  const { user, logout } = useAuth();
  const location = useLocation();
  const [mobileOpen, setMobileOpen] = useState(false);

  const isActive = (path: string) => location.pathname === path || location.pathname.startsWith(path);

  const navLinks = [
    { to: '/families', label: 'Families' },
    { to: '/chat', label: 'Messages' },
    { to: '/feed', label: 'Feed' },
  ];

  return (
    <>
      {/* Desktop + Mobile Top Bar */}
      <nav style={{
        position: 'sticky',
        top: 0,
        zIndex: 100,
        background: 'rgba(255,255,255,0.85)',
        backdropFilter: 'blur(12px)',
        WebkitBackdropFilter: 'blur(12px)',
        borderBottom: '1px solid var(--color-border)',
        padding: '0 var(--space-xl)',
        height: '60px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
      }}>
        {/* Logo */}
        <Link to="/families" style={{
          display: 'flex',
          alignItems: 'center',
          gap: '8px',
          textDecoration: 'none',
          fontWeight: 800,
          fontSize: 20,
          background: 'var(--color-brand-gradient)',
          WebkitBackgroundClip: 'text',
          WebkitTextFillColor: 'transparent',
          letterSpacing: '-0.5px',
        }}>
          <span style={{
            background: 'var(--color-brand-gradient)',
            WebkitBackgroundClip: 'text',
            WebkitTextFillColor: 'transparent',
            fontSize: 24,
          }}>
            ◉
          </span>
          InTheLoop
        </Link>

        {/* Desktop Nav Links */}
        <div style={{
          display: 'flex',
          alignItems: 'center',
          gap: '4px',
        }} className="hide-mobile">
          {navLinks.map(link => (
            <Link
              key={link.to}
              to={link.to}
              style={{
                padding: '8px 14px',
                borderRadius: 'var(--radius-md)',
                fontSize: 14,
                fontWeight: isActive(link.to) ? 600 : 500,
                color: isActive(link.to) ? 'var(--color-brand)' : 'var(--color-text-secondary)',
                background: isActive(link.to) ? 'var(--color-brand-light)' : 'transparent',
                transition: 'all var(--transition-fast)',
                textDecoration: 'none',
              }}
              onMouseEnter={e => {
                if (!isActive(link.to)) e.currentTarget.style.color = 'var(--color-text)';
              }}
              onMouseLeave={e => {
                if (!isActive(link.to)) e.currentTarget.style.color = 'var(--color-text-secondary)';
              }}
            >
              {link.label}
            </Link>
          ))}
        </div>

        {/* User Menu */}
        <div style={{
          display: 'flex',
          alignItems: 'center',
          gap: '12px',
        }}>
          {user && (
            <Link
              to="/profile"
              className="hide-mobile"
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '8px',
                padding: '4px 8px 4px 4px',
                borderRadius: 'var(--radius-full)',
                transition: 'background var(--transition-fast)',
                textDecoration: 'none',
                color: 'inherit',
              }}
              onMouseEnter={e => e.currentTarget.style.background = 'var(--color-surface-hover)'}
              onMouseLeave={e => e.currentTarget.style.background = 'transparent'}
            >
              <div className="avatar avatar-sm" style={{
                background: 'var(--color-brand-gradient)',
                width: 32,
                height: 32,
                fontSize: 13,
              }}>
                {user.firstName?.[0]}{user.lastName?.[0]}
              </div>
              <span style={{ fontSize: 14, fontWeight: 500 }}>
                {user.firstName}
              </span>
            </Link>
          )}
          <button
            onClick={logout}
            className="hide-mobile"
            style={{
              padding: '6px 14px',
              fontSize: 13,
              fontWeight: 600,
              background: 'transparent',
              color: 'var(--color-text-secondary)',
              border: '1px solid var(--color-border)',
              borderRadius: 'var(--radius-md)',
              cursor: 'pointer',
              fontFamily: 'var(--font-sans)',
              transition: 'all var(--transition-fast)',
            }}
            onMouseEnter={e => {
              e.currentTarget.style.background = 'var(--color-error-bg)';
              e.currentTarget.style.color = 'var(--color-error)';
              e.currentTarget.style.borderColor = 'var(--color-error)';
            }}
            onMouseLeave={e => {
              e.currentTarget.style.background = 'transparent';
              e.currentTarget.style.color = 'var(--color-text-secondary)';
              e.currentTarget.style.borderColor = 'var(--color-border)';
            }}
          >
            Logout
          </button>

          {/* Mobile Hamburger */}
          <button
            onClick={() => setMobileOpen(!mobileOpen)}
            style={{
              display: 'none',
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              padding: '8px',
              fontSize: 22,
              color: 'var(--color-text)',
              lineHeight: 1,
            }}
            className="mobile-only"
            aria-label="Toggle menu"
          >
            {mobileOpen ? '✕' : '☰'}
          </button>
        </div>
      </nav>

      {/* Mobile Bottom Nav */}
      <nav style={{
        position: 'fixed',
        bottom: 0,
        left: 0,
        right: 0,
        zIndex: 100,
        background: 'rgba(255,255,255,0.95)',
        backdropFilter: 'blur(12px)',
        WebkitBackdropFilter: 'blur(12px)',
        borderTop: '1px solid var(--color-border)',
        display: 'none',
        justifyContent: 'center',
        padding: '6px 0 env(safe-area-inset-bottom, 8px)',
      }} className="mobile-only">
        <div style={{
          display: 'flex',
          justifyContent: 'space-around',
          width: '100%',
          maxWidth: 500,
        }}>
          {[
            { to: '/families', label: 'Families', icon: '👨‍👩‍👧' },
            { to: '/feed', label: 'Feed', icon: '📰' },
            { to: '/chat', label: 'Messages', icon: '💬' },
            { to: '/profile', label: 'Profile', icon: '👤' },
          ].map(item => (
            <Link
              key={item.to}
              to={item.to}
              style={{
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                gap: '2px',
                padding: '6px 12px',
                borderRadius: 'var(--radius-md)',
                textDecoration: 'none',
                color: isActive(item.to) ? 'var(--color-brand)' : 'var(--color-text-tertiary)',
                background: isActive(item.to) ? 'var(--color-brand-light)' : 'transparent',
                transition: 'all var(--transition-fast)',
                fontSize: 10,
                fontWeight: isActive(item.to) ? 600 : 500,
              }}
            >
              <span style={{ fontSize: 20 }}>{item.icon}</span>
              {item.label}
            </Link>
          ))}
        </div>
      </nav>

      {/* Mobile Dropdown */}
      {mobileOpen && (
        <div style={{
          position: 'absolute',
          top: '60px',
          right: '0',
          left: '0',
          background: 'var(--color-surface)',
          borderBottom: '1px solid var(--color-border)',
          boxShadow: 'var(--shadow-lg)',
          padding: 'var(--space-lg)',
          display: 'none',
        }} className="mobile-only">
          <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
            {navLinks.map(link => (
              <Link
                key={link.to}
                to={link.to}
                onClick={() => setMobileOpen(false)}
                style={{
                  padding: '12px 16px',
                  borderRadius: 'var(--radius-md)',
                  fontSize: 16,
                  fontWeight: isActive(link.to) ? 600 : 500,
                  color: isActive(link.to) ? 'var(--color-brand)' : 'var(--color-text)',
                  background: isActive(link.to) ? 'var(--color-brand-light)' : 'transparent',
                  textDecoration: 'none',
                }}
              >
                {link.label}
              </Link>
            ))}
            <button
              onClick={() => { logout(); setMobileOpen(false); }}
              style={{
                padding: '12px 16px',
                borderRadius: 'var(--radius-md)',
                fontSize: 16,
                fontWeight: 600,
                color: 'var(--color-error)',
                background: 'var(--color-error-bg)',
                border: 'none',
                cursor: 'pointer',
                fontFamily: 'var(--font-sans)',
                textAlign: 'left',
                marginTop: '8px',
              }}
            >
              Logout
            </button>
          </div>
        </div>
      )}

      {/* Spacer for mobile bottom nav */}
      <div style={{ height: '64px' }} className="mobile-only" />
    </>
  );
}
