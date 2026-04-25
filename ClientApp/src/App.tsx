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
          <Route path="/app/" element={<HomePage />} />
          <Route path="/app/arrangementer" element={<ArrangementerPage />} />
          <Route path="/app/betingelser" element={<BetingelserPage />} />
          <Route path="/app/cookies" element={<CookiesPage />} />
          <Route path="/app/calendar" element={<CalendarPage />} />
          <Route path="/app/library" element={<LibraryPage />} />
          <Route path="/app/users" element={<UsersPage />} />
          <Route path="/app/roles" element={<RolesPage />} />
          <Route path="/app/messages" element={<MessagesPage />} />
          <Route path="/app/pay" element={<PayPage />} />
          <Route path="/app/pay/product/:id" element={<PayProductPage />} />
          <Route path="/app/pay/success" element={<PaySuccessPage />} />
          <Route path="/app/pay/kontingent-success" element={<KontingentSuccessPage />} />
          <Route path="/app/pay/cancel" element={<PayCancelPage />} />
          <Route path="*" element={<Navigate to="/app/" replace />} />
        </Routes>
      </div>
    </BrowserRouter>
  );
}

export default App;
