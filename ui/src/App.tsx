import { Routes, Route, Navigate } from 'react-router-dom'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { AppLayout } from '@/components/layout/AppLayout'
import { DensityProvider } from '@/contexts/DensityContext'
import Login from '@/pages/Login'
import Dashboard from '@/pages/Dashboard'
import Robots from '@/pages/Robots'
import Machines from '@/pages/Machines'
import Processes from '@/pages/Processes'
import Jobs from '@/pages/Jobs'
import Queues from '@/pages/Queues'
import Logs from '@/pages/Logs'
import Metrics from '@/pages/Metrics'
import Alerts from '@/pages/Alerts'
import Launcher from '@/pages/Launcher'

export default function App() {
  return (
    <DensityProvider>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }>
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/robots"    element={<Robots />} />
          <Route path="/machines"  element={<Machines />} />
          <Route path="/processes" element={<Processes />} />
          <Route path="/jobs"      element={<Jobs />} />
          <Route path="/launcher" element={<Launcher />} />
          <Route path="/queues"    element={<Queues />} />
          <Route path="/logs"      element={<Logs />} />
          <Route path="/metrics"   element={<Metrics />} />
          <Route path="/alerts"    element={<Alerts />} />
        </Route>
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </DensityProvider>
  )
}
