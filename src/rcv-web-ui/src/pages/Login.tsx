import { useAuth } from '../context/AuthContext'

function GoogleIcon() {
  return (
    <svg
      width="18"
      height="18"
      viewBox="0 0 18 18"
      aria-hidden="true"
      focusable="false"
    >
      <path
        d="M17.64 9.2c0-.637-.057-1.251-.164-1.84H9v3.481h4.844a4.14 4.14 0 0 1-1.796 2.716v2.259h2.908C16.658 14.093 17.64 11.785 17.64 9.2Z"
        fill="#4285F4"
      />
      <path
        d="M9 18c2.43 0 4.467-.806 5.956-2.18l-2.908-2.259c-.806.54-1.837.86-3.048.86-2.344 0-4.328-1.584-5.036-3.711H.957v2.332A8.997 8.997 0 0 0 9 18Z"
        fill="#34A853"
      />
      <path
        d="M3.964 10.71A5.41 5.41 0 0 1 3.682 9c0-.593.102-1.17.282-1.71V4.958H.957A8.996 8.996 0 0 0 0 9c0 1.452.348 2.827.957 4.042l3.007-2.332Z"
        fill="#FBBC05"
      />
      <path
        d="M9 3.58c1.321 0 2.508.454 3.44 1.345l2.582-2.58C13.463.891 11.426 0 9 0A8.997 8.997 0 0 0 .957 4.958L3.964 6.29C4.672 4.163 6.656 3.58 9 3.58Z"
        fill="#EA4335"
      />
    </svg>
  )
}

function MicrosoftIcon() {
  return (
    <svg
      width="18"
      height="18"
      viewBox="0 0 18 18"
      aria-hidden="true"
      focusable="false"
    >
      <rect x="0"   y="0"   width="8.5" height="8.5" fill="#F25022" />
      <rect x="9.5" y="0"   width="8.5" height="8.5" fill="#7FBA00" />
      <rect x="0"   y="9.5" width="8.5" height="8.5" fill="#00A4EF" />
      <rect x="9.5" y="9.5" width="8.5" height="8.5" fill="#FFB900" />
    </svg>
  )
}

interface ProviderButtonProps {
  icon: React.ReactNode
  label: string
  onClick: () => void
}

function ProviderButton({ icon, label, onClick }: ProviderButtonProps) {
  return (
    <button
      onClick={onClick}
      className="flex items-center justify-center gap-3 w-full px-4 py-3 border border-gray-300 rounded-lg bg-white text-gray-700 font-medium text-sm hover:bg-gray-50 hover:border-gray-400 transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
    >
      {icon}
      {label}
    </button>
  )
}

export default function Login() {
  const { login } = useAuth()

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="w-full max-w-md mx-auto p-8 bg-white rounded-xl shadow-sm border border-gray-100">
        <div className="mb-8 text-center">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">Sign in to RCV</h1>
          <p className="text-sm text-gray-500">Choose your authentication provider</p>
        </div>

        <div className="flex flex-col gap-3">
          <ProviderButton
            icon={<GoogleIcon />}
            label="Sign in with Google"
            onClick={() => login('google')}
          />
          <ProviderButton
            icon={<MicrosoftIcon />}
            label="Sign in with Microsoft"
            onClick={() => login('microsoft')}
          />
        </div>
      </div>
    </div>
  )
}
