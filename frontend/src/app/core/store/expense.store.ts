import { signalStore, withState, withMethods, patchState, withComputed } from '@ngrx/signals';
import { inject, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ExpenseSummaryDto, ExpenseDto, PagedResponse, CreateExpenseDto, CategoryDto, UpdateExpenseDto } from '../../models/expense.model';
import { environment } from '../../../environment/environment';
import { firstValueFrom } from 'rxjs';
import { NotificationService } from '../services/notification.service';

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

// Queue for multi-item undo safety using a signal-wrapped Map
// Key: id (number), Value: { timerId: any, originalItem: ExpenseDto }
const pendingDeletions = signal<Map<number, { timerId: any, originalItem: ExpenseDto }>>(new Map());

export const ExpenseStore = signalStore(
    { providedIn: 'root' },
    withState(initialState),
    withComputed((state) => ({
        expenses: computed(() => state.pagedResponse()?.items ?? []),
        totalPages: computed(() => state.pagedResponse()?.totalPages ?? 0),
        totalCount: computed(() => state.pagedResponse()?.totalCount ?? 0),
        currentPage: computed(() => state.pagedResponse()?.pageNumber ?? 1),
    })),
    withMethods((store, http = inject(HttpClient), notification = inject(NotificationService)) => {
        const loadSummary = async (startDate?: string, endDate?: string) => {
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
        };

        const loadExpenses = async (pageNumber: number = 1, pageSize: number = 10) => {
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
        };

        const loadNextPage = async () => {
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
        };

        const loadCategories = async () => {
            try {
                const categories = await firstValueFrom(
                    http.get<CategoryDto[]>(`${environment.apiUrl}/expenses/categories`)
                );
                patchState(store, { categories });
            } catch (err: any) {
                console.error('Failed to load categories', err);
            }
        };

        const addExpense = async (dto: CreateExpenseDto) => {
            const tempId = Math.floor(Math.random() * -1000);
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

                if (store.pagedResponse()) {
                    const updatedItems = store.pagedResponse()!.items.map(item =>
                        item.id === tempId ? { ...item, id: newExpenseId } : item
                    );
                    patchState(store, {
                        pagedResponse: { ...store.pagedResponse()!, items: updatedItems }
                    });
                }

                await loadSummary();
            } catch (err: any) {
                if (currentResponse) patchState(store, { pagedResponse: currentResponse });
                patchState(store, { error: err.error?.message || 'Failed to add expense' });
                throw err;
            }
        };

        const undoDelete = (id: number) => {
            const currentMap = pendingDeletions();
            const deletion = currentMap.get(id);
            if (!deletion) return;

            // 1. Cancel timer
            clearTimeout(deletion.timerId);

            // 2. Remove from queue
            const newMap = new Map(currentMap);
            newMap.delete(id);
            pendingDeletions.set(newMap);

            // 3. Restore to UI
            const currentResponse = store.pagedResponse();
            if (currentResponse) {
                patchState(store, {
                    pagedResponse: {
                        ...currentResponse,
                        items: [deletion.originalItem, ...currentResponse.items].sort((a, b) =>
                            new Date(b.date).getTime() - new Date(a.date).getTime()),
                        totalCount: currentResponse.totalCount + 1
                    }
                });
            }
        };

        const deleteExpense = async (id: number) => {
            // Guard: Fix ID mapping bug (preventing [object Object])
            if (typeof id !== 'number') {
                console.error('Invalid ID passed to deleteExpense:', id);
                return;
            }

            const currentResponse = store.pagedResponse();
            if (!currentResponse) return;

            const backupItem = currentResponse.items.find(i => i.id === id);
            if (!backupItem) return;

            // 1. Optimistic removal
            patchState(store, {
                pagedResponse: {
                    ...currentResponse,
                    items: currentResponse.items.filter(i => i.id !== id),
                    totalCount: currentResponse.totalCount - 1
                }
            });

            // 2. Schedule actual deletion
            const timerId = setTimeout(async () => {
                const mapAfterTimer = pendingDeletions();
                const newMap = new Map(mapAfterTimer);
                newMap.delete(id);
                pendingDeletions.set(newMap);

                try {
                    // Explicit numeric ID passing using template literal
                    await firstValueFrom(http.delete(`${environment.apiUrl}/expenses/${id}`));
                    await loadSummary();
                } catch (err: any) {
                    // Fail-safe rollback
                    patchState(store, { pagedResponse: currentResponse });
                    patchState(store, { error: err.error?.message || 'Failed to delete expense' });
                }
            }, 5000);

            // 3. Add to pending queue
            const currentMap = pendingDeletions();
            const nextMap = new Map(currentMap);
            nextMap.set(id, { timerId, originalItem: backupItem });
            pendingDeletions.set(nextMap);

            notification.showUndo(
                `Deleted ${backupItem.description}`,
                () => undoDelete(id),
                () => { }
            );
        };

        const updateExpense = async (id: number, dto: UpdateExpenseDto) => {
            patchState(store, { isLoading: true });
            try {
                await firstValueFrom(http.put(`${environment.apiUrl}/expenses/${id}`, dto));

                const currentResponse = store.pagedResponse();
                if (currentResponse) {
                    const updatedItems = currentResponse.items.map(item =>
                        item.id === id
                            ? { ...item, ...dto, date: dto.date }
                            : item
                    );
                    patchState(store, {
                        pagedResponse: { ...currentResponse, items: updatedItems },
                        isLoading: false
                    });
                }

                await loadSummary();
            } catch (err: any) {
                patchState(store, {
                    isLoading: false,
                    error: err.error?.message || 'Failed to update expense'
                });
                throw err;
            }
        };

        return {
            loadSummary,
            loadExpenses,
            loadNextPage,
            loadCategories,
            addExpense,
            deleteExpense,
            undoDelete,
            updateExpense
        };
    })
);
