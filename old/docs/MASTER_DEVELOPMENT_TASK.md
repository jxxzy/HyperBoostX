# MASTER DEVELOPMENT TASK — HYPERBOOSTX V3 FINAL
## FULL IMPLEMENTATION, REAL ACTIONS, PRODUCTION-READY RELEASE

**Product:** HyperBoostX V3  
**Target platform:** Windows 10 & Windows 11  
**Development status target:** Production Ready — Stable Release  
**Task type:** Full Repository Audit, Development, Implementation, Testing, Build & Release

---

## 1. PRIMARY DIRECTIVE

Bertindak sebagai seluruh tim pengembangan HyperBoostX, meliputi:

- Principal Software Architect
- Senior Windows Software Engineer
- Senior .NET Developer
- Python Backend Engineer
- Windows Performance Engineer
- AI Integration Engineer
- Cybersecurity Engineer
- UI/UX Designer
- QA Automation Engineer
- DevOps & Release Engineer

Tanggung jawab utama adalah menyelesaikan HyperBoostX V3 menjadi aplikasi yang benar-benar siap digunakan, bukan hanya prototype, mockup, proof of concept, atau kumpulan dokumentasi.

Lanjutkan dari kondisi repository saat ini. Jangan menghapus progres sebelumnya atau melakukan rewrite tanpa alasan teknis yang tervalidasi.

Kerjakan seluruh implementasi secara langsung pada source code.

Jangan berhenti setelah memberikan analisis, rekomendasi, roadmap, checklist, atau laporan audit. Implementasikan seluruh perbaikan yang dapat dikerjakan, jalankan pengujian, dan hasilkan build aplikasi.

---

## 2. NON-NEGOTIABLE REQUIREMENTS

Seluruh ketentuan berikut wajib dipenuhi:

- 100% fitur memiliki fungsi nyata.
- 100% tombol memiliki fungsi yang sesuai.
- 100% menu legacy memiliki pemetaan fitur.
- Zero dummy features.
- Zero placeholder functionality.
- Zero fake success.
- Zero fake benchmark.
- Zero simulated optimization pada production mode.
- Zero hardcoded performance improvement.
- Zero silent failure.
- Zero broken navigation.
- Zero unhandled exception pada skenario yang diuji.
- Zero exposed credentials.
- Zero unfinished critical features.

Dilarang menganggap fitur selesai hanya karena UI berhasil ditampilkan, API mengembalikan HTTP 200, atau proses berhasil dijalankan.

Keberhasilan optimasi harus diverifikasi melalui kondisi sistem Windows yang sebenarnya.

Apabila suatu fungsi tidak didukung oleh hardware, Windows, driver, atau permission, tampilkan status Unsupported atau Limited Mode beserta alasan yang jelas.

Jangan memalsukan keberhasilan.

---

## 3. FULL REPOSITORY AUDIT

Sebelum melakukan perubahan besar, audit seluruh repository, termasuk:

- Source code.
- Git history dan tags yang tersedia.
- Legacy versions V1 sampai V2.10.
- Implementasi V3 terbaru.
- Frontend.
- Backend.
- API routes.
- Services.
- Scripts.
- Windows Registry operations.
- Dependencies.
- Installer.
- Updater.
- Tests.
- Documentation.
- Assets.
- Configuration files.
- Security implementation.
- Dead code dan duplicate code.

Gunakan catatan historis 73 menu, 606 tombol/actions, 361 API paths, dan 365 route rules sebagai referensi audit, bukan sebagai angka yang wajib dipalsukan apabila repository aktual berbeda.

Buat Legacy Feature Registry yang memetakan:

- Feature ID.
- Nama fitur.
- Versi asal.
- Lokasi implementasi.
- UI handler.
- Backend handler.
- Hardware requirements.
- Risk level.
- Verification method.
- Rollback method.
- Test coverage.
- Implementation status.

Seluruh fitur lama harus dipertahankan, diperbaiki, digabungkan secara logis, atau diganti dengan implementasi yang lebih baik.

Fitur berbahaya atau tidak didukung harus memiliki alasan penghentian dan alternatif yang aman.

