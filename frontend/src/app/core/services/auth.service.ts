import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { environment } from '../../../environment/environment';
import { LoginDto, AuthResponseDto } from '../../models/auth.model';

export type AuthStatus = 'idle' | 'loading' | 'error' | 'unconfirmed';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private readonly http = inject(HttpClient);
    private readonly router = inject(Router);

    // Core State
    private readonly _user = signal<AuthResponseDto | null>(null);
    private readonly _status = signal<AuthStatus>('idle');
    private readonly _error = signal<string | string[] | null>(null);
    private readonly _unconfirmedEmail = signal<string | null>(null);

    // Public Accessors (Signals)
    public readonly currentUser = this._user.asReadonly();
    public readonly isAuthenticated = computed(() => !!this._user());
    public readonly status = this._status.asReadonly();
    public readonly error = this._error.asReadonly();
    public readonly isLoading = computed(() => this._status() === 'loading');
    public readonly unconfirmedEmail = this._unconfirmedEmail.asReadonly();

    constructor() { }

    /**
     * Login using credentials and persist token
     * Senior Tip: Ensure state is updated BEFORE navigation to avoid Guard race conditions.
     */
    login(credentials: LoginDto) {
        this._status.set('loading');
        this._error.set(null);
        this._unconfirmedEmail.set(null);

        return this.http.post<AuthResponseDto>(`${environment.apiUrl}/auth/login`, credentials).pipe(
            tap((response) => {
                if (response.token) {
                    localStorage.setItem('token', response.token);

                    // 1. Update Signal state synchronously
                    this._user.set(response);
                    this._status.set('idle');

                    // 2. Trigger navigation (Guards will now see updated state)
                    this.router.navigate(['/dashboard']);
                }
            }),
            catchError((err) => {
                let errorMessage: string | string[] = 'Login failed';
                let status: AuthStatus = 'error';

                if (err.status === 401 && err.error?.message?.toLowerCase().includes('confirm your email')) {
                    status = 'unconfirmed';
                    errorMessage = err.error.message;
                    this._unconfirmedEmail.set(credentials.email);
                } else {
                    errorMessage = err.error?.errors
                        ? Object.values(err.error.errors).flat() as string[]
                        : (err.error?.message || 'Invalid credentials.');
                }

                this._error.set(errorMessage);
                this._status.set(status);
                return of(null);
            })
        );
    }

    resendConfirmation() {
        const email = this._unconfirmedEmail();
        if (!email) return;

        this._status.set('loading');
        this.http.post(`${environment.apiUrl}/auth/resend-confirmation`, { email }).subscribe({
            next: () => {
                this._status.set('idle');
                this._error.set('Confirmation email resent! Please check your inbox.');
            },
            error: () => {
                this._status.set('error');
                this._error.set('Failed to resend confirmation email.');
            }
        });
    }

    logout() {
        localStorage.removeItem('token');
        this._user.set(null);
        this._status.set('idle');
        this.router.navigate(['/login']);
    }

    /**
     * Rehydrates the session on startup.
     * Returns a Promise so APP_INITIALIZER can wait for it.
     */
    initializeAuth(): Promise<void> {
        const token = localStorage.getItem('token');
        if (!token) {
            return Promise.resolve();
        }

        this._status.set('loading');
        return new Promise((resolve) => {
            this.http.get<AuthResponseDto>(`${environment.apiUrl}/auth/me`).pipe(
                catchError((err) => {
                    if (err.status === 401) {
                        localStorage.removeItem('token');
                        this._user.set(null);
                    }
                    this._status.set('idle');
                    return of(null);
                })
            ).subscribe(user => {
                if (user) {
                    this._user.set(user);
                }
                this._status.set('idle');
                resolve();
            });
        });
    }
}
