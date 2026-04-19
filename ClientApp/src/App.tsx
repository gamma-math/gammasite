import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import CalendarPage from './pages/CalendarPage';
import LibraryPage from './pages/LibraryPage';
import UsersPage from './pages/UsersPage';
import RolesPage from './pages/RolesPage';
import MessagesPage from './pages/MessagesPage';
import PayPage from './pages/PayPage';
import PayProductPage from './pages/PayProductPage';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/calendar" element={<CalendarPage />} />
        <Route path="/library" element={<LibraryPage />} />
        <Route path="/users" element={<UsersPage />} />
        <Route path="/roles" element={<RolesPage />} />
        <Route path="/messages" element={<MessagesPage />} />
        <Route path="/pay" element={<PayPage />} />
        <Route path="/pay/product/:id" element={<PayProductPage />} />
        {/* Additional routes added here as MVC views are migrated */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App
