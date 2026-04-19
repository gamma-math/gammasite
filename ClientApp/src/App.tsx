import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import CalendarPage from './pages/CalendarPage';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/calendar" element={<CalendarPage />} />
        {/* Additional routes added here as MVC views are migrated */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App
