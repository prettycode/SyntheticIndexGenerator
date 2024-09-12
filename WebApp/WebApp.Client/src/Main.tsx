import React, { useState } from 'react';
import ReactDOM from 'react-dom/client';
import PortfolioAppIndex from './PortfolioAppIndex/PortfolioAppIndex';
import BackTestAppIndex from './BackTestAppIndex/BackTestAppIndex';

const SelectedApp = () => {
    const [selectedApp, setSelectedApp] = useState(null);

    const renderSelectedApp = () => {
        switch (selectedApp) {
            case 'portfolio':
                return <PortfolioAppIndex />;
            case 'backtest':
                return <BackTestAppIndex />;
            default:
                return null;
        }
    };

    return (
        <div>
            {!selectedApp ? (
                <div>
                    <button onClick={() => setSelectedApp('portfolio')}>Portfolio Composition</button>
                    <button onClick={() => setSelectedApp('backtest')}>Portfolio Backtest</button>
                </div>
            ) : (
                renderSelectedApp()
            )}
        </div>
    );
};

ReactDOM.createRoot(document.getElementById('root')).render(
    <React.StrictMode>
        <SelectedApp />
    </React.StrictMode>
);
