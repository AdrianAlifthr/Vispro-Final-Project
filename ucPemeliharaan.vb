Imports System
Imports System.Globalization
Imports System.Text.Json
Imports System.Threading.Tasks

''' <summary>Satu jadwal pemeliharaan dari view `v_maintenances` di Supabase.</summary>
Public Class MaintRow
    Public Property Id As String
    Public Property Aset As String
    Public Property Tanggal As Date
    Public Property Biaya As Decimal
    Public Property Status As String     ' Terjadwal / Proses / Selesai
    Public Property Catatan As String = ""

    Public Sub New(aset As String, tgl As Date, biaya As Decimal, status As String, Optional id As String = "", Optional catatan As String = "")
        Me.Id = id : Me.Aset = aset : Tanggal = tgl : Me.Biaya = biaya : Me.Status = status : Me.Catatan = catatan
    End Sub
End Class

''' <summary>Halaman Pemeliharaan yang terhubung ke Supabase.</summary>
Public Class ucPemeliharaan
    Inherits UserControl

    Private Const PageSize As Integer = 5
    Private Const StatusCol As Integer = 3
    Private Const AksiCol As Integer = 4

    Private ReadOnly _id As New CultureInfo("id-ID")
    Private ReadOnly _data As New List(Of MaintRow)
    Private ReadOnly _assetIds As New Dictionary(Of String, String)()
    Private _page As Integer = 0

    Private ReadOnly _grid As DataGridView
    Private ReadOnly _lblFooter As Label
    Private ReadOnly _lblPage As Label
    Private ReadOnly _cbAset As AppComboBox
    Private ReadOnly _dtTanggal As AppDateBox
    Private ReadOnly _txtBiaya As AppTextBox
    Private ReadOnly _txtCatatan As AppTextBox
    Private ReadOnly _btnSave As AppButton

    Private Shared ReadOnly StatusFlow As String() = {"Terjadwal", "Proses", "Selesai"}

    Public Sub New()
        BackColor = Theme.OffWhite
        DoubleBuffered = True

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

        _lblFooter = MakeLabel("Memuat data dari Supabase...", 12, Theme.Grey, 16, 530)
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

        _cbAset = New AppComboBox() With {.PlaceholderText = "Memuat aset..."}
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
        _btnSave = New AppButton() With {.Text = "Simpan Jadwal", .Kind = ButtonKind.Primary, .Location = New Point(128, 510), .Size = New Size(218, 36), .BackColor = Theme.OffWhite}
        AddHandler _btnSave.Click, AddressOf SaveSchedule
        form.Controls.Add(_btnSave)
        Controls.Add(form)

        AddHandler Me.Load, AddressOf OnControlLoad
    End Sub

    Private Async Sub OnControlLoad(sender As Object, e As EventArgs)
        Await LoadAllAsync()
    End Sub

    Private Async Function LoadAllAsync() As Task
        Try
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
            _cbAset.PlaceholderText = "Pilih aset"
            _cbAset.SetItems(asetNames.ToArray())

            Dim raw = Await SupabaseClient.GetJsonAsync("/rest/v1/v_maintenances?order=maintenance_date.desc,created_at.desc")
            _data.Clear()
            Using doc = JsonDocument.Parse(raw)
                For Each el In doc.RootElement.EnumerateArray()
                    Dim id = SupabaseClient.GetStr(el, "id")
                    Dim aset = SupabaseClient.GetStr(el, "asset_name")
                    Dim dt = SupabaseClient.GetDate(el, "maintenance_date")
                    Dim cost = SupabaseClient.GetDec(el, "cost")
                    Dim st = SupabaseClient.GetStr(el, "status")
                    Dim stUi = If(st.Length > 0, Char.ToUpper(st(0)) & st.Substring(1), "Terjadwal")
                    Dim notes = SupabaseClient.GetStr(el, "notes")
                    _data.Add(New MaintRow(aset, dt, cost, stUi, id, notes))
                Next
            End Using
            _page = 0
            BindPage()
        Catch ex As Exception
            _lblFooter.Text = "Gagal memuat jadwal dari Supabase."
            MessageBox.Show("Gagal memuat jadwal pemeliharaan:" & vbCrLf & ex.Message, "Error Supabase", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

    Private Sub Place(card As Control, caption As String, ctl As Control, y As Integer)
        card.Controls.Add(MakeLabel(caption, 12, Theme.Charcoal, 20, y, FontStyle.Bold))
        ctl.Location = New Point(20, y + 25)
        ctl.Width = 326
        card.Controls.Add(ctl)
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
        Dim c As Color = ColorTranslator.FromHtml("#7B6A2C")
        If txt = "Proses" Then c = Theme.Orange
        If txt = "Selesai" Then c = Theme.StatusGreen
        DrawOutlineBadge(e.Graphics, txt, e.CellBounds.X + 8, e.CellBounds.Y + e.CellBounds.Height \ 2, c)
        e.Handled = True
    End Sub

    Private Sub GridMouseMove(sender As Object, e As DataGridViewCellMouseEventArgs)
        _grid.Cursor = If(e.RowIndex >= 0 AndAlso e.ColumnIndex = AksiCol, Cursors.Hand, Cursors.Default)
    End Sub

    Private Async Sub GridMouseClick(sender As Object, e As DataGridViewCellMouseEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex <> AksiCol Then Return
        Dim r = _data(_page * PageSize + e.RowIndex)
        Dim info = "Aset      : " & r.Aset & vbCrLf &
                   "Tanggal   : " & r.Tanggal.ToString("dd MMM yyyy", _id) & vbCrLf &
                   "Biaya     : " & Rp(r.Biaya) & vbCrLf &
                   "Status    : " & r.Status
        If r.Catatan <> "" Then info &= vbCrLf & "Catatan   : " & r.Catatan

        Dim idx = Array.IndexOf(StatusFlow, r.Status)
        If idx >= 0 AndAlso idx < StatusFlow.Length - 1 Then
            Dim nextStatus = StatusFlow(idx + 1)
            Dim ans = MessageBox.Show(info & vbCrLf & vbCrLf & "Ubah status menjadi """ & nextStatus & """?", "Detail pemeliharaan", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If ans = DialogResult.Yes Then
                Try
                    Dim payload As New Dictionary(Of String, Object) From {{"status", nextStatus.ToLower()}}
                    Await SupabaseClient.PatchJsonAsync($"/rest/v1/maintenances?id=eq.{r.Id}", payload)
                    Await LoadAllAsync()
                Catch ex As Exception
                    MessageBox.Show("Gagal memperbarui status pemeliharaan:" & vbCrLf & ex.Message, "Error Supabase", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
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

    Private Async Sub SaveSchedule(sender As Object, e As EventArgs)
        If _cbAset.SelectedItem Is Nothing Then
            MessageBox.Show("Pilih aset yang akan dirawat.", "Data belum lengkap", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim digits = DigitsIn(_txtBiaya.Inner.Text)
        Dim biaya As Decimal = If(digits = "", 0D, Decimal.Parse(digits))
        Dim notes = _txtCatatan.Inner.Text.Trim()

        Dim payload As New Dictionary(Of String, Object) From {
            {"asset_id", _assetIds(_cbAset.SelectedItem)},
            {"maintenance_date", _dtTanggal.Picker.Value.ToString("yyyy-MM-dd")},
            {"cost", biaya},
            {"notes", If(notes = "", Nothing, notes)},
            {"status", "terjadwal"}
        }
        If Not String.IsNullOrEmpty(SupabaseClient.CurrentUserId) Then
            payload("user_id") = SupabaseClient.CurrentUserId
        End If

        _btnSave.Enabled = False
        Try
            Await SupabaseClient.PostJsonAsync("maintenances", payload)
            MessageBox.Show("Jadwal pemeliharaan berhasil disimpan ke Supabase!", "Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information)
            ResetForm(sender, e)
            Await LoadAllAsync()
        Catch ex As Exception
            MessageBox.Show("Gagal menyimpan jadwal pemeliharaan:" & vbCrLf & ex.Message, "Error Supabase", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            _btnSave.Enabled = True
        End Try
    End Sub
End Class
