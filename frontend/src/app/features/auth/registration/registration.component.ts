import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { RouterModule } from '@angular/router';
import { AuthStore } from '../../../core/store/auth.store';
import { RegisterDto } from '../../../models/auth.model';
import { passwordMatchValidator } from './password-match.validator';

@Component({
    selector: 'app-registration',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatInputModule,
        MatButtonModule,
        MatFormFieldModule,
        RouterModule
    ],
    templateUrl: './registration.component.html',
    styleUrl: './registration.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegistrationComponent {
    private fb = inject(FormBuilder);
    readonly store = inject(AuthStore);

    isArray = Array.isArray;
    asArray(val: any): string[] { return val as string[]; }

    registerForm = this.fb.nonNullable.group({
        name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
        email: ['', [Validators.required, Validators.email, Validators.maxLength(100)]],
        password: ['', [
            Validators.required,
            Validators.minLength(8),
            Validators.maxLength(64),
            Validators.pattern(/^(?=.*[0-9])(?=.*[!@#$%^&*(),.?":{}|<>]).*$/)
        ]],
        confirmPassword: ['', [Validators.required]]
    }, { validators: passwordMatchValidator });

    getControlErrorMessage(controlName: string): string {
        const control = this.registerForm.get(controlName);
        if (!control || !control.touched) {
            return '';
        }

        if (control.errors?.['required']) return 'This field is required.';
        if (control.errors?.['email']) return 'Please enter a valid email address.';
        if (control.errors?.['minlength']) {
            if (controlName === 'name') return 'Name must be at least 2 characters.';
            return `Too short (min ${control.errors['minlength'].requiredLength} chars).`;
        }
        if (control.errors?.['maxlength']) return 'This field is too long.';
        if (control.errors?.['pattern']) {
            return 'Password must have 1 number and 1 special character (#!*).';
        }

        return '';
    }

    onSubmit() {
        if (this.registerForm.invalid) {
            this.registerForm.markAllAsTouched();
            return;
        }

        const formValue = this.registerForm.getRawValue();
        const request: RegisterDto = {
            name: formValue.name,
            email: formValue.email,
            password: formValue.password,
            confirmPassword: formValue.confirmPassword
        };

        this.store.register(request);
    }
}
