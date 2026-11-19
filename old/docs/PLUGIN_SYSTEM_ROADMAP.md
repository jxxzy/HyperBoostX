# Plugin System Roadmap

v2.0.0 includes an internal registry foundation only. HyperBoostX does not load third-party plugin code in this release.

## Future Categories

- Network diagnostics
- GPU guidance
- Benchmark importers
- Cleanup scanners
- Startup analyzers
- Game profile providers
- Knowledge Base packs
- Report exporters
- Streaming checks
- RGB detection
- Hardware database adapters

## Security Requirements Before External Plugins

- No remote code loading by default.
- Signed plugin packages.
- Permission manifest reviewed before install.
- Sandbox or restricted execution model.
- No secret access unless explicitly granted.
- No mutating action without Safety Guard, session token, approval, and restore metadata.

## Current Status

Roadmap only. Do not advertise a plugin marketplace as complete.
