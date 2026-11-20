Imports System.Globalization
Imports System.IO

''' <summary>Form Tambah / Edit Aset. Tanpa parameter = Tambah, dengan AssetRow = Edit.</summary>
Public Class ucAsetForm
    Inherits UserControl

    Private ReadOnly _editing As AssetRow
    Private ReadOnly _txtKode As AppTextBox
    Private ReadOnly _txtNama As AppTextBox
    Private ReadOnly _cbKategori As AppComboBox
    Private ReadOnly _cbLokasi As AppComboBox
    Private ReadOnly _cbSupplier As AppComboBox
    Private ReadOnly _dtTanggal As AppDateBox
    Private ReadOnly _txtHarga As AppTextBox
    Private ReadOnly _txtJumlah As AppTextBox
    Private ReadOnly _cbKondisi As AppComboBox
    Private ReadOnly _txtKet As AppTextBox
    Private ReadOnly _photo As PhotoBox

    Public Sub New(Optional editing As AssetRow = Nothing)
        _editing = editing
        BackColor = Theme.OffWhite
        DoubleBuffered = True

        ' ===== Breadcrumb + judul =====
        Dim crumb = MakeLabel("Data Aset / Barang", 12, Theme.Grey, 24, 20)
        crumb.Cursor = Cursors.Hand
        AddHandler crumb.Click, AddressOf GoBack
        Controls.Add(crumb)
        Controls.Add(MakeLabel("/", 12, Theme.Grey, 134, 20))
        Controls.Add(MakeLabel(If(editing Is Nothing, "Tambah Aset", "Edit Aset"), 12, Theme.Grey, 155, 20))
        Controls.Add(MakeLabel(If(editing Is Nothing, "Tambah Aset", "Edit Aset"), 26, Theme.Charcoal, 24, 46, FontStyle.Bold))

        ' ===== Kartu kiri: Informasi aset (24,104 | 722 x 472) =====
        Dim card As New RoundedPanel() With {.Location = New Point(24, 104), .Size = New Size(722, 472), .Radius = 8}
        card.Controls.Add(MakeLabel("Informasi aset", 20, Theme.Charcoal, 24, 14, FontStyle.Bold))
        card.Controls.Add(MakeLabel("Lengkapi data utama aset di bawah ini.", 12, Theme.Grey, 24, 48))

        _txtKode = New AppTextBox()
        _txtNama = New AppTextBox()
        _cbKategori = New AppComboBox() With {.PlaceholderText = "Pilih kategori"}
        _cbLokasi = New AppComboBox() With {.PlaceholderText = "Pilih lokasi"}
        _cbSupplier = New AppComboBox() With {.PlaceholderText = "Pilih supplier"}
        _dtTanggal = New AppDateBox()
        _txtHarga = New AppTextBox() With {.PlaceholderText = "Rp  0"}
        _txtJumlah = New AppTextBox() With {.PlaceholderText = "1"}
        _cbKondisi = New AppComboBox() With {.PlaceholderText = "Pilih kondisi"}
        _txtKet = New AppTextBox() With {.PlaceholderText = "Tambahkan keterangan"}

        ' TODO (Supabase): isi daftar ini dari tabel categories, locations, suppliers.
        _cbKategori.SetItems("Elektronik", "Furnitur", "Peralatan", "ATK")
        _cbLokasi.SetItems("Ruang IT", "Ruang Rapat", "Gudang A", "Ruang Server", "Administrasi", "Ruang Keuangan")
        _cbSupplier.SetItems("PT Teknologi Nusantara", "CV Mitra Furnitur", "UD Sumber Jaya")
        _cbKondisi.SetItems("Baik", "Rusak ringan", "Rusak berat")

        AddField(card, "Kode Aset *", _txtKode, 0, 0)
        AddField(card, "Nama Aset *", _txtNama, 1, 0)
        AddField(card, "Kategori *", _cbKategori, 0, 1)
        AddField(card, "Lokasi *", _cbLokasi, 1, 1)
        AddField(card, "Supplier", _cbSupplier, 0, 2)
        AddField(card, "Tanggal Perolehan", _dtTanggal, 1, 2)
        AddField(card, "Harga Perolehan", _txtHarga, 0, 3)
        AddField(card, "Jumlah *", _txtJumlah, 1, 3)
        AddField(card, "Kondisi *", _cbKondisi, 0, 4)
        AddField(card, "Keterangan", _txtKet, 1, 4)
        Controls.Add(card)

        AddHandler _txtJumlah.Inner.KeyPress, AddressOf DigitsOnly
        AddHandler _txtHarga.Inner.KeyPress, AddressOf DigitsOnly
        AddHandler _txtHarga.Inner.Leave, AddressOf FormatHarga

        ' ===== Kartu kanan: Foto aset (738,104 | 340 x 335) =====
        Dim cardFoto As New RoundedPanel() With {.Location = New Point(738, 104), .Size = New Size(340, 335), .Radius = 8}
        cardFoto.Controls.Add(MakeLabel("Foto aset", 20, Theme.Charcoal, 20, 14, FontStyle.Bold))
        _photo = New PhotoBox() With {.Location = New Point(20, 60), .Size = New Size(300, 180)}
        AddHandler _photo.Click, AddressOf PickPhoto
        cardFoto.Controls.Add(_photo)
        Dim btnPhoto As New AppButton() With {.Text = "Pilih Foto", .Kind = ButtonKind.Outline, .Location = New Point(20, 256), .Size = New Size(300, 36), .BackColor = Theme.OffWhite}
        AddHandler btnPhoto.Click, AddressOf PickPhoto
        cardFoto.Controls.Add(btnPhoto)
        cardFoto.Controls.Add(MakeLabel("JPG, PNG  •  Maks. 2 MB", 12, Theme.Grey, 20, 301))
        Controls.Add(cardFoto)

        ' ===== Catatan + tombol =====
        Controls.Add(MakeLabel("* Wajib diisi. Data dapat diubah kembali setelah disimpan.", 12, Theme.Grey, 24, 592))
        Dim btnCancel As New AppButton() With {.Text = "Batal", .Kind = ButtonKind.Outline, .Location = New Point(814, 636), .Size = New Size(120, 36), .BackColor = Theme.OffWhite}
        AddHandler btnCancel.Click, AddressOf GoBack
        Controls.Add(btnCancel)
        Dim btnSave As New AppButton() With {.Text = "Simpan", .Kind = ButtonKind.Primary, .Location = New Point(950, 636), .Size = New Size(152, 36), .BackColor = Theme.OffWhite}
        AddHandler btnSave.Click, AddressOf SaveAsset
        Controls.Add(btnSave)

        FillInitialValues()
    End Sub

    ' ---------- layout ----------
    Private Sub AddField(card As Control, caption As String, ctl As Control, col As Integer, row As Integer)
        Dim x = If(col = 0, 24, 373)
        Dim y = 88 + row * 76
        card.Controls.Add(MakeLabel(caption, 14, Theme.Charcoal, x, y, FontStyle.Bold))
        ctl.Location = New Point(x, y + 28)
        ctl.Width = 325
        card.Controls.Add(ctl)
    End Sub

    Private Sub FillInitialValues()
        If _editing Is Nothing Then
            _txtKode.Inner.Text = "AST-008"   ' TODO (Supabase): hitung kode berikutnya dari DB
            _txtJumlah.Inner.Text = "1"
        Else
            _txtKode.Inner.Text = _editing.Kode
            _txtNama.Inner.Text = _editing.Nama
            _cbKategori.SelectByText(_editing.Kategori)
            _cbLokasi.SelectByText(_editing.Lokasi)
            _cbKondisi.SelectByText(_editing.Kondisi)
            _txtJumlah.Inner.Text = _editing.Jumlah.ToString()
            ' TODO (Supabase): supplier, tanggal, harga, keterangan & foto dari tabel assets.
        End If
    End Sub

    ' ---------- input ----------
    Private Sub DigitsOnly(sender As Object, e As KeyPressEventArgs)
        If Not Char.IsControl(e.KeyChar) AndAlso Not Char.IsDigit(e.KeyChar) AndAlso e.KeyChar <> "."c Then e.Handled = True
    End Sub

    Private Shared Function DigitsIn(s As String) As String
        Return New String(s.Where(Function(c) Char.IsDigit(c)).ToArray())
    End Function

    Private Sub FormatHarga(sender As Object, e As EventArgs)
        Dim d = DigitsIn(_txtHarga.Inner.Text)
        If d = "" Then
            _txtHarga.Inner.Text = ""
        Else
            _txtHarga.Inner.Text = "Rp  " & Decimal.Parse(d).ToString("N0", New CultureInfo("id-ID"))
        End If
    End Sub

    Private Sub PickPhoto(sender As Object, e As EventArgs)
        Using dlg As New OpenFileDialog() With {.Title = "Pilih foto aset", .Filter = "Gambar (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png"}
            If dlg.ShowDialog() <> DialogResult.OK Then Return
            If New FileInfo(dlg.FileName).Length > 2 * 1024 * 1024 Then
                MessageBox.Show("Ukuran foto maksimal 2 MB.", "Foto terlalu besar", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            _photo.LoadImage(dlg.FileName)
        End Using
    End Sub

    ' ---------- simpan / batal ----------
    Private Sub SaveAsset(sender As Object, e As EventArgs)
        Dim missing As New List(Of String)
        If _txtKode.Inner.Text.Trim() = "" Then missing.Add("Kode Aset")
        If _txtNama.Inner.Text.Trim() = "" Then missing.Add("Nama Aset")
        If _cbKategori.SelectedItem Is Nothing Then missing.Add("Kategori")
        If _cbLokasi.SelectedItem Is Nothing Then missing.Add("Lokasi")
        If _cbKondisi.SelectedItem Is Nothing Then missing.Add("Kondisi")
        Dim qty As Integer
        If Not Integer.TryParse(_txtJumlah.Inner.Text, qty) OrElse qty < 1 Then missing.Add("Jumlah (minimal 1)")

        If missing.Count > 0 Then
            MessageBox.Show("Lengkapi data berikut:" & vbCrLf & "- " & String.Join(vbCrLf & "- ", missing), "Data belum lengkap", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' TODO (Supabase): INSERT (tambah) atau UPDATE (edit) ke tabel assets,
        ' lalu unggah foto ke Supabase Storage dan simpan URL-nya di kolom photo_url.
        MessageBox.Show("Aset " & _txtKode.Inner.Text.Trim() & " berhasil disimpan (mode demo).", "Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information)
        GoBack(sender, e)
    End Sub

    Private Sub GoBack(sender As Object, e As EventArgs)
        TryCast(FindForm(), FormMain)?.ShowPage(New ucAsetList(), 1)
    End Sub
End Class
