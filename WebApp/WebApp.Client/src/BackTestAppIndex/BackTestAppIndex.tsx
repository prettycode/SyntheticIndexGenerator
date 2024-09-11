import React, { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import BackTestApp from '../BackTestApp/BackTestApp';
import './BackTestAppIndex.css';

createRoot(document.getElementById('root')).render(
    <StrictMode>
        <BackTestApp />
    </StrictMode>
);
