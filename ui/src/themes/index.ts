export type ThemeId = 'dark' | 'light' | 'ocean' | 'pink'

export interface ThemeMeta {
  id: ThemeId
  labelKey: string  // i18n key
  previewColor: string
}

export const THEMES: ThemeMeta[] = [
  { id: 'dark',  labelKey: 'theme.dark',  previewColor: '#181b1f' },
  { id: 'light', labelKey: 'theme.light', previewColor: '#ffffff' },
  { id: 'ocean', labelKey: 'theme.ocean', previewColor: '#0369a1' },
  { id: 'pink',  labelKey: 'theme.pink',  previewColor: '#e91e8c' },
]

const STORAGE_KEY = 'botpulse-theme'

export function getStoredTheme(): ThemeId {
  const stored = localStorage.getItem(STORAGE_KEY)
  if (stored && THEMES.some(t => t.id === stored)) return stored as ThemeId
  return 'dark'
}

export function setTheme(theme: ThemeId): void {
  document.documentElement.setAttribute('data-theme', theme)
  localStorage.setItem(STORAGE_KEY, theme)
}

export function initTheme(): void {
  const theme = getStoredTheme()
  document.documentElement.setAttribute('data-theme', theme)
}
