import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UndoSnackbarComponent } from '../../shared/components/undo-snackbar/undo-snackbar.component';

@Injectable({
    providedIn: 'root'
})
export class NotificationService {
    private readonly snackBar = inject(MatSnackBar);

    showUndo(message: string, onUndo: () => void, onDismiss: () => void) {
        const snackBarRef = this.snackBar.openFromComponent(UndoSnackbarComponent, {
            duration: 5000,
            horizontalPosition: 'end',
            verticalPosition: 'bottom',
            panelClass: ['undo-snackbar-panel'],
            data: { message }
        });

        // The custom component triggers the action on the Ref
        snackBarRef.onAction().subscribe(() => {
            onUndo();
        });

        snackBarRef.afterDismissed().subscribe((info) => {
            if (!info.dismissedByAction) {
                onDismiss();
            }
        });
    }
}
