import { Dialog, DialogBackdrop, DialogPanel, DialogTitle } from '@headlessui/react'
import {
  ArrowPathIcon,
  Bars3Icon,
  CheckCircleIcon,
  ExclamationTriangleIcon,
  XMarkIcon,
} from '@heroicons/react/24/outline'
import { useState, type ReactNode } from 'react'
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { errorMessage } from './api'
import { useAuth } from './auth'

export function AppShell() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const [signingOut, setSigningOut] = useState(false)
  const [signOutError, setSignOutError] = useState<string | null>(null)

  const nav = user
    ? [
        { to: '/dashboard', label: 'My polls' },
        { to: '/polls/new', label: 'Create poll' },
      ]
    : [
        { to: '/', label: 'How it works' },
        { to: '/login', label: 'Sign in' },
      ]

  const signOut = async () => {
    setOpen(false)
    setSigningOut(true)
    setSignOutError(null)
    try {
      await logout()
      navigate('/')
    } catch (error) {
      setSignOutError(errorMessage(error))
    } finally {
      setSigningOut(false)
    }
  }

  return (
    <div className="flex min-h-screen flex-col">
      <a
        href="#main-content"
        className="fixed left-4 top-4 z-50 -translate-y-24 rounded-lg bg-ink-950 px-4 py-2 text-white focus:translate-y-0"
      >
        Skip to content
      </a>
      <header className="border-b bg-white">
        <nav className="container-page flex min-h-18 items-center justify-between" aria-label="Main navigation">
          <Link to="/" className="flex items-center gap-3 text-xl font-bold tracking-tight">
            <span
              aria-hidden="true"
              className="grid size-9 place-items-center rounded-xl bg-sage-700 text-lg text-white"
            >
              1
            </span>
            Ranked
          </Link>
          <button
            className="rounded-lg p-2 sm:hidden"
            type="button"
            aria-label="Toggle navigation"
            aria-expanded={open}
            onClick={() => setOpen((value) => !value)}
          >
            {open ? <XMarkIcon className="size-6" /> : <Bars3Icon className="size-6" />}
          </button>
          <div className="hidden items-center gap-1 sm:flex">
            {nav.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  `rounded-lg px-3 py-2 font-semibold ${isActive ? 'bg-sage-100 text-sage-700' : 'hover:bg-cream-100'}`
                }
              >
                {item.label}
              </NavLink>
            ))}
            {user && (
              <>
                <span className="ml-3 text-sm text-ink-700">{user.displayName ?? user.email ?? 'Signed in'}</span>
                <button type="button" disabled={signingOut} className="ml-2 rounded-lg px-3 py-2 font-semibold hover:bg-cream-100 disabled:opacity-50" onClick={signOut}>
                  {signingOut ? 'Signing out…' : 'Sign out'}
                </button>
              </>
            )}
          </div>
        </nav>
        {open && (
          <div className="container-page grid gap-1 border-t py-3 sm:hidden">
            {nav.map((item) => (
              <NavLink key={item.to} to={item.to} className="rounded-lg px-3 py-3 font-semibold" onClick={() => setOpen(false)}>
                {item.label}
              </NavLink>
            ))}
            {user && (
              <button type="button" disabled={signingOut} className="rounded-lg px-3 py-3 text-left font-semibold disabled:opacity-50" onClick={signOut}>
                {signingOut ? 'Signing out…' : 'Sign out'}
              </button>
            )}
          </div>
        )}
        {signOutError && <div className="container-page pb-3"><Alert tone="error">{signOutError}</Alert></div>}
      </header>
      <main id="main-content" className="flex-1">
        <Outlet />
      </main>
      <footer className="mt-16 border-t bg-white py-8 text-center text-sm text-ink-500">
        Make every preference count.
      </footer>
    </div>
  )
}

