import { Link, Outlet } from 'react-router-dom'
import { AuthButton } from './AuthButton'

export function Layout() {
  return (
    <>
      <header className="flex items-center justify-between p-4 bg-gray-800 text-white">
        <nav>
          <Link to="/">RCV</Link>
        </nav>
        <AuthButton />
      </header>
      <main><Outlet /></main>
    </>
  )
}
