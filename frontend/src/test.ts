import 'zone.js';
import 'zone.js/testing';
import { getTestBed } from '@angular/core/testing';
import {
    BrowserDynamicTestingModule,
    platformBrowserDynamicTesting,
} from '@angular/platform-browser-dynamic/testing';

// Initialize the Angular testing environment only once
const testBed = getTestBed();
if (!testBed.platform) {
    testBed.initTestEnvironment(
        BrowserDynamicTestingModule,
        platformBrowserDynamicTesting(),
    );
}

// Expose Zone globally for Vitest context
(window as any).Zone = (window as any).Zone || (globalThis as any).Zone;
