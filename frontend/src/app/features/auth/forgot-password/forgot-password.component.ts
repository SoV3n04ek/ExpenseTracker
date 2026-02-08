import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { AuthStore } from '../../../core/store/auth.store';

@Component({
    selector: 'app-forgot-password',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        RouterModule,
        MatInputModule,
        MatButtonModule,
        MatFormFieldModule
    ],
    templateUrl: './forgot-password.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ForgotPasswordComponent {
    private fb = inject(FormBuilder);
    readonly store = inject(AuthStore);

    submitted = signal(false);
    readonly Array = Array;

    forgotForm = this.fb.nonNullable.group({
        email: ['', [Validators.required, Validators.email]]
    });

    async onSubmit() {
        if (this.forgotForm.valid) {
            try {
                await this.store.forgotPassword(this.forgotForm.getRawValue().email);
                this.submitted.set(true);
            } catch (err) {
                // Error handled by store
            }
        }
    }

    asArray(val: any): string[] {
        return Array.isArray(val) ? val : [val];
    }
}
