import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { RouterModule } from '@angular/router'; // Required for routerLink
import { AuthStore } from '../../../core/store/auth.store';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterModule], // Need this for the links to work!
  templateUrl: './sidebar.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SidebarComponent {
  private readonly store = inject(AuthStore);

  logout() {
    this.store.logout();
  }
}