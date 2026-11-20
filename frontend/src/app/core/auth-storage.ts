import type { UsuarioPerfil } from './models';

export const AUTH_TOKEN_KEY = 'desanti-auth-token-v1';

export function obterTokenAuth(): string | null {
  try {
    if (typeof localStorage === 'undefined') return null;
    return localStorage.getItem(AUTH_TOKEN_KEY);
  } catch {
    return null;
  }
}

export function salvarTokenAuth(token: string | null): void {
  try {
    if (typeof localStorage === 'undefined') return;
    if (token && token.trim()) {
      localStorage.setItem(AUTH_TOKEN_KEY, token.trim());
    } else {
      localStorage.removeItem(AUTH_TOKEN_KEY);
    }
  } catch {
    // Ignora falhas de acesso ao storage.
  }
}

export function usuarioEhAdmin(user: UsuarioPerfil | null | undefined): boolean {
  return Boolean(user && user.role.toLowerCase() === 'admin');
}
