Imports System
Imports System.Globalization
Imports System.Text.Json
Imports System.Threading.Tasks

Public Enum TrxMode
    Masuk
    Keluar
End Enum

''' <summary>Satu baris riwayat transaksi barang dari view `v_stock_transactions`.</summary>
Public Class TrxRow
    Public Property No As String
    Public Property Tanggal As Date
    Public Property Barang As String
    Public Property Pihak As String
    Public Property Jumlah As String
    Public Property Petugas As String
    Public Property Catatan As String = ""

    Public Sub New(no As String, tgl As Date, barang As String, pihak As String, jumlah As String, petugas As String, Optional catatan As String = "")
        Me.No = no : Tanggal = tgl : Me.Barang = barang : Me.Pihak = pihak : Me.Jumlah = jumlah : Me.Petugas = petugas : Me.Catatan = catatan
    End Sub
End Class

''' <summary>Halaman Barang Masuk / Barang Keluar yang terhubung ke Supabase.</summary>
Public Class ucTransaksi
    Inherits UserControl

    Private Const PageSize As Integer = 5
    Private ReadOnly _mode As TrxMode
    Private ReadOnly _user As String
    Private ReadOnly _id As New CultureInfo("id-ID")
    Private ReadOnly _data As New List(Of TrxRow)
    Private ReadOnly _assetIds As New Dictionary(Of String, String)()
    Private ReadOnly _supplierIds As New Dictionary(Of String, String)()
    Private _page As Integer = 0

    Private ReadOnly _dtTanggal As AppDateBox
    Private ReadOnly _cbBarang As AppComboBox
    Private ReadOnly _txtJumlah As AppTextBox
    Private ReadOnly _cbPihak As AppComboBox
    Private ReadOnly _txtRef As AppTextBox
    Private ReadOnly _txtCatatan As AppTextBox
    Private ReadOnly _btnSave As AppButton
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

        Dim judul = If(IsMasuk, "Barang Masuk", "Barang Keluar")
        Controls.Add(MakeLabel(judul, 26, Theme.Charcoal, 24, 22, FontStyle.Bold))
        Controls.Add(MakeLabel(If(IsMasuk, "Catat penerimaan barang dan pantau penambahan stok.", "Catat pengeluaran barang untuk kebutuhan operasional."), 14, Theme.Grey, 24, 62))

        ' ===== Kartu form "Transaksi baru" (24,100 | 1078 x 218) =====
        Dim card As New RoundedPanel() With {.Location = New Point(24, 100), .Size = New Size(1078, 218), .Radius = 8}
        card.Controls.Add(MakeLabel("Transaksi baru", 18, Theme.Charcoal, 16, 14, FontStyle.Bold))

        _dtTanggal = New AppDateBox()
        _dtTanggal.Picker.CustomFormat = "d MMM yyyy"
        _cbBarang = New AppComboBox() With {.PlaceholderText = "Memuat aset..."}
        _txtJumlah = New AppTextBox() With {.PlaceholderText = "1"}
        _txtJumlah.Inner.Text = "1"
        AddHandler _txtJumlah.Inner.KeyPress, AddressOf DigitsOnly
        _cbPihak = New AppComboBox()
        If IsMasuk Then
            _cbPihak.PlaceholderText = "Memuat supplier..."
        Else
            _cbPihak.PlaceholderText = "Pilih penerima / tujuan"
            _cbPihak.SetItems("Divisi Operasional", "Divisi Keuangan", "Divisi Administrasi", "Divisi IT", "Ruang Rapat", "Gudang A")
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
        _btnSave = New AppButton() With {.Text = If(IsMasuk, "Simpan Barang Masuk", "Simpan Barang Keluar"), .Kind = ButtonKind.Primary, .Location = New Point(814, 158), .Size = New Size(248, 36), .BackColor = Theme.OffWhite}
        AddHandler _btnSave.Click, AddressOf SaveTrx
        card.Controls.Add(_btnSave)
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

        _lblFooter = MakeLabel("Memuat data dari Supabase...", 12, Theme.Grey, 16, 304)
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

        AddHandler Me.Load, AddressOf OnControlLoad
    End Sub

    Private Async Sub OnControlLoad(sender As Object, e As EventArgs)
        Await LoadAllAsync()
    End Sub

    Private Async Function LoadAllAsync() As Task
        Try
            ' 1. Dropdown aset
            Dim asetJson = Await SupabaseClient.GetJsonAsync("/rest/v1/assets?select=id,asset_code,name&order=name.asc")
            _assetIds.Clear()
            Dim asetNames As New List(Of String)()
            Using doc = JsonDocument.Parse(asetJson)
                For Each el In doc.RootElement.EnumerateArray()
                    Dim label = SupabaseClient.GetStr(el, "name") & " (" & SupabaseClient.GetStr(el, "asset_code") & ")"
                    If Not _assetIds.ContainsKey(label) Then
                        _assetIds.Add(label, SupabaseClient.GetStr(el, "id"))
                        asetNames.Add(label)
                    End If
                Next
            End Using
            _cbBarang.PlaceholderText = "Pilih aset / barang"
            _cbBarang.SetItems(asetNames.ToArray())

            ' 2. Dropdown supplier (khusus masuk)
            If IsMasuk Then
                Dim supJson = Await SupabaseClient.GetJsonAsync("/rest/v1/suppliers?select=id,name&order=name.asc")
                _supplierIds.Clear()
                Dim supNames As New List(Of String)()
                Using doc = JsonDocument.Parse(supJson)
                    For Each el In doc.RootElement.EnumerateArray()
                        Dim nm = SupabaseClient.GetStr(el, "name")
                        If Not _supplierIds.ContainsKey(nm) Then
                            _supplierIds.Add(nm, SupabaseClient.GetStr(el, "id"))
                            supNames.Add(nm)
                        End If
                    Next
                End Using
                _cbPihak.PlaceholderText = "Pilih supplier"
                _cbPihak.SetItems(supNames.ToArray())
            End If

            ' 3. Riwayat transaksi
            Dim tipe = If(IsMasuk, "masuk", "keluar")
            Dim trxJson = Await SupabaseClient.GetJsonAsync($"/rest/v1/v_stock_transactions?type=eq.{tipe}&order=trx_date.desc,created_at.desc")
            _data.Clear()
            Using doc = JsonDocument.Parse(trxJson)
                For Each el In doc.RootElement.EnumerateArray()
                    Dim refNo = SupabaseClient.GetStr(el, "ref_no")
                    Dim tgl = SupabaseClient.GetDate(el, "trx_date")
                    Dim barang = SupabaseClient.GetStr(el, "asset_name")
                    Dim pihak = If(IsMasuk, SupabaseClient.GetStr(el, "supplier_name"), SupabaseClient.GetStr(el, "destination"))
                    Dim qty = SupabaseClient.GetInt(el, "quantity")
                    Dim officer = SupabaseClient.GetStr(el, "officer_name")
                    Dim notes = SupabaseClient.GetStr(el, "notes")
                    _data.Add(New TrxRow(refNo, tgl, barang, If(pihak = "", "-", pihak), qty & " unit", If(officer = "", "-", officer), notes))
                Next
            End Using

            _txtRef.Inner.Text = NextRef()
            _page = 0
            BindPage()
        Catch ex As Exception
            _lblFooter.Text = "Gagal memuat data dari Supabase."
            MessageBox.Show("Gagal memuat transaksi dari Supabase:" & vbCrLf & ex.Message, "Error Supabase", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

    ' ---------- layout & helper ----------
    Private Sub Place(card As Control, caption As String, ctl As Control, x As Integer, y As Integer, w As Integer)
        card.Controls.Add(MakeLabel(caption, 12, Theme.Charcoal, x, y, FontStyle.Bold))
        ctl.Location = New Point(x, y + 25)
        ctl.Width = w
        card.Controls.Add(ctl)
    End Sub

    Private Function NextRef() As String
        Return If(IsMasuk, "BM", "BK") & "-" & Date.Now.ToString("MMyy") & "-" & (_data.Count + 1).ToString("000")
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
        _lblFooter.Text = If(_data.Count = 0, "Belum ada transaksi.", "Menampilkan " & first & "–" & last & " dari " & _data.Count & " data")
        _lblPage.Text = (_page + 1) & " / " & PageCount()
    End Sub

    ' ---------- tabel ----------
    Private Sub GridMouseMove(sender As Object, e As DataGridViewCellMouseEventArgs)
        _grid.Cursor = If(e.RowIndex >= 0 AndAlso e.ColumnIndex = AksiCol, Cursors.Hand, Cursors.Default)
    End Sub

    Private Sub GridMouseClick(sender As Object, e As DataGridViewCellMouseEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex <> AksiCol Then Return
        Dim r = _data(_page * PageSize + e.RowIndex)
        Dim msg = "No. Transaksi : " & r.No & vbCrLf &
                  "Tanggal       : " & r.Tanggal.ToString("dd MMM yyyy", _id) & vbCrLf &
                  "Barang        : " & r.Barang & vbCrLf &
                  If(IsMasuk, "Supplier      : ", "Tujuan        : ") & r.Pihak & vbCrLf &
                  "Jumlah        : " & r.Jumlah & vbCrLf &
                  "Petugas       : " & r.Petugas
        If r.Catatan <> "" Then msg &= vbCrLf & "Catatan       : " & r.Catatan
        MessageBox.Show(msg, "Detail transaksi", MessageBoxButtons.OK, MessageBoxIcon.Information)
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

    Private Async Sub SaveTrx(sender As Object, e As EventArgs)
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

        Dim notes = _txtCatatan.Inner.Text.Trim()
        Dim payload As New Dictionary(Of String, Object) From {
            {"ref_no", _txtRef.Inner.Text.Trim()},
            {"asset_id", _assetIds(_cbBarang.SelectedItem)},
            {"type", If(IsMasuk, "masuk", "keluar")},
            {"quantity", qty},
            {"trx_date", _dtTanggal.Picker.Value.ToString("yyyy-MM-dd")},
            {"notes", If(notes = "", Nothing, notes)}
        }
        If Not String.IsNullOrEmpty(SupabaseClient.CurrentUserId) Then
            payload("user_id") = SupabaseClient.CurrentUserId
        End If
        If IsMasuk Then
            payload("supplier_id") = _supplierIds(_cbPihak.SelectedItem)
        Else
            payload("destination") = _cbPihak.SelectedItem
        End If

        _btnSave.Enabled = False
        Try
            Await SupabaseClient.PostJsonAsync("stock_transactions", payload)
            MessageBox.Show(If(IsMasuk, "Barang masuk", "Barang keluar") & " berhasil disimpan ke Supabase dan stok otomatis diperbarui!", "Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information)
            ResetForm(sender, e)
            Await LoadAllAsync()
        Catch ex As Exception
            MessageBox.Show("Gagal menyimpan transaksi ke Supabase:" & vbCrLf & ex.Message, "Error Supabase", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            _btnSave.Enabled = True
        End Try
    End Sub
End Class
