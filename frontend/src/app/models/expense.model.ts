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
  categoryId: number;
  categoryName: string;
  amount: number;
  percentage: number;
}

export interface CategoryDto {
  id: number;
  name: string;
}

export interface PagedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}