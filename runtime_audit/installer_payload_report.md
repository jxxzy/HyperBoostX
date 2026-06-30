# HyperBoostX Installer Payload Verification

Expected version: 2.10.0-beta.1
Installer: F:\BOOSTER BY MR.4NONY\HyperBoostXInstaller.exe

| Check | Status | Evidence |
| --- | --- | --- |
| version sync script passed | PASS | scripts\\verify_version_sync.ps1 |
| release artifact content script passed | PASS | scripts\\verify_release_artifact_contents.ps1 |
| NSIS file exists | PASS | F:\BOOSTER BY MR.4NONY\HyperBoostXInstaller.nsi |
| NSIS DisplayVersion uses current version | PASS | expected DisplayVersion 2.10.0-beta.1 |
| NSIS writes HKLM uninstall metadata | PASS | uninstall registry entry |
| NSIS writes QuietUninstallString | PASS | silent uninstall metadata |
| NSIS writes owner publisher | PASS | publisher metadata |
| NSIS creates Start Menu shortcut | PASS | Start Menu shortcut |
| NSIS creates desktop shortcut | PASS | desktop shortcut |
| installer exists | PASS | F:\BOOSTER BY MR.4NONY\HyperBoostXInstaller.exe |
| installer hash available | PASS | 05e689131175efd1acfe40e6995b014f48228cd4156fcf99724283a3887f5a6d |
| v2.10 checksum file exists | PASS | F:\BOOSTER BY MR.4NONY\SHA256SUMS_v2.10.0-beta.1.txt |
| checksum file includes current installer hash | PASS | 05e689131175efd1acfe40e6995b014f48228cd4156fcf99724283a3887f5a6d |
