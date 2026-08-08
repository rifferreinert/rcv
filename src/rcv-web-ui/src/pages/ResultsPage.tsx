import { TrophyIcon } from '@heroicons/react/24/outline'
import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { Alert, EmptyState, ErrorState, LoadingState, PageHeader } from '../components'
import { pollsApi } from '../api'
import { formatDate, resultsPollingInterval } from '../utils'

export function ResultsPage() {
  const { id = '' } = useParams()
  const [selectedRound, setSelectedRound] = useState<number | null>(null)
  const query = useQuery({
    queryKey: ['results', id],
    queryFn: async () => {
      const [poll, results] = await Promise.all([
        pollsApi.get(id),
        pollsApi.results(id),
      ])
      return { poll, results }
    },
    enabled: Boolean(id),
    refetchInterval: ({ state }) =>
      resultsPollingInterval(state.data?.results.state, state.data?.poll.status),
  })

  if (query.isLoading) return <LoadingState label="Counting votes…" />
  if (query.error) return <ErrorState error={query.error} onRetry={() => void query.refetch()} />
  const { poll, results } = query.data!
  const roundIndex = selectedRound === null ? Math.max(0, results.rounds.length - 1) : Math.min(selectedRound, results.rounds.length - 1)
  const round = results.rounds[roundIndex]
  const activeVotes = round ? Object.values(round.voteCounts).reduce((sum, votes) => sum + votes, 0) : 0
  const eliminatedIds = new Set(results.rounds.slice(0, roundIndex + 1).map(({ eliminatedOptionId }) => eliminatedOptionId).filter(Boolean))
  const roundOptions = round
    ? poll.options.map((option) => {
        const votes = round.voteCounts[option.id] ?? 0
        return {
          ...option,
          votes,
          percentage: activeVotes === 0 ? 0 : votes / activeVotes * 100,
          status: round.eliminatedOptionId === option.id ? 'Eliminated this round' : eliminatedIds.has(option.id) ? 'Eliminated' : 'Active',
        }
      })
    : []

  return (
    <div className="container-page py-10">
      <PageHeader
        eyebrow={results.state === 'Final' ? 'Final results' : results.state === 'InProgress' ? 'Live results' : 'Results'}
        title={poll.title}
        description={`${results.totalVotes} ${results.totalVotes === 1 ? 'ballot' : 'ballots'} cast${poll.closedAt ? ` · Closed ${formatDate(poll.closedAt)}` : ` · Counted ${formatDate(results.calculatedAt)}`}`}
        actions={<Link className="btn-secondary" to={`/polls/${id}`}>Back to poll</Link>}
      />
      {results.state === 'NoVotes' ? (
        <EmptyState title="No votes yet" description={poll.status === 'Closed' ? 'This poll closed without any ballots.' : 'Results will appear here after the first ballot is submitted.'} />
      ) : (
        <div className="grid gap-6">
          {results.state === 'InProgress' && <Alert tone="info"><strong>Live count:</strong> These results update while voting is open and may change as ballots are submitted or revised.</Alert>}
          {results.state === 'Final' && results.isTie && (
            <section className="card border-amber-300 bg-amber-50 text-center">
              <h2 className="text-2xl font-bold">The result is a tie</h2>
              <p className="mt-2 text-ink-700">{results.tiedOptions.map(({ text }) => text).join(' and ')} finished level after the final round.</p>
            </section>
          )}
          {results.state === 'Final' && results.winner && !results.isTie && (
            <section className="card border-sage-500 bg-sage-100 text-center">
              <TrophyIcon className="mx-auto size-10 text-sage-700" aria-hidden="true" />
              <p className="mt-3 text-sm font-bold uppercase tracking-widest text-sage-700">Winner</p>
              <h2 className="mt-1 text-3xl font-bold">{results.winner.text}</h2>
            </section>
          )}
          {round && (
            <section className="card" aria-labelledby="round-heading">
              <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
                <div>
                  <h2 id="round-heading" className="text-2xl font-bold">Round {round.roundNumber}</h2>
                  <p className="mt-1 text-ink-500">{activeVotes} active · {Math.max(0, results.totalVotes - activeVotes)} exhausted</p>
                </div>
                {results.rounds.length > 1 && (
                  <div>
                    <label className="label" htmlFor="round">View round</label>
                    <select id="round" className="field min-w-36" value={roundIndex} onChange={(event) => setSelectedRound(Number(event.target.value))}>
                      {results.rounds.map((item, index) => <option key={item.roundNumber} value={index}>Round {item.roundNumber}</option>)}
                    </select>
                  </div>
                )}
              </div>
              <div className="mt-8 h-80 w-full" role="img" aria-label={`Bar chart of votes in round ${round.roundNumber}`}>
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={roundOptions} margin={{ top: 10, right: 10, left: 0, bottom: 45 }}>
                    <CartesianGrid strokeDasharray="3 3" vertical={false} />
                    <XAxis dataKey="text" angle={-20} textAnchor="end" interval={0} height={70} />
                    <YAxis allowDecimals={false} />
                    <Tooltip formatter={(value) => [`${String(value)} votes`, 'Votes']} />
                    <Bar dataKey="votes" fill="#43815c" radius={[6, 6, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </div>
              <div className="mt-8 overflow-x-auto">
                <table className="w-full border-collapse text-left">
                  <caption className="sr-only">Vote totals for round {round.roundNumber}</caption>
                  <thead>
                    <tr className="border-b"><th className="p-3">Choice</th><th className="p-3 text-right">Votes</th><th className="p-3 text-right">Share</th><th className="p-3">Status</th></tr>
                  </thead>
                  <tbody>
                    {roundOptions.map((option) => (
                      <tr key={option.id} className="border-b last:border-0">
                        <th scope="row" className="p-3 font-semibold">{option.text}</th>
                        <td className="p-3 text-right">{option.votes}</td>
                        <td className="p-3 text-right">{option.percentage.toFixed(1)}%</td>
                        <td className="p-3">{option.status}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>
          )}
        </div>
      )}
    </div>
  )
}