---

## 4. APPLICATION ARCHITECTURE

Pertahankan arsitektur yang sudah berjalan apabila masih layak digunakan.

Evaluasi dan integrasikan komponen berikut:

- C# dan .NET.
- WPF desktop application.
- MVVM architecture.
- Python Flask backend apabila masih dibutuhkan.
- .NET launcher.
- Windows service atau privileged worker.
- Secure local IPC.
- SQLite local database.
- Hardware detection engine.
- Action execution engine.
- Safety engine.
- Backup and restore engine.
- Diagnostics engine.
- AI provider abstraction.
- Installer and updater.

Apabila repository V3 telah menggunakan arsitektur .NET yang lebih baru, lanjutkan arsitektur tersebut dan migrasikan fitur legacy secara terverifikasi.

Pastikan backend lifecycle, startup, shutdown, crash recovery, timeout, cancellation, dan permission management berjalan dengan baik.

Jangan membuka backend lokal ke jaringan publik secara default.

---

## 5. MANDATORY FEATURE CENTERS

Implementasikan seluruh pusat fitur berikut dengan aksi nyata.

### A. Intelligent Dashboard

- Real-time CPU usage.
- GPU usage.
- RAM usage.
- VRAM usage.
- Storage usage.
- Network activity.
- Temperature monitoring.
- CPU and GPU clock monitoring.
- Windows information.
- System uptime.
- Hardware health.
- Active performance profile.
- Background process monitoring.
- Optimization history.
- System health recommendations.

Data wajib berasal dari telemetry yang benar-benar tersedia.

### B. Smart Scan & One Click Boost

- Full system scan.
- Hardware capability analysis.
- Windows configuration analysis.
- Startup analysis.
- Background application analysis.
- Storage health analysis.
- Driver health analysis.
- Gaming readiness analysis.
- Bottleneck indicators.
- Recommended optimizations.
- Safe One Click Boost.
- Post-optimization verification.

One Click Boost hanya menjalankan tindakan kompatibel dan aman yang telah disetujui pengguna.

### C. CPU Optimization Center

- CPU identification.
- Logical and physical core detection.
- CPU utilization monitoring.
- Power plan management.
- Process priority management.
- CPU affinity management untuk aplikasi yang kompatibel.
- Background workload management.
- Intel compatibility.
- AMD compatibility.
- Thermal monitoring.
- CPU bottleneck analysis.

Jangan melakukan perubahan voltage, multiplier, BIOS, atau overclock otomatis.

### D. RAM & Smart Memory Guard

- Memory diagnostics.
- RAM utilization monitoring.
- Memory pressure detection.
- High-memory process identification.
- Memory usage history.
- Standby memory diagnostics.
- Pagefile analysis.
- Safe pagefile configuration.
- Memory leak indicators.
- Gaming memory recommendations.
- Configurable Smart Memory Guard.
- Emergency memory pressure handling.

Hindari pemaksaan memory purge berkala yang dapat menurunkan performa atau menyebabkan stutter.

### E. GPU Optimization Center

- NVIDIA GPU detection.
- AMD GPU detection.
- Intel GPU detection.
- GPU driver identification.
- VRAM monitoring.
- GPU temperature monitoring.
- Hardware acceleration analysis.
- Supported GPU configuration.
- Shader cache diagnostics.
- Shader cache management.
- GPU workload monitoring.
- GPU bottleneck detection.
- Vendor-specific performance recommendations.

Jangan mengubah voltage, firmware, power limit, atau clock secara berbahaya.

### F. Gaming & FPS Optimization Center

- Automatic game detection.
- Installed game library.
- Steam game detection.
- Epic Games detection.
- Rockstar Games detection.
- Xbox game detection.
- Manual game registration.
- Per-game performance profile.
- Automatic profile activation.
- Automatic profile restoration.
- Background application management.
- Gaming power profile.
- Game Mode management.
- Graphics configuration recommendations.
- FPS monitoring apabila telemetry tersedia.
- Frame time monitoring.
- 1% low FPS measurement apabila data mencukupi.
- Stutter detection.
- Game session reports.
- Game performance history.

