const path = require('path');

module.exports = {
    entry: './wwwroot/ts/chart/chart.ts', // Updated path to reflect new structure
    output: {
        filename: 'bundle.js',
        path: path.resolve(__dirname, 'wwwroot/js'),
    },
    resolve: {
        extensions: ['.ts', '.js'],
        modules: [
            'node_modules',
            path.resolve(__dirname, 'wwwroot/ts')
        ]
    },
    module: {
        rules: [
            {
                test: /\.ts$/,
                use: 'ts-loader',
                exclude: /node_modules/,
            },
        ],
    },
    mode: 'development',
};
