import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import PortfolioApp from '../PortfolioApp/PortfolioApp';
import './PortfolioAppIndex.css';
import 'bootstrap/dist/css/bootstrap.css';
import 'font-awesome/css/font-awesome.min.css';

createRoot(document.getElementById('root')).render(
    <StrictMode>
        <PortfolioApp />
    </StrictMode>
);