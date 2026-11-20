Imports System.Globalization

''' <summary>Satu jadwal pemeliharaan (nanti dari tabel maintenances).</summary>
Public Class MaintRow
    Public Property Aset As String
    Public Property Tanggal As Date
    Public Property Biaya As Decimal
    Public Property Status As String     ' Terjadwal / Proses / Selesai
    Public Property Catatan As String = ""

    Public Sub New(aset As String, tgl As Date, biaya As Decimal, status As String)
        Me.Aset = aset : Tanggal = tgl : Me.Biaya = biaya : Me.Status = status
    End Sub
End Class

''' <summary>Halaman Pemeliharaan: jadwal di kiri, form "Jadwalkan perawatan" di kanan.</summary>
Public Class ucPemeliharaan
    Inherits UserControl

    Private Const PageSize As Integer = 5
    Private Const StatusCol As Integer = 3
    Private Const AksiCol As Integer = 4

    Private ReadOnly _id As New CultureInfo("id-ID")
    Private ReadOnly _data As New List(Of MaintRow)
    Private _page As Integer = 0

    Private ReadOnly _grid As DataGridView
    Private ReadOnly _lblFooter As Label
    Private ReadOnly _lblPage As Label
    Private ReadOnly _cbAset As AppComboBox
    Private ReadOnly _dtTanggal As AppDateBox
    Private ReadOnly _txtBiaya As AppTextBox
    Private ReadOnly _txtCatatan As AppTextBox

    Private Shared ReadOnly StatusFlow As String() = {"Terjadwal", "Proses", "Selesai"}

    Public Sub New()
        BackColor = Theme.OffWhite
        DoubleBuffered = True
        LoadSampleData()

        Controls.Add(MakeLabel("Pemeliharaan", 26, Theme.Charcoal, 24, 22, FontStyle.Bold))
        Controls.Add(MakeLabel("Jadwalkan perawatan aset agar tetap siap digunakan.", 14, Theme.Grey, 24, 62))

        ' ===== Kartu kiri: jadwal (24,100 | 696 x 568) =====
        Dim card As New RoundedPanel() With {.Location = New Point(24, 100), .Size = New Size(696, 568), .Radius = 8}
        card.Controls.Add(MakeLabel("Jadwal pemeliharaan", 18, Theme.Charcoal, 16, 14, FontStyle.Bold))

        Dim wrap As New Panel() With {.Location = New Point(16, 52), .Size = New Size(664, 250), .BackColor = Theme.OffWhite}
        _grid = New DataGridView() With {.Dock = DockStyle.Fill}
        StyleGrid(_grid, 42)
        _grid.ColumnHeadersDefaultCellStyle.Padding = New Padding(10, 0, 0, 0)
        _grid.DefaultCellStyle.Padding = New Padding(10, 0, 0, 0)
        Dim heads As String() = {"Aset", "Tanggal", "Biaya", "Status", "Aksi"}
        Dim widths As Integer() = {218, 112, 110, 126, 98}
        For i = 0 To heads.Length - 1
            _grid.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = heads(i), .Width = widths(i), .SortMode = DataGridViewColumnSortMode.NotSortable})
        Next
        With _grid.Columns(0).DefaultCellStyle
            .Font = AppFont(13, FontStyle.Bold)
            .ForeColor = Theme.Charcoal
            .SelectionForeColor = Theme.Charcoal
        End With
        AddHandler _grid.CellPainting, AddressOf GridCellPainting
        AddHandler _grid.CellMouseMove, AddressOf GridMouseMove
        AddHandler _grid.CellMouseClick, AddressOf GridMouseClick
        wrap.Controls.Add(_grid)
        card.Controls.Add(wrap)

        _lblFooter = MakeLabel("", 12, Theme.Grey, 16, 530)
        card.Controls.Add(_lblFooter)
        Dim btnPrev As New AppButton() With {.Text = "Sebelumnya", .Kind = ButtonKind.Outline, .Location = New Point(412, 520), .Size = New Size(108, 36), .BackColor = Theme.OffWhite}
        AddHandler btnPrev.Click, Sub(s, e) GoPage(_page - 1)
        card.Controls.Add(btnPrev)
        _lblPage = MakeLabel("1 / 1", 12, Theme.Charcoal, 524, 530, FontStyle.Bold, 48, 18)
        _lblPage.TextAlign = ContentAlignment.MiddleCenter
        card.Controls.Add(_lblPage)
        Dim btnNext As New AppButton() With {.Text = "Berikutnya", .Kind = ButtonKind.Outline, .Location = New Point(576, 520), .Size = New Size(104, 36), .BackColor = Theme.OffWhite}
        AddHandler btnNext.Click, Sub(s, e) GoPage(_page + 1)
        card.Controls.Add(btnNext)
        Controls.Add(card)

        ' ===== Kartu kanan: form (736,100 | 366 x 568) =====
        Dim form As New RoundedPanel() With {.Location = New Point(736, 100), .Size = New Size(366, 568), .Radius = 8}
        form.Controls.Add(MakeLabel("Jadwalkan perawatan", 18, Theme.Charcoal, 20, 20, FontStyle.Bold))

        _cbAset = New AppComboBox() With {.PlaceholderText = "Pilih aset"}
        ' TODO (Supabase): isi dari tabel assets.
        _cbAset.SetItems("Proyektor Epson", "AC Daikin 1 PK", "Laptop Lenovo ThinkPad", "Printer Epson", "UPS APC 1200VA", "Printer Canon", "Kursi Kantor", "Kamera Canon EOS")
        _dtTanggal = New AppDateBox()
        _dtTanggal.Picker.CustomFormat = "d MMM yyyy"
        _txtBiaya = New AppTextBox() With {.PlaceholderText = "Rp 0"}
        _txtCatatan = New AppTextBox() With {.PlaceholderText = "Contoh: Pembersihan dan pemeriksaan rutin"}
        AddHandler _txtBiaya.Inner.KeyPress, AddressOf DigitsOnly
        AddHandler _txtBiaya.Inner.Leave, AddressOf FormatBiaya

        Place(form, "Aset", _cbAset, 63)
        Place(form, "Tanggal pemeliharaan", _dtTanggal, 143)
        Place(form, "Estimasi biaya", _txtBiaya, 223)
        Place(form, "Catatan", _txtCatatan, 303)

        form.Controls.Add(MakeLabel("Biaya aktual dapat diperbarui setelah" & vbCrLf & "pemeliharaan selesai.", 12, Theme.Grey, 20, 440, FontStyle.Regular, 326, 40))

        Dim btnCancel As New AppButton() With {.Text = "Batal", .Kind = ButtonKind.Outline, .Location = New Point(20, 510), .Size = New Size(96, 36), .BackColor = Theme.OffWhite}
        AddHandler btnCancel.Click, AddressOf ResetForm
        form.Controls.Add(btnCancel)
        Dim btnSave As New AppButton() With {.Text = "Simpan Jadwal", .Kind = ButtonKind.Primary, .Location = New Point(128, 510), .Size = New Size(218, 36), .BackColor = Theme.OffWhite}
        AddHandler btnSave.Click, AddressOf SaveSchedule
        form.Controls.Add(btnSave)
        Controls.Add(form)

        BindPage()
    End Sub

    Private Sub Place(card As Control, caption As String, ctl As Control, y As Integer)
        card.Controls.Add(MakeLabel(caption, 12, Theme.Charcoal, 20, y, FontStyle.Bold))
        ctl.Location = New Point(20, y + 25)
        ctl.Width = 326
        card.Controls.Add(ctl)
    End Sub

    ' ---------- data & paginasi ----------
    Private Sub LoadSampleData()
        ' TODO (Supabase): SELECT dari maintenances JOIN assets ORDER BY maintenance_date DESC.
        Dim d = Function(day As Integer) New Date(2025, 6, day)
        _data.Add(New MaintRow("Proyektor Epson", d(26), 250000D, "Terjadwal"))
        _data.Add(New MaintRow("AC Daikin 1 PK", d(25), 350000D, "Terjadwal"))
        _data.Add(New MaintRow("Laptop Lenovo", d(24), 150000D, "Proses"))
        _data.Add(New MaintRow("Printer Epson", d(20), 200000D, "Selesai"))
        _data.Add(New MaintRow("UPS APC 1200VA", d(18), 400000D, "Selesai"))
        _data.Add(New MaintRow("Printer Canon", d(15), 175000D, "Selesai"))
        _data.Add(New MaintRow("AC Daikin 1 PK", d(10), 300000D, "Selesai"))
        _data.Add(New MaintRow("Kursi Kantor", d(5), 80000D, "Selesai"))
    End Sub

    Private Shared Function Rp(v As Decimal) As String
        Return "Rp" & v.ToString("N0", New CultureInfo("id-ID"))
    End Function

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
            _grid.Rows.Add(r.Aset, r.Tanggal.ToString("dd MMM yyyy", _id), Rp(r.Biaya), r.Status, "Detail")
        Next
        _grid.ClearSelection()
        Dim first = skip + 1
        Dim last = Math.Min(skip + PageSize, _data.Count)
        _lblFooter.Text = If(_data.Count = 0, "Belum ada jadwal.", "Menampilkan " & first & "–" & last & " dari " & _data.Count & " data")
        _lblPage.Text = (_page + 1) & " / " & PageCount()
    End Sub

    ' ---------- tabel ----------
    Private Sub GridCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex <> StatusCol Then Return
        e.PaintBackground(e.CellBounds, False)
        Dim txt = Convert.ToString(e.Value)
        Dim c As Color = ColorTranslator.FromHtml("#7B6A2C")    ' Terjadwal
        If txt = "Proses" Then c = Theme.Orange
        If txt = "Selesai" Then c = Theme.StatusGreen
        DrawOutlineBadge(e.Graphics, txt, e.CellBounds.X + 8, e.CellBounds.Y + e.CellBounds.Height \ 2, c)
        e.Handled = True
    End Sub

    Private Sub GridMouseMove(sender As Object, e As DataGridViewCellMouseEventArgs)
        _grid.Cursor = If(e.RowIndex >= 0 AndAlso e.ColumnIndex = AksiCol, Cursors.Hand, Cursors.Default)
    End Sub

    Private Sub GridMouseClick(sender As Object, e As DataGridViewCellMouseEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex <> AksiCol Then Return
        Dim r = _data(_page * PageSize + e.RowIndex)
        Dim info = "Aset      : " & r.Aset & vbCrLf &
                   "Tanggal   : " & r.Tanggal.ToString("dd MMM yyyy", _id) & vbCrLf &
                   "Biaya     : " & Rp(r.Biaya) & vbCrLf &
                   "Status    : " & r.Status
        If r.Catatan <> "" Then info &= vbCrLf & "Catatan   : " & r.Catatan

        ' Catatan: opsi memajukan status ini tambahan di luar desain Figma.
        Dim idx = Array.IndexOf(StatusFlow, r.Status)
        If idx >= 0 AndAlso idx < StatusFlow.Length - 1 Then
            Dim nextStatus = StatusFlow(idx + 1)
            Dim ans = MessageBox.Show(info & vbCrLf & vbCrLf & "Ubah status menjadi """ & nextStatus & """?", "Detail pemeliharaan", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If ans = DialogResult.Yes Then
                ' TODO (Supabase): UPDATE maintenances SET status = ... (dan assets.status = 'maintenance' / 'tersedia')
                r.Status = nextStatus
                BindPage()
            End If
        Else
            MessageBox.Show(info, "Detail pemeliharaan", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    ' ---------- form ----------
    Private Sub DigitsOnly(sender As Object, e As KeyPressEventArgs)
        If Not Char.IsControl(e.KeyChar) AndAlso Not Char.IsDigit(e.KeyChar) AndAlso e.KeyChar <> "."c Then e.Handled = True
    End Sub

    Private Shared Function DigitsIn(s As String) As String
        Return New String(s.Where(Function(c) Char.IsDigit(c)).ToArray())
    End Function

    Private Sub FormatBiaya(sender As Object, e As EventArgs)
        Dim digits = DigitsIn(_txtBiaya.Inner.Text)
        _txtBiaya.Inner.Text = If(digits = "", "", "Rp " & Decimal.Parse(digits).ToString("N0", _id))
    End Sub

    Private Sub ResetForm(sender As Object, e As EventArgs)
        _cbAset.SelectedIndex = -1
        _dtTanggal.Picker.Value = Date.Today
        _txtBiaya.Inner.Text = ""
        _txtCatatan.Inner.Text = ""
    End Sub

    Private Sub SaveSchedule(sender As Object, e As EventArgs)
        If _cbAset.SelectedItem Is Nothing Then
            MessageBox.Show("Pilih aset yang akan dirawat.", "Data belum lengkap", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim digits = DigitsIn(_txtBiaya.Inner.Text)
        Dim biaya As Decimal = If(digits = "", 0D, Decimal.Parse(digits))

        ' TODO (Supabase): INSERT INTO maintenances (asset_id, user_id, maintenance_date, cost, notes, status).
        Dim row As New MaintRow(_cbAset.SelectedItem, _dtTanggal.Picker.Value.Date, biaya, "Terjadwal") With {.Catatan = _txtCatatan.Inner.Text.Trim()}
        _data.Add(row)
        _data.Sort(Function(a, b) b.Tanggal.CompareTo(a.Tanggal))
        _page = 0
        BindPage()
        ResetForm(sender, e)
        MessageBox.Show("Jadwal pemeliharaan tersimpan (mode demo).", "Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub
End Class
