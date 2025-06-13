import { Table, TableState, Updater } from '@tanstack/table-core';

/**
 * Updates the state of a TanStack Table instance using the provided updater function or value.
 *
 * This utility handles the common pattern of state management in TanStack Table where:
 * 1. Get the current state from the table
 * 2. Apply the updater function or use the direct value
 * 3. Sync the new state back to the table via setOptions
 *
 * This is typically used within the `onStateChange` callback to maintain state persistence
 * in vanilla TypeScript implementations without reactive frameworks.
 *
 * @template TData - The type of data in the table rows
 * @param table - The TanStack Table instance to update
 * @param updater - Either a function that transforms the current state to new state,
 *                  or a direct TableState object to set as the new state
 */
export function updateTableState<TData>(table: Table<TData>, updater: Updater<TableState>): void {
    let currentState = table.getState();

    if (typeof updater === 'function') {
        currentState = updater(currentState);
    } else {
        currentState = updater;
    }

    table.setOptions((prev) => ({
        ...prev,
        state: currentState,
    }));
}
