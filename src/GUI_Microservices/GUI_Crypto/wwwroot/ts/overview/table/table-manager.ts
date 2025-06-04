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
import { columns } from './table-core';
import { renderMiniCharts } from './mini-chart';
import { fetchCoins, deleteCoin, openChart } from '../services';
import { initializeToastr } from '../../configs/toastr-config';

export class TableManager {
    private readonly table: HTMLTableElement;
    private readonly tableBody: HTMLElement;
    private readonly thead: HTMLElement;
    private readonly searchInput: HTMLInputElement;
    private readonly stablecoinCheckbox: HTMLInputElement;
    private readonly btnConfirmDelete: HTMLButtonElement;

    private tableCore: Table<OverviewCoin>;
    private tableState: TableState = {
        sorting: [],
        columnFilters: [
            {
                id: 'isStablecoin',
                value: false,
            },
        ],
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
        const stablecoinCheckbox = document.getElementById('showStablecoins');
        const btnConfirmDelete = document.getElementById('confirmDeleteBtn');
        const deleteModalElement = document.getElementById('deleteConfirmModal');
        const thead = document.querySelector('#coinTable thead');

        this.table = table as HTMLTableElement;
        this.tableBody = tableBody as HTMLElement;
        this.thead = thead as HTMLElement;
        this.searchInput = searchInput as HTMLInputElement;
        this.stablecoinCheckbox = stablecoinCheckbox as HTMLInputElement;
        this.stablecoinCheckbox.checked = false;
        this.btnConfirmDelete = btnConfirmDelete as HTMLButtonElement;
        this.deleteModal = new bootstrap.Modal(deleteModalElement!);

        const options: TableOptionsResolved<OverviewCoin> = {
            data: [],
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
        this.stablecoinCheckbox.addEventListener('change', () => this.handleStablecoinFilter());
        this.thead.addEventListener('click', (event: any) => {
            const header = event.target.closest('th.sortable');
            if (!header) return;
            this.handleSortingChange(header);
        });
        this.btnConfirmDelete.addEventListener('click', () => this.handleDeleteConfirmation());
    }

    private handleSearch(): void {
        this.tableState.globalFilter = this.searchInput.value.toLowerCase().trim();
        this.updateTableState();
    }

    private handleStablecoinFilter(): void {
        this.tableState.columnFilters = this.stablecoinCheckbox.checked
            ? []
            : [
                  {
                      id: 'isStablecoin',
                      value: false,
                  },
              ];
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

    private createFullChartButton(row: OverviewCoin): HTMLButtonElement {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'btn btn-sm btn-outline-primary';
        button.disabled = !row.tradingPair;

        const iconElement = document.createElement('i');
        iconElement.className = 'fas fa-chart-line';
        button.appendChild(iconElement);

        if (row.tradingPair) {
            button.addEventListener('click', async () => {
                try {
                    await openChart(row);
                } catch (error) {
                    toastr.error(`Failed to open chart: ${error}`);
                }
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
                if (currentPosition === newIndex) return;

                const nextSibling = this.tableBody.children[newIndex] || null;
                if (nextSibling === rowNew) return;
                this.tableBody.insertBefore(rowNew, nextSibling);
            } else {
                rowNew = document.createElement('tr');
                rowNew.setAttribute('data-coin-id', coinId.toString());

                const nextSibling = this.tableBody.children[newIndex] || null;
                this.tableBody.insertBefore(rowNew, nextSibling);
                this.updateRowContent(row, chartsToUpdate, rowNew);
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

    private updateRowContent(
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
                        JSON.stringify(row.original.klineData ?? [])
                    );
                    chartDiv.setAttribute(
                        'data-is-stablecoin',
                        row.original.isStablecoin.toString()
                    );
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
                default:
                    td.textContent = value?.toString() ?? '';
            }

            rowNew.appendChild(td);
        });
    }

    public async refreshTableData(): Promise<void> {
        const coins = await fetchCoins();
        this.tableCore.setOptions((prev) => {
            return {
                ...prev,
                data: coins,
            };
        });
        this.renderTable();
    }

    private updateTableState(): void {
        this.tableCore.setOptions((prev) => ({
            ...prev,
            state: this.tableState,
        }));
        this.renderTable();
    }
}
