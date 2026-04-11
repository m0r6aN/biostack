'use client';

import { cn } from '@/lib/utils';
import { ButtonHTMLAttributes, forwardRef } from 'react';

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'ghost' | 'outline' | 'danger';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
}

const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = 'primary', size = 'md', isLoading, children, disabled, ...props }, ref) => {
    const variants = {
      primary: 'bg-emerald-500 text-white hover:bg-emerald-400 shadow-[0_0_15px_rgba(16,185,129,0.25)] active:scale-[0.98]',
      secondary: 'bg-white/5 text-white/80 hover:bg-white/10 hover:text-white border border-white/10 active:scale-[0.98]',
      ghost: 'text-white/40 hover:text-white/80 hover:bg-white/5',
      outline: 'bg-transparent border border-emerald-500/30 text-emerald-400 hover:bg-emerald-500/10 hover:border-emerald-500/50',
      danger: 'bg-rose-500 text-white hover:bg-rose-400 shadow-[0_0_15px_rgba(244,63,94,0.25)]',
    };

    const sizes = {
      sm: 'h-8 px-3 text-[11px] rounded-lg',
      md: 'h-10 px-5 text-[13px] rounded-xl',
      lg: 'h-12 px-8 text-[15px] rounded-2xl',
    };

    return (
      <button
        ref={ref}
        disabled={disabled || isLoading}
        className={cn(
          'relative inline-flex items-center justify-center font-bold tracking-tight transition-all duration-200 focus:outline-none disabled:opacity-50 disabled:cursor-not-allowed overflow-hidden',
          variants[variant],
          sizes[size],
          className
        )}
        {...props}
      >
        {isLoading && (
          <div className="absolute inset-0 flex items-center justify-center bg-inherit">
            <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
            </svg>
          </div>
        )}
        <span className={cn('flex items-center gap-2', isLoading && 'invisible')}>
          {children}
        </span>
      </button>
    );
  }
);

Button.displayName = 'Button';

export { Button };