export function PageHeader({
  eyebrow,
  title,
  description,
  actions,
}: {
  eyebrow?: string
  title: string
  description?: string
  actions?: ReactNode
}) {
  return (
    <header className="mb-8 flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
      <div>
        {eyebrow && <p className="mb-2 font-bold uppercase tracking-widest text-sage-700">{eyebrow}</p>}
        <h1 className="text-3xl font-bold tracking-tight sm:text-4xl">{title}</h1>
        {description && <p className="mt-3 max-w-2xl text-lg text-ink-700">{description}</p>}
      </div>
      {actions && <div className="flex flex-wrap gap-3">{actions}</div>}
    </header>
  )
}

export function LoadingState({ label = 'Loading…' }: { label?: string }) {
  return (
    <div className="container-page grid min-h-64 place-items-center" role="status">
      <div className="flex items-center gap-3 font-semibold text-ink-700">
        <ArrowPathIcon className="size-6 animate-spin" aria-hidden="true" />
        {label}
      </div>
    </div>
  )
}

export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  return (
    <div className="container-page py-12">
      <div className="card mx-auto max-w-xl border-red-200 bg-red-50" role="alert">
        <ExclamationTriangleIcon className="mb-3 size-8 text-red-700" aria-hidden="true" />
        <h2 className="text-xl font-bold">We couldn’t load this</h2>
        <p className="mt-2 text-red-900">{errorMessage(error)}</p>
        {onRetry && (
          <button type="button" className="btn-secondary mt-5" onClick={onRetry}>
            Try again
          </button>
        )}
      </div>
    </div>
  )
}

export function Alert({ children, tone = 'success' }: { children: ReactNode; tone?: 'success' | 'error' | 'info' }) {
  const styles = tone === 'error' ? 'border-red-200 bg-red-50 text-red-900' : tone === 'info' ? 'border-blue-200 bg-blue-50 text-blue-950' : 'border-green-200 bg-green-50 text-green-950'
  return (
    <div className={`flex gap-3 rounded-xl border p-4 ${styles}`} role={tone === 'error' ? 'alert' : 'status'}>
      {tone === 'success' && <CheckCircleIcon className="size-6 shrink-0" aria-hidden="true" />}
      <div>{children}</div>
    </div>
  )
}

export function ConfirmDialog({
  open,
  title,
  description,
  confirmLabel,
  destructive = false,
  busy = false,
  onClose,
  onConfirm,
}: {
  open: boolean
  title: string
  description: string
  confirmLabel: string
  destructive?: boolean
  busy?: boolean
  onClose: () => void
  onConfirm: () => void
}) {
  return (
    <Dialog open={open} onClose={busy ? () => undefined : onClose} className="relative z-50">
      <DialogBackdrop className="fixed inset-0 bg-ink-950/50" />
      <div className="fixed inset-0 grid place-items-center overflow-y-auto p-4">
        <DialogPanel className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl">
          <DialogTitle className="text-xl font-bold">{title}</DialogTitle>
          <p className="mt-3 text-ink-700">{description}</p>
          <div className="mt-6 flex justify-end gap-3">
            <button type="button" className="btn-secondary" disabled={busy} onClick={onClose}>Cancel</button>
            <button type="button" className={destructive ? 'btn-danger' : 'btn-primary'} disabled={busy} onClick={onConfirm}>
              {busy ? 'Working…' : confirmLabel}
            </button>
          </div>
        </DialogPanel>
      </div>
    </Dialog>
  )
}

export function FieldError({ message }: { message?: string }) {
  if (!message) return null
  return <p className="mt-1 text-sm font-medium text-red-700">{message}</p>
}

export function EmptyState({ title, description, action }: { title: string; description: string; action?: ReactNode }) {
  return (
    <div className="rounded-2xl border border-dashed bg-white px-6 py-14 text-center">
      <h2 className="text-xl font-bold">{title}</h2>
      <p className="mx-auto mt-2 max-w-md text-ink-700">{description}</p>
      {action && <div className="mt-5">{action}</div>}
    </div>
  )
}
