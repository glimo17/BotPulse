import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/contexts/AuthContext'
import { ActivitySquare, Globe, Eye, EyeOff } from 'lucide-react'
import i18n from '@/i18n'

export default function Login() {
  const { t } = useTranslation()
  const { login } = useAuth()
  const navigate = useNavigate()
  const [username, setUsername] = useState(() => localStorage.getItem('botpulse-remember-user') ?? '')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [remember, setRemember] = useState(() => !!localStorage.getItem('botpulse-remember-user'))
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [lang, setLang] = useState(i18n.language)

  const toggleLang = () => {
    const next = lang === 'es' ? 'en' : 'es'
    i18n.changeLanguage(next)
    localStorage.setItem('botpulse-lang', next)
    setLang(next)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await login(username, password)
      if (remember) {
        localStorage.setItem('botpulse-remember-user', username)
      } else {
        localStorage.removeItem('botpulse-remember-user')
      }
      navigate('/dashboard')
    } catch {
      setError(t('auth.invalidCredentials'))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-[var(--color-bg-primary)] flex items-center justify-center p-4">
      {/* Language toggle */}
      <button
        onClick={toggleLang}
        className="fixed top-4 right-4 flex items-center gap-1.5 text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] text-sm transition-colors"
      >
        <Globe size={14} />
        {lang.toUpperCase()}
      </button>

      <div className="w-full max-w-sm">
        {/* Logo */}
        <div className="flex items-center justify-center gap-3 mb-8">
          <div className="w-10 h-10 rounded-lg bg-accent flex items-center justify-center">
            <ActivitySquare size={22} className="text-white" />
          </div>
          <div>
            <h1 className="text-xl font-bold text-white leading-tight">BotPulse</h1>
            <p className="text-xs text-[var(--color-text-secondary)]">{t('auth.tagline')}</p>
          </div>
        </div>

        {/* Form card */}
        <div className="card p-6">
          <h2 className="text-lg font-semibold text-[var(--color-text-primary)] mb-5">{t('auth.login')}</h2>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm text-[var(--color-text-secondary)] mb-1.5">{t('auth.username')}</label>
              <input
                type="text"
                value={username}
                onChange={e => setUsername(e.target.value)}
                required
                autoFocus
                className="w-full bg-[var(--color-bg-hover)] border border-[var(--color-border)] rounded-md px-3 py-2 text-[var(--color-text-primary)] text-sm placeholder-[var(--color-text-muted)] focus:outline-none focus:border-accent focus:ring-1 focus:ring-accent transition-colors"
                placeholder="admin"
              />
            </div>
            <div>
              <label className="block text-sm text-[var(--color-text-secondary)] mb-1.5">{t('auth.password')}</label>
              <div className="relative">
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  required
                  className="w-full bg-[var(--color-bg-hover)] border border-[var(--color-border)] rounded-md px-3 py-2 pr-10 text-[var(--color-text-primary)] text-sm focus:outline-none focus:border-accent focus:ring-1 focus:ring-accent transition-colors"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(v => !v)}
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] transition-colors"
                  tabIndex={-1}
                >
                  {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              </div>
            </div>

            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="remember"
                checked={remember}
                onChange={e => setRemember(e.target.checked)}
                className="w-3.5 h-3.5 rounded border-[var(--color-border)] bg-[var(--color-bg-hover)] text-[var(--color-accent)] focus:ring-[var(--color-accent)] focus:ring-1"
              />
              <label htmlFor="remember" className="text-xs text-[var(--color-text-secondary)] cursor-pointer select-none">
                {t('auth.rememberMe')}
              </label>
            </div>

            {error && (
              <div className="text-error text-sm bg-error/10 border border-error/20 rounded-md px-3 py-2">
                {error}
              </div>
            )}

            <button
              type="submit"
              disabled={loading}
              className="w-full bg-accent hover:bg-accent-hover disabled:opacity-50 disabled:cursor-not-allowed text-white font-medium py-2 px-4 rounded-md text-sm transition-colors flex items-center justify-center gap-2"
            >
              {loading ? (
                <>
                  <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                  {t('auth.loggingIn')}
                </>
              ) : (
                t('auth.loginButton')
              )}
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}
