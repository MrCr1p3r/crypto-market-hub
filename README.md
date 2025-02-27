# CryptoChartAnalyzer

## Central Package Management

This solution uses .NET's Central Package Management (CPM) to ensure consistent package versions across all projects.

### How It Works

- **Directory.Packages.props**: Contains all package versions used across the solution
- **Directory.Build.props**: Ensures all projects use central package management

### Adding a New Package

To add a new package to the solution:

1. Add the package version to Directory.Packages.props
2. Reference the package in your project file without specifying the version

### Updating Package Versions

To update a package version, only update the version in Directory.Packages.props. All projects using that package will automatically use the new version.

### Benefits

- Consistent package versions across all projects
- Simplified package management
- Easier dependency updates
- Reduced risk of version conflicts
