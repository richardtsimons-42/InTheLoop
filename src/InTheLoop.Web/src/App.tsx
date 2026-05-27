import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './context/AuthContext';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import FamiliesPage from './pages/FamiliesPage';
import CreateFamilyPage from './pages/CreateFamilyPage';
import FamilySettingsPage from './pages/FamilySettingsPage';
import FeedPage from './pages/FeedPage';
import ProfilePage from './pages/ProfilePage';
import ChatPage from './pages/ChatPage';
import NavBar from './components/NavBar';

function PrivateRoute({ children }: { children: JSX.Element }) {
  const { token } = useAuth();
  return token ? children : <Navigate to="/login" />;
}

export default function App() {
  return (
    <BrowserRouter>
      <>
        <NavBar />
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/families" element={<PrivateRoute><FamiliesPage /></PrivateRoute>} />
          <Route path="/families/new" element={<PrivateRoute><CreateFamilyPage /></PrivateRoute>} />
          <Route path="/families/:familyId/settings" element={<PrivateRoute><FamilySettingsPage /></PrivateRoute>} />
          <Route path="/families/:familyId" element={<PrivateRoute><FeedPage /></PrivateRoute>} />
          <Route path="/feed" element={<PrivateRoute><FeedPage /></PrivateRoute>} />
          <Route path="/chat" element={<PrivateRoute><ChatPage /></PrivateRoute>} />
          <Route path="/chat/:conversationId" element={<PrivateRoute><ChatPage /></PrivateRoute>} />
          <Route path="/profile" element={<PrivateRoute><ProfilePage /></PrivateRoute>} />
          <Route path="*" element={<Navigate to="/login" />} />
        </Routes>
      </>
    </BrowserRouter>
  );
}
