import { ImportModalManager } from './import-modal-manager';
import { DeleteAllModalManager } from './delete-all-modal-manager';
import { getElement } from './utils/dom-utils';
import { initializeOverviewRealtime } from './realtime-init';
import { TableManager } from './table/table-manager';

export class Overview {
    private readonly modalManager: ImportModalManager;
    private readonly deleteAllModalManager: DeleteAllModalManager;
    private readonly tableManager: TableManager;

    // DOM Elements with suffix naming
    private readonly massImportBtn = getElement('massImportBtn', HTMLButtonElement);
    private readonly deleteAllBtn = getElement('deleteAllBtn', HTMLButtonElement);

    constructor() {
        // Initialize TableManager first so it can receive SignalR events
        this.tableManager = new TableManager();

        // Pass table manager to modal managers to avoid duplicate instances
        this.modalManager = new ImportModalManager(this.tableManager);
        this.deleteAllModalManager = new DeleteAllModalManager(this.tableManager);

        this.setupEventListeners();
    }

    private setupEventListeners(): void {
        // Modal events
        this.massImportBtn.addEventListener('click', () => this.modalManager.showMassImportModal());
        this.deleteAllBtn.addEventListener('click', () =>
            this.deleteAllModalManager.showDeleteAllConfirmation()
        );
    }

    /**
     * Initialize real-time functionality
     */
    public async initializeRealtime(): Promise<void> {
        await initializeOverviewRealtime(this.tableManager);
    }
}

// Initialize when DOM is ready
let overviewInstance: Overview | null = null;

document.addEventListener('DOMContentLoaded', async () => {
    if (!overviewInstance) {
        overviewInstance = new Overview();
        // Initialize real-time connection after overview is ready
        await overviewInstance.initializeRealtime();
    }
});