Prioritaskan kompatibilitas untuk GTA V Legacy, GTA V Enhanced, game kompetitif, game AAA, dan game berbasis Unreal Engine maupun Unity.

Dilarang melakukan modifikasi yang mengganggu anti-cheat atau integritas game.

### G. Storage Optimization Center

- HDD detection.
- SATA SSD detection.
- NVMe SSD detection.
- Storage health information.
- SMART status apabila tersedia.
- Disk space analysis.
- Temporary files cleanup.
- Windows cache cleanup.
- Application cache cleanup.
- Recycle Bin management.
- Duplicate file detection.
- Large file detection.
- TRIM capability analysis.
- Windows-supported drive optimization.
- Storage benchmark.
- Storage bottleneck analysis.

Jangan menjalankan defragmentasi tradisional pada SSD secara paksa.

File pribadi tidak boleh dihapus tanpa preview dan persetujuan pengguna.

### H. Network Optimization Center

- Network adapter detection.
- Connection diagnostics.
- Ping measurement.
- Jitter measurement.
- Packet loss measurement.
- DNS benchmark.
- DNS configuration.
- DNS cache management.
- Network troubleshooting.
- TCP configuration analysis.
- Network latency profile.
- Network interface diagnostics.
- Gaming network recommendations.
- Network reset melalui metode Windows yang didukung.

Jangan menjanjikan ping lebih rendah apabila faktor pembatas berada pada ISP atau server tujuan.

### I. Windows Optimization Center

- Windows configuration diagnostics.
- Startup optimization.
- Background application control.
- Approved scheduled task management.
- Safe service management.
- Visual effect optimization.
- Power management.
- Search indexing analysis.
- Windows notification management.
- Storage Sense integration.
- Privacy settings management.
- Supported registry optimizations.
- Windows feature compatibility.

Semua perubahan harus mempertimbangkan dependensi Windows.

### J. Debloat Center

- Installed application inventory.
- Microsoft Store application detection.
- Optional application analysis.
- Safe debloat recommendations.
- Application uninstall.
- Restore instructions.
- Essential application protection.
- Dependency-aware package management.
- User-defined exclusions.

Jangan menghapus komponen Windows yang diperlukan untuk boot, login, security, Microsoft Store dependencies, atau fungsi sistem lainnya.

### K. Driver Health Center

- Installed driver inventory.
- Driver version detection.
- Device health detection.
- Missing driver detection.
- Problematic device detection.
- Driver compatibility recommendations.
- Official vendor support links.
- Supported driver backup and recovery.
- Windows Device Manager integration.

Dilarang melakukan instalasi driver otomatis dari sumber tidak terpercaya.

### L. Boot Optimization Center

- Startup program inventory.
- Startup impact analysis.
- Boot time measurement.
- Boot performance history.
- Startup application management.
- Login performance analysis.
- Windows boot event analysis.

Jangan mengubah konfigurasi boot berisiko tinggi secara otomatis.

### M. Repair & Troubleshooting Center

- Windows system file verification.
- SFC integration.
- DISM integration.
- Windows Update troubleshooting.
- Network repair.
- DNS repair.
- Storage diagnostics.
- Windows service diagnostics.
- Application troubleshooting.
- Error code analysis.
- Repair history.
- Post-repair verification.

### N. Creator & Streaming Center

- OBS detection.
- Streamlabs detection.
- Video encoder capability detection.
- NVIDIA NVENC detection.
- AMD AMF detection.
- Intel Quick Sync detection.
- Streaming performance profile.
- Recording performance profile.
- OBS configuration recommendations.
- CPU and GPU workload analysis.
- Creator application performance recommendations.
- Audio application performance profile.

### O. Hardware Diagnostics Center

- CPU information.
- GPU information.
- Motherboard information.
- RAM information.
- Storage information.
- BIOS information dalam mode read-only.
- Network hardware information.
- Battery health apabila tersedia.
- Sensor monitoring.
- Thermal warnings.
- Hardware compatibility analysis.
- Exportable system information.

### P. Performance Profile Center

Sediakan profil berikut:

