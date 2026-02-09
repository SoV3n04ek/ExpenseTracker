import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { RouterModule } from '@angular/router';
import { AuthStore } from '../../../core/store/auth.store';
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
        name: ['', [Validators.required, Validators.maxLength(50)]],
        email: ['', [Validators.required, Validators.email, Validators.maxLength(100)]],
        password: ['', [
            Validators.required,
            Validators.minLength(8),
            Validators.maxLength(64),
            Validators.pattern(/^(?=.*[0-9])(?=.*[!@#$%^&*(),.?":{}|<>]).*$/)
        ]],
        confirmPassword: ['', [Validators.required]]
    }, { validators: passwordMatchValidator });

    onSubmit() {
        if (this.registerForm.valid) {
            const { confirmPassword, ...registerDto } = this.registerForm.getRawValue();
            this.store.register(registerDto);
        }
    }
}
