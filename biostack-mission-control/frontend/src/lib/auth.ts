import NextAuth from 'next-auth';
import Apple from 'next-auth/providers/apple';
import Discord from 'next-auth/providers/discord';
import GitHub from 'next-auth/providers/github';
import Google from 'next-auth/providers/google';

const apiUrl          = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
const callbackSecret  = process.env.AUTH_CALLBACK_SECRET || '';

/**
 * After every OAuth sign-in we POST the user's resolved identity to the .NET backend.
 * The backend upserts the AppUser and returns a JWT that we store in the session token.
 */
async function syncUserWithBackend(provider: string, providerAccountId: string, email: string, name: string, image?: string | null) {
  try {
    const res = await fetch(`${apiUrl}/api/v1/auth/oauth-callback`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Callback-Secret': callbackSecret,
      },
      body: JSON.stringify({ provider, providerAccountId, email, name, image }),
    });
    if (!res.ok) {
      console.error('[BioStack Auth] Backend sync failed:', res.status, await res.text());
      return null;
    }
    return (await res.json()) as {
      accessToken: string;
      expiresInSeconds: number;
      user: { id: string; email: string; displayName: string; avatarUrl?: string; role: number };
    };
  } catch (err) {
    console.error('[BioStack Auth] Backend sync error:', err);
    return null;
  }
}

export const { handlers, signIn, signOut, auth } = NextAuth({
  providers: [
    Google({
      clientId:     process.env.GOOGLE_CLIENT_ID!,
      clientSecret: process.env.GOOGLE_CLIENT_SECRET!,
    }),
    GitHub({
      clientId:     process.env.GITHUB_CLIENT_ID!,
      clientSecret: process.env.GITHUB_CLIENT_SECRET!,
    }),
    Discord({
      clientId:     process.env.DISCORD_CLIENT_ID!,
      clientSecret: process.env.DISCORD_CLIENT_SECRET!,
    }),
    Apple({
      clientId:     process.env.APPLE_CLIENT_ID!,
      clientSecret: process.env.APPLE_CLIENT_SECRET!,
    }),
  ],

  session: { strategy: 'jwt' },

  callbacks: {
    async jwt({ token, account, profile, user }) {
      // First sign-in: account and profile are set
      if (account && profile) {
        const providerAccountId = account.providerAccountId;
        const email    = (profile as { email?: string }).email ?? token.email ?? '';
        const name     = (profile as { name?: string }).name  ?? token.name  ?? '';
        const image    = (profile as { picture?: string; image?: string; avatar_url?: string }).picture
                      ?? (profile as { image?: string }).image
                      ?? (profile as { avatar_url?: string }).avatar_url
                      ?? null;

        const resp = await syncUserWithBackend(account.provider, providerAccountId, email as string, name as string, image);
        if (resp) {
          token.backendAccessToken = resp.accessToken;
          token.bioUserId          = resp.user.id;
          token.role               = resp.user.role;          // 0=User, 1=Admin
          token.displayName        = resp.user.displayName;
          token.avatarUrl          = resp.user.avatarUrl;
        }
      }
      return token;
    },

    async session({ session, token }) {
      session.backendAccessToken = token.backendAccessToken as string | undefined;
      session.user.id            = token.bioUserId as string ?? '';
      session.user.role          = (token.role as number) ?? 0;
      session.user.name          = (token.displayName as string) ?? session.user.name;
      session.user.image         = (token.avatarUrl as string) ?? session.user.image;
      return session;
    },
  },
});
