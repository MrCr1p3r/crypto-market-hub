import * as bootstrap from 'bootstrap';
import toastr from 'toastr';

import {
    Table,
    createTable,
    Row,
    TableOptionsResolved,
    getCoreRowModel,
    getSortedRowModel,
    getFilteredRowModel,
    type TableState,
} from '@tanstack/table-core';

import { OverviewCoin } from '../interfaces/overview-coin';
import { CoinMarketData } from '../interfaces/coin-market-data';
import { columns } from './table-core';
import { destroyAllChartsForRerender, renderMiniCharts } from './mini-chart';
import { fetchCoins, deleteCoin } from '../services';
import { initializeToastr } from '../../configs/toastr-config';
import { KlineDataUpdate } from 'realtime/interfaces/kline-signalr';

export class TableManager {
    private readonly table: HTMLTableElement;
    private readonly tableBody: HTMLElement;
    private readonly thead: HTMLElement;
    private readonly searchInput: HTMLInputElement;
    private readonly confirmDeleteBtn: HTMLButtonElement;

    private tableCore: Table<OverviewCoin>;
    private currentCoins: OverviewCoin[] = [];
    private tableState: TableState = {
        sorting: [],
        columnFilters: [],
        globalFilter: '',
        columnVisibility: {},
        columnOrder: [],
        columnPinning: {},
        rowSelection: {},
        expanded: {},
        grouping: [],
        columnSizing: {},
        rowPinning: {},
        columnSizingInfo: {
            isResizingColumn: false,
            startOffset: null,
            startSize: null,
            columnSizingStart: [],
            deltaOffset: 0,
            deltaPercentage: 0,
        },
        pagination: { pageIndex: 0, pageSize: 10 },
    };

    private deleteModal: bootstrap.Modal;
    private coinToDelete: { id: number; symbol: string } | null = null;

    constructor() {
        const table = document.getElementById('coinTable');
        const tableBody = document.getElementById('coinTableBody');
        const searchInput = document.getElementById('tableSearch');
        const confirmDeleteBtn = document.getElementById('confirmDeleteBtn');
        const deleteModalElement = document.getElementById('deleteConfirmModal');
        const thead = document.querySelector('#coinTable thead');

        this.table = table as HTMLTableElement;
        this.tableBody = tableBody as HTMLElement;
        this.thead = thead as HTMLElement;
        this.searchInput = searchInput as HTMLInputElement;
        this.confirmDeleteBtn = confirmDeleteBtn as HTMLButtonElement;
        this.deleteModal = new bootstrap.Modal(deleteModalElement!);

        const options: TableOptionsResolved<OverviewCoin> = {
            data: this.currentCoins,
            columns,
            getCoreRowModel: getCoreRowModel(),
            getSortedRowModel: getSortedRowModel(),
            getFilteredRowModel: getFilteredRowModel(),
            state: this.tableState,
            onStateChange: () => {},
            enableSorting: true,
            enableFilters: true,
            renderFallbackValue: null,
        };

        this.tableCore = createTable(options);

        this.setupEventListeners();
        this.refreshTableData();
        initializeToastr();
    }

    private setupEventListeners(): void {
        this.searchInput.addEventListener('input', () => this.handleSearch());
        this.thead.addEventListener('click', (event: MouseEvent) => {
            const header = (event.target as HTMLElement)?.closest('th.sortable');
            if (!header) return;
            this.handleSortingChange(header as HTMLElement);
        });
        this.confirmDeleteBtn.addEventListener('click', () => this.handleDeleteConfirmation());
    }

    private handleSearch(): void {
        this.tableState.globalFilter = this.searchInput.value.toLowerCase().trim();
        this.updateTableState();
    }

    private handleSortingChange(target: HTMLElement): void {
        const columnId = target.getAttribute('data-column-id');
        const currentSort = this.tableCore.getState().sorting;
        const currentSortItem = currentSort?.[0];
        const isDesc = currentSortItem?.id === columnId && !currentSortItem?.desc;

        this.tableState.sorting = [{ id: columnId!, desc: isDesc }];
        this.updateTableState();
        this.updateSortIcons();
    }

    private async handleDeleteConfirmation(): Promise<void> {
        if (!this.coinToDelete) return;
        await deleteCoin(this.coinToDelete.id);
        await this.refreshTableData();
        this.deleteModal.hide();
        this.coinToDelete = null;
        toastr.success('Coin deleted successfully');
    }

