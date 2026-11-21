Imports System
Imports System.Drawing.Drawing2D
Imports System.Globalization
Imports System.Text.Json
Imports System.Threading.Tasks

Public Class ucDashboard
    Inherits UserControl

    Private ReadOnly _id As New CultureInfo("id-ID")
    Private ReadOnly _grid As DataGridView
    Private ReadOnly _cardTotalAset As StatCard
    Private ReadOnly _cardTotalStok As StatCard
    Private ReadOnly _cardLowStock As StatCard
    Private ReadOnly _cardBorrowed As StatCard
    Private ReadOnly _lblBarPeriod As Label
    Private ReadOnly _barChart As BarChart
    Private ReadOnly _donutChart As DonutChart
    Private ReadOnly _donutCatLabels(3) As Label
    Private ReadOnly _donutPctLabels(3) As Label
    Private ReadOnly _refreshTimer As Timer
    Private _isLoading As Boolean = False

    Public Sub New(userName As String)
        BackColor = Theme.OffWhite
        DoubleBuffered = True

        ' ===== BANNER SELAMAT DATANG (24,24 | 1078 x 96) =====
        Dim banner As New GradientPanel() With {.Location = New Point(24, 24), .Size = New Size(1078, 96), .Radius = 16, .Angle = 12.0F}
        banner.Controls.Add(MakeLabel(Greeting() & ", " & Capitalize(userName) & "!", 24, Theme.Charcoal, 24, 18, FontStyle.Bold))
        banner.Controls.Add(MakeLabel("Berikut ringkasan inventaris Anda secara real-time dari Supabase.", 14, Theme.Charcoal, 24, 56))
        banner.Controls.Add(MakeLabel(Date.Now.ToString("dddd, d MMMM yyyy", _id), 12, Theme.Charcoal, 830, 38, FontStyle.Bold))
        Controls.Add(banner)

        ' ===== 4 KARTU RINGKASAN (y=136, tinggi 104, lebar 256) =====
        _cardTotalAset = New StatCard("Total Aset", "0", "Memuat data...", False) With {.Location = New Point(24, 136)}
        _cardTotalStok = New StatCard("Total Stok Barang", "0", "Barang habis pakai", False) With {.Location = New Point(298, 136)}
        _cardLowStock = New StatCard("Barang Hampir Habis", "0", "Perlu pengadaan ulang", True) With {.Location = New Point(572, 136)}
        _cardBorrowed = New StatCard("Aset Dipinjam", "0", "Memuat data...", False) With {.Location = New Point(846, 136)}
        Controls.Add(_cardTotalAset)
        Controls.Add(_cardTotalStok)
        Controls.Add(_cardLowStock)
        Controls.Add(_cardBorrowed)

        ' ===== KARTU GRAFIK BATANG (24,256 | 656 x 224) =====
        Dim cardBar As New RoundedPanel() With {.Location = New Point(24, 256), .Size = New Size(656, 224), .Radius = 8}
        cardBar.Controls.Add(MakeLabel("Transaksi Bulanan", 18, Theme.Charcoal, 16, 12, FontStyle.Bold))
        _lblBarPeriod = MakeLabel("6 bulan terakhir", 12, Theme.Grey, 510, 16)
        cardBar.Controls.Add(_lblBarPeriod)
        cardBar.Controls.Add(Legend(392, 50, Theme.Orange, "Masuk"))
        cardBar.Controls.Add(Legend(492, 50, Theme.Grey, "Keluar"))
        _barChart = New BarChart() With {.Location = New Point(16, 76), .Size = New Size(624, 140)}
        cardBar.Controls.Add(_barChart)
        Controls.Add(cardBar)

        ' ===== KARTU DONUT (696,256 | 406 x 224) =====
        Dim cardDonut As New RoundedPanel() With {.Location = New Point(696, 256), .Size = New Size(406, 224), .Radius = 8}
        cardDonut.Controls.Add(MakeLabel("Aset per Kategori", 18, Theme.Charcoal, 16, 12, FontStyle.Bold))
        _donutChart = New DonutChart() With {.Location = New Point(24, 65), .Size = New Size(140, 140)}
        cardDonut.Controls.Add(_donutChart)
        Dim cols As Color() = {Theme.Orange, Theme.Charcoal, Theme.Beige, Theme.Grey}
        For i = 0 To 3
            Dim yy = 66 + i * 34
            cardDonut.Controls.Add(New Panel() With {.BackColor = cols(i), .Location = New Point(192, yy + 4), .Size = New Size(10, 10)})
            _donutCatLabels(i) = MakeLabel("-", 12, Theme.Grey, 214, yy, FontStyle.Regular, 120, 18)
            _donutPctLabels(i) = MakeLabel("0%", 12, Theme.Grey, 340, yy, FontStyle.Regular, 50, 18)
            cardDonut.Controls.Add(_donutCatLabels(i))
            cardDonut.Controls.Add(_donutPctLabels(i))
        Next
        Controls.Add(cardDonut)

        ' ===== AKTIVITAS TERBARU =====
        Controls.Add(MakeLabel("Aktivitas Terbaru", 18, Theme.Charcoal, 24, 498, FontStyle.Bold))
        Dim btnAll As New AppButton() With {.Text = "Lihat semua", .Kind = ButtonKind.Outline, .Location = New Point(966, 492), .Size = New Size(136, 36), .BackColor = Theme.OffWhite}
        AddHandler btnAll.Click, Sub(s, e) TryCast(FindForm(), FormMain)?.NavigateTo(2)
        Controls.Add(btnAll)

        Dim wrap As New RoundedPanel() With {.Location = New Point(24, 536), .Size = New Size(1078, 156), .Radius = 6}
        _grid = BuildGrid()
        _grid.Location = New Point(1, 1)
        _grid.Size = New Size(1076, 154)
        wrap.Controls.Add(_grid)
        Controls.Add(wrap)

        ' Auto-refresh setiap 15 detik agar data tetap sinkron secara real-time
        _refreshTimer = New Timer() With {.Interval = 15000}
        AddHandler _refreshTimer.Tick, AddressOf OnRefreshTick
        AddHandler Me.Load, AddressOf OnControlLoad
        AddHandler Me.Disposed, Sub(s, e) _refreshTimer.Stop()
    End Sub

    Private Async Sub OnControlLoad(sender As Object, e As EventArgs)
        Await LoadDashboardAsync()
        _refreshTimer.Start()
    End Sub

    Private Async Sub OnRefreshTick(sender As Object, e As EventArgs)
        If Not IsDisposed AndAlso Visible Then
            Await LoadDashboardAsync()
        End If
    End Sub

    Private Async Function LoadDashboardAsync() As Task
        If _isLoading Then Return
        _isLoading = True
        Try
            ' 1. Ringkasan 4 kartu
            Dim sumJson = Await SupabaseClient.GetJsonAsync("/rest/v1/v_dashboard_summary")
            Dim totalAset As Integer = 0
            Using doc = JsonDocument.Parse(sumJson)
                If doc.RootElement.GetArrayLength() > 0 Then
                    Dim r = doc.RootElement(0)
                    totalAset = SupabaseClient.GetInt(r, "total_assets")
                    Dim jenis = SupabaseClient.GetInt(r, "total_asset_types")
                    Dim stok = SupabaseClient.GetInt(r, "total_stock")
                    Dim low = SupabaseClient.GetInt(r, "low_stock_count")
                    Dim borrowed = SupabaseClient.GetInt(r, "borrowed_count")
                    Dim overdue = SupabaseClient.GetInt(r, "overdue_count")

                    _cardTotalAset.SetData(totalAset.ToString("N0", _id), jenis & " jenis aset terdaftar")
                    _cardTotalStok.SetData(stok.ToString("N0", _id), "Barang habis pakai di semua lokasi")
                    _cardLowStock.SetData(low.ToString("N0", _id), "Perlu pengadaan ulang")
                    _cardBorrowed.SetData(borrowed.ToString("N0", _id), overdue & " terlambat jatuh tempo")
                End If
            End Using

            ' 2. Grafik batang transaksi bulanan (6 bulan terakhir)
            Dim monthlyJson = Await SupabaseClient.GetJsonAsync("/rest/v1/v_monthly_transactions?order=month.desc&limit=6")
            Dim mMap As New Dictionary(Of String, (Masuk As Integer, Keluar As Integer))()
            Using doc = JsonDocument.Parse(monthlyJson)
                For Each el In doc.RootElement.EnumerateArray()
                    Dim dt = SupabaseClient.GetDate(el, "month")
                    Dim key = dt.ToString("yyyy-MM")
                    mMap(key) = (SupabaseClient.GetInt(el, "masuk"), SupabaseClient.GetInt(el, "keluar"))
                Next
            End Using

            Dim labels(5) As String
            Dim sIn(5) As Integer
            Dim sOut(5) As Integer
            Dim maxV As Integer = 10
            Dim firstM = New Date(Date.Today.Year, Date.Today.Month, 1).AddMonths(-5)
            For i = 0 To 5
                Dim d = firstM.AddMonths(i)
                labels(i) = d.ToString("MMM", _id)
                Dim key = d.ToString("yyyy-MM")
                If mMap.ContainsKey(key) Then
                    sIn(i) = mMap(key).Masuk
                    sOut(i) = mMap(key).Keluar
                    maxV = Math.Max(maxV, Math.Max(sIn(i), sOut(i)))
                End If
            Next
            _lblBarPeriod.Text = firstM.ToString("MMM yyyy", _id) & " – " & Date.Today.ToString("MMM yyyy", _id)
            _barChart.Labels = labels
            _barChart.SeriesIn = sIn
            _barChart.SeriesOut = sOut
            _barChart.MaxValue = Math.Max(10, CInt(Math.Ceiling(maxV / 10.0) * 10))
            _barChart.Invalidate()

            ' 3. Grafik donut aset per kategori
            Dim catJson = Await SupabaseClient.GetJsonAsync("/rest/v1/v_category_totals?order=total_assets.desc")
            Dim catList As New List(Of (Name As String, Qty As Integer))()
            Using doc = JsonDocument.Parse(catJson)
                For Each el In doc.RootElement.EnumerateArray()
                    Dim q = SupabaseClient.GetInt(el, "total_assets")
                    If q > 0 Then catList.Add((SupabaseClient.GetStr(el, "name"), q))
                Next
            End Using

            Dim vals(3) As Integer
            Dim names(3) As String
            For i = 0 To 3
                names(i) = "-"
                vals(i) = 0
            Next
            Dim sumCat = catList.Sum(Function(x) x.Qty)
            If catList.Count <= 4 Then
                For i = 0 To catList.Count - 1
                    names(i) = catList(i).Name
                    vals(i) = catList(i).Qty
                Next
            Else
                For i = 0 To 2
                    names(i) = catList(i).Name
                    vals(i) = catList(i).Qty
                Next
                names(3) = "Lainnya"
                vals(3) = catList.Skip(3).Sum(Function(x) x.Qty)
            End If

            _donutChart.Values = vals
            _donutChart.CenterValue = totalAset.ToString("N0", _id)
            _donutChart.Invalidate()
            For i = 0 To 3
                _donutCatLabels(i).Text = names(i)
                Dim pct = If(sumCat > 0, CInt(Math.Round(vals(i) * 100.0 / sumCat)), 0)
                _donutPctLabels(i).Text = If(names(i) = "-", "", pct & "%")
            Next

            ' 4. Aktivitas terbaru
            Dim actJson = Await SupabaseClient.GetJsonAsync("/rest/v1/v_recent_activity?limit=10")
            _grid.Rows.Clear()
            Using doc = JsonDocument.Parse(actJson)
                For Each el In doc.RootElement.EnumerateArray()
                    Dim t = SupabaseClient.GetDate(el, "happened_at")
                    Dim waktu = If(t.Date = Date.Today, "Hari ini, " & t.ToString("HH:mm"), t.ToString("d MMM yyyy, HH:mm", _id))
                    Dim act = SupabaseClient.GetStr(el, "activity")
                    Dim aset = SupabaseClient.GetStr(el, "asset_name")
                    Dim officer = SupabaseClient.GetStr(el, "officer")
                    Dim st = FormatStatus(SupabaseClient.GetStr(el, "asset_status"))
                    _grid.Rows.Add(waktu, act, aset, officer, st)
                Next
            End Using
            If _grid.Rows.Count = 0 Then
                _grid.Rows.Add("-", "Belum ada transaksi / peminjaman tercatat", "-", "-", "Tersedia")
            End If
            _grid.ClearSelection()
        Catch
            ' Biarkan tenang saat auto-refresh agar tidak mengganggu pengguna
        Finally
            _isLoading = False
        End Try
    End Function

    Private Shared Function FormatStatus(dbVal As String) As String
        Select Case dbVal
            Case "tersedia" : Return "Tersedia"
            Case "dipinjam" : Return "Dipinjam"
            Case "maintenance" : Return "Maintenance"
            Case "rusak" : Return "Rusak"
            Case Else : Return If(String.IsNullOrEmpty(dbVal), "Tersedia", dbVal)
        End Select
    End Function

    ' ---------- helper ----------
    Private Function Legend(x As Integer, y As Integer, c As Color, caption As String) As Control
        Dim p As New Panel() With {.Location = New Point(x, y), .Size = New Size(60, 18), .BackColor = Theme.OffWhite}
        p.Controls.Add(New Panel() With {.BackColor = c, .Location = New Point(0, 4), .Size = New Size(10, 10)})
        p.Controls.Add(MakeLabel(caption, 12, Theme.Grey, 18, 1))
        Return p
    End Function

    Private Shared Function Greeting() As String
        Dim h = Date.Now.Hour
        If h < 11 Then Return "Selamat pagi"
        If h < 15 Then Return "Selamat siang"
        If h < 18 Then Return "Selamat sore"
        Return "Selamat malam"
    End Function

    Private Shared Function Capitalize(s As String) As String
        If String.IsNullOrEmpty(s) Then Return s
        Return Char.ToUpper(s(0)) & s.Substring(1)
    End Function

    ' ---------- tabel aktivitas ----------
    Private Function BuildGrid() As DataGridView
        Dim g As New DataGridView() With {
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .AllowUserToResizeRows = False,
            .AllowUserToResizeColumns = False,
            .ReadOnly = True,
            .RowHeadersVisible = False,
            .ScrollBars = ScrollBars.Vertical,
            .BorderStyle = BorderStyle.None,
            .CellBorderStyle = DataGridViewCellBorderStyle.None,
            .EnableHeadersVisualStyles = False,
            .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            .ColumnHeadersHeight = 40,
            .RowTemplate = New DataGridViewRow() With {.Height = 38},
            .BackgroundColor = Theme.OffWhite,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False
        }
        g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None

        With g.ColumnHeadersDefaultCellStyle
            .BackColor = Theme.Beige
            .ForeColor = Theme.Charcoal
            .Font = AppFont(13, FontStyle.Bold)
            .Padding = New Padding(13, 0, 0, 0)
            .SelectionBackColor = Theme.Beige
            .SelectionForeColor = Theme.Charcoal
        End With
        With g.DefaultCellStyle
            .BackColor = Theme.OffWhite
            .ForeColor = Theme.Grey
            .Font = AppFont(13)
            .Padding = New Padding(13, 0, 0, 0)
            .SelectionBackColor = Theme.OffWhite
            .SelectionForeColor = Theme.Grey
        End With
        With g.AlternatingRowsDefaultCellStyle
            .BackColor = Theme.Beige
            .SelectionBackColor = Theme.Beige
            .SelectionForeColor = Theme.Grey
        End With

        Dim heads As String() = {"Waktu", "Aktivitas", "Aset / Barang", "Petugas", "Status"}
        Dim widths As Integer() = {173, 230, 330, 210, 133}
        For i = 0 To heads.Length - 1
            g.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = heads(i), .Width = widths(i), .SortMode = DataGridViewColumnSortMode.NotSortable})
        Next

        AddHandler g.CellPainting, AddressOf GridCellPainting
        AddHandler g.SelectionChanged, Sub(s, e) g.ClearSelection()
        Return g
    End Function

    Private Sub GridCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex <> 4 Then Return
        e.PaintBackground(e.CellBounds, False)
        Dim txt As String = Convert.ToString(e.Value)
        Dim f = AppFont(12, FontStyle.Bold)
        Dim w = TextRenderer.MeasureText(txt, f).Width + 20
        Dim h = 26
        Dim r As New Rectangle(e.CellBounds.X + 12, e.CellBounds.Y + (e.CellBounds.Height - h) \ 2, w, h)
        Dim bg = If(txt = "Tersedia", Theme.StatusGreen, Theme.Orange)
        FillRounded(e.Graphics, r, 4, bg)
        DrawText(e.Graphics, txt, f, Color.White, r, StringAlignment.Center)
        e.Handled = True
    End Sub
