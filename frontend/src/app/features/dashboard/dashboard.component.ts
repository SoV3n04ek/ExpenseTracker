import { Component, inject, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SidebarComponent } from '../../shared/components/sidebar/sidebar.component';
import { AuthStore } from '../../core/store/auth.store';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { AddExpenseDialogComponent } from './components/add-expense-dialog/add-expense-dialog.component';
import { ExpenseStore } from '../../core/store/expense.store';
import { UIStore } from '../../core/store/ui.store';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatDialogModule, MatIconModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent implements OnInit {
  readonly authStore = inject(AuthStore);
  readonly expenseStore = inject(ExpenseStore);
  readonly uiStore = inject(UIStore);
  private readonly dialog = inject(MatDialog);

  ngOnInit(): void {
    this.expenseStore.loadSummary();
    this.expenseStore.loadExpenses();
    this.expenseStore.loadCategories();
  }

  openAddExpenseDialog(): void {
    const dialogRef = this.dialog.open(AddExpenseDialogComponent, {
      width: '500px',
      disableClose: true
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        // Refresh summary if needed, though addExpense already does it
      }
    });
  }
}