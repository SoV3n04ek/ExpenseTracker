import { Component, inject, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SidebarComponent } from '../../shared/components/sidebar/sidebar.component';
import { AuthStore } from '../../core/store/auth.store';
import { ExpenseStore } from '../../core/store/expense.store';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, SidebarComponent, MatButtonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent implements OnInit {
  readonly authStore = inject(AuthStore);
  readonly expenseStore = inject(ExpenseStore);

  ngOnInit(): void {
    this.expenseStore.loadSummary();
  }
}