import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';

interface UIState {
    isSidebarCollapsed: boolean;
    isMobileMenuOpen: boolean;
    currency: string;
}

const initialState: UIState = {
    isSidebarCollapsed: false,
    isMobileMenuOpen: false,
    currency: 'USD',
};

export const UIStore = signalStore(
    { providedIn: 'root' },
    withState(initialState),
    withMethods((store) => ({
        toggleSidebar() {
            patchState(store, { isSidebarCollapsed: !store.isSidebarCollapsed() });
        },
        toggleMobileMenu() {
            patchState(store, { isMobileMenuOpen: !store.isMobileMenuOpen() });
        },
        closeMobileMenu() {
            patchState(store, { isMobileMenuOpen: false });
        },
        setSidebarCollapsed(collapsed: boolean) {
            patchState(store, { isSidebarCollapsed: collapsed });
        },
        updateCurrency(currency: string) {
            patchState(store, { currency });
        }
    }))
);
