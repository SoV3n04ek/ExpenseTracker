import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { RouterModule } from '@angular/router';

@Component({
    selector: 'app-register-success',
    standalone: true,
    imports: [CommonModule, MatButtonModule, RouterModule],
    templateUrl: './register-success.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegisterSuccessComponent { }
