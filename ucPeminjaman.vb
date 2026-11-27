Imports System
Imports System.Globalization
Imports System.Text.Json
Imports System.Threading.Tasks

''' <summary>Satu data peminjaman dari view `v_loans` di Supabase.</summary>
Public Class LoanRow
    Public Property Id As String
    Public Property No As String
    Public Property Peminjam As String
    Public Property Aset As String
    Public Property TglPinjam As Date
    Public Property TglKembali As Date
    Public Property Status As String      ' Dipinjam / Terlambat / Dikembalikan
    Public Property Catatan As String = ""
    Public Property RequestId As String = ""

    Public Sub New(no As String, peminjam As String, aset As String, pinjam As Date, kembali As Date, status As String, Optional id As String = "", Optional catatan As String = "")
        Me.Id = id : Me.No = no : Me.Peminjam = peminjam : Me.Aset = aset
        TglPinjam = pinjam : TglKembali = kembali : Me.Status = status : Me.Catatan = catatan
    End Sub
End Class

''' <summary>Kartu ringkasan kecil: angka besar + judul + keterangan.</summary>
Public Class LoanStat
    Inherits RoundedPanel

    Private ReadOnly _lblValue As Label

    Public Sub New(title As String, note As String)
        Size = New Size(348, 76)
        Radius = 8
        _lblValue = MakeLabel("0", 26, Theme.Charcoal, 16, 20, FontStyle.Bold)
        Controls.Add(_lblValue)
        Controls.Add(MakeLabel(title, 14, Theme.Charcoal, 64, 14, FontStyle.Bold))
        Controls.Add(MakeLabel(note, 12, Theme.Grey, 64, 36))
    End Sub

    Public WriteOnly Property Value As Integer
        Set(v As Integer)
            _lblValue.Text = v.ToString()
        End Set
    End Property
End Class

