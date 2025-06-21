import { KlineData } from './interfaces/kline-data';
import { KlineDataRequest, ExchangeKlineInterval, Exchange } from './interfaces/kline-data-request';
import { fetchKlineData } from './services';
import { renderChart, rerenderChart } from './utils/chart-utils';
import toastr from 'toastr';
import { initializeToastr } from '../configs/toastr-config';
import ApexCharts from 'apexcharts';

export class Chart {
    private static instance: Chart | null = null;
    private chartInstance: ApexCharts;
    private chartContainer!: HTMLElement;
    private startDateInput!: HTMLInputElement;
    private endDateInput!: HTMLInputElement;
    private timeframeSelect!: HTMLSelectElement;
    private startSelected: boolean = false;
    private endSelected: boolean = false;

    // Store chart initialization data
    private currentTradingPairId: number = 0;
    private mainCoinData: { id: number; symbol: string; name: string } = {
        id: 0,
        symbol: '',
        name: '',
    };
    private quoteCoinData: { id: number; symbol: string; name: string } = {
        id: 0,
        symbol: '',
        name: '',
    };
    private currentExchanges: Exchange[] = [];

    private constructor(klineData: KlineData[], coinSymbol: string) {
        initializeToastr();
        this.initializeElements();
        this.extractChartData();
        this.initializeDateInputs();
        this.chartInstance = renderChart(this.chartContainer, klineData, coinSymbol);
        this.setupEventListeners();
    }

    public static initialize(klineData: KlineData[], coinSymbol: string): Chart {
        if (!Chart.instance) {
            Chart.instance = new Chart(klineData, coinSymbol);
        }
        return Chart.instance;
    }

    public static autoInitialize(): void {
        document.addEventListener('DOMContentLoaded', () => {
            const klineDataElement = document.getElementById('chartData');
            const coinSymbolElement = document.getElementById('selectedMainCoin');

            if (!klineDataElement || !coinSymbolElement) {
                console.error('Required elements for chart initialization not found');
                return;
            }

            const klineData = JSON.parse(klineDataElement.getAttribute('data-kline-data') || '[]');
            const coinSymbol = coinSymbolElement.textContent || '';

            if (!klineData.length || !coinSymbol) {
                console.error('Invalid chart data or coin symbol');
                return;
            }

            Chart.initialize(klineData, coinSymbol);
        });
    }

    private initializeElements(): void {
        this.chartContainer = document.getElementById('priceChart') as HTMLElement;
        this.startDateInput = document.getElementById('start') as HTMLInputElement;
        this.endDateInput = document.getElementById('end') as HTMLInputElement;
        this.timeframeSelect = document.getElementById('timeframeSelect') as HTMLSelectElement;
    }

    private extractChartData(): void {
        // Extract trading pair data from DOM elements
        const chartDataElement = document.getElementById('chartData');

        if (!chartDataElement) {
            throw new Error('Chart data element not found');
        }

        // Extract main coin data from chartData element
        const mainCoinIdStr = chartDataElement.getAttribute('data-main-coin-id');
        const mainCoinName = chartDataElement.getAttribute('data-main-coin-name');
        const mainCoinSymbol = chartDataElement.getAttribute('data-main-coin-symbol');

        if (!mainCoinIdStr || !mainCoinName || !mainCoinSymbol) {
            throw new Error('Main coin data is incomplete');
        }

        this.mainCoinData = {
            id: parseInt(mainCoinIdStr),
            symbol: mainCoinSymbol,
            name: mainCoinName,
        };

        // Extract quote coin data and trading pair ID from active dropdown item
        const activeDropdownItem = document.querySelector('.dropdown-item.active') as HTMLElement;

        if (!activeDropdownItem) {
            throw new Error('No active trading pair found in dropdown');
        }

        const quoteCoinIdStr = activeDropdownItem.dataset['quoteCoinId'];
        const quoteCoinName = activeDropdownItem.dataset['quoteCoinName'];
        const quoteCoinSymbol = activeDropdownItem.dataset['quote'];
        const tradingPairIdStr = activeDropdownItem.dataset['tradingPairId'];
        const tradingPairExchanges = activeDropdownItem.dataset['tradingPairExchanges'];

        if (
            !quoteCoinIdStr ||
            !quoteCoinName ||
            !quoteCoinSymbol ||
            !tradingPairIdStr ||
            !tradingPairExchanges
        ) {
            throw new Error('Quote coin data is incomplete in active dropdown item');
        }

        this.currentTradingPairId = parseInt(tradingPairIdStr);
        this.currentExchanges = JSON.parse(tradingPairExchanges) as Exchange[];
        this.quoteCoinData = {
            id: parseInt(quoteCoinIdStr),
            symbol: quoteCoinSymbol,
            name: quoteCoinName,
        };
    }

    private initializeDateInputs(): void {
        const now = new Date();
        const oneWeekAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
        this.startDateInput.value = oneWeekAgo.toISOString().slice(0, 16);
        this.startDateInput.max = now.toISOString().slice(0, 16);
        this.endDateInput.value = now.toISOString().slice(0, 16);
        this.endDateInput.max = now.toISOString().slice(0, 16);
    }

