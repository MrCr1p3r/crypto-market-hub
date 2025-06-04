// External modules
import * as bootstrap from 'bootstrap';
import toastr from 'toastr';
import { getElement, appendNoItemsMessage, appendListItem } from './utils/dom-utils';
import { createCoins } from './services';
import { CoinSearch } from './coin-search';
import { initializeToastr } from '../configs/toastr-config';
import { TableManager } from './table/table-manager';
import { shuffle } from './utils/array-utils';
import { CoinCreationRequest } from './interfaces/coin-creation-request';
import { CandidateCoin } from './interfaces/candidate-coin';

export class ModalManager {
    private static readonly ITEMS_PER_PAGE = 30;
    private readonly selectedCoins: Set<string> = new Set();

    private readonly massImportModal: bootstrap.Modal;

    private readonly massImportModalElement = getElement('massImportModal', HTMLElement);
    private readonly massImportSearchInput = getElement('massImportCoinSearch', HTMLInputElement);
    private readonly loadingSpinner = getElement('loadingSpinner', HTMLElement);
    private readonly massImportSearchResults = getElement('massImportSearchResults', HTMLElement);
    private readonly autoImportSettings = getElement('autoImportSettings', HTMLElement);
    private readonly manualImportSettings = getElement('manualImportSettings', HTMLElement);
    private readonly autoImportCoinCount = getElement('autoImportCoinCount', HTMLInputElement);
    private readonly selectedCoinsContainer = getElement('selectedCoinsContainer', HTMLElement);
    private readonly selectedCount = getElement('selectedCount', HTMLElement);
    private readonly startImportBtn = getElement('startImportBtn', HTMLButtonElement);
    private readonly importTypeRadios = document.getElementsByName(
        'importType'
    ) as NodeListOf<HTMLInputElement>;

    private readonly coinSearch: CoinSearch;
    private readonly tableManager = new TableManager();

    private readonly fullPageOverlay = getElement('fullPageOverlay', HTMLElement);
    private readonly modalCloseBtn = getElement('massImportModalClose', HTMLButtonElement);

    constructor() {
        this.massImportModal = new bootstrap.Modal(this.massImportModalElement);

        this.coinSearch = new CoinSearch();
        this.tableManager = new TableManager();

        this.setupEventListeners();
        initializeToastr();
    }

    private setupEventListeners(): void {
        // Input events
        this.massImportSearchInput.addEventListener('input', (e) =>
            this.handleSearch((e.target as HTMLInputElement).value)
        );

        // Mass import events
        this.importTypeRadios.forEach((radio) => {
            radio.addEventListener('change', () => this.handleImportTypeChange());
        });
        this.startImportBtn.addEventListener('click', () => this.handleStartImport());

        this.massImportModalElement.addEventListener('hidden.bs.modal', () =>
            this.resetMassImportModal()
        );
    }

    public async showMassImportModal(): Promise<void> {
        this.massImportModal.show();
        this.showLoading(true);

        // Initialize import type and checkbox state
        const autoImportRadio = Array.from(this.importTypeRadios).find(
            (radio) => radio.value === 'auto'
        );
        if (autoImportRadio) {
            autoImportRadio.checked = true;
            this.handleImportTypeChange();
        }

        const coins = await this.coinSearch.fetchAndProcessCoins();
        this.displayCoins(coins.slice(0, ModalManager.ITEMS_PER_PAGE));
        this.showLoading(false);
    }

    private showLoading(show: boolean): void {
        const elements = [
            { element: this.loadingSpinner, showClass: !show },
            { element: this.massImportSearchResults, showClass: show },
        ];

        elements.forEach(({ element, showClass }) => {
            element.classList.toggle('d-none', showClass);
        });

        this.massImportSearchInput.disabled = show;
    }

    private handleSearch(searchTerm: string): void {
        const upperTerm = searchTerm.toUpperCase();
        const coins =
            upperTerm.length < 1
                ? this.coinSearch.getListedCoins()
                : this.coinSearch
                      .getListedCoins()
                      .filter((coin) => coin.toUpperCase().includes(upperTerm));

        this.displayCoins(coins.slice(0, ModalManager.ITEMS_PER_PAGE));
    }

    private displayCoins(coins: readonly string[]): void {
        const searchResults = this.massImportSearchResults;
        if (!searchResults) return;

        searchResults.innerHTML = '';

        if (coins.length === 0) {
            appendNoItemsMessage(searchResults, 'No coins found');
            return;
        }

        coins.forEach((coin) =>
            appendListItem(searchResults, coin, () => this.selectCoinForMassImport(coin))
        );
    }

    private selectCoinForMassImport(symbol: string): void {
        if (this.selectedCoins.has(symbol)) {
            this.selectedCoins.delete(symbol);
        } else {
            this.selectedCoins.add(symbol);
        }
        this.updateSelectedCoinsDisplay();
    }

