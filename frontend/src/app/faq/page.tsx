import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { featuredFaqs } from '@/lib/marketing';

export default function FaqPage() {
  const faqSchema = {
    '@context': 'https://schema.org',
    '@type': 'FAQPage',
    mainEntity: featuredFaqs.map((faq) => ({
      '@type': 'Question',
      name: faq.question,
      acceptedAnswer: {
        '@type': 'Answer',
        text: faq.answer,
      },
    })),
  };

  return (
    <div className="min-h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />

      <main className="mx-auto max-w-5xl px-5 py-16 sm:px-8 lg:py-20">
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(faqSchema) }}
        />

        <section className="max-w-3xl">
          <p className="text-xs font-semibold uppercase tracking-[0.32em] text-emerald-300/70">
            FAQ
          </p>
          <h1 className="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-5xl">
            Clear answers for the questions that determine trust.
          </h1>
          <p className="mt-5 text-lg leading-8 text-white/62">
            BioStack’s position is intentionally narrow: observation, organization, and research
            intelligence. The answers below keep that boundary explicit.
          </p>
        </section>

        <section className="mt-14 space-y-5">
          {featuredFaqs.map((faq) => (
            <article key={faq.question} className="rounded-[1.75rem] border border-white/10 bg-white/[0.03] p-6">
              <h2 className="text-xl font-semibold text-white">{faq.question}</h2>
              <p className="mt-3 leading-8 text-white/64">{faq.answer}</p>
            </article>
          ))}
        </section>
      </main>

      <MarketingFooter />
    </div>
  );
}
