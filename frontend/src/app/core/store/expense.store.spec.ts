import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ExpenseStore } from './expense.store';
import { environment } from '../../../environment/environment';
import { ExpenseDto, PagedResponse } from '../../models/expense.model';
import { NotificationService } from '../services/notification.service';
import { vi } from 'vitest';

describe('ExpenseStore', () => {
    let store: any;
    let httpMock: HttpTestingController;
    let notificationServiceMock: any;

    const mockExpenses: ExpenseDto[] = [
        { id: 1, description: 'Lunch', amount: 15, date: '2024-03-20', categoryName: 'Food' },
        { id: 2, description: 'Bus', amount: 3, date: '2024-03-20', categoryName: 'Transport' }
    ];

    const mockPagedResponse: PagedResponse<ExpenseDto> = {
        items: mockExpenses,
        totalCount: 2,
        pageNumber: 1,
        pageSize: 10,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false
    };

    const mockSummary = {
        totalAmount: 18,
        categories: [
            { categoryName: 'Food', amount: 15, percentage: 83.33 },
            { categoryName: 'Transport', amount: 3, percentage: 16.67 }
        ]
    };

    beforeEach(() => {
        notificationServiceMock = {
            showUndo: vi.fn()
        };

        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(),
                provideHttpClientTesting(),
                ExpenseStore,
                { provide: NotificationService, useValue: notificationServiceMock }
            ]
        });

        store = TestBed.inject(ExpenseStore);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
        vi.useRealTimers();
    });

    it('should load expenses and update state', async () => {
        const loadPromise = store.loadExpenses();

        httpMock.expectOne(`${environment.apiUrl}/expenses?pageNumber=1&pageSize=10`).flush(mockPagedResponse);
        await loadPromise;

        expect(store.expenses()).toEqual(mockExpenses);
        expect(store.isLoading()).toBe(false);
    });

    it('should optimistically delete an expense and show undo notification', async () => {
        vi.useFakeTimers();
        const loadPromise = store.loadExpenses();
        httpMock.expectOne(`${environment.apiUrl}/expenses?pageNumber=1&pageSize=10`).flush(mockPagedResponse);
        await loadPromise;

        const expenseToDelete = mockExpenses[0];
        await store.deleteExpense(expenseToDelete.id);

        expect(store.expenses()).not.toContain(expenseToDelete);
        expect(notificationServiceMock.showUndo).toHaveBeenCalled();

        // Advance time to trigger actual deletion
        vi.advanceTimersByTime(5000);

        const deleteReq = httpMock.expectOne(`${environment.apiUrl}/expenses/${expenseToDelete.id}`);
        expect(deleteReq.request.method).toBe('DELETE');
        deleteReq.flush({});

        await Promise.resolve();
        httpMock.expectOne(`${environment.apiUrl}/expenses/summary`).flush(mockSummary);

        expect(store.expenses()).not.toContain(expenseToDelete);
    });

    it('should undo deletion and restore state', async () => {
        vi.useFakeTimers();
        const loadPromise = store.loadExpenses();
        httpMock.expectOne(`${environment.apiUrl}/expenses?pageNumber=1&pageSize=10`).flush(mockPagedResponse);
        await loadPromise;

        const expenseToDelete = mockExpenses[0];
        await store.deleteExpense(expenseToDelete.id);

        // Act - Undo
        store.undoDelete(expenseToDelete.id);

        expect(store.expenses()).toContain(expenseToDelete);

        // Advance time - should NOT trigger DELETE call
        vi.advanceTimersByTime(5000);
        httpMock.expectNone(`${environment.apiUrl}/expenses/${expenseToDelete.id}`);
    });

    it('should handle pagination and append items for next page', async () => {
        const load1Promise = store.loadExpenses(1, 10);
        httpMock.expectOne(`${environment.apiUrl}/expenses?pageNumber=1&pageSize=10`).flush({
            ...mockPagedResponse,
            hasNextPage: true
        });
        await load1Promise;

        const nextPromise = store.loadNextPage();

        const req = httpMock.expectOne(`${environment.apiUrl}/expenses?pageNumber=2&pageSize=10`);
        req.flush({
            items: [{ id: 3, description: 'Coffee', amount: 5, date: '2024-03-21', categoryName: 'Food' }],
            totalCount: 3,
            pageNumber: 2,
            pageSize: 10,
            totalPages: 1,
            hasPreviousPage: true,
            hasNextPage: false
        });
        await nextPromise;

        expect(store.expenses().length).toBe(3);
        expect(store.expenses()[2].description).toBe('Coffee');
    });
});
