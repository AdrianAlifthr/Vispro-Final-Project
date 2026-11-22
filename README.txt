AsetKu - Desktop UI (VB.NET WinForms) - Login, Dashboard, Data Aset, Form Aset, Detail Aset, Dialog Hapus, Barang Masuk, Barang Keluar, Peminjaman + dialog Pinjam Aset, Pemeliharaan, Master Data, Manajemen User, Laporan

CARA PAKAI (Visual Studio 2022):
1. Pastikan workload ".NET desktop development" terpasang (butuh .NET 8 SDK).
2. Buka File > Open > Project/Solution > AsetKu.sln (jangan Open Folder).
3. Untuk Designer: klik kanan FormLogin.vb atau FormMain.vb > View Designer (Shift+F7).
4. Tekan F5 untuk menjalankan aplikasi. Login demo menerima username dan password apa saja yang tidak kosong.

Atau dari terminal:  dotnet run

Catatan:
- Layout Login dan kerangka form utama sekarang ada di file .Designer.vb dan bisa diedit melalui Designer.
- Isi halaman menu (uc*.vb) masih dibuat lewat kode; dashboard dimuat saat aplikasi berjalan.
- Font: pakai "Inter" jika terpasang, kalau tidak otomatis "Segoe UI".
- Login sementara menerima input apa pun yang terisi (lihat TODO Supabase di FormLogin.vb).
- Data dashboard masih dummy (lihat TODO Supabase di ucDashboard.vb).
- Semua menu sudah dibuat. Semua data masih dummy (TODO Supabase).
