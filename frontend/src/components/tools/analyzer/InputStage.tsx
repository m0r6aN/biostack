'use client';

import { trackAnalyzerEvent } from '@/lib/analyzerAnalytics';
import type { ProtocolAnalyzerInputType, PersonProfile } from '@/lib/types';
import type { AnalyzerGoalSelection } from '@/lib/analyzerGoals';
import type { ChangeEvent } from 'react';
import { useEffect, useRef } from 'react';
import { AnalyzerGoalPicker } from './AnalyzerGoalPicker';
import { RefineAnalysisPanel } from './RefineAnalysisPanel';
import type { AnalyzerContextFields } from './useAnalyzerSession';

const supportedFileTypes = '.pdf,.docx,.xlsx,.csv,.txt,.jpg,.jpeg,.png,.webp';
const maxUploadLabel = 'Up to 12 MB';

const modeTabs: Array<{
  id: ProtocolAnalyzerInputType;
  label: string;
  title: string;
  description: string;
}> = [
  { id: 'Paste', label: 'Paste', title: 'Paste protocol text', description: 'Paste raw notes, tables, or cheat sheet text.' },
  { id: 'FileUpload', label: 'Upload', title: 'Upload a protocol file', description: 'PDF, DOCX, XLSX, CSV, TXT, JPG, PNG, or WEBP.' },
  { id: 'CameraScan', label: 'Scan', title: 'Scan a protocol', description: 'Use your camera on mobile or choose a photo on desktop.' },
  { id: 'Link', label: 'Link', title: 'Analyze a shared document link', description: 'Paste a public document URL and we will fetch it when supported.' },
];

type InputStageProps = {
  mode: ProtocolAnalyzerInputType;
  inputText: string;
  linkUrl: string;
  selectedFile: File | null;
  goals: AnalyzerGoalSelection;
  context: AnalyzerContextFields;
  profile: PersonProfile | null;
  isAuthenticated: boolean;
  isPending: boolean;
  error: string;
  onModeChange: (mode: ProtocolAnalyzerInputType) => void;
  onInputTextChange: (text: string) => void;
  onLinkUrlChange: (url: string) => void;
  onFileSelected: (file: File | null) => void;
  onGoalsChange: (goals: AnalyzerGoalSelection) => void;
  onContextChange: (context: AnalyzerContextFields) => void;
  onAnalyze: () => void;
  onClear: () => void;
  onLoadExample: (example: 'healing' | 'fatLoss' | 'longevity') => void;
  onScanRequested: () => void;
};