    private updateSelectedCoinsDisplay(): void {
        this.selectedCount.textContent = this.selectedCoins.size.toString();

        this.selectedCoinsContainer.innerHTML = '';
        Array.from(this.selectedCoins)
            .sort()
            .forEach((symbol) => {
                const div = document.createElement('div');
                div.className = 'd-flex justify-content-between align-items-center mb-2';

                const symbolSpan = document.createElement('span');
                symbolSpan.textContent = symbol;
                div.appendChild(symbolSpan);

                const controls = document.createElement('div');
                controls.className = 'd-flex gap-2';

                const removeBtn = document.createElement('button');
                removeBtn.type = 'button';
                removeBtn.className = 'btn btn-sm btn-outline-danger';
                removeBtn.innerHTML = '<i class="fas fa-times"></i>';
                removeBtn.onclick = () => {
                    this.selectedCoins.delete(symbol);
                    this.updateSelectedCoinsDisplay();
                };

                controls.appendChild(removeBtn);
                div.appendChild(controls);
                this.selectedCoinsContainer.appendChild(div);
            });

        this.validateManualSelection();
    }

    private validateManualSelection(): void {
        this.startImportBtn.disabled = this.selectedCoins.size === 0;
    }

    private handleImportTypeChange(): boolean {
        const isManual =
            Array.from(this.importTypeRadios).find((radio) => radio.checked)?.value === 'manual';
        this.autoImportSettings.classList.toggle('d-none', isManual);
        this.manualImportSettings.classList.toggle('d-none', !isManual);
        this.startImportBtn.disabled = this.selectedCoins.size === 0;

        return isManual;
    }

    private async handleStartImport(): Promise<void> {
        const isManual = this.handleImportTypeChange();

        try {
            // Prevent modal from being closed
            this.modalCloseBtn.disabled = true;
            this.massImportModalElement.dataset['bsBackdrop'] = 'static';
            this.massImportModalElement.dataset['bsKeyboard'] = 'false';

            // Show progress
            this.fullPageOverlay.classList.remove('d-none');

            await this.importCoins(isManual);

            this.massImportModal.hide();
            this.tableManager.refreshTableData();
            toastr.success('Mass import completed successfully');
        } catch (error) {
            console.error('Error during mass import:', error);
            toastr.error('Failed to complete mass import');
        } finally {
            // Re-enable modal closing and hide overlay
            this.modalCloseBtn.disabled = false;
            this.massImportModalElement.dataset['bsBackdrop'] = 'true';
            this.massImportModalElement.dataset['bsKeyboard'] = 'true';
            this.fullPageOverlay.classList.add('d-none');
        }
    }

    private async importCoins(isManual: boolean): Promise<void> {
        console.log(this.coinSearch.getSelectedCoinsData(Array.from(this.selectedCoins)));

        const coins = isManual
            ? this.coinSearch
                  .getSelectedCoinsData(Array.from(this.selectedCoins))
                  .map((candidateCoin) => this.convertToCoinCreationRequest(candidateCoin))
            : await this.prepareAutoCoins();

        const result = await createCoins(coins);
        if (!result.isSuccess) {
            throw new Error(result.error);
        }
    }

    private convertToCoinCreationRequest(candidate: CandidateCoin): CoinCreationRequest {
        return {
            id: candidate.id ?? null,
            symbol: candidate.symbol,
            name: candidate.name,
            category: candidate.category ?? null,
            idCoinGecko: candidate.idCoinGecko ?? null,
            tradingPairs: candidate.tradingPairs.map((tp) => ({
                coinQuote: {
                    id: tp.coinQuote.id ?? null,
                    symbol: tp.coinQuote.symbol,
                    name: tp.coinQuote.name,
                    category: tp.coinQuote.category ?? null,
                    idCoinGecko: tp.coinQuote.idCoinGecko ?? null,
                },
                exchanges: tp.exchanges,
            })),
        };
    }

    private async prepareAutoCoins(): Promise<CoinCreationRequest[]> {
        const count = parseInt(this.autoImportCoinCount.value);

        const availableCoins = [...this.coinSearch.getListedCoins()];

        const mainCoins = shuffle(availableCoins).slice(0, Math.min(count, availableCoins.length));

        return this.coinSearch
            .getSelectedCoinsData(mainCoins)
            .map((candidateCoin) => this.convertToCoinCreationRequest(candidateCoin));
    }

    private resetMassImportModal(): void {
        this.selectedCoins.clear();
        this.updateSelectedCoinsDisplay();
        this.startImportBtn.disabled = false;
        this.autoImportCoinCount.value = '150';
        this.massImportSearchInput.value = '';
        this.massImportSearchResults.innerHTML = '';
        this.coinSearch.clearCoins();

        const autoImportRadio = Array.from(this.importTypeRadios).find(
            (radio) => radio.value === 'auto'
        );
        if (autoImportRadio) {
            autoImportRadio.checked = true;
        }
    }
}
