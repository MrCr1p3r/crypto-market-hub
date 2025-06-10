import eslint from '@eslint/js';
import tseslint from '@typescript-eslint/eslint-plugin';
import tsparser from '@typescript-eslint/parser';
import prettier from 'eslint-plugin-prettier';
import eslintConfigPrettier from 'eslint-config-prettier';
import globals from 'globals';

export default [
    eslint.configs.recommended,
    {
        files: ['**/*.ts'],
        languageOptions: {
            parser: tsparser,
            parserOptions: {
                ecmaVersion: 'latest',
                sourceType: 'module'
            },
            globals: {
                ...globals.browser,
            }
        },
        plugins: {
            '@typescript-eslint': tseslint,
            'prettier': prettier
        },
        rules: {
            'prettier/prettier': 'error',
            '@typescript-eslint/explicit-function-return-type': 'warn',
            '@typescript-eslint/no-explicit-any': 'warn',
            '@typescript-eslint/no-unused-vars': 'warn'
        }
    },
    {
        ignores: ['node_modules/**', 'wwwroot/js/**']
    },
    eslintConfigPrettier
]; 