''' <summary>Halaman Peminjaman Aset yang terhubung ke Supabase.</summary>
Public Class ucPeminjaman
    Inherits UserControl

    Private Const PageSize As Integer = 6
    Private Const StatusCol As Integer = 5
    Private Const AksiCol As Integer = 6

    Private ReadOnly _id As New CultureInfo("id-ID")
    Private ReadOnly _user As String
    Private ReadOnly _all As New List(Of LoanRow)
    Private _view As New List(Of LoanRow)
    Private _page As Integer = 0

    Private ReadOnly _statBorrowed As LoanStat
    Private ReadOnly _statLate As LoanStat
    Private ReadOnly _statReturned As LoanStat
    Private ReadOnly _txtSearch As AppTextBox
    Private ReadOnly _cbStatus As AppComboBox
    Private ReadOnly _cbBulan As AppComboBox
    Private ReadOnly _grid As DataGridView
    Private ReadOnly _lblFooter As Label
    Private ReadOnly _lblPage As Label

    Public Sub New(userName As String)
        _user = userName
        BackColor = Theme.OffWhite
        DoubleBuffered = True

        ' ===== Judul + tombol =====
        Controls.Add(MakeLabel("Peminjaman Aset", 26, Theme.Charcoal, 24, 22, FontStyle.Bold))
        Controls.Add(MakeLabel("Kelola peminjaman, tenggat pengembalian, dan ketersediaan aset.", 14, Theme.Grey, 24, 62))
        Dim btnAdd As New AppButton() With {.Text = "+ Pinjam Aset", .Location = New Point(946, 24), .Size = New Size(156, 36), .BackColor = Theme.OffWhite}
        AddHandler btnAdd.Click, AddressOf OnPinjamClick
        Controls.Add(btnAdd)

        ' ===== 3 kartu ringkasan (y=100) =====
        _statBorrowed = New LoanStat("Sedang dipinjam", "aset aktif") With {.Location = New Point(24, 100)}
        _statLate = New LoanStat("Terlambat", "perlu ditindaklanjuti") With {.Location = New Point(389, 100)}
        _statReturned = New LoanStat("Dikembalikan", "transaksi selesai") With {.Location = New Point(754, 100)}
        Controls.Add(_statBorrowed)
        Controls.Add(_statLate)
        Controls.Add(_statReturned)

        ' ===== Baris filter (y=196) =====
        _txtSearch = New AppTextBox() With {.Location = New Point(24, 196), .Width = 480, .PlaceholderText = "Cari peminjam atau aset..."}
        _cbStatus = New AppComboBox() With {.Location = New Point(520, 196), .Width = 200}
        _cbBulan = New AppComboBox() With {.Location = New Point(736, 196), .Width = 200}
        _cbStatus.SetItems("Semua status", "Dipinjam", "Terlambat", "Dikembalikan")
        _cbBulan.SetItems("Semua bulan")
        _cbStatus.SelectedIndex = 0
        _cbBulan.SelectedIndex = 0
        Controls.Add(_txtSearch)
        Controls.Add(_cbStatus)
        Controls.Add(_cbBulan)
        Dim btnRefresh As New AppButton() With {.Text = "Refresh", .Kind = ButtonKind.Outline, .Location = New Point(952, 196), .Size = New Size(150, 36), .BackColor = Theme.OffWhite}
        AddHandler btnRefresh.Click, AddressOf OnRefreshClick
        Controls.Add(btnRefresh)

        ' ===== Kartu daftar (24,252 | 1078 x 428) =====
        Dim card As New RoundedPanel() With {.Location = New Point(24, 252), .Size = New Size(1078, 428), .Radius = 8}
        card.Controls.Add(MakeLabel("Daftar peminjaman", 18, Theme.Charcoal, 16, 14, FontStyle.Bold))

        Dim wrap As New Panel() With {.Location = New Point(16, 52), .Size = New Size(1046, 292), .BackColor = Theme.OffWhite}
        _grid = New DataGridView() With {.Dock = DockStyle.Fill}
        StyleGrid(_grid, 42)
        _grid.ColumnHeadersDefaultCellStyle.Padding = New Padding(10, 0, 0, 0)
        _grid.DefaultCellStyle.Padding = New Padding(10, 0, 0, 0)
        Dim heads As String() = {"No. Pinjam", "Peminjam", "Aset", "Tgl. Pinjam", "Tgl. Kembali", "Status", "Aksi"}
        Dim widths As Integer() = {126, 166, 232, 118, 118, 152, 134}
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

        _lblFooter = MakeLabel("Memuat data dari Supabase...", 12, Theme.Grey, 16, 386)
        card.Controls.Add(_lblFooter)
        Dim btnPrev As New AppButton() With {.Text = "Sebelumnya", .Kind = ButtonKind.Outline, .Location = New Point(790, 376), .Size = New Size(108, 36), .BackColor = Theme.OffWhite}
        AddHandler btnPrev.Click, Sub(s, e) GoPage(_page - 1)
        card.Controls.Add(btnPrev)
        _lblPage = MakeLabel("1 / 1", 12, Theme.Charcoal, 904, 386, FontStyle.Bold, 48, 18)
        _lblPage.TextAlign = ContentAlignment.MiddleCenter
        card.Controls.Add(_lblPage)
        Dim btnNext As New AppButton() With {.Text = "Berikutnya", .Kind = ButtonKind.Outline, .Location = New Point(958, 376), .Size = New Size(104, 36), .BackColor = Theme.OffWhite}
        AddHandler btnNext.Click, Sub(s, e) GoPage(_page + 1)
        card.Controls.Add(btnNext)
        Controls.Add(card)

        AddHandler _txtSearch.Inner.TextChanged, AddressOf OnFilterChanged
        AddHandler _cbStatus.SelectedIndexChanged, AddressOf OnFilterChanged
        AddHandler _cbBulan.SelectedIndexChanged, AddressOf OnFilterChanged
        AddHandler Me.Load, AddressOf OnControlLoad
    End Sub

    Private Async Sub OnControlLoad(sender As Object, e As EventArgs)
        Await LoadFromSupabaseAsync()
    End Sub

    Private Async Sub OnRefreshClick(sender As Object, e As EventArgs)
        Await LoadFromSupabaseAsync()
    End Sub

    Private Async Function LoadFromSupabaseAsync() As Task
        Try
            Dim raw = Await SupabaseClient.GetJsonAsync("/rest/v1/v_loans?order=loan_date.desc,created_at.desc")
            _all.Clear()
            Using doc = JsonDocument.Parse(raw)
                For Each el In doc.RootElement.EnumerateArray()
                    Dim id = SupabaseClient.GetStr(el, "id")
                    Dim no = SupabaseClient.GetStr(el, "loan_no")
                    Dim borrower = SupabaseClient.GetStr(el, "borrower_name")
                    Dim aset = SupabaseClient.GetStr(el, "asset_name")
                    Dim tPinjam = SupabaseClient.GetDate(el, "loan_date")
                    Dim retStr = SupabaseClient.GetStr(el, "return_date")
                    Dim tKembali = If(retStr <> "", SupabaseClient.GetDate(el, "return_date"), SupabaseClient.GetDate(el, "due_date"))
                    Dim stDb = SupabaseClient.GetStr(el, "effective_status")
                    Dim stUi = FormatLoanStatus(stDb)
                    Dim notes = SupabaseClient.GetStr(el, "notes")
                    Dim row = New LoanRow(no, borrower, aset, tPinjam, tKembali, stUi, id, notes)
                    row.RequestId = SupabaseClient.GetStr(el, "request_id")
                    _all.Add(row)
                Next
            End Using

            Dim months = New List(Of String) From {"Semua bulan"}
            months.AddRange(_all.Select(Function(x) x.TglPinjam.ToString("MMMM yyyy", _id)).Distinct())
            _cbBulan.SetItems(months.ToArray())
            _cbBulan.SelectedIndex = 0
            ApplyFilter()
        Catch ex As Exception
            _lblFooter.Text = "Gagal memuat peminjaman dari Supabase."
            MessageBox.Show("Gagal memuat data peminjaman:" & vbCrLf & ex.Message, "Error Supabase", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

    Private Shared Function FormatLoanStatus(dbVal As String) As String
        Select Case dbVal
            Case "dipinjam" : Return "Dipinjam"
            Case "terlambat" : Return "Terlambat"
            Case "dikembalikan" : Return "Dikembalikan"
            Case Else : Return dbVal
        End Select
    End Function

    Private Function NextNo() As String
        Return "PJM-" & Date.Now.ToString("MMyy") & "-" & (_all.Count + 1).ToString("000")
    End Function

    ' ---------- filter, statistik, paginasi ----------
    Private Sub OnFilterChanged(sender As Object, e As EventArgs)
        ApplyFilter()
    End Sub

    Private Sub ApplyFilter()
        If _grid Is Nothing OrElse _cbBulan Is Nothing OrElse _cbBulan.SelectedItem Is Nothing OrElse _cbStatus.SelectedItem Is Nothing Then Return
        Dim q = _txtSearch.Inner.Text.Trim().ToLower()
        Dim sta = If(_cbStatus.SelectedIndex > 0, _cbStatus.SelectedItem, Nothing)
        Dim bln = If(_cbBulan.SelectedIndex > 0, _cbBulan.SelectedItem, Nothing)

        _view = _all.Where(Function(r)
                               If q <> "" AndAlso Not (r.No.ToLower().Contains(q) OrElse r.Peminjam.ToLower().Contains(q) OrElse r.Aset.ToLower().Contains(q)) Then Return False
                               If sta IsNot Nothing AndAlso r.Status <> sta Then Return False
                               If bln IsNot Nothing AndAlso r.TglPinjam.ToString("MMMM yyyy", _id) <> bln Then Return False
                               Return True
                           End Function).ToList()
        _page = 0
        BindPage()
        UpdateStats(bln)
    End Sub

    Private Sub UpdateStats(bulan As String)
        _statBorrowed.Value = _all.Where(Function(r) r.Status <> "Dikembalikan").Count()
        _statLate.Value = _all.Where(Function(r) r.Status = "Terlambat").Count()
        _statReturned.Value = _all.Where(Function(r) r.Status = "Dikembalikan" AndAlso (bulan Is Nothing OrElse r.TglKembali.ToString("MMMM yyyy", _id) = bulan)).Count()
    End Sub

    Private Function PageCount() As Integer
        Return Math.Max(1, CInt(Math.Ceiling(_view.Count / PageSize)))
    End Function

    Private Sub GoPage(p As Integer)
        If p < 0 OrElse p >= PageCount() Then Return
        _page = p
        BindPage()
    End Sub

    Private Sub BindPage()
        _grid.Rows.Clear()
        Dim skip = _page * PageSize
        For Each r In _view.Skip(skip).Take(PageSize)
            _grid.Rows.Add(r.No, r.Peminjam, r.Aset, r.TglPinjam.ToString("dd MMM yyyy", _id), r.TglKembali.ToString("dd MMM yyyy", _id), r.Status, If(r.Status = "Dikembalikan", "Detail", "Kembalikan"))
        Next
        _grid.ClearSelection()
        Dim first = skip + 1
        Dim last = Math.Min(skip + PageSize, _view.Count)
        _lblFooter.Text = If(_view.Count = 0, "Belum ada data peminjaman.", "Menampilkan " & first & "–" & last & " dari " & _view.Count & " peminjaman")
        _lblPage.Text = (_page + 1) & " / " & PageCount()
    End Sub

    ' ---------- tabel ----------
    Private Sub GridCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex <> StatusCol Then Return
        e.PaintBackground(e.CellBounds, False)
        Dim txt = Convert.ToString(e.Value)
        Dim c As Color = Theme.Orange
        If txt = "Terlambat" Then c = Theme.Danger
        If txt = "Dikembalikan" Then c = Theme.StatusGreen
        DrawOutlineBadge(e.Graphics, txt, e.CellBounds.X + 8, e.CellBounds.Y + e.CellBounds.Height \ 2, c)
        e.Handled = True
    End Sub

    Private Sub GridMouseMove(sender As Object, e As DataGridViewCellMouseEventArgs)
        _grid.Cursor = If(e.RowIndex >= 0 AndAlso e.ColumnIndex = AksiCol, Cursors.Hand, Cursors.Default)
    End Sub

    Private Sub GridMouseClick(sender As Object, e As DataGridViewCellMouseEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex <> AksiCol Then Return
        Dim r = _view(_page * PageSize + e.RowIndex)
        ShowDetail(r)
    End Sub

    Private Async Sub ShowDetail(r As LoanRow)
        Dim info = "No. Pinjam    : " & r.No & vbCrLf &
                   "Peminjam      : " & r.Peminjam & vbCrLf &
                   "Aset          : " & r.Aset & vbCrLf &
                   "Tgl. Pinjam   : " & r.TglPinjam.ToString("dd MMM yyyy", _id) & vbCrLf &
                   "Tgl. Kembali  : " & r.TglKembali.ToString("dd MMM yyyy", _id) & vbCrLf &
                   "Status        : " & r.Status
        If r.Catatan <> "" Then info &= vbCrLf & "Keperluan     : " & r.Catatan

        If r.Status = "Dikembalikan" OrElse r.RequestId <> "" Then
            If r.RequestId <> "" AndAlso r.Status <> "Dikembalikan" Then info &= vbCrLf & vbCrLf & "Pengembalian diajukan user dan diverifikasi di menu Verifikasi Pengajuan."
            MessageBox.Show(info, "Detail peminjaman", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Else
            Dim ans = MessageBox.Show(info & vbCrLf & vbCrLf & "Tandai aset ini sudah dikembalikan?", "Detail peminjaman", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If ans = DialogResult.Yes Then
                Try
                    Dim payload As New Dictionary(Of String, Object) From {
                        {"status", "dikembalikan"},
                        {"return_date", Date.Today.ToString("yyyy-MM-dd")}
                    }
                    Await SupabaseClient.PatchJsonAsync($"/rest/v1/loans?id=eq.{r.Id}", payload)
                    Await LoadFromSupabaseAsync()
                Catch ex As Exception
                    MessageBox.Show("Gagal memperbarui status peminjaman:" & vbCrLf & ex.Message, "Error Supabase", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End If
    End Sub

    ' ---------- aksi ----------
    Private Async Sub OnPinjamClick(sender As Object, e As EventArgs)
        MessageBox.Show("Peminjaman dibuat oleh user divisi dan dimulai setelah admin mencatat penyerahan.", "Gunakan Verifikasi Pengajuan")
        Return
        Dim owner = FindForm()
        If owner Is Nothing Then Return

        Dim peminjamList As New List(Of String)()
        Dim asetMap As New Dictionary(Of String, String)()
        Dim asetList As New List(Of String)()

        Try
            Dim usrJson = Await SupabaseClient.GetJsonAsync("/rest/v1/users?select=full_name&is_active=eq.true&order=full_name.asc")
            Using doc = JsonDocument.Parse(usrJson)
                For Each el In doc.RootElement.EnumerateArray()
                    Dim nm = SupabaseClient.GetStr(el, "full_name")
                    If nm <> "" AndAlso Not peminjamList.Contains(nm) Then peminjamList.Add(nm)
                Next
            End Using

            Dim asetJson = Await SupabaseClient.GetJsonAsync("/rest/v1/assets?select=id,asset_code,name&status=eq.tersedia&order=name.asc")
            Using doc = JsonDocument.Parse(asetJson)
                For Each el In doc.RootElement.EnumerateArray()
                    Dim label = SupabaseClient.GetStr(el, "name") & " (" & SupabaseClient.GetStr(el, "asset_code") & ")"
                    If Not asetMap.ContainsKey(label) Then
                        asetMap.Add(label, SupabaseClient.GetStr(el, "id"))
                        asetList.Add(label)
                    End If
                Next
            End Using
        Catch ex As Exception
            MessageBox.Show("Gagal memuat daftar peminjam/aset dari Supabase:" & vbCrLf & ex.Message, "Error Supabase", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End Try

        If asetList.Count = 0 Then
            MessageBox.Show("Tidak ada aset dengan status Tersedia untuk dipinjam.", "Aset kosong", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using dlg As New DlgPinjamAset(peminjamList.ToArray(), asetList.ToArray())
            If ShowDialogOver(owner, New Rectangle(240, 64, 1126, 704), dlg) <> DialogResult.OK Then Return
            Try
                Dim payload As New Dictionary(Of String, Object) From {
                    {"loan_no", NextNo()},
                    {"asset_id", asetMap(dlg.Aset)},
                    {"borrower_name", dlg.Peminjam},
                    {"loan_date", dlg.TglPinjam.ToString("yyyy-MM-dd")},
                    {"due_date", dlg.TglKembali.ToString("yyyy-MM-dd")},
                    {"status", "dipinjam"},
                    {"notes", If(dlg.Catatan = "", Nothing, dlg.Catatan)}
                }
                If Not String.IsNullOrEmpty(SupabaseClient.CurrentUserId) Then
                    payload("user_id") = SupabaseClient.CurrentUserId
                End If
                Await SupabaseClient.PostJsonAsync("loans", payload)
                Await LoadFromSupabaseAsync()
            Catch ex As Exception
                MessageBox.Show("Gagal menyimpan peminjaman ke Supabase:" & vbCrLf & ex.Message, "Error Supabase", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub
End Class

' =====================================================================
' DIALOG "PINJAM ASET"  (560 x 595)
' =====================================================================
Public Class DlgPinjamAset
    Inherits Form

    Private ReadOnly _cbPeminjam As AppComboBox
    Private ReadOnly _cbAset As AppComboBox
    Private ReadOnly _dtPinjam As AppDateBox
    Private ReadOnly _dtKembali As AppDateBox
    Private ReadOnly _txtCatatan As AppTextBox

    Public ReadOnly Property Peminjam As String
        Get
            Return _cbPeminjam.SelectedItem
        End Get
    End Property

    Public ReadOnly Property Aset As String
        Get
            Return _cbAset.SelectedItem
        End Get
    End Property

    Public ReadOnly Property TglPinjam As Date
        Get
            Return _dtPinjam.Picker.Value.Date
        End Get
    End Property

    Public ReadOnly Property TglKembali As Date
        Get
            Return _dtKembali.Picker.Value.Date
        End Get
    End Property

    Public ReadOnly Property Catatan As String
        Get
            Return _txtCatatan.Inner.Text.Trim()
        End Get
    End Property

    Public Sub New(peminjam As String(), aset As String())
        FormBorderStyle = FormBorderStyle.None
        ShowInTaskbar = False
        ClientSize = New Size(560, 595)
        BackColor = Theme.OffWhite
        Region = New Region(RoundedRect(New Rectangle(0, 0, Width, Height), 10))

        Controls.Add(MakeLabel("Pinjam Aset", 24, Theme.Charcoal, 24, 22, FontStyle.Bold))
        Dim lblClose = MakeLabel("×", 26, Theme.Charcoal, 508, 18)
        lblClose.Cursor = Cursors.Hand
        AddHandler lblClose.Click, AddressOf OnCloseClick
        Controls.Add(lblClose)
        Controls.Add(MakeLabel("Lengkapi data peminjaman aset di bawah ini.", 14, Theme.Grey, 24, 58))

        _cbPeminjam = New AppComboBox() With {.PlaceholderText = "Pilih nama peminjam"}
        _cbPeminjam.SetItems(peminjam)
        _cbAset = New AppComboBox() With {.PlaceholderText = "Pilih aset yang tersedia"}
        _cbAset.SetItems(aset)
        _dtPinjam = New AppDateBox()
        _dtPinjam.Picker.CustomFormat = "d MMM yyyy"
        _dtKembali = New AppDateBox()
        _dtKembali.Picker.CustomFormat = "d MMM yyyy"
        _dtKembali.Picker.Value = Date.Today.AddDays(3)
        _txtCatatan = New AppTextBox() With {.PlaceholderText = "Contoh: Presentasi klien di Ruang Rapat"}

        Place("Peminjam", _cbPeminjam, 24, 104, 512)
        Place("Aset", _cbAset, 24, 184, 512)
        Place("Tanggal pinjam", _dtPinjam, 24, 264, 248)
        Place("Rencana kembali", _dtKembali, 288, 264, 248)
        Place("Keperluan / Catatan", _txtCatatan, 24, 344, 512)

        Controls.Add(MakeLabel("Pastikan kondisi aset sudah diperiksa sebelum diserahkan.", 12, Theme.Grey, 24, 450))

        Dim btnCancel As New AppButton() With {.Text = "Batal", .Kind = ButtonKind.Outline, .Location = New Point(24, 527), .Size = New Size(152, 36), .DialogResult = DialogResult.Cancel}
        Dim btnOk As New AppButton() With {.Text = "Pinjam Aset", .Kind = ButtonKind.Primary, .Location = New Point(188, 527), .Size = New Size(348, 36)}
        AddHandler btnOk.Click, AddressOf OnOkClick
        Controls.Add(btnCancel)
        Controls.Add(btnOk)
        CancelButton = btnCancel
    End Sub

    Private Sub Place(caption As String, ctl As Control, x As Integer, y As Integer, w As Integer)
        Controls.Add(MakeLabel(caption, 12, Theme.Charcoal, x, y, FontStyle.Bold))
        ctl.Location = New Point(x, y + 25)
        ctl.Width = w
        Controls.Add(ctl)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Using p As New Pen(Theme.Beige, 1)
            e.Graphics.DrawLine(p, 24, 495, 536, 495)
        End Using
    End Sub

    Private Sub OnCloseClick(sender As Object, e As EventArgs)
        DialogResult = DialogResult.Cancel
    End Sub

    Private Sub OnOkClick(sender As Object, e As EventArgs)
        Dim missing As New List(Of String)
        If _cbPeminjam.SelectedItem Is Nothing Then missing.Add("Peminjam")
        If _cbAset.SelectedItem Is Nothing Then missing.Add("Aset")
        If missing.Count > 0 Then
            MessageBox.Show("Lengkapi data berikut:" & vbCrLf & "- " & String.Join(vbCrLf & "- ", missing), "Data belum lengkap", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If TglKembali < TglPinjam Then
            MessageBox.Show("Rencana kembali tidak boleh sebelum tanggal pinjam.", "Tanggal tidak valid", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        DialogResult = DialogResult.OK
    End Sub
End Class
