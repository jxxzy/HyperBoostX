# HyperBoostX User Guide Untuk Pemula

Panduan ini dibuat untuk pengguna awam yang baru pertama kali install HyperBoostX. Ikuti urutan dari atas ke bawah supaya pemakaian tetap aman, mudah dipahami, dan bisa di-undo kalau ada perubahan yang tidak cocok di PC kamu.

Target versi: `HyperBoostX v1.3.0 Stable`

## Ringkasan Singkat

HyperBoostX adalah aplikasi optimasi Windows untuk membantu membaca kondisi PC, memberi rekomendasi aman, membersihkan beban latar belakang, menyiapkan mode gaming/streaming/creator, dan membuat laporan sebelum/sesudah boost.

HyperBoostX bukan alat overclock, bukan alat hack driver, dan tidak menjamin FPS naik di semua PC. Fokus utamanya adalah membuat Windows lebih rapi, mengurangi beban background, memberi saran aman, menjaga Safety Guard aktif, dan menyediakan restore/undo untuk perubahan yang didukung.

Alur paling aman untuk pengguna awam:

```text
Install -> Buka HyperBoostX -> Dashboard -> Restore & Backup -> Smart Recommendation -> One Click Boost -> Review Plan -> Approve -> Lihat Report -> Undo kalau perlu
```

## Sebelum Install

Pastikan kamu download file resmi:

- `HyperBoostXInstaller.exe`
- Opsional: `SHA256SUMS.txt` untuk cek checksum

Jika Windows menampilkan `Unknown Publisher` atau SmartScreen, itu bisa terjadi karena installer belum ditandatangani code signing certificate. Kalau file diambil dari GitHub Release resmi HyperBoostX, lanjutkan hanya jika kamu yakin sumbernya benar.

Jangan download file dari link acak, mirror tidak resmi, atau arsip yang isinya banyak file internal seperti backend mentah, debug package, cache, atau log.

## Cara Install

1. Jalankan `HyperBoostXInstaller.exe`.
2. Kalau muncul User Account Control, pilih `Yes` jika kamu memang ingin memasang aplikasi.
3. Ikuti installer sampai selesai.
4. Buka HyperBoostX dari Desktop atau Start Menu.
5. Tunggu sampai status backend/system link tersambung.

Default install biasanya berada di:

```text
C:\Program Files\HyperBoostX
```

Data user lama tetap disimpan di:

```text
%LocalAppData%\HyperBoost X
```

Folder data user itu dipertahankan supaya konfigurasi, log, backup, dan restore metadata dari versi sebelumnya tidak hilang.

## Yang Harus Dilakukan Saat Pertama Kali Buka

Jangan langsung masuk ke fitur advanced. Lakukan ini dulu:

1. Buka `Dashboard`.
2. Pastikan backend status terlihat sehat/connected.
3. Baca kondisi CPU, RAM, disk, network, dan health score.
4. Buka `Restore & Backup`.
5. Pastikan fitur restore/undo bisa diakses.
6. Buka `GPU Center` dan klik refresh.
7. Buka `Smart Recommendation` untuk melihat saran awal.
8. Baru gunakan `One Click Boost` mode aman/balanced.

Kalau status backend belum connected, jangan jalankan boost dulu. Tutup aplikasi, buka ulang dari shortcut HyperBoostX, lalu cek lagi.

## Aturan Aman Untuk Pengguna Awam

Ikuti aturan ini supaya tidak salah pakai:

- Pakai mode `Safe` atau `Balanced` terlebih dahulu.
- Baca rencana aksi sebelum klik approve.
- Jangan matikan Windows Defender.
- Jangan matikan Windows Update permanen.
- Jangan disable service GPU, audio, network, antivirus, atau driver.
- Jangan pakai overclock, undervolt, voltage tweak, BIOS/UEFI tweak, atau driver hack.
- Jangan hapus file pribadi seperti Documents, Downloads, Pictures, Desktop, atau folder game secara manual dari tool cleanup.
- Jangan gunakan `Advanced Tweaks` kalau belum paham risikonya.
- Kalau ragu, jangan apply. Gunakan `Smart Recommendation` atau baca report dulu.

## Menu Yang Aman Untuk Pemula

Menu yang direkomendasikan untuk pengguna awam:

- `Dashboard`
- `Smart Recommendation`
- `One Click Boost`
- `GPU Center`
- `Cleanup`
- `Startup Manager`
- `Background Apps`
- `Network Booster`
- `Restore & Backup`
- `Settings`
- `About`

Menu yang boleh dipakai, tapi harus dibaca pelan-pelan:

- `Gaming Mode`
- `Streaming Mode`
- `Creator Mode`
- `Storage`
- `DNS & Latency Tools`
- `Security & Health`
- `Repair Tools`
- `Driver & Update Center`
- `Apps Manager`

Menu untuk pengguna yang lebih paham Windows:

- `Tweaks Center`
- `Advanced Tweaks`
- `Windows Services`
- `Power Optimization`
- `Visual Effects`
- `Windows Features`
- `Update Control`
- `App Uninstaller`
- `Restore Point Manager`
- `Scheduled Automation`
- `Utilities Tools`

## Penjelasan Semua Fitur

| Menu | Fungsi | Cara pakai untuk awam | Catatan aman |
|---|---|---|---|
| `Dashboard` | Melihat kondisi utama PC | Buka pertama kali, lihat CPU/RAM/disk/network/skor | Kalau ada warning, baca detailnya dulu |
| `One Click Boost` | Boost cepat dengan plan | Pilih safe/balanced, baca plan, approve | Jangan approve action yang tidak dipahami |
| `Smart Recommendation` | Rekomendasi otomatis | Jalankan scan, baca alasan, ikuti saran aman | Cocok untuk pemula |
| `GPU Center` | Deteksi GPU dan overlay | Klik refresh, lihat GPU/vendor/overlay | Jangan disable driver GPU |
| `Gaming Mode` | Persiapan main game | Aktifkan sebelum game, lihat rekomendasi | Jangan pause app yang sedang dipakai |
| `Streaming Mode` | Persiapan streaming | Pakai untuk OBS/Discord/TikTok LIVE | Jangan pause OBS/Discord saat live |
| `Creator Mode` | Persiapan editing/render | Pakai saat Premiere/DaVinci/Blender | Pastikan file kerja sudah disimpan |
| `Performance Boost` | Boost performa umum | Pakai mode ringan dulu | Hindari mode ekstrem untuk pemula |
| `Startup Manager` | Mengatur aplikasi saat Windows menyala | Disable app yang jelas tidak penting | Jangan disable driver/security/audio |
| `Background Apps` | Melihat app yang berat | Tutup app yang tidak dipakai | Jangan force-kill app penting |
| `Cleanup` | Membersihkan file sementara | Jalankan safe cleanup | Jangan hapus file pribadi |
| `Storage` | Melihat kondisi penyimpanan | Cek disk usage dan ruang kosong | Hindari delete manual sembarangan |
| `Network Booster` | Optimasi network ringan | Test DNS, flush DNS, cek hasil | Tidak menjamin ping selalu turun |
| `DNS & Latency Tools` | Cek DNS, latency, packet loss | Pakai kalau game lag atau browsing lambat | Simpan DNS lama jika ganti manual |
| `Privacy Center` | Pengaturan privasi Windows | Pakai rekomendasi aman | Jangan mematikan komponen Windows yang tidak paham |
| `Security & Health` | Cek kesehatan keamanan dan sistem | Baca status, jangan panik kalau ada warning | HyperBoostX tidak force-disable Defender |
| `Apps Manager` | Melihat aplikasi terinstall | Uninstall app yang jelas tidak dipakai | Jangan uninstall driver/vendor utility sembarangan |
| `App Uninstaller` | Uninstall aplikasi | Pakai uninstall normal dulu | Force remove hanya jika yakin |
| `Tweaks Center` | Tweak Windows | Baca risiko dan undo | Tidak untuk klik asal |
| `Advanced Tweaks` | Tweak lanjutan | Untuk power user | Pemula sebaiknya hindari |
| `Windows Services` | Melihat layanan Windows | Review saja jika awam | Jangan disable service penting |
| `Power Optimization` | Mengatur performa/daya | Pilih balanced/performance aman | Laptop bisa lebih boros baterai |
| `Visual Effects` | Mengurangi efek visual Windows | Pakai untuk PC low-end | Bisa mengubah tampilan Windows |
| `Windows Features` | Fitur optional Windows | Baca fungsi sebelum ubah | Jangan disable fitur yang dipakai app/game |
| `Update Control` | Mengatur update | Gunakan opsi aman | Jangan disable update permanen |
| `Repair Tools` | SFC/DISM/network repair | Pakai saat Windows error | Biasanya perlu admin dan bisa lama |
| `Driver & Update Center` | Cek driver | Gunakan vendor resmi | Jangan install driver dari sumber tidak jelas |
| `Restore & Backup` | Undo/restore/backup | Buka sebelum dan sesudah boost | Ini menu penyelamat utama |
| `Restore Point Manager` | Restore point Windows | Buat restore point sebelum perubahan besar | Memerlukan fitur restore Windows aktif |
| `Scheduled Automation` | Otomasi berkala | Pakai default/safe saja | Jangan auto-run aksi risky |
| `Utilities Tools` | Kumpulan tool cepat | Gunakan tool yang jelas | Jangan jalankan tool repair tanpa alasan |
| `Settings` | Tema, AI, safety, config | Pastikan Safety Guard aktif | Jangan matikan Require Approval |
| `About` | Info aplikasi | Cek versi dan status | Versi stable target: `1.3.0` |

