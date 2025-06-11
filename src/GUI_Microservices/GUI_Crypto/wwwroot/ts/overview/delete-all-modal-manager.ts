import * as bootstrap from 'bootstrap';
import toastr from 'toastr';
import { deleteAllCoins } from './services';
import { getElement } from './utils/dom-utils';
import { TableManager } from './table/table-manager';

export class DeleteAllModalManager {
    private readonly deleteAllModal: bootstrap.Modal;
    private readonly tableManager: TableManager;

    // DOM Elements with suffix naming
    private readonly deleteAllModalElement = getElement('deleteAllConfirmModal', HTMLElement);
    private readonly confirmDeleteAllBtn = getElement('confirmDeleteAllBtn', HTMLButtonElement);
    private readonly cancelDeleteAllBtn = getElement('cancelDeleteAllBtn', HTMLButtonElement);
    private readonly fullPageOverlay = getElement('fullPageOverlay', HTMLElement);
    private readonly modalCloseBtn = getElement('deleteAllModalClose', HTMLButtonElement);

    constructor(tableManager: TableManager) {
        this.tableManager = tableManager;
        this.deleteAllModal = new bootstrap.Modal(this.deleteAllModalElement);
        this.setupEventListeners();
    }

    private setupEventListeners(): void {
        this.confirmDeleteAllBtn.addEventListener('click', () =>
            this.handleDeleteAllConfirmation()
        );
    }

    public showDeleteAllConfirmation(): void {
        this.deleteAllModal.show();
    }

    private async handleDeleteAllConfirmation(): Promise<void> {
        try {
            // Prevent modal from being closed
            this.modalCloseBtn.disabled = true;
            this.confirmDeleteAllBtn.disabled = true;
            this.cancelDeleteAllBtn.disabled = true;
            this.deleteAllModalElement.dataset['bsBackdrop'] = 'static';
            this.deleteAllModalElement.dataset['bsKeyboard'] = 'false';

            // Show progress overlay
            this.fullPageOverlay.classList.remove('d-none');

            const success = await deleteAllCoins();
            if (success) {
                await this.tableManager.refreshTableData();
                this.deleteAllModal.hide();
                toastr.success('All coins deleted successfully');
            } else {
                toastr.error('Failed to delete all coins');
            }
        } catch (error) {
            toastr.error(`Error deleting all coins: ${error}`);
        } finally {
            // Re-enable modal closing and hide overlay
            this.modalCloseBtn.disabled = false;
            this.confirmDeleteAllBtn.disabled = false;
            this.cancelDeleteAllBtn.disabled = false;
            this.deleteAllModalElement.dataset['bsBackdrop'] = 'true';
            this.deleteAllModalElement.dataset['bsKeyboard'] = 'true';
            this.fullPageOverlay.classList.add('d-none');
        }
    }
}
