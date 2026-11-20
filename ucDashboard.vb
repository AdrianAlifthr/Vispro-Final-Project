Imports System.Drawing.Drawing2D
Imports System.Globalization

Public Class ucDashboard
    Inherits UserControl

    Private ReadOnly _grid As DataGridView

    Public Sub New(userName As String)
        BackColor = Theme.OffWhite
        DoubleBuffered = True

        ' ===== BANNER SELAMAT DATANG (24,24 | 1078 x 96) =====
        Dim banner As New GradientPanel() With {.Location = New Point(24, 24), .Size = New Size(1078, 96), .Radius = 16, .Angle = 12.0F}
        banner.Controls.Add(MakeLabel(Greeting() & ", " & Capitalize(userName) & "!", 24, Theme.Charcoal, 24, 18, FontStyle.Bold))
        banner.Controls.Add(MakeLabel("Berikut ringkasan inventaris Anda hari ini.", 14, Theme.Charcoal, 24, 56))
        Dim id As New CultureInfo("id-ID")
        banner.Controls.Add(MakeLabel(Date.Now.ToString("dddd, d MMMM yyyy", id), 12, Theme.Charcoal, 830, 38, FontStyle.Bold))
        Controls.Add(banner)

        ' ===== 4 KARTU RINGKASAN (y=136, tinggi 104, lebar 256) =====
        ' TODO (Supabase): isi nilai dari query COUNT/SUM.
        Controls.Add(New StatCard("Total Aset", "1.248", "42 aset baru bulan ini", False) With {.Location = New Point(24, 136)})
        Controls.Add(New StatCard("Total Stok Barang", "3.560", "Di seluruh lokasi", False) With {.Location = New Point(298, 136)})
        Controls.Add(New StatCard("Barang Hampir Habis", "12", "Perlu pengadaan ulang", True) With {.Location = New Point(572, 136)})
        Controls.Add(New StatCard("Aset Dipinjam", "18", "8 jatuh tempo minggu ini", False) With {.Location = New Point(846, 136)})

        ' ===== KARTU GRAFIK BATANG (24,256 | 656 x 224) =====
        Dim cardBar As New RoundedPanel() With {.Location = New Point(24, 256), .Size = New Size(656, 224), .Radius = 8}
        cardBar.Controls.Add(MakeLabel("Transaksi Bulanan", 18, Theme.Charcoal, 16, 12, FontStyle.Bold))
        cardBar.Controls.Add(MakeLabel("Jan – Jun 2025", 12, Theme.Grey, 510, 16))
        cardBar.Controls.Add(Legend(392, 50, Theme.Orange, "Masuk"))
        cardBar.Controls.Add(Legend(492, 50, Theme.Grey, "Keluar"))
        cardBar.Controls.Add(New BarChart() With {.Location = New Point(16, 76), .Size = New Size(624, 140)})
        Controls.Add(cardBar)

        ' ===== KARTU DONUT (696,256 | 406 x 224) =====
        Dim cardDonut As New RoundedPanel() With {.Location = New Point(696, 256), .Size = New Size(406, 224), .Radius = 8}
        cardDonut.Controls.Add(MakeLabel("Aset per Kategori", 18, Theme.Charcoal, 16, 12, FontStyle.Bold))
        cardDonut.Controls.Add(New DonutChart() With {.Location = New Point(24, 65), .Size = New Size(140, 140)})
        Dim cats As String() = {"Elektronik", "Furnitur", "Peralatan", "Lainnya"}
        Dim pct As String() = {"42%", "28%", "18%", "12%"}
        Dim cols As Color() = {Theme.Orange, Theme.Charcoal, Theme.Beige, Theme.Grey}
        For i = 0 To 3
            Dim yy = 66 + i * 34
            cardDonut.Controls.Add(New Panel() With {.BackColor = cols(i), .Location = New Point(192, yy + 4), .Size = New Size(10, 10)})
            cardDonut.Controls.Add(MakeLabel(cats(i), 12, Theme.Grey, 214, yy))
            cardDonut.Controls.Add(MakeLabel(pct(i), 12, Theme.Grey, 276, yy))
        Next
        Controls.Add(cardDonut)

        ' ===== AKTIVITAS TERBARU =====
        Controls.Add(MakeLabel("Aktivitas Terbaru", 18, Theme.Charcoal, 24, 498, FontStyle.Bold))
        Dim btnAll As New AppButton() With {.Text = "Lihat semua", .Kind = ButtonKind.Outline, .Location = New Point(966, 492), .Size = New Size(136, 36), .BackColor = Theme.OffWhite}
        Controls.Add(btnAll)

        Dim wrap As New RoundedPanel() With {.Location = New Point(24, 536), .Size = New Size(1078, 156), .Radius = 6}
        _grid = BuildGrid()
        _grid.Location = New Point(1, 1)
        _grid.Size = New Size(1076, 154)
        wrap.Controls.Add(_grid)
        Controls.Add(wrap)

        LoadActivity()
    End Sub

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
            .ScrollBars = ScrollBars.None,
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

    Private Sub LoadActivity()
        ' TODO (Supabase): ganti dengan data dari tabel stock_transactions / loans.
        _grid.Rows.Add("Hari ini, 09:42", "Barang masuk · 24 unit", "Mouse Logitech M170", "Aditya Pratama", "Tersedia")
        _grid.Rows.Add("Hari ini, 09:15", "Peminjaman aset", "Laptop Lenovo ThinkPad", "Budi Santoso", "Dipinjam")
        _grid.Rows.Add("23 Jun 2025, 14:30", "Peminjaman aset", "Proyektor Epson", "Siti Rahmawati", "Dipinjam")
        _grid.ClearSelection()
    End Sub

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

    Public Sub New(title As String, value As String, note As String, highlight As Boolean)
        Size = New Size(256, 104)
        Radius = 8
        Controls.Add(MakeLabel(title, 13, Theme.Grey, 16, 12))
        Controls.Add(MakeLabel(value, 32, If(highlight, Theme.Orange, Theme.Charcoal), 16, 28, FontStyle.Bold))
        Controls.Add(MakeLabel(note, 12, Theme.Grey, 16, 76))
        Dim bar As New Panel() With {.BackColor = If(highlight, Theme.Orange, Theme.Beige), .Location = New Point(224, 16), .Size = New Size(16, 4)}
        Controls.Add(bar)
    End Sub
