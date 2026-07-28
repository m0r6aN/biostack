import type { Metadata } from 'next';

export const SITE_NAME = 'BioStack';
export const SITE_URL = new URL('https://biostack.cc');
export const SITE_ORIGIN = SITE_URL.origin;
export const SITE_TITLE = 'BioStack | Protocol Operations';
export const SITE_DESCRIPTION =
  'Your protocol operations system. Track compounds, surface overlap, and turn daily signal into continuity.';

export function absoluteSiteUrl(path = '/') {
  return new URL(path, SITE_URL).toString();
}

type PublicPageMetadataInput = {
  title: string;
  description: string;
  path: `/${string}` | '/';
};

export function createPublicPageMetadata({
  title,
  description,
  path,
}: PublicPageMetadataInput): Metadata {
  return {
    title,
    description,
    alternates: {
      canonical: path,
    },
    openGraph: {
      type: 'website',
      locale: 'en_US',
      url: path,
      siteName: SITE_NAME,
      title,
      description,
    },
    twitter: {
      card: 'summary',
      title,
      description,
    },
  };
}

export const ROOT_METADATA: Metadata = {
  metadataBase: SITE_URL,
  title: {
    default: SITE_TITLE,
    template: '%s',
  },
  description: SITE_DESCRIPTION,
  openGraph: {
    type: 'website',
    locale: 'en_US',
    url: '/',
    siteName: SITE_NAME,
    title: SITE_TITLE,
    description: SITE_DESCRIPTION,
  },
  twitter: {
    card: 'summary',
    title: SITE_TITLE,
    description: SITE_DESCRIPTION,
  },
  icons: {
    icon: [
      { url: '/favicon.ico', sizes: 'any' },
      { url: '/favicon.svg', type: 'image/svg+xml' },
    ],
  },
};
