// External modules
import * as bootstrap from 'bootstrap';
import toastr from 'toastr';

// Internal modules
import type { ListedCoins } from './interfaces/listed-coins';
import { fetchListedCoins, createCoin } from './services';
import { initializeToastr } from '../utils/toastr-config';
import { TableManager } from './table-manager';

export class Overview {
    private static readonly ITEMS_PER_PAGE = 40;
    private static instance: Overview | null;
    private readonly tableManager: TableManager;

    static {
        Overview.instance = null;
        document.addEventListener('DOMContentLoaded', () => {
            if (!Overview.instance) {
                Overview.instance = new Overview();
            }
        });
    }

    private readonly listedCoins: string[] = [];
    private selectedCoin: string | null = null;
    private addCoinModal: bootstrap.Modal;

    // DOM Elements
    private readonly addNewCoinBtn!: HTMLButtonElement;
    private readonly addCoinModalElement!: HTMLElement;
    private readonly coinSearchInput!: HTMLInputElement;
    private readonly coinNameInput!: HTMLInputElement;
    private readonly addCoinBtn!: HTMLButtonElement;
    private readonly loadingSpinner!: HTMLElement;
    private readonly searchResults!: HTMLElement;
    private readonly selectedCoinSection!: HTMLElement;
    private readonly selectedCoinSymbol!: HTMLElement;

    private constructor() {
        this.initializeElements();
        initializeToastr();
        this.setupEventListeners();
        this.addCoinModal = new bootstrap.Modal(this.addCoinModalElement);
        this.tableManager = new TableManager();
    }

    private initializeElements(): void {
        const elements = {
            addNewCoinBtn: 'addNewCoinBtn',
            addCoinModalElement: 'addCoinModal',
            coinSearchInput: 'coinSearch',
            coinNameInput: 'coinName',
            addCoinBtn: 'addCoinBtn',
            loadingSpinner: 'loadingSpinner',
            searchResults: 'searchResults',
            selectedCoinSection: 'selectedCoin',
            selectedCoinSymbol: 'selectedCoinSymbol'
        } as const;

        // Initialize all DOM elements at once
        Object.entries(elements).forEach(([key, id]) => {
            const element = document.getElementById(id);
            if (!element) {
                throw new Error(`Element with id '${id}' not found`);
            }
            (this as any)[key] = element;
        });
    }

    private setupEventListeners(): void {
        // Add coin modal events
        this.addNewCoinBtn.addEventListener('click', () => this.openAddCoinModal());

        // Input events
        this.coinSearchInput.addEventListener('input', (e) => this.handleSearch((e.target as HTMLInputElement).value));
        this.coinNameInput.addEventListener('input', (e) => this.handleCoinNameInput(e));
        this.addCoinBtn.addEventListener('click', () => this.handleAddCoin());
    }

    private handleCoinNameInput(e: Event): void {
        const input = e.target as HTMLInputElement;
        this.addCoinBtn.disabled = input.value.trim().length === 0;
    }

    private async openAddCoinModal(): Promise<void> {;
        this.addCoinModal.show();
        
        this.showLoading(true);
        
        try {
            const coins = await this.fetchAndProcessCoins();
            this.listedCoins.push(...coins);
            this.displayCoins(coins.slice(0, Overview.ITEMS_PER_PAGE));
        } catch (error) {
            console.error('Error fetching listed coins:', error);
            toastr.error('Failed to fetch available coins');
        } finally {
            this.showLoading(false);
        }
    }

    private async fetchAndProcessCoins(): Promise<string[]> {
        const data: ListedCoins = await fetchListedCoins();
        const allCoins: string[] = [
            ...(data.binanceCoins || []),
            ...(data.bybitCoins || []),
            ...(data.mexcCoins || [])
        ];
        return [...new Set(allCoins)].sort();
    }

    private showLoading(show: boolean): void {
        const elements = [
            { element: this.loadingSpinner, showClass: !show },
            { element: this.searchResults, showClass: show },
        ];

        elements.forEach(({ element, showClass }) => {
            element.classList.toggle('d-none', showClass);
        });

        this.coinSearchInput.disabled = show;
    }

    private handleSearch(searchTerm: string): void {
        const upperTerm = searchTerm.toUpperCase();
        const coins = upperTerm.length < 1 
            ? this.listedCoins 
            : this.listedCoins.filter(coin => coin.toUpperCase().includes(upperTerm));

        this.displayCoins(coins.slice(0, Overview.ITEMS_PER_PAGE));
    }

    private displayCoins(coins: readonly string[]): void {
        if (!this.searchResults) return;

        this.searchResults.innerHTML = '';
        
        if (coins.length === 0) {
            this.appendNoCoinsFoundMessage();
            return;
        }
        coins.forEach(coin => this.appendCoinElement(coin));
    }

    private appendNoCoinsFoundMessage(): void {
        const div = document.createElement('div');
        div.className = 'list-group-item bg-dark text-light text-center';
        div.textContent = 'No coins found';
        this.searchResults.appendChild(div);
    }

    private appendCoinElement(coin: string): void {
        const div = document.createElement('div');
        div.className = 'list-group-item list-group-item-action bg-dark text-light';
        div.textContent = coin;
        div.onclick = () => this.selectCoin(coin);
        this.searchResults.appendChild(div);
    }

    private selectCoin(symbol: string): void {
        this.selectedCoin = symbol;
        this.selectedCoinSymbol.textContent = symbol;
        this.selectedCoinSection.classList.remove('d-none');
        this.addCoinBtn.disabled = true;
        this.searchResults.innerHTML = '';
        this.coinSearchInput.value = symbol;
        this.coinNameInput.value = '';
        this.coinNameInput.focus();
    }

    private async handleAddCoin(): Promise<void> {
        if (!this.selectedCoin) return;

        const name = this.coinNameInput.value.trim();
        if (!this.validateCoinName(name)) return;

        try {
            await this.createNewCoin(name);
        } catch (error: unknown) {
            this.handleAddCoinError(error as { status?: number });
        }
    }

    private validateCoinName(name: string): boolean {
        if (!name) {
            toastr.warning('Please enter a name for the coin');
            return false;
        }
        return true;
    }

    private async createNewCoin(name: string): Promise<void> {
        const response = await createCoin({
            symbol: this.selectedCoin!,
            name
        });

        if (response) {
            toastr.success('Coin added successfully');
            this.addCoinModal.hide();
            this.resetModal();
            this.tableManager.refreshTableData();
        }
    }

    private handleAddCoinError(error: { status?: number }): void {
        if (error.status === 409) {
            toastr.error('This coin already exists in the database');
        } else {
            console.error('Error adding coin:', error);
            toastr.error('Failed to add coin');
        }
    }

    private resetModal(): void {
        const elements = [
            { element: this.coinSearchInput, value: '', disabled: false },
            { element: this.coinNameInput, value: '' }
        ];

        elements.forEach(({ element, value, disabled }) => {
            element.value = value;
            if (disabled !== undefined) element.disabled = disabled;
        });

        this.selectedCoinSection.classList.add('d-none');
        this.searchResults.classList.remove('d-none');
        this.loadingSpinner.classList.add('d-none');
        this.addCoinBtn.disabled = true;
        this.selectedCoin = null;

        if (this.listedCoins.length > 0) {
            this.displayCoins(this.listedCoins.slice(0, Overview.ITEMS_PER_PAGE));
        }
    }
} 
