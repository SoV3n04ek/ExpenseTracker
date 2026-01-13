import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthState, AuthResponseDto, LoginDto, RegisterDto } from '../../models/auth.model'; // Cleaned up
import { firstValueFrom } from 'rxjs';
import { Router } from '@angular/router';

const initialState: AuthState = {
  user: null,
  isLoading: false,
  error: null,
};

export const AuthStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  
  withComputed(({ user }) => ({
    isAuthenticated: computed(() => !!user()),
    userName: computed(() => user()?.name ?? 'Guest'),
  })),

  withMethods((store, http = inject(HttpClient), router = inject(Router)) => ({
    async login(credentials: LoginDto) { // Added type
      patchState(store, { isLoading: true, error: null });
      try {
        const response = await firstValueFrom(
          http.post<AuthResponseDto>('https://localhost:7253/api/auth/login', credentials)
        );
        localStorage.setItem('token', response.token);
        patchState(store, { user: response, isLoading: false });
        router.navigate(['/dashboard']);
      } catch (err: any) {
        patchState(store, { 
          error: err.error?.message ?? 'Login failed', 
          isLoading: false 
        });
      }
    },

    async register(dto: RegisterDto) { // Added type
      patchState(store, { isLoading: true, error: null });
      try {
        const response = await firstValueFrom(
          http.post<AuthResponseDto>('https://localhost:7253/api/auth/register', dto)
        );
        patchState(store, { isLoading: false });
        return response.message; 
      } catch (err: any) {
        patchState(store, { 
          error: err.error?.message ?? 'Registration failed', 
          isLoading: false 
        });
        throw err;
      }
    },

    logout() {
      localStorage.removeItem('token');
      patchState(store, { user: null });
      router.navigate(['/login']);
    }
  }))
);