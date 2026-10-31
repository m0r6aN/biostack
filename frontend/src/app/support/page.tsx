import Link from 'next/link';
import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { createPublicPageMetadata } from '@/lib/site';
import { SUPPORT_EMAIL, SUPPORT_ENTRIES, SUPPORT_GROUPS } from '@/lib/supportContent';

export const metadata = createPublicPageMetadata({
  title: 'Support and Help | BioStack',
  description:
    'Help with signing in, consent, exporting or deleting your data, evidence tiers, billing, and reporting a data error. What BioStack does and does not do.',
  path: '/support',
});

export default function SupportPage() {
  const faqSchema = {
    '@context': 'https://schema.org',
    '@type': 'FAQPage',
    mainEntity: SUPPORT_ENTRIES.map((entry) => ({
      '@type': 'Question',
      name: entry.question,
      acceptedAnswer: { '@type': 'Answer', text: entry.answer.join(' ') },
    })),
  };

  return (
    <div className="min-h-screen pb-24 md:pb-0" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />

      <main id="main" tabIndex={-1} className="mx-auto max-w-5xl px-5 py-12 sm:px-8 lg:py-16">
        <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(faqSchema) }} />

        <section className="max-w-3xl">
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-emerald-300/70">Support</p>
          <h1 className="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-5xl">How can we help?</h1>
          <p className="mt-5 text-lg leading-8 text-white/62">
            Answers about signing in, your data, and what BioStack does and does not do. If you do not find what you need,
            use the contact form or email {SUPPORT_EMAIL}, and we will read it.
          </p>
          <div className="mt-6 flex flex-wrap gap-3">
            <Link
              href="/contact"
              className="inline-flex min-h-11 items-center rounded-lg bg-emerald-400 px-5 text-sm font-semibold text-slate-950 transition-colors hover:bg-emerald-300"
            >
              Contact us
            </Link>
            <Link
              href="/account/data"
              className="inline-flex min-h-11 items-center rounded-lg border border-white/12 px-5 text-sm font-semibold text-white transition-colors hover:border-white/24"
            >
              Export or delete my data
            </Link>
          </div>
        </section>

        <nav aria-label="Support topics" className="mt-10 rounded-lg border border-white/10 bg-white/[0.03] p-5">
          <h2 className="text-sm font-semibold uppercase tracking-[0.2em] text-white/70">On this page</h2>
          <ul className="mt-3 grid gap-x-6 gap-y-1 sm:grid-cols-2">
            {SUPPORT_GROUPS.map((group) => (
              <li key={group.id}>
                <a href={`#${group.id}`} className="inline-flex min-h-8 items-center text-sm text-emerald-200 underline-offset-4 hover:underline">
                  {group.heading}
                </a>
              </li>
            ))}
          </ul>
        </nav>

        <div className="mt-12 space-y-14">
          {SUPPORT_GROUPS.map((group) => (
            <section key={group.id} id={group.id} aria-labelledby={`${group.id}-heading`} className="scroll-mt-24">
              <h2 id={`${group.id}-heading`} className="text-2xl font-semibold text-white">
                {group.heading}
              </h2>
              <div className="mt-5 space-y-4">
                {group.entries.map((entry) => (
                  <article key={entry.id} id={entry.id} className="scroll-mt-24 rounded-lg border border-white/10 bg-white/[0.03] p-6">
                    <h3 className="text-lg font-semibold text-white">{entry.question}</h3>
                    <div className="mt-3 space-y-3">
                      {entry.answer.map((paragraph) => (
                        <p key={paragraph} className="leading-7 text-white/70">
                          {paragraph}
                        </p>
                      ))}
                    </div>
                    {entry.links && entry.links.length > 0 && (
                      <ul className="mt-4 flex flex-wrap gap-x-5 gap-y-1">
                        {entry.links.map((link) => (
                          <li key={link.href}>
                            {link.href.startsWith('mailto:') ? (
                              <a href={link.href} className="inline-flex min-h-8 items-center text-sm font-medium text-emerald-200 underline underline-offset-4 hover:text-emerald-100">
                                {link.label}
                              </a>
                            ) : (
                              <Link href={link.href} className="inline-flex min-h-8 items-center text-sm font-medium text-emerald-200 underline underline-offset-4 hover:text-emerald-100">
                                {link.label}
                              </Link>
                            )}
                          </li>
                        ))}
                      </ul>
                    )}
                  </article>
                ))}
              </div>
            </section>
          ))}
        </div>
      </main>

      <MarketingFooter />
    </div>
  );
}
