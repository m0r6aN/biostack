'use client';

import { CheckCircle2 } from 'lucide-react';
import { useState } from 'react';
import { ModalShell } from './ModalShell';

interface ContactCareTeamModalProps {
  onClose: () => void;
  onSubmit: (message: string) => Promise<void> | void;
}

export function ContactCareTeamModal({ onClose, onSubmit }: ContactCareTeamModalProps) {
  const [message, setMessage] = useState('');
  const [sending, setSending] = useState(false);
  const [sent, setSent] = useState(false);

  async function handleSend() {
    if (!message.trim() || sending) return;
    setSending(true);
    try {
      await onSubmit(message.trim());
      setSent(true);
      setTimeout(onClose, 2000);
    } finally {
      setSending(false);
    }
  }

  if (sent) {
    return (
      <ModalShell onClose={onClose} labelledBy="care-sent-title">
        <div className="py-4 text-center">
          <CheckCircle2 className="mx-auto h-10 w-10 text-emerald-400" />
          <h2 id="care-sent-title" className="mt-3 font-semibold text-white">
            Note Saved
          </h2>
          <p className="mt-1 text-sm text-white/55">
            The note was added to your BioStack protocol record.
          </p>
        </div>
      </ModalShell>
    );
  }

  return (
    <ModalShell onClose={onClose} labelledBy="care-modal-title">
      <div className="pr-8">
        <h2 id="care-modal-title" className="text-xl font-semibold tracking-tight text-white">
          Care Team Note
        </h2>
        <p className="mt-1 text-sm text-white/55">
          Save an observational update or question with your protocol record. This does not send a notification.
        </p>
      </div>

      <textarea
        value={message}
        onChange={(event) => setMessage(event.target.value)}
        placeholder="Type your message here..."
        className="mt-5 h-28 w-full resize-none rounded-xl border border-white/[0.08] bg-black/20 p-4 text-sm text-white outline-none placeholder:text-white/30 focus:border-emerald-400/50"
      />

      <div className="mt-4 flex justify-end gap-3">
        <button
          type="button"
          onClick={onClose}
          className="rounded-xl border border-white/[0.1] px-5 py-2.5 text-sm font-medium text-white/75 transition-colors hover:border-white/20"
        >
          Cancel
        </button>
        <button
          type="button"
          onClick={handleSend}
          disabled={!message.trim() || sending}
          className="rounded-xl bg-emerald-500 px-5 py-2.5 text-sm font-semibold text-slate-950 transition-colors hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {sending ? 'Saving' : 'Save Note'}
        </button>
      </div>
    </ModalShell>
  );
}
