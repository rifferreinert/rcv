import { zodResolver } from '@hookform/resolvers/zod'
import { PlusIcon, TrashIcon } from '@heroicons/react/24/outline'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useFieldArray, useForm } from 'react-hook-form'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { z } from 'zod'
import { errorMessage, pollsApi } from '../api'
import { useAuth } from '../auth'
import { Alert, ErrorState, FieldError, LoadingState, PageHeader } from '../components'
import type { PollInput, UpdatePollInput } from '../types'

const optionSchema = z.object({ text: z.string().trim().min(1, 'Enter an option.').max(500, 'Keep options under 500 characters.') })
const pollSchema = z
  .object({
    title: z.string().trim().min(1, 'Enter a title.').max(500, 'Keep the title under 500 characters.'),
    description: z.string().trim().max(2000, 'Keep the description under 2,000 characters.'),
    options: z.array(optionSchema).min(2, 'Add at least two options.').max(50, 'A poll can have at most 50 options.'),
    hasDeadline: z.boolean(),
    closesAt: z.string(),
    isResultsPublic: z.boolean(),
  })
  .superRefine((values, context) => {
    const normalized = values.options.map(({ text }) => text.trim().toLocaleLowerCase())
    if (new Set(normalized).size !== normalized.length) {
      context.addIssue({ code: 'custom', message: 'Options must be unique.', path: ['options'] })
    }
    if (values.hasDeadline) {
      if (!values.closesAt) {
        context.addIssue({ code: 'custom', message: 'Choose a deadline.', path: ['closesAt'] })
      } else if (!Number.isFinite(new Date(values.closesAt).getTime())) {
        context.addIssue({ code: 'custom', message: 'Enter a valid deadline.', path: ['closesAt'] })
      } else if (new Date(values.closesAt).getTime() <= Date.now()) {
        context.addIssue({ code: 'custom', message: 'The deadline must be in the future.', path: ['closesAt'] })
      }
    }
  })

type PollFormValues = z.infer<typeof pollSchema>

function localDateTime(value: string | null) {
  if (!value) return ''
  const date = new Date(value)
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 16)
}