End Class

' =====================================================================
' GRAFIK BATANG (Masuk vs Keluar)
' =====================================================================
Public Class BarChart
    Inherits Control

    Public Property Labels As String() = {"Jan", "Feb", "Mar", "Apr", "Mei", "Jun"}
    Public Property SeriesIn As Integer() = {62, 78, 56, 105, 88, 118}
    Public Property SeriesOut As Integer() = {32, 45, 38, 64, 48, 72}
    Public Property MaxValue As Integer = 120

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint Or ControlStyles.ResizeRedraw, True)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.Clear(Theme.OffWhite)
        Dim topY As Integer = 12, baseY As Integer = 92
        Dim left As Integer = 33, right As Integer = Width - 12
        Dim fontAx = AppFont(12)

        ' garis grid + label sumbu Y
        For Each v In {0, MaxValue \ 2, MaxValue}
            Dim y = baseY - CInt((baseY - topY) * v / MaxValue)
            Using p As New Pen(Theme.Beige, 1)
                g.DrawLine(p, left, y, right, y)
            End Using
            DrawText(g, v.ToString(), fontAx, Theme.Grey, New Rectangle(0, y - 9, 30, 18))
        Next

        ' batang
        For i = 0 To Labels.Length - 1
            Dim c = 80 + i * 92
            Dim hIn = CInt((baseY - topY) * SeriesIn(i) / MaxValue)
            Dim hOut = CInt((baseY - topY) * SeriesOut(i) / MaxValue)
            Using b As New SolidBrush(Theme.Orange)
                g.FillRectangle(b, c - 21, baseY - hIn, 18, hIn)
            End Using
            Using b As New SolidBrush(Theme.Grey)
                g.FillRectangle(b, c + 3, baseY - hOut, 18, hOut)
            End Using
            DrawText(g, Labels(i), fontAx, Theme.Grey, New Rectangle(c - 30, baseY + 8, 60, 20), StringAlignment.Center)
        Next
    End Sub
End Class

' =====================================================================
' GRAFIK DONUT
' =====================================================================
Public Class DonutChart
    Inherits Control

    Public Property Values As Integer() = {42, 28, 18, 12}
    Public Property SliceColors As Color() = {Theme.Orange, Theme.Charcoal, Theme.Beige, Theme.Grey}
    Public Property CenterValue As String = "1.248"
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
        Dim start As Single = 90.0F   ' mulai dari bawah, searah jarum jam

        For i = 0 To Values.Length - 1
            Dim sweep As Single = 360.0F * Values(i) / total
            Using p As New Pen(SliceColors(i), thickness) With {.LineJoin = LineJoin.Round}
                g.DrawArc(p, rect, start, sweep)
            End Using
            start += sweep
        Next

        DrawText(g, CenterValue, AppFont(18, FontStyle.Bold), Theme.Charcoal, New Rectangle(0, Height \ 2 - 20, Width, 24), StringAlignment.Center)
        DrawText(g, CenterCaption, AppFont(12), Theme.Grey, New Rectangle(0, Height \ 2 + 2, Width, 18), StringAlignment.Center)
    End Sub
End Class
