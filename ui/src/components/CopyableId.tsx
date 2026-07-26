import { useState } from 'react'
import { Copy, Check } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { clsx } from 'clsx'

interface Props {
  id: string
  maxLength?: number
  className?: string
}

export function CopyableId({ id, maxLength = 12, className }: Props) {
  const { t } = useTranslation()
  const [copied, setCopied] = useState(false)

  const handleCopy = async (e: React.MouseEvent) => {
    e.stopPropagation()
    await navigator.clipboard.writeText(id)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  const display = id.length > maxLength ? `${id.substring(0, maxLength)}…` : id

  return (
    <button
      onClick={handleCopy}
      className={clsx(
        'group inline-flex items-center gap-1.5 font-mono text-xs text-gray-400 hover:text-gray-200 transition-colors',
        className
      )}
      title={copied ? t('common.copied') : t('common.copyId')}
    >
      <span>{display}</span>
      {copied ? (
        <Check size={11} className="text-success shrink-0" />
      ) : (
        <Copy size={11} className="opacity-0 group-hover:opacity-100 shrink-0 transition-opacity" />
      )}
    </button>
  )
}
