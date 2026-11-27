# AsetKu

AsetKu adalah aplikasi desktop inventaris aset dan barang yang dibuat dengan **Visual Basic .NET**, **Windows Forms**, dan **.NET 8**. Aplikasi menggunakan Supabase untuk autentikasi serta penyimpanan data melalui REST API.

## Fitur

| Menu | Fungsi |
| --- | --- |
| Login | Masuk dengan email dan password Supabase Auth; profil pengguna diambil dari tabel `users`. |
| Dashboard | Ringkasan aset, stok, peminjaman, grafik transaksi, kategori, dan aktivitas terbaru. |
| Data Aset / Barang | Melihat, mencari, memfilter, menambah, mengubah, dan menghapus aset. |
| Barang Masuk | Admin mencatat penerimaan stok. |
| Pengajuan Divisi | User divisi mengajukan Barang Keluar atau Peminjaman sesuai jenis aset. |
| Verifikasi Pengajuan | Admin menyetujui, menyerahkan kepada pemohon, dan memverifikasi pengembalian. |
| Pemeliharaan | Mencatat jadwal dan status pemeliharaan. |
| Master Data | Mengelola kategori, lokasi, dan supplier. |
| Laporan | Melihat ringkasan aset, mengekspor CSV yang dapat dibuka di Excel, dan membuka pratinjau cetak untuk PDF. |
| Manajemen User | Admin mengelola akun serta divisi, dan menonaktifkan akun tanpa menghapus riwayat. |

## Kebutuhan

- Windows.
- Visual Studio 2022 dengan workload **.NET desktop development** dan dukungan .NET 8, atau .NET SDK yang dapat membangun target `net8.0-windows`.
- Koneksi internet untuk login dan operasi data.
- Proyek Supabase yang sudah memiliki skema tabel dan view sesuai kebutuhan aplikasi. Berkas migrasi atau SQL skema belum tersedia di repositori ini.
- Akun Supabase Auth yang memiliki baris profil di `public.users` dengan `is_active = true`.

Proyek tidak mendeklarasikan paket NuGet tambahan di `AsetKu.vbproj`.

## Menjalankan di Visual Studio

Untuk mencoba **Portal User** tanpa perubahan database, masuk melalui form email dan password memakai salah satu akun mock berikut. Semua akun memakai password `demo123`.

| Nama | Email | Divisi |
| --- | --- | --- |
| Andi Pratama | `andi@demo.local` | Divisi IT |
| Siti Rahma | `siti@demo.local` | Divisi Keuangan |
| Budi Santoso | `budi@demo.local` | Divisi Administrasi |

Data mock disimpan hanya di memori aplikasi dan hilang saat aplikasi ditutup; tidak ada data mock yang dikirim ke Supabase.

1. Buka **File > Open > Project/Solution**, lalu pilih `AsetKu.sln`.
2. Tunggu proses restore selesai. Jika diminta, pasang komponen .NET desktop yang sesuai.
3. Pastikan **AsetKu** dipilih sebagai *Startup Project*.
4. Tekan **F5** untuk menjalankan dengan debugger, atau **Ctrl+F5** tanpa debugger.
5. Masuk menggunakan email dan password akun Supabase yang valid.

Portal Admin memerlukan akun Supabase Auth aktif; `admin/admin` bukan kredensial bawaan. Portal User saat ini memakai tiga akun mock lokal sampai kontrak database tersedia.

Pada versi antarmuka awal, login demo menerima sembarang username dan password yang tidak kosong, sehingga `admin/admin` dahulu dapat membuka dashboard. Sejak integrasi Supabase, kredensial tersebut hanya bisa dipakai jika akun yang sesuai benar-benar dibuat di Supabase Auth; form saat ini meminta email akun tersebut. Membuka dashboard dengan login demo lama juga tidak memberikan sesi Supabase untuk memuat data.

Form `FormLogin.vb` dan `FormMain.vb` memiliki file `.Designer.vb` serta `.resx`. Di Solution Explorer, klik kanan form lalu pilih **View Designer** atau tekan **Shift+F7**. Halaman menu dalam file `uc*.vb` dibuat melalui kode dan dilihat saat aplikasi berjalan.

### Melalui terminal

Jalankan dari folder proyek:

```powershell
dotnet restore AsetKu.sln
dotnet build AsetKu.sln
dotnet run --project AsetKu.vbproj
```