    private updateSortIcons(): void {
        this.table.querySelectorAll('th[data-column-id] i').forEach((icon) => {
            icon.className = 'fas fa-sort';
        });

        const currentSortingState = this.tableCore.getState().sorting;
        if (currentSortingState === undefined) return;

        const currentSort = currentSortingState[0];
        if (!currentSort) return;

        const header = this.table.querySelector(`th[data-column-id="${currentSort.id}"]`);
        const icon = header?.querySelector('i');
        if (!icon) return;
        icon.className = `fas fa-sort-${currentSort.desc ? 'down' : 'up'}`;
    }

    private createCoinDeleteButton(): HTMLButtonElement {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'btn btn-sm btn-outline-danger';

        const iconElement = document.createElement('i');
        iconElement.className = 'fas fa-trash';
        button.appendChild(iconElement);

        return button;
    }

    private createFullChartButton(coin: OverviewCoin): HTMLButtonElement {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'btn btn-sm btn-outline-primary';
        button.disabled = !coin.klineData?.tradingPair;

        const iconElement = document.createElement('i');
        iconElement.className = 'fas fa-chart-line';
        button.appendChild(iconElement);

        if (coin.klineData?.tradingPair) {
            button.addEventListener('click', () => {
                window.open(`/chart/${coin.id}/${coin.klineData!.tradingPair!.id}`, '_blank');
            });
        }

        return button;
    }

    private confirmDelete(id: number, symbol: string): void {
        this.coinToDelete = { id, symbol };
        const symbolSpan = document.getElementById('deleteCoinSymbol');
        if (symbolSpan) symbolSpan.textContent = symbol;
        this.deleteModal.show();
    }

    private renderTable(): void {
        const processedRows = this.tableCore.getRowModel().rows;
        const existingRows = Array.from(this.tableBody.children) as HTMLTableRowElement[];
        const chartsToUpdate: Element[] = [];

        // Create a map of existing rows by coin ID for quick lookup
        const existingRowsMap = new Map<number, HTMLTableRowElement>();
        const currentPositions = new Map<number, number>();
        existingRows.forEach((row, index) => {
            const coinId = Number(row.getAttribute('data-coin-id'));
            existingRowsMap.set(coinId, row);
            currentPositions.set(coinId, index);
        });

        processedRows.forEach((row: Row<OverviewCoin>, newIndex: number) => {
            const coinId = row.original.id;
            let rowNew: HTMLTableRowElement;

            if (existingRowsMap.has(coinId)) {
                rowNew = existingRowsMap.get(coinId)!;
                existingRowsMap.delete(coinId);

                const currentPosition = currentPositions.get(coinId);
                const needsRepositioning = currentPosition !== newIndex;

                if (needsRepositioning) {
                    const nextSibling = this.tableBody.children[newIndex] || null;
                    if (nextSibling !== rowNew) {
                        this.tableBody.insertBefore(rowNew, nextSibling);
                    }
                }

                // Update existing row content with new data
                this.updateExistingRowContent(row, rowNew);
            } else {
                rowNew = document.createElement('tr');
                rowNew.setAttribute('data-coin-id', coinId.toString());

                const nextSibling = this.tableBody.children[newIndex] || null;
                this.tableBody.insertBefore(rowNew, nextSibling);
                this.updateNewRowContent(row, chartsToUpdate, rowNew);
            }
        });

        existingRowsMap.forEach((row) => {
            this.tableBody.removeChild(row);
        });

        // Instead of rendering all charts immediately, set up intersection observer
        this.setupChartObservers(chartsToUpdate);
    }

    private setupChartObservers(charts: Element[]): void {
        const options = {
            root: null, // use viewport
            rootMargin: '50px', // start loading slightly before they come into view
            threshold: 0.1, // trigger when even 10% of the element is visible
        };

        const observer = new IntersectionObserver((entries) => {
            entries.forEach((entry) => {
                if (entry.isIntersecting) {
                    const chartDiv = entry.target as HTMLElement;
                    renderMiniCharts([chartDiv]);
                    observer.unobserve(chartDiv); // Stop observing once rendered
                }
            });
        }, options);

        charts.forEach((chart) => observer.observe(chart));
    }

