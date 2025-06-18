import toastr from 'toastr';
import $ from 'jquery';

/**
 * Manages cache warmup state and UI interactions
 */
export class CacheWarmupManager {
    private static readonly CACHE_WARMUP_TOAST_CLASS = 'toast-cache-warmup';

    private readonly importButton: HTMLButtonElement;

    constructor(importButton: HTMLButtonElement) {
        this.importButton = importButton;
        this.setupEventListeners();
        this.initializeButtonState();
    }

    /**
     * Setup event listeners for cache warmup completion
     */
    private setupEventListeners(): void {
        document.addEventListener('cacheWarmupCompleted', () => {
            this.handleCacheWarmupCompleted();
        });
    }

    /**
     * Initialize button state - always start disabled and show loading toast
     * Server will notify via SignalR if cache is already warmed up
     */
    private initializeButtonState(): void {
        this.disableImportButton();
        this.showCacheWarmupToast();
    }

    /**
     * Handle cache warmup completion event
     */
    private handleCacheWarmupCompleted(): void {
        // Enable import button
        this.enableImportButton();

        // Dismiss loading toast and show success
        this.dismissCacheWarmupToast();
        this.showCacheWarmupSuccessToast();
    }

    /**
     * Disable import button with loading state
     */
    private disableImportButton(): void {
        this.importButton.disabled = true;
        this.importButton.setAttribute('title', 'Import is disabled while caches are warming up');
    }

    /**
     * Enable import button with normal state
     */
    private enableImportButton(): void {
        this.importButton.disabled = false;
        this.importButton.removeAttribute('title');
    }

    /**
     * Show persistent cache warmup loading toast
     */
    private showCacheWarmupToast(): void {
        const toastHtml = `
            <div class="d-flex align-items-center">
                <div class="spinner-border spinner-border-sm text-light me-3" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <div>
                    <strong>Warming up caches...</strong><br>
                    <small>Coins import will be enabled once this completes</small>
                </div>
            </div>
        `;

        toastr.info(toastHtml, '', {
            timeOut: 0, // Persistent
            extendedTimeOut: 0,
            closeButton: false,
            tapToDismiss: false,
            positionClass: 'toast-top-right',
            toastClass: CacheWarmupManager.CACHE_WARMUP_TOAST_CLASS,
        });
    }

    /**
     * Dismiss the cache warmup loading toast
     */
    private dismissCacheWarmupToast(): void {
        const toastElement = document.getElementsByClassName(
            CacheWarmupManager.CACHE_WARMUP_TOAST_CLASS
        )[0] as HTMLElement;
        if (toastElement) {
            toastr.clear($(toastElement));
        }
    }

    /**
     * Show success toast when cache warmup completes
     */
    private showCacheWarmupSuccessToast(): void {
        toastr.success('Cache warmup completed! Coin import is now available.', 'Ready to Import', {
            timeOut: 5000,
            positionClass: 'toast-top-right',
        });
    }
}
