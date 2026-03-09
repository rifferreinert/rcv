import { useAuth } from '../context/AuthContext'

export function AuthButton() {
  const { user, logout } = useAuth()

  if (!user) return null

  return (
    <>
      <span>{user.displayName}</span>
      <button onClick={logout}>Logout</button>
    </>
  )
}
