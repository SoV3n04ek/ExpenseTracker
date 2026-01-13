import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthStore } from '../store/auth.store';

export const authGuard: CanActivateFn = () => {
  const store = inject(AuthStore);
  const router = inject(Router);

  // Using the Signal from our store!
  if (store.isAuthenticated()) {
    return true;
  }

  // Not logged in? Go to login page.
  router.navigate(['/login']);
  return false;
};