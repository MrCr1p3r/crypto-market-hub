export function getElement<T extends HTMLElement>(id: string, type: new () => T): T {
    const element = document.getElementById(id);
    if (!(element instanceof type)) {
        throw new Error(`Element with id '${id}' not found or incorrect type`);
    }
    return element;
}

export function createButton(
    className: string,
    icon: string,
    onClick?: () => void,
    disabled = false
): HTMLButtonElement {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = className;
    button.disabled = disabled;

    const iconElement = document.createElement('i');
    iconElement.className = icon;
    button.appendChild(iconElement);

    if (onClick) button.addEventListener('click', onClick);
    return button;
}

export function createHiddenInput(name: string, value: string): HTMLInputElement {
    const input = document.createElement('input');
    input.type = 'hidden';
    input.name = name;
    input.value = value;
    return input;
}

export function appendNoItemsMessage(
    container: HTMLElement,
    message: string = 'No items found',
    className: string = 'list-group-item bg-dark text-light text-center'
): void {
    const div = document.createElement('div');
    div.className = className;
    div.textContent = message;
    container.appendChild(div);
}

export function appendListItem(
    container: HTMLElement,
    text: string,
    onClick: () => void,
    className: string = 'list-group-item list-group-item-action bg-dark text-light'
): void {
    const div = document.createElement('div');
    div.className = className;
    div.textContent = text;
    div.onclick = onClick;
    container.appendChild(div);
}
