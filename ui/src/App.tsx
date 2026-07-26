import { Routes, Route, Navigate } from 'react-router-dom'
import { useState } from 'react'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { AppLayout } from '@/components/layout/AppLayout'
import { DensityProvider } from '@/contexts/DensityContext'
import Login from '@/pages/Login'
import Dashboard from '@/pages/Dashboard'

// Placeholder pages
const Placeholder = ({ name }: { name: string }) => (
  <div className="card p-8 text-center">
    <p className="text-gray-400 text-sm">{name} — próximamente</p>
  </div>
)

export default function App() {
  const [_cmdOpen, setCmdOpen] = useState(false)

  return (
    <DensityProvider>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route element={
          <ProtectedRoute>
            <AppLayout onCommandPaletteOpen={() => setCmdOpen(true)} />
          </ProtectedRoute>
        }>
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/robots"    element={<Placeholder name="Robots" />} />
          <Route path="/machines"  element={<Placeholder name="Machines" />} />
          <Route path="/processes" element={<Placeholder name="Processes" />} />
          <Route path="/jobs"      element={<Placeholder name="Jobs" />} />
          <Route path="/queues"    element={<Placeholder name="Queues" />} />
          <Route path="/logs"      element={<Placeholder name="Logs" />} />
          <Route path="/metrics"   element={<Placeholder name="Metrics" />} />
          <Route path="/alerts"    element={<Placeholder name="Alerts" />} />
        </Route>
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </DensityProvider>
  )
}
