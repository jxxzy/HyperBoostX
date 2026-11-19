# GPU Vendor Center v2.10.0

Release status: HyperBoostX v2.10.0 Owner Lab / Release Candidate

Status: PASS for vendor-neutral source contract; hardware lab expansion remains recommended.

## Supported Vendor Guidance

- NVIDIA
- AMD
- Intel
- Microsoft Basic Display Adapter
- Unknown GPU fallback

## Safety Boundary

GPU Center provides detection, driver guidance, vendor knowledge, and report/export paths. It does not perform overclock, undervolt, voltage, BIOS, forced driver-service changes, silent driver installs, or fake latest-driver claims.

## Evidence

- `app/services/monitoring/gpu_detection_service.py`
- `wpf/Views/GpuCenterView.xaml`
- `scripts/verify_gpu_vendor_neutrality.ps1`

