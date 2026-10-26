# Daftar Dependensi dan Lisensi Proyek (Dependencies & Licenses)

Dokumen ini memetakan seluruh dependensi eksternal, komponen sistem operasi Windows, paket NuGet, dan lisensi perangkat lunak yang digunakan dalam pengembangan **DynamicTopBar**.

---

## 1. Ringkasan Kepatuhan & Lisensi

DynamicTopBar dibangun dengan prinsip **zero-bloat**, mengutamakan komponen bawaan .NET 9 dan sistem operasi Windows (Windows Imaging Component, WinRT, Win32 APIs) untuk menjaga ukuran distribusi tetap ringkas, performa tinggi, dan bebas dari dependensi komersial pihak ketiga yang membatasi hak cipta.

Seluruh library yang digunakan berlisensi bebas (permissive open-source) yang mengizinkan redistribusi komersial maupun non-komersial tanpa kewajiban copyleft.

---

## 2. Dependensi Paket NuGet

### A. Proyek `DynamicTopBar.App`

| Paket NuGet | Versi | Pemilik / Vendor | Lisensi | Kegunaan dalam Aplikasi |
|---|---|---|---|---|
| **Microsoft.WindowsAppSDK** | 1.6+ | Microsoft | [MIT](https://github.com/microsoft/WindowsAppSDK/blob/main/LICENSE) | Komponen UI WinUI 3, XAML controls, lifecycle, dan interop desktop Windows modern. |
| **Microsoft.Windows.SDK.BuildTools** | * (Latest) | Microsoft | [Microsoft Software License Terms](https://www.nuget.org/packages/Microsoft.Windows.SDK.BuildTools) | Tooling build dan kompilasi metadata Windows SDK. |
| **Microsoft.Windows.SDK.BuildTools.WinApp** | * (Latest) | Microsoft | [Microsoft Software License Terms](https://www.nuget.org/packages/Microsoft.Windows.SDK.BuildTools.WinApp) | Generator kode interop WinApp SDK. |
| **System.Security.Cryptography.ProtectedData** | 10.0.13 | Microsoft (.NET Foundation) | [MIT](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT) | Perlindungan kredensial lokal via Windows Data Protection API (DPAPI). |

### B. Proyek `DynamicTopBar.Core`

| Komponen | Versi | Pemilik | Lisensi | Catatan Arsitektur |
|---|---|---|---|---|
| **.NET 9 BCL (Base Class Library)** | 9.0.x | Microsoft (.NET Foundation) | [MIT](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT) | **Nol dependensi NuGet eksternal.** Seluruh logika inti (JSON, IO, Hashing, Regex, Encoding, ZIP) menggunakan library bawaan .NET 9 murni. |

### C. Proyek `DynamicTopBar.Tests`

| Paket NuGet | Versi | Lisensi | Kegunaan |
|---|---|---|---|
| **xunit** | 2.8.2 | [Apache-2.0](https://github.com/xunit/xunit/blob/main/LICENSE) | Framework pengujian unit otomatis. |
| **xunit.runner.visualstudio** | 2.8.2 | [Apache-2.0](https://github.com/xunit/visualstudio.xunit/blob/main/LICENSE) | Test runner adapter Visual Studio / `dotnet test`. |
| **Microsoft.NET.Test.Sdk** | 17.13.0 | [Microsoft Software License Terms](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk) | Target MSBuild untuk eksekusi test runner. |

---

## 3. Komponen Sistem Operasi Windows (Native & Platform APIs)

Aplikasi memanfaatkan subsistem Windows native tanpa menginstal runtime pihak ketiga:

1. **Windows Imaging Component (WIC) via `Windows.Graphics.Imaging`:**
   - **Komponen:** `BitmapDecoder`, `BitmapEncoder`, `BitmapTransform`, `BitmapPropertySet`.
   - **Lisensi:** Bagian bawaan dari sistem operasi Windows 10 dan Windows 11.
   - **Kegunaan:** Akselerasi perangkat keras untuk kompresi JPEG, PNG, dan WebP, manipulasi dimensi (resize), pemotongan (crop), rotasi, dan pembalikan.
   - **Keuntungan:** Tidak memerlukan library pihak ketiga (seperti ImageSharp atau SkiaSharp) sehingga menghemat ukuran binary >25 MB dan bebas dari isu lisensi komersial SixLabors.

2. **Windows Global System Media Transport Controls (GSMTC):**
   - **Komponen:** `Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager`.
   - **Lisensi:** Windows OS component.
   - **Kegunaan:** Kontrol media global universal (Spotify, YouTube, VLC, browser) tanpa integrasi spesifik per aplikasi.

3. **Win32 Platform Interop:**
   - **Komponen:** `user32.dll` (`RegisterHotKey`, `UnregisterHotKey`, `SetWindowSubclass`, `PostMessage`).
   - **Lisensi:** Windows OS Core.
   - **Kegunaan:** Global shortcut engine dan komunikasi antar-instance (single-instance mutex enforcement).

---

## 4. Evaluasi Kompatibilitas Target .NET 9 & Persyaratan Distribusi

- **Kompatibilitas Runtime:** Seluruh proyek dikompilasi pada Target Framework Moniker `.NET 9` (`net9.0-windows10.0.26100.0` untuk App, `net9.0` untuk Core dan Tests).
- **Penggunaan Memori:** Operasi pengolahan berkas dan gambar dijalankan secara streaming (`FileStream`, buffer 64KB) guna membatasi lonjakan memori pada berkas berukuran besar.
- **Redistribusi Standalone / Framework-Dependent:** Proyek mendukung distribusi MSIX maupun unpackaged (portable ZIP) dengan runtime `.NET 9 Desktop Runtime` atau `PublishTrimmed/PublishSingleFile`.
