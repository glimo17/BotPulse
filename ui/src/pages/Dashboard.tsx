import React from 'react'
import { useTranslation } from 'react-i18next'
import { Bot, Briefcase, ListOrdered, Bell } from 'lucide-react'

const KPICard = ({ label, value, sub, icon: Icon, color }: {
  label: string; value: string | number; sub?: string
  icon: React.ElementType; color: string
}) => (
  <div className="card p-5 flex items-start gap-4">
    <div className={`w-10 h-10 rounded-lg flex items-center justify-center shrink-0 ${color}`}>
      <Icon size={20} className="text-white" />
    </div>
    <div className="min-w-0">
      <p className="text-2xl font-bold text-gray-100">{value}</p>
      <p className="text-sm text-gray-400 mt-0.5">{label}</p>
      {sub && <p className="text-xs text-gray-500 mt-1">{sub}</p>}
    </div>
  </div>
)

export default function Dashboard() {
  const { t } = useTranslation()
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl font-bold text-gray-100">{t('nav.dashboard')}</h1>
        <p className="text-sm text-gray-500 mt-1">Plataforma de operaciones RPA</p>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <KPICard label="Robots" value="—" sub="Cargando datos..." icon={Bot} color="bg-accent" />
        <KPICard label="Jobs hoy" value="—" sub="Cargando datos..." icon={Briefcase} color="bg-success" />
        <KPICard label="Queue backlog" value="—" sub="Cargando datos..." icon={ListOrdered} color="bg-warning" />
        <KPICard label="Alertas activas" value="—" sub="Cargando datos..." icon={Bell} color="bg-error" />
      </div>

      <div className="card p-6 text-center text-gray-500">
        <p className="text-sm">Dashboard con datos reales en la siguiente tarea</p>
      </div>
    </div>
  )
}
