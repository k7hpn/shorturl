# Change Log

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/).

## Unreleased

### Added

- Logging to Seq
- Improved overall logging and containerized logging

### Changed

- Migrate from ASP.NET Core 2.2 to ASP.NET Core 8 (LTS)
- Update Docker build script
- Transition settings to be namespaced
- Cache values in JSON rather than as binary data

### Removed

- SQL Server based logging

## 1.0.0-beta4 - 2019-05-29

### Fixed

- Remove faulty IP address request muting

## 1.0.0-beta3 - 2019-05-28

### Changed

- Improve granularity in application version logging
- Mute common extension probe (.php)
- Mute common probes (favicon.ico, index.htm)
- Mute requests that come to an IP address

## 1.0.0-beta2 - 2019-05-23

### Changed

- Add configuration for Redis namespace

## 1.0.0-beta1 - 2019-05-22

### Added

- Ability to add multiple domains to a group for domain-specific routing
- Record redirections based on supplied stub after the URL
- Counting and last accessed for group and record accesses
- Detailed logging of each access for more specific reporting
- Caching of domains to reduce database lookups
- Ability to remove a domain from the cache if it has changed in the database
- Logging of missing domains and records
