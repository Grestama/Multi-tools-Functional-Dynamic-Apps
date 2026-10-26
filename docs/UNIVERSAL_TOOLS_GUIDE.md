# Universal Tools Engine — Panduan Lengkap Tools & Spesifikasi

Panduan resmi penggunaan, spesifikasi teknis, parameter, dan perlindungan keamanan seluruh utilitas pada **DynamicTopBar Universal Tools Engine**.

---

## 1. Arsitektur & Pintu Masuk Penggunaan

Universal Tools Engine dapat diakses melalui empat pintu masuk (entry points):
1. **DynamicTopBar Expanded Panel (`ToolsContentHost`):** Akses instan dalam antarmuka overlay capsule (376 × 224 DIPs) dengan chip pemilih kategori, input multiline, dan tombol eksekusi cepat.
2. **Dedicated Tools Workspace (`ToolsWindow`):** Jendela mandiri terpisah (960 × 650 DIPs) untuk operasi berskala besar (pratinjau transformasi gambar, tabel batch rename, folder size breakdown, dan live regex testing). Dibuka melalui tombol pop-out (``) di panel overlay atau melalui Command Palette.
3. **Command Palette (`Ctrl+Shift+P`):** Setiap tool terdaftar otomatis ke registry Command Palette dengan pencarian fuzzy berbasis judul, kategori, dan kata kunci kontekstual.
4. **Global Shortcuts:** Pintasan keyboard global yang dapat dikonfigurasi melalui Global Shortcut Engine.

---

## 2. Image Tools

Implementasi platform: Windows Imaging Component (WIC) akselerasi perangkat keras via `Windows.Graphics.Imaging` (`BitmapDecoder`, `BitmapEncoder`, `BitmapTransform`).

### A. Fitur & Kemampuan
| Fitur | Deskripsi | Format Didukung |
|---|---|---|
| **Kompresi Gambar** | Optimasi ukuran file dengan pengaturan kualitas 1–100%. | JPEG, PNG (lossless), WebP |
| **Konversi Format** | Transformasi raster format gambar secara non-destruktif. | JPEG, PNG, WebP |
| **Resize** | Mengubah dimensi lebar/tinggi dengan opsi penjagaan rasio aspek (`MaintainAspectRatio`). | Semua format |
| **Crop** | Pemotongan area koordinat gambar (X, Y, Width, Height) dengan validasi batas kanvas. | Semua format |
| **Rotate & Flip** | Rotasi 90°, 180°, 270° searah jarum jam, serta pembalikan horizontal dan vertikal. | Semua format |
| **Pembersihan Metadata** | Opsi peniadaan metadata EXIF/XMP (`StripMetadata = true`) untuk privasi dan efisiensi ukuran file. | Semua format |
| **Batch Processing** | Pemrosesan kumpulan banyak file gambar secara asinkron dengan progress bar dan laporan ringkasan. | Semua format |
| **Statistik Penghematan** | Pelaporan presisi: Ukuran Awal, Ukuran Akhir, Byte Dihemat, dan Persentase Penghematan (`SavingsPercentage`). | Semua format |

### B. Aturan Keamanan & Integritas Data
- **Non-Destructive by Default:** Berkas asli tidak pernah ditimpa secara diam-diam. Nama berkas hasil kompresi secara otomatis diberi sufiks `_processed` atau diskriminator angka unik (`_1`, `_2`) jika berkas tujuan telah ada.
- **Validasi Berkas:** Validasi keberadaan file, integritas header file, dan pencegahan alokasi memori berlebih pada dimensi gambar ekstrem.
- **Klaim Penghematan Jujur:** Jika ukuran hasil pemrosesan sama atau lebih besar dari aslinya (misalnya kompresi PNG pada gambar yang sudah sangat optimal), `SavingsPercentage` bernilai `0.0%` dan `SavedBytes` bernilai `0`.

---

## 3. File Tools

Implementasi platform: `DynamicTopBar.Core.Services.FileToolsService` (Thread-safe, platform-independent, streaming I/O).

