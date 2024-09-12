import React from 'react';
import ReactDOM from 'react-dom/client';
import PortfolioAppIndex from './PortfolioAppIndex/PortfolioAppIndex';
import BackTestAppIndex from './BackTestAppIndex/BackTestAppIndex';

const SelectedApp = () => {
    const [selectedApp, setSelectedApp] = React.useState(null);

    return (
        <div>
            {!selectedApp && (
                <div>
                    <button onClick={() => setSelectedApp('portfolio')}>Portfolio Composition</button>
                    <button onClick={() => setSelectedApp('backtest')}>Portfolio Backtest</button>
                </div>
            )}
            {selectedApp === 'portfolio' && <PortfolioAppIndex />}
            {selectedApp === 'backtest' && <BackTestAppIndex />}
        </div>
    );
};

ReactDOM.createRoot(document.getElementById('root')).render(
    <React.StrictMode>
        <SelectedApp />
    </React.StrictMode>
);
