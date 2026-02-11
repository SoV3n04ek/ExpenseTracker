import { Component, inject } from '@angular/core';
import { MatSnackBarRef, MAT_SNACK_BAR_DATA, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatButtonModule } from '@angular/material/button';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'app-undo-snackbar',
    standalone: true,
    imports: [CommonModule, MatSnackBarModule, MatButtonModule],
    template: `
    <div class="flex items-center justify-between gap-4 py-1">
      <span class="text-white">{{ data.message }}</span>
      <button mat-button color="accent" (click)="snackBarRef.dismissWithAction()">
        UNDO
      </button>
    </div>
    <div class="absolute bottom-0 left-0 h-1 bg-amber-400 countdown-bar"></div>
  `,
    styles: [`
    :host {
      display: block;
      position: relative;
    }
    .countdown-bar {
      width: 100%;
      animation: shrink 5s linear forwards;
    }
    @keyframes shrink {
      from { width: 100%; }
      to { width: 0%; }
    }
  `]
})
export class UndoSnackbarComponent {
    readonly data = inject(MAT_SNACK_BAR_DATA);
    readonly snackBarRef = inject(MatSnackBarRef);
}
