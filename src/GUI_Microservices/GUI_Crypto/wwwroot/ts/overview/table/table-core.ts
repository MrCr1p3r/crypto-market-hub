import { ColumnDef } from '@tanstack/table-core';
import { OverviewCoin } from '../interfaces/overview-coin';

// Define column configuration
export const columns: ColumnDef<OverviewCoin>[] = [
    {
        id: 'id',
        accessorKey: 'id',
        header: 'ID',
        enableSorting: true,
        sortingFn: 'alphanumeric',
    },
    {
        id: 'symbol',
        accessorKey: 'symbol',
        header: 'Symbol',
        enableSorting: true,
        sortingFn: 'alphanumeric',
    },
    {
        id: 'name',
        accessorKey: 'name',
        header: 'Name',
        enableSorting: true,
        sortingFn: 'alphanumeric',
    },
    {
        id: 'marketCapUsd',
        accessorKey: 'marketCapUsd',
        header: 'Market Cap',
        enableSorting: true,
        sortingFn: 'basic',
    },
    {
        id: 'price',
        accessorKey: 'priceUsd',
        header: 'Price',
        enableSorting: true,
        sortingFn: 'basic',
    },
    {
        id: 'priceChangePercentage24h',
        accessorKey: 'priceChangePercentage24h',
        header: '24h Change',
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
        accessorKey: 'klineData',
        header: '',
        enableSorting: false,
    },
];
