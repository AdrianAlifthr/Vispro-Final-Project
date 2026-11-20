Imports System.Drawing.Printing
Imports System.Globalization
Imports System.IO
Imports System.Text

''' <summary>Satu baris laporan aset (nanti dari query assets + categories + locations).</summary>
Public Class ReportRow
    Public Property Kode As String
    Public Property Nama As String
    Public Property Kategori As String
    Public Property Lokasi As String
    Public Property Jumlah As Integer
    Public Property Nilai As Decimal
    Public Property Tgl As Date

    Public Sub New(kode As String, nama As String, kategori As String, lokasi As String, jumlah As Integer, nilai As Decimal, tgl As Date)
        Me.Kode = kode : Me.Nama = nama : Me.Kategori = kategori : Me.Lokasi = lokasi
        Me.Jumlah = jumlah : Me.Nilai = nilai : Me.Tgl = tgl
    End Sub
End Class

''' <summary>Halaman Laporan: filter periode/kategori, ringkasan, pratinjau, dan export.</summary>
Public Class ucLaporan
    Inherits UserControl

    Private Const PageSize As Integer = 5

    Private ReadOnly _id As New CultureInfo("id-ID")
    Private ReadOnly _all As New List(Of ReportRow)
    Private ReadOnly _trxDates As New List(Of Date)
    Private _rows As New List(Of ReportRow)
    Private _page As Integer = 0
    Private _printIdx As Integer = 0
    Private _start As Date
    Private _end As Date

    Private ReadOnly _dtStart As AppDateBox
    Private ReadOnly _dtEnd As AppDateBox
    Private ReadOnly _cbKategori As AppComboBox
    Private ReadOnly _lblAset As Label
    Private ReadOnly _lblNilai As Label
    Private ReadOnly _lblTrx As Label
    Private ReadOnly _lblPeriode As Label
    Private ReadOnly _grid As DataGridView
    Private ReadOnly _lblFooter As Label
    Private ReadOnly _pager As Panel

    Public Sub New()
        BackColor = Theme.OffWhite
        DoubleBuffered = True
        LoadSampleData()

        Controls.Add(MakeLabel("Laporan", 26, Theme.Charcoal, 24, 22, FontStyle.Bold))
        Controls.Add(MakeLabel("Ringkasan inventaris dan transaksi dalam satu tampilan.", 14, Theme.Grey, 24, 62))
        Dim btnPdf As New AppButton() With {.Text = "Export PDF", .Kind = ButtonKind.Outline, .Location = New Point(822, 30), .Size = New Size(132, 36), .BackColor = Theme.OffWhite}
        AddHandler btnPdf.Click, AddressOf ExportPdf
        Controls.Add(btnPdf)
        Dim btnXls As New AppButton() With {.Text = "Export Excel", .Kind = ButtonKind.Primary, .Location = New Point(970, 30), .Size = New Size(132, 36), .BackColor = Theme.OffWhite}
        AddHandler btnXls.Click, AddressOf ExportExcel
        Controls.Add(btnXls)

        ' ===== Kartu filter (24,100 | 1078 x 106) =====
        Dim filter As New RoundedPanel() With {.Location = New Point(24, 100), .Size = New Size(1078, 106), .Radius = 8}
        _dtStart = New AppDateBox()
        _dtStart.Picker.CustomFormat = "dd MMM yyyy"
        _dtStart.Picker.Value = New Date(2025, 6, 1)
        _dtEnd = New AppDateBox()
        _dtEnd.Picker.CustomFormat = "dd MMM yyyy"
        _dtEnd.Picker.Value = New Date(2025, 6, 30)
        _cbKategori = New AppComboBox()
        _cbKategori.SetItems("Semua kategori", "Elektronik", "Furnitur", "ATK", "Peralatan")
        _cbKategori.SelectedIndex = 0
        AddFilterField(filter, "Tanggal mulai", _dtStart, 24, 216)
        AddFilterField(filter, "Tanggal akhir", _dtEnd, 256, 216)
        AddFilterField(filter, "Kategori", _cbKategori, 488, 254)
        Dim btnShow As New AppButton() With {.Text = "Tampilkan", .Kind = ButtonKind.Primary, .Location = New Point(758, 34), .Size = New Size(136, 36), .BackColor = Theme.OffWhite}
        AddHandler btnShow.Click, AddressOf OnShowClick
        filter.Controls.Add(btnShow)
        Controls.Add(filter)

        ' ===== 3 kartu ringkasan (y=226, tinggi 70) =====
        _lblAset = AddStat("Total aset", 24, 348)
        _lblNilai = AddStat("Nilai perolehan", 388, 348)
        _lblTrx = AddStat("Transaksi periode ini", 752, 350)

        ' ===== Kartu pratinjau (24,316 | 1078 x 356) =====
        Dim card As New RoundedPanel() With {.Location = New Point(24, 316), .Size = New Size(1078, 356), .Radius = 8}
        card.Controls.Add(MakeLabel("Pratinjau laporan aset", 18, Theme.Charcoal, 16, 19, FontStyle.Bold))
        _lblPeriode = MakeLabel("", 12, Theme.Grey, 822, 21)
        card.Controls.Add(_lblPeriode)

        _grid = New DataGridView() With {.Location = New Point(16, 60), .Size = New Size(1046, 260)}
        StyleGrid(_grid, 44)
        _grid.ColumnHeadersDefaultCellStyle.Padding = New Padding(16, 0, 0, 0)
        _grid.DefaultCellStyle.Padding = New Padding(16, 0, 0, 0)
        _grid.DefaultCellStyle.Font = AppFont(14)
        Dim zebra = Color.FromArgb(240, 239, 233)
        _grid.AlternatingRowsDefaultCellStyle.BackColor = zebra
        _grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = zebra
        Dim heads As String() = {"Kode aset", "Nama aset", "Kategori", "Lokasi", "Jumlah", "Nilai perolehan"}
        Dim widths As Integer() = {130, 290, 170, 170, 90, 196}
        For i = 0 To heads.Length - 1
            _grid.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = heads(i), .Width = widths(i), .SortMode = DataGridViewColumnSortMode.NotSortable})
        Next
        card.Controls.Add(_grid)

        _lblFooter = MakeLabel("", 12, Theme.Grey, 16, 327)
        card.Controls.Add(_lblFooter)
        _pager = New Panel() With {.Location = New Point(760, 326), .Size = New Size(302, 22), .BackColor = Theme.OffWhite}
        card.Controls.Add(_pager)
        Controls.Add(card)

        ApplyFilter()
    End Sub

    ' ---------- layout helper ----------
    Private Sub AddFilterField(card As Control, caption As String, ctl As Control, x As Integer, w As Integer)
        card.Controls.Add(MakeLabel(caption, 12, Theme.Charcoal, x, 22, FontStyle.Bold))
        ctl.Location = New Point(x, 47)
        ctl.Width = w
        card.Controls.Add(ctl)
    End Sub

    Private Function AddStat(caption As String, x As Integer, w As Integer) As Label
        Dim c As New RoundedPanel() With {.Location = New Point(x, 226), .Size = New Size(w, 70), .Radius = 8}
        c.Controls.Add(MakeLabel(caption, 12, Theme.Grey, 16, 12))
        Dim v = MakeLabel("-", 20, Theme.Charcoal, 16, 36, FontStyle.Bold)
        c.Controls.Add(v)
        Controls.Add(c)
        Return v
    End Function

    ' ---------- data ----------
    Private Sub LoadSampleData()
        ' TODO (Supabase): SELECT assets JOIN categories, locations (nilai = price * quantity).
        Dim d = Function(y As Integer, m As Integer, day As Integer) New Date(y, m, day)
        _all.Add(New ReportRow("AST-001", "Laptop Lenovo ThinkPad", "Elektronik", "Ruang IT", 12, 174000000D, d(2025, 5, 1)))
        _all.Add(New ReportRow("AST-002", "Proyektor Epson", "Elektronik", "Ruang Rapat", 3, 28500000D, d(2025, 3, 10)))
        _all.Add(New ReportRow("AST-003", "Kursi Kantor", "Furnitur", "Gudang A", 48, 72000000D, d(2025, 2, 15)))
        _all.Add(New ReportRow("AST-004", "AC Daikin 1 PK", "Elektronik", "Ruang Server", 2, 12000000D, d(2025, 4, 20)))
        _all.Add(New ReportRow("AST-005", "Printer Canon", "Elektronik", "Administrasi", 1, 3500000D, d(2025, 1, 12)))
        _all.Add(New ReportRow("BRG-006", "Kertas A4 80 gsm", "ATK", "Gudang A", 8, 640000D, d(2025, 6, 2)))
        _all.Add(New ReportRow("AST-007", "Meja Kerja", "Furnitur", "Ruang Keuangan", 24, 36000000D, d(2025, 6, 16)))
        _all.Add(New ReportRow("AST-008", "Laptop Lenovo ThinkPad", "Elektronik", "Ruang IT", 5, 72500000D, d(2025, 6, 16)))
        _all.Add(New ReportRow("AST-009", "Kamera Canon EOS", "Elektronik", "Ruang Rapat", 2, 24000000D, d(2025, 7, 5)))
        _all.Add(New ReportRow("AST-010", "Speaker Portable", "Elektronik", "Ruang Rapat", 4, 6000000D, d(2025, 2, 2)))
        _all.Add(New ReportRow("AST-011", "Map Arsip", "ATK", "Gudang B", 120, 1800000D, d(2025, 3, 22)))
        _all.Add(New ReportRow("AST-012", "Tinta Printer Epson", "ATK", "Gudang A", 16, 2400000D, d(2025, 6, 20)))

        ' TODO (Supabase): hitung dari stock_transactions + loans pada rentang tanggal.
        For Each day In {3, 5, 6, 9, 10, 12, 16, 18, 19, 20, 23, 24, 26}
            _trxDates.Add(d(2025, 6, day))
        Next
        _trxDates.Add(d(2025, 5, 28))
        _trxDates.Add(d(2025, 7, 2))
    End Sub

    Private Function Rp(v As Decimal) As String
        Return "Rp " & v.ToString("N0", _id)
    End Function

    ' ---------- filter & tampilan ----------
    Private Sub OnShowClick(sender As Object, e As EventArgs)
        If _dtEnd.Picker.Value.Date < _dtStart.Picker.Value.Date Then
            MessageBox.Show("Tanggal akhir tidak boleh sebelum tanggal mulai.", "Periode tidak valid", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        ApplyFilter()
    End Sub

    Private Sub ApplyFilter()
        _start = _dtStart.Picker.Value.Date
        _end = _dtEnd.Picker.Value.Date
        Dim kat = If(_cbKategori.SelectedIndex > 0, _cbKategori.SelectedItem, Nothing)

        ' Asumsi: laporan = posisi aset yang sudah tercatat s.d. tanggal akhir (sesuaikan bila perlu).
        _rows = _all.Where(Function(r) r.Tgl <= _end AndAlso (kat Is Nothing OrElse r.Kategori = kat)).OrderBy(Function(r) r.Kode).ToList()

        _lblAset.Text = _rows.Sum(Function(r) r.Jumlah).ToString("N0", _id) & " unit"
        _lblNilai.Text = Rp(_rows.Sum(Function(r) r.Nilai))
        _lblTrx.Text = _trxDates.Where(Function(t) t >= _start AndAlso t <= _end).Count() & " transaksi"
        _lblPeriode.Text = PeriodText()
        _page = 0
        BindPage()
    End Sub

    Private Function PeriodText() As String
        If _start.Year = _end.Year AndAlso _start.Month = _end.Month Then
            Return "Periode " & _start.ToString("dd", _id) & "–" & _end.ToString("dd MMMM yyyy", _id)
        End If
        Return "Periode " & _start.ToString("dd MMM yyyy", _id) & " – " & _end.ToString("dd MMM yyyy", _id)
    End Function

    Private Function PageCount() As Integer
        Return Math.Max(1, CInt(Math.Ceiling(_rows.Count / PageSize)))
    End Function

    Private Sub GoPage(p As Integer)
        If p < 0 OrElse p >= PageCount() Then Return
        _page = p
        BindPage()
    End Sub

    Private Sub BindPage()
        _grid.Rows.Clear()
        Dim shown = _rows.Skip(_page * PageSize).Take(PageSize).ToList()
        For Each r In shown
            _grid.Rows.Add(r.Kode, r.Nama, r.Kategori, r.Lokasi, r.Jumlah.ToString(), Rp(r.Nilai))
        Next
        _grid.ClearSelection()
        _lblFooter.Text = If(_rows.Count = 0, "Tidak ada data pada periode ini.", "Menampilkan " & shown.Count & " dari " & _rows.Count.ToString("N0", _id) & " aset")
        BuildPager()
    End Sub

    ' ---------- paginasi:  ‹ 1 2 3 … 155 › ----------
    Private Sub BuildPager()
        _pager.Controls.Clear()
        Dim pc = PageCount()
        Dim cur = _page + 1
        Dim items As New List(Of String)
        items.Add("‹")
        If pc <= 6 Then
            For i = 1 To pc
                items.Add(i.ToString())
            Next
        ElseIf cur <= 3 Then
            items.AddRange({"1", "2", "3", "…", pc.ToString()})
        ElseIf cur >= pc - 2 Then
            items.AddRange({"1", "…", (pc - 2).ToString(), (pc - 1).ToString(), pc.ToString()})
        Else
            items.AddRange({"1", "…", (cur - 1).ToString(), cur.ToString(), (cur + 1).ToString(), "…", pc.ToString()})
        End If
        items.Add("›")

        Dim x = 0
        For Each it In items
            Dim num As Integer
            Dim isNum = Integer.TryParse(it, num)
            Dim bold = isNum AndAlso num = cur
            Dim f = AppFont(12, If(bold, FontStyle.Bold, FontStyle.Regular))
            Dim w = TextRenderer.MeasureText(it, f, Size.Empty, TextFormatFlags.NoPadding).Width + 4
            Dim lbl = MakeLabel(it, 12, If(bold, Theme.Charcoal, Theme.Grey), x, 2, If(bold, FontStyle.Bold, FontStyle.Regular), w, 18)
            lbl.TextAlign = ContentAlignment.MiddleCenter
            Dim target As Integer = -1
            If isNum Then target = num - 1
            If it = "‹" Then target = _page - 1
            If it = "›" Then target = _page + 1
            If target >= 0 Then
                lbl.Cursor = Cursors.Hand
                Dim t = target
                AddHandler lbl.Click, Sub(s, ev) GoPage(t)
            End If
            _pager.Controls.Add(lbl)
            x += w + 8
        Next
    End Sub

    ' ---------- export ----------
    Private Sub ExportExcel(sender As Object, e As EventArgs)
        If _rows.Count = 0 Then
            MessageBox.Show("Tidak ada data untuk diekspor.", "Export Excel", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Using dlg As New SaveFileDialog() With {.Title = "Simpan laporan", .Filter = "File CSV (dapat dibuka di Excel)|*.csv", .FileName = "Laporan-Aset-" & _end.ToString("yyyyMMdd")}
            If dlg.ShowDialog() <> DialogResult.OK Then Return
            Dim sb As New StringBuilder()
            sb.AppendLine("sep=;")
            sb.AppendLine("Kode aset;Nama aset;Kategori;Lokasi;Jumlah;Nilai perolehan")
            For Each r In _rows
                sb.AppendLine(String.Join(";", Csv(r.Kode), Csv(r.Nama), Csv(r.Kategori), Csv(r.Lokasi), r.Jumlah.ToString(), r.Nilai.ToString("0")))
            Next
            Try
                File.WriteAllText(dlg.FileName, sb.ToString(), New UTF8Encoding(True))
                MessageBox.Show("Laporan disimpan ke:" & vbCrLf & dlg.FileName, "Export Excel", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As IOException
                MessageBox.Show("File tidak dapat disimpan (mungkin sedang terbuka di program lain)." & vbCrLf & ex.Message, "Export Excel", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Using
    End Sub

    Private Shared Function Csv(s As String) As String
        Return """" & s.Replace("""", """""") & """"
    End Function

    ''' <summary>Export PDF: buka pratinjau cetak; pilih printer "Microsoft Print to PDF" untuk menyimpan sebagai PDF.</summary>
    Private Sub ExportPdf(sender As Object, e As EventArgs)
        If _rows.Count = 0 Then
            MessageBox.Show("Tidak ada data untuk dicetak.", "Export PDF", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Using doc As New PrintDocument()
            AddHandler doc.BeginPrint, Sub(s, ev) _printIdx = 0
            AddHandler doc.PrintPage, AddressOf OnPrintPage
            Using prev As New PrintPreviewDialog() With {.Document = doc, .Width = 900, .Height = 700, .StartPosition = FormStartPosition.CenterParent}
                prev.ShowDialog(FindForm())
            End Using
        End Using
    End Sub

    Private Sub OnPrintPage(sender As Object, e As PrintPageEventArgs)
        Dim g = e.Graphics
        Dim m = e.MarginBounds
        Dim y As Single = m.Top
        Using fTitle As New Font(Theme.FontFamilyName, 16, FontStyle.Bold),
              fHead As New Font(Theme.FontFamilyName, 9, FontStyle.Bold),
              fText As New Font(Theme.FontFamilyName, 9)
            g.DrawString("Laporan Aset - AsetKu", fTitle, Brushes.Black, m.Left, y)
            y += 30
            g.DrawString(PeriodText() & "   |   Total " & _lblAset.Text & "   |   Nilai " & _lblNilai.Text, fText, Brushes.DimGray, m.Left, y)
            y += 28

            Dim xs As Single() = {0, 70, 250, 330, 430, 480}
            Dim heads As String() = {"Kode", "Nama aset", "Kategori", "Lokasi", "Jml", "Nilai perolehan"}
            For i = 0 To heads.Length - 1
                g.DrawString(heads(i), fHead, Brushes.Black, m.Left + xs(i), y)
            Next
            y += 20
            g.DrawLine(Pens.Black, m.Left, y, m.Right, y)
            y += 6

            While _printIdx < _rows.Count AndAlso y + 20 < m.Bottom
                Dim r = _rows(_printIdx)
                Dim cells As String() = {r.Kode, r.Nama, r.Kategori, r.Lokasi, r.Jumlah.ToString(), Rp(r.Nilai)}
                For i = 0 To cells.Length - 1
                    g.DrawString(cells(i), fText, Brushes.Black, m.Left + xs(i), y)
                Next
                y += 20
                _printIdx += 1
            End While
        End Using
        e.HasMorePages = _printIdx < _rows.Count
    End Sub
End Class
