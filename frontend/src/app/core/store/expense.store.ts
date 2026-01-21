import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';
import { inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ExpenseSummaryDto } from '../../models/expense.model';
import { environment } from '../../../environment/environment';
import { firstValueFrom } from 'rxjs';

interface ExpenseState {
    summary: ExpenseSummaryDto | null;
    isLoading: boolean;
    error: string | null;
}

const initialState: ExpenseState = {
    summary: null,
    isLoading: false,
    error: null,
};

export const ExpenseStore = signalStore(
    { providedIn: 'root' },
    withState(initialState),
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
    }))
);
