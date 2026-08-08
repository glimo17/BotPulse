import { useAuth } from '@/contexts/AuthContext'

interface PermissionGateProps {
  /** Single permission required */
  permission?: string
  /** Any one of these permissions is sufficient */
  anyOf?: string[]
  children: React.ReactNode
  /** Rendered when the user lacks the required permission. Defaults to null. */
  fallback?: React.ReactNode
}

/**
 * Renders children only when the current user has the required permission(s).
 * This is a UX optimization — it does NOT replace server-side enforcement.
 *
 * Usage:
 *   <PermissionGate permission="Roles.Update">
 *     <button>Create Role</button>
 *   </PermissionGate>
 *
 *   <PermissionGate anyOf={['Users.Create', 'Users.Update']}>
 *     ...
 *   </PermissionGate>
 */
export function PermissionGate({ permission, anyOf, children, fallback = null }: PermissionGateProps) {
  const { hasPermission, hasAnyPermission } = useAuth()

  const allowed = permission
    ? hasPermission(permission)
    : anyOf
      ? hasAnyPermission(...anyOf)
      : true

  return allowed ? <>{children}</> : <>{fallback}</>
}
