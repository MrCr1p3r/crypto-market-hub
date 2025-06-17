import ApexCharts, { ApexOptions } from 'apexcharts';
import { KlineData } from '../interfaces/kline-data';

export function renderChart(
    chartContainer: HTMLElement,
    klineData: KlineData[],
    coinSymbol: string
): ApexCharts {
    const options: ApexOptions = {
        series: [
            {
                data: klineData.map((k) => ({
                    x: new Date(k.openTime),
                    y: [k.openPrice, k.highPrice, k.lowPrice, k.closePrice],
                })),
            },
        ],
        chart: {
            type: 'candlestick',
            height: 400,
            background: 'transparent',
            foreColor: '#ffffff',
        },
        theme: {
            mode: 'dark',
        },
        title: {
            text: coinSymbol + ' Price Chart',
            align: 'left',
            style: {
                fontSize: '18px',
                fontWeight: 'bold',
                color: '#ffffff',
            },
        },
        plotOptions: {
            candlestick: {
                wick: {
                    useFillColor: true,
                },
            },
        },
        xaxis: {
            type: 'datetime',
            labels: {
                style: {
                    colors: '#ffffff',
                    fontSize: '12px',
                },
                datetimeFormatter: {
                    year: 'yyyy',
                    month: "MMM 'yy",
                    day: 'dd MMM',
                    hour: 'HH:mm',
                },
            },
            axisBorder: {
                color: '#444444',
            },
            axisTicks: {
                color: '#444444',
            },
        },
        yaxis: {
            labels: {
                style: {
                    colors: '#ffffff',
                    fontSize: '12px',
                },
                formatter: function (value) {
                    return value.toFixed(6);
                },
            },
            axisBorder: {
                color: '#444444',
            },
            axisTicks: {
                color: '#444444',
            },
        },
        grid: {
            borderColor: '#333333',
            strokeDashArray: 3,
            xaxis: {
                lines: {
                    show: true,
                },
            },
            yaxis: {
                lines: {
                    show: true,
                },
            },
        },
        tooltip: {
            theme: 'dark',
            x: {
                format: 'yyyy-MM-dd HH:mm:ss',
            },
            y: {
                formatter: function (value) {
                    return value.toFixed(6);
                },
            },
            style: {
                fontSize: '12px',
            },
        },
        legend: {
            labels: {
                colors: '#ffffff',
            },
        },
    };

    const chartInstance = new ApexCharts(chartContainer, options);
    chartInstance.render();
    return chartInstance;
}

export async function rerenderChart(
    chartInstance: ApexCharts,
    klineData: KlineData[]
): Promise<void> {
    const updatedSeries = klineData.map((k: KlineData) => ({
        x: new Date(k.openTime),
        y: [k.openPrice, k.highPrice, k.lowPrice, k.closePrice],
    }));

    chartInstance.updateSeries([{ data: updatedSeries }]);
}
