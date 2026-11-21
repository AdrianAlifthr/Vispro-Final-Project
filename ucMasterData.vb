Imports System
Imports System.Globalization
Imports System.Text.Json
Imports System.Threading.Tasks

''' <summary>Strip tab "Kategori | Lokasi | Supplier" (tiap tab 160px) sesuai desain.</summary>
Public Class MasterTabs
    Inherits Control

    Private Shared ReadOnly Captions As String() = {"Kategori", "Lokasi", "Supplier"}
    Private _selected As Integer = 0
    Public Event SelectedChanged As EventHandler

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        Cursor = Cursors.Hand
        Size = New Size(1078, 44)
    End Sub

    Public Property SelectedIndex As Integer
        Get
            Return _selected
        End Get
        Set(value As Integer)
            If value = _selected Then Return
            _selected = value
            Invalidate()
            RaiseEvent SelectedChanged(Me, EventArgs.Empty)
        End Set
    End Property

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        Dim i = e.X \ 160
        If i >= 0 AndAlso i < Captions.Length Then SelectedIndex = i
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.Clear(If(Parent IsNot Nothing, Parent.BackColor, Theme.OffWhite))
        FillRounded(g, New Rectangle(0, 0, Width - 1, Height - 1), 6, Theme.OffWhite, Theme.Beige)
        For i = 0 To Captions.Length - 1
            Dim r As New Rectangle(i * 160 + 1, 1, 159, Height - 3)
            Dim active = (i = _selected)
            If active Then
                FillRounded(g, r, 5, Theme.Beige)
                Using b As New SolidBrush(Theme.Orange)
                    g.FillRectangle(b, r.X + 16, Height - 5, 128 - 1, 2)
                End Using
            End If
            DrawText(g, Captions(i), AppFont(14, If(active, FontStyle.Bold, FontStyle.Regular)), If(active, Theme.Charcoal, Theme.Grey), r, StringAlignment.Center)
        Next
    End Sub
End Class

Public Class MasterField
    Public Property Caption As String
    Public Property Placeholder As String
    Public Property Idx As Integer

    Public Sub New(caption As String, placeholder As String, idx As Integer)
        Me.Caption = caption : Me.Placeholder = placeholder : Me.Idx = idx
    End Sub
End Class

''' <summary>Konfigurasi satu tab Master Data (Kategori / Lokasi / Supplier).</summary>
Public Class MasterConfig
    Public Property Entity As String          ' "kategori"
    Public Property TableName As String       ' "categories"
    Public Property ViewName As String        ' "v_category_totals"
    Public Property Prefix As String          ' "KAT"
    Public Property Headers As String()
    Public Property Widths As Integer()
    Public Property GridIdx As Integer()
    Public Property Fields As New List(Of MasterField)
    Public Property HasKodeField As Boolean
    Public Property TotalIdx As Integer = -1
    Public Property RowLen As Integer         ' Indeks terakhir (RowLen) menyimpan UUID baris dari Supabase
    Public Property Rows As New List(Of String())
End Class

