import React, { useEffect, useState } from 'react';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import './BackTestApp.css';

function getLocaleSeparators() {
    const format = new Intl.NumberFormat(navigator.language).formatToParts(1234.5);
    const separators: { thousandsSep?: string; decimalPoint?: string } = {};
    format.forEach((part) => {
        if (part.type === 'group') {
            separators.thousandsSep = part.value;
        } else if (part.type === 'decimal') {
            separators.decimalPoint = part.value;
        }
    });
    return separators;
}

const { thousandsSep, decimalPoint } = getLocaleSeparators();

Highcharts.setOptions({
    lang: {
        thousandsSep: thousandsSep,
        decimalPoint: decimalPoint
    },
    tooltip: {
        valueDecimals: 2
    }
});

function BackTestApp() {
    const portfolioOptions = [
        {
            name: 'Aggressive Value Tilt ($^USLCB 25%, $^USSCV 75%)',
            id: 'aggressive',
            portfolio: [
                { Ticker: '$^USLCB', Percentage: 25 },
                { Ticker: '$^USSCV', Percentage: 75 }
            ]
        },
        {
            name: 'Value Barbell ($^USLCB 50%, $^USSCV 50%)',
            id: 'default',
            portfolio: [
                { Ticker: '$^USLCB', Percentage: 50 },
                { Ticker: '$^USSCV', Percentage: 50 }
            ]
        },
        {
            name: 'Conservative Value Tilt ($^USLCB 75%, $^USSCV 25%)',
            id: 'conservative',
            portfolio: [
                { Ticker: '$^USLCB', Percentage: 75 },
                { Ticker: '$^USSCV', Percentage: 25 }
            ]
        },
        {
            name: 'AVUV',
            id: 'AVUV',
            portfolio: [{ Ticker: 'AVUV', Percentage: 100 }]
        },
        {
            name: '$TBILL',
            id: 'TBILL',
            portfolio: [{ Ticker: '$TBILL', Percentage: 100 }]
        },
        {
            name: '$GOLDX',
            id: 'GOLD',
            portfolio: [{ Ticker: '$GOLDX', Percentage: 100 }]
        },
        {
            name: '$KMLMX',
            id: 'KMLM',
            portfolio: [{ Ticker: '$KMLMX', Percentage: 100 }]
        },
        {
            name: '$DBMFX',
            id: 'DBMF',
            portfolio: [{ Ticker: '$DBMFX', Percentage: 100 }]
        }
    ];

    const periodOptions = [
        { name: 'Daily', id: 0 },
        { name: 'Monthly', id: 1 },
        { name: 'Yearly', id: 2 }
    ];

    const rebalanceOptions = [
        { name: 'None', id: 0 },
        { name: 'Daily', id: 1 },
        { name: 'Weekly', id: 2 },
        { name: 'Monthly', id: 3 },
        { name: 'Quarterly', id: 4 },
        { name: 'Semi-Annually', id: 5 },
        { name: 'Annually', id: 6 }
    ];

    const [isAppLoading, setIsAppLoading] = useState(true);
    const [selectedPortfolioId, setSelectedPortfolioId] = useState('default');
    const [selectedPeriodTypeId, setSelectedPeriodTypeId] = useState(
        periodOptions.find((option) => option.name === 'Daily').id
    );
    const [selectedRebalanceStrategyId, setSelectedRebalanceId] = useState(
        rebalanceOptions.find((option) => option.name === 'Annually').id
    );
    const [selectedIsLogScale, setSelectedIsLogScale] = useState(false);

    const [portfolioBackTests, setPortfolioBackTests] = useState([]);
    const [isLoadingBackTest, setIsLoadingBackTest] = useState(true);
    const [chartOptions, setChartOptions] = useState({});
    const [drawdownChartOptions, setDrawdownChartOptions] = useState({});

    useEffect(() => {
        setIsLoadingBackTest(true);
        (async () => {
            await fetchPortfolio(selectedPortfolioId, selectedPeriodTypeId, selectedRebalanceStrategyId, 10000);
            setIsAppLoading(false);
            setIsLoadingBackTest(false);
        })();
    }, [selectedPortfolioId, selectedPeriodTypeId, selectedRebalanceStrategyId]);

    useEffect(() => {
        if (portfolioBackTests) {
            updatePerformanceChartOptions();
            updateDrawdownChartOptions();
        }
    }, [portfolioBackTests]);

    const handlePortfolioChange = (event) => {
        setSelectedPortfolioId(event.target.value);
    };

    const handlePeriodTypeChange = (event) => {
        setSelectedPeriodTypeId(Number(event.target.value));
    };

    const handleRebalanceStrategyChange = (event) => {
        setSelectedRebalanceId(Number(event.target.value));
    };

    const handleLogScaleChange = (event) => {
        setSelectedIsLogScale(event.target.checked);
    };

    const formatNumber = (number) =>
        number.toLocaleString(undefined, {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });

    const formatCurrency = (number) =>
        new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD'
        }).format(number);

    const updatePerformanceChartOptions = () => {
        setChartOptions({
            chart: {
                type: 'spline',
                zoomType: 'x'
            },
            title: {
                text: null
            },
            xAxis: {
                type: 'datetime'
            },
            yAxis: {
                title: {
                    text: 'Portfolio Balance ($)'
                },
                type: selectedIsLogScale ? 'logarithmic' : 'linear'
            },
            series: portfolioBackTests.flatMap((portfolioBackTest, i) => [
                {
                    lineWidth: 1,
                    name: `P${i} Starting Balance`,
                    data: portfolioBackTest.aggregatePerformance.map((item) => [
                        new Date(item.periodStart).getTime(),
                        item.startingBalance
                    ])
                },
                {
                    lineWidth: 1,
                    name: `P${i} Ending Balance`,
                    data: portfolioBackTest.aggregatePerformance.map((item) => [
                        new Date(item.periodStart).getTime(),
                        item.endingBalance
                    ])
                }
            ]),
            tooltip: {
                valuePrefix: '$',
                shared: true
            }
        });
    };

    const updateDrawdownChartOptions = () => {
        setDrawdownChartOptions({
            chart: {
                type: 'spline',
                zoomType: 'x'
            },
            title: {
                text: null
            },
            xAxis: {
                type: 'datetime'
            },
            yAxis: {
                title: {
                    text: 'Drawdown'
                },
                labels: {
                    formatter: function () {
                        return this.value + '%';
                    }
                },
                max: 0
            },
            series: portfolioBackTests.map((portfolioBackTest, i) => ({
                lineWidth: 1,
                name: `P${i} Drawdown`,
                data: portfolioBackTest.aggregatePerformanceDrawdownsReturns.map((item) => [
                    new Date(item.periodStart).getTime(),
                    item.returnPercentage
                ])
            })),
            tooltip: {
                valueSuffix: '%'
            }
        });
    };

    const generateCSV = (data, headers) => {
        const csvContent = [headers.join(','), ...data.map((row) => row.join(','))].join('\n');
        return csvContent;
    };

    const downloadCSV = (csvContent, fileName) => {
        const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
        const link = document.createElement('a');
        if (link.download !== undefined) {
            const url = URL.createObjectURL(blob);
            link.setAttribute('href', url);
            link.setAttribute('download', fileName);
            link.style.visibility = 'hidden';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        }
    };

    const handleSavePortfolioValueCSV = () => {
        if (portfolioBackTests?.length) {
            for (const [index, portfolioBackTest] of portfolioBackTests.entries()) {
                const csvData = portfolioBackTest.aggregatePerformance.map((item) => [
                    item.periodStart.substr(0, 10),
                    item.startingBalance,
                    item.endingBalance
                ]);
                const headers = ['Period Start', 'Period Starting Balance', 'Period Ending Balance'];
                const csvContent = generateCSV(csvData, headers);
                downloadCSV(csvContent, `P${index}-portfolio-balance.csv`);
            }
        }
    };

    const handleSaveDrawdownCSV = () => {
        if (portfolioBackTests?.length) {
            for (const [index, portfolioBackTest] of portfolioBackTests.entries()) {
                const csvData = portfolioBackTest.aggregatePerformanceDrawdownsReturns.map((item) => [
                    item.periodStart.substr(0, 10),
                    item.returnPercentage
                ]);
                const headers = ['Period Start', 'Period Drawdown Percentage '];
                const csvContent = generateCSV(csvData, headers);
                downloadCSV(csvContent, `P${index}-drawdown.csv`);
            }
        }
    };

    const contents = isAppLoading ? (
        <p>Loading&hellip;</p>
    ) : (
        <>
            <div>All performance calculations assume dividends reinvested and 0% income tax on dividends.</div>

            <h3>Configuration</h3>
            <div style={{ textAlign: 'left', marginBottom: '20px' }}>
                <div style={{ marginBottom: '10px' }}>
                    <label
                        htmlFor="portfolio-select"
                        style={{ marginRight: '10px' }}
                    >
                        Portfolio:{' '}
                    </label>
                    <select
                        id="portfolio-select"
                        value={selectedPortfolioId}
                        onChange={handlePortfolioChange}
                    >
                        {portfolioOptions.map((option) => (
                            <option
                                key={option.id}
                                value={option.id}
                            >
                                {option.name}
                            </option>
                        ))}
                    </select>
                </div>
                <div style={{ marginBottom: '10px' }}>
                    <label
                        htmlFor="period-select"
                        style={{ marginRight: '10px' }}
                    >
                        History resolution:{' '}
                    </label>
                    <select
                        id="period-select"
                        value={selectedPeriodTypeId}
                        onChange={handlePeriodTypeChange}
                    >
                        {periodOptions.map((option) => (
                            <option
                                key={option.id}
                                value={option.id}
                            >
                                {option.name}
                            </option>
                        ))}
                    </select>
                </div>
                <div>
                    <label
                        htmlFor="rebalance-select"
                        style={{ marginRight: '10px' }}
                    >
                        Rebalancing frequency:{' '}
                    </label>
                    <select
                        id="rebalance-select"
                        value={selectedRebalanceStrategyId}
                        onChange={handleRebalanceStrategyChange}
                    >
                        {rebalanceOptions.map((option) => (
                            <option
                                key={option.id}
                                value={option.id}
                            >
                                {option.name}
                            </option>
                        ))}
                    </select>
                </div>
            </div>

            <div className={isLoadingBackTest ? 'loading' : ''}>
                <h3>Overview</h3>
                {portfolioBackTests.map((portfolioBackTest) => (
                    <table
                        key={portfolioBackTest.id}
                        style={{ display: 'block', float: 'left' }}
                    >
                        <tbody>
                            <tr>
                                <td>First Period:</td>
                                <td>{portfolioBackTest.aggregatePerformance[0].periodStart.substr(0, 10)}</td>
                            </tr>
                            <tr>
                                <td>Last Period:</td>
                                <td>
                                    {portfolioBackTest.aggregatePerformance[
                                        portfolioBackTest.aggregatePerformance.length - 1
                                    ].periodStart.substr(0, 10)}
                                </td>
                            </tr>
                            <tr>
                                <td>Period(s):</td>
                                <td>{portfolioBackTest.aggregatePerformance.length.toLocaleString()}</td>
                            </tr>
                            <tr>
                                <td>Starting Balance:</td>
                                <td>{formatCurrency(portfolioBackTest.aggregatePerformance[0].startingBalance)}</td>
                            </tr>
                            <tr>
                                <td>Ending Balance:</td>
                                <td>
                                    {formatCurrency(
                                        portfolioBackTest.aggregatePerformance[
                                            portfolioBackTest.aggregatePerformance.length - 1
                                        ].endingBalance
                                    )}
                                </td>
                            </tr>
                            <tr>
                                <td>CAGR:</td>
                                <td>{formatNumber(portfolioBackTest.cagr * 100)}%</td>
                            </tr>
                            <tr>
                                <td>Time to 2x @ CAGR:</td>
                                <td>{formatNumber(portfolioBackTest.yearsBeforeDoubling)} years</td>
                            </tr>
                            <tr>
                                <td>Maximum Drawdown:</td>
                                <td>{formatNumber(portfolioBackTest.maximumDrawdownPercentage)}%</td>
                            </tr>
                            <tr>
                                <td>Rebalances:</td>
                                <td>
                                    {portfolioBackTest.rebalancesByTicker[
                                        Object.keys(portfolioBackTest.rebalancesByTicker)[0]
                                    ].length.toLocaleString()}
                                </td>
                            </tr>
                            <tr>
                                <td>Rebalance Strategy:</td>
                                <td>
                                    {
                                        rebalanceOptions.find(
                                            (option) => option.id === portfolioBackTest.rebalanceStrategy
                                        ).name
                                    }
                                </td>
                            </tr>
                        </tbody>
                    </table>
                ))}

                <div style={{ clear: 'both' }}></div>

                <h3>Portfolio Value</h3>
                <div>
                    <HighchartsReact
                        highcharts={Highcharts}
                        options={chartOptions}
                    />
                </div>
                <div style={{ marginTop: '0.5em', textAlign: 'center' }}>
                    <label>
                        <input
                            type="checkbox"
                            checked={selectedIsLogScale}
                            onChange={handleLogScaleChange}
                        />
                        Logarithmic Scale
                    </label>
                </div>

                <h3>Portfolio Drawdowns</h3>
                <div>
                    <HighchartsReact
                        highcharts={Highcharts}
                        options={drawdownChartOptions}
                    />
                </div>
            </div>
        </>
    );

    return (
        <div>
            <h2 id="tableLabel">Portfolio Back Test</h2>
            {contents}
            {!!portfolioBackTests?.length && (
                <div style={{ marginTop: '20px' }}>
                    <button onClick={handleSavePortfolioValueCSV}>Portfolio Value CSV</button>
                    <button
                        onClick={handleSaveDrawdownCSV}
                        style={{ marginLeft: '10px' }}
                    >
                        Portfolio Drawdown CSV
                    </button>
                </div>
            )}
        </div>
    );

    async function fetchPortfolio(portfolioId, periodType, rebalanceStrategy, startingBalance) {
        // Not working, not sure what's going on. Not important for now, may disappear completely.

        const portfolio = portfolioOptions.find((option) => (option.id = portfolioId))?.portfolio;

        if (!portfolio) {
            throw new Error('Unrecognized portfolio type.');
        }

        const singlePortfolioBackTestRequest = {
            portfolio,
            startingBalance,
            periodType,
            rebalanceStrategy
        };

        const fetchSinglePortfolioBackTest = fetch(`https://localhost:7118/api/BackTest/GetPortfolioBackTest`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(singlePortfolioBackTestRequest)
        });

        await fetchSinglePortfolioBackTest
            .then((response) => response.json())
            .then((backTest) => {
                debugger;
                setPortfolioBackTests([backTest]);
            })
            .catch(console.error);
        /*
        const multiPortfolioBackTestRequest = {
            portfolios: [
                [{ Ticker: '$^USLCB', Percentage: 100 }],
                [{ Ticker: '$^USSCV', Percentage: 100 }],
                [{ Ticker: '$KMLMX,KMLM', Percentage: 100 }],
                [{ Ticker: '$DBMFX,DBMF', Percentage: 100 }],
                [{ Ticker: '$GOLDX,GLD,GLDM', Percentage: 100 }],
                [{ Ticker: '$TBILL,USFR', Percentage: 100 }]
            ],
            startingBalance,
            periodType,
            rebalanceStrategy
        };

        const fetchMultiPortfolioBackTest = fetch(`https://localhost:7118/api/BackTest/GetPortfolioBackTests`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(multiPortfolioBackTestRequest)
        });

        await fetchMultiPortfolioBackTest
            .then((response) => response.json())
            .then(setPortfolioBackTests)
            .catch(console.error);*/
    }
}

export default BackTestApp;
