Imports System.Globalization

Public Enum TrxMode
    Masuk
    Keluar
End Enum

''' <summary>Satu baris riwayat transaksi barang (nanti dari tabel stock_transactions).</summary>
Public Class TrxRow
    Public Property No As String
    Public Property Tanggal As Date
    Public Property Barang As String
    Public Property Pihak As String      ' supplier (masuk) / tujuan (keluar)
    Public Property Jumlah As String     ' mis. "24 unit"
    Public Property Petugas As String

    Public Sub New(no As String, tgl As Date, barang As String, pihak As String, jumlah As String, petugas As String)
        Me.No = no : Tanggal = tgl : Me.Barang = barang : Me.Pihak = pihak : Me.Jumlah = jumlah : Me.Petugas = petugas
    End Sub
End Class

''' <summary>Halaman Barang Masuk / Barang Keluar (satu komponen, dua mode).</summary>
Public Class ucTransaksi
    Inherits UserControl

    Private Const PageSize As Integer = 5
    Private ReadOnly _mode As TrxMode
    Private ReadOnly _user As String
    Private ReadOnly _id As New CultureInfo("id-ID")
    Private ReadOnly _data As New List(Of TrxRow)
    Private _page As Integer = 0

    Private ReadOnly _dtTanggal As AppDateBox
    Private ReadOnly _cbBarang As AppComboBox
    Private ReadOnly _txtJumlah As AppTextBox
    Private ReadOnly _cbPihak As AppComboBox
    Private ReadOnly _txtRef As AppTextBox
    Private ReadOnly _txtCatatan As AppTextBox
    Private ReadOnly _grid As DataGridView
    Private ReadOnly _lblFooter As Label
    Private ReadOnly _lblPage As Label
    Private Const AksiCol As Integer = 6

    Private ReadOnly Property IsMasuk As Boolean
        Get
            Return _mode = TrxMode.Masuk
        End Get
    End Property

    Public Sub New(mode As TrxMode, userName As String)
        _mode = mode
        _user = If(String.IsNullOrWhiteSpace(userName), "Pengguna", Char.ToUpper(userName(0)) & userName.Substring(1))
        BackColor = Theme.OffWhite
        DoubleBuffered = True
        LoadSampleData()

        Dim judul = If(IsMasuk, "Barang Masuk", "Barang Keluar")
        Controls.Add(MakeLabel(judul, 26, Theme.Charcoal, 24, 22, FontStyle.Bold))
        Controls.Add(MakeLabel(If(IsMasuk, "Catat penerimaan barang dan pantau penambahan stok.", "Catat pengeluaran barang untuk kebutuhan operasional."), 14, Theme.Grey, 24, 62))

        ' ===== Kartu form "Transaksi baru" (24,100 | 1078 x 218) =====
        Dim card As New RoundedPanel() With {.Location = New Point(24, 100), .Size = New Size(1078, 218), .Radius = 8}
        card.Controls.Add(MakeLabel("Transaksi baru", 18, Theme.Charcoal, 16, 14, FontStyle.Bold))

        _dtTanggal = New AppDateBox()
        _dtTanggal.Picker.CustomFormat = "d MMM yyyy"
        _cbBarang = New AppComboBox() With {.PlaceholderText = "Pilih aset / barang"}
        _cbBarang.SetItems("Mouse Logitech M170", "Kertas A4 80 gsm", "Laptop Lenovo ThinkPad", "Kursi Kantor", "Tinta Printer Epson", "Map Arsip", "Kabel HDMI 2 meter", "Proyektor Epson", "Printer Canon", "Meja Kerja")
        _txtJumlah = New AppTextBox() With {.PlaceholderText = "1"}
        _txtJumlah.Inner.Text = "1"
        AddHandler _txtJumlah.Inner.KeyPress, AddressOf DigitsOnly
        _cbPihak = New AppComboBox()
        If IsMasuk Then
            _cbPihak.PlaceholderText = "Pilih supplier"
            _cbPihak.SetItems("PT Sumber Teknologi", "CV Mitra Kantor", "CV Furni Nusantara", "PT Teknologi Nusantara")   ' TODO (Supabase): tabel suppliers
        Else
            _cbPihak.PlaceholderText = "Pilih penerima / tujuan"
            _cbPihak.SetItems("Divisi Operasional", "Divisi Keuangan", "Divisi Administrasi", "Divisi IT", "Ruang Rapat")
        End If
        _txtRef = New AppTextBox()
        _txtCatatan = New AppTextBox() With {.PlaceholderText = "Tambahkan catatan (opsional)"}

        Place(card, "Tanggal transaksi", _dtTanggal, 16, 51, 249)
        Place(card, "Aset / Barang", _cbBarang, 281, 51, 249)
        Place(card, "Jumlah", _txtJumlah, 546, 51, 249)
        Place(card, If(IsMasuk, "Supplier", "Penerima / Tujuan"), _cbPihak, 811, 51, 249)
        Place(card, "Nomor referensi", _txtRef, 16, 131, 249)
        Place(card, "Catatan", _txtCatatan, 281, 131, 397)

        Dim btnCancel As New AppButton() With {.Text = "Batal", .Kind = ButtonKind.Outline, .Location = New Point(694, 158), .Size = New Size(108, 36), .BackColor = Theme.OffWhite}
        AddHandler btnCancel.Click, AddressOf ResetForm
        card.Controls.Add(btnCancel)
        Dim btnSave As New AppButton() With {.Text = If(IsMasuk, "Simpan Barang Masuk", "Simpan Barang Keluar"), .Kind = ButtonKind.Primary, .Location = New Point(814, 158), .Size = New Size(248, 36), .BackColor = Theme.OffWhite}
        AddHandler btnSave.Click, AddressOf SaveTrx
        card.Controls.Add(btnSave)
        Controls.Add(card)

        ' ===== Kartu riwayat (24,338 | 1078 x 342) =====
        Dim hist As New RoundedPanel() With {.Location = New Point(24, 338), .Size = New Size(1078, 342), .Radius = 8}
        hist.Controls.Add(MakeLabel("Riwayat " & judul, 18, Theme.Charcoal, 16, 14, FontStyle.Bold))

        Dim wrap As New Panel() With {.Location = New Point(16, 52), .Size = New Size(1046, 230), .BackColor = Theme.OffWhite}
        _grid = New DataGridView() With {.Dock = DockStyle.Fill}
        StyleGrid(_grid, 38)
        _grid.ColumnHeadersDefaultCellStyle.Padding = New Padding(10, 0, 0, 0)
        _grid.DefaultCellStyle.Padding = New Padding(10, 0, 0, 0)
        Dim heads As String() = {"No. Transaksi", "Tanggal", "Nama Barang", If(IsMasuk, "Supplier", "Tujuan"), "Jumlah", "Petugas", "Aksi"}
        Dim widths As Integer() = {130, 108, 220, 200, 90, 190, 108}
        For i = 0 To heads.Length - 1
            _grid.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = heads(i), .Width = widths(i), .SortMode = DataGridViewColumnSortMode.NotSortable})
        Next
        With _grid.Columns(0).DefaultCellStyle
            .Font = AppFont(13, FontStyle.Bold)
            .ForeColor = Theme.Charcoal
            .SelectionForeColor = Theme.Charcoal
        End With
        AddHandler _grid.CellMouseMove, AddressOf GridMouseMove
        AddHandler _grid.CellMouseClick, AddressOf GridMouseClick
        wrap.Controls.Add(_grid)
        hist.Controls.Add(wrap)

        _lblFooter = MakeLabel("", 12, Theme.Grey, 16, 304)
        hist.Controls.Add(_lblFooter)
        Dim btnPrev As New AppButton() With {.Text = "Sebelumnya", .Kind = ButtonKind.Outline, .Location = New Point(795, 294), .Size = New Size(107, 36), .BackColor = Theme.OffWhite}
        AddHandler btnPrev.Click, Sub(s, e) GoPage(_page - 1)
        hist.Controls.Add(btnPrev)
        _lblPage = MakeLabel("1 / 1", 12, Theme.Charcoal, 906, 304, FontStyle.Bold, 48, 18)
        _lblPage.TextAlign = ContentAlignment.MiddleCenter
        hist.Controls.Add(_lblPage)
        Dim btnNext As New AppButton() With {.Text = "Berikutnya", .Kind = ButtonKind.Outline, .Location = New Point(958, 294), .Size = New Size(104, 36), .BackColor = Theme.OffWhite}
        AddHandler btnNext.Click, Sub(s, e) GoPage(_page + 1)
        hist.Controls.Add(btnNext)
        Controls.Add(hist)

        _txtRef.Inner.Text = NextRef()
        BindPage()
    End Sub

    ' ---------- layout & data ----------
    Private Sub Place(card As Control, caption As String, ctl As Control, x As Integer, y As Integer, w As Integer)
        card.Controls.Add(MakeLabel(caption, 12, Theme.Charcoal, x, y, FontStyle.Bold))
        ctl.Location = New Point(x, y + 25)
        ctl.Width = w
        card.Controls.Add(ctl)
    End Sub

    Private Sub LoadSampleData()
        ' TODO (Supabase): SELECT dari stock_transactions WHERE type = 'masuk' / 'keluar' JOIN assets, users.
        Dim d = Function(day As Integer) New Date(2025, 6, day)
        If IsMasuk Then
            _data.Add(New TrxRow("BM-0625-018", d(24), "Mouse Logitech M170", "PT Sumber Teknologi", "24 unit", "Aditya Pratama"))
            _data.Add(New TrxRow("BM-0625-017", d(23), "Kertas A4 80 gsm", "CV Mitra Kantor", "50 rim", "Rina Lestari"))
            _data.Add(New TrxRow("BM-0625-016", d(22), "Laptop Lenovo ThinkPad", "PT Sumber Teknologi", "5 unit", "Aditya Pratama"))
            _data.Add(New TrxRow("BM-0625-015", d(21), "Kursi Kantor", "CV Furni Nusantara", "12 unit", "Dimas Saputra"))
            _data.Add(New TrxRow("BM-0625-014", d(20), "Tinta Printer Epson", "CV Mitra Kantor", "16 botol", "Rina Lestari"))
            _data.Add(New TrxRow("BM-0625-013", d(19), "Printer Canon", "PT Sumber Teknologi", "2 unit", "Aditya Pratama"))
            _data.Add(New TrxRow("BM-0625-012", d(18), "Kertas A4 80 gsm", "CV Mitra Kantor", "30 rim", "Rina Lestari"))
            _data.Add(New TrxRow("BM-0625-011", d(17), "Meja Kerja", "CV Furni Nusantara", "6 unit", "Dimas Saputra"))
        Else
            _data.Add(New TrxRow("BK-0625-012", d(24), "Kertas A4 80 gsm", "Divisi Keuangan", "10 rim", "Rina Lestari"))
            _data.Add(New TrxRow("BK-0625-011", d(23), "Mouse Logitech M170", "Divisi Operasional", "4 unit", "Dimas Saputra"))
            _data.Add(New TrxRow("BK-0625-010", d(22), "Tinta Printer Epson", "Divisi Administrasi", "2 botol", "Aditya Pratama"))
            _data.Add(New TrxRow("BK-0625-009", d(21), "Map Arsip", "Divisi Keuangan", "20 unit", "Rina Lestari"))
            _data.Add(New TrxRow("BK-0625-008", d(20), "Kabel HDMI 2 meter", "Ruang Rapat", "3 unit", "Dimas Saputra"))
            _data.Add(New TrxRow("BK-0625-007", d(19), "Printer Canon", "Divisi IT", "1 unit", "Aditya Pratama"))
            _data.Add(New TrxRow("BK-0625-006", d(18), "Kertas A4 80 gsm", "Divisi Administrasi", "5 rim", "Rina Lestari"))
            _data.Add(New TrxRow("BK-0625-005", d(17), "Kursi Kantor", "Ruang Rapat", "4 unit", "Dimas Saputra"))
        End If
    End Sub

    Private Function NextRef() As String
        Return If(IsMasuk, "BM", "BK") & "-" & Date.Now.ToString("MMyy") & "-" & (_data.Count + 1).ToString("000")
    End Function

    Private Shared Function UnitFor(barang As String) As String
        If barang.StartsWith("Kertas") Then Return "rim"
        If barang.StartsWith("Tinta") Then Return "botol"
        Return "unit"
    End Function

    ' ---------- paginasi ----------
    Private Function PageCount() As Integer
        Return Math.Max(1, CInt(Math.Ceiling(_data.Count / PageSize)))
    End Function

    Private Sub GoPage(p As Integer)
        If p < 0 OrElse p >= PageCount() Then Return
        _page = p
        BindPage()
    End Sub

    Private Sub BindPage()
        _grid.Rows.Clear()
        Dim skip = _page * PageSize
        For Each r In _data.Skip(skip).Take(PageSize)
            _grid.Rows.Add(r.No, r.Tanggal.ToString("dd MMM yyyy", _id), r.Barang, r.Pihak, r.Jumlah, r.Petugas, "Detail")
        Next
        _grid.ClearSelection()
        Dim first = skip + 1
        Dim last = Math.Min(skip + PageSize, _data.Count)
        _lblFooter.Text = If(_data.Count = 0, "Belum ada data.", "Menampilkan " & first & "–" & last & " dari " & _data.Count & " data")
        _lblPage.Text = (_page + 1) & " / " & PageCount()
    End Sub

    ' ---------- tabel ----------
    Private Sub GridMouseMove(sender As Object, e As DataGridViewCellMouseEventArgs)
        _grid.Cursor = If(e.RowIndex >= 0 AndAlso e.ColumnIndex = AksiCol, Cursors.Hand, Cursors.Default)
    End Sub

    Private Sub GridMouseClick(sender As Object, e As DataGridViewCellMouseEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex <> AksiCol Then Return
        Dim r = _data(_page * PageSize + e.RowIndex)
        ' TODO: ganti dengan dialog detail transaksi bila ada di desain.
        MessageBox.Show("No. Transaksi : " & r.No & vbCrLf &
                        "Tanggal       : " & r.Tanggal.ToString("dd MMM yyyy", _id) & vbCrLf &
                        "Barang        : " & r.Barang & vbCrLf &
                        If(IsMasuk, "Supplier      : ", "Tujuan        : ") & r.Pihak & vbCrLf &
                        "Jumlah        : " & r.Jumlah & vbCrLf &
                        "Petugas       : " & r.Petugas, "Detail transaksi", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ' ---------- form ----------
    Private Sub DigitsOnly(sender As Object, e As KeyPressEventArgs)
        If Not Char.IsControl(e.KeyChar) AndAlso Not Char.IsDigit(e.KeyChar) Then e.Handled = True
    End Sub

    Private Sub ResetForm(sender As Object, e As EventArgs)
        _dtTanggal.Picker.Value = Date.Today
        _cbBarang.SelectedIndex = -1
        _cbPihak.SelectedIndex = -1
        _txtJumlah.Inner.Text = "1"
        _txtCatatan.Inner.Text = ""
        _txtRef.Inner.Text = NextRef()
    End Sub

    Private Sub SaveTrx(sender As Object, e As EventArgs)
        Dim missing As New List(Of String)
        If _cbBarang.SelectedItem Is Nothing Then missing.Add("Aset / Barang")
        Dim qty As Integer
        If Not Integer.TryParse(_txtJumlah.Inner.Text, qty) OrElse qty < 1 Then missing.Add("Jumlah (minimal 1)")
        If _cbPihak.SelectedItem Is Nothing Then missing.Add(If(IsMasuk, "Supplier", "Penerima / Tujuan"))
        If _txtRef.Inner.Text.Trim() = "" Then missing.Add("Nomor referensi")
        If missing.Count > 0 Then
            MessageBox.Show("Lengkapi data berikut:" & vbCrLf & "- " & String.Join(vbCrLf & "- ", missing), "Data belum lengkap", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' TODO (Supabase): INSERT INTO stock_transactions (asset_id, user_id, type, quantity, trx_date, notes, ...)
        ' lalu update assets.quantity (+ untuk masuk, - untuk keluar; tolak jika stok tidak cukup).
        Dim row As New TrxRow(_txtRef.Inner.Text.Trim(), _dtTanggal.Picker.Value.Date, _cbBarang.SelectedItem, _cbPihak.SelectedItem, qty & " " & UnitFor(_cbBarang.SelectedItem), _user)
        _data.Insert(0, row)
        _page = 0
        BindPage()
        ResetForm(sender, e)
        MessageBox.Show(If(IsMasuk, "Barang masuk", "Barang keluar") & " tersimpan (mode demo).", "Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub
End Class
