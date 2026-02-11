import { trigger, transition, style, animate } from '@angular/animations';
import { Component, inject, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SidebarComponent } from '../../shared/components/sidebar/sidebar.component';
import { AuthStore } from '../../core/store/auth.store';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { AddExpenseDialogComponent } from './components/add-expense-dialog/add-expense-dialog.component';
import { EditExpenseDialogComponent } from './components/edit-expense-dialog/edit-expense-dialog.component';
import { ExpenseStore } from '../../core/store/expense.store';
import { UIStore } from '../../core/store/ui.store';
import { ExpenseDto } from '../../models/expense.model';
import { MatMenuModule } from '@angular/material/menu';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatDialogModule, MatIconModule, MatMenuModule],
  animations: [
    trigger('rowAnimation', [
      transition(':leave', [
        style({ opacity: 1, transform: 'translateX(0)' }),
        animate('300ms ease-out', style({ opacity: 0, transform: 'translateX(20px)' }))
      ])
    ])
  ],
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

  openEditExpenseDialog(expense: ExpenseDto): void {
    this.dialog.open(EditExpenseDialogComponent, {
      width: '500px',
      data: expense,
      disableClose: true
    });
  }

  onDeleteExpense(id: number): void {
    this.expenseStore.deleteExpense(id);
  }
}