## Cara Pakai Dashboard

Gunakan Dashboard untuk membaca kondisi PC secara cepat.

Yang perlu diperhatikan:

- `CPU Usage`: kalau tinggi saat idle, mungkin ada app berat.
- `RAM Usage`: kalau tinggi, tutup app yang tidak dipakai.
- `Disk Usage`: kalau disk penuh, pakai Cleanup/Storage.
- `Network`: kalau lambat, pakai Network Booster atau DNS & Latency Tools.
- `PC Health Score`: gambaran umum kesehatan PC.
- `Gaming Readiness`: kesiapan untuk game.
- `Streaming Readiness`: kesiapan untuk live/recording.
- `Startup Cleanliness`: seberapa bersih aplikasi startup.

Jika ada status `Partial`, artinya sebagian pengecekan berhasil dan sebagian tidak tersedia. Baca detailnya sebelum menjalankan ulang.

## Cara Pakai One Click Boost Dengan Aman

1. Tutup aplikasi yang tidak perlu.
2. Simpan pekerjaan penting dulu.
3. Buka `Restore & Backup` untuk memastikan restore/undo bisa diakses.
4. Buka `One Click Boost`.
5. Pilih mode `Safe` atau `Balanced`.
6. Klik analyze/scan jika tersedia.
7. Baca daftar tindakan yang akan dilakukan.
8. Pastikan Safety Guard aktif.
9. Klik approve hanya untuk action yang aman.
10. Tunggu proses selesai.
11. Baca before/after report.

Contoh hasil yang mungkin terlihat:

```text
CPU Idle: 7% -> 3%
RAM Usage: 48% -> 39%
Startup Apps: 18 -> 12
Safety Guard: Active
Undo: Available
```

Angka ini adalah hasil pengukuran/snapshot saat itu, bukan janji FPS naik.

## Cara Pakai GPU Center

GPU Center membantu membaca GPU dan aplikasi overlay/vendor yang mungkin memengaruhi gaming.

Langkah:

1. Buka `GPU Center`.
2. Klik `Refresh GPU Center`.
3. Lihat GPU vendor:
   - NVIDIA GeForce GTX/RTX
   - AMD Radeon/RX/Vega
   - Intel Arc
   - Intel Iris Xe/UHD/iGPU
   - Microsoft Basic Display Adapter
   - Unknown GPU fallback
4. Lihat VRAM, GPU usage, temperature, driver version jika tersedia.
5. Lihat daftar overlay/vendor apps.
6. Klik export report jika ingin menyimpan laporan GPU.

Klasifikasi app yang mungkin muncul:

- `Safe to keep`: aman dibiarkan.
- `Can pause while gaming`: boleh dipause kalau tidak sedang dipakai.
- `Heavy background service`: bisa berat, tapi tetap butuh keputusan user.
- `Needs user decision`: jangan dimatikan otomatis.
- `Do not disable`: jangan dimatikan.
- `Unknown`: analisis manual.

Contoh app yang bisa terdeteksi:

- NVIDIA App
- GeForce Experience
- NVIDIA Overlay
- ShadowPlay
- AMD Software Adrenalin
- Radeon Overlay
- Intel Arc Control
- Discord Overlay
- Steam Overlay
- Xbox Game Bar
- OBS
- TikTok LIVE Studio
- MSI Afterburner
- RivaTuner Statistics Server
- SignalRGB
- Armoury Crate
- iCUE
- Razer Synapse
- Logitech G Hub

