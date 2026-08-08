import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../auth'
import type { Provider } from '../types'

export function LoginPage() {
  const { user, login, error } = useAuth()
  const location = useLocation()
  const state = location.state as { returnTo?: string } | null

  if (user) return <Navigate to={state?.returnTo ?? '/dashboard'} replace />

  const providerButton = (provider: Provider, label: string) => (
    <button type="button" className="btn-secondary w-full justify-center" onClick={() => login(provider, state?.returnTo)}>
      Continue with {label}
    </button>
  )

  return (
    <div className="container-page py-16">
      <section className="card mx-auto max-w-md p-7 sm:p-9" aria-labelledby="login-heading">
        <p className="font-bold uppercase tracking-widest text-sage-700">Welcome</p>
        <h1 id="login-heading" className="mt-2 text-3xl font-bold">Sign in to Ranked</h1>
        <p className="mt-3 text-ink-700">Use a trusted account to create polls and cast or revise your ballot.</p>
        {error && <p className="mt-5 rounded-xl bg-red-50 p-3 text-red-800" role="alert">{error.message}</p>}
        <div className="mt-7 grid gap-3">
          {providerButton('google', 'Google')}
          {providerButton('microsoft', 'Microsoft')}
        </div>
        <p className="mt-6 text-sm text-ink-500">Authentication is handled securely. This app never sees your provider password.</p>
      </section>
    </div>
  )
}
