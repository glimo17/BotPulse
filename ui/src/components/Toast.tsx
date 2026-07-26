import { useState, useEffect, useCallback } from 'react'
import { AlertOctagon, X } from 'lucide-react'
import { clsx } from 'clsx'

export interface ToastMessage {
  id: string
  message: string
  severity: 'Critical' | 'Warning' | 'Info'
}

interface ToastProps { toast: ToastMessage; onDismiss: (id: string) => void }

function Toast({ toast, onDismiss }: ToastProps) {
  useEffect(() => {
    const timer = setTimeout(() => onDismiss(toast.id), 5000)
    return () => clearTimeout(timer)
  }, [toast.id, onDismiss])

  return (
    <div className={clsx(
      'flex items-start gap-3 px-4 py-3 rounded-lg border shadow-lg text-sm max-w-sm',
      toast.severity === 'Critical' ? 'bg-error/10 border-error/30' :
      toast.severity === 'Warning'  ? 'bg-warning/10 border-warning/30' :
                                      'bg-accent/10 border-accent/30'
    )}>
      <AlertOctagon size={15} className={clsx('mt-0.5 shrink-0',
        toast.severity === 'Critical' ? 'text-error' :
        toast.severity === 'Warning'  ? 'text-warning' : 'text-accent'
      )} />
      <p className="flex-1 text-gray-200">{toast.message}</p>
      <button onClick={() => onDismiss(toast.id)} className="text-gray-500 hover:text-gray-300 shrink-0">
        <X size={13} />
      </button>
    </div>
  )
}

interface ContainerProps { toasts: ToastMessage[]; onDismiss: (id: string) => void }

export function ToastContainer({ toasts, onDismiss }: ContainerProps) {
  if (toasts.length === 0) return null
  return (
    <div className="fixed bottom-4 right-4 flex flex-col gap-2 z-50">
      {toasts.map(t => <Toast key={t.id} toast={t} onDismiss={onDismiss} />)}
    </div>
  )
}

export function useToast() {
  const [toasts, setToasts] = useState<ToastMessage[]>([])

  const addToast = useCallback((message: string, severity: ToastMessage['severity'] = 'Info') => {
    const id = Math.random().toString(36).slice(2)
    setToasts(prev => [...prev, { id, message, severity }])
  }, [])

  const dismiss = useCallback((id: string) => {
    setToasts(prev => prev.filter(t => t.id !== id))
  }, [])

  return { toasts, addToast, dismiss }
}
