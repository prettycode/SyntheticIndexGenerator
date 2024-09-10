import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import BackTestApp from '../BackTestApp/BackTestApp';
import './BackTestIndex.css';

createRoot(document.getElementById('root')).render(
    <StrictMode>
        <BackTestApp />
    </StrictMode>
);
