import Link from 'next/link';
import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { SITE_ORIGIN, createPublicPageMetadata } from '@/lib/site';

export const metadata = createPublicPageMetadata({
  title: 'FDA Committee Recommends Six Peptides for Compounding: What the Vote Does and Does Not Mean | BioStack',
  description:
    'In July 2026, an FDA advisory committee voted to recommend BPC-157, KPV, TB-500, MOTS-c, Epitalon, and Semax for the 503A compounding list. What that recommendation is, what it is not, and what the research record says about each compound.',
  path: '/knowledge/insights/fda-pcac-2026-peptide-vote',
});

const PUBLISHED_AT = '2026-08-29';

const recommendedCompounds = [
  {
    slug: 'bpc-157',
    live: true,
    name: 'BPC-157',
    summary:
      'A 2025 systematic review located 36 studies and identified one human clinical study, reporting that no clinical safety data were found. The bulk of the literature is preclinical.',
  },
  {
    slug: 'kpv',
    live: false,
    name: 'KPV',
    summary:
      'The controlled evidence base is animal-model work, notably in experimental colitis. No controlled human efficacy trials were located in our research pass.',
  },
  {
    slug: 'tb-500',
    live: true,
    name: 'TB-500 (Thymosin beta-4 fragment)',
    summary:
      'Marketed widely for recovery; the human trial record for the gray-market fragment product is thin, and claims frequently extrapolate from preclinical thymosin beta-4 research.',
  },
  {
    slug: 'mots-c',
    live: true,
    name: 'MOTS-c',
    summary:
      'A mitochondrial-derived peptide with mechanistic and early human data. Our dossier grades its efficacy claims as insufficient pending controlled human trials.',
  },
  {
    slug: 'epitalon',
    live: false,
    name: 'Epitalon',
    summary:
      'Most human data trace to Russian studies of the related epithalamin preparation. Telomerase-related claims carry an unresolved benefit-versus-risk question that our dossier keeps under review.',
  },
  {
    slug: 'semax',
    live: false,
    name: 'Semax',
    summary:
      'A Russian-registered drug with human stroke studies of limited size and design. It holds no FDA approval, and its label-grade safety documentation is not available from U.S. regulators.',
  },
];

function ArticleJsonLd() {
  const siteUrl = SITE_ORIGIN;
  const jsonLd = {
    '@context': 'https://schema.org',
    '@type': 'Article',
    headline:
      'FDA Committee Recommends Six Peptides for Compounding: What the Vote Does and Does Not Mean',
    datePublished: PUBLISHED_AT,
    dateModified: PUBLISHED_AT,
    author: { '@type': 'Organization', name: 'BioStack' },
    publisher: { '@type': 'Organization', name: 'BioStack', url: siteUrl },
    mainEntityOfPage: `${siteUrl}/knowledge/insights/fda-pcac-2026-peptide-vote`,
    description:
      'What the July 2026 FDA Pharmacy Compounding Advisory Committee recommendation on six peptides is, what it is not, and what the research record says about each compound.',
  };
  return (
    <script
      type="application/ld+json"
      dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
    />
  );
}

