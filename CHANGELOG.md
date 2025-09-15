# Changelog

All notable changes to PipeFlow will be documented in this file.

## [2.1.0] - 2024-01-14

### Added
- PostgreSQL support with reader and writer classes
- Bulk insert operations for PostgreSQL
- Upsert support with ON CONFLICT handling
- Async PostgreSQL operations

## [2.0.0] - 2024-01-14

### Added
- Builder pattern with lazy execution
- Full async/await support with CancellationToken
- Entity Framework Core integration
- IQueryable support with automatic paging
- Consistent From/To API naming pattern
- Streaming support for large datasets
- PipelineResult class with execution metrics
- Parallel processing improvements

### Changed
- API now uses builder pattern instead of static methods
- All I/O operations now have async versions
- Renamed WriteToXxx methods to ToXxx for consistency
- Improved memory efficiency with streaming

### Fixed
- Memory usage issues with large datasets
- Performance bottlenecks in parallel processing

## [1.0.0] - 2023-12-01

### Added
- Initial release
- Support for CSV, JSON, Excel, SQL Server, MongoDB
- Basic pipeline operations (Filter, Map, GroupBy)
- Cloud storage support (AWS S3, Azure Blob, Google Cloud Storage)
- REST API integration
- Data validation framework