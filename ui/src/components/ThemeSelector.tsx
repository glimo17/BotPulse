import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Palette, Check } from 'lucide-react'
import { THEMES, getStoredTheme, setTheme } from '@/themes'
import type { ThemeId } from '@/themes'

export function ThemeSelector() {
  const { t } = useTranslation()
  const [current, setCurrent] = useState<ThemeId>(getStoredTheme())
  const [open, setOpen] = useState(false)

  const handleSelect = (id: ThemeId) => {
    setTheme(id)
    setCurrent(id)
    setOpen(false)
  }

  return (
    <div className="relative">
      <button
        onClick={() => setOpen(v => !v)}
        className="flex items-center gap-1 px-2 py-1.5 text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] rounded-md text-xs transition-colors"
        title={t('theme.select')}
      >
        <Palette size={13} />
      </button>

      {open && (
        <>
          <div className="fixed inset-0 z-40" onClick={() => setOpen(false)} />
          <div className="absolute right-0 top-full mt-1 w-40 bg-[var(--color-bg-card)] border border-[var(--color-border)] rounded-lg shadow-xl z-50 py-1">
            {THEMES.map(theme => (
              <button
                key={theme.id}
                onClick={() => handleSelect(theme.id)}
                className="w-full flex items-center gap-2 px-3 py-2 text-xs text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition-colors"
              >
                <span
                  className="w-3 h-3 rounded-full border border-[var(--color-border)]"
                  style={{ backgroundColor: theme.previewColor }}
                />
                <span className="flex-1 text-left">{t(theme.labelKey)}</span>
                {current === theme.id && <Check size={12} className="text-[var(--color-accent)]" />}
              </button>
            ))}
          </div>
        </>
      )}
    </div>
  )
}
