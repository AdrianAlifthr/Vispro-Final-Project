''' <summary>Satu baris data aset (nanti diganti dari tabel `assets` di Supabase).</summary>
Public Class AssetRow
    Public Property Kode As String
    Public Property Nama As String
    Public Property Kategori As String
    Public Property Lokasi As String
    Public Property Jumlah As Integer
    Public Property Kondisi As String
    Public Property Status As String

    Public Sub New(kode As String, nama As String, kategori As String, lokasi As String, jumlah As Integer, kondisi As String, status As String)
        Me.Kode = kode : Me.Nama = nama : Me.Kategori = kategori : Me.Lokasi = lokasi
        Me.Jumlah = jumlah : Me.Kondisi = kondisi : Me.Status = status
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
    Private ReadOnly _lblFooter As Label
    Private Const AksiCol As Integer = 7
    Private Const StatusCol As Integer = 6

    Public Sub New()
        BackColor = Theme.OffWhite
        DoubleBuffered = True
        LoadSampleData()

        ' ===== Judul + tombol =====
        Controls.Add(MakeLabel("Data Aset / Barang", 26, Theme.Charcoal, 24, 22, FontStyle.Bold))
        Controls.Add(MakeLabel("Kelola seluruh aset dan persediaan di satu tempat.", 14, Theme.Grey, 24, 66))

        Dim btnAdd As New AppButton() With {.Text = "+ Tambah Aset", .Location = New Point(816, 28), .Size = New Size(160, 36), .BackColor = Theme.OffWhite}
        AddHandler btnAdd.Click, AddressOf OnAddClick
        Controls.Add(btnAdd)
        Dim btnExport As New AppButton() With {.Text = "Export", .Kind = ButtonKind.Outline, .Location = New Point(988, 28), .Size = New Size(114, 36), .BackColor = Theme.OffWhite}
        AddHandler btnExport.Click, AddressOf OnExportClick
        Controls.Add(btnExport)

        ' ===== Kartu filter (24,104 | 1078 x 84) =====
        Dim filterCard As New RoundedPanel() With {.Location = New Point(24, 104), .Size = New Size(1078, 84), .Radius = 8}
        _txtSearch = New AppTextBox() With {.Location = New Point(16, 24), .Width = 318, .PlaceholderText = "Cari kode atau nama aset..."}
        _cbKategori = New AppComboBox() With {.Location = New Point(350, 24), .Width = 214}
        _cbLokasi = New AppComboBox() With {.Location = New Point(580, 24), .Width = 214}
        _cbStatus = New AppComboBox() With {.Location = New Point(810, 24), .Width = 252}
        _cbKategori.SetItems("Semua kategori", "Elektronik", "Furnitur", "Peralatan", "ATK")
        _cbLokasi.SetItems("Semua lokasi", "Ruang IT", "Ruang Rapat", "Gudang A", "Ruang Server", "Administrasi", "Ruang Keuangan")
        _cbStatus.SetItems("Semua status", "Tersedia", "Dipinjam", "Maintenance", "Rusak", "Stok Menipis")
        _cbKategori.SelectedIndex = 0 : _cbLokasi.SelectedIndex = 0 : _cbStatus.SelectedIndex = 0
        For Each c As Control In New Control() {_txtSearch, _cbKategori, _cbLokasi, _cbStatus}
            filterCard.Controls.Add(c)
        Next
        Controls.Add(filterCard)

        ' ===== Judul tabel =====
        Controls.Add(MakeLabel("Daftar aset", 18, Theme.Charcoal, 24, 214, FontStyle.Bold))
        Controls.Add(MakeLabel("1.248 aset terdaftar", 12, Theme.Grey, 920, 218))

        ' ===== Tabel (24,252 | 1078 x 350) =====
        Dim wrap As New RoundedPanel() With {.Location = New Point(24, 252), .Size = New Size(1078, 350), .Radius = 6}
        _grid = New DataGridView() With {.Location = New Point(1, 1), .Size = New Size(1076, 348)}
        StyleGrid(_grid, 44)
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

        ' ===== Footer + paginasi =====
        _lblFooter = MakeLabel("", 12, Theme.Grey, 24, 628)
        Controls.Add(_lblFooter)
        ' TODO (Supabase): paginasi sungguhan (range/limit). Sementara tombol hanya tampilan.
        Controls.Add(New AppButton() With {.Text = "‹ Sebelumnya", .Kind = ButtonKind.Outline, .Location = New Point(756, 620), .Size = New Size(132, 36), .BackColor = Theme.OffWhite})
        Controls.Add(New AppButton() With {.Text = "1", .Kind = ButtonKind.Primary, .Location = New Point(900, 620), .Size = New Size(36, 36), .BackColor = Theme.OffWhite})
        Controls.Add(New AppButton() With {.Text = "2", .Kind = ButtonKind.Outline, .Location = New Point(944, 620), .Size = New Size(36, 36), .BackColor = Theme.OffWhite})
        Controls.Add(New AppButton() With {.Text = "Berikutnya ›", .Kind = ButtonKind.Outline, .Location = New Point(992, 620), .Size = New Size(110, 36), .BackColor = Theme.OffWhite})

        AddHandler _txtSearch.Inner.TextChanged, AddressOf OnFilterChanged
        AddHandler _cbKategori.SelectedIndexChanged, AddressOf OnFilterChanged
        AddHandler _cbLokasi.SelectedIndexChanged, AddressOf OnFilterChanged
        AddHandler _cbStatus.SelectedIndexChanged, AddressOf OnFilterChanged

        ApplyFilter()
    End Sub

    ' ---------- data ----------
    Private Sub LoadSampleData()
        ' TODO (Supabase): ganti dengan SELECT dari tabel assets JOIN categories/locations.
        _all.Add(New AssetRow("AST-001", "Laptop Lenovo ThinkPad", "Elektronik", "Ruang IT", 12, "Baik", "Tersedia"))
        _all.Add(New AssetRow("AST-002", "Proyektor Epson", "Elektronik", "Ruang Rapat", 3, "Baik", "Dipinjam"))
        _all.Add(New AssetRow("AST-003", "Kursi Kantor", "Furnitur", "Gudang A", 48, "Baik", "Tersedia"))
        _all.Add(New AssetRow("AST-004", "AC Daikin 1 PK", "Elektronik", "Ruang Server", 2, "Rusak ringan", "Maintenance"))
        _all.Add(New AssetRow("AST-005", "Printer Canon", "Elektronik", "Administrasi", 1, "Rusak berat", "Rusak"))
        _all.Add(New AssetRow("BRG-006", "Kertas A4 80 gsm", "ATK", "Gudang A", 8, "Baik", "Stok Menipis"))
        _all.Add(New AssetRow("AST-007", "Meja Kerja", "Furnitur", "Ruang Keuangan", 24, "Baik", "Tersedia"))
    End Sub

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
        _lblFooter.Text = If(_shown.Count = 0, "Tidak ada data yang cocok.", "Menampilkan 1–" & _shown.Count & " dari 1.248 aset")
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

    Private Sub OnExportClick(sender As Object, e As EventArgs)
        ' TODO: ekspor ke CSV/Excel.
        MessageBox.Show("Fitur Export akan dibuat di layar Laporan.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub DeleteRow(row As AssetRow)
        Dim msg = row.Nama & " (" & row.Kode & ") akan dihapus." & vbCrLf & "Tindakan ini tidak dapat dibatalkan."
        If ConfirmDialog.Ask(FindForm(), "Hapus aset ini?", msg, "Hapus Aset") Then
            ' TODO (Supabase): DELETE FROM assets WHERE asset_code = row.Kode
            _all.Remove(row)
            ApplyFilter()
        End If
    End Sub
End Class