''' <summary>Panel satu tab: daftar di kiri, form tambah/edit di kanan (terhubung Supabase).</summary>
Public Class MasterPane
    Inherits Panel

    Private ReadOnly _cfg As MasterConfig
    Private ReadOnly _editors As New List(Of AppTextBox)
    Private _view As New List(Of String())
    Private _editing As String() = Nothing

    Private ReadOnly _grid As DataGridView
    Private ReadOnly _txtSearch As AppTextBox
    Private ReadOnly _lblCount As Label
    Private ReadOnly _lblFormTitle As Label
    Private ReadOnly _lblFormSub As Label
    Private ReadOnly _btnSave As AppButton

    Public Sub New(cfg As MasterConfig)
        _cfg = cfg
        Size = New Size(1126, 544)
        BackColor = Theme.OffWhite
        Dim ent = cfg.Entity

        ' ===== Kartu kiri (24,8 | 686 x 464) =====
        Dim pnlLeft As New RoundedPanel() With {.Location = New Point(24, 8), .Size = New Size(686, 464), .Radius = 8}
        pnlLeft.Controls.Add(MakeLabel("Daftar " & ent, 18, Theme.Charcoal, 16, 19, FontStyle.Bold))
        _lblCount = MakeLabel("Memuat...", 12, Theme.Grey, 16, 47)
        pnlLeft.Controls.Add(_lblCount)
        _txtSearch = New AppTextBox() With {.Location = New Point(432, 20), .Width = 238, .PlaceholderText = "Cari " & ent & "..."}
        pnlLeft.Controls.Add(_txtSearch)

        _grid = New DataGridView() With {.Location = New Point(16, 80), .Size = New Size(654, 376)}
        StyleGrid(_grid, 40)
        _grid.ScrollBars = ScrollBars.Vertical
        _grid.ColumnHeadersDefaultCellStyle.Padding = New Padding(16, 0, 0, 0)
        _grid.DefaultCellStyle.Padding = New Padding(16, 0, 0, 0)
        _grid.DefaultCellStyle.Font = AppFont(14)
        For i = 0 To cfg.Headers.Length - 1
            _grid.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = cfg.Headers(i), .Width = cfg.Widths(i), .SortMode = DataGridViewColumnSortMode.NotSortable})
        Next
        _grid.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = "Aksi", .Width = cfg.Widths(cfg.Headers.Length), .SortMode = DataGridViewColumnSortMode.NotSortable})
        AddHandler _grid.CellPainting, AddressOf GridCellPainting
        AddHandler _grid.CellMouseMove, AddressOf GridMouseMove
        AddHandler _grid.CellMouseClick, AddressOf GridMouseClick
        pnlLeft.Controls.Add(_grid)
        Controls.Add(pnlLeft)

        ' ===== Kartu kanan (734,8 | 368 x 464) =====
        Dim pnlRight As New RoundedPanel() With {.Location = New Point(734, 8), .Size = New Size(368, 464), .Radius = 8}
        _lblFormTitle = MakeLabel("", 20, Theme.Charcoal, 24, 22, FontStyle.Bold)
        _lblFormSub = MakeLabel("", 12, Theme.Grey, 24, 53)
        pnlRight.Controls.Add(_lblFormTitle)
        pnlRight.Controls.Add(_lblFormSub)
        For i = 0 To cfg.Fields.Count - 1
            Dim f = cfg.Fields(i)
            Dim y = 88 + i * 80
            pnlRight.Controls.Add(MakeLabel(f.Caption, 12, Theme.Charcoal, 24, y, FontStyle.Bold))
            Dim tb As New AppTextBox() With {.Location = New Point(24, y + 26), .Width = 320, .PlaceholderText = f.Placeholder}
            _editors.Add(tb)
            pnlRight.Controls.Add(tb)
        Next
        _btnSave = New AppButton() With {.Text = "Simpan", .Kind = ButtonKind.Primary, .Location = New Point(24, 404), .Size = New Size(152, 36), .BackColor = Theme.OffWhite}
        AddHandler _btnSave.Click, AddressOf SaveRow
        pnlRight.Controls.Add(_btnSave)
        Dim btnCancel As New AppButton() With {.Text = "Batal", .Kind = ButtonKind.Outline, .Location = New Point(192, 404), .Size = New Size(152, 36), .BackColor = Theme.OffWhite}
        AddHandler btnCancel.Click, AddressOf CancelEdit
        pnlRight.Controls.Add(btnCancel)
        Controls.Add(pnlRight)

        Controls.Add(MakeLabel("Data master terhubung langsung ke Supabase dan dipakai di seluruh transaksi AsetKu.", 12, Theme.Grey, 24, 492))

        AddHandler _txtSearch.Inner.TextChanged, AddressOf OnSearchChanged
        ResetForm()
    End Sub

    Public Async Function LoadFromSupabaseAsync() As Task
        _lblCount.Text = "Memuat..."
        Try
            Dim raw = Await SupabaseClient.GetJsonAsync("/rest/v1/" & _cfg.ViewName & "?order=code.asc")
            _cfg.Rows.Clear()
            Using doc = JsonDocument.Parse(raw)
                For Each el In doc.RootElement.EnumerateArray()
                    Dim id = SupabaseClient.GetStr(el, "id")
                    Dim code = SupabaseClient.GetStr(el, "code")
                    Dim name = SupabaseClient.GetStr(el, "name")
                    Select Case _cfg.TableName
                        Case "categories"
                            Dim total = SupabaseClient.GetInt(el, "total_assets").ToString()
                            Dim desc = SupabaseClient.GetStr(el, "description")
                            _cfg.Rows.Add({code, name, total, desc, id})
                        Case "locations"
                            Dim total = SupabaseClient.GetInt(el, "total_assets").ToString()
                            Dim pic = SupabaseClient.GetStr(el, "person_in_charge")
                            _cfg.Rows.Add({code, name, total, pic, id})
                        Case "suppliers"
                            Dim phone = SupabaseClient.GetStr(el, "phone")
                            Dim email = SupabaseClient.GetStr(el, "email")
                            Dim addr = SupabaseClient.GetStr(el, "address")
                            _cfg.Rows.Add({code, name, phone, email, addr, id})
                    End Select
                Next
            End Using
            ResetForm()
            ApplyFilter()
        Catch ex As Exception
            _lblCount.Text = "Gagal memuat"
            MessageBox.Show("Gagal memuat data " & _cfg.Entity & " dari Supabase:" & vbCrLf & ex.Message, "Error Supabase", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

    ' ---------- tabel ----------
    Private Sub OnSearchChanged(sender As Object, e As EventArgs)
        ApplyFilter()
    End Sub

    Private Sub ApplyFilter()
        Dim q = _txtSearch.Inner.Text.Trim().ToLower()
        _view = _cfg.Rows.Where(Function(r) q = "" OrElse _cfg.GridIdx.Any(Function(ix) r(ix).ToLower().Contains(q))).ToList()
        _grid.Rows.Clear()
        For Each r In _view
            Dim cells As New List(Of Object)
            For Each ix In _cfg.GridIdx
                cells.Add(r(ix))
            Next
            cells.Add("")
            _grid.Rows.Add(cells.ToArray())
        Next
        _grid.ClearSelection()
        _lblCount.Text = _cfg.Rows.Count & " " & _cfg.Entity
    End Sub

    Private Shared ReadOnly ActionNames As String() = {"Edit", "Hapus"}

    Private Function ActionRects(cell As Size) As Rectangle()
        Dim f = AppFont(13, FontStyle.Bold)
        Dim flags = TextFormatFlags.NoPadding
        Dim sepW = TextRenderer.MeasureText(" · ", f, Size.Empty, flags).Width
        Dim x = 16
        Dim res(1) As Rectangle
        For i = 0 To 1
            Dim w = TextRenderer.MeasureText(ActionNames(i), f, Size.Empty, flags).Width
            res(i) = New Rectangle(x, 0, w, cell.Height)
            x += w + sepW
        Next
        Return res
    End Function

    Private Sub GridCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex <> _cfg.Headers.Length Then Return
        e.PaintBackground(e.CellBounds, False)
        Dim f = AppFont(13, FontStyle.Bold)
        Dim flags = TextFormatFlags.NoPadding Or TextFormatFlags.VerticalCenter Or TextFormatFlags.Left
        Dim rects = ActionRects(e.CellBounds.Size)
        For i = 0 To 1
            Dim r = rects(i)
            r.Offset(e.CellBounds.X, e.CellBounds.Y)
            TextRenderer.DrawText(e.Graphics, ActionNames(i), f, r, Theme.Grey, flags)
            If i = 0 Then
                TextRenderer.DrawText(e.Graphics, " · ", f, New Rectangle(r.Right, r.Y, 16, r.Height), Theme.Grey, flags)
            End If
        Next
        e.Handled = True
    End Sub

    Private Function HitAction(col As Integer, row As Integer, pt As Point) As Integer
        If row < 0 OrElse col <> _cfg.Headers.Length Then Return -1
        Dim rects = ActionRects(_grid.GetCellDisplayRectangle(col, row, False).Size)
        For i = 0 To 1
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
        Dim row = _view(e.RowIndex)
        If act = 0 Then StartEdit(row) Else DeleteRow(row)
    End Sub

    ' ---------- form tambah / edit ----------
    Private Function NextKode() As String
        Dim maxN = 0
        For Each r In _cfg.Rows
            Dim parts = r(0).Split("-"c)
            Dim n As Integer
            If parts.Length = 2 AndAlso Integer.TryParse(parts(1), n) Then maxN = Math.Max(maxN, n)
        Next
        Return _cfg.Prefix & "-" & (maxN + 1).ToString("000")
    End Function

    Private Sub ResetForm()
        _editing = Nothing
        _lblFormTitle.Text = "Tambah " & _cfg.Entity
        _lblFormSub.Text = "Lengkapi data berikut untuk menambah."
        For i = 0 To _cfg.Fields.Count - 1
            _editors(i).Inner.Text = ""
            If _cfg.Fields(i).Idx = 0 Then _editors(i).Inner.Text = NextKode()
        Next
    End Sub

    Private Sub StartEdit(row As String())
        _editing = row
        _lblFormTitle.Text = "Edit " & _cfg.Entity
        _lblFormSub.Text = "Ubah data berikut lalu simpan."
        For i = 0 To _cfg.Fields.Count - 1
            _editors(i).Inner.Text = row(_cfg.Fields(i).Idx)
        Next
    End Sub

    Private Sub CancelEdit(sender As Object, e As EventArgs)
        ResetForm()
    End Sub

    Private Async Sub SaveRow(sender As Object, e As EventArgs)
        Dim values(_cfg.RowLen - 1) As String
        For i = 0 To values.Length - 1
            values(i) = ""
        Next
        If _editing IsNot Nothing Then
            Array.Copy(_editing, values, _cfg.RowLen)
        End If
        For i = 0 To _cfg.Fields.Count - 1
            values(_cfg.Fields(i).Idx) = _editors(i).Inner.Text.Trim()
        Next

        If values(1) = "" Then
            MessageBox.Show("Nama " & _cfg.Entity & " wajib diisi.", "Data belum lengkap", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If _cfg.HasKodeField AndAlso values(0) = "" Then
            MessageBox.Show("Kode " & _cfg.Entity & " wajib diisi.", "Data belum lengkap", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        For i = 0 To _cfg.Fields.Count - 1
            If _cfg.Fields(i).Caption.ToLower().Contains("email") AndAlso values(_cfg.Fields(i).Idx) <> "" AndAlso Not values(_cfg.Fields(i).Idx).Contains("@") Then
                MessageBox.Show("Format alamat email tidak valid.", "Email tidak valid", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
        Next

        Dim payload As New Dictionary(Of String, Object)()
        Select Case _cfg.TableName
            Case "categories"
                payload("code") = values(0)
                payload("name") = values(1)
                payload("description") = If(values(3) = "", Nothing, values(3))
            Case "locations"
                payload("code") = values(0)
                payload("name") = values(1)
                payload("person_in_charge") = If(values(3) = "", Nothing, values(3))
            Case "suppliers"
                payload("name") = values(1)
                payload("phone") = If(values(2) = "", Nothing, values(2))
                payload("email") = If(values(3) = "", Nothing, values(3))
                payload("address") = If(values(4) = "", Nothing, values(4))
        End Select

        _btnSave.Enabled = False
        Try
            If _editing Is Nothing Then
                Await SupabaseClient.PostJsonAsync(_cfg.TableName, payload)
            Else
                Dim dbId = _editing(_cfg.RowLen)
                Await SupabaseClient.PatchJsonAsync($"/rest/v1/{_cfg.TableName}?id=eq.{dbId}", payload)
            End If
            Await LoadFromSupabaseAsync()
        Catch ex As Exception
            MessageBox.Show("Gagal menyimpan data " & _cfg.Entity & " ke Supabase (pastikan role Anda admin):" & vbCrLf & ex.Message, "Error Supabase", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            _btnSave.Enabled = True
        End Try
    End Sub

    Private Async Sub DeleteRow(row As String())
        If _cfg.TotalIdx >= 0 Then
            Dim total As Integer
            If Integer.TryParse(row(_cfg.TotalIdx), total) AndAlso total > 0 Then
                MessageBox.Show(row(1) & " masih dipakai oleh " & total & " aset, jadi tidak dapat dihapus." & vbCrLf & "Pindahkan asetnya ke " & _cfg.Entity & " lain terlebih dahulu.", "Tidak dapat dihapus", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
        End If
        Dim msg = row(1) & " (" & row(0) & ") akan dihapus dari Supabase." & vbCrLf & "Tindakan ini tidak dapat dibatalkan."
        If Not ConfirmDialog.Ask(FindForm(), "Hapus " & _cfg.Entity & " ini?", msg, "Hapus") Then Return

        Try
            Dim dbId = row(_cfg.RowLen)
            Await SupabaseClient.DeleteAsync($"/rest/v1/{_cfg.TableName}?id=eq.{dbId}")
            Await LoadFromSupabaseAsync()
        Catch ex As Exception
            MessageBox.Show("Gagal menghapus " & _cfg.Entity & " dari Supabase:" & vbCrLf & ex.Message, "Error Supabase", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class

''' <summary>Halaman Master Data dengan tiga tab (terhubung Supabase).</summary>
Public Class ucMasterData
    Inherits UserControl

    Private ReadOnly _tabs As MasterTabs
    Private ReadOnly _panes(2) As MasterPane

    Public Sub New()
        BackColor = Theme.OffWhite
        DoubleBuffered = True

        Controls.Add(MakeLabel("Master Data", 26, Theme.Charcoal, 24, 22, FontStyle.Bold))
        Controls.Add(MakeLabel("Kelola data referensi untuk inventaris yang konsisten.", 14, Theme.Grey, 24, 62))

        _tabs = New MasterTabs() With {.Location = New Point(24, 100)}
        AddHandler _tabs.SelectedChanged, AddressOf OnTabChanged
        Controls.Add(_tabs)

        _panes(0) = New MasterPane(BuildKategori())
        _panes(1) = New MasterPane(BuildLokasi())
        _panes(2) = New MasterPane(BuildSupplier())
        For Each p In _panes
            p.Location = New Point(0, 160)
            Controls.Add(p)
        Next
        ShowTab(0)
        AddHandler Me.Load, AddressOf OnControlLoad
    End Sub

    Private Async Sub OnControlLoad(sender As Object, e As EventArgs)
        For Each p In _panes
            Await p.LoadFromSupabaseAsync()
        Next
    End Sub

    Private Sub OnTabChanged(sender As Object, e As EventArgs)
        ShowTab(_tabs.SelectedIndex)
    End Sub

    Private Sub ShowTab(i As Integer)
        For k = 0 To 2
            _panes(k).Visible = (k = i)
        Next
    End Sub

    Private Shared Function BuildKategori() As MasterConfig
        Dim c As New MasterConfig() With {
            .Entity = "kategori", .TableName = "categories", .ViewName = "v_category_totals", .Prefix = "KAT",
            .RowLen = 4, .TotalIdx = 2, .HasKodeField = True,
            .Headers = {"Kode", "Nama kategori", "Total aset"},
            .Widths = {110, 278, 122, 144},
            .GridIdx = {0, 1, 2}
        }
        c.Fields.Add(New MasterField("Kode kategori", "KAT-009", 0))
        c.Fields.Add(New MasterField("Nama kategori", "Masukkan nama kategori", 1))
        c.Fields.Add(New MasterField("Keterangan", "Keterangan (opsional)", 3))
        Return c
    End Function

    Private Shared Function BuildLokasi() As MasterConfig
        Dim c As New MasterConfig() With {
            .Entity = "lokasi", .TableName = "locations", .ViewName = "v_location_totals", .Prefix = "LOK",
            .RowLen = 4, .TotalIdx = 2, .HasKodeField = True,
            .Headers = {"Kode", "Nama lokasi", "Total aset"},
            .Widths = {110, 278, 122, 144},
            .GridIdx = {0, 1, 2}
        }
        c.Fields.Add(New MasterField("Kode lokasi", "LOK-009", 0))
        c.Fields.Add(New MasterField("Nama lokasi", "Masukkan nama lokasi", 1))
        c.Fields.Add(New MasterField("Penanggung jawab", "Nama penanggung jawab", 3))
        Return c
    End Function

    Private Shared Function BuildSupplier() As MasterConfig
        Dim c As New MasterConfig() With {
            .Entity = "supplier", .TableName = "suppliers", .ViewName = "suppliers", .Prefix = "SUP",
            .RowLen = 5, .TotalIdx = -1, .HasKodeField = False,
            .Headers = {"Kode", "Nama supplier", "Kontak"},
            .Widths = {100, 238, 172, 144},
            .GridIdx = {0, 1, 2}
        }
        c.Fields.Add(New MasterField("Nama supplier", "Masukkan nama supplier", 1))
        c.Fields.Add(New MasterField("No. telepon", "08xx-xxxx-xxxx", 2))
        c.Fields.Add(New MasterField("Alamat email", "email@supplier.co.id", 3))
        c.Fields.Add(New MasterField("Alamat", "Alamat lengkap supplier", 4))
        Return c
    End Function
End Class