Jangan mematikan service driver GPU. Jika ingin pause overlay, pastikan kamu tidak sedang recording, streaming, atau butuh overlay itu.

## Cara Pakai Smart Recommendation

Smart Recommendation cocok untuk pemula karena HyperBoostX membaca kondisi PC lalu memberi saran.

Langkah:

1. Buka `Smart Recommendation`.
2. Jalankan scan.
3. Baca masalah yang ditemukan.
4. Baca alasan rekomendasi.
5. Jalankan hanya action yang aman.
6. Kalau diminta approval, baca dulu risikonya.

Contoh rekomendasi aman:

- Bersihkan cache sementara.
- Refresh DNS.
- Review startup app berat.
- Pause overlay saat gaming jika tidak recording.
- Gunakan power profile yang sesuai.

Contoh rekomendasi yang harus ditolak jika muncul dari sumber apa pun:

- Disable Defender.
- Disable Windows Update permanen.
- Disable GPU driver service.
- Overclock/undervolt/ubah voltage.
- Edit BIOS/UEFI.
- Hapus file sistem.

## Cara Pakai Cleanup

Cleanup dipakai untuk membersihkan file sementara.

Untuk pemula:

1. Buka `Cleanup`.
2. Pilih safe cleanup.
3. Jalankan scan jika tersedia.
4. Baca apa yang akan dibersihkan.
5. Jalankan cleanup.
6. Cek hasil di report.

Aman dibersihkan biasanya:

- Temporary files.
- Cache aplikasi tertentu.
- Log lama yang tidak penting.
- Recycle Bin jika kamu yakin isinya tidak dibutuhkan.

Jangan bersihkan:

- Documents.
- Downloads yang belum dicek.
- Folder game.
- Folder project kerja.
- Foto/video pribadi.
- File yang kamu tidak tahu fungsinya.

## Cara Pakai Startup Manager

Startup Manager membantu mempercepat Windows saat boot.

Langkah:

1. Buka `Startup Manager`.
2. Lihat daftar aplikasi yang berjalan saat startup.
3. Fokus pada app dengan impact tinggi.
4. Disable app yang jelas tidak perlu saat Windows menyala.
5. Restart PC jika ingin melihat hasil.

Biasanya aman dinonaktifkan dari startup:

- Launcher game yang jarang dipakai.
- Updater aplikasi non-penting.
- RGB app yang tidak selalu dibutuhkan.
- App chat yang tidak ingin otomatis hidup.

Jangan disable:

- Antivirus/security.
- Audio driver.
- Touchpad/keyboard utility laptop.
- GPU driver/control service.
- Network driver/helper.
- App kerja yang wajib auto-start.

## Cara Pakai Background Apps

Background Apps membantu melihat aplikasi yang memakai CPU/RAM/GPU.

Langkah:

1. Buka `Background Apps`.
2. Lihat app yang memakai resource tinggi.
3. Tutup aplikasi biasa dari tombol close aplikasinya.
4. Jangan force kill app sistem.

Kalau tidak yakin, jangan hentikan proses. Cari nama aplikasinya dulu atau gunakan Smart Recommendation.

## Cara Pakai Gaming Mode

Gaming Mode dipakai sebelum main game.

Langkah aman:

1. Buka `Dashboard`.
2. Buka `GPU Center` dan refresh.
3. Buka `Gaming Mode`.
4. Pilih profile yang sesuai, misalnya general gaming/esports jika tersedia.
5. Jalankan safe/balanced preparation.
6. Buka game.
7. Setelah selesai, cek report.
8. Undo jika ada efek yang tidak cocok.

Jika kamu memakai Discord, OBS, Steam Overlay, Xbox Game Bar, atau recording tool, jangan pause tool itu kalau sedang dibutuhkan.

## Cara Pakai Streaming Mode

Streaming Mode dipakai untuk live atau recording.

Langkah:

1. Buka OBS/Discord/TikTok LIVE Studio sesuai kebutuhan.
2. Buka HyperBoostX.
3. Masuk `Streaming Mode`.
4. Jalankan scan.
5. Baca saran CPU/RAM/GPU/network.
6. Jangan pause OBS, Discord, atau encoder yang sedang dipakai.
7. Jalankan network/DNS test jika stream lag.

