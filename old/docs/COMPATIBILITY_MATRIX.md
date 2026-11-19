# Compatibility Matrix

Audit date: 2026-06-27

| Dimension | Tested here | Status | Notes |
| --- | --- | --- | --- |
| Windows 10 Pro build 26200 x64 | Yes | PASS | Current environment. |
| Windows 11 | No | PARTIAL | Needs lab. |
| NVIDIA GTX/RTX | No real hardware proof in this pass | PARTIAL | Detection/provider code and tests exist. |
| AMD RX | No | PARTIAL | Vendor-aware fallback exists. |
| Intel Arc/iGPU | No | PARTIAL | Vendor-aware fallback exists. |
| Microsoft Basic/Unknown GPU | Simulated/fallback code | PARTIAL | Route/tests cover fallback. |
| Intel CPU | Environment-dependent, not asserted | PARTIAL | System info service exists. |
| AMD Ryzen | No | PARTIAL | Needs lab. |
| 8GB/16GB/32GB RAM | Current machine only | PARTIAL | Detection route exists. |
| SSD/HDD/NVMe | Current machine only | PARTIAL | Storage route exists. |
| Online | Yes | PASS | Local tests do not require cloud except optional provider. |
| Offline | Not explicitly tested | PARTIAL | Core local backend should work; provider checks become setup-required. |
| API key missing | Yes | PASS | NVIDIA setup status test is friendly. |
| Non-admin | Yes | PASS | Preview/safety paths tested. |
| Admin | No | BLOCKED | Need elevated owner lab. |

Final compatibility status: PARTIAL because hardware and admin matrix cannot be fully proven on one non-elevated machine.