    private updateExistingRowContent(
        row: Row<OverviewCoin>,
        existingRow: HTMLTableRowElement
    ): void {
        // Update only the data-driven cells, not the chart or actions
        const cells = existingRow.children;

        this.tableCore.getAllColumns().forEach((column, index) => {
            const td = cells[index] as HTMLTableCellElement;
            if (!td) return;

            const value = row.getValue(column.id);

            switch (column.id) {
                case 'chart':
                case 'actions':
                    // Skip chart and actions - they don't need real-time updates
                    break;
                case 'marketCapUsd': {
                    const marketCap = value as number | null;
                    if (marketCap) {
                        if (marketCap >= 1_000_000_000) {
                            td.textContent = `$${(marketCap / 1_000_000_000).toFixed(2)}B`;
                        } else if (marketCap >= 1_000_000) {
                            td.textContent = `$${(marketCap / 1_000_000).toFixed(2)}M`;
                        } else {
                            td.textContent = `$${marketCap.toLocaleString()}`;
                        }
                    } else {
                        td.textContent = 'N/A';
                    }
                    break;
                }
                case 'price': {
                    const price = row.original.priceUsd;
                    if (price) {
                        const priceNum = parseFloat(price);
                        td.textContent = `$${priceNum.toFixed(priceNum < 1 ? 6 : 2)}`;
                    } else {
                        td.textContent = 'N/A';
                    }
                    break;
                }
                case 'priceChangePercentage24h': {
                    const priceChange = value as number | null;
                    if (priceChange !== null) {
                        td.textContent = `${priceChange >= 0 ? '+' : ''}${priceChange.toFixed(2)}%`;
                        td.className = priceChange >= 0 ? 'text-success' : 'text-danger';
                    } else {
                        td.textContent = 'N/A';
                        td.className = '';
                    }
                    break;
                }
                default:
                    // Update other text-based columns
                    td.textContent = value?.toString() ?? 'N/A';
            }
        });
    }

    private updateNewRowContent(
        row: Row<OverviewCoin>,
        chartsToUpdate: Element[],
        rowNew: HTMLTableRowElement
    ): void {
        this.tableCore.getAllColumns().forEach((column) => {
            const td = document.createElement('td');
            const value = row.getValue(column.id);

            switch (column.id) {
                case 'chart': {
                    td.className = 'd-flex align-items-center justify-content-center';
                    const chartDiv = document.createElement('div');
                    chartDiv.className = 'mini-chart not-rendered'; // Add not-rendered class
                    chartDiv.setAttribute(
                        'data-kline-data',
                        JSON.stringify(row.original.klineData?.klines ?? [])
                    );
                    chartDiv.setAttribute(
                        'data-is-stablecoin',
                        (row.original.category === 1).toString() // 1 is Stablecoin enum value
                    );
                    if (row.original.klineData?.tradingPair?.id) {
                        chartDiv.setAttribute(
                            'data-trading-pair-id',
                            row.original.klineData.tradingPair.id.toString()
                        );
                    }
                    td.appendChild(chartDiv);
                    chartsToUpdate.push(chartDiv);
                    break;
                }
                case 'actions': {
                    const actionsContainer = document.createElement('div');
                    actionsContainer.className = 'd-flex gap-2 justify-content-end';
                    actionsContainer.appendChild(this.createFullChartButton(row.original));
                    const deleteButton = this.createCoinDeleteButton();
                    deleteButton.addEventListener('click', () =>
                        this.confirmDelete(row.original.id, row.original.symbol)
                    );
                    actionsContainer.appendChild(deleteButton);
                    td.appendChild(actionsContainer);
                    break;
                }
                case 'marketCapUsd': {
                    const marketCap = value as number | null;
                    if (marketCap) {
                        // Format market cap in billions/millions
                        if (marketCap >= 1_000_000_000) {
                            td.textContent = `$${(marketCap / 1_000_000_000).toFixed(2)}B`;
                        } else if (marketCap >= 1_000_000) {
                            td.textContent = `$${(marketCap / 1_000_000).toFixed(2)}M`;
                        } else {
                            td.textContent = `$${marketCap.toLocaleString()}`;
                        }
                    } else {
                        td.textContent = 'N/A';
                    }
                    break;
                }
                case 'price': {
                    const price = row.original.priceUsd;
                    td.className = 'price-cell';
                    if (price) {
                        const priceNum = parseFloat(price);
                        td.textContent = `$${priceNum.toFixed(priceNum < 1 ? 6 : 2)}`;
                    } else {
                        td.textContent = 'N/A';
                    }
                    break;
                }
                case 'priceChangePercentage24h': {
                    const priceChange = value as number | null;
                    if (priceChange !== null) {
                        td.textContent = `${priceChange >= 0 ? '+' : ''}${priceChange.toFixed(2)}%`;
                        td.className = priceChange >= 0 ? 'text-success' : 'text-danger';
                    } else {
                        td.textContent = 'N/A';
                    }
                    break;
                }
                default:
                    td.textContent = value?.toString() ?? 'N/A';
            }

            rowNew.appendChild(td);
        });
    }

    public async refreshTableData(): Promise<void> {
        const coins = await fetchCoins();
        this.currentCoins = coins;
        this.tableCore.setOptions((prev) => {
            return {
                ...prev,
                data: this.currentCoins,
            };
        });
        this.renderTable();
    }

