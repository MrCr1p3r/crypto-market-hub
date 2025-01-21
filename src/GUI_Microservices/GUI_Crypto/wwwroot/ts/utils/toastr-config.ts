import toastr from 'toastr';

export interface ToastrOptions {
    readonly closeButton: boolean;
    readonly progressBar: boolean;
    readonly positionClass: string;
    readonly timeOut: number;
}

export const DEFAULT_TOASTR_OPTIONS: ToastrOptions = {
    closeButton: true,
    progressBar: true,
    positionClass: "toast-top-right",
    timeOut: 3000
};

export function initializeToastr(options: Partial<ToastrOptions> = {}): void {
    toastr.options = {
        ...DEFAULT_TOASTR_OPTIONS,
        ...options
    };
} 
