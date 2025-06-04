import { fetchCandidateCoins } from './services';
import { CandidateCoin } from './interfaces/candidate-coin';

export class CoinSearch {
    private static readonly ITEMS_PER_PAGE = 40;
    private readonly listedCoins: string[] = [];
    private readonly coinLookup: Map<string, CandidateCoin> = new Map();

    public async fetchAndProcessCoins(): Promise<string[]> {
        const candidateCoins: CandidateCoin[] = await fetchCandidateCoins();
        this.listedCoins.length = 0;
        this.coinLookup.clear();

        // Create display strings and build lookup map
        const displayStrings = candidateCoins.map((coin) => {
            const displayString = `${coin.name} (${coin.symbol})`;
            this.coinLookup.set(displayString, coin);
            return displayString;
        });

        // Remove duplicates and sort
        this.listedCoins.push(...[...new Set(displayStrings)].sort());
        return this.listedCoins;
    }

    public handleSearch(searchTerm: string): string[] {
        const upperTerm = searchTerm.toUpperCase();
        const coins =
            upperTerm.length < 1
                ? this.listedCoins
                : this.listedCoins.filter((coin) => coin.toUpperCase().includes(upperTerm));

        return coins.slice(0, CoinSearch.ITEMS_PER_PAGE);
    }

    public getCoinByDisplayString(displayString: string): CandidateCoin | undefined {
        return this.coinLookup.get(displayString);
    }

    public getSelectedCoinsData(selectedDisplayStrings: string[]): CandidateCoin[] {
        return selectedDisplayStrings
            .map((str) => this.coinLookup.get(str))
            .filter((coin) => coin !== undefined);
    }

    public clearCoins(): void {
        this.listedCoins.length = 0;
        this.coinLookup.clear();
    }

    public getListedCoins(): readonly string[] {
        return this.listedCoins;
    }
}