- Balanced.
- Maximum Safe Performance.
- Gaming.
- Competitive Gaming.
- Streaming.
- Content Creation.
- Office.
- Low Specification PC.
- Laptop Battery.
- Quiet Mode.
- Custom Profile.
- Windows Default Restore.

Setiap profil harus memiliki daftar tindakan nyata, compatibility check, conflict detection, preview, verification, dan rollback.

### Q. Safe Tweak Library

Targetkan library hingga 300+ tindakan optimasi yang benar-benar unik apabila dapat dibuktikan aman dan bermanfaat.

Jangan menambah tweak palsu, duplikat, atau tidak relevan untuk mengejar jumlah.

Setiap tweak wajib memiliki:

- Unique ID.
- Description.
- Technical explanation.
- Supported Windows version.
- Hardware requirements.
- Required permission.
- Current value.
- Recommended value.
- Risk classification.
- Expected behavior.
- Apply implementation.
- Verification implementation.
- Rollback implementation.
- Test coverage.

### R. Automation Center

- Scheduled scans.
- Scheduled cleanup.
- Hardware monitoring rules.
- Temperature notifications.
- Memory pressure notifications.
- Game launch detection.
- Automatic performance profile switching.
- Automatic profile restoration.
- Automation history.
- Automation cancellation.
- Emergency stop.

Automation harus bersifat opt-in.

### S. Benchmark & Stutter Analyzer

- CPU benchmark.
- Storage benchmark.
- Memory benchmark apabila tersedia.
- GPU benchmark melalui metode yang valid.
- Boot time benchmark.
- Network latency benchmark.
- FPS and frame time analysis apabila tersedia.
- Before and after comparison.
- Performance session history.
- Exportable benchmark results.

Gunakan pengukuran nyata, kondisi pengujian yang sebanding, dan timestamp.

Jangan membuat angka peningkatan performa secara statis maupun menggunakan estimasi AI sebagai hasil benchmark.

### T. Diagnostics, Reports & Device Management

- System diagnostic logs.
- Windows event analysis.
- Application error analysis.
- Device inventory.
- Device status.
- Optimization history.
- Exportable diagnostic reports.
- HTML report.
- JSON report.
- CSV report.
- Search and filtering.
- Privacy-aware log redaction.

### U. Plugin Management & Trust Center

- Plugin inventory.
- Plugin compatibility verification.
- Plugin permission management.
- Trusted plugin validation.
- Plugin enable/disable.
- Plugin isolation sesuai kemampuan arsitektur.
- Plugin activity logs.

Jangan menjalankan kode plugin yang tidak terpercaya secara otomatis.

---

## 6. AI ENGINE — NVIDIA INTEGRATION

Implementasikan AI Engine yang benar-benar terhubung dengan provider yang valid.

Fitur:

- NVIDIA AI provider integration.
- Hingga 10 pilihan model yang benar-benar tersedia dan kompatibel.
- Dynamic model discovery apabila didukung provider.
- Bring Your Own API Key.
- Gateway integration apabila dikonfigurasi.
- Secure credential storage.
- Connection testing.
- Model selection.
- AI troubleshooting assistant.
- AI performance analysis.
- AI hardware recommendations.
- AI optimization planning.
- AI explanation untuk setiap tindakan.
- Request timeout handling.
- Rate limit handling.
- Provider error handling.
- Local rule-based fallback.
- Offline optimization workflow.

Jangan menggunakan daftar model fiktif atau API endpoint palsu.

AI tidak boleh menjalankan perintah shell arbitrer atau melakukan perubahan sistem secara langsung.

Alur AI wajib:

Scan PC → Hardware Analysis → AI Recommendation → Safety Validation → User Approval → Action Engine → Verification → Performance Report.

Aplikasi harus tetap dapat digunakan tanpa koneksi AI.

---

## 7. REAL ACTION EXECUTION ENGINE

Seluruh aksi perubahan sistem wajib melalui lifecycle terpusat:

Detect → Validate → Preview → Approve → Backup → Apply → Verify → Evidence → History → Rollback.

Implementasikan status:

