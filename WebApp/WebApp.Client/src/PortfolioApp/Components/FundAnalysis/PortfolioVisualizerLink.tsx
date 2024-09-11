import React, { useEffect, useState } from 'react';
import { getBacktestUrl } from '../../Fund/utils/getBacktestUrl';
import { FundAllocation } from '../../Fund/models/Fund/FundAllocation';
import { faExternalLink } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

type PortfolioVisualizerBacktestLinkProps = {
    allocations?: Array<FundAllocation>;
    url?: string;
};

export const PortfolioVisualizerLink: React.FC<
    PortfolioVisualizerBacktestLinkProps & React.HTMLAttributes<HTMLSpanElement>
> = ({ allocations, url, className, style }) => {
    const [calculatedUrl, setCalculatedUrl] = useState<string | undefined>(url ?? undefined);

    useEffect(() => {
        if (allocations) {
            (async () => setCalculatedUrl(await getBacktestUrl(allocations)))();
        } else if (url) {
            setCalculatedUrl(url);
        }
    }, [allocations, url]);

    return (
        <>
            <span
                title="PortfolioVisualizer.com"
                style={{ ...style, fontSize: 11 }}
                className={className ? `${className} badge bg-light text-dark` : 'badge bg-light text-dark'}
            >
                PV&nbsp;&nbsp;
                <a
                    href={calculatedUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    title="Open backtest in Portfolio Visualizer"
                >
                    <FontAwesomeIcon icon={faExternalLink} />
                </a>
            </span>
        </>
    );
};
