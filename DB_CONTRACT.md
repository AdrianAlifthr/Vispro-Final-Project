# Kontrak Supabase untuk AsetKu multi user

Dokumen ini untuk pemegang proyek Supabase. Aplikasi desktop tidak menjalankan migrasi dan tidak menyimpan service role key. Fitur baru dapat dicoba dengan mode demo sampai kontrak ini tersedia di database.

## Pengguna, divisi, dan jenis aset

- `public.divisions(id, name)` menyediakan daftar divisi. `public.users.division_id` mengacu ke divisi untuk setiap akun `staff`; `users.user_code`, `full_name`, `role`, dan `is_active` dipakai untuk identitas penyerahan. Admin boleh melihat semua akun dan mengubah divisi/status; staff hanya melihat profilnya sendiri.
- Aplikasi saat ini membuat akun lewat Supabase Auth signup lalu admin memperbarui profilnya. Kebijakan server harus memastikan hanya admin dapat mengubah `role`, `division_id`, dan `is_active`; akun signup baru jangan otomatis aktif sebagai admin/staff. Untuk pengelolaan Auth yang lebih lengkap, pemegang database dapat menyediakan layanan server tepercaya. Jangan menaruh service role key di aplikasi desktop. Menonaktifkan akun memakai `is_active = false`, bukan menghapus profil, agar riwayat tetap utuh.
- `assets.request_kind` bernilai `out` atau `loan` dan ditetapkan admin pada form aset. `out` untuk barang habis pakai/penyerahan permanen; `loan` untuk aset yang wajib kembali. Hanya jenis yang cocok boleh diajukan.

## Pengajuan dan tampilan riwayat

- `asset_requests` menyimpan `id`, `request_type`, `asset_id`, `quantity`, `notes`, `due_date`, `requester_id`, `division_id`, `status`, `decision_note`, `reviewed_by`, `reviewed_at`, `handed_by`, `handed_at`, `created_at`. Status: `pending`, `approved`, `rejected`, `cancelled`, `handed_out`, `return_pending`, `returned`.
- Pada insert, server menetapkan `requester_id = auth.uid()`, divisi dari profil saat pengajuan, `status = pending`, dan waktu pembuatan. Pemohon adalah satu-satunya penerima/peminjam. Identitas dan status dari klien harus diabaikan atau ditolak. `quantity >= 1`; untuk `loan` harus 1 dan `due_date` wajib.
- `v_asset_requests` (dengan `security_invoker` atau pembatasan setara) menyediakan `id`, `request_type`, `asset_name`, `asset_code`, `requester_name`, `requester_code`, `division_name`, `quantity`, `status`, `decision_note`, `notes`, `created_at`, `due_date`, `handed_by_name`, `handed_at`. Admin melihat semua; staff hanya pengajuannya sendiri. Simpan nama/divisi pada riwayat agar pemindahan divisi akun tidak mengubah catatan lama.
- Transaksi final di `stock_transactions` dan `loans` terhubung unik ke `asset_requests.id`. View `v_stock_transactions` dan `v_loans` tetap memuat data lama; tambahkan `request_id` pada `v_loans`. Untuk riwayat baru, tampilkan nama pemohon dan divisinya, bukan hanya petugas admin.

## RPC atomik

Semua RPC menerima JWT pengguna dan memeriksa peran/status akun di server. Setiap transisi mengunci baris pengajuan; transisi tidak sah dan panggilan ulang harus ditolak tanpa efek kedua.

| RPC | Input | Efek |
| --- | --- | --- |
| `approve_asset_request` | `p_request_id`, `p_note` | Admin mengubah `pending` menjadi `approved`; stok dan status aset belum berubah. |
| `reject_asset_request` | `p_request_id`, `p_note` | Admin mengubah `pending` menjadi `rejected`; alasan wajib, stok tetap. |
| `cancel_asset_request` | `p_request_id`, `p_note` | Admin membatalkan `approved` yang belum diserahkan; alasan wajib, stok tetap. |
| `handover_asset_request` | `p_request_id` | Admin memeriksa akun pemohon masih aktif, jenis aset cocok, dan ketersediaan terbaru. Untuk `out`, buat satu transaksi keluar dan kurangi stok melalui mekanisme yang ada; untuk `loan`, buat satu pinjaman aktif. Simpan petugas/waktu dan ubah ke `handed_out` dalam satu transaksi. |
| `request_loan_return` | `p_request_id` | Hanya pemohon pinjaman `handed_out` dapat mengubahnya ke `return_pending`; aset tetap dipinjam. |
| `confirm_loan_return` | `p_request_id` | Admin menutup pinjaman terkait dan mengubah `return_pending` ke `returned` dalam satu transaksi; aset kembali tersedia. |

Gunakan batasan unik serta penguncian yang mencegah dua penyerahan atas aset/stok yang sama. Jika trigger lama sudah mengurangi stok saat insert `stock_transactions`, RPC jangan mengurangi lagi. Pinjaman baru dengan `request_id` tidak boleh ditutup melalui PATCH umum; hanya `confirm_loan_return`. PATCH status untuk pinjaman lama tanpa `request_id` tetap hanya boleh untuk admin.

## Uji serah terima

- Akun staff tidak dapat mengubah role, divisi, status akun, transaksi final, atau pengajuan milik orang lain.
- Persetujuan tidak mengubah stok; penyerahan pertama mengubahnya sekali; penyerahan ulang ditolak. Dua admin yang menyerahkan saat stok tersisa satu tidak membuat stok negatif.
- Admin melihat nama, kode user, dan divisi pemohon sebelum penyerahan. Pengajuan user nonaktif tidak dapat diserahkan.
- Pengembalian baru berstatus selesai hanya setelah diverifikasi admin. Riwayat pengajuan tidak hilang saat akun dinonaktifkan atau dipindahkan divisi.
