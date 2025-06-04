import Sortable from 'sortablejs';

export function initializeSortable(
    element: HTMLElement,
    onEndCallback: () => void,
    options: Partial<Sortable.Options> = {}
): Sortable {
    return new Sortable(element, {
        animation: 150,
        ghostClass: 'sortable-ghost',
        onEnd: onEndCallback,
        ...options,
    });
}
