import { Link } from 'react-router-dom'

export function NotFoundPage() {
  return (
    <div className="container-page grid min-h-[60vh] place-items-center py-16 text-center">
      <div>
        <p className="font-bold uppercase tracking-widest text-sage-700">404</p>
        <h1 className="mt-2 text-4xl font-bold">Page not found</h1>
        <p className="mt-3 text-lg text-ink-700">The page may have moved, or the poll link may be incomplete.</p>
        <Link className="btn-primary mt-7" to="/">Go home</Link>
      </div>
    </div>
  )
}
