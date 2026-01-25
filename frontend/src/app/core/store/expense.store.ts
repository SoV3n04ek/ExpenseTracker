import { signalStore, withState, withMethods, patchState, withComputed } from '@ngrx/signals';
import { inject, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ExpenseSummaryDto, ExpenseDto, PagedResponse, CreateExpenseDto, CategoryDto } from '../../models/expense.model';
import { environment } from '../../../environment/environment';
import { firstValueFrom } from 'rxjs';

interface ExpenseState {
    summary: ExpenseSummaryDto | null;
    pagedResponse: PagedResponse<ExpenseDto> | null;
    categories: CategoryDto[];
    isLoading: boolean;
    error: string | null;
}

const initialState: ExpenseState = {
    summary: null,
    pagedResponse: null,
    categories: [],
    isLoading: false,
    error: null,
};

export const ExpenseStore = signalStore(
    { providedIn: 'root' },
    withState(initialState),
    withComputed((state) => ({
        expenses: computed(() => state.pagedResponse()?.items ?? []),
        totalPages: computed(() => state.pagedResponse()?.totalPages ?? 0),
        totalCount: computed(() => state.pagedResponse()?.totalCount ?? 0),
        currentPage: computed(() => state.pagedResponse()?.pageNumber ?? 1),
    })),
    withMethods((store, http = inject(HttpClient)) => ({
        async loadSummary(startDate?: string, endDate?: string) {
            patchState(store, { isLoading: true, error: null });
            try {
                const params: any = {};
                if (startDate) params.startDate = startDate;
                if (endDate) params.endDate = endDate;

                const summary = await firstValueFrom(
                    http.get<ExpenseSummaryDto>(`${environment.apiUrl}/expenses/summary`, { params })
                );
                patchState(store, { summary, isLoading: false });
            } catch (err: any) {
                patchState(store, {
                    isLoading: false,
                    error: err.error?.message || 'Failed to load expense summary',
                });
            }
        },

        async loadExpenses(pageNumber: number = 1, pageSize: number = 10) {
            patchState(store, { isLoading: true, error: null });
            try {
                const response = await firstValueFrom(
                    http.get<PagedResponse<ExpenseDto>>(`${environment.apiUrl}/expenses`, {
                        params: { pageNumber, pageSize }
                    })
                );
                patchState(store, { pagedResponse: response, isLoading: false });
            } catch (err: any) {
                patchState(store, {
                    isLoading: false,
                    error: err.error?.message || 'Failed to load expenses',
                });
            }
        },

        async loadNextPage() {
            const currentResponse = store.pagedResponse();
            if (!currentResponse || !currentResponse.hasNextPage || store.isLoading()) return;

            patchState(store, { isLoading: true });
            try {
                const nextPage = currentResponse.pageNumber + 1;
                const response = await firstValueFrom(
                    http.get<PagedResponse<ExpenseDto>>(`${environment.apiUrl}/expenses`, {
                        params: { pageNumber: nextPage, pageSize: currentResponse.pageSize }
                    })
                );

                patchState(store, {
                    pagedResponse: {
                        ...response,
                        items: [...currentResponse.items, ...response.items]
                    },
                    isLoading: false
                });
            } catch (err: any) {
                patchState(store, {
                    isLoading: false,
                    error: err.error?.message || 'Failed to load more expenses',
                });
            }
        },

        async loadCategories() {
            try {
                const categories = await firstValueFrom(
                    http.get<CategoryDto[]>(`${environment.apiUrl}/expenses/categories`)
                );
                patchState(store, { categories });
            } catch (err: any) {
                console.error('Failed to load categories', err);
            }
        },

        async addExpense(dto: CreateExpenseDto) {
            // Optimistic UI Update
            const tempId = Math.floor(Math.random() * -1000); // Negative ID for temp items
            const currentResponse = store.pagedResponse();
            const categoryName = store.categories().find(c => c.id === dto.categoryId)?.name || 'Unknown';

            const optimisticExpense: ExpenseDto = {
                id: tempId,
                description: dto.description,
                amount: dto.amount,
                date: dto.date,
                categoryName: categoryName
            };

            if (currentResponse) {
                patchState(store, {
                    pagedResponse: {
                        ...currentResponse,
                        items: [optimisticExpense, ...currentResponse.items],
                        totalCount: currentResponse.totalCount + 1
                    }
                });
            }

            try {
                const newExpenseId = await firstValueFrom(
                    http.post<number>(`${environment.apiUrl}/expenses`, dto)
                );

                // Replace optimistic item with actual one if we wanted to be super precise, 
                // but usually a re-fetch or just updating the ID is enough.
                // For simplicity here, let's just refresh the summary and partial update.
                if (store.pagedResponse()) {
                    const updatedItems = store.pagedResponse()!.items.map(item =>
                        item.id === tempId ? { ...item, id: newExpenseId } : item
                    );
                    patchState(store, {
                        pagedResponse: { ...store.pagedResponse()!, items: updatedItems }
                    });
                }

                // Also refresh summary to keep totals in sync
                this.loadSummary();
            } catch (err: any) {
                // Rollback on error
                if (currentResponse) {
                    patchState(store, { pagedResponse: currentResponse });
                }
                patchState(store, { error: err.error?.message || 'Failed to add expense' });
                throw err;
            }
        }
    }))
);
