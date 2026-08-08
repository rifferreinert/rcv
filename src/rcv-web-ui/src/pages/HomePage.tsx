import { ArrowRightIcon, BarsArrowDownIcon, CheckBadgeIcon, ShareIcon } from '@heroicons/react/24/outline'
import { Link } from 'react-router-dom'
import { useAuth } from '../auth'

export function HomePage() {
  const { user } = useAuth()
  return (
    <>
      <section className="overflow-hidden border-b bg-cream-100">
        <div className="container-page grid gap-10 py-20 lg:grid-cols-[1.2fr_.8fr] lg:items-center lg:py-28">
          <div>
            <p className="mb-4 font-bold uppercase tracking-[.2em] text-sage-700">Ranked choice voting, made clear</p>
            <h1 className="max-w-3xl text-5xl font-bold leading-[1.05] tracking-tight sm:text-6xl">
              Better decisions start with everyone’s preferences.
            </h1>
            <p className="mt-6 max-w-2xl text-xl leading-relaxed text-ink-700">
              Create a poll, share one private link, and let voters rank as many choices as they want.
            </p>
            <Link to={user ? '/polls/new' : '/login'} className="btn-primary mt-8 gap-2">
              {user ? 'Create a poll' : 'Get started'} <ArrowRightIcon className="size-5" aria-hidden="true" />
            </Link>
          </div>
          <div className="rounded-3xl bg-ink-950 p-7 text-white shadow-xl">
            <p className="text-sm font-bold uppercase tracking-widest text-sage-100">Example ballot</p>
            <ol className="mt-6 grid gap-3">
              {['Community garden', 'Playground upgrade', 'Outdoor stage'].map((choice, index) => (
                <li key={choice} className="flex items-center gap-4 rounded-xl bg-white/10 p-4">
                  <span className="grid size-8 place-items-center rounded-full bg-coral-500 font-bold">{index + 1}</span>
                  <span className="font-semibold">{choice}</span>
                </li>
              ))}
            </ol>
          </div>
        </div>
      </section>
      <section className="container-page py-20" aria-labelledby="how-heading">
        <h2 id="how-heading" className="text-3xl font-bold">Simple for organizers and voters</h2>
        <div className="mt-10 grid gap-5 md:grid-cols-3">
          {[
            [CheckBadgeIcon, 'Create', 'Add two or more choices, choose a deadline, and decide when results are visible.'],
            [ShareIcon, 'Share', 'Send the direct poll link. There is no public directory of polls.'],
            [BarsArrowDownIcon, 'Rank', 'Voters order all or just some choices. Each ballot can be revised while voting is open.'],
          ].map(([Icon, title, body]) => (
            <article key={title as string} className="card">
              <Icon className="size-9 text-sage-700" aria-hidden="true" />
              <h3 className="mt-5 text-xl font-bold">{title as string}</h3>
              <p className="mt-2 leading-relaxed text-ink-700">{body as string}</p>
            </article>
          ))}
        </div>
      </section>
    </>
  )
}
