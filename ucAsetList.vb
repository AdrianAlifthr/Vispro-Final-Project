Imports System
Imports System.Text.Json
Imports System.Threading.Tasks

''' <summary>Satu baris data aset dari view `v_assets` di Supabase.</summary>
Public Class AssetRow
    Public Property Id As String
    Public Property Kode As String
    Public Property Nama As String
    Public Property Kategori As String
    Public Property Lokasi As String
    Public Property Jumlah As Integer
    Public Property Kondisi As String
    Public Property Status As String
    Public Property Supplier As String
    Public Property TanggalPerolehan As String
    Public Property Harga As Decimal
    Public Property Keterangan As String

    Public Sub New(kode As String, nama As String, kategori As String, lokasi As String, jumlah As Integer, kondisi As String, status As String,
                   Optional id As String = "", Optional supplier As String = "", Optional tanggalPerolehan As String = "",
                   Optional harga As Decimal = 0D, Optional keterangan As String = "")
        Me.Id = id
        Me.Kode = kode : Me.Nama = nama : Me.Kategori = kategori : Me.Lokasi = lokasi
        Me.Jumlah = jumlah : Me.Kondisi = kondisi : Me.Status = status
        Me.Supplier = supplier : Me.TanggalPerolehan = tanggalPerolehan
        Me.Harga = harga : Me.Keterangan = keterangan
    End Sub
End Class

Public Class ucAsetList
    Inherits UserControl

    Private ReadOnly _all As New List(Of AssetRow)
    Private ReadOnly _shown As New List(Of AssetRow)
    Private ReadOnly _grid As DataGridView
    Private ReadOnly _txtSearch As AppTextBox
    Private ReadOnly _cbKategori As AppComboBox
    Private ReadOnly _cbLokasi As AppComboBox
    Private ReadOnly _cbStatus As AppComboBox
    Private ReadOnly _lblTotal As Label
    Private ReadOnly _lblFooter As Label
    Private ReadOnly _refreshTimer As Timer
    Private _isLoading As Boolean = False
    Private Const AksiCol As Integer = 7
    Private Const StatusCol As Integer = 6

    Public Sub New()
        BackColor = Theme.OffWhite
        DoubleBuffered = True

        ' ===== Judul + tombol =====
        Controls.Add(MakeLabel("Data Aset / Barang", 26, Theme.Charcoal, 24, 22, FontStyle.Bold))
        Controls.Add(MakeLabel("Kelola seluruh aset dan persediaan di satu tempat.", 14, Theme.Grey, 24, 66))

        Dim btnAdd As New AppButton() With {.Text = "+ Tambah Aset", .Location = New Point(816, 28), .Size = New Size(160, 36), .BackColor = Theme.OffWhite}
        AddHandler btnAdd.Click, AddressOf OnAddClick
        Controls.Add(btnAdd)
        Dim btnRefresh As New AppButton() With {.Text = "Refresh", .Kind = ButtonKind.Outline, .Location = New Point(988, 28), .Size = New Size(114, 36), .BackColor = Theme.OffWhite}
        AddHandler btnRefresh.Click, AddressOf OnRefreshClick
        Controls.Add(btnRefresh)

        ' ===== Kartu filter (24,104 | 1078 x 84) =====
        Dim filterCard As New RoundedPanel() With {.Location = New Point(24, 104), .Size = New Size(1078, 84), .Radius = 8}
        _txtSearch = New AppTextBox() With {.Location = New Point(16, 24), .Width = 318, .PlaceholderText = "Cari kode atau nama aset..."}
        _cbKategori = New AppComboBox() With {.Location = New Point(350, 24), .Width = 214}
        _cbLokasi = New AppComboBox() With {.Location = New Point(580, 24), .Width = 214}
        _cbStatus = New AppComboBox() With {.Location = New Point(810, 24), .Width = 252}
        _cbKategori.SetItems("Semua kategori")
        _cbLokasi.SetItems("Semua lokasi")
        _cbStatus.SetItems("Semua status", "Tersedia", "Dipinjam", "Maintenance", "Rusak", "Stok Menipis")
        _cbKategori.SelectedIndex = 0 : _cbLokasi.SelectedIndex = 0 : _cbStatus.SelectedIndex = 0
        For Each c As Control In New Control() {_txtSearch, _cbKategori, _cbLokasi, _cbStatus}
            filterCard.Controls.Add(c)
        Next
        Controls.Add(filterCard)

        ' ===== Judul tabel =====
        Controls.Add(MakeLabel("Daftar aset", 18, Theme.Charcoal, 24, 214, FontStyle.Bold))
        _lblTotal = MakeLabel("Memuat data...", 12, Theme.Grey, 920, 218)
        Controls.Add(_lblTotal)

        ' ===== Tabel (24,252 | 1078 x 350) =====
        Dim wrap As New RoundedPanel() With {.Location = New Point(24, 252), .Size = New Size(1078, 350), .Radius = 6}
        _grid = New DataGridView() With {.Location = New Point(1, 1), .Size = New Size(1076, 348)}
        StyleGrid(_grid, 44)
        _grid.ScrollBars = ScrollBars.Vertical
        Dim heads As String() = {"Kode", "Nama", "Kategori", "Lokasi", "Jumlah", "Kondisi", "Status", "Aksi"}
        Dim widths As Integer() = {94, 212, 104, 134, 62, 108, 130, 232}
        For i = 0 To heads.Length - 1
            _grid.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = heads(i), .Width = widths(i), .SortMode = DataGridViewColumnSortMode.NotSortable})
        Next
        _grid.Columns(0).DefaultCellStyle.ForeColor = Theme.Charcoal
        _grid.Columns(0).DefaultCellStyle.SelectionForeColor = Theme.Charcoal
        AddHandler _grid.CellPainting, AddressOf GridCellPainting
        AddHandler _grid.CellMouseMove, AddressOf GridMouseMove
        AddHandler _grid.CellMouseClick, AddressOf GridMouseClick
        wrap.Controls.Add(_grid)
        Controls.Add(wrap)

        ' ===== Footer =====
        _lblFooter = MakeLabel("Memuat data dari Supabase...", 12, Theme.Grey, 24, 628)
        Controls.Add(_lblFooter)

        AddHandler _txtSearch.Inner.TextChanged, AddressOf OnFilterChanged
        AddHandler _cbKategori.SelectedIndexChanged, AddressOf OnFilterChanged
        AddHandler _cbLokasi.SelectedIndexChanged, AddressOf OnFilterChanged
        AddHandler _cbStatus.SelectedIndexChanged, AddressOf OnFilterChanged

        ' Auto-refresh setiap 15 detik agar data tetap sinkron secara real-time
        _refreshTimer = New Timer() With {.Interval = 15000}
        AddHandler _refreshTimer.Tick, AddressOf OnRefreshTick
        AddHandler Me.Load, AddressOf OnControlLoad
        AddHandler Me.Disposed, Sub(s, e) _refreshTimer.Stop()
    End Sub

    ' ---------- data dari Supabase ----------
    Private Async Sub OnControlLoad(sender As Object, e As EventArgs)
        Await LoadFromSupabaseAsync(False)
        _refreshTimer.Start()
    End Sub

    Private Async Sub OnRefreshTick(sender As Object, e As EventArgs)
        If Not IsDisposed AndAlso Visible Then
            Await LoadFromSupabaseAsync(True)
        End If
    End Sub

    Private Async Sub OnRefreshClick(sender As Object, e As EventArgs)
        Await LoadFromSupabaseAsync(False)
    End Sub

    Private Async Function LoadFromSupabaseAsync(silent As Boolean) As Task
        If _isLoading Then Return
        _isLoading = True
        If Not silent Then
            _lblTotal.Text = "Memuat..."
            _lblFooter.Text = "Memuat data dari Supabase..."
        End If
        Try
            Dim prevKat = _cbKategori.SelectedItem
            Dim prevLok = _cbLokasi.SelectedItem

            Dim raw = Await SupabaseClient.GetJsonAsync("/rest/v1/v_assets?order=asset_code.asc")
            _all.Clear()

            Using doc = JsonDocument.Parse(raw)
                For Each el In doc.RootElement.EnumerateArray()
                    Dim id = SupabaseClient.GetStr(el, "id")
                    Dim kode = SupabaseClient.GetStr(el, "asset_code")
                    Dim nama = SupabaseClient.GetStr(el, "name")
                    Dim kategori = SupabaseClient.GetStr(el, "category_name")
                    Dim lokasi = SupabaseClient.GetStr(el, "location_name")
                    Dim supplier = SupabaseClient.GetStr(el, "supplier_name")
                    Dim jumlah = SupabaseClient.GetInt(el, "quantity")
                    Dim harga = SupabaseClient.GetDec(el, "price")
                    Dim tgl = SupabaseClient.GetStr(el, "acquired_date")
                    Dim ket = SupabaseClient.GetStr(el, "description")
                    Dim condDb = SupabaseClient.GetStr(el, "condition")
                    Dim statusDb = SupabaseClient.GetStr(el, "status")
                    Dim lowStock = SupabaseClient.GetBool(el, "low_stock")

                    Dim kondisiUi = FormatCondition(condDb)
                    Dim statusUi = FormatStatus(statusDb, lowStock)

                    _all.Add(New AssetRow(kode, nama, kategori, lokasi, jumlah, kondisiUi, statusUi, id, supplier, tgl, harga, ket))
                Next
            End Using

            Dim kats = New List(Of String) From {"Semua kategori"}
            kats.AddRange(_all.Select(Function(x) x.Kategori).Where(Function(x) x <> "").Distinct().OrderBy(Function(x) x))
            _cbKategori.SetItems(kats.ToArray())
            If prevKat IsNot Nothing AndAlso kats.Contains(prevKat) Then
                _cbKategori.SelectByText(prevKat)
            Else
                _cbKategori.SelectedIndex = 0
            End If

            Dim loks = New List(Of String) From {"Semua lokasi"}
            loks.AddRange(_all.Select(Function(x) x.Lokasi).Where(Function(x) x <> "").Distinct().OrderBy(Function(x) x))
            _cbLokasi.SetItems(loks.ToArray())
            If prevLok IsNot Nothing AndAlso loks.Contains(prevLok) Then
                _cbLokasi.SelectByText(prevLok)
            Else
                _cbLokasi.SelectedIndex = 0
            End If

            _lblTotal.Text = _all.Count & " aset terdaftar"
            ApplyFilter()
        Catch ex As Exception
            If Not silent Then
                _lblTotal.Text = "Gagal memuat"
                _lblFooter.Text = "Gagal mengambil data dari Supabase."
                MessageBox.Show("Gagal memuat data aset dari Supabase:" & vbCrLf & ex.Message, "Error Supabase", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Finally
            _isLoading = False
        End Try
    End Function

    Private Shared Function FormatCondition(dbVal As String) As String
        Select Case dbVal
            Case "baik" : Return "Baik"
            Case "rusak_ringan" : Return "Rusak ringan"
            Case "rusak_berat" : Return "Rusak berat"
            Case Else : Return dbVal
        End Select
    End Function

    Private Shared Function FormatStatus(dbVal As String, lowStock As Boolean) As String
        If dbVal = "tersedia" AndAlso lowStock Then Return "Stok Menipis"
        Select Case dbVal
            Case "tersedia" : Return "Tersedia"
            Case "dipinjam" : Return "Dipinjam"
            Case "maintenance" : Return "Maintenance"
            Case "rusak" : Return "Rusak"
            Case Else : Return dbVal
        End Select
    End Function

    Private Sub OnFilterChanged(sender As Object, e As EventArgs)
        ApplyFilter()
    End Sub

    Private Sub ApplyFilter()
        If _grid Is Nothing OrElse _cbStatus Is Nothing OrElse _cbStatus.SelectedItem Is Nothing Then Return
        Dim q = _txtSearch.Inner.Text.Trim().ToLower()
        Dim kat = If(_cbKategori.SelectedIndex > 0, _cbKategori.SelectedItem, Nothing)
        Dim lok = If(_cbLokasi.SelectedIndex > 0, _cbLokasi.SelectedItem, Nothing)
        Dim sta = If(_cbStatus.SelectedIndex > 0, _cbStatus.SelectedItem, Nothing)

        _shown.Clear()
        _grid.Rows.Clear()
        For Each a In _all
            If q <> "" AndAlso Not (a.Kode.ToLower().Contains(q) OrElse a.Nama.ToLower().Contains(q)) Then Continue For
            If kat IsNot Nothing AndAlso a.Kategori <> kat Then Continue For
            If lok IsNot Nothing AndAlso a.Lokasi <> lok Then Continue For
            If sta IsNot Nothing AndAlso a.Status <> sta Then Continue For
            _shown.Add(a)
            _grid.Rows.Add(a.Kode, a.Nama, a.Kategori, a.Lokasi, a.Jumlah.ToString(), a.Kondisi, a.Status, "")
        Next
        _grid.ClearSelection()
        _lblFooter.Text = If(_shown.Count = 0, "Tidak ada data yang cocok.", "Menampilkan 1–" & _shown.Count & " dari " & _all.Count & " aset")
    End Sub

    ' ---------- tabel: badge status & tautan aksi ----------
    Private Shared ReadOnly ActionNames As String() = {"Edit", "Hapus", "Detail"}

    Private Function ActionRects(cell As Size) As Rectangle()
        Dim f = AppFont(12)
        Dim flags = TextFormatFlags.NoPadding
        Dim sepW = TextRenderer.MeasureText(" · ", f, Size.Empty, flags).Width
        Dim x = 13
        Dim res(2) As Rectangle
        For i = 0 To 2
            Dim w = TextRenderer.MeasureText(ActionNames(i), f, Size.Empty, flags).Width
            res(i) = New Rectangle(x, 0, w, cell.Height)
            x += w + sepW
        Next
        Return res
    End Function

    Private Sub GridCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 Then Return
        If e.ColumnIndex = StatusCol Then
            e.PaintBackground(e.CellBounds, False)
            DrawBadge(e.Graphics, Convert.ToString(e.Value), e.CellBounds.X + 12, e.CellBounds.Y + e.CellBounds.Height \ 2)
            e.Handled = True
        ElseIf e.ColumnIndex = AksiCol Then
            e.PaintBackground(e.CellBounds, False)
            Dim f = AppFont(12)
            Dim flags = TextFormatFlags.NoPadding Or TextFormatFlags.VerticalCenter Or TextFormatFlags.Left
            Dim rects = ActionRects(e.CellBounds.Size)
            For i = 0 To 2
                Dim r = rects(i)
                r.Offset(e.CellBounds.X, e.CellBounds.Y)
                TextRenderer.DrawText(e.Graphics, ActionNames(i), f, r, Theme.Grey, flags)
                If i < 2 Then
                    Dim sep As New Rectangle(r.Right, r.Y, 14, r.Height)
                    TextRenderer.DrawText(e.Graphics, " · ", f, sep, Theme.Grey, flags)
                End If
            Next
            e.Handled = True
        End If
    End Sub

    Private Function HitAction(colIndex As Integer, rowIndex As Integer, pt As Point) As Integer
        If rowIndex < 0 OrElse colIndex <> AksiCol Then Return -1
        Dim rects = ActionRects(_grid.GetCellDisplayRectangle(colIndex, rowIndex, False).Size)
        For i = 0 To 2
            If rects(i).Contains(pt) Then Return i
        Next
        Return -1
    End Function

    Private Sub GridMouseMove(sender As Object, e As DataGridViewCellMouseEventArgs)
        _grid.Cursor = If(HitAction(e.ColumnIndex, e.RowIndex, e.Location) >= 0, Cursors.Hand, Cursors.Default)
    End Sub

    Private Sub GridMouseClick(sender As Object, e As DataGridViewCellMouseEventArgs)
        Dim act = HitAction(e.ColumnIndex, e.RowIndex, e.Location)
        If act < 0 Then Return
        Dim row = _shown(e.RowIndex)
        Select Case act
            Case 0 : Host()?.ShowPage(New ucAsetForm(row), 1)
            Case 1 : DeleteRow(row)
            Case 2 : Host()?.ShowPage(New ucAsetDetail(row), 1)
        End Select
    End Sub

    ' ---------- aksi ----------
    Private Function Host() As FormMain
        Return TryCast(FindForm(), FormMain)
    End Function

    Private Sub OnAddClick(sender As Object, e As EventArgs)
        Host()?.ShowPage(New ucAsetForm(), 1)
    End Sub

    Private Async Sub DeleteRow(row As AssetRow)
        Dim msg = row.Nama & " (" & row.Kode & ") akan dihapus dari Supabase." & vbCrLf & "Tindakan ini tidak dapat dibatalkan."
        If Not ConfirmDialog.Ask(FindForm(), "Hapus aset ini?", msg, "Hapus Aset") Then Return

        Try
            Await SupabaseClient.DeleteAsync("/rest/v1/assets?id=eq." & row.Id)
            _all.Remove(row)
            _lblTotal.Text = _all.Count & " aset terdaftar"
            ApplyFilter()
        Catch ex As Exception
            MessageBox.Show("Gagal menghapus aset (pastikan role Anda admin dan aset tidak dipakai transaksi):" & vbCrLf & ex.Message,
                            "Gagal Hapus", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class