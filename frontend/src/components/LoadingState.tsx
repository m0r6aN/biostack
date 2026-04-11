import { BioStackLogo } from '@/components/ui/BioStackLogo';

export function LoadingState() {
  return (
    <div className="flex flex-col items-center justify-center py-12 px-4">
      <BioStackLogo
        variant="icon"
        size="lg"
        theme="dark"
        loading
        className="mb-4"
      />
      <p className="text-sm text-white/50">Loading...</p>
    </div>
  );
}

export function LoadingSkeleton() {
  return (
    <div className="space-y-4">
      {[...Array(3)].map((_, i) => (
        <div key={i} className="h-16 bg-white/[0.04] rounded-2xl animate-pulse"></div>
      ))}
    </div>
  );
}