export function InputStage({
  mode,
  inputText,
  linkUrl,
  selectedFile,
  goals,
  context,
  profile,
  isAuthenticated,
  isPending,
  error,
  onModeChange,
  onInputTextChange,
  onLinkUrlChange,
  onFileSelected,
  onGoalsChange,
  onContextChange,
  onAnalyze,
  onClear,
  onLoadExample,
  onScanRequested,
}: InputStageProps) {
  const uploadInputRef = useRef<HTMLInputElement | null>(null);
  const cameraInputRef = useRef<HTMLInputElement | null>(null);

  const modeConfig = modeTabs.find((tab) => tab.id === mode) ?? modeTabs[0];

  // When the parent clears inputs, selectedFile becomes null but the native
  // file-input widgets still show the old filename (the parent can't reach
  // these refs). Reset them here so the UI matches the cleared state.
  useEffect(() => {
    if (selectedFile === null) {
      if (uploadInputRef.current) uploadInputRef.current.value = '';
      if (cameraInputRef.current) cameraInputRef.current.value = '';
    }
  }, [selectedFile]);

  const analyzeDisabled =
    isPending ||
    (mode === 'Paste' && inputText.trim().length === 0) ||
    (mode === 'Link' && linkUrl.trim().length === 0) ||
    ((mode === 'FileUpload' || mode === 'CameraScan') && !selectedFile);

  function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0] ?? null;
    onFileSelected(file);
  }

  function handleScanRequested() {
    trackAnalyzerEvent('analyzer_scan_selected', { inputType: 'CameraScan' });
    trackAnalyzerEvent('analyzer_input_mode_selected', { inputType: 'CameraScan' });
    onScanRequested();
    // The parent's onScanRequested sets mode='CameraScan', which renders the
    // camera file input. Defer with setTimeout(...,0) so the input exists past
    // the parent's setMode before we open the native picker (parity with the
    // monolith's auto-open).
    window.setTimeout(() => cameraInputRef.current?.click(), 0);
  }

  return (
    <section className="rounded-lg border border-white/[0.08] bg-[#121923]/95 p-4 shadow-[0_16px_48px_rgba(0,0,0,0.24)] sm:p-5">
      {/* Mode tabs */}
      <div className="flex flex-wrap gap-2">
        {modeTabs.map((tab) => (
          <button
            key={tab.id}
            type="button"
            onClick={() => {
              onModeChange(tab.id);
              trackAnalyzerEvent('analyzer_input_mode_selected', { inputType: tab.id });
              if (tab.id === 'CameraScan') {
                trackAnalyzerEvent('analyzer_scan_selected', { inputType: tab.id });
              }
            }}
            className={`rounded-full border px-4 py-2 text-sm font-semibold transition-colors ${
              mode === tab.id
                ? 'border-emerald-300/45 bg-emerald-400/14 text-emerald-100'
                : 'border-white/10 text-white/65 hover:border-white/20 hover:text-white'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Mobile scan button */}
      <button
        type="button"
        onClick={handleScanRequested}
        className="mt-4 inline-flex min-h-11 w-full items-center justify-center rounded-lg border border-emerald-300/35 bg-emerald-400/12 px-4 text-sm font-bold text-emerald-50 transition-colors hover:bg-emerald-400/18 md:hidden"
      >
        Scan protocol
      </button>

      {/* Mode panel */}
      <div className="mt-5">
        <h2 className="text-lg font-semibold text-white">{modeConfig.title}</h2>
        <p className="mt-2 text-sm leading-6 text-white/58">{modeConfig.description}</p>

        {mode === 'Paste' && (
          <>
            <label className="mt-4 block">
              <span className="mb-3 block text-sm font-semibold text-white/72">Protocol text</span>
              <textarea
                value={inputText}
                onChange={(event) => onInputTextChange(event.target.value)}
                rows={16}
                className="min-h-80 w-full resize-y rounded-lg border border-white/10 bg-[#0F141B] p-4 text-sm leading-6 text-white outline-none transition-colors placeholder:text-white/30 focus:border-emerald-400/45"
                placeholder={`BPC-157 500mcg daily\nTB-500 500mcg daily\nNAD+ 100mg 2x weekly\n8 weeks on, 8 weeks off`}
              />
            </label>
            <p className="mt-3 text-sm leading-6 text-white/52">
              Paste raw protocol text, tables, dosing notes, or cheat sheet content. We will extract compounds, doses, frequencies, and stack structure automatically.
            </p>
          </>
        )}

        {mode === 'FileUpload' && (
          <UploadPanel
            selectedFile={selectedFile}
            inputRef={uploadInputRef}
            onTrigger={() => uploadInputRef.current?.click()}
            onChange={handleFileChange}
            title="Drop in a protocol file"
            description="Upload PDF, DOCX, XLSX, CSV, TXT, JPG, JPEG, PNG, or WEBP."
          />
        )}

        {mode === 'CameraScan' && (
          <UploadPanel
            selectedFile={selectedFile}
            inputRef={cameraInputRef}
            onTrigger={() => cameraInputRef.current?.click()}
            onChange={handleFileChange}
            title="Scan protocol"
            description="Use your camera on mobile or choose a protocol photo. BioStack will extract text from the image."
            capture="environment"
            accept="image/*"
            helper="Camera scan works best on mobile. On desktop, you can still choose an image from your device."
          />
        )}

        {mode === 'Link' && (
          <>
            <label className="mt-4 block">
              <span className="mb-3 block text-sm font-semibold text-white/72">Shared document URL</span>
              <input
                type="url"
                value={linkUrl}
                onChange={(event) => onLinkUrlChange(event.target.value)}
                className="min-h-12 w-full rounded-lg border border-white/10 bg-[#0F141B] px-4 text-sm text-white outline-none transition-colors placeholder:text-white/30 focus:border-emerald-400/45"
                placeholder="https://example.com/shared-protocol.pdf"
              />
            </label>
            <p className="mt-3 text-sm leading-6 text-white/52">
              Public direct-file links work best right now. Unsupported or protected sources will return a clear message instead of failing silently.
            </p>
          </>
        )}

        {/* Trust line */}
        <p className="mt-2 text-xs leading-5 text-white/38">
          Educational analysis only. Verify all dosing math manually.
        </p>
      </div>

      {/* Accepted files card */}
      <div className="mt-4 rounded-lg border border-white/10 bg-black/20 p-3 text-sm text-white/55">
        <p className="font-semibold text-white/78">Accepted files</p>
        <p className="mt-2">PDF, DOCX, XLSX, CSV, TXT, JPG, PNG, WEBP</p>
        <p className="mt-2 text-xs text-white/42">{maxUploadLabel}</p>
      </div>

      {/* Goal picker */}
      <div className="mt-4">
        <AnalyzerGoalPicker selection={goals} onChange={onGoalsChange} />
      </div>

      {/* Refine analysis panel */}
      <div className="mt-4">
        <RefineAnalysisPanel
          context={context}
          onChange={onContextChange}
          profile={profile}
          isAuthenticated={isAuthenticated}
        />
      </div>

      {/* Action row */}
      <div className="mt-5 flex flex-col gap-3 sm:flex-row sm:flex-wrap">
        <button
          type="button"
          onClick={onAnalyze}
          disabled={analyzeDisabled}
          className="inline-flex min-h-12 items-center justify-center rounded-lg bg-emerald-400 px-5 text-sm font-bold text-[#07110c] transition-colors hover:bg-emerald-300 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {isPending ? 'Analyzing Protocol...' : 'Analyze Protocol'}
        </button>
        <button
          type="button"
          onClick={onClear}
          className="min-h-12 rounded-lg border border-white/10 px-4 text-sm font-semibold text-white/72 transition-colors hover:border-white/20 hover:text-white"
        >
          Clear
        </button>
      </div>

      {/* Example links */}
      <div className="mt-4 flex flex-wrap gap-2">
        <ExampleButton label="Healing stack" onClick={() => onLoadExample('healing')} />
        <ExampleButton label="Fat loss stack" onClick={() => onLoadExample('fatLoss')} />
        <ExampleButton label="Longevity stack" onClick={() => onLoadExample('longevity')} />
      </div>

      {/* Failure state */}
      {error && <AnalyzerFailureState message={error} onRetry={onAnalyze} />}
    </section>
  );
}

function UploadPanel({
  selectedFile,
  inputRef,
  onTrigger,
  onChange,
  title,
  description,
  accept = supportedFileTypes,
  capture,
  helper,
}: {
  selectedFile: File | null;
  inputRef: React.RefObject<HTMLInputElement | null>;
  onTrigger: () => void;
  onChange: (event: ChangeEvent<HTMLInputElement>) => void;
  title: string;
  description: string;
  accept?: string;
  capture?: 'environment';
  helper?: string;
}) {
  return (
    <div className="mt-4 rounded-2xl border border-dashed border-white/15 bg-[#0F141B] p-5">
      <input ref={inputRef} type="file" accept={accept} capture={capture} onChange={onChange} className="hidden" />
      <p className="text-sm font-semibold text-white">{title}</p>
      <p className="mt-2 text-sm leading-6 text-white/56">{description}</p>
      <div className="mt-4 flex flex-wrap gap-3">
        <button type="button" onClick={onTrigger} className="rounded-lg bg-emerald-400 px-4 py-2 text-sm font-semibold text-slate-950 hover:bg-emerald-300">
          {capture ? 'Use camera' : 'Choose file'}
        </button>
        <span className="rounded-lg border border-white/10 px-3 py-2 text-sm text-white/50">{maxUploadLabel}</span>
      </div>
      {selectedFile ? (
        <p className="mt-4 text-sm text-emerald-100/85">
          Ready: {selectedFile.name} ({Math.max(1, Math.round(selectedFile.size / 1024))} KB)
        </p>
      ) : (
        <p className="mt-4 text-sm text-white/42">Supported types: PDF, DOCX, XLSX, CSV, TXT, JPG, PNG, WEBP</p>
      )}
      {helper ? <p className="mt-2 text-xs leading-5 text-white/38">{helper}</p> : null}
    </div>
  );
}

function ExampleButton({ label, onClick }: { label: string; onClick: () => void }) {
  return (
    <button type="button" onClick={onClick} className="rounded-lg border border-white/10 px-3 py-2 text-left text-sm font-semibold text-white/70 transition-colors hover:border-white/20 hover:text-white">
      {label}
    </button>
  );
}

function AnalyzerFailureState({ message, onRetry }: { message: string; onRetry: () => void }) {
  return (
    <div className="mt-4 rounded-lg border border-amber-300/20 bg-amber-400/[0.08] p-4">
      <p className="text-sm font-semibold text-amber-50">Analysis is temporarily unavailable.</p>
      <p className="mt-2 text-sm leading-6 text-amber-50/78">{message}</p>
      <p className="mt-1 text-sm leading-6 text-white/52">
        Calculators and locally saved work still work while the intelligence service recovers.
      </p>
      <button
        type="button"
        onClick={onRetry}
        className="mt-3 rounded-lg border border-amber-200/25 px-4 py-2 text-sm font-semibold text-amber-50 transition-colors hover:border-amber-100/45 hover:text-white"
      >
        Try again
      </button>
    </div>
  );
}
