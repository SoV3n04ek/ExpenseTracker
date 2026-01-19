import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SidebarComponent } from '../../shared/components/sidebar/sidebar.component'; // 1. Import the class
import { AuthStore } from '../../core/store/auth.store';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  // 2. Add SidebarComponent to this list
  imports: [CommonModule, SidebarComponent, MatButtonModule],
  templateUrl: './dashboard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent {
  readonly store = inject(AuthStore);
}