    private setupEventListeners(): void {
        this.startDateInput.addEventListener('change', this.handleStartDateChange.bind(this));
        this.endDateInput.addEventListener('change', this.handleEndDateChange.bind(this));
        this.timeframeSelect.addEventListener('change', this.handleTimeframeChange.bind(this));
    }

    private handleStartDateChange(): void {
        this.startSelected = true;
        if (this.startSelected && this.endSelected) {
            this.rerenderChartWithCurrentValues();
            return;
        }
        const startDate = new Date(this.startDateInput.value);
        const timeframe = parseInt(this.timeframeSelect.value);
        const now = new Date();
        const calculatedMaxEndDate = new Date(startDate.getTime() + timeframe * 1000 * 60 * 1000);
        const maxEndDate = calculatedMaxEndDate > now ? now : calculatedMaxEndDate;

        this.endDateInput.min = startDate.toISOString().slice(0, 16);
        this.endDateInput.max = maxEndDate.toISOString().slice(0, 16);
        if (!this.endSelected) {
            this.endDateInput.focus();
            (this.endDateInput as HTMLInputElement & { showPicker(): void }).showPicker();
        }
    }

    private handleEndDateChange(): void {
        this.endSelected = true;
        if (this.startSelected && this.endSelected) {
            this.rerenderChartWithCurrentValues();
            return;
        }
        const endDate = new Date(this.endDateInput.value);
        const timeframe = parseInt(this.timeframeSelect.value);
        const minStartDate = new Date(endDate.getTime() - timeframe * 1000 * 60 * 1000);
        this.startDateInput.min = minStartDate.toISOString().slice(0, 16);
        this.startDateInput.max = endDate.toISOString().slice(0, 16);
        if (!this.startSelected) {
            this.startDateInput.focus();
            (this.startDateInput as HTMLInputElement & { showPicker(): void }).showPicker();
        }
    }

    private handleTimeframeChange(): void {
        const timeframe = parseInt(this.timeframeSelect.value);
        const endDate = new Date(this.endDateInput.value);
        const calculatedStartDate = new Date(endDate.getTime() - timeframe * 1000 * 60 * 1000);
        const currentStartDate = new Date(this.startDateInput.value);

        if (calculatedStartDate > currentStartDate) {
            this.startDateInput.value = calculatedStartDate.toISOString().slice(0, 16);
        }

        this.rerenderChartWithCurrentValues();
    }

    private parseTimeframeToInterval(timeframeValue: string): ExchangeKlineInterval {
        const timeframe = parseInt(timeframeValue);

        // Map timeframe values to ExchangeKlineInterval enum
        switch (timeframe) {
            case 1:
                return ExchangeKlineInterval.OneMinute;
            case 5:
                return ExchangeKlineInterval.FiveMinutes;
            case 15:
                return ExchangeKlineInterval.FifteenMinutes;
            case 30:
                return ExchangeKlineInterval.ThirtyMinutes;
            case 60:
                return ExchangeKlineInterval.OneHour;
            case 240:
                return ExchangeKlineInterval.FourHours;
            case 1440:
                return ExchangeKlineInterval.OneDay;
            case 10080:
                return ExchangeKlineInterval.OneWeek;
            case 43200:
                return ExchangeKlineInterval.OneMonth;
            default:
                throw new Error(`Unknown timeframe value: ${timeframe}`);
        }
    }

    private async rerenderChartWithCurrentValues(): Promise<void> {
        const timeframe = this.timeframeSelect.value;
        const startDate = new Date(this.startDateInput.value);
        const endDate = new Date(this.endDateInput.value);

        const request: KlineDataRequest = {
            idTradingPair: this.currentTradingPairId,
            coinMain: this.mainCoinData,
            coinQuote: this.quoteCoinData,
            exchanges: this.currentExchanges,
            interval: this.parseTimeframeToInterval(timeframe),
            startTime: startDate.toISOString(),
            endTime: endDate.toISOString(),
            limit: 1000,
        };

        try {
            const fetchedKlineData = await fetchKlineData(request);

            if (fetchedKlineData.length === 0) {
                toastr.warning(
                    `No data found for trading pair ${request.coinMain.symbol}/${request.coinQuote.symbol}.
                    <br> Please select a different trading pair or a different time period.`
                );
            } else {
                await rerenderChart(this.chartInstance, fetchedKlineData);
            }
        } catch (error) {
            toastr.error(`Failed to fetch chart data: ${error}`);
            console.error('Chart data fetch error:', error);
        }

        this.startSelected = false;
        this.endSelected = false;
        this.startDateInput.min = '';
        this.startDateInput.max = '';
        this.endDateInput.min = '';
        this.endDateInput.max = new Date().toISOString().slice(0, 16);
    }
}

// Auto-initialize the chart
Chart.autoInitialize();
