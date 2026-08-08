import { useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router-dom'
import { pollsApi, type PollStatusFilter } from '../api'
import { EmptyState, ErrorState, LoadingState, PageHeader } from '../components'
import { formatDate } from '../utils'

const PAGE_SIZE = 8
const statuses: { value: PollStatusFilter; label: string }[] = [
  { value: 'all', label: 'All' },
  { value: 'active', label: 'Open' },
  { value: 'closed', label: 'Closed' },
]

export function DashboardPage() {
  const [params, setParams] = useSearchParams()
  const rawStatus = params.get('status')
  const status: PollStatusFilter = rawStatus === 'active' || rawStatus === 'closed' ? rawStatus : 'all'
  const page = Math.max(1, Number(params.get('page')) || 1)
  const query = useQuery({
    queryKey: ['polls', status, page],
    queryFn: () => pollsApi.list({ status, page, pageSize: PAGE_SIZE }),
  })

  const change = (nextStatus: PollStatusFilter, nextPage = 1) => {
    const next = new URLSearchParams()
    if (nextStatus !== 'all') next.set('status', nextStatus)
    if (nextPage > 1) next.set('page', String(nextPage))
    setParams(next)
  }

  if (query.isLoading) return <LoadingState label="Loading your polls…" />
  if (query.error) return <ErrorState error={query.error} onRetry={() => void query.refetch()} />

  const result = query.data!
  const totalPages = Math.max(1, Math.ceil(result.totalCount / result.pageSize))
  return (
    <div className="container-page py-10">
      <PageHeader
        eyebrow="Dashboard"
        title="Your polls"
        description="Manage voting, share direct links, and follow results."
        actions={<Link className="btn-primary" to="/polls/new">Create poll</Link>}
      />
      <div className="mb-6 flex gap-2 overflow-x-auto" role="group" aria-label="Filter polls">
        {statuses.map((item) => (
          <button
            key={item.value}
            type="button"
            aria-pressed={status === item.value}
            className={`rounded-full px-4 py-2 font-semibold ${status === item.value ? 'bg-ink-950 text-white' : 'border bg-white'}`}
            onClick={() => change(item.value)}
          >
            {item.label}
          </button>
        ))}
      </div>
      {result.items.length === 0 ? (
        <EmptyState
          title={status === 'all' ? 'Create your first poll' : `No ${status} polls`}
          description="Polls you create will appear here. Only people with a direct link can find them."
          action={status === 'all' ? <Link className="btn-primary" to="/polls/new">Create poll</Link> : undefined}
        />
      ) : (
        <div className="grid gap-4">
          {result.items.map((poll) => (
            <article key={poll.id} className="card flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <div className="flex flex-wrap items-center gap-3">
                  <h2 className="text-xl font-bold"><Link className="hover:underline" to={`/polls/${poll.id}`}>{poll.title}</Link></h2>
                  <span className={`rounded-full px-2.5 py-1 text-xs font-bold uppercase ${poll.status === 'Active' ? 'bg-green-100 text-green-800' : 'bg-ink-950/10'}`}>
                    {poll.status === 'Active' ? 'Open' : 'Closed'}
                  </span>
                </div>
                <p className="mt-2 text-sm text-ink-500">{poll.options.length} choices · {poll.status === 'Closed' ? `Closed ${formatDate(poll.closedAt)}` : `Closes ${formatDate(poll.closesAt)}`}</p>
              </div>
              <div className="flex gap-3">
                {poll.status === 'Active' && <Link className="btn-secondary" to={`/polls/${poll.id}/edit`}>Edit</Link>}
                <Link className="btn-secondary" to={`/polls/${poll.id}/results`}>Results</Link>
              </div>
            </article>
          ))}
        </div>
      )}
      {totalPages > 1 && (
        <nav className="mt-8 flex items-center justify-center gap-4" aria-label="Poll pages">
          <button className="btn-secondary" type="button" disabled={page <= 1} onClick={() => change(status, page - 1)}>Previous</button>
          <span aria-live="polite">Page {result.page} of {totalPages}</span>
          <button className="btn-secondary" type="button" disabled={page >= totalPages} onClick={() => change(status, page + 1)}>Next</button>
        </nav>
      )}
    </div>
  )
}
