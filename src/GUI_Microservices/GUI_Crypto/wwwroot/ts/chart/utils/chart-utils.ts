import ApexCharts, { ApexOptions } from 'apexcharts';
import { KlineData } from '../interfaces/kline-data';

export function renderChart(
    chartContainer: HTMLElement,
    klineData: KlineData[],
    coinSymbol: string
) {
    const options: ApexOptions = {
        series: [
            {
                data: klineData.map((k) => ({
                    x: new Date(k.openTime),
                    y: [k.openPrice, k.highPrice, k.lowPrice, k.closePrice]
                }))
            }
        ],
        chart: {
            type: 'candlestick',
            height: 400
        },
        title: {
            text: coinSymbol + ' Price',
            align: 'left'
        },
        xaxis: {
            type: 'datetime'
        },
        tooltip: {
            x: {
                format: 'yyyy-MM-dd HH:mm:ss'
            }
        }
    };

    const chartInstance = new ApexCharts(chartContainer, options);
    chartInstance.render();
    return chartInstance;
}

export async function rerenderChart(
    chartInstance: ApexCharts,
    klineData: KlineData[]
) {
    const updatedSeries = klineData.map((k: KlineData) => ({
        x: new Date(k.openTime),
        y: [k.openPrice, k.highPrice, k.lowPrice, k.closePrice]
    }));

    chartInstance.updateSeries([{ data: updatedSeries }]);
}
