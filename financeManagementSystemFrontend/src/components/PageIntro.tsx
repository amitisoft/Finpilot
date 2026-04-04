import type { ReactNode } from 'react';
import { cn } from '../lib/utils';

interface PageIntroProps {
  eyebrow: string;
  title: string;
  description?: string;
  actions?: ReactNode;
  aside?: ReactNode;
  className?: string;
}

export function PageIntro({ eyebrow, title, description, actions, aside, className }: PageIntroProps) {
  return (
    <div className={cn('app-page-intro', className)}>
      <div className="app-page-intro__body">
        <p className="app-page-intro__eyebrow">{eyebrow}</p>
        <h2 className="app-page-intro__title">{title}</h2>
        {description ? <p className="app-page-intro__description">{description}</p> : null}
        {actions ? <div className="app-page-intro__actions">{actions}</div> : null}
      </div>
      {aside ? <div className="app-page-intro__aside">{aside}</div> : null}
    </div>
  );
}
