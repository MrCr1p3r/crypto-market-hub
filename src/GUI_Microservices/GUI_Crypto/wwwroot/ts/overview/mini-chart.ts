import ApexCharts, { ApexOptions } from 'apexcharts';
import { KlineData } from '../chart/interfaces/kline-data';

export function initializeMiniCharts(): void {
    const miniChartElements = document.querySelectorAll('.mini-chart');
    renderMiniCharts(Array.from(miniChartElements));
}

export function renderMiniCharts(elements: Element[]): void {
    elements.forEach((element) => {
        const klineDataStr = element.getAttribute('data-kline-data');
        if (!klineDataStr) return;
        
        const klineData = JSON.parse(klineDataStr) as KlineData[];
        if (klineData.length === 0) return;

        element.innerHTML = '';

        const options: ApexOptions = {
            series: [{
                data: klineData.map((k) => ({
                    x: new Date(k.openTime),
                    y: [k.openPrice, k.highPrice, k.lowPrice, k.closePrice]
                }))
            }],
            chart: {
                type: 'candlestick',
                height: 100,
                width: 250,
                toolbar: {
                    show: false
                },
                sparkline: {
                    enabled: true
                }
            },
            grid: {
                show: false
            },
            xaxis: {
                labels: {
                    show: false
                },
                axisTicks: {
                    show: false
                },
                axisBorder: {
                    show: false
                }
            },
            yaxis: {
                labels: {
                    show: false
                },
                tooltip: {
                    enabled: false
                }
            },
            tooltip: {
                enabled: false
            }
        };

        const chart = new ApexCharts(element, options);
        chart.render();
    });
} 