Streaming lebih sensitif daripada gaming biasa. Jangan mengejar RAM kosong dengan mematikan app yang justru dipakai streaming.

## Cara Pakai Creator Mode

Creator Mode cocok untuk editing dan rendering.

Contoh aplikasi:

- Adobe Premiere
- DaVinci Resolve
- Photoshop
- Blender
- General editing apps

Langkah:

1. Simpan project terlebih dahulu.
2. Buka `Creator Mode`.
3. Pilih profile creator/general editing jika tersedia.
4. Jalankan rekomendasi aman.
5. Jangan bersihkan cache project yang sedang dipakai kecuali yakin.
6. Setelah rendering/editing selesai, cek report.

## Cara Pakai Network Booster Dan DNS Tools

Gunakan fitur ini kalau:

- Game terasa lag.
- Ping tinggi.
- Browsing lambat.
- DNS terasa bermasalah.
- Koneksi sempat putus-putus.

Langkah aman:

1. Buka `Network Booster`.
2. Jalankan diagnostics/test.
3. Jalankan flush DNS jika disarankan.
4. Jalankan DNS latency test.
5. Jangan ganti DNS permanen kalau tidak paham.

Catatan: HyperBoostX bisa membantu refresh DNS dan memberi saran network, tetapi tidak bisa menjamin ping selalu turun karena ping juga dipengaruhi ISP, server game, Wi-Fi, router, dan kondisi jaringan.

## Cara Pakai Repair Tools

Repair Tools dipakai kalau Windows terasa rusak, error, atau tidak stabil.

Contoh kegunaan:

- SFC scan.
- DISM repair.
- Reset network ringan.
- Cleanup repair cache.

Untuk pemula:

1. Jangan jalankan semua repair sekaligus.
2. Mulai dari scan.
3. Baca output.
4. Jalankan repair jika benar-benar perlu.
5. Siapkan waktu karena SFC/DISM bisa lama.
6. Jalankan sebagai Administrator jika diminta.

Kalau PC sedang dipakai kerja penting, jangan jalankan repair berat dulu.

## Cara Pakai Driver & Update Center

Gunakan untuk melihat kondisi driver dan update.

Aturan aman:

- Update driver dari sumber resmi NVIDIA, AMD, Intel, Microsoft, atau vendor laptop/motherboard.
- Jangan install driver dari website acak.
- Jangan uninstall driver GPU kalau tidak paham.
- Jangan matikan service driver GPU.

Jika GPU tidak terdeteksi dengan benar, update driver resmi bisa membantu. Namun HyperBoostX tetap menyediakan unknown fallback supaya aplikasi tidak crash.

## Cara Pakai Restore & Backup

Ini menu yang harus diketahui semua pengguna.

Gunakan sebelum:

- Boost besar.
- Tweak Windows.
- Repair tools.
- Startup cleanup besar.
- Update/driver action.

Gunakan setelah:

- PC terasa aneh setelah boost.
- App tertentu tidak berjalan normal.
- Network/audio/game terasa berubah.

Langkah dasar:

1. Buka `Restore & Backup`.
2. Lihat restore metadata atau backup yang tersedia.
3. Pilih undo/restore yang sesuai.
4. Ikuti instruksi.
5. Restart PC jika diminta.

Jika tidak ada undo tersedia, jangan lanjut apply tweak baru. Catat masalah dan laporkan bug.

## Cara Pakai Settings

Settings berisi pengaturan aplikasi.

Untuk pemula, pastikan ini aktif:

- Safety Guard: aktif.
- Require Approval: aktif.
- Auto Fallback AI: aktif jika memakai AI.
- Beginner/Safe mode jika tersedia.
- Animation intensity: low/balanced jika PC lemah.

Jangan matikan Safety Guard hanya demi hasil cepat. Safety Guard adalah lapisan yang mencegah tindakan berisiko.

## Cara Pakai NVIDIA Copilot / AI Doctor

AI bersifat opsional. HyperBoostX tetap bisa dipakai tanpa API key AI.

Jika ingin memakai AI:

1. Buka `Settings / AI`.
2. Confirm provider NVIDIA jika tersedia.
3. Paste NVIDIA API key ke input yang masked.
4. Klik `Save NVIDIA API Key`.
5. Pilih default model dan fallback model.
6. Pastikan `Auto Fallback`, `Safety Guard`, dan `Require Approval` aktif.
7. Klik `Test NVIDIA Connection`.
8. Gunakan prompt sederhana.

