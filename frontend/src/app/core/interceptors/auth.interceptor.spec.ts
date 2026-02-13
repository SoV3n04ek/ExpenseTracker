import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { authInterceptor } from './auth.interceptor';
import { environment } from '../../../environment/environment';
import { vi } from 'vitest';

describe('AuthInterceptor', () => {
    let httpMock: HttpTestingController;
    let httpClient: HttpClient;

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
                provideHttpClient(withInterceptors([authInterceptor])),
                provideHttpClientTesting(),
            ]
        });

        httpMock = TestBed.inject(HttpTestingController);
        httpClient = TestBed.inject(HttpClient);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should add Authorization header when token exists and URL is API URL', () => {
        localStorage.setItem('token', 'fake-token');
        const testUrl = `${environment.apiUrl}/test`;

        httpClient.get(testUrl).subscribe();

        const req = httpMock.expectOne(testUrl);
        expect(req.request.headers.has('Authorization')).toBe(true);
        expect(req.request.headers.get('Authorization')).toBe('Bearer fake-token');
    });

    it('should NOT add Authorization header when NO token exists', () => {
        localStorage.removeItem('token');
        const testUrl = `${environment.apiUrl}/test`;

        httpClient.get(testUrl).subscribe();

        const req = httpMock.expectOne(testUrl);
        expect(req.request.headers.has('Authorization')).toBe(false);
    });

    it('should NOT add Authorization header for non-API URLs', () => {
        localStorage.setItem('token', 'fake-token');
        const testUrl = 'https://other-domain.com/data';

        httpClient.get(testUrl).subscribe();

        const req = httpMock.expectOne(testUrl);
        expect(req.request.headers.has('Authorization')).toBe(false);
    });
});
