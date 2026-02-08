import { signalStore, withState, withMethods, withComputed, patchState, withHooks } from '@ngrx/signals';
import { computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthState, AuthResponseDto, LoginDto, RegisterDto, ResetPasswordRequest } from '../../models/auth.model'; // Cleaned up
import { firstValueFrom } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../../../environment/environment';

const initialState: AuthState = {
  user: null,
  isLoading: false,
  error: null,
  status: 'idle',
  unconfirmedEmail: null,
};

export const AuthStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),

  withComputed(({ user }) => ({
    isAuthenticated: computed(() => !!user()),
    userName: computed(() => user()?.name ?? 'Guest'),
  })),

  withMethods((store, http = inject(HttpClient), router = inject(Router)) => ({
    async login(credentials: LoginDto) {
      patchState(store, { isLoading: true, error: null, status: 'loading' });
      try {
        const response = await firstValueFrom(
          http.post<AuthResponseDto>(`${environment.apiUrl}/auth/login`, credentials)
        );
        localStorage.setItem('token', response.token);
        patchState(store, { user: response, isLoading: false, status: 'idle' });
        router.navigate(['/dashboard']);
      } catch (err: any) {
        let errorMessage: string | string[] = 'Login failed';
        let status: 'idle' | 'loading' | 'unconfirmed' | 'error' = 'error';

        if (err.status === 0) {
          errorMessage = 'Backend server is offline. Please start the service.';
        } else if (err.status === 401) {
          if (err.error?.message?.toLowerCase().includes('confirm your email')) {
            status = 'unconfirmed';
            errorMessage = err.error.message;
            patchState(store, { unconfirmedEmail: credentials.email });
          } else {
            errorMessage = 'Invalid credentials.';
          }
        } else if (err.status === 403) {
          errorMessage = 'Invalid credentials.';
        } else if (err.status === 400 && err.error?.errors) {
          errorMessage = Object.values(err.error.errors).flat().join(' ');
        } else {
          errorMessage = err.error?.message || 'Login failed';
        }

        patchState(store, {
          error: errorMessage,
          isLoading: false,
          status
        });
      }
    },

    async register(dto: RegisterDto) {
      patchState(store, { isLoading: true, error: null, status: 'loading' });
      try {
        const response = await firstValueFrom(
          http.post<AuthResponseDto>(`${environment.apiUrl}/auth/register`, dto)
        );
        patchState(store, { isLoading: false, status: 'idle' });
        router.navigate(['/register-success']);
        return response.message;
      } catch (err: any) {
        let errorMessage: string | string[] = 'Registration failed';

        if (err.status === 0) {
          errorMessage = 'Backend server is offline. Please start the service.';
        } else if (err.status === 400 && err.error?.errors) {
          errorMessage = Object.values(err.error.errors).flat().join(' ');
        } else {
          errorMessage = err.error?.message || 'Registration failed';
        }

        patchState(store, {
          error: errorMessage,
          isLoading: false,
          status: 'error'
        });
        throw err;
      }
    },

    async resendConfirmation() {
      const email = store.unconfirmedEmail();
      if (!email) return;

      patchState(store, { isLoading: true, error: null });
      try {
        await firstValueFrom(
          http.post(`${environment.apiUrl}/auth/resend-confirmation`, { email })
        );
        patchState(store, {
          isLoading: false,
          error: 'Confirmation email resent! Please check your inbox.',
          status: 'idle'
        });
      } catch (err: any) {
        patchState(store, {
          isLoading: false,
          error: 'Failed to resend confirmation email.',
          status: 'error'
        });
      }
    },

    async confirmEmail(userId: string, token: string) {
      patchState(store, { isLoading: true, error: null, status: 'loading' });
      try {
        await firstValueFrom(
          http.get(`${environment.apiUrl}/auth/confirm-email`, {
            params: { userId, token }
          })
        );
        patchState(store, { isLoading: false, status: 'confirmed' });
      } catch (err: any) {
        patchState(store, {
          isLoading: false,
          error: err.error?.message || 'Email confirmation failed.',
          status: 'error'
        });
      }
    },

    async validateSession() {
      const token = localStorage.getItem('token');
      if (!token) return;

      patchState(store, { isLoading: true });
      try {
        // GET /auth/me to verify token and get user info
        const user = await firstValueFrom(
          http.get<AuthResponseDto>(`${environment.apiUrl}/auth/me`)
        );
        patchState(store, { user, isLoading: false, status: 'idle' });
      } catch (err) {
        localStorage.removeItem('token');
        patchState(store, { user: null, isLoading: false, status: 'idle' });
      }
    },

    logout() {
      localStorage.removeItem('token');
      patchState(store, { user: null, status: 'idle', unconfirmedEmail: null });
      router.navigate(['/login']);
    },

    async resetPassword(data: ResetPasswordRequest) {
      patchState(store, { isLoading: true, error: null, status: 'loading' });
      try {
        await firstValueFrom(
          http.post(`${environment.apiUrl}/auth/reset-password`, data)
        );
        patchState(store, { isLoading: false, status: 'idle' });
      } catch (err: any) {
        let errorMessage: string | string[] = 'Password reset failed';

        if (err.status === 400 && err.error?.errors) {
          errorMessage = Object.values(err.error.errors).flat().join(' ');
        } else {
          errorMessage = err.error?.message || 'Password reset failed';
        }

        patchState(store, {
          error: errorMessage,
          isLoading: false,
          status: 'error'
        });
        throw err;
      }
    },

    async forgotPassword(email: string) {
      patchState(store, { isLoading: true, error: null, status: 'loading' });
      try {
        await firstValueFrom(
          http.post(`${environment.apiUrl}/auth/forgot-password`, { email })
        );
        patchState(store, { isLoading: false, status: 'idle' });
      } catch (err: any) {
        // For security, we only show errors for technical failures (500, 0, etc.)
        // 4xx errors are treated as success from the UI perspective to prevent enumeration.
        const isTechnicalError = err.status === 0 || err.status >= 500;

        patchState(store, {
          isLoading: false,
          error: isTechnicalError ? (err.error?.message || 'Server error. Please try again later.') : null,
          status: isTechnicalError ? 'error' : 'idle'
        });

        // Even on non-technical error, we want the component to transition to "submitted"
        if (!isTechnicalError) {
          return;
        }
        throw err;
      }
    }

  })),

  withHooks({
    onInit(store) {
      store.validateSession();
    },
  })

);