- Ready.
- Running.
- Success.
- Already Applied.
- Failed.
- Verification Failed.
- Unsupported.
- Blocked by Safety Guard.
- Permission Required.
- Restart Required.
- Rolled Back.

Success hanya diberikan apabila kondisi sistem setelah perubahan telah diverifikasi.

Apabila perubahan gagal sebagian, lakukan rollback apabila memungkinkan dan simpan bukti kegagalan.

Dukung asynchronous execution, timeout, cancellation yang aman, transaction journal, concurrency control, dan recovery setelah crash atau reboot.

---

## 8. SAFETY GUARD

Implementasikan centralized Safety Guard yang wajib digunakan oleh seluruh fitur.

Klasifikasi:

- Read Only.
- Safe.
- Moderate Risk.
- Advanced.
- Restricted.
- Blocked.

Wajib memblokir:

- Unsafe registry modification.
- Irreversible critical system modifications.
- Boot configuration berbahaya.
- Windows Defender permanent disable.
- Windows Update permanent disable.
- BIOS modification.
- Automatic overclock.
- Automatic undervolt.
- Voltage modification.
- Arbitrary shell execution dari AI.
- Automatic untrusted driver installation.
- Penghapusan critical Windows files.
- Penghapusan data pengguna tanpa persetujuan.

Gunakan least privilege dan privilege elevation hanya ketika diperlukan.

---

## 9. BACKUP, RESTORE & ROLLBACK

Implementasikan:

- Pre-action backup.
- Registry value backup.
- Registry existence tracking.
- Service configuration backup.
- Power plan backup.
- Application configuration backup.
- Supported Windows Restore Point integration.
- Individual action rollback.
- Batch rollback.
- Profile rollback.
- Restore history.
- Emergency recovery.
- Crash recovery.
- Reboot recovery.
- Backup integrity verification.

Untuk tindakan reversible, rollback wajib benar-benar mengembalikan kondisi sebelumnya.

Untuk tindakan yang tidak reversible, tampilkan peringatan dan persetujuan eksplisit sebelum eksekusi.

Jangan mengklaim berhasil membuat restore point apabila Windows menolak permintaan tersebut.

---

## 10. LCD PERFORMANCE GUARD

Pertahankan fitur LCD Performance Guard dalam area Privacy & Security.

Implementasikan:

- LCD and display software detection.
- Resource usage monitoring.
- Background process monitoring.
- High CPU usage detection.
- High GPU usage detection.
- Configurable thresholds.
- Safe process management.
- User-defined exclusions.
- Resource usage history.
- Performance notifications.

Pastikan kompatibilitas secara aman terhadap software monitoring, AIO LCD, external LCD, sensor display, dan hardware utilities.

Jangan menghentikan komponen pendinginan penting atau software pengendali pompa dan kipas secara otomatis.

---

## 11. LICENSE & CLOUD CENTER

Pertahankan License Center sesuai arsitektur local-only yang telah ditentukan sebelumnya.

Implementasikan:

- Local license information.
- Local application entitlement apabila diperlukan.
- Local configuration management.
- License status diagnostics.
- Application version information.
- Secure local state.
- Privacy information.

Jangan mengarang sistem Free/Pro, aktivasi key online, device limit, cloud synchronization, atau subscription server yang belum tersedia.

Jangan menciptakan backend lisensi palsu.

---

## 12. DISCORD WEBHOOK & ERROR REPORTING

Implementasikan Discord Webhook integration yang nyata untuk:

- Application errors.
- Critical exceptions.
- Backend crashes.
- Failed actions.
- Installer errors.
- Update errors.
- Automated test results.
- Release build results.

Wajib tersedia:

- Webhook configuration.
- Test connection.
- Enable/disable.
- Privacy consent.
- Log redaction.
- Rate limiting.
- Retry policy.
- Local log fallback.

Jangan memasukkan webhook URL, API keys, password, username, data pribadi, atau credential ke source code maupun laporan publik.

---

## 13. PREMIUM UI/UX

HyperBoostX V3 harus memiliki tampilan modern dan profesional.

Implementasikan:

