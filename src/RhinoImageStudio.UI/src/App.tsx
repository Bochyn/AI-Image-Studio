import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { HomePage } from './pages/HomePage';
import { StudioPage } from './pages/StudioPage';
import { ToastProvider } from './components/Common/ToastProvider';

function App() {
  return (
    <ToastProvider>
      <Router>
        <div className="min-h-screen w-full bg-background text-text">
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/project/:sessionId" element={<StudioPage />} />
            {/* Legacy route for backwards compatibility */}
            <Route path="/session/:sessionId" element={<StudioPage />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </div>
      </Router>
    </ToastProvider>
  );
}

export default App;
