import { notFound } from "next/navigation";
import { cookies } from "next/headers";
import Link from "next/link";
import { Shield, ArrowLeft, AlertCircle } from "lucide-react";
import { getApiBaseUrl } from "@/lib/apiBase";
import type { DecisionReceiptResponse } from "@/lib/types";

interface ReceiptPageProps {
  params: Promise<{ uri: string }>;
}

async function fetchReceipt(
  encodedUri: string,
): Promise<DecisionReceiptResponse | null> {
  const decoded = decodeURIComponent(encodedUri);
  const reEncoded = encodeURIComponent(decoded);
  const base = getApiBaseUrl();
  const cookieHeader = (await cookies()).toString();

  try {
    const res = await fetch(`${base}/api/v1/receipts/${reEncoded}`, {
      cache: "no-store",
      headers: cookieHeader ? { Cookie: cookieHeader } : undefined,
    });
    if (res.status === 404) return null;
    if (!res.ok) throw new Error(`API error ${res.status}`);
    return res.json();
  } catch {
    return null;
  }
}

function FieldRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="grid grid-cols-[160px_1fr] gap-3 py-3 border-b border-white/5 last:border-0">
      <span className="text-[10px] font-bold uppercase tracking-widest text-white/30 pt-0.5">
        {label}
      </span>
      <span className="text-sm font-mono text-white/70 break-all">{value}</span>
    </div>
  );
}

function EffectStatusPill({ status }: { status: string }) {
  const isNonEffecting = status === "non-effecting";
  return (
    <span
      className={[
        "inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-xs font-medium",
        isNonEffecting
          ? "border-blue-500/30 bg-blue-500/10 text-blue-400"
          : "border-white/10 bg-white/5 text-white/50",
      ].join(" ")}
    >
      <span
        className={[
          "w-2 h-2 rounded-full shrink-0",
          isNonEffecting ? "bg-blue-400" : "bg-white/40",
        ].join(" ")}
      />
      {status}
    </span>
  );
}

export default async function ReceiptPage({ params }: ReceiptPageProps) {
  const { uri } = await params;
  const receipt = await fetchReceipt(uri);

  if (receipt === null) {
    notFound();
  }

  return (
    <main className="min-h-screen bg-[#0a0a0b] text-white">
      <div className="max-w-2xl mx-auto px-4 py-10">
        {/* Back nav */}
        <Link
          href="/"
          className="inline-flex items-center gap-1.5 text-xs text-white/40 hover:text-white/70 transition-colors mb-8"
        >
          <ArrowLeft className="w-3.5 h-3.5" />
          Back
        </Link>

        {/* Header */}
        <div className="flex items-start gap-3 mb-8">
          <div className="mt-0.5 w-8 h-8 rounded-lg bg-white/5 border border-white/10 flex items-center justify-center shrink-0">
            <Shield className="w-4 h-4 text-white/40" aria-hidden />
          </div>
          <div>
            <h1 className="text-sm font-bold uppercase tracking-widest text-white/60 mb-1">
              Decision Receipt
            </h1>
            <p className="text-xs font-mono text-white/30 break-all">
              {receipt.receiptUri}
            </p>
          </div>
        </div>

        {/* Effect status */}
        <div className="mb-6">
          <EffectStatusPill status={receipt.effectStatus} />
        </div>

        {/* Fields */}
        <div className="rounded-xl border border-white/10 bg-white/[0.02] px-6 py-2 mb-6">
          <FieldRow label="Subject" value={receipt.subjectUri} />
          <FieldRow label="Tenant" value={receipt.tenantId} />
          <FieldRow label="Actor" value={receipt.actorId} />
          <FieldRow
            label="Timestamp"
            value={new Date(receipt.timestampUtc).toISOString()}
          />
          <FieldRow label="Decision" value={receipt.decision} />
          <FieldRow label="Effect status" value={receipt.effectStatus} />
          <FieldRow label="Input hash" value={receipt.inputHash} />
          <FieldRow label="Policy hash" value={receipt.policyHash.value} />
          <FieldRow label="Policy version" value={receipt.policyHash.version} />
        </div>

        {/* Evidence refs */}
        <div>
          <h2 className="text-[9px] font-bold uppercase tracking-widest text-white/20 mb-3">
            Evidence References
          </h2>
          {receipt.evidenceRefs.length === 0 ? (
            <p className="text-xs text-white/30 italic">
              No evidence references attached.
            </p>
          ) : (
            <ul className="space-y-2">
              {receipt.evidenceRefs.map((ref: string, i: number) => (
                <li
                  key={i}
                  className="text-xs font-mono text-white/50 bg-white/5 rounded-lg px-4 py-2 border border-white/5 break-all"
                >
                  {ref}
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </main>
  );
}