- Modern Windows desktop design.
- Responsive application layout.
- Dark and Light Mode.
- Sidebar navigation.
- Global search.
- Real-time dashboard.
- Performance charts.
- Hardware cards.
- Interactive action panels.
- Feature descriptions.
- Loading indicators.
- Progress indicators.
- Confirmation dialogs.
- Error dialogs.
- Notification center.
- Settings.
- Action history.
- Accessibility.
- High DPI compatibility.
- Keyboard navigation.

Sediakan tiga mode penggunaan:

**Beginner Mode:** optimasi aman dan sederhana.

**Advanced Mode:** kontrol fitur yang lebih lengkap.

**Expert Mode:** diagnostic dan konfigurasi lanjutan yang tetap dilindungi Safety Guard.

Jangan membuat halaman yang hanya berisi kumpulan tombol.

Setiap halaman wajib menjelaskan fungsi, kondisi sistem, manfaat, risiko, dan hasil tindakan.

---

## 14. WINDOWS COMPATIBILITY

Target compatibility:

- Windows 10.
- Windows 11.
- Windows Home.
- Windows Pro.
- Windows Enterprise.
- Windows Education.
- Windows LTSC.
- Modified Windows.

Hardware coverage:

- Intel.
- AMD.
- NVIDIA.
- Intel integrated graphics.
- AMD integrated graphics.
- Low-spec computers.
- High-end gaming computers.
- Desktop computers.
- Laptops.
- Virtual machines untuk pengujian.

Terapkan capability detection.

Untuk Windows modifikasi, gunakan Limited Mode jika komponen yang dibutuhkan telah dihapus oleh sistem operasi.

Bedakan status kompatibilitas berdasarkan hasil pengujian nyata, bukan hanya nama versi Windows.

---

## 15. APPLICATION PERFORMANCE

HyperBoostX tidak boleh menjadi penyebab komputer semakin berat.

Optimalkan:

- Application startup time.
- Idle memory usage.
- Idle CPU usage.
- Background service overhead.
- Telemetry polling interval.
- GPU rendering overhead.
- Database performance.
- Log rotation.
- IPC performance.
- Resource disposal.
- Process lifecycle.
- Memory leak prevention.

Jalankan profiling dan dokumentasikan hasil pengukuran aktual.

---

## 16. FULL AUTOMATED TESTING

Implementasikan dan jalankan:

- Unit tests.
- Integration tests.
- Backend API tests.
- UI automation tests.
- Button binding tests.
- Navigation tests.
- Action execution tests.
- Registry verification tests.
- Backup tests.
- Rollback tests.
- Safety Guard tests.
- Hardware compatibility tests.
- AI provider tests.
- Webhook tests.
- Database tests.
- Installer tests.
- Updater tests.
- Uninstaller tests.
- Crash recovery tests.
- Reboot recovery tests.
- Security tests.
- End-to-end tests.
- Installed runtime smoke tests.

Gunakan isolated test environment atau disposable Windows VM untuk pengujian perubahan sistem.

Mock diperbolehkan untuk unit testing, tetapi bukan pengganti pengujian aksi nyata.

Seluruh critical tests wajib lulus.

Pengujian yang tidak dapat dijalankan harus ditandai NOT RUN atau BLOCKED, bukan PASS.

---

## 17. INSTALLER, UPDATER & UNINSTALLER

Hasil akhir wajib berupa aplikasi Windows yang dapat digunakan tanpa menjalankan source code secara manual.

Sediakan installer sesuai kebutuhan tipe Windows:

- HyperBoostX V3 — Standard Windows.
- HyperBoostX V3 — Windows LTSC.
- HyperBoostX V3 — Modified Windows.

Ketiga installer harus menggunakan core aplikasi yang sama. Perbedaan hanya pada compatibility handling, prerequisites, dan konfigurasi instalasi yang diperlukan.

Installer wajib mendukung:

- Dependency installation.
- Prerequisite detection.
- Installation wizard.
- Application shortcuts.
- Clean installation.
- Upgrade installation.
- Existing configuration preservation.
- Database migration.
- Repair installation.
- Clean uninstallation.
- Failed installation recovery.
- Update verification.
- Application signing apabila certificate tersedia.
- SHA-256 checksums.
- Secure update mechanism.

