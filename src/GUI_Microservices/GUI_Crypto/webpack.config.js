import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export default {
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
