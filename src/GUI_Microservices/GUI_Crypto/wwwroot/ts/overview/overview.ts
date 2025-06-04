import { ModalManager } from './modal-manager';
import { getElement } from './utils/dom-utils';

export class Overview {
    private static instance: Overview | null;
    private readonly modalManager: ModalManager;

    // DOM Elements
    private readonly massImportBtn = getElement('massImportBtn', HTMLButtonElement);

    static {
        Overview.instance = null;
        document.addEventListener('DOMContentLoaded', () => {
            if (!Overview.instance) {
                Overview.instance = new Overview();
            }
        });
    }

    private constructor() {
        this.modalManager = new ModalManager();
        this.setupEventListeners();
    }

    private setupEventListeners(): void {
        // Mass import modal events
        this.massImportBtn.addEventListener('click', () => this.modalManager.showMassImportModal());
    }
}
