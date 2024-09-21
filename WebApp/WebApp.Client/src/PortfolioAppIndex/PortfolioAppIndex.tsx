import React from 'react';
import ReactDOM from 'react-dom/client';
import PortfolioApp from '../PortfolioApp/PortfolioApp';
import './PortfolioAppIndex.css';
import 'bootstrap/dist/css/bootstrap.css';
import 'font-awesome/css/font-awesome.min.css';

ReactDOM.createRoot(document.getElementById('root')).render(
    <React.StrictMode>
        <PortfolioApp />
    </React.StrictMode>
);
