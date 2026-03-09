import { Link, Outlet } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export function Layout() {
  const { user, logout } = useAuth()

  return (
    <>
      <header className="flex items-center justify-between p-4 bg-gray-800 text-white">
        <nav>
          <Link to="/">RCV</Link>
        </nav>
        <div>
          {user ? (
            <>
              <span>{user.displayName}</span>
              <button onClick={logout}>Logout</button>
            </>
          ) : (
            <Link to="/login">Login</Link>
          )}
        </div>
      </header>
      <main><Outlet /></main>
    </>
  )
}
