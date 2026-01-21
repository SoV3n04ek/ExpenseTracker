import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthStore } from '../../../core/store/auth.store';
import { ChangeDetectionStrategy } from '@angular/core';

@Component({
    selector: 'app-confirm-email',
    standalone: true,
    imports: [CommonModule, RouterLink],
    templateUrl: './confirm-email.component.html',
    styleUrl: './confirm-email.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmEmailComponent implements OnInit {
    private route = inject(ActivatedRoute);
    readonly authStore = inject(AuthStore);

    ngOnInit(): void {
        const userId = this.route.snapshot.queryParamMap.get('userId');
        const token = this.route.snapshot.queryParamMap.get('token');

        if (userId && token) {
            this.authStore.confirmEmail(userId, token);
        } else {
            // Handle missing params if needed
        }
    }
}
