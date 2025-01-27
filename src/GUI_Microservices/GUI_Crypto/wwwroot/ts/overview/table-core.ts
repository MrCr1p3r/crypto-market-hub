import { ColumnDef } from '@tanstack/table-core';
import { OverviewCoin } from './interfaces/coin-overview';

// Define column configuration
export const columns: ColumnDef<OverviewCoin>[] = [
    {
        id: 'id',
        accessorKey: 'id',
        header: 'ID',
        enableSorting: true,
        sortingFn: 'alphanumeric'
    },
    {
        id: 'symbol',
        accessorKey: 'symbol',
        header: 'Symbol',
        enableSorting: true,
        sortingFn: 'alphanumeric'
    },
    {
        id: 'name',
        accessorKey: 'name',
        header: 'Name',
        enableSorting: true,
        sortingFn: 'alphanumeric'
    },
    {
        id: 'price',
        header: 'Price',
        enableSorting: true,
        sortingFn: 'basic',
        accessorFn: (row) => row.klineData[row.klineData.length - 1]?.closePrice.toFixed(2) ?? 'N/A',
    },
    {
        id: 'isStablecoin',
        accessorKey: 'isStablecoin',
        header: 'Stablecoin',
        enableSorting: true,
        sortingFn: 'basic',
    },
    {
        id: 'chart',
        accessorKey: 'klineData',
        header: 'Chart',
        enableSorting: false,
    },
    {
        id: 'actions',
        accessorKey: 'tradingPair',
        header: '',
        enableSorting: false,
    }
];