End Class

' =====================================================================
' KARTU RINGKASAN
' =====================================================================
Public Class StatCard
    Inherits RoundedPanel

    Private ReadOnly _lblValue As Label
    Private ReadOnly _lblNote As Label

    Public Sub New(title As String, value As String, note As String, highlight As Boolean)
        Size = New Size(256, 104)
        Radius = 8
        Controls.Add(MakeLabel(title, 13, Theme.Grey, 16, 12))
        _lblValue = MakeLabel(value, 32, If(highlight, Theme.Orange, Theme.Charcoal), 16, 28, FontStyle.Bold)
        Controls.Add(_lblValue)
        _lblNote = MakeLabel(note, 12, Theme.Grey, 16, 76)
        Controls.Add(_lblNote)
        Dim bar As New Panel() With {.BackColor = If(highlight, Theme.Orange, Theme.Beige), .Location = New Point(224, 16), .Size = New Size(16, 4)}
        Controls.Add(bar)
    End Sub

    Public Sub SetData(value As String, note As String)
        _lblValue.Text = value
        _lblNote.Text = note
    End Sub
End Class

' =====================================================================
' GRAFIK BATANG (Masuk vs Keluar)
' =====================================================================
Public Class BarChart
    Inherits Control

    Public Property Labels As String() = {"-", "-", "-", "-", "-", "-"}
    Public Property SeriesIn As Integer() = {0, 0, 0, 0, 0, 0}
    Public Property SeriesOut As Integer() = {0, 0, 0, 0, 0, 0}
    Public Property MaxValue As Integer = 10

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint Or ControlStyles.ResizeRedraw, True)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.Clear(Theme.OffWhite)
        Dim topY As Integer = 12, baseY As Integer = 92
        Dim left As Integer = 33, right As Integer = Width - 12
        Dim fontAx = AppFont(12)
        Dim safeMax = Math.Max(1, MaxValue)

        ' garis grid + label sumbu Y
        For Each v In {0, safeMax \ 2, safeMax}
            Dim y = baseY - CInt((baseY - topY) * v / safeMax)
            Using p As New Pen(Theme.Beige, 1)
                g.DrawLine(p, left, y, right, y)
            End Using
            DrawText(g, v.ToString(), fontAx, Theme.Grey, New Rectangle(0, y - 9, 30, 18))
        Next

        ' batang
        For i = 0 To Labels.Length - 1
            Dim c = 80 + i * 92
            Dim hIn = CInt((baseY - topY) * SeriesIn(i) / safeMax)
            Dim hOut = CInt((baseY - topY) * SeriesOut(i) / safeMax)
            If hIn > 0 Then
                Using b As New SolidBrush(Theme.Orange)
                    g.FillRectangle(b, c - 21, baseY - hIn, 18, hIn)
                End Using
            End If
            If hOut > 0 Then
                Using b As New SolidBrush(Theme.Grey)
                    g.FillRectangle(b, c + 3, baseY - hOut, 18, hOut)
                End Using
            End If
            DrawText(g, Labels(i), fontAx, Theme.Grey, New Rectangle(c - 30, baseY + 8, 60, 20), StringAlignment.Center)
        Next
    End Sub
