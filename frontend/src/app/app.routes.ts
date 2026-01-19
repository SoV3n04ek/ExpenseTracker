import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/registration/registration.component').then(m => m.RegistrationComponent)
  },
  {
    path: 'register-success',
    loadComponent: () => import('./features/auth/register-success/register-success.component').then(m => m.RegisterSuccessComponent)
  },
  {
    path: 'dashboard',
    canActivate: [authGuard], // <--- The Guard protects this!
    loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
];