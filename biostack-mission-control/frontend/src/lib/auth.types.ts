// Extend NextAuth's built-in types so TypeScript knows about our custom session fields.
import 'next-auth';
import 'next-auth/jwt';

declare module 'next-auth' {
  interface Session {
    /** JWT issued by the .NET backend — forwarded in API Authorization headers */
    backendAccessToken?: string;
    user: {
      id:     string;
      name?:  string | null;
      email?: string | null;
      image?: string | null;
      /** 0 = User, 1 = Admin */
      role:   number;
    };
  }
}

declare module 'next-auth/jwt' {
  interface JWT {
    backendAccessToken?: string;
    bioUserId?:          string;
    role?:               number;
    displayName?:        string;
    avatarUrl?:          string;
  }
}
