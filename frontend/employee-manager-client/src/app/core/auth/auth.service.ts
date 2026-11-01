import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';

import { ApiService } from '../services/api.service';
import { CurrentUser, LoginResult } from './auth.model';

export const SESSION_STORAGE_KEY = 'employee-manager.session';

/** Holds the signed-in user's session (bearer token and profile), persisted across reloads. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  private readonly session = signal<LoginResult | null>(readStoredSession());

  readonly currentUser = computed<CurrentUser | null>(() => this.session()?.user ?? null);

  login(email: string, password: string): Observable<CurrentUser> {
    return this.api.post<LoginResult>('auth/login', { email, password }).pipe(
      tap((result) => this.setSession(result)),
      map((result) => result.user),
    );
  }

  logout(): void {
    this.setSession(null);
  }

  /** The bearer token, or null when signed out. An expired session is cleared. */
  accessToken(): string | null {
    const session = this.session();
    if (session && isExpired(session)) {
      this.logout();
      return null;
    }
    return session?.accessToken ?? null;
  }

  private setSession(session: LoginResult | null): void {
    this.session.set(session);
    try {
      if (session) {
        localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
      } else {
        localStorage.removeItem(SESSION_STORAGE_KEY);
      }
    } catch {
      // Storage can be unavailable (e.g. blocked site data); the session then lasts until reload.
    }
  }
}

function isExpired(session: LoginResult): boolean {
  return Date.parse(session.expiresAt) <= Date.now();
}

function readStoredSession(): LoginResult | null {
  try {
    const stored = localStorage.getItem(SESSION_STORAGE_KEY);
    const session = stored ? (JSON.parse(stored) as LoginResult) : null;
    return session && !isExpired(session) ? session : null;
  } catch {
    return null;
  }
}
