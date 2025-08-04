import ApexCharts, { ApexOptions } from 'apexcharts';
import { Kline } from '../../shared/interfaces/kline';

// Add CSS styles for unrendered charts and messages
const style = document.createElement('style');
style.textContent = `
    .mini-chart {
        min-height: 100px;
        min-width: 250px;
        display: flex;
        align-items: center;
        justify-content: center;
    }
    .mini-chart.not-rendered {
        height: 100px;
        width: 250px;
        background: rgba(255, 255, 255, 0.05);
        border-radius: 4px;
    }
    .mini-chart-message {
        height: 100%;
        width: 100%;
        display: flex;
        align-items: center;
        justify-content: center;
        color: #6c757d;
        font-style: italic;
        text-align: center;
        background: rgba(255, 255, 255, 0.05);
        border-radius: 4px;
        padding: 10px;
        margin: 0;
    }
`;
document.head.appendChild(style);

export function renderMiniCharts(elements: Element[]): void {
    elements.forEach((element) => {
        if (!(element instanceof HTMLElement)) return;

        // Skip if already rendered
        if (!element.classList.contains('not-rendered')) return;

        const klineDataStr = element.getAttribute('data-kline-data');
        if (!klineDataStr) {
            element.classList.remove('not-rendered');
            element.innerHTML =
                '<div class="mini-chart-message">No kline data was found for this coin</div>';
            return;
        }

        try {
            const klineData = JSON.parse(klineDataStr) as Kline[];
            if (klineData.length === 0) {
                element.classList.remove('not-rendered');
                element.innerHTML =
                    '<div class="mini-chart-message">No kline data was found for this coin</div>';
                return;
            }

            // Remove the not-rendered class
            element.classList.remove('not-rendered');

            // Clear any existing content
            element.innerHTML = '';

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
                    height: 100,
                    width: 250,
                    toolbar: {
                        show: false,
                    },
                    sparkline: {
                        enabled: true,
                    },
                    animations: {
                        enabled: false, // Disable animations for better performance
                    },
                },
                grid: {
                    show: false,
                },
                xaxis: {
                    labels: {
                        show: false,
                    },
                    axisTicks: {
                        show: false,
                    },
                    axisBorder: {
                        show: false,
                    },
                },
                yaxis: {
                    labels: {
                        show: false,
                    },
                    tooltip: {
                        enabled: false,
                    },
                },
                tooltip: {
                    enabled: false,
                },
            };

            const chart = new ApexCharts(element, options);
            chart.render();

            // Store chart instance for future updates
            (element as HTMLElement & { __chartInstance?: ApexCharts }).__chartInstance = chart;
        } catch (error) {
            console.error('Error rendering mini chart:', error);
            // Re-add the not-rendered class in case of error
            element.classList.add('not-rendered');
        }
    });
}

/**
 * Destroy all existing charts and mark them for re-rendering
 * Returns the elements that need to be re-observed by the table manager
 */
export function destroyAllChartsForRerender(): Element[] {
    // Find all chart elements that have been rendered
    const allChartElements = document.querySelectorAll('.mini-chart:not(.not-rendered)');
    const elementsToReobserve: Element[] = [];

    allChartElements.forEach((chartElement) => {
        const element = chartElement as HTMLElement;

        // Destroy existing chart if it exists
        const existingChart = (element as HTMLElement & { __chartInstance?: ApexCharts })
            .__chartInstance;
        if (existingChart) {
            existingChart.destroy();
            delete (element as HTMLElement & { __chartInstance?: ApexCharts }).__chartInstance;
        }

        // Clear element and mark as not-rendered
        element.innerHTML = '';
        element.classList.add('not-rendered');

        // Add to list for re-observing
        elementsToReobserve.push(element);
    });

    return elementsToReobserve;
}
