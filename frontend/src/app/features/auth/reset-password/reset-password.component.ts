import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { AuthStore } from '../../../core/store/auth.store';

export function passwordMatchValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
        const password = control.get('newPassword');
        const confirmPassword = control.get('confirmPassword');

        return password && confirmPassword && password.value !== confirmPassword.value
            ? { passwordMismatch: true }
            : null;
    };
}

@Component({
    selector: 'app-reset-password',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        RouterModule,
        MatInputModule,
        MatButtonModule,
        MatFormFieldModule
    ],
    templateUrl: './reset-password.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ResetPasswordComponent implements OnInit {
    private fb = inject(FormBuilder);
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    readonly store = inject(AuthStore);
    readonly Array = Array;

    asArray(val: any): string[] {
        return Array.isArray(val) ? val : [val];
    }

    // UI state signals
    successMessage = signal<string | null>(null);
    redirectCountdown = signal<number>(3);

    resetForm = this.fb.nonNullable.group({
        email: ['', [Validators.required, Validators.email]],
        token: ['', [Validators.required]],
        newPassword: ['', [
            Validators.required,
            Validators.minLength(8),
            Validators.maxLength(64),
            Validators.pattern(/^(?=.*[0-9])(?=.*[!@#$%^&*(),.?":{}|<>]).*$/)
        ]],
        confirmPassword: ['', [Validators.required]]
    }, { validators: passwordMatchValidator() });

    ngOnInit() {
        const email = this.route.snapshot.queryParamMap.get('email');
        const token = this.route.snapshot.queryParamMap.get('token');

        if (!email || !token) {
            this.router.navigate(['/login'], { queryParams: { error: 'Missing reset information.' } });
            return;
        }

        this.resetForm.patchValue({ email, token });
    }

    async onSubmit() {
        if (this.resetForm.valid) {
            try {
                await this.store.resetPassword(this.resetForm.getRawValue());
                this.successMessage.set('Password reset successfully! Redirecting to login...');

                const interval = setInterval(() => {
                    this.redirectCountdown.update(v => v - 1);
                    if (this.redirectCountdown() <= 0) {
                        clearInterval(interval);
                        this.router.navigate(['/login']);
                    }
                }, 1000);

            } catch (err) {
                // Error is handled by AuthStore and displayed in template via store.error()
            }
        }
    }

    get control() {
        return this.resetForm.controls;
    }
}
