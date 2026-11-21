Imports System
Imports System.Drawing.Drawing2D
Imports System.Globalization
Imports System.Text.Json
Imports System.Threading.Tasks

''' <summary>Strip tab "Informasi | Riwayat Transaksi | Pemeliharaan" sesuai desain.</summary>
Public Class DetailTabs
    Inherits Control

    Private Shared ReadOnly Captions As String() = {"Informasi", "Riwayat Transaksi", "Pemeliharaan"}
    Private Shared ReadOnly Widths As Integer() = {178, 292, 244}
    Private _selected As Integer = 0
    Public Event SelectedChanged As EventHandler

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        Cursor = Cursors.Hand
        Size = New Size(712, 48)
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

    Private Function TabAt(x As Integer) As Integer
        Dim start = 0
        For i = 0 To Widths.Length - 1
            If x >= start AndAlso x < start + Widths(i) Then Return i
            start += Widths(i)
        Next
        Return -1
    End Function

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        Dim i = TabAt(e.X)
        If i >= 0 Then SelectedIndex = i
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        Dim x = 0
        For i = 0 To Captions.Length - 1
            Dim w = If(i = Captions.Length - 1, Width - x, Widths(i))
            Dim r As New Rectangle(x, 0, w, Height)
            Dim active = (i = _selected)
            Using b As New SolidBrush(If(active, Theme.OffWhite, Theme.Beige))
                g.FillRectangle(b, r)
            End Using
            If active Then
                Using b As New SolidBrush(Theme.Orange)
                    g.FillRectangle(b, x, Height - 3, w, 3)
                End Using
            End If
            DrawText(g, Captions(i), AppFont(14, If(active, FontStyle.Bold, FontStyle.Regular)), If(active, Theme.Orange, Theme.Grey), New Rectangle(x + 20, 0, w - 20, Height - 2))
            x += Widths(i)
        Next
    End Sub
End Class

''' <summary>Halaman Detail Aset yang terhubung ke Supabase.</summary>
Public Class ucAsetDetail
    Inherits UserControl

    Private ReadOnly _id As New CultureInfo("id-ID")
    Private ReadOnly _row As AssetRow
    Private ReadOnly _tabs As DetailTabs
    Private ReadOnly _panels(2) As Panel
    Private ReadOnly _lblLastTime As Label
    Private ReadOnly _lblLastTitle As Label
    Private ReadOnly _lblLastSub As Label

    Public Sub New(row As AssetRow)
        _row = row
        BackColor = Theme.OffWhite
        DoubleBuffered = True

        ' ===== Breadcrumb, judul, tombol Edit =====
        Dim crumb = MakeLabel("Data Aset / Barang", 12, Theme.Grey, 24, 20)
        crumb.Cursor = Cursors.Hand
        AddHandler crumb.Click, AddressOf GoBack
        Controls.Add(crumb)
        Controls.Add(MakeLabel("/", 12, Theme.Grey, 134, 20))
        Controls.Add(MakeLabel("Detail Aset", 12, Theme.Grey, 155, 20))
        Controls.Add(MakeLabel(row.Nama, 26, Theme.Charcoal, 24, 46, FontStyle.Bold))

        Dim btnEdit As New AppButton() With {.Text = "Edit Aset", .Kind = ButtonKind.Outline, .Location = New Point(966, 48), .Size = New Size(136, 36), .BackColor = Theme.OffWhite}
        AddHandler btnEdit.Click, AddressOf OnEditClick
        Controls.Add(btnEdit)

        BuildSummaryCard()
        _tabs = BuildTabCard()

        ' ===== Kartu bawah: aktivitas terakhir =====
        Dim card As New RoundedPanel() With {.Location = New Point(24, 548), .Size = New Size(1078, 120), .Radius = 8}
        card.Controls.Add(MakeLabel("Aktivitas terakhir", 18, Theme.Charcoal, 20, 20, FontStyle.Bold))
        _lblLastTime = MakeLabel("-", 12, Theme.Grey, 20, 58)
        _lblLastTitle = MakeLabel("Memuat riwayat...", 14, Theme.Charcoal, 224, 56, FontStyle.Bold)
        _lblLastSub = MakeLabel("", 12, Theme.Grey, 224, 82)
        card.Controls.Add(_lblLastTime)
        card.Controls.Add(_lblLastTitle)
        card.Controls.Add(_lblLastSub)
        Dim btn As New AppButton() With {.Text = "Lihat riwayat", .Kind = ButtonKind.Outline, .Location = New Point(912, 54), .Size = New Size(144, 36), .BackColor = Theme.OffWhite}
        AddHandler btn.Click, Sub(s, e) _tabs.SelectedIndex = 1
        card.Controls.Add(btn)
        Controls.Add(card)

        ShowTab(0)
        AddHandler Me.Load, AddressOf OnControlLoad
    End Sub

    Private Async Sub OnControlLoad(sender As Object, e As EventArgs)
        Await LoadRelatedAsync()
    End Sub

    Private Async Function LoadRelatedAsync() As Task
        If String.IsNullOrEmpty(_row.Id) Then Return
        Try
            ' 1. Riwayat Transaksi & Peminjaman aset ini
            Dim historyRows As New List(Of (SortAt As Date, Cells As String()))()
            Dim stJson = Await SupabaseClient.GetJsonAsync($"/rest/v1/v_stock_transactions?asset_id=eq.{_row.Id}&order=trx_date.desc&limit=5")
            Using doc = JsonDocument.Parse(stJson)
                For Each el In doc.RootElement.EnumerateArray()
                    Dim dt = SupabaseClient.GetDate(el, "trx_date")
                    Dim tp = SupabaseClient.GetStr(el, "type")
                    Dim qty = SupabaseClient.GetInt(el, "quantity")
                    Dim officer = SupabaseClient.GetStr(el, "officer_name")
                    Dim refNo = SupabaseClient.GetStr(el, "ref_no")
                    Dim label = If(tp = "masuk", "Barang masuk", "Barang keluar") & " · " & qty & " unit"
                    historyRows.Add((dt, {dt.ToString("dd MMM yyyy", _id), label, If(officer = "", "-", officer), refNo}))
                Next
            End Using

            Dim lnJson = Await SupabaseClient.GetJsonAsync($"/rest/v1/v_loans?asset_id=eq.{_row.Id}&order=loan_date.desc&limit=5")
            Using doc = JsonDocument.Parse(lnJson)
                For Each el In doc.RootElement.EnumerateArray()
                    Dim dt = SupabaseClient.GetDate(el, "loan_date")
                    Dim st = SupabaseClient.GetStr(el, "effective_status")
                    Dim borrower = SupabaseClient.GetStr(el, "borrower_name")
                    Dim loanNo = SupabaseClient.GetStr(el, "loan_no")
                    Dim label = If(st = "dikembalikan", "Pengembalian aset", "Peminjaman aset")
                    historyRows.Add((dt, {dt.ToString("dd MMM yyyy", _id), label, borrower, loanNo}))
                Next
            End Using

            Dim orderedHist = historyRows.OrderByDescending(Function(x) x.SortAt).Take(4).ToList()
            RenderHistoryPanel(_panels(1), orderedHist.Select(Function(x) x.Cells).ToList())

            If orderedHist.Count > 0 Then
                Dim top = orderedHist(0)
                _lblLastTime.Text = top.Cells(0)
                _lblLastTitle.Text = top.Cells(1)
                _lblLastSub.Text = "Oleh " & top.Cells(2) & "  •  Dokumen " & top.Cells(3)
            Else
                _lblLastTime.Text = "-"
                _lblLastTitle.Text = "Belum ada aktivitas untuk aset ini"
                _lblLastSub.Text = "Transaksi barang masuk/keluar atau peminjaman akan tampil di sini."
            End If

            ' 2. Pemeliharaan aset ini
            Dim mRows As New List(Of String())()
            Dim mtJson = Await SupabaseClient.GetJsonAsync($"/rest/v1/v_maintenances?asset_id=eq.{_row.Id}&order=maintenance_date.desc&limit=4")
            Using doc = JsonDocument.Parse(mtJson)
                For Each el In doc.RootElement.EnumerateArray()
                    Dim dt = SupabaseClient.GetDate(el, "maintenance_date")
                    Dim notes = SupabaseClient.GetStr(el, "notes")
                    Dim cost = SupabaseClient.GetDec(el, "cost")
                    Dim st = SupabaseClient.GetStr(el, "status")
                    Dim stUi = If(st.Length > 0, Char.ToUpper(st(0)) & st.Substring(1), "-")
                    mRows.Add({dt.ToString("dd MMM yyyy", _id), If(notes = "", "Perawatan rutin", notes), "Rp " & cost.ToString("N0", _id), stUi})
                Next
            End Using
            RenderMaintenancePanel(_panels(2), mRows)
        Catch
            _lblLastTitle.Text = "Gagal memuat riwayat aset"
        End Try
    End Function

    ' ---------- kartu kiri: ringkasan ----------
    Private Sub BuildSummaryCard()
        Dim card As New RoundedPanel() With {.Location = New Point(24, 104), .Size = New Size(340, 420), .Radius = 8}
        Dim photo As New RoundedPanel() With {.Location = New Point(20, 16), .Size = New Size(298, 200), .Radius = 6, .FillColor = Theme.BeigeLight, .BorderColor = Theme.BeigeLight}
        photo.Controls.Add(MakeLabel("Belum ada foto", 14, Theme.Grey, 0, 90, FontStyle.Regular, 298, 20))
        DirectCast(photo.Controls(0), Label).TextAlign = ContentAlignment.MiddleCenter
        card.Controls.Add(photo)

        card.Controls.Add(MakeLabel(_row.Kode, 12, Theme.Grey, 20, 232))
        card.Controls.Add(MakeLabel(_row.Nama, 18, Theme.Charcoal, 20, 262, FontStyle.Bold))
        Dim badge As New StatusBadge() With {.Location = New Point(20, 301)}
        badge.Status = _row.Status
        card.Controls.Add(badge)
        card.Controls.Add(MakeLabel(_row.Jumlah & " unit  •  Kondisi " & _row.Kondisi.ToLower(), 14, Theme.Grey, 20, 350))
        Controls.Add(card)
    End Sub

    ' ---------- kartu kanan: tab ----------
    Private Function BuildTabCard() As DetailTabs
        Dim card As New RoundedPanel() With {.Location = New Point(388, 104), .Size = New Size(714, 420), .Radius = 8}
        For i = 0 To 2
            _panels(i) = New Panel() With {.Location = New Point(1, 49), .Size = New Size(712, 369), .BackColor = Theme.OffWhite}
            card.Controls.Add(_panels(i))
        Next
        BuildInfoPanel(_panels(0))
        RenderHistoryPanel(_panels(1), New List(Of String())())
        RenderMaintenancePanel(_panels(2), New List(Of String())())

        Dim tabs As New DetailTabs() With {.Location = New Point(1, 1)}
        AddHandler tabs.SelectedChanged, AddressOf OnTabChanged
        card.Controls.Add(tabs)
        Controls.Add(card)
        Return tabs
    End Function

    Private Sub OnTabChanged(sender As Object, e As EventArgs)
        ShowTab(_tabs.SelectedIndex)
    End Sub

    Private Sub ShowTab(index As Integer)
        For i = 0 To 2
            _panels(i).Visible = (i = index)
        Next
    End Sub

    Private Sub AddInfo(p As Panel, caption As String, value As String, col As Integer, row As Integer)
        Dim x = If(col = 0, 23, 369)
        p.Controls.Add(MakeLabel(caption, 12, Theme.Grey, x, 23 + row * 66))
        p.Controls.Add(MakeLabel(value, 14, Theme.Charcoal, x, 45 + row * 66, FontStyle.Bold))
    End Sub

    Private Sub BuildInfoPanel(p As Panel)
        Dim tglStr = "-"
        Dim dt As Date
        If Date.TryParse(_row.TanggalPerolehan, dt) Then tglStr = dt.ToString("d MMMM yyyy", _id)
        Dim hargaStr = "Rp " & _row.Harga.ToString("N0", _id)

        AddInfo(p, "Kategori", _row.Kategori, 0, 0)
        AddInfo(p, "Lokasi", _row.Lokasi, 1, 0)
        AddInfo(p, "Supplier", If(String.IsNullOrEmpty(_row.Supplier), "-", _row.Supplier), 0, 1)
        AddInfo(p, "Tanggal Perolehan", tglStr, 1, 1)
        AddInfo(p, "Harga Perolehan", hargaStr, 0, 2)
        AddInfo(p, "Jumlah", _row.Jumlah & " unit", 1, 2)
        AddInfo(p, "Kondisi", _row.Kondisi, 0, 3)
        AddInfo(p, "Total Nilai", "Rp " & (_row.Harga * _row.Jumlah).ToString("N0", _id), 1, 3)
        p.Controls.Add(New Panel() With {.BackColor = Theme.Beige, .Location = New Point(23, 287), .Size = New Size(666, 1)})
        p.Controls.Add(MakeLabel("Keterangan", 12, Theme.Grey, 23, 302))
        p.Controls.Add(MakeLabel(If(String.IsNullOrEmpty(_row.Keterangan), "-", _row.Keterangan), 14, Theme.Charcoal, 23, 327))
    End Sub

    Private Sub RenderHistoryPanel(p As Panel, rows As List(Of String()))
        p.Controls.Clear()
        p.Controls.Add(MakeLabel("Riwayat Transaksi", 20, Theme.Charcoal, 23, 21, FontStyle.Bold))
        p.Controls.Add(MakeLabel("Catatan pergerakan dan perubahan stok aset.", 12, Theme.Grey, 23, 54))
        If rows.Count > 0 Then
            p.Controls.Add(MiniTable({"Tanggal", "Aktivitas", "Petugas", "Referensi"}, {113, 212, 160, 181}, rows, 81))
            p.Controls.Add(MakeLabel("Menampilkan " & rows.Count & " transaksi terakhir", 12, Theme.Grey, 23, 316))
        Else
            p.Controls.Add(MakeLabel("Belum ada riwayat transaksi untuk aset ini.", 13, Theme.Grey, 23, 100))
        End If
    End Sub

    Private Sub RenderMaintenancePanel(p As Panel, rows As List(Of String()))
        p.Controls.Clear()
        p.Controls.Add(MakeLabel("Pemeliharaan aset", 20, Theme.Charcoal, 23, 21, FontStyle.Bold))
        p.Controls.Add(MakeLabel("Jadwal dan catatan perawatan aset ini.", 12, Theme.Grey, 23, 54))
        If rows.Count > 0 Then
            p.Controls.Add(MiniTable({"Tanggal", "Pekerjaan", "Biaya", "Status"}, {113, 263, 140, 150}, rows, 81))
            p.Controls.Add(MakeLabel("Biaya aktual diperbarui setelah pekerjaan selesai.", 12, Theme.Grey, 23, 316))
        Else
            p.Controls.Add(MakeLabel("Belum ada jadwal pemeliharaan untuk aset ini.", 13, Theme.Grey, 23, 100))
        End If
    End Sub

    Private Function MiniTable(heads As String(), widths As Integer(), rows As List(Of String()), top As Integer) As Control
        Dim height = 41 + rows.Count * 48
        Dim wrap As New RoundedPanel() With {.Location = New Point(23, top), .Size = New Size(666, height), .Radius = 6}
        Dim g As New DataGridView() With {.Location = New Point(1, 1), .Size = New Size(664, height - 2)}
        StyleGrid(g, 48)
        g.ColumnHeadersHeight = 40
        For i = 0 To heads.Length - 1
            g.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = heads(i), .Width = widths(i), .SortMode = DataGridViewColumnSortMode.NotSortable})
        Next
        g.Columns(0).DefaultCellStyle.ForeColor = Theme.Charcoal
        g.Columns(0).DefaultCellStyle.SelectionForeColor = Theme.Charcoal
        For Each r In rows
            g.Rows.Add(r)
        Next
        g.ClearSelection()
        wrap.Controls.Add(g)
        Return wrap
    End Function

    ' ---------- navigasi ----------
    Private Sub OnEditClick(sender As Object, e As EventArgs)
        TryCast(FindForm(), FormMain)?.ShowPage(New ucAsetForm(_row), 1)
    End Sub

    Private Sub GoBack(sender As Object, e As EventArgs)
        TryCast(FindForm(), FormMain)?.ShowPage(New ucAsetList(), 1)
    End Sub
End Class
