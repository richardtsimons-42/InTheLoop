import { useState, useEffect, createContext, useContext, ReactNode } from 'react';
import axios from 'axios';
import { authApi } from '../services/api';
import { userApi } from '../services/userApi';

export interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  avatarUrl: string | null;
  isVerified: boolean;
  createdAt: string;
}

interface AuthContextType {
  user: UserProfile | null;
  token: string | null;
  login: (email: string, password: string) => Promise<void>;
  register: (firstName: string, lastName: string, email: string, password: string) => Promise<void>;
  logout: () => void;
  refreshProfile: () => Promise<void>;
  currentUserId: string | null;
}

const AuthContext = createContext<AuthContextType | null>(null);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [token, setToken] = useState<string | null>(localStorage.getItem('token'));
  const [currentUserId, setCurrentUserId] = useState<string | null>(null);

  axios.defaults.headers.common['Authorization'] = token ? `Bearer ${token}` : '';

  const refreshProfile = async () => {
    if (!token) {
      setUser(null);
      return;
    }
    try {
      const profile = await userApi.getProfile();
      setUser(profile);
      setCurrentUserId(profile.id);
      localStorage.setItem('currentUserId', profile.id);
    } catch {
      setUser(null);
    }
  };

  useEffect(() => {
    refreshProfile();
  }, [token]);

  const login = async (email: string, password: string) => {
    const response = await axios.post('/api/auth/login', { email, password });
    setToken(response.data.token);
    localStorage.setItem('token', response.data.token);
    axios.defaults.headers.common['Authorization'] = `Bearer ${response.data.token}`;
    await refreshProfile();
  };

  const register = async (firstName: string, lastName: string, email: string, password: string) => {
    const response = await axios.post('/api/auth/register', { firstName, lastName, email, password });
    setToken(response.data.token);
    localStorage.setItem('token', response.data.token);
    axios.defaults.headers.common['Authorization'] = `Bearer ${response.data.token}`;
    await refreshProfile();
  };

  const logout = async () => {
    try {
      await authApi.logout();
    } catch {
      // Backend may be down — still clear local state
    }
    setToken(null);
    setUser(null);
    setCurrentUserId(null);
    localStorage.removeItem('token');
    localStorage.removeItem('currentUserId');
    delete axios.defaults.headers.common['Authorization'];
  };

  return (
    <AuthContext.Provider value={{ user, token, login, register, logout, refreshProfile, currentUserId }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within AuthProvider');
  return context;
};
