# Code Signing Readiness

Status: `STABLE_UNSIGNED`

HyperBoostX v2.10.0 sudah dipaketkan dan dirilis sebagai stable unsigned build. Tidak ada certificate thumbprint atau PFX owner di workspace ini, jadi artifact tidak diklaim signed.

## Current Decision

- Installer v2.10.0 dirilis unsigned.
- Windows dapat menampilkan Unknown Publisher atau SmartScreen warning.
- User harus memverifikasi SHA256 sebelum install.
- Signing hanya boleh dilakukan jika owner menyediakan certificate/PFX resmi.

## Required Before Signed Release

- Obtain trusted code-signing certificate.
- Sign installer, launcher, WPF executable, and packaged backend executable.
- Verify every signed artifact with `Get-AuthenticodeSignature`.
- Regenerate checksum manifests after signing.
- Upload the signed installer and matching checksums to the GitHub Release.

## Command Template

```powershell
.\scripts\release\sign_release.ps1 -Thumbprint "<CERT_THUMBPRINT>"
Get-AuthenticodeSignature .\HyperBoostXInstaller.exe
```