Jangan meminta pengguna menginstal Python, Visual Studio, atau dependency development secara manual.

Jangan menyertakan secrets, private development paths, maupun PDB debug files yang tidak dibutuhkan dalam distribusi publik.

---

## 18. FINAL DELIVERABLES

Hasilkan deliverables berikut:

1. Complete production source code.
2. Windows executable.
3. Standard Windows installer.
4. LTSC Windows installer.
5. Modified Windows installer.
6. Portable build apabila arsitektur mendukung.
7. Update package.
8. Release manifest.
9. SHA-256 checksum files.
10. Changelog.
11. User manual.
12. Developer documentation.
13. Legacy Feature Registry.
14. Full Feature Audit.
15. Full QA Matrix.
16. Security Audit.
17. Compatibility Matrix.
18. Benchmark Report.
19. Installer Validation Report.
20. Known Issues Report.
21. Final Release Readiness Report.

Gunakan folder release yang terstruktur dan sesuai dengan build system repository.

Sediakan build scripts yang dapat dijalankan ulang secara konsisten.

---

## 19. EXECUTION STRATEGY

Kerjakan secara berurutan:

PHASE 1 — Repository Audit & Legacy Mapping.

PHASE 2 — Architecture Stabilization.

PHASE 3 — Core Action Engine.

PHASE 4 — Safety, Backup & Rollback.

PHASE 5 — Complete Feature Implementation.

PHASE 6 — AI, License, LCD & Discord Integration.

PHASE 7 — UI/UX Completion.

PHASE 8 — Performance Optimization.

PHASE 9 — Security Hardening.

PHASE 10 — Automated Testing.

PHASE 11 — Installer & Updater Build.

PHASE 12 — Installed Runtime Validation.

PHASE 13 — Final Release Packaging.

Jangan menunggu persetujuan pada setiap fase untuk pekerjaan pengembangan biasa.

Ambil keputusan teknis yang diperlukan berdasarkan repository aktual.

Jangan melakukan tindakan destruktif pada komputer host, menghapus data pengguna, atau melakukan deployment publik tanpa otorisasi.

Simpan checkpoint progres dalam DEVELOPMENT_PROGRESS.md agar pekerjaan dapat dilanjutkan apabila sesi atau context window berakhir.

---

## 20. STRICT STABLE RELEASE GATE

HyperBoostX V3 hanya boleh diberi status STABLE apabila:

- Seluruh legacy capabilities telah dipetakan.
- Seluruh fitur yang ditawarkan benar-benar berfungsi.
- Seluruh tombol telah divalidasi.
- Tidak ada placeholder production functionality.
- Tidak ada fake success.
- Tidak ada fake benchmark.
- Tidak ada critical security vulnerability yang diketahui.
- Tidak ada critical bug yang belum selesai.
- Seluruh critical tests lulus.
- Safety Guard berfungsi.
- Backup dan rollback telah diuji.
- Installer berhasil dibuat dan diuji.
- Aplikasi berhasil dijalankan setelah instalasi.
- Pengujian runtime Windows aktual telah dilakukan.
- Release artifacts telah tersedia dan diverifikasi.

Apabila ada persyaratan yang belum terpenuhi, lanjutkan perbaikan atau gunakan status RELEASE CANDIDATE dengan blocker yang transparan.

Jangan memberikan label STABLE hanya karena compilation atau unit testing berhasil.

---

## FINAL EXECUTION ORDER

START WORKING ON THE EXISTING REPOSITORY NOW.

Do not merely write a development plan.

Do not stop at documentation.

Do not create fake implementations.

Do not replace working features with placeholders.

Audit everything.

Implement everything that is supported.

Fix every reproducible critical issue.

Run real tests.

Build the application.

Build the installers.

Validate the installed runtime.

Generate the final release artifacts.

Deliver HyperBoostX V3 as a complete, safe, functional, and production-ready Windows optimization application.

**The final product must be usable by real users, not just developers.**