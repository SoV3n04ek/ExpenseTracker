import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { ExpenseStore } from '../../../../core/store/expense.store';

@Component({
    selector: 'app-add-expense-dialog',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatDialogModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        MatSelectModule,
        MatDatepickerModule,
        MatNativeDateModule
    ],
    templateUrl: './add-expense-dialog.component.html',
    styleUrl: './add-expense-dialog.component.css'
})
export class AddExpenseDialogComponent implements OnInit {
    private readonly fb = inject(FormBuilder);
    private readonly dialogRef = inject(MatDialogRef<AddExpenseDialogComponent>);
    readonly store = inject(ExpenseStore);

    expenseForm: FormGroup = this.fb.group({
        description: ['', [Validators.required, Validators.maxLength(100)]],
        amount: [null, [Validators.required, Validators.min(0.01)]],
        date: [new Date(), Validators.required],
        categoryId: [null, Validators.required]
    });

    ngOnInit(): void {
        if (this.store.categories().length === 0) {
            this.store.loadCategories();
        }
    }

    onSubmit(): void {
        if (this.expenseForm.valid) {
            const formValue = this.expenseForm.value;
            const dto = {
                ...formValue,
                date: formValue.date.toISOString()
            };

            this.store.addExpense(dto)
                .then(() => {
                    this.dialogRef.close(true);
                })
                .catch(err => {
                    console.error('Error adding expense', err);
                });
        }
    }

    onCancel(): void {
        this.dialogRef.close();
    }
}