export default function PcacVoteInsightPage() {
  return (
    <div className="min-h-screen pb-24 md:pb-0" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />
      <ArticleJsonLd />

      <main className="mx-auto max-w-3xl px-5 py-12 sm:px-8 lg:py-16">
        <p className="text-xs font-semibold uppercase tracking-[0.3em] text-emerald-300/70">
          Insights · Regulatory record
        </p>
        <h1 className="mt-4 text-3xl font-semibold tracking-tight text-white sm:text-5xl">
          An FDA committee recommended six peptides for compounding. Here is what that does and
          does not mean.
        </h1>
        <p className="mt-4 text-sm text-white/40">Published {PUBLISHED_AT} · Sources cited below</p>

        <section className="mt-10 space-y-5 text-base leading-8 text-white/72">
          <p>
            On July 23–24, 2026, the U.S. Food and Drug Administration&apos;s Pharmacy Compounding
            Advisory Committee (PCAC) evaluated seven peptides and voted to recommend six of them
            — BPC-157, KPV, TB-500, MOTS-c, Epitalon, and Semax — for inclusion on the 503A Bulks
            List. One peptide, emideltide (DSIP), was rejected. The National Community Pharmacists
            Association reported the outcome directly: &quot;The Food and Drug Administration&apos;s
            Pharmacy Compounding Advisory Committee (PCAC) voted to recommend that the FDA include
            peptides BPC-157, KPV, TB-500, MOTS-c, Epitalon, and Semax on the 503A Bulks
            List.&quot;
          </p>
          <p>
            If you follow peptide communities, you have probably seen this vote described as
            everything from &quot;FDA approval&quot; to &quot;legalization.&quot; It is neither.
            This page records what the vote actually is, and what the research record says about
            each compound involved.
          </p>
        </section>

        <section className="mt-12">
          <h2 className="text-2xl font-semibold text-white">What the recommendation is</h2>
          <div className="mt-5 space-y-5 text-base leading-8 text-white/72">
            <p>
              The 503A Bulks List names bulk drug substances that licensed compounding pharmacies
              may use to prepare medications under section 503A of the Food, Drug, and Cosmetic
              Act. A PCAC vote is an advisory recommendation to FDA. It is not binding: FDA must
              still act on the recommendation before any of these peptides are added to the list,
              and as of this article&apos;s publication date, FDA has not done so.
            </p>
            <p>
              The vote is still a meaningful signal. For several of these compounds it reverses
              years of restrictive posture — BPC-157, for example, sat in FDA&apos;s Category 2
              (&quot;significant safety risks&quot;) bulk-substances table until an April 2026
              restructuring removed it after its original nomination was withdrawn, a procedural
              change rather than a new safety finding.
            </p>
          </div>
        </section>

        <section className="mt-12">
          <h2 className="text-2xl font-semibold text-white">What the recommendation is not</h2>
          <div className="mt-5 space-y-5 text-base leading-8 text-white/72">
            <p>
              It is not FDA approval. None of the six peptides has been through a new drug
              application. No agency reviewed manufacturing consistency, efficacy for a claimed
              indication, or a label&apos;s worth of safety documentation, because for these
              compounds no such label exists.
            </p>
            <p>
              It is not an efficacy determination. A 503A listing addresses what pharmacies may
              legally compound — it says nothing about whether a compound does what its marketing
              claims. The committee did not evaluate the questions most buyers actually have.
            </p>
            <p>
              It is not a safety endorsement. The controlled human safety record for most of
              these compounds is thin to absent, and a compounding pathway does not change that.
            </p>
          </div>
        </section>

        <section className="mt-12">
          <h2 className="text-2xl font-semibold text-white">
            What the research record says, compound by compound
          </h2>
          <p className="mt-4 text-base leading-8 text-white/72">
            Each entry links to the full BioStack dossier: sources, verbatim quotes, evidence
            grades, and the gaps, graded by{' '}
            <Link href="/knowledge/methodology" className="text-emerald-300 hover:text-emerald-200">
              our public methodology
            </Link>
            .
          </p>
          <div className="mt-6 space-y-4">
            {recommendedCompounds.map((compound) => (
              <div
                key={compound.slug}
                className="rounded-lg border border-white/10 bg-white/[0.035] p-5"
              >
                {compound.live ? (
                  <Link
                    href={`/knowledge/${compound.slug}`}
                    className="text-lg font-semibold text-emerald-300 hover:text-emerald-200"
                  >
                    {compound.name}
                  </Link>
                ) : (
                  <p className="text-lg font-semibold text-white">
                    {compound.name}
                    <span className="ml-2 text-xs font-normal uppercase tracking-wider text-white/40">
                      dossier in final review
                    </span>
                  </p>
                )}
                <p className="mt-2 text-sm leading-6 text-white/62">{compound.summary}</p>
              </div>
            ))}
          </div>
        </section>

        <section className="mt-12">
          <h2 className="text-2xl font-semibold text-white">What happens next</h2>
          <div className="mt-5 space-y-5 text-base leading-8 text-white/72">
            <p>
              FDA decides whether to adopt the committee&apos;s recommendation. There is no
              statutory deadline. Until FDA acts, none of the six peptides is on the 503A Bulks
              List, and their regulatory status is unchanged. We re-verify the primary FDA sources
              in our dossiers when that status changes, and each dossier records the date its
              regulatory claims were last checked against the live record.
            </p>
            <p>
              BioStack does not sell, recommend, or advise on any compound. This page, like every
              dossier, is an educational record of what is known — and what is not.
            </p>
          </div>
        </section>

        <section className="mt-12 rounded-lg border border-white/10 bg-white/[0.03] p-5">
          <h2 className="text-sm font-semibold uppercase tracking-wider text-white/50">Sources</h2>
          <ul className="mt-3 space-y-2 text-sm leading-6 text-white/62">
            <li>
              National Community Pharmacists Association, &quot;FDA advisory committee nominates
              six peptides for pharmacies to compound,&quot; qAM, July 31, 2026.
            </li>
            <li>
              U.S. FDA, &quot;Certain bulk drug substances for use in compounding that may present
              significant safety risks&quot; (live listing; April 2026 restructuring), accessed
              August 2026.
            </li>
            <li>
              Primary literature and regulatory sources for each compound are cited with verbatim
              quotes inside the linked dossiers.
            </li>
          </ul>
        </section>
      </main>

      <MarketingFooter />
    </div>
  );
}
