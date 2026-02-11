import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { ExpenseStore } from '../../../../core/store/expense.store';
import { ExpenseDto, UpdateExpenseDto } from '../../../../models/expense.model';

@Component({
    selector: 'app-edit-expense-dialog',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatDialogModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        MatDatepickerModule,
        MatNativeDateModule
    ],
    templateUrl: './edit-expense-dialog.component.html',
    styles: [`
        .expense-form { display: flex; flex-direction: column; gap: 1rem; min-width: 350px; padding: 1rem 0; }
        .form-row { display: flex; gap: 1rem; }
        .error-message { color: #f44336; font-size: 0.875rem; margin-top: 0.5rem; }
    `]
})
export class EditExpenseDialogComponent implements OnInit {
    private readonly fb = inject(FormBuilder);
    private readonly dialogRef = inject(MatDialogRef<EditExpenseDialogComponent>);
    readonly data: ExpenseDto = inject(MAT_DIALOG_DATA);
    readonly store = inject(ExpenseStore);

    expenseForm: FormGroup = this.fb.group({
        description: [this.data.description, [Validators.required, Validators.maxLength(100)]],
        amount: [this.data.amount, [Validators.required, Validators.min(0.01)]],
        date: [new Date(this.data.date), Validators.required],
    });

    ngOnInit(): void { }

    onSubmit(): void {
        if (this.expenseForm.valid) {
            const formValue = this.expenseForm.value;
            const dto: UpdateExpenseDto = {
                description: formValue.description,
                amount: formValue.amount,
                date: formValue.date.toISOString()
            };

            this.store.updateExpense(this.data.id, dto)
                .then(() => {
                    this.dialogRef.close(true);
                })
                .catch(err => {
                    console.error('Error updating expense', err);
                });
        }
    }

    onCancel(): void {
        this.dialogRef.close();
    }
}