Default model:

- `nvidia/nemotron-3-nano-30b-a3b`

Fallback model:

- `nvidia/nvidia-nemotron-nano-9b-v2`

Contoh prompt aman:

```text
Prepare my PC for gaming safely.
```

```text
Find why my PC feels slow and suggest safe fixes only.
```

```text
Check my PC for streaming readiness without disabling important services.
```

AI harus membuat plan terlebih dahulu. AI tidak boleh menjalankan action sistem sebelum user approve.

Jangan minta AI untuk:

- Disable Defender.
- Disable Windows Update permanen.
- Overclock/undervolt.
- Ubah voltage.
- Edit BIOS/UEFI.
- Disable driver GPU.
- Hapus file sistem.

## Workflow Harian Yang Disarankan

### Sebelum Main Game

1. Buka HyperBoostX.
2. Dashboard.
3. GPU Center refresh.
4. Smart Recommendation.
5. One Click Boost safe/balanced.
6. Gaming Mode.
7. Jalankan game.

### Setelah Main Game

1. Buka Dashboard.
2. Lihat report.
3. Jika semua normal, selesai.
4. Jika ada masalah, buka Restore & Backup lalu undo.

### Sebelum Streaming

1. Buka OBS/Discord/tool streaming.
2. Buka HyperBoostX.
3. Streaming Mode.
4. Network test.
5. Jangan pause OBS/Discord/encoder.
6. Mulai streaming.

### Untuk PC Lemot Saat Baru Menyala

1. Dashboard.
2. Startup Manager.
3. Disable app startup yang jelas tidak penting.
4. Cleanup safe.
5. Restart PC.
6. Dashboard lagi untuk cek hasil.

### Untuk Ping Tinggi

1. Dashboard.
2. Network Booster.
3. DNS & Latency Tools.
4. Flush DNS jika disarankan.
5. Test lagi.
6. Jika masih tinggi, cek Wi-Fi/router/ISP/server game.

### Untuk Disk Penuh

1. Storage.
2. Cleanup safe.
3. Review Recycle Bin.
4. Hapus manual hanya file pribadi yang sudah pasti tidak dibutuhkan.
5. Jangan hapus folder Windows, Program Files, AppData, atau folder game sembarangan.

## Memahami Status Dan Report

Status umum:

- `PASS`: berhasil.
- `Partial`: sebagian berhasil, sebagian tidak tersedia atau tidak bisa dijalankan.
- `Warning`: ada hal yang perlu dibaca.
- `Blocked`: Safety Guard menolak action berisiko.
- `Undo Available`: ada jalur undo/restore.
- `Unknown`: data tidak tersedia dari Windows/driver.

Kalau melihat `Blocked`, itu bukan selalu error. Sering kali itu berarti HyperBoostX berhasil melindungi PC dari action yang tidak aman.

## Kalau GPU Terdeteksi Unknown

Ini tidak selalu masalah besar.

Penyebab umum:

- Driver belum lengkap.
- Windows/WMI tidak memberi data GPU.
- Laptop hybrid graphics menyembunyikan detail.
- Microsoft Basic Display Adapter sedang dipakai.
- Counter driver tidak tersedia.

Yang bisa dilakukan:

1. Update driver resmi.
2. Restart PC.
3. Refresh GPU Center.
4. Jika tetap unknown, gunakan safe fallback.

Jangan pakai driver hack untuk memaksa deteksi.

## Kalau Backend Tidak Connected

Backend adalah service lokal HyperBoostX yang berjalan di PC kamu sendiri.

Coba langkah ini:

1. Tutup HyperBoostX.
2. Buka lagi dari shortcut HyperBoostX, bukan file backend manual.
3. Tunggu 10-20 detik.
4. Cek Dashboard.
5. Jika tetap gagal, restart PC.
6. Jika masih gagal, reinstall dengan installer resmi.

Jangan menjalankan banyak instance HyperBoostX sekaligus.

## Kalau Setelah Boost Ada Masalah

Ikuti urutan ini:

1. Jangan apply boost/tweak tambahan.
2. Buka `Restore & Backup`.
3. Cari undo/restore terbaru.
4. Jalankan undo.
5. Restart PC jika diminta.
6. Jika masalah masih ada, export log/crash report.
7. Laporkan bug memakai `BUG_REPORT_TEMPLATE.md`.

