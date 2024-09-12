import React, { useEffect, useState } from 'react';
import { getFundAnalysisForCustomFund } from '../../Fund/transformers/FundAnalysis/getFundAnalysisForCustomFund';
import type { FundAnalysis } from '../../Fund/models/FundAnalysis/FundAnalysis';
import { FundAllocation } from '../../Fund/models/Fund/FundAllocation';
import { Fund } from '../../Fund/models/Fund/Fund';
import { fetchFundByFundId } from '../../Fund/services/fetchFundByFundId';
import { sum } from '../../Fund/utils/sum';
import './FundAnalysis.css';

interface FundAnalysisProps {
    fundAllocations: Array<Array<FundAllocation>>;
}

const showRedundantTitles = true;

const FundAnalysis: React.FC<FundAnalysisProps> = ({ fundAllocations }) => {
    const [fundAnalysis, setFundAnalysis] = useState<Array<FundAnalysis> | undefined>(undefined);
    const [fundLookupCache, setFundLookupCache] = useState<Record<string, Fund> | undefined>(undefined);

    useEffect(() => {
        (async () => {
            const cache: Record<string, Fund> = {};
            const analysis: Array<FundAnalysis> = await Promise.all(
                fundAllocations.map((portfolio) => getFundAnalysisForCustomFund(portfolio))
            );

            await Promise.all(
                analysis
                    .flatMap((a) => a.flattened)
                    .map(async (holding) => (cache[holding.fundId] = await fetchFundByFundId(holding.fundId)))
            );

            setFundAnalysis(analysis);
            setFundLookupCache(cache);
        })();
    }, [fundAllocations]);

    return (
        <>
            <h3>Portoflio Analysis</h3>
            {!fundAnalysis?.length && <>No completed portfolios to analyze.</>}
            {fundAnalysis &&
                fundLookupCache &&
                fundAnalysis.map((analysis, portfolioIndex) => (
                    <div
                        key={portfolioIndex}
                        className="float-start"
                        style={{ marginRight: 75 }}
                    >
                        <h4>{showRedundantTitles || portfolioIndex === 0 ? 'Portfolio Decomposed' : <>&nbsp;</>}</h4>
                        <table className="table table-sm">
                            <thead>
                                <tr>
                                    <th>Ticker</th>
                                    <th>Name</th>
                                    <th style={{ textAlign: 'right' }}>Weight</th>
                                </tr>
                            </thead>
                            <tbody>
                                {analysis.flattened.map((fund, index) => (
                                    <tr key={index}>
                                        <td style={{ width: '1%', paddingRight: '25px', whiteSpace: 'nowrap' }}>
                                            {fundLookupCache[fund.fundId].tickerSymbol}
                                        </td>
                                        <td>{fundLookupCache[fund.fundId].name}</td>
                                        <td style={{ textAlign: 'right' }}>{fund.percentage.toFixed(2)}%</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>

                        <h4>{showRedundantTitles || portfolioIndex === 0 ? 'Portfolio Leverage' : <>&nbsp;</>}</h4>
                        <div style={{ marginBottom: '1rem' }}>{analysis.leverage.toFixed(2)}&times;</div>

                        <h4>{showRedundantTitles || portfolioIndex === 0 ? 'Delevered Composition' : <>&nbsp;</>}</h4>
                        <table className="table table-sm">
                            <thead>
                                <tr>
                                    <th>Ticker</th>
                                    <th>Name</th>
                                    <th style={{ textAlign: 'right' }}>Weight</th>
                                </tr>
                            </thead>
                            <tbody>
                                {analysis.delevered.map((fund, index) => (
                                    <tr key={index}>
                                        <td style={{ width: '1%', paddingRight: '15px', whiteSpace: 'nowrap' }}>
                                            {fundLookupCache[String(fund.fundId)].tickerSymbol}
                                        </td>
                                        <td>{fundLookupCache[String(fund.fundId)].name}</td>
                                        <td style={{ textAlign: 'right' }}>{fund.percentage.toFixed(2)}%</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>

                        <h4>{showRedundantTitles || portfolioIndex === 0 ? 'Portfolio Asset Classes' : <>&nbsp;</>}</h4>
                        <table className="table table-sm">
                            <thead>
                                <tr>
                                    <th>Asset Class</th>
                                    <th style={{ textAlign: 'right' }}>Weight</th>
                                </tr>
                            </thead>
                            <tbody>
                                {analysis.decomposed.marketRegion &&
                                    Object.entries(analysis.decomposed.assetClass).map(([assetClass, funds], index) => (
                                        <tr key={index}>
                                            <td>{assetClass}</td>
                                            <td style={{ textAlign: 'right' }}>
                                                {sum(funds.map((fund) => fund.percentage)).toFixed(2)}%
                                            </td>
                                        </tr>
                                    ))}
                            </tbody>
                        </table>

                        <h4>
                            {showRedundantTitles || portfolioIndex === 0 ? (
                                <>
                                    Portfolio Regions <span style={{ fontSize: 'smaller' }}>(All Asset Classes)</span>
                                </>
                            ) : (
                                <>&nbsp;</>
                            )}
                        </h4>
                        <table className="table table-sm">
                            <thead>
                                <tr>
                                    <th>Region</th>
                                    <th style={{ textAlign: 'right' }}>Weight</th>
                                </tr>
                            </thead>
                            <tbody>
                                {Object.entries(analysis.decomposed.marketRegion).map(([region, funds], index) => (
                                    <tr key={index}>
                                        <td>{region}</td>
                                        <td style={{ textAlign: 'right' }}>
                                            {funds.reduce((total, fund) => total + fund.percentage, 0).toFixed(2)}%
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>

                        {Object.entries(analysis.decomposed.assetByRegion).map(([assetClass, regions]) => {
                            const totalPercentage = Object.values(regions)
                                .flat()
                                .reduce((acc, fund) => acc + fund.percentage, 0);

                            return (
                                <React.Fragment key={assetClass}>
                                    <h4>
                                        {showRedundantTitles || portfolioIndex === 0 ? (
                                            `${assetClass} by Region`
                                        ) : (
                                            <>&nbsp;</>
                                        )}
                                    </h4>
                                    <table className="table table-sm">
                                        <thead>
                                            <tr>
                                                <th>{assetClass}</th>
                                                <th style={{ textAlign: 'right' }}>Weight</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {regions &&
                                                Object.entries(regions).map(([region, funds]) => (
                                                    <tr key={region}>
                                                        <td>{region}</td>
                                                        <td style={{ textAlign: 'right' }}>
                                                            {totalPercentage === 0 && <>{(0).toFixed(2)}</>}
                                                            {totalPercentage !== 0 &&
                                                                (
                                                                    (funds.reduce(
                                                                        (acc, fund) => acc + fund.percentage,
                                                                        0
                                                                    ) /
                                                                        totalPercentage) *
                                                                    100
                                                                ).toFixed(2)}
                                                            %
                                                        </td>
                                                    </tr>
                                                ))}
                                        </tbody>
                                    </table>
                                </React.Fragment>
                            );
                        })}
                    </div>
                ))}
        </>
    );
};

export default FundAnalysis;
