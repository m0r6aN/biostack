type OAuthProfile = {
  email?: string | null;
  name?: string | null;
  username?: string | null;
  login?: string | null;
  preferred_username?: string | null;
  picture?: string | { data?: { url?: string | null } | null } | null;
  image?: string | null;
  avatar_url?: string | null;
  profile_image_url?: string | null;
};

export function resolveOAuthIdentity({
  provider,
  providerAccountId,
  profile,
  token,
}: {
  provider: string;
  providerAccountId: string;
  profile: OAuthProfile;
  token: { email?: string | null; name?: string | null };
}) {
  const email = profile.email?.trim() || token.email?.trim() || `oauth+${provider}-${providerAccountId}@biostack.local`;
  const name =
    profile.name?.trim() ||
    profile.username?.trim() ||
    profile.login?.trim() ||
    profile.preferred_username?.trim() ||
    token.name?.trim() ||
    `${provider.charAt(0).toUpperCase()}${provider.slice(1)} user`;

  const image =
    (typeof profile.picture === 'string' ? profile.picture : profile.picture?.data?.url) ||
    profile.image ||
    profile.avatar_url ||
    profile.profile_image_url ||
    null;

  return {
    email: email.toLowerCase(),
    name,
    image,
  };
}