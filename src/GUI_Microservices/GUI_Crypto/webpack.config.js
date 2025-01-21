const path = require('path');

module.exports = {
    entry: {
        chart: './wwwroot/ts/chart/chart.ts',
        overview: './wwwroot/ts/overview/overview.ts'
    },
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                use: 'ts-loader',
                exclude: /node_modules/
            }
        ]
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js'],
        modules: [
            'node_modules',
            path.resolve(__dirname, 'wwwroot/ts')
        ]
    },
    output: {
        filename: '[name].js',
        path: path.resolve(__dirname, 'wwwroot/js')
    }
};
