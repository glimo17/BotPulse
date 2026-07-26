import React, { createContext, useContext, useState } from 'react'

type Density = 'comfortable' | 'compact'

interface DensityContextType {
  density: Density
  toggleDensity: () => void
  rowHeight: number
  cellPadding: string
}

const DensityContext = createContext<DensityContextType>({
  density: 'comfortable',
  toggleDensity: () => {},
  rowHeight: 52,
  cellPadding: 'px-4 py-3',
})

export function DensityProvider({ children }: { children: React.ReactNode }) {
  const [density, setDensity] = useState<Density>(
    () => (localStorage.getItem('botpulse-density') as Density) || 'comfortable'
  )

  const toggleDensity = () => {
    setDensity(prev => {
      const next = prev === 'comfortable' ? 'compact' : 'comfortable'
      localStorage.setItem('botpulse-density', next)
      return next
    })
  }

  return (
    <DensityContext.Provider value={{
      density,
      toggleDensity,
      rowHeight: density === 'compact' ? 36 : 52,
      cellPadding: density === 'compact' ? 'px-3 py-1' : 'px-4 py-3',
    }}>
      {children}
    </DensityContext.Provider>
  )
}

export const useDensity = () => useContext(DensityContext)
