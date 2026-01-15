import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations'; // Changed this
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { ApplicationConfig, provideZonelessChangeDetection } from '@angular/core'; // Change this

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(), // Modern, high-performance mode
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimations()
  ]
};