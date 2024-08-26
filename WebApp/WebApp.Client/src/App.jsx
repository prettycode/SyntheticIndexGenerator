import React, { useEffect, useState } from 'react';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import './App.css';

function App() {
    const portfolioOptions = [
        { name: 'Default ($^USSCV 50%, $^USLCB 50%)', value: 'default' },
        { name: 'Aggressive ($^USSCV 70%, $^USLCB 30%)', value: 'aggressive' },
        { name: 'Conservative ($^USSCV 30%, $^USLCB 70%)', value: 'conservative' },
        { name: 'AVUV', value: 'AVUV' },
        { name: 'SPY', value: 'SPY' },
        { name: 'SPY/AVUV', value: 'SPY/AVUV' }
    ];

    const periodOptions = [
        { name: 'Daily', value: 0 },
        { name: 'Monthly', value: 1 },
        { name: 'Yearly', value: 2 },
    ];

    const rebalanceOptions = [
        { name: 'None', value: 0 },
        { name: 'Daily', value: 1 },
        { name: 'Weekly', value: 2 },
        { name: 'Monthly', value: 3 },
        { name: 'Quarterly', value: 4 },
        { name: 'Semi-Annually', value: 5 },
        { name: 'Annually', value: 6 }
    ];

    const dailyPeriodOptionValue = periodOptions.find(option => option.name === 'Daily').value;
    const annualRebalanceOptionValue = rebalanceOptions.find(option => option.name === 'Annually').value;

    const [portfolioBackTest, setPortfolioBackTest] = useState();
    const [selectedPortfolio, setSelectedPortfolio] = useState('default');
    const [periodType, setPeriodType] = useState(dailyPeriodOptionValue);
    const [rebalanceFrequency, setRebalanceFrequency] = useState(annualRebalanceOptionValue);
    const [isLoadingBackTest, setIsLoadingBackTest] = useState(true);
    const [isLogScale, setIsLogScale] = useState(false);
    const [chartOptions, setChartOptions] = useState({});
    const [drawdownChartOptions, setDrawdownChartOptions] = useState({});

    useEffect(() => {
        setIsLoadingBackTest(true);
        (async () => {
            await fetchPortfolio(selectedPortfolio, periodType, rebalanceFrequency);
            setIsLoadingBackTest(false);
        })();
    }, [selectedPortfolio, periodType, rebalanceFrequency]);

    useEffect(() => {
        if (portfolioBackTest) {
            updatePerformanceChartOptions();
            updateDrawdownChartOptions();
        }
    }, [portfolioBackTest, isLogScale]);

    const handlePortfolioChange = (event) => {
        setSelectedPortfolio(event.target.value);
    };

    const handlePeriodTypeChange = (event) => {
        setPeriodType(Number(event.target.value));
    };

    const handleRebalanceFrequencyChange = (event) => {
        setRebalanceFrequency(Number(event.target.value));
    };

    const handleLogScaleChange = (event) => {
        setIsLogScale(event.target.checked);
    };

    const formatNumber = (number) =>
        number.toLocaleString(undefined, {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        });

    const formatCurrency = (number) =>
        new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD',
        }).format(number);

    const updatePerformanceChartOptions = () => {
        setChartOptions({
            title: {
                text: null
            },
            xAxis: {
                type: 'datetime'
            },
            yAxis: {
                title: {
                    text: 'Portfolio Balance'
                },
                labels: {
                    formatter: function () {
                        return '$' + this.axis.defaultLabelFormatter.call(this);
                    }
                },
                type: isLogScale ? 'logarithmic' : 'linear'
            },
            series: [{
                lineWidth: 1,
                name: 'Ending Balance',
                data: portfolioBackTest.aggregatePerformance.map(item => [
                    new Date(item.periodStart).getTime(),
                    item.endingBalance
                ])
            }]
        });
    };

    const updateDrawdownChartOptions = () => {
        setDrawdownChartOptions({
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
            series: [{
                lineWidth: 1,
                name: 'Drawdown',
                data: portfolioBackTest.aggregatePerformanceDrawdownsReturns.map(item => [
                    new Date(item.periodStart).getTime(),
                    item.returnPercentage
                ])
            }],
            tooltip: {
                pointFormatter: function () {
                    return `<span style="color:${this.color}">\u25CF</span> ${this.series.name}: <b>${this.y.toFixed(2)}%</b><br/>`;
                }
            }
        });
    };

    const contents = portfolioBackTest === undefined
        ? <p>Loading&hellip;</p>
        : <>
            <div>
                All performance calculations assume dividends reinvested and 0% income tax on dividends.
            </div>

            <h3>Configuration</h3>
            <div style={{ textAlign: 'left', marginBottom: '20px' }}>
                <div style={{ marginBottom: '10px' }}>
                    <label htmlFor="portfolio-select" style={{ marginRight: '10px' }}>Portfolio: </label>
                    <select id="portfolio-select" value={selectedPortfolio} onChange={handlePortfolioChange}>
                        {portfolioOptions.map((option) => (
                            <option key={option.value} value={option.value}>
                                {option.name}
                            </option>
                        ))}
                    </select>
                </div>
                <div style={{ marginBottom: '10px' }}>
                    <label htmlFor="period-select" style={{ marginRight: '10px' }}>History resolution: </label>
                    <select id="period-select" value={periodType} onChange={handlePeriodTypeChange}>
                        {periodOptions.map((option) => (
                            <option key={option.value} value={option.value}>
                                {option.name}
                            </option>
                        ))}
                    </select>
                </div>
                <div>
                    <label htmlFor="rebalance-select" style={{ marginRight: '10px' }}>Rebalancing frequency: </label>
                    <select id="rebalance-select" value={rebalanceFrequency} onChange={handleRebalanceFrequencyChange}>
                        {rebalanceOptions.map((option) => (
                            <option key={option.value} value={option.value}>
                                {option.name}
                            </option>
                        ))}
                    </select>
                </div>
            </div>

            <div className={isLoadingBackTest ? 'loading' : ''} >
                <h3>Overview</h3>
                <table>
                    <tbody>
                        <tr>
                            <td>First Period:</td>
                            <td>{portfolioBackTest.aggregatePerformance[0].periodStart.substr(0, 10)}</td>
                        </tr>
                        <tr>
                            <td>Last Period:</td>
                            <td>{portfolioBackTest.aggregatePerformance[portfolioBackTest.aggregatePerformance.length - 1].periodStart.substr(0, 10)}</td>
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
                            <td>{formatCurrency(portfolioBackTest.aggregatePerformance[portfolioBackTest.aggregatePerformance.length - 1].endingBalance)}</td>
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
                                {portfolioBackTest.rebalancesByTicker[Object.keys(portfolioBackTest.rebalancesByTicker)[0]].length.toLocaleString()}
                            </td>
                        </tr>
                        <tr>
                            <td>Rebalance Strategy:</td>
                            <td>
                                {rebalanceOptions.find(option => option.value === portfolioBackTest.rebalanceStrategy).name}
                            </td>
                        </tr>
                    </tbody>
                </table>

                <h3>Portfolio Value</h3>
                <div>
                    <HighchartsReact highcharts={Highcharts} options={chartOptions} />
                </div>
                <div style={{ marginTop: '0.5em', textAlign: 'center' }}>
                    <label>
                        <input
                            type="checkbox"
                            checked={isLogScale}
                            onChange={handleLogScaleChange}
                        />
                        Logarithmic Scale
                    </label>
                </div>

                <h3>Portfolio Drawdowns</h3>
                <div>
                    <HighchartsReact highcharts={Highcharts} options={drawdownChartOptions} />
                </div>

            </div>
        </>;

    return (
        <div>
            <h2 id="tableLabel">Portfolio Back Test</h2>
            {contents}
        </div>
    );

    async function fetchPortfolio(portfolioType, periodType, rebalanceFrequency) {
        let portfolioConstituents;
        switch (portfolioType) {
            case 'aggressive':
                portfolioConstituents = [
                    { Ticker: '$^USSCV', Percentage: 70 },
                    { Ticker: '$^USLCB', Percentage: 30 }
                ];
                break;
            case 'conservative':
                portfolioConstituents = [
                    { Ticker: '$^USSCV', Percentage: 30 },
                    { Ticker: '$^USLCB', Percentage: 70 }
                ];
                break;
            case 'AVUV':
                portfolioConstituents = [
                    { Ticker: 'AVUV', Percentage: 100 }
                ];
                break;
            case 'SPY':
                portfolioConstituents = [
                    { Ticker: 'SPY', Percentage: 100 }
                ];
                break;
            case 'SPY/AVUV':
                portfolioConstituents = [
                    { Ticker: 'SPY', Percentage: 50 },
                    { Ticker: 'AVUV', Percentage: 50 }
                ];
                break;
            default:
                portfolioConstituents = [
                    { Ticker: '$^USSCV', Percentage: 50 },
                    { Ticker: '$^USLCB', Percentage: 50 }
                ];
        }

        const response = await fetch(`https://localhost:7219/api/BackTest/GetPortfolioBackTest`, {
            method: 'POST',
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                startingBalance: 10000,
                periodType,
                rebalanceStrategy: rebalanceFrequency,
                portfolioConstituents
            })
        });
        const data = await response.json();
        setPortfolioBackTest(data);
    }
}

export default App;