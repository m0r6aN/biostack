import { describe, expect, it } from 'vitest';
import { resolveOAuthIdentity } from '@/lib/oauthIdentity';

describe('resolveOAuthIdentity', () => {
  it('falls back to a synthetic local email when the provider does not return one', () => {
    const identity = resolveOAuthIdentity({
      provider: 'instagram',
      providerAccountId: '12345',
      profile: { username: 'stackpilot' },
      token: {},
    });

    expect(identity.email).toBe('oauth+instagram-12345@biostack.local');
    expect(identity.name).toBe('stackpilot');
  });

  it('extracts nested Facebook-style picture URLs and normalizes email casing', () => {
    const identity = resolveOAuthIdentity({
      provider: 'facebook',
      providerAccountId: '999',
      profile: {
        email: 'User@Example.com',
        name: 'Bio Stack',
        picture: { data: { url: 'https://cdn.example.test/avatar.png' } },
      },
      token: {},
    });

    expect(identity.email).toBe('user@example.com');
    expect(identity.name).toBe('Bio Stack');
    expect(identity.image).toBe('https://cdn.example.test/avatar.png');
  });
});