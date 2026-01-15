import { Component, inject,  } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthStore } from './core/store/auth.store';
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `<router-outlet></router-outlet>`,
  styles: [`
    :host {
      display: block;
      height: 100vh;
      width: 100vw;
    }
  `]
})
export class AppComponent {
  readonly store = inject(AuthStore);

  constructor() {
    // Advanced Tip: Use a temporary effect to watch the store in the console
    console.log('AuthStore Initialized:', this.store.user());
  }
}