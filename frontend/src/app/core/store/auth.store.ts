import { signalStore, withState, withMethods, withComputed, patchState, withHooks } from '@ngrx/signals';
import { computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthState, AuthResponseDto, LoginDto, RegisterDto } from '../../models/auth.model'; // Cleaned up
import { firstValueFrom } from 'rxjs';
import { Router } from '@angular/router';

const initialState: AuthState = {
  user: null,
  isLoading: false,
  error: null,
  status: 'idle',
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
          http.post<AuthResponseDto>('https://localhost:7253/api/auth/login', credentials)
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
          http.post<AuthResponseDto>('https://localhost:7253/api/auth/register', dto)
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
      const email = (store as any).unconfirmedEmail?.();
      if (!email) return;

      patchState(store, { isLoading: true, error: null });
      try {
        await firstValueFrom(
          http.post('https://localhost:7253/api/auth/resend-confirmation', { email })
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

    logout() {
      localStorage.removeItem('token');
      patchState(store, { user: null });
      router.navigate(['/login']);
    }
  })),

  withHooks({
    onInit(store) {
      const token = localStorage.getItem('token');
      if (token) {
        // In a real app, you might want to decode the token or verify it with the backend
        // For now, we'll just set a dummy user or keep it as is if logic allows
        console.log('Token found in localStorage, user remains authenticated (stub)');
      }
    },
  })
);