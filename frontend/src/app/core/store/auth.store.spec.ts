import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthStore } from './auth.store';
import { environment } from '../../../environment/environment';
import { AuthResponseDto } from '../../models/auth.model';
import { vi } from 'vitest';

describe('AuthStore', () => {
    let store: any;
    let httpMock: HttpTestingController;

    const mockUser: AuthResponseDto = {
        userId: '123',
        name: 'Test User',
        email: 'test@example.com',
        token: 'fake-jwt-token',
        expiration: new Date().toISOString(),
        message: 'Success'
    };

    beforeEach(() => {
        // Mock localStorage using window
        const storage: Record<string, string> = {};
        Object.defineProperty(window, 'localStorage', {
            value: {
                getItem: vi.fn((key) => storage[key] || null),
                setItem: vi.fn((key, value) => storage[key] = value),
                removeItem: vi.fn((key) => delete storage[key]),
                clear: vi.fn(() => { }),
                length: 0,
                key: vi.fn()
            },
            writable: true
        });

        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(),
                provideHttpClientTesting(),
                provideRouter([
                    { path: 'dashboard', component: class { } },
                    { path: 'login', component: class { } }
                ]),
                AuthStore,
            ]
        });

        // Initial setup for common services
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should login successfully and save token', async () => {
        store = TestBed.inject(AuthStore);
        const loginPromise = store.login({ email: 'test@example.com', password: 'password' });

        const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
        expect(req.request.method).toBe('POST');
        req.flush(mockUser);

        await loginPromise;

        expect(store.user()).toEqual(mockUser);
        expect(store.isAuthenticated()).toBe(true);
        expect(localStorage.setItem).toHaveBeenCalledWith('token', 'fake-jwt-token');
    });

    it('should map 401 unconfirmed email error correctly', async () => {
        store = TestBed.inject(AuthStore);
        const loginPromise = store.login({ email: 'unconfirmed@example.com', password: 'password' });

        const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
        req.flush({ message: 'Please confirm your email' }, { status: 401, statusText: 'Unauthorized' });

        await loginPromise;

        expect(store.status()).toBe('unconfirmed');
        expect(store.error()).toBe('Please confirm your email');
        expect(store.unconfirmedEmail()).toBe('unconfirmed@example.com');
    });

    it('should map 400 validation errors to an array', async () => {
        store = TestBed.inject(AuthStore);
        const loginPromise = store.login({ email: 'invalid', password: 'p' });

        const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
        req.flush({
            errors: {
                Email: ['Invalid email format'],
                Password: ['Password too short']
            }
        }, { status: 400, statusText: 'Bad Request' });

        await loginPromise;

        expect(Array.isArray(store.error())).toBe(true);
        expect(store.error()).toContain('Invalid email format');
        expect(store.error()).toContain('Password too short');
    });

    it('should validate session on init if token exists', async () => {
        window.localStorage.setItem('token', 'mock-token');

        // Injecting to trigger the effect/hook
        store = TestBed.inject(AuthStore);

        // Allow microtasks for navigate/validateSession to trigger
        await Promise.resolve();
        await Promise.resolve();

        const req = httpMock.expectOne(`${environment.apiUrl}/auth/me`);
        req.flush(mockUser);
        await Promise.resolve();

        expect(store.user()).toEqual(mockUser);
        expect(store.isAuthenticated()).toBe(true);
    });

    it('should clear session on logout', () => {
        store = TestBed.inject(AuthStore);
        store.logout();
        expect(localStorage.removeItem).toHaveBeenCalledWith('token');
        expect(store.user()).toBeNull();
        expect(store.isAuthenticated()).toBe(false);
    });
});
