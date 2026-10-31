'use client';

import { createContext, useContext } from 'react';
import type { ComponentPropsWithoutRef } from 'react';

/**
 * AppShell renders the one `main#main` landmark around authenticated app routes. Pages rendered
 * both inside and outside the shell must not add a second `main` (axe: landmark-no-duplicate-main,
 * landmark-main-is-top-level) and, outside the shell, must own the skip-link target themselves
 * (axe: skip-link). `PageMain` does the right thing in both cases.
 */
export const ShellMainContext = createContext(false);

export function PageMain({ children, ...props }: ComponentPropsWithoutRef<'main'>) {
  const insideShellMain = useContext(ShellMainContext);

  if (insideShellMain) {
    return <div {...(props as ComponentPropsWithoutRef<'div'>)}>{children}</div>;
  }

  return (
    <main id="main" tabIndex={-1} {...props}>
      {children}
    </main>
  );
}