### A. Fitur & Kemampuan
| Utilitas | Deskripsi & Aturan | Output / Format |
|---|---|---|
| **Batch Rename** | Pengubahan nama banyak file secara massal dengan simulasi pratinjau (`SimulateBatchRenameAsync`), deteksi tabrakan (collision detection), dan pencegahan karakter terlarang Windows (`\ / : * ? " < > \|`). | Simulasi & rename fisik |
| **Aturan Rename** | 1. Find/Replace Text (case sensitive/insensitive)<br>2. Prefix & Suffix<br>3. Padded Numbering (`_001`, `_002`)<br>4. Change Case (Lowercase, Uppercase, TitleCase)<br>5. Change Extension | Beragam format file |
| **ZIP & Extract ZIP** | Pembuatan arsip `.zip` dari daftar berkas/folder dan ekstraksi arsip dengan proteksi penimpaan. | Format standar `.zip` |
| **Folder Size Analyzer** | Analisis disk usage direktori secara rekursif, menghitung total bytes, jumlah file, subfolder, serta breakdown 10 ekstensi terbesar.<br>**Proteksi Reparse Point:** Otomatis melewati Windows NTFS Junctions dan Symlinks (`FileAttributes.ReparsePoint`) untuk mencegah circular infinite loop. | Laporan MB/GB & file count |
| **File Search** | Pencarian cepat berkas berbasis pola glob wildcard (`*.txt`, `*report*`, `project*.*`) dengan filter ukuran minimum dan maksimum bytes. | Daftar path & ukuran file |
| **Cryptographic Hash** | Penghitungan checksum berkas secara streaming tanpa memuat seluruh file ke RAM. Mendukung algoritma MD5, SHA-1, SHA-256, dan SHA-512 dengan pembatalan via `CancellationToken`. | Checksum heksadesimal |
| **File Format Converter** | Konversi format file data terstruktur dan teks:<br>1. CSV ↔ JSON (array of objects)<br>2. CSV ↔ TSV (tab-delimited)<br>3. Text Encoding (UTF-8, UTF-16, ASCII, ISO-8859-1 dengan/tanpa BOM)<br>4. Line Endings (CRLF Windows ↔ LF Unix) | Non-destruktif |
| **Folder Favorites** | Manajemen pintasan folder favorit pengguna dengan penyimpanan atomic JSON lokal (`favorite_folders.json`). | Daftar folder favorit |
| **Recent Files** | Pencatatan riwayat berkas terakhir yang dibuka (maksimal 20 item, rolling FIFO) dengan penyimpanan atomic JSON lokal (`recent_files.json`). | Riwayat file |

### B. Aturan Keamanan Berkas
- Tidak ada file asli yang dihapus selama atau setelah operasi konversi.
- Penanganan `UnauthorizedAccessException` dan `IOException` (file terkunci oleh proses lain) menghasilkan laporan kesalahan yang informatif tanpa mematikan aplikasi.

---

## 4. Advanced Clipboard Tools

Implementasi platform: `DynamicTopBar.Core.Services.ClipboardService` dengan gerbang privasi ketat.

### A. Fitur & Kemampuan
- **Deteksi Jenis Konten Otomatis:** Setiap snippet dianalisis dan diklasifikasikan secara real-time ke dalam:
  - `Text`: Teks umum
  - `URL`: Tautan web (`http://`, `https://`)
  - `Code`: Potongan kode program (C#, JS, Python, HTML/XML)
  - `JSON`: Struktur JSON valid
  - `Email`: Alamat email RFC 5322
  - `Number`: Angka atau nilai numerik murni
- **Penyaringan Data Sensitif:** Pola regex otomatis mendeteksi dan mencegah penangkapan data kredensial:
  - JSON Web Tokens (JWT)
  - Nomor kartu kredit (Visa, MasterCard, Amex, Discover)
  - Kunci privat SSH / RSA (`BEGIN PRIVATE KEY`)
  - HTTP Bearer authorization tokens
- **Pencarian & Kategori:** Pencarian teks bebas dan filter instan berdasarkan jenis konten.
- **Pinning:** Menyematkan snippet penting agar tidak terhapus saat pembersihan massal.
- **Auto-Pruning:** Pembersihan otomatis snippet lama berdasarkan batas hari (`AutoPruneDays`) dan batas kapasitas maksimal (`MaxSnippetCapacity`).
- **Blacklist Aplikasi:** Pengecualian aplikasi sumber (misalnya password manager seperti KeePass, 1Password, Bitwarden).
- **Ekspor & Impor:** Pencadangan riwayat clipboard lokal ke file JSON terenkripsi/terproteksi.

### B. Postur Privasi
- **100% Opt-In Default:** Clipboard history dinonaktifkan secara bawaan (`IsOptInEnabled = false`). Tidak ada satu pun teks yang disalin atau disimpan sebelum pengguna menyetujuinya secara sadar.
- **Zero Cloud / Telemetry:** Seluruh data clipboard hanya tersimpan di memori lokal dan file JSON terproteksi pada mesin lokal.

---

## 5. Developer Utilities Suite

Implementasi platform: `DynamicTopBar.Core.Services.DeveloperUtilitiesService`.

1. **JSON Formatter & Validator:**
   - Memvalidasi sintaks JSON.
   - Pilihan Pretty-print (2 spasi atau 4 spasi) dan Minify.
   - Melaporkan baris, kolom, dan posisi byte jika terdapat kesalahan parsing.
2. **Base64 Encoder / Decoder:**
   - Standar RFC 4648 dan varian URL-safe (`-` dan `_` menggantikan `+` dan `/`, tanpa padding `=`).
3. **URL Encoder / Decoder:**
   - Mengonversi karakter khusus ke percent-encoding RFC 3986 dan sebaliknya.
4. **UUID / GUID Generator:**
   - Mendukung format "D" (32 digit dengan strip), "N" (32 digit tanpa strip), "B" (diapit kurung kurawal), dan "P" (diapit tanda kurung biasa).
   - Opsi huruf kapital (uppercase) dan pembuatan batch (5x sekaligus).
5. **Streaming Text Hash:**
   - Penghitungan hash teks instan untuk SHA-256, SHA-512, MD5, dan SHA-1.
6. **Text Formatter & Case Converter:**
   - Transformasi Uppercase, Lowercase, Title Case, CamelCase, SnakeCase, KebabCase.
   - Sort lines ascending/descending dan deduplikasi baris berulang.
   - Statistik teks real-time: Word Count, Character Count, Line Count.
7. **Regex Tester dengan ReDoS Guard:**
   - Evaluasi ekspresi reguler dengan batasan waktu eksekusi ketat (**`RegexEvaluationTimeout = 2.0s`**) untuk mencegah serangan Catastrophic Backtracking membekukan proses.
   - Ekstraksi match count, indeks match, dan capture groups.
8. **Timestamp & Date Converter:**
   - Konversi Unix Epoch detik dan milidetik ke ISO 8601 UTC serta format lokal Windows.
   - Parsing string tanggal ISO/RFC ke Unix Epoch.
9. **Color Spaces Converter & Picker:**
   - Konversi dua arah antara HEX (`#RRGGBB` / `#AARRGGBB`), RGB (`rgb(r, g, b)`), dan HSL (`hsl(h, s%, l%)`).
   - Ekstraksi komponen individual (Alpha, Hue, Saturation, Lightness).
10. **QR Code Matrix Generator:**
    - Pembuatan matriks QR Code murni dalam C# tanpa dependensi library eksternal.
    - Menghasilkan representasi visual vektor SVG yang siap di-copy dan ASCII art preview.
11. **Universal Unit Converter:**
    - Kategori Storage: Bytes, Kilobytes, Megabytes, Gigabytes, Terabytes.
    - Kategori Length: Millimeters, Centimeters, Meters, Kilometers, Inches, Feet, Yards, Miles.
    - Kategori Mass: Milligrams, Grams, Kilograms, Metric Tons, Ounces, Pounds.
    - Kategori Temperature: Celsius, Fahrenheit, Kelvin.

---

## 6. Ringkasan Integrasi Command Palette

Seluruh alat dapat dipanggil melalui Command Palette (`Ctrl+Shift+P`):
- `Open Universal Tools Workspace` (`cmd-open-tools-workspace`)
- `Image Tools: Open Workspace` (`cmd-open-image-tools-workspace`)
- `File Tools: Open Workspace` (`cmd-open-file-tools-workspace`)
- `Developer Utilities: Open Workspace` (`cmd-open-dev-tools-workspace`)
- `Clipboard History: Open Workspace` (`cmd-open-clipboard-workspace`)
- Seluruh tool individu (`tool-json-format`, `tool-base64`, `tool-url-codec`, `tool-uuid`, `tool-text-format`, `tool-regex`, `tool-hash`, `tool-timestamp`, `tool-color`, `tool-qr`, `tool-unit-converter`, `tool-file-rename`, `tool-file-zip`, `tool-file-folder-size`, `tool-file-search`, `tool-file-hash`, `tool-file-convert`, `tool-image-compress`, `tool-image-resize`, `tool-image-convert`).
