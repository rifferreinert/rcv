import { useParams } from 'react-router-dom'

export default function PollDetail() {
  const { id } = useParams<{ id: string }>()
  return (
    <div className="max-w-4xl mx-auto p-8">
      <h1 className="text-2xl font-bold mb-6">Poll Detail</h1>
      <p className="text-gray-600">Poll ID: {id}</p>
    </div>
  )
}
