import {
  DndContext,
  KeyboardSensor,
  PointerSensor,
  TouchSensor,
  closestCenter,
  useSensor,
  useSensors,
  type DragEndEvent,
} from '@dnd-kit/core'
import {
  SortableContext,
  arrayMove,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import {
  ArrowDownIcon,
  ArrowUpIcon,
  Bars3Icon,
  LinkIcon,
  MinusIcon,
  PlusIcon,
} from '@heroicons/react/24/outline'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { errorMessage, pollsApi } from '../api'
import { useAuth } from '../auth'
import {
  Alert,
  ConfirmDialog,
  EmptyState,
  ErrorState,
  LoadingState,
  PageHeader,
} from '../components'
import type { PollOption } from '../types'
import { formatDate } from '../utils'

function RankedOption({
  option,
  rank,
  first,
  last,
  onMove,
  onRemove,
}: {
  option: PollOption
  rank: number
  first: boolean
  last: boolean
  onMove: (direction: -1 | 1) => void
  onRemove: () => void
}) {
  const sortable = useSortable({ id: option.id })
  return (
    <li
      ref={sortable.setNodeRef}
      style={{ transform: CSS.Transform.toString(sortable.transform), transition: sortable.transition }}
      className={`flex items-center gap-3 rounded-xl border bg-white p-3 ${sortable.isDragging ? 'z-10 shadow-lg' : ''}`}
    >
      <span className="grid size-8 shrink-0 place-items-center rounded-full bg-sage-700 font-bold text-white" aria-label={`Rank ${rank}`}>
        {rank}
      </span>
      <span className="min-w-0 flex-1 font-semibold">{option.text}</span>
      <div className="flex shrink-0">
        <button type="button" className="rounded-lg p-2 hover:bg-cream-100 disabled:opacity-30" disabled={first} onClick={() => onMove(-1)} aria-label={`Move ${option.text} up`}>
          <ArrowUpIcon className="size-5" />
        </button>
        <button type="button" className="rounded-lg p-2 hover:bg-cream-100 disabled:opacity-30" disabled={last} onClick={() => onMove(1)} aria-label={`Move ${option.text} down`}>
          <ArrowDownIcon className="size-5" />
        </button>
        <button type="button" className="rounded-lg p-2 text-red-700 hover:bg-red-50" onClick={onRemove} aria-label={`Remove ${option.text} from ranking`}>
          <MinusIcon className="size-5" />
        </button>
        <button
          type="button"
          className="touch-none rounded-lg p-2 hover:bg-cream-100"
          {...sortable.attributes}
          {...sortable.listeners}
          aria-label={`Drag ${option.text} to change rank`}
        >
          <Bars3Icon className="size-5" />
        </button>
      </div>
    </li>
  )
}

export function PollPage() {
  const { id = '' } = useParams()
  const { user } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [rankedIds, setRankedIds] = useState<string[]>([])
  const initialized = useRef('')
  const [message, setMessage] = useState<string | null>((location.state as { saved?: string } | null)?.saved ?? null)
  const [dialog, setDialog] = useState<'close' | 'delete' | null>(null)

  const pollQuery = useQuery({ queryKey: ['poll', id], queryFn: () => pollsApi.get(id), enabled: Boolean(id) })
  const voteQuery = useQuery({
    queryKey: ['poll', id, 'vote'],
    queryFn: () => pollsApi.voteStatus(id),
    enabled: Boolean(user && id),
    retry: false,
  })

  useEffect(() => {
    if (voteQuery.data && initialized.current !== id) {
      setRankedIds(voteQuery.data.rankedOptionIds)
      initialized.current = id
    }
  }, [id, voteQuery.data])

  const voteMutation = useMutation({
    mutationFn: () => pollsApi.vote(id, rankedIds),
    onSuccess: async () => {
      setMessage(voteQuery.data?.hasVoted ? 'Your revised ballot was saved.' : 'Your ballot was saved.')
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['poll', id, 'vote'] }),
        queryClient.invalidateQueries({ queryKey: ['poll', id], exact: true }),
        queryClient.invalidateQueries({ queryKey: ['results', id] }),
      ])
    },
  })
  const manageMutation = useMutation({
    mutationFn: async (action: 'close' | 'delete') => {
      if (action === 'close') await pollsApi.close(id)
      else await pollsApi.delete(id)
    },
    onSuccess: async (_, action) => {
      setDialog(null)
      await queryClient.invalidateQueries({ queryKey: ['polls'] })
      if (action === 'delete') {
        queryClient.removeQueries({ queryKey: ['poll', id] })
        queryClient.removeQueries({ queryKey: ['results', id] })
        navigate('/dashboard', { replace: true })
      } else {
        setMessage('Voting is now closed.')
        await Promise.all([
          queryClient.invalidateQueries({ queryKey: ['poll', id] }),
          queryClient.invalidateQueries({ queryKey: ['results', id] }),
        ])
      }
    },
  })
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 6 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 150, tolerance: 5 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  )

  if (pollQuery.isLoading) return <LoadingState label="Loading poll…" />
  if (pollQuery.error) return <ErrorState error={pollQuery.error} onRetry={() => void pollQuery.refetch()} />
  const poll = pollQuery.data!
  const isCreator = user?.id.toLowerCase() === poll.creator.id.toLowerCase()
  const isExpired = poll.closesAt ? new Date(poll.closesAt).getTime() <= Date.now() : false
  const isOpen = poll.status === 'Active' && !isExpired
  const ranked = rankedIds.map((optionId) => poll.options.find(({ id: optionIdCandidate }) => optionIdCandidate === optionId)).filter((option): option is PollOption => Boolean(option))
  const available = poll.options.filter((option) => !rankedIds.includes(option.id))

  const move = (index: number, direction: -1 | 1) => {
    setRankedIds((current) => arrayMove(current, index, index + direction))
  }
  const dragEnd = ({ active, over }: DragEndEvent) => {
    if (over && active.id !== over.id) {
      setRankedIds((current) => arrayMove(current, current.indexOf(String(active.id)), current.indexOf(String(over.id))))
    }
  }
  const share = async () => {
    try {
      await navigator.clipboard.writeText(window.location.href)
      setMessage('Poll link copied to your clipboard.')
    } catch {
      setMessage('Copy the poll URL from your browser to share it.')
    }
  }

  return (
    <div className="container-page py-10">
      <PageHeader
        eyebrow={isOpen ? 'Voting open' : 'Voting closed'}
        title={poll.title}
        description={poll.description ?? undefined}
        actions={
          <>
            <button type="button" className="btn-secondary gap-2" onClick={share}><LinkIcon className="size-5" /> Share</button>
            {(poll.isResultsPublic || isCreator || !isOpen) && <Link className="btn-secondary" to={`/polls/${id}/results`}>View results</Link>}
          </>
        }
      />
      {message && <div className="mb-6"><Alert>{message}</Alert></div>}
      {(voteMutation.error || manageMutation.error) && <div className="mb-6"><Alert tone="error">{errorMessage(voteMutation.error ?? manageMutation.error)}</Alert></div>}
      <div className="grid gap-6 lg:grid-cols-[1fr_20rem]">
        <section className="card" aria-labelledby="ballot-heading">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <h2 id="ballot-heading" className="text-2xl font-bold">Your ranking</h2>
              <p className="mt-2 text-ink-700">Add as many choices as you want, then arrange your favorite first.</p>
            </div>
            {voteQuery.data?.hasVoted && <span className="rounded-full bg-sage-100 px-3 py-1 text-sm font-bold text-sage-700">Ballot submitted</span>}
          </div>
          {!isOpen ? (
            <div className="mt-7"><Alert tone="info">This poll is closed. Ballots can no longer be submitted or changed.</Alert></div>
          ) : !user ? (
            <div className="mt-7 rounded-xl bg-cream-100 p-6 text-center">
              <p className="font-semibold">Sign in to rank these choices and save your ballot.</p>
              <Link className="btn-primary mt-4" to="/login" state={{ returnTo: location.pathname }}>Sign in to vote</Link>
            </div>
          ) : voteQuery.isLoading ? (
            <div className="mt-7"><LoadingState label="Loading your ballot…" /></div>
          ) : voteQuery.error ? (
            <ErrorState error={voteQuery.error} onRetry={() => void voteQuery.refetch()} />
          ) : voteQuery.data?.hasVoted && !voteQuery.data.canChange ? (
            <div className="mt-7"><Alert tone="info">Your ballot is recorded and can no longer be changed.</Alert></div>
          ) : (
            <>
              <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={dragEnd}>
                <SortableContext items={rankedIds} strategy={verticalListSortingStrategy}>
                  <ol className="mt-7 grid gap-3" aria-label="Ranked choices" aria-live="polite">
                    {ranked.map((option, index) => (
                      <RankedOption
                        key={option.id}
                        option={option}
                        rank={index + 1}
                        first={index === 0}
                        last={index === ranked.length - 1}
                        onMove={(direction) => move(index, direction)}
                        onRemove={() => setRankedIds((current) => current.filter((optionId) => optionId !== option.id))}
                      />
                    ))}
                  </ol>
                </SortableContext>
              </DndContext>
              {ranked.length === 0 && <div className="mt-7"><EmptyState title="Your ballot is empty" description="Add at least one choice below to begin." /></div>}
              {available.length > 0 && (
                <div className="mt-7 border-t pt-6">
                  <h3 className="font-bold">Unranked choices</h3>
                  <ul className="mt-3 grid gap-2 sm:grid-cols-2">
                    {available.map((option) => (
                      <li key={option.id}>
                        <button type="button" className="flex min-h-11 w-full items-center gap-2 rounded-xl border bg-white p-3 text-left font-semibold hover:bg-cream-100" onClick={() => setRankedIds((current) => [...current, option.id])}>
                          <PlusIcon className="size-5 shrink-0 text-sage-700" aria-hidden="true" /> {option.text}
                        </button>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
              <button type="button" className="btn-primary mt-7 w-full sm:w-auto" disabled={ranked.length === 0 || voteMutation.isPending} onClick={() => voteMutation.mutate()}>
                {voteMutation.isPending ? 'Saving ballot…' : voteQuery.data?.hasVoted ? 'Save revised ballot' : 'Submit ballot'}
              </button>
            </>
          )}
        </section>
        <aside className="space-y-5">
          <section className="card">
            <h2 className="font-bold">Poll details</h2>
            <dl className="mt-4 grid gap-4 text-sm">
              <div><dt className="text-ink-500">Created by</dt><dd className="font-semibold">{poll.creator.displayName ?? 'Anonymous organizer'}</dd></div>
              <div><dt className="text-ink-500">{isOpen ? 'Voting closes' : 'Closed'}</dt><dd className="font-semibold">{formatDate(isOpen ? poll.closesAt : poll.closedAt ?? poll.closesAt)}</dd></div>
              <div><dt className="text-ink-500">Choices</dt><dd className="font-semibold">{poll.options.length}</dd></div>
            </dl>
          </section>
          {isCreator && (
            <section className="card">
              <h2 className="font-bold">Organizer controls</h2>
              <div className="mt-4 grid gap-3">
                {isOpen && poll.voteCount === 0 && <Link className="btn-secondary" to={`/polls/${id}/edit`}>Edit poll</Link>}
                {isOpen && <button type="button" className="btn-secondary" onClick={() => setDialog('close')}>Close voting</button>}
                <button type="button" className="rounded-xl px-4 py-2 font-semibold text-red-700 hover:bg-red-50" onClick={() => setDialog('delete')}>Delete poll</button>
              </div>
            </section>
          )}
        </aside>
      </div>
      <ConfirmDialog
        open={dialog !== null}
        title={dialog === 'delete' ? 'Delete this poll?' : 'Close voting now?'}
        description={dialog === 'delete' ? 'This removes the poll from your dashboard and makes its link unavailable. This cannot be undone.' : 'No new or revised ballots will be accepted. This cannot be undone.'}
        confirmLabel={dialog === 'delete' ? 'Delete poll' : 'Close voting'}
        destructive={dialog === 'delete'}
        busy={manageMutation.isPending}
        onClose={() => setDialog(null)}
        onConfirm={() => dialog && manageMutation.mutate(dialog)}
      />
    </div>
  )
}
