# HyperBoostX v3

Aplikasi desktop Windows, dibuat di folder `v3 stable final`. Nama folder adalah lokasi kerja, **bukan klaim bahwa semua persyaratan Stable sudah selesai**.

## Instal dan pakai

1. Buka `dist/HyperBoostX-v3-Setup-x64.exe`.
2. Pilih Install, lalu jalankan shortcut **HyperBoostX v3**.
3. Mulai dari **Smart Scan**. Pilih Center untuk membaca data yang relevan.
4. Pada Optimize, Gaming, Startup atau Power: pilih perubahan, tekan **Preview & Apply**, tinjau before/after, lalu setujui bila sesuai.
5. **Restore** menampilkan perubahan yang dikelola aplikasi. Gunakan **Verify current state** atau **Restore selected**. Konflik dengan perubahan eksternal memblokir rollback otomatis.

Installer untuk Windows x64 menyertakan runtime .NET. Pengguna tidak perlu menginstal SDK, Python, Flask, atau membuka terminal. Instalasi per-user tidak memerlukan seluruh aplikasi berjalan sebagai Administrator. Paket belum ditandatangani dengan sertifikat produksi.

Paket portable: ekstrak `dist/HyperBoostX-v3-Portable-x64.zip`, lalu buka `HyperBoostX.exe`. Data portable dan installed disimpan pada lokasi lokal pengguna yang sama; tidak ada service yang diinstal diam-diam.

## Yang tersedia

- Pembacaan langsung OS, CPU, RAM, GPU/driver, drive, jaringan, aplikasi, startup, perangkat, Defender, Firewall, TPM, power plan, creator devices dan Event Log.
- Preferensi transparency dan game capture, disable HKCU Run yang dipilih, pemilihan power plan yang benar-benar tersedia.
- Preview sekali pakai, Safety Guard, backup DPAPI, jurnal SQLite, read-back, rollback dengan deteksi drift, recovery record terpisah dari database.
- Ping enam sampel, laporan JSON/CSV/Markdown, support ZIP berisi metadata terbatas, Knowledge Base lokal.
- Search fitur/intent, tema dark/light, tujuan penggunaan, detail Expert, pemulihan posisi jendela, rendering software untuk kompatibilitas.
- Handoff ke alat Windows resmi. Handoff diberi label sebagai pembukaan alat eksternal, bukan hasil perubahan yang diverifikasi HyperBoostX.

Lihat [RELEASE_STATUS.md](RELEASE_STATUS.md) untuk batas implementasi. Fitur dan benchmark yang tidak tersedia tidak diberi angka buatan.

Perbaikan dan bukti pemeriksaan terbaru: [Audit 16 November 2026](docs/AUDIT_FIX_2026-11-16.md). Reports kini dapat membuka kembali 100 laporan tersimpan; CLI juga menyediakan `ping <host>` dan `reports`.

## Data dan recovery

Data: `%LOCALAPPDATA%\HyperBoostX\v3`. Riwayat/backup sensitif dienkripsi DPAPI untuk akun Windows yang sama. Jangan memindahkan backup ke akun lain dan berharap dapat melakukan restore.

`hbx.exe --help` menyediakan CLI pemulihan jika GUI gagal. Contoh: `hbx.exe history`, `hbx.exe verify <change-id>`, `hbx.exe rollback <change-id> --yes`. Jalankan di akun Windows yang membuat perubahan.

Uninstall mempertahankan konfigurasi Windows dan data recovery. Tinjau Restore sebelum uninstall jika ingin membatalkan perubahan. Instal ulang akan memakai data lokal yang sama.

## Build

Windows, .NET SDK 8.0.425, NSIS 3 diperlukan untuk membangun paket, bukan untuk menjalankannya.

```powershell
dotnet build HyperBoostX.sln -c Release
dotnet test HyperBoostX.sln -c Release
powershell -ExecutionPolicy Bypass -File scripts/package.ps1
```

Audit historis dapat diulang dengan `python scripts/audit_legacy.py` dari workspace yang memiliki V1 dan riwayat Git. Build baru tidak bergantung pada Flask atau executable beta.
