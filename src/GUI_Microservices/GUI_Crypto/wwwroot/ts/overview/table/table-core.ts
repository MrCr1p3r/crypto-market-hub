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
        sortingFn: (rowA, rowB, columnId): number => {
            const aValue = rowA.getValue(columnId) as string | null;
            const bValue = rowB.getValue(columnId) as string | null;

            const aNum = aValue ? parseFloat(aValue) : 0;
            const bNum = bValue ? parseFloat(bValue) : 0;

            return aNum - bNum;
        },
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
