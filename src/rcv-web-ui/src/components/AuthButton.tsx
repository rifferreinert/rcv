import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export function AuthButton() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  if (!user) {
    return (
      <Link
        to="/login"
        className="text-sm font-medium underline hover:no-underline"
      >
        Login
      </Link>
    )
  }

  const handleLogout = async () => {
    await logout()
    navigate('/')
  }

  return (
    <span className="flex items-center gap-2 text-sm">
      <span className="font-medium">{user.displayName}</span>
      <span aria-hidden="true">|</span>
      <button
        onClick={handleLogout}
        className="underline hover:no-underline cursor-pointer border-none bg-transparent p-0"
      >
        Logout
      </button>
    </span>
  )
}