    /**
     * Bulk update multiple coins (for batch updates)
     */
    public updateCoinsMarketData(updates: CoinMarketData[]): void {
        const updatedCoins: Array<{ coinId: number; isRise: boolean }> = [];
        let hasAnyChanges = false;

        // First, update all coin data without rendering
        updates.forEach((marketData) => {
            const coinIndex = this.currentCoins.findIndex((coin) => coin.id === marketData.id);
            if (coinIndex === -1) {
                return;
            }

            const originalCoin = this.currentCoins[coinIndex];
            if (!originalCoin) {
                console.error(`Coin at index ${coinIndex} is undefined`);
                return;
            }

            // Create a copy with updates
            const updatedCoin: OverviewCoin = { ...originalCoin };
            let hasChanges = false;
            let isRise: boolean | null = null;

            // Update price if provided
            if (marketData.priceUsd !== undefined && updatedCoin.priceUsd !== marketData.priceUsd) {
                if (updatedCoin.priceUsd !== null) {
                    isRise = marketData.priceUsd > updatedCoin.priceUsd;
                }
                updatedCoin.priceUsd = marketData.priceUsd;
                hasChanges = true;
            }

            // Update market cap if provided
            if (
                marketData.marketCapUsd !== undefined &&
                updatedCoin.marketCapUsd !== marketData.marketCapUsd
            ) {
                updatedCoin.marketCapUsd = marketData.marketCapUsd;
                hasChanges = true;
            }

            // Update 24h change if provided
            if (
                marketData.priceChangePercentage24h !== undefined &&
                updatedCoin.priceChangePercentage24h !== marketData.priceChangePercentage24h
            ) {
                updatedCoin.priceChangePercentage24h = marketData.priceChangePercentage24h;
                hasChanges = true;
            }

            if (hasChanges) {
                this.currentCoins[coinIndex] = updatedCoin;
                hasAnyChanges = true;

                if (isRise !== null) {
                    updatedCoins.push({ coinId: marketData.id, isRise: isRise });
                }
            }
        });

        // Render table once if there were any changes
        if (hasAnyChanges) {
            this.tableCore.setOptions((prev) => ({
                ...prev,
                data: [...this.currentCoins],
            }));
            this.renderTable();

            // Add visual indicators for all updated coins at once
            updatedCoins.forEach(({ coinId, isRise }) => {
                this.addUpdateIndicator(coinId, isRise);
            });
        }
    }

    /**
     * Add visual update indicator to the price cell only
     */
    private addUpdateIndicator(coinId: number, isRise: boolean): void {
        const row = this.tableBody.querySelector(
            `tr[data-coin-id="${coinId}"]`
        ) as HTMLTableRowElement;
        if (!row) return;

        // Find the price cell specifically
        const priceCell = row.querySelector('.price-cell') as HTMLTableCellElement;
        if (!priceCell) {
            console.warn(`Price cell not found for coin ${coinId}`);
            return;
        }

        const addedClass = isRise ? 'coin-price-rise' : 'coin-price-fall';
        priceCell.classList.add(addedClass);

        // Remove the class after animation completes
        const onAnimEnd = (e: AnimationEvent): void => {
            if (e.animationName === (isRise ? 'priceRiseFlash' : 'priceFallFlash')) {
                priceCell.classList.remove(addedClass);
                priceCell.removeEventListener('animationend', onAnimEnd);
            }
        };

        priceCell.addEventListener('animationend', onAnimEnd);
    }

    private updateTableState(): void {
        this.tableCore.setOptions((prev) => ({
            ...prev,
            state: this.tableState,
        }));
        this.renderTable();
    }

    /**
     * Update kline data for multiple trading pairs and refresh charts
     */
    public updateKlineData(klineDataUpdates: KlineDataUpdate[]): void {
        let hasAnyUpdates = false;

        // Update kline data in current coins
        klineDataUpdates.forEach((klineUpdate) => {
            const coinWithTradingPair = this.currentCoins.find(
                (coin) => coin.klineData?.tradingPair?.id === klineUpdate.idTradingPair
            );

            if (coinWithTradingPair && coinWithTradingPair.klineData) {
                coinWithTradingPair.klineData.klines = klineUpdate.klines;
                hasAnyUpdates = true;

                // Update the data attribute in the DOM
                const chartElement = document.querySelector(
                    `[data-trading-pair-id="${klineUpdate.idTradingPair}"] .mini-chart`
                ) as HTMLElement;
                if (chartElement) {
                    chartElement.setAttribute(
                        'data-kline-data',
                        JSON.stringify(klineUpdate.klines)
                    );
                }
            }
        });

        // If any kline data was updated, refresh all charts
        if (hasAnyUpdates) {
            const elementsToReobserve = destroyAllChartsForRerender();
            if (elementsToReobserve.length > 0) {
                this.setupChartObservers(elementsToReobserve);
            }
        }
    }
}
