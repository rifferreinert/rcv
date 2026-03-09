import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export function AuthButton() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  if (!user) return <Link to="/login">Login</Link>

  const handleLogout = async () => {
    await logout()
    navigate('/')
  }

  return (
    <>
      <span>{user.displayName}</span>
      <button onClick={handleLogout}>Logout</button>
    </>
  )
}
