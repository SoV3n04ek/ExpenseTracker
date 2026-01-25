import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { UIStore } from '../../../core/store/ui.store';

@Component({
    selector: 'app-main-layout',
    standalone: true,
    imports: [CommonModule, RouterOutlet, SidebarComponent],
    templateUrl: './main-layout.component.html',
    styleUrl: './main-layout.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class MainLayoutComponent {
    readonly uiStore = inject(UIStore);
}
