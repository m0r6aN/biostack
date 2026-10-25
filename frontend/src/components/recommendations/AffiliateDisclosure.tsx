interface AffiliateDisclosureProps {
  className?: string;
}

export function AffiliateDisclosure({ className }: AffiliateDisclosureProps) {
  return (
    <p className={['text-[11px] leading-5 text-white/36', className].filter(Boolean).join(' ')}>
      Links open third-party retailer search results.
    </p>
  );
}