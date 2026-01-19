import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthStore } from './core/store/auth.store';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `<router-outlet></router-outlet>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent {
  readonly store = inject(AuthStore);
  constructor() {
    console.log('AuthStore Initialized:', this.store.user());
  }
}