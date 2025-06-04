import toastr from 'toastr';

export function validateCoinName(name: string): boolean {
    if (name) return true;

    toastr.warning('Please enter a name for the coin');
    return false;
}