## Cara Export Report Atau Crash Report

Gunakan report jika ingin meminta bantuan.

Report bisa berisi:

- Versi HyperBoostX.
- Windows version.
- CPU.
- RAM.
- GPU vendor/model.
- Error message.
- Stack trace jika ada.
- Last action.
- Backend status.
- Timestamp.

HyperBoostX meredact data sensitif seperti API key, AI key, token, GitHub token, username, path sensitif, dan future license key. Walau begitu, tetap baca report sebelum membagikannya.

## Cara Uninstall

1. Buka Windows Settings.
2. Masuk `Apps`.
3. Pilih `Installed apps`.
4. Cari `HyperBoostX`.
5. Klik uninstall.

Uninstall menghapus aplikasi utama. Data user seperti config, backup, log, dan restore metadata di `%LocalAppData%\HyperBoost X` bisa tetap dipertahankan untuk kompatibilitas.

## Tanya Jawab Cepat

### Apakah HyperBoostX aman?

HyperBoostX dirancang dengan Safety Guard, approval, dan restore/undo untuk action yang didukung. Tetap baca plan sebelum approve.

### Apakah HyperBoostX mematikan Defender?

Tidak. HyperBoostX tidak force-disable Windows Defender.

### Apakah FPS pasti naik?

Tidak. HyperBoostX tidak menjamin FPS naik di semua PC. Tujuannya mengurangi beban background, membersihkan startup, memberi rekomendasi aman, dan membuat Windows lebih siap gaming.

### Apakah bisa dipakai di AMD Radeon?

Bisa. v1.3.0 mendukung AMD Radeon/RX/Vega dan AMD integrated graphics detection.

### Apakah bisa dipakai di Intel Arc atau iGPU?

Bisa. v1.3.0 mendukung Intel Arc, Iris Xe, UHD, dan iGPU safe fallback.

### Apakah bisa dipakai di laptop?

Bisa. HyperBoostX mendukung safe laptop/hybrid graphics handling, tapi data hardware bisa berbeda tergantung driver dan vendor laptop.

### Apakah harus punya AI key?

Tidak. AI opsional. Fitur utama tetap bisa dipakai tanpa AI key.

### Kalau muncul Unknown Publisher saat install?

Itu bisa terjadi karena installer belum code-signed. Pastikan file berasal dari GitHub Release resmi.

## Rekomendasi Untuk Pemula

Gunakan urutan ini untuk minggu pertama:

Hari pertama:

1. Dashboard.
2. GPU Center.
3. Smart Recommendation.
4. One Click Boost safe.
5. Baca report.

Hari kedua sampai ketujuh:

1. Dashboard.
2. Startup Manager jika boot lambat.
3. Cleanup safe jika disk penuh.
4. Gaming Mode sebelum main.
5. Restore & Backup jika ada masalah.

Jangan menyentuh Advanced Tweaks atau Windows Services sampai kamu benar-benar paham efeknya.

## Checklist Sebelum Klik Approve

Sebelum approve action apa pun, tanyakan:

- Apakah saya paham action ini?
- Apakah ada undo/restore?
- Apakah Safety Guard aktif?
- Apakah action ini tidak mematikan Defender?
- Apakah action ini tidak mematikan driver GPU/audio/network?
- Apakah action ini tidak menghapus file pribadi?
- Apakah saya sudah menyimpan pekerjaan penting?

Kalau salah satu jawabannya `tidak`, jangan approve dulu.

## Cara Melaporkan Bug

Gunakan format ini:

```text
HyperBoostX Bug Report

Version:
Windows:
CPU:
RAM:
GPU:
Issue:
Steps before error:
Screenshot:
Logs if available:
```

Sertakan juga:

- Menu yang sedang dibuka.
- Action terakhir yang diklik.
- Apakah backend connected.
- Apakah Safety Guard memberi warning.
- Apakah undo tersedia.

## Kesimpulan

Untuk pengguna awam, cukup pakai fitur inti:

```text
Dashboard -> GPU Center -> Smart Recommendation -> One Click Boost -> Report -> Restore & Backup
```

Fitur lain boleh dipakai jika sudah paham tujuannya. HyperBoostX paling aman digunakan sebagai alat scan, plan, approve, boost, report, dan undo, bukan sebagai alat klik semua tweak sekaligus.
