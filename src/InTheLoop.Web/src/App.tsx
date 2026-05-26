import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './context/AuthContext';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import FamiliesPage from './pages/FamiliesPage';
import CreateFamilyPage from './pages/CreateFamilyPage';
import FeedPage from './pages/FeedPage';
import NavBar from './components/NavBar';

function PrivateRoute({ children }: { children: JSX.Element }) {
  const { token } = useAuth();
  return token ? children : <Navigate to="/login" />;
}

export default function App() {
  return (
    <>
      <NavBar />
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/families" element={<PrivateRoute><FamiliesPage /></PrivateRoute>} />
          <Route path="/families/new" element={<PrivateRoute><CreateFamilyPage /></PrivateRoute>} />
          <Route path="/families/:familyId" element={<PrivateRoute><FeedPage /></PrivateRoute>} />
          <Route path="/feed" element={<PrivateRoute><div>Feed (placeholder)</div></PrivateRoute>} />
          <Route path="*" element={<Navigate to="/login" />} />
        </Routes>
      </BrowserRouter>
    </>
  );
}
