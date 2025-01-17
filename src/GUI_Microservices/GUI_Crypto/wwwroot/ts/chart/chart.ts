import { KlineData } from './interfaces/kline-data';
import { formatDate } from './utils/date-utils';
import { KlineDataRequest } from './interfaces/kline-data-request';
import { fetchKlineData } from './services';
import { renderChart, rerenderChart } from './utils/chart-utils';
import toastr from 'toastr';

export class Chart {
    private static instance: Chart | null = null;
    private chartInstance: any;
    private chartContainer!: HTMLElement;
    private startDateInput!: HTMLInputElement;
    private endDateInput!: HTMLInputElement;
    private timeframeSelect!: HTMLSelectElement;
    private selectedMainCoin!: HTMLElement;
    private tradingPairItems!: NodeListOf<Element>;
    private startSelected: boolean = false;
    private endSelected: boolean = false;

    private constructor(klineData: KlineData[], coinSymbol: string) {
        this.initializeToastr();
        this.initializeElements();
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

    private initializeToastr(): void {
        toastr.options = {
            positionClass: "toast-top-right",
            timeOut: 3000,
            closeButton: true
        };
    }

    private initializeElements(): void {
        this.chartContainer = document.getElementById('priceChart') as HTMLElement;
        this.startDateInput = document.getElementById('start') as HTMLInputElement;
        this.endDateInput = document.getElementById('end') as HTMLInputElement;
        this.timeframeSelect = document.getElementById('timeframeSelect') as HTMLSelectElement;
        this.selectedMainCoin = document.getElementById('selectedMainCoin') as HTMLElement;
        this.tradingPairItems = document.querySelectorAll('.dropdown-item');
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
        this.tradingPairItems.forEach(item => {
            item.addEventListener('click', this.handleTradingPairChange.bind(this));
        });
        this.startDateInput.addEventListener('change', this.handleStartDateChange.bind(this));
        this.endDateInput.addEventListener('change', this.handleEndDateChange.bind(this));
        this.timeframeSelect.addEventListener('change', this.handleTimeframeChange.bind(this));
    }

    private handleTradingPairChange(event: Event): void {
        const element = event.currentTarget as HTMLElement;
        const quoteCoin = element.dataset.quote as string;
        document.getElementById('selectedQuoteCoin')!.textContent = quoteCoin;

        this.tradingPairItems.forEach(i => i.classList.remove('active'));
        element.classList.add('active');

        this.rerenderChartWithCurrentValues();
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
            (this.endDateInput as any).showPicker();
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
            (this.startDateInput as any).showPicker();
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

    private async rerenderChartWithCurrentValues(): Promise<void> {
        const timeframe = this.timeframeSelect.value;
        const startDate = new Date(this.startDateInput.value);
        const endDate = new Date(this.endDateInput.value);
        const request: KlineDataRequest = {
            coinMainSymbol: this.selectedMainCoin.textContent!,
            coinQuoteSymbol: document.getElementById('selectedQuoteCoin')!.textContent!,
            interval: timeframe,
            startTime: formatDate(startDate),
            endTime: formatDate(endDate)
        };
        const fetchedKlineData = await fetchKlineData(request);
        if (fetchedKlineData.length === 0) {
            toastr.warning(
                `No data found for trading pair ${request.coinMainSymbol}/${request.coinQuoteSymbol}.
                <br> Please select a different trading pair or a different time period.`);
        }
        else {
            await rerenderChart(this.chartInstance, fetchedKlineData);
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