export function PollFormPage() {
  const { id } = useParams()
  const { user } = useAuth()
  const isEdit = Boolean(id)
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const pollQuery = useQuery({
    queryKey: ['poll', id],
    queryFn: () => pollsApi.get(id!),
    enabled: isEdit,
  })
  const form = useForm<PollFormValues>({
    resolver: zodResolver(pollSchema),
    defaultValues: {
      title: '',
      description: '',
      options: [{ text: '' }, { text: '' }],
      hasDeadline: false,
      closesAt: '',
      isResultsPublic: true,
    },
    values: pollQuery.data
      ? {
          title: pollQuery.data.title,
          description: pollQuery.data.description ?? '',
          options: pollQuery.data.options.map(({ text }) => ({ text })),
          hasDeadline: Boolean(pollQuery.data.closesAt),
          closesAt: localDateTime(pollQuery.data.closesAt),
          isResultsPublic: pollQuery.data.isResultsPublic,
        }
      : undefined,
  })
  const options = useFieldArray({ control: form.control, name: 'options' })
  const hasDeadline = form.watch('hasDeadline')

  const mutation = useMutation({
    mutationFn: async (values: PollFormValues) => {
      const input: PollInput = {
        title: values.title.trim(),
        description: values.description.trim(),
        options: values.options.map(({ text }) => text.trim()),
        closesAt: values.hasDeadline ? new Date(values.closesAt).toISOString() : null,
        isResultsPublic: values.isResultsPublic,
      }
      if (!isEdit) return pollsApi.create(input)

      const updateInput: UpdatePollInput = {
        ...input,
        removeClosesAt: !values.hasDeadline,
      }
      return pollsApi.update(id!, updateInput)
    },
    onSuccess: async (poll) => {
      queryClient.setQueryData(['poll', poll.id], poll)
      await queryClient.invalidateQueries({ queryKey: ['polls'] })
      navigate(`/polls/${poll.id}`, { replace: true, state: { saved: isEdit ? 'Poll updated.' : 'Poll created. Share its link when you are ready.' } })
    },
  })

  if (isEdit && pollQuery.isLoading) return <LoadingState label="Loading poll…" />
  if (pollQuery.error) return <ErrorState error={pollQuery.error} onRetry={() => void pollQuery.refetch()} />
  if (pollQuery.data && user?.id.toLowerCase() !== pollQuery.data.creator.id.toLowerCase()) {
    return <ErrorState error={new Error('Only the poll creator can edit this poll.')} />
  }
  if (pollQuery.data?.status === 'Closed') {
    return <ErrorState error={new Error('Closed polls can no longer be edited.')} />
  }
  if (pollQuery.data && pollQuery.data.voteCount > 0) {
    return <ErrorState error={new Error('Polls can no longer be edited after voting begins.')} />
  }

  return (
    <div className="container-page py-10">
      <PageHeader
        eyebrow={isEdit ? 'Poll settings' : 'New poll'}
        title={isEdit ? 'Edit poll' : 'Create a ranked poll'}
        description="Give voters clear choices. They can rank every option or submit a partial ballot."
      />
      <form className="grid gap-6 lg:grid-cols-[1fr_20rem]" onSubmit={form.handleSubmit((values) => mutation.mutate(values))} noValidate>
        <div className="grid gap-6">
          {mutation.error && <Alert tone="error">{errorMessage(mutation.error)}</Alert>}
          <section className="card grid gap-5" aria-labelledby="details-heading">
            <h2 id="details-heading" className="text-xl font-bold">Poll details</h2>
            <div>
              <label className="label" htmlFor="title">Title</label>
              <input id="title" className="field" autoFocus {...form.register('title')} aria-invalid={Boolean(form.formState.errors.title)} />
              <FieldError message={form.formState.errors.title?.message} />
            </div>
            <div>
              <label className="label" htmlFor="description">Description <span className="font-normal text-ink-500">(optional)</span></label>
              <textarea id="description" className="field min-h-28" {...form.register('description')} />
              <FieldError message={form.formState.errors.description?.message} />
            </div>
          </section>
          <section className="card" aria-labelledby="options-heading">
            <div className="flex items-center justify-between gap-4">
              <div>
                <h2 id="options-heading" className="text-xl font-bold">Options</h2>
                <p className="mt-1 text-sm text-ink-500">Add 2–50 unique choices.</p>
              </div>
              <button
                type="button"
                className="btn-secondary gap-2"
                disabled={options.fields.length >= 50}
                onClick={() => options.append({ text: '' })}
              >
                <PlusIcon className="size-5" aria-hidden="true" /> Add
              </button>
            </div>
            <FieldError message={form.formState.errors.options?.root?.message ?? form.formState.errors.options?.message} />
            <ol className="mt-5 grid gap-3">
              {options.fields.map((field, index) => (
                <li key={field.id} className="flex items-start gap-3">
                  <label className="sr-only" htmlFor={`option-${index}`}>Option {index + 1}</label>
                  <span className="mt-2.5 grid size-7 shrink-0 place-items-center rounded-full bg-sage-100 text-sm font-bold text-sage-700">{index + 1}</span>
                  <div className="flex-1">
                    <input id={`option-${index}`} className="field mt-0" {...form.register(`options.${index}.text`)} aria-invalid={Boolean(form.formState.errors.options?.[index]?.text)} />
                    <FieldError message={form.formState.errors.options?.[index]?.text?.message} />
                  </div>
                  <button
                    type="button"
                    className="mt-1 rounded-lg p-2 text-red-700 hover:bg-red-50 disabled:opacity-40"
                    aria-label={`Remove option ${index + 1}`}
                    disabled={options.fields.length <= 2}
                    onClick={() => options.remove(index)}
                  >
                    <TrashIcon className="size-5" aria-hidden="true" />
                  </button>
                </li>
              ))}
            </ol>
          </section>
        </div>
        <aside className="space-y-6">
          <section className="card grid gap-5" aria-labelledby="settings-heading">
            <h2 id="settings-heading" className="text-xl font-bold">Settings</h2>
            <label className="flex gap-3">
              <input type="checkbox" className="mt-1 size-5 accent-sage-700" {...form.register('hasDeadline')} />
              <span><strong className="block">Set a deadline</strong><span className="text-sm text-ink-500">Voting closes automatically.</span></span>
            </label>
            {hasDeadline && (
              <div>
                <label className="label" htmlFor="closesAt">Voting closes</label>
                <input id="closesAt" type="datetime-local" className="field" {...form.register('closesAt')} />
                <FieldError message={form.formState.errors.closesAt?.message} />
              </div>
            )}
            <label className="flex gap-3">
              <input type="checkbox" className="mt-1 size-5 accent-sage-700" {...form.register('isResultsPublic')} />
              <span><strong className="block">Show live results</strong><span className="text-sm text-ink-500">Anyone with the link can see results before voting closes.</span></span>
            </label>
          </section>
          <button type="submit" className="btn-primary w-full" disabled={mutation.isPending}>
            {mutation.isPending ? 'Saving…' : isEdit ? 'Save changes' : 'Create poll'}
          </button>
          <Link className="btn-secondary w-full" to={isEdit ? `/polls/${id}` : '/dashboard'}>Cancel</Link>
        </aside>
      </form>
    </div>
  )
}
