import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { DashboardComponent } from './dashboard.component';
import { AuthStore } from '../../core/store/auth.store';
import { ExpenseStore } from '../../core/store/expense.store';
import { UIStore } from '../../core/store/ui.store';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { By } from '@angular/platform-browser';
import { vi } from 'vitest';

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  let mockAuthStore: any;
  let mockExpenseStore: any;
  let mockUIStore: any;
  let mockDialog: any;

  beforeEach(async () => {
    mockAuthStore = {
      userName: signal('Test User'),
      isAuthenticated: signal(true)
    };

    mockExpenseStore = {
      expenses: signal([
        { id: 1, description: 'Coffee', amount: 5, categoryName: 'Food', date: '2024-03-20' }
      ]),
      summary: signal({
        totalAmount: 5,
        categories: [{ categoryName: 'Food', amount: 5, percentage: 100 }]
      }),
      totalCount: signal(1),
      totalPages: signal(1),
      currentPage: signal(1),
      isLoading: signal(false),
      loadSummary: vi.fn(),
      loadExpenses: vi.fn(),
      loadCategories: vi.fn(),
      deleteExpense: vi.fn()
    };

    mockUIStore = {
      selectedCurrency: signal('USD'),
      updateCurrency: vi.fn()
    };

    mockDialog = {
      open: vi.fn().mockReturnValue({
        afterClosed: () => ({ subscribe: vi.fn() })
      })
    };

    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideNoopAnimations(),
        provideRouter([]),
        { provide: AuthStore, useValue: mockAuthStore },
        { provide: ExpenseStore, useValue: mockExpenseStore },
        { provide: UIStore, useValue: mockUIStore },
        { provide: MatDialog, useValue: mockDialog }
      ]
    })
      .overrideComponent(DashboardComponent, {
        set: {
          providers: [
            { provide: AuthStore, useValue: mockAuthStore },
            { provide: ExpenseStore, useValue: mockExpenseStore },
            { provide: UIStore, useValue: mockUIStore },
            { provide: MatDialog, useValue: mockDialog }
          ]
        }
      })
      .compileComponents();

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should welcome the user by name', () => {
    const welcomeText = fixture.debugElement.query(By.css('h1')).nativeElement.textContent;
    expect(welcomeText).toContain('Welcome back, Test User!');
  });

  it('should render the expense list', () => {
    const rows = fixture.debugElement.queryAll(By.css('.expense-row'));
    expect(rows.length).toBe(1);
    expect(rows[0].nativeElement.textContent).toContain('Coffee');
  });

  it('should show the total amount with correct currency', () => {
    const amountText = fixture.debugElement.query(By.css('.total-amount-card .amount')).nativeElement.textContent;
    expect(amountText).toContain('$5.00');
  });

  it('should trigger delete action when delete button is clicked', () => {
    const deleteBtn = fixture.debugElement.query(By.css('button[color="warn"]'));
    deleteBtn.nativeElement.click();
    expect(mockExpenseStore.deleteExpense).toHaveBeenCalledWith(1);
  });

  it('should open dialog when add button is clicked', () => {
    const addBtn = fixture.debugElement.query(By.css('.add-expense-fab'));
    addBtn.nativeElement.click();
    expect(mockDialog.open).toHaveBeenCalled();
  });

  it('should show zero state when no expenses exist', () => {
    mockExpenseStore.totalCount.set(0);
    fixture.detectChanges();

    const zeroState = fixture.debugElement.query(By.css('.zero-state-card'));
    expect(zeroState).toBeTruthy();
    expect(zeroState.nativeElement.textContent).toContain('No expenses found');
  });
});
