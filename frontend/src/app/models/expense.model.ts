export interface ExpenseDto {
  id: number;
  description: string;
  amount: number;
  date: string; // ISO string from .NET
  categoryName: string;
}

export interface CreateExpenseDto {
  description: string;
  amount: number;
  date: string;
  categoryId: number;
}

export interface ExpenseSummaryDto {
  totalAmount: number;
  categories: CategorySummaryDto[];
}

export interface CategorySummaryDto {
  categoryName: string;
  amount: number;
  percentage: number;
}