Hasil build Debug berada di `bin/Debug/net8.0-windows/`. Folder `bin/`, `obj/`, dan `.vs/` diabaikan oleh Git melalui `.gitignore`.

## Konfigurasi Supabase

Untuk alur multi user, pemegang database perlu menyediakan tabel, view, RPC, dan RLS yang dijelaskan di [DB_CONTRACT.md](DB_CONTRACT.md). Aplikasi tidak membuat atau mengubah skema Supabase secara otomatis. Sampai kontrak itu tersedia, portal user dan verifikasi pengajuan belum dapat digunakan secara penuh.

`SupabaseClient.vb` berisi `SupabaseUrl` dan `SupabaseAnonKey` yang dipakai aplikasi saat ini. Untuk memakai proyek Supabase lain, ubah kedua konstanta tersebut agar menunjuk ke proyek Anda. Gunakan **publishable/anon key**, bukan service role key, pada aplikasi desktop. Atur kebijakan akses Supabase sesuai peran pengguna dan operasi yang diperlukan.

Alur login:

1. `FormLogin` mengirim email dan password ke Supabase Auth.
2. `SupabaseClient` membaca profil dari `public.users` berdasarkan ID akun Auth.
3. Login dilanjutkan jika profil ditemukan dan `is_active` bernilai `true`.
4. `FormMain` membuka dashboard dan menampilkan nama serta peran pengguna.

Tabel yang dirujuk kode meliputi `users`, `assets`, `categories`, `locations`, `suppliers`, `stock_transactions`, `loans`, dan `maintenances`. View yang dirujuk meliputi `v_assets`, `v_dashboard_summary`, `v_monthly_transactions`, `v_category_totals`, `v_location_totals`, `v_recent_activity`, `v_stock_transactions`, `v_loans`, dan `v_maintenances`. Daftar ini membantu mencocokkan skema Supabase; definisi kolom, relasi, dan kebijakan database harus disiapkan pada proyek Supabase.

## Struktur proyek

```text
AsetKu.sln / AsetKu.slnx    Solution Visual Studio
AsetKu.vbproj               Konfigurasi proyek WinForms .NET 8
Program.vb                  Titik masuk aplikasi
FormLogin.vb                Form autentikasi
FormMain.vb                 Form utama dan navigasi menu
Form*.Designer.vb, *.resx   Layout dan resource form
uc*.vb                      Halaman fitur dan dialog terkait
ucAsetForm.resx             Resource halaman formulir aset
SupabaseClient.vb           Klien HTTP dan helper JSON Supabase
Theme.vb, UiExtras.vb       Tema dan komponen antarmuka bersama
README.md                   Panduan proyek
```

Seluruh file sumber saat ini berada di root proyek. Susunan ini masih mudah dibuka dan dibangun karena jumlah file relatif sedikit, tetapi pengelompokan berdasarkan `Forms/`, `Pages/`, `Components/`, dan `Services/` akan memudahkan pengembangan saat proyek bertambah. Jika file dipindahkan, pindahkan pasangan `.Designer.vb` dan `.resx` bersama formnya, lalu periksa tampilan Designer dan build ulang. `AsetKu.sln` adalah pilihan yang didokumentasikan untuk Visual Studio; `AsetKu.slnx` juga tersedia.

## Catatan penggunaan

- Tombol **Export Excel** menghasilkan file `.csv` dengan pemisah titik koma, bukan workbook `.xlsx`.
- Tombol **Export PDF** membuka pratinjau cetak. Pilih printer PDF di dialog cetak untuk menyimpan PDF.
- Dashboard menyegarkan data secara berkala saat halaman tampil.
- Font antarmuka menggunakan **Inter** jika terpasang dan **Segoe UI** sebagai pengganti.

## Jika ada masalah

| Gejala | Pemeriksaan |
| --- | --- |
| Target .NET 8 tidak tersedia | Pasang workload .NET desktop serta dukungan .NET 8 di Visual Studio Installer. |
| Build gagal setelah membuka solution | Jalankan `dotnet restore AsetKu.sln`, lalu `dotnet build AsetKu.sln`. |
| Login ditolak | Periksa email/password, baris profil di `public.users`, serta nilai `is_active`. |
| Data kosong atau muncul error Supabase | Periksa koneksi internet, URL/key proyek, tabel/view, dan kebijakan akses Supabase. |
| Designer tidak menampilkan halaman menu | File `uc*.vb` menyusun kontrol melalui kode; jalankan aplikasi untuk melihat halaman tersebut. |