End Class

' =====================================================================
' GRAFIK DONUT
' =====================================================================
Public Class DonutChart
    Inherits Control

    Public Property Values As Integer() = {0, 0, 0, 0}
    Public Property SliceColors As Color() = {Theme.Orange, Theme.Charcoal, Theme.Beige, Theme.Grey}
    Public Property CenterValue As String = "0"
    Public Property CenterCaption As String = "aset"

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint Or ControlStyles.ResizeRedraw, True)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.Clear(Theme.OffWhite)
        g.SmoothingMode = SmoothingMode.AntiAlias

        Const thickness As Integer = 20
        Dim rect As New Rectangle(thickness \ 2 + 1, thickness \ 2 + 1, Width - thickness - 3, Height - thickness - 3)
        Dim total As Integer = Values.Sum()

        If total <= 0 Then
            Using p As New Pen(Theme.Beige, thickness)
                g.DrawArc(p, rect, 0, 360)
            End Using
        Else
            Dim start As Single = 90.0F
            For i = 0 To Values.Length - 1
                If Values(i) <= 0 Then Continue For
                Dim sweep As Single = 360.0F * Values(i) / total
                Using p As New Pen(SliceColors(i), thickness) With {.LineJoin = LineJoin.Round}
                    g.DrawArc(p, rect, start, sweep)
                End Using
                start += sweep
            Next
        End If

        DrawText(g, CenterValue, AppFont(18, FontStyle.Bold), Theme.Charcoal, New Rectangle(0, Height \ 2 - 20, Width, 24), StringAlignment.Center)
        DrawText(g, CenterCaption, AppFont(12), Theme.Grey, New Rectangle(0, Height \ 2 + 2, Width, 18), StringAlignment.Center)
    End Sub
End Class
