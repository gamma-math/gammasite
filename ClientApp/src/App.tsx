import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Navbar from './components/Navbar';
import HomePage from './pages/HomePage';
import ArrangementerPage from './pages/ArrangementerPage';
import BetingelserPage from './pages/BetingelserPage';
import CookiesPage from './pages/CookiesPage';
import CalendarPage from './pages/CalendarPage';
import LibraryPage from './pages/LibraryPage';
import UsersPage from './pages/UsersPage';
import RolesPage from './pages/RolesPage';
import MessagesPage from './pages/MessagesPage';
import PayPage from './pages/PayPage';
import PayProductPage from './pages/PayProductPage';
import PaySuccessPage from './pages/PaySuccessPage';
import KontingentSuccessPage from './pages/KontingentSuccessPage';
import PayCancelPage from './pages/PayCancelPage';

function App() {
  return (
    <BrowserRouter>
      <Navbar />
      <div className="container">
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/arrangementer" element={<ArrangementerPage />} />
          <Route path="/betingelser" element={<BetingelserPage />} />
          <Route path="/cookies" element={<CookiesPage />} />
          <Route path="/calendar" element={<CalendarPage />} />
          <Route path="/library" element={<LibraryPage />} />
          <Route path="/users" element={<UsersPage />} />
          <Route path="/roles" element={<RolesPage />} />
          <Route path="/messages" element={<MessagesPage />} />
          <Route path="/pay" element={<PayPage />} />
          <Route path="/pay/product/:id" element={<PayProductPage />} />
          <Route path="/pay/success" element={<PaySuccessPage />} />
          <Route path="/pay/kontingent-success" element={<KontingentSuccessPage />} />
          <Route path="/pay/cancel" element={<PayCancelPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </div>
    </BrowserRouter>
  );
}

export default App;
