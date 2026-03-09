import { Link } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export function AuthButton() {
  const { user, logout } = useAuth()

  if (!user) return <Link to="/login">Login</Link>

  return (
    <>
      <span>{user.displayName}</span>
      <button onClick={logout}>Logout</button>
    </>
  )
}
