import globals from 'globals';
import pluginJs from '@eslint/js';
import tseslint from 'typescript-eslint';
import pluginReact from 'eslint-plugin-react';

export default [
    { files: ['src/**/*.{js,mjs,cjs,ts,jsx,tsx}'] },
    { ignores: ['dist'] },
    { settings: { react: { version: '18.3' } } },
    { languageOptions: { globals: globals.browser } },
    pluginJs.configs.recommended,
    ...tseslint.configs.recommended,
    pluginReact.configs.flat.recommended
];
