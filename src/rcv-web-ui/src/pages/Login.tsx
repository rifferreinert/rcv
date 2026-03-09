import { useAuth } from '../context/AuthContext'

export default function Login() {
  const { login } = useAuth()

  return (
    <div className="max-w-md mx-auto p-8">
      <h1 className="text-2xl font-bold mb-6">Sign In</h1>
      <button onClick={() => login('google')}>Sign in with Google</button>
      <button onClick={() => login('microsoft')}>Sign in with Microsoft</button>
    </div>
  )
}
