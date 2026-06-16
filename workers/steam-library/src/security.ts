export interface AllowedOriginEnv {
  ALLOWED_ORIGIN: string;
  CATALOG_RETURN_URL: string;
}

export function isAllowedOrigin(origin: string | null, env: AllowedOriginEnv): boolean {
  return origin === env.ALLOWED_ORIGIN;
}

export function corsOrigin(origin: string | null, env: AllowedOriginEnv): string {
  return origin ?? env.ALLOWED_ORIGIN;
}
