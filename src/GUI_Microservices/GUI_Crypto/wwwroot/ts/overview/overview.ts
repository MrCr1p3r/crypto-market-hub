import { ModalManager } from './modal-manager';
import { getElement } from './utils/dom-utils';
import { initializeOverviewRealtime } from './realtime-init';
import { TableManager } from './table/table-manager';

export class Overview {
    private readonly modalManager: ModalManager;
    private readonly tableManager: TableManager;

    // DOM Elements
    private readonly massImportBtn = getElement('massImportBtn', HTMLButtonElement);

    constructor() {
        // Initialize TableManager first so it can receive SignalR events
        this.tableManager = new TableManager();

        // Pass table manager to modal manager to avoid duplicate instances
        this.modalManager = new ModalManager(this.tableManager);

        this.setupEventListeners();
    }

    private setupEventListeners(): void {
        // Mass import modal events
        this.massImportBtn.addEventListener('click', () => this.modalManager.showMassImportModal());
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
