Imports System
Imports System.Text.Json
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks

''' <summary>Satu pengguna aplikasi dari tabel `users` di Supabase.</summary>
Public Class UserRow
    Public Property DbId As String
    Public Property Id As String
    Public Property Nama As String
    Public Property Email As String
    Public Property Username As String
    Public Property Role As String        ' Admin / Staff (Viewer lama hanya ditampilkan)
    Public Property DivisionId As String = ""
    Public Property DivisionName As String = ""
    Public Property Aktif As Boolean = True

    Public Sub New(id As String, nama As String, email As String, username As String, role As String, aktif As Boolean, Optional dbId As String = "")
        Me.DbId = dbId : Me.Id = id : Me.Nama = nama : Me.Email = email : Me.Username = username : Me.Role = role : Me.Aktif = aktif
    End Sub
End Class

''' <summary>Halaman Manajemen User yang terhubung ke Supabase.</summary>
Public Class ucUser
    Inherits UserControl

    Private Const PageSize As Integer = 8
    Private Const RoleCol As Integer = 4
    Private Const StatusCol As Integer = 5
    Private Const AksiCol As Integer = 6

    Private ReadOnly _all As New List(Of UserRow)
    Private _view As New List(Of UserRow)
    Private _page As Integer = 0

    Private ReadOnly _txtSearch As AppTextBox
    Private ReadOnly _cbRole As AppComboBox
    Private ReadOnly _cbStatus As AppComboBox
    Private ReadOnly _cbDivision As AppComboBox
    Private ReadOnly _lblTotal As Label
    Private ReadOnly _grid As DataGridView
    Private ReadOnly _lblFooter As Label
    Private ReadOnly _lblPage As Label

    Public Sub New()
        BackColor = Theme.OffWhite
        DoubleBuffered = True

        Controls.Add(MakeLabel("Manajemen User", 26, Theme.Charcoal, 24, 22, FontStyle.Bold))
        Controls.Add(MakeLabel("Atur akses pengguna sesuai peran dan tanggung jawab.", 14, Theme.Grey, 24, 62))
        Dim btnAdd As New AppButton() With {.Text = "+ Tambah User", .Location = New Point(950, 30), .Size = New Size(152, 36), .BackColor = Theme.OffWhite}
        AddHandler btnAdd.Click, AddressOf OnAddClick
        Controls.Add(btnAdd)

        ' ===== Filter (y=102) =====
        _txtSearch = New AppTextBox() With {.Location = New Point(24, 102), .Width = 300, .PlaceholderText = "Cari nama, email, atau username..."}
        _cbDivision = New AppComboBox() With {.Location = New Point(338, 102), .Width = 210}
        _cbRole = New AppComboBox() With {.Location = New Point(562, 102), .Width = 160}
        _cbStatus = New AppComboBox() With {.Location = New Point(736, 102), .Width = 160}
        _cbDivision.SetItems("Semua divisi")
        _cbRole.SetItems("Semua role", "Admin", "Staff")
        _cbStatus.SetItems("Semua status", "Aktif", "Nonaktif")
        _cbRole.SelectedIndex = 0 : _cbStatus.SelectedIndex = 0
        Controls.Add(_txtSearch)
        Controls.Add(_cbDivision)
        Controls.Add(_cbRole)
        Controls.Add(_cbStatus)
        _lblTotal = MakeLabel("Memuat...", 12, Theme.Grey, 912, 112)
        Controls.Add(_lblTotal)

        ' ===== Kartu tabel (24,164 | 1078 x 452) =====
        Dim card As New RoundedPanel() With {.Location = New Point(24, 164), .Size = New Size(1078, 452), .Radius = 8}
        _grid = New DataGridView() With {.Location = New Point(1, 1), .Size = New Size(1076, 392)}
        StyleGrid(_grid, 44)
        _grid.ColumnHeadersDefaultCellStyle.Padding = New Padding(16, 0, 0, 0)
        _grid.DefaultCellStyle.Padding = New Padding(16, 0, 0, 0)
        _grid.DefaultCellStyle.Font = AppFont(14)
        Dim zebra = Color.FromArgb(240, 239, 233)
        _grid.AlternatingRowsDefaultCellStyle.BackColor = zebra
        _grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = zebra
        Dim heads As String() = {"ID user", "Nama lengkap", "Email", "Divisi", "Role", "Status", "Aksi"}
        Dim widths As Integer() = {104, 180, 205, 174, 104, 112, 197}
        For i = 0 To heads.Length - 1
            _grid.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = heads(i), .Width = widths(i), .SortMode = DataGridViewColumnSortMode.NotSortable})
        Next
        _grid.Columns(1).DefaultCellStyle.ForeColor = Theme.Charcoal
        _grid.Columns(1).DefaultCellStyle.SelectionForeColor = Theme.Charcoal
        AddHandler _grid.CellPainting, AddressOf GridCellPainting
        AddHandler _grid.CellMouseMove, AddressOf GridMouseMove
        AddHandler _grid.CellMouseClick, AddressOf GridMouseClick
        card.Controls.Add(_grid)

        _lblFooter = MakeLabel("Memuat pengguna dari Supabase...", 12, Theme.Grey, 16, 417)
        card.Controls.Add(_lblFooter)
        Dim prev = MakeLabel("‹", 14, Theme.Grey, 968, 414)
        prev.Cursor = Cursors.Hand
        AddHandler prev.Click, Sub(s, e) GoPage(_page - 1)
        card.Controls.Add(prev)
        _lblPage = MakeLabel("1", 12, Theme.Charcoal, 982, 417, FontStyle.Regular, 24, 18)
        _lblPage.TextAlign = ContentAlignment.MiddleCenter
        card.Controls.Add(_lblPage)
        Dim nxt = MakeLabel("›", 14, Theme.Grey, 1004, 414)
        nxt.Cursor = Cursors.Hand
        AddHandler nxt.Click, Sub(s, e) GoPage(_page + 1)
        card.Controls.Add(nxt)
        Controls.Add(card)

        ' ===== Bar legenda role (24,636 | 1078 x 44) =====
        Dim bar As New Panel() With {.Location = New Point(24, 636), .Size = New Size(1078, 44), .BackColor = Theme.Beige}
        bar.Controls.Add(MakeLabel("Admin: kelola inventaris dan verifikasi     •     Staff: ajukan kebutuhan divisi", 12, Theme.Grey, 16, 13))
        Controls.Add(bar)

        AddHandler _txtSearch.Inner.TextChanged, AddressOf OnFilterChanged
        AddHandler _cbRole.SelectedIndexChanged, AddressOf OnFilterChanged
        AddHandler _cbDivision.SelectedIndexChanged, AddressOf OnFilterChanged
        AddHandler _cbStatus.SelectedIndexChanged, AddressOf OnFilterChanged
        AddHandler Me.Load, AddressOf OnControlLoad
    End Sub

    Private Async Sub OnControlLoad(sender As Object, e As EventArgs)
        Await LoadFromSupabaseAsync()
    End Sub

    Private Async Function LoadFromSupabaseAsync() As Task
        Try
            If SupabaseClient.IsDemoSession Then
                _all.Clear()
                For Each u In DemoData.Users()
                    Dim row = New UserRow(u.Code, u.Name, u.Email, u.Email.Split("@"c)(0), "Staff", u.Active, u.Id)
                    row.DivisionId = u.DivisionId
                    row.DivisionName = u.DivisionName
                    _all.Add(row)
                Next
                RefreshDivisionFilter()
                ApplyFilter()
                Return
            End If
            Dim divisionNames As New Dictionary(Of String, String)()
            Try
                Dim divisionsJson = Await SupabaseClient.GetJsonAsync("/rest/v1/divisions?select=id,name")
                Using divisionsDoc = JsonDocument.Parse(divisionsJson)
                    For Each division In divisionsDoc.RootElement.EnumerateArray()
                        divisionNames(SupabaseClient.GetStr(division, "id")) = SupabaseClient.GetStr(division, "name")
                    Next
                End Using
            Catch
                ' Database lama mungkin belum memiliki tabel divisi.
            End Try
            Dim raw = Await SupabaseClient.GetJsonAsync("/rest/v1/users?order=user_code.asc")
            _all.Clear()
            Using doc = JsonDocument.Parse(raw)
                For Each el In doc.RootElement.EnumerateArray()
                    Dim dbId = SupabaseClient.GetStr(el, "id")
                    Dim code = SupabaseClient.GetStr(el, "user_code")
                    Dim nama = SupabaseClient.GetStr(el, "full_name")
                    Dim email = SupabaseClient.GetStr(el, "email")
                    Dim uname = SupabaseClient.GetStr(el, "username")
                    Dim roleDb = SupabaseClient.GetStr(el, "role")
                    Dim roleUi = If(roleDb.Length > 0, Char.ToUpper(roleDb(0)) & roleDb.Substring(1), "Viewer")
                    Dim aktif = SupabaseClient.GetBool(el, "is_active")
                    Dim row = New UserRow(code, nama, email, uname, roleUi, aktif, dbId)
                    row.DivisionId = SupabaseClient.GetStr(el, "division_id")
                    If divisionNames.ContainsKey(row.DivisionId) Then row.DivisionName = divisionNames(row.DivisionId)
                    _all.Add(row)
                Next
            End Using
            RefreshDivisionFilter()
            ApplyFilter()
        Catch ex As Exception
            _lblTotal.Text = "Gagal memuat"
            MessageBox.Show("Gagal memuat daftar pengguna:" & vbCrLf & ex.Message, "Error Supabase", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

    Private Sub RefreshDivisionFilter()
        Dim previous = If(_cbDivision.SelectedItem, "Semua divisi").ToString()
        Dim names = New List(Of String) From {"Semua divisi"}
        names.AddRange(_all.Select(Function(u) u.DivisionName).Where(Function(n) n <> "").Distinct().OrderBy(Function(n) n))
        _cbDivision.SetItems(names.ToArray())
        _cbDivision.SelectByText(If(names.Contains(previous), previous, "Semua divisi"))
    End Sub

    ' ---------- filter & paginasi ----------
    Private Sub OnFilterChanged(sender As Object, e As EventArgs)
        _page = 0
        ApplyFilter()
    End Sub

    Private Sub ApplyFilter()
        If _grid Is Nothing OrElse _cbRole.SelectedItem Is Nothing OrElse _cbStatus.SelectedItem Is Nothing Then Return
        Dim q = _txtSearch.Inner.Text.Trim().ToLower()
        Dim role = If(_cbRole.SelectedIndex > 0, _cbRole.SelectedItem, Nothing)
        Dim sta = If(_cbStatus.SelectedIndex > 0, _cbStatus.SelectedItem, Nothing)
        Dim division = If(_cbDivision.SelectedIndex > 0, _cbDivision.SelectedItem, Nothing)

        _view = _all.Where(Function(u)
                               If q <> "" AndAlso Not (u.Id.ToLower().Contains(q) OrElse u.Nama.ToLower().Contains(q) OrElse u.Email.ToLower().Contains(q) OrElse u.Username.ToLower().Contains(q)) Then Return False
                               If role IsNot Nothing AndAlso u.Role <> role Then Return False
                               If sta IsNot Nothing AndAlso (If(u.Aktif, "Aktif", "Nonaktif")) <> sta Then Return False
                               If division IsNot Nothing AndAlso u.DivisionName <> division Then Return False
                               Return True
                           End Function).ToList()
        If _page >= PageCount() Then _page = PageCount() - 1
        BindPage()
        _lblTotal.Text = _all.Count & " pengguna terdaftar"
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
        For Each u In _view.Skip(skip).Take(PageSize)
            _grid.Rows.Add(u.Id, u.Nama, u.Email, If(u.DivisionName = "", "-", u.DivisionName), u.Role, If(u.Aktif, "Aktif", "Nonaktif"), "")
        Next
        _grid.ClearSelection()
        Dim first = skip + 1
        Dim last = Math.Min(skip + PageSize, _view.Count)
        _lblFooter.Text = If(_view.Count = 0, "Tidak ada pengguna yang cocok.", "Menampilkan " & first & "–" & last & " dari " & _view.Count & " pengguna")
        _lblPage.Text = (_page + 1).ToString()
    End Sub

    ' ---------- tabel: badge role/status & aksi ----------
    Private Sub DrawTag(g As Graphics, text As String, x As Integer, cy As Integer, border As Color, fore As Color, fill As Color)
        Dim r As New Rectangle(x, cy - 13, 78, 26)
        FillRounded(g, New Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1), 3, fill, border)
        DrawText(g, text, AppFont(12, FontStyle.Bold), fore, r, StringAlignment.Center)
    End Sub

    Private Shared ReadOnly ActionNames As String() = {"Edit", "Status"}

    Private Function ActionRects(cell As Size) As Rectangle()
        Dim f = AppFont(14, FontStyle.Bold)
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
        If e.RowIndex < 0 Then Return
        Dim cy = e.CellBounds.Y + e.CellBounds.Height \ 2
        Select Case e.ColumnIndex
            Case RoleCol
                e.PaintBackground(e.CellBounds, False)
                DrawTag(e.Graphics, Convert.ToString(e.Value), e.CellBounds.X + 16, cy, Theme.Beige, Theme.Charcoal, Theme.OffWhite)
                e.Handled = True
            Case StatusCol
                e.PaintBackground(e.CellBounds, False)
                Dim aktif = (Convert.ToString(e.Value) = "Aktif")
                DrawTag(e.Graphics, Convert.ToString(e.Value), e.CellBounds.X + 16, cy, If(aktif, Theme.StatusGreen, Theme.Beige), If(aktif, Theme.StatusGreen, Theme.Charcoal), Theme.OffWhite)
                e.Handled = True
            Case AksiCol
                e.PaintBackground(e.CellBounds, False)
                Dim f = AppFont(14, FontStyle.Bold)
                Dim flags = TextFormatFlags.NoPadding Or TextFormatFlags.VerticalCenter Or TextFormatFlags.Left
                Dim rects = ActionRects(e.CellBounds.Size)
                For i = 0 To 1
                    Dim r = rects(i)
                    r.Offset(e.CellBounds.X, e.CellBounds.Y)
                    TextRenderer.DrawText(e.Graphics, ActionNames(i), f, r, Theme.Grey, flags)
                    If i = 0 Then TextRenderer.DrawText(e.Graphics, " · ", f, New Rectangle(r.Right, r.Y, 18, r.Height), Theme.Grey, flags)
                Next
                e.Handled = True
        End Select
    End Sub

    Private Function HitAction(col As Integer, row As Integer, pt As Point) As Integer
        If row < 0 OrElse col <> AksiCol Then Return -1
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
        Dim u = _view(_page * PageSize + e.RowIndex)
        If act = 0 Then EditUser(u) Else ToggleUserStatus(u)
    End Sub

    ' ---------- aksi ----------
    Private Async Sub OnAddClick(sender As Object, e As EventArgs)
        Dim owner = FindForm()
        If owner Is Nothing Then Return
        Dim divisions = Await LoadDivisionsAsync()
        Using dlg As New DlgUser(Nothing, _all, divisions, SupabaseClient.IsDemoSession)
            If ShowDialogOver(owner, owner.ClientRectangle, dlg) <> DialogResult.OK Then Return
            Try
                If SupabaseClient.IsDemoSession Then
                    DemoData.SaveUser(New DemoData.DemoUser With {.Name = dlg.NamaLengkap, .Email = dlg.Email,
                        .DivisionId = dlg.DivisionId, .DivisionName = divisions.Single(Function(p) p.Value = dlg.DivisionId).Key})
                    Await LoadFromSupabaseAsync()
                    Return
                End If
                Dim newId = Await SupabaseClient.SignUpUserAsync(dlg.Email, dlg.Password, dlg.NamaLengkap, dlg.Username)
                If Not String.IsNullOrEmpty(newId) Then
                    Dim payload As New Dictionary(Of String, Object) From {
                        {"full_name", dlg.NamaLengkap},
                        {"username", dlg.Username},
                        {"role", dlg.Role.ToLower()}
                    }
                    If dlg.Role = "Staff" Then payload("division_id") = dlg.DivisionId
                    Await SupabaseClient.PatchJsonAsync($"/rest/v1/users?id=eq.{newId}", payload)
                End If
                MessageBox.Show("User " & dlg.NamaLengkap & " berhasil ditambahkan ke Supabase!", "Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Await LoadFromSupabaseAsync()
            Catch ex As Exception
                MessageBox.Show("Gagal menambahkan user ke Supabase:" & vbCrLf & ex.Message, "Error Supabase", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Async Sub EditUser(u As UserRow)
        Dim owner = FindForm()
        If owner Is Nothing Then Return
        Dim divisions = Await LoadDivisionsAsync()
        Using dlg As New DlgUser(u, _all, divisions, SupabaseClient.IsDemoSession)
            If ShowDialogOver(owner, owner.ClientRectangle, dlg) <> DialogResult.OK Then Return
            Try
                If SupabaseClient.IsDemoSession Then
                    DemoData.SaveUser(New DemoData.DemoUser With {.Id = u.DbId, .Name = dlg.NamaLengkap,
                        .Email = dlg.Email, .DivisionId = dlg.DivisionId,
                        .DivisionName = divisions.Single(Function(p) p.Value = dlg.DivisionId).Key, .Active = u.Aktif})
                    Await LoadFromSupabaseAsync()
                    Return
                End If
                Dim payload As New Dictionary(Of String, Object) From {
                    {"full_name", dlg.NamaLengkap},
                    {"email", dlg.Email},
                    {"username", dlg.Username},
                    {"role", dlg.Role.ToLower()}
                }
                If dlg.Role = "Staff" Then
                    payload("division_id") = dlg.DivisionId
                ElseIf u.DivisionId <> "" Then
                    payload("division_id") = Nothing
                End If
                Await SupabaseClient.PatchJsonAsync($"/rest/v1/users?id=eq.{u.DbId}", payload)
                Await LoadFromSupabaseAsync()
            Catch ex As Exception
                MessageBox.Show("Gagal memperbarui user di Supabase:" & vbCrLf & ex.Message, "Error Supabase", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Async Function LoadDivisionsAsync() As Task(Of Dictionary(Of String, String))
        If SupabaseClient.IsDemoSession Then Return DemoData.Divisions()
        Dim result As New Dictionary(Of String, String)()
        Try
            Dim raw = Await SupabaseClient.GetJsonAsync("/rest/v1/divisions?select=id,name&order=name.asc")
            Using doc = JsonDocument.Parse(raw)
                For Each item In doc.RootElement.EnumerateArray()
                    result(SupabaseClient.GetStr(item, "name")) = SupabaseClient.GetStr(item, "id")
                Next
            End Using
        Catch ex As Exception
            MessageBox.Show("Daftar divisi belum tersedia: " & ex.Message, "Supabase", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
        Return result
    End Function

    Private Async Sub ToggleUserStatus(u As UserRow)
        If u.DbId = SupabaseClient.CurrentUserId Then
            MessageBox.Show("Akun yang sedang digunakan tidak dapat dinonaktifkan.")
            Return
        End If
        If u.Aktif AndAlso u.Role = "Admin" AndAlso _all.Where(Function(x) x.Role = "Admin" AndAlso x.Aktif).Count() <= 1 Then
            MessageBox.Show("Admin aktif terakhir tidak dapat dinonaktifkan.")
            Return
        End If
        Dim nextStatus = If(u.Aktif, "nonaktif", "aktif")
        If Not ConfirmDialog.Ask(FindForm(), "Ubah status user?", u.Nama & " akan menjadi " & nextStatus & ".", "Ubah Status") Then Return

        Try
            If SupabaseClient.IsDemoSession Then
                DemoData.SaveUser(New DemoData.DemoUser With {.Id = u.DbId, .Name = u.Nama, .Email = u.Email,
                    .DivisionId = u.DivisionId, .DivisionName = u.DivisionName, .Active = Not u.Aktif})
            Else
                Await SupabaseClient.PatchJsonAsync($"/rest/v1/users?id=eq.{u.DbId}", New With {.is_active = Not u.Aktif})
            End If
            Await LoadFromSupabaseAsync()
        Catch ex As Exception
            MessageBox.Show("Gagal mengubah status user:" & vbCrLf & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class

' =====================================================================
' DIALOG TAMBAH / EDIT USER  (560 x 584)
' =====================================================================
Public Class DlgUser
    Inherits Form

    Private ReadOnly _editing As UserRow
    Private ReadOnly _users As List(Of UserRow)
    Private ReadOnly _txtNama As AppTextBox
    Private ReadOnly _txtEmail As AppTextBox
    Private ReadOnly _txtUsername As AppTextBox
    Private ReadOnly _cbRole As AppComboBox
    Private ReadOnly _txtPassword As AppTextBox
    Private ReadOnly _cbDivision As AppComboBox
    Private ReadOnly _divisions As Dictionary(Of String, String)
    Private ReadOnly _demo As Boolean

    Public ReadOnly Property NamaLengkap As String
        Get
            Return _txtNama.Inner.Text.Trim()
        End Get
    End Property

    Public ReadOnly Property Email As String
        Get
            Return _txtEmail.Inner.Text.Trim()
        End Get
    End Property

    Public ReadOnly Property Username As String
        Get
            Return _txtUsername.Inner.Text.Trim()
        End Get
    End Property

    Public ReadOnly Property Role As String
        Get
            Return _cbRole.SelectedItem
        End Get
    End Property

    Public ReadOnly Property Password As String
        Get
            Return _txtPassword.Inner.Text
        End Get
    End Property

    Public ReadOnly Property DivisionId As String
        Get
            If _cbDivision.SelectedItem Is Nothing Then Return ""
            Return _divisions(_cbDivision.SelectedItem.ToString())
        End Get
    End Property

    Public Sub New(editing As UserRow, users As List(Of UserRow), divisions As Dictionary(Of String, String), Optional demo As Boolean = False)
        _editing = editing
        _users = users
        _divisions = divisions
        _demo = demo
        Dim isEdit = (editing IsNot Nothing)

        FormBorderStyle = FormBorderStyle.None
        ShowInTaskbar = False
        ClientSize = New Size(560, 584)
        BackColor = Theme.OffWhite
        Region = New Region(RoundedRect(New Rectangle(0, 0, Width, Height), 10))

        Controls.Add(MakeLabel(If(isEdit, "Edit user", "Tambah user"), 24, Theme.Charcoal, 24, 26, FontStyle.Bold))
        Dim lblClose = MakeLabel("×", 26, Theme.Charcoal, 506, 22)
        lblClose.Cursor = Cursors.Hand
        AddHandler lblClose.Click, AddressOf OnCloseClick
        Controls.Add(lblClose)
        Controls.Add(MakeLabel(If(isEdit, "Perbarui data pengguna dan hak aksesnya.", "Buat akun baru dan tentukan hak aksesnya."), 14, Theme.Grey, 24, 65))

        _txtNama = New AppTextBox() With {.PlaceholderText = "Nama lengkap pengguna"}
        _txtEmail = New AppTextBox() With {.PlaceholderText = "nama@asetku.co.id"}
        _txtUsername = New AppTextBox() With {.PlaceholderText = "username"}
        _cbRole = New AppComboBox() With {.PlaceholderText = "Pilih role"}
        _cbRole.SetItems("Admin", "Staff")
        If demo Then _cbRole.Enabled = False
        _cbDivision = New AppComboBox() With {.PlaceholderText = "Pilih divisi"}
        _cbDivision.SetItems(divisions.Keys.ToArray())
        _txtPassword = New AppTextBox() With {.PlaceholderText = If(isEdit, "Kosongkan jika tidak diubah", "Minimal 8 karakter")}
        _txtPassword.Inner.UseSystemPasswordChar = True

        Place("Nama lengkap *", _txtNama, 24, 113, 512)
        Place("Email *", _txtEmail, 24, 191, 512)
        Place("Username *", _txtUsername, 24, 269, 248)
        Place("Role *", _cbRole, 288, 269, 248)
        If Not demo Then Place(If(isEdit, "Password baru (opsional)", "Password sementara *"), _txtPassword, 24, 347, 512)
        Place("Divisi (wajib untuk Staff)", _cbDivision, 24, If(demo, 347, 425), 512)

        Dim btnCancel As New AppButton() With {.Text = "Batal", .Kind = ButtonKind.Outline, .Location = New Point(248, 516), .Size = New Size(136, 36), .DialogResult = DialogResult.Cancel}
        Dim btnOk As New AppButton() With {.Text = "Simpan user", .Kind = ButtonKind.Primary, .Location = New Point(400, 516), .Size = New Size(136, 36)}
        AddHandler btnOk.Click, AddressOf OnOkClick
        Controls.Add(btnCancel)
        Controls.Add(btnOk)
        CancelButton = btnCancel

        If isEdit Then
            _txtNama.Inner.Text = editing.Nama
            _txtEmail.Inner.Text = editing.Email
            _txtUsername.Inner.Text = editing.Username
            _cbRole.SelectByText(If(demo, "Staff", editing.Role))
            For Each pair In divisions
                If pair.Value = editing.DivisionId Then _cbDivision.SelectByText(pair.Key)
            Next
        Else
            _cbRole.SelectByText("Staff")
        End If
    End Sub

    Private Sub Place(caption As String, ctl As Control, x As Integer, y As Integer, w As Integer)
        Controls.Add(MakeLabel(caption, 12, Theme.Charcoal, x, y, FontStyle.Bold))
        ctl.Location = New Point(x, y + 25)
        ctl.Width = w
        Controls.Add(ctl)
    End Sub

    Private Sub OnCloseClick(sender As Object, e As EventArgs)
        DialogResult = DialogResult.Cancel
    End Sub

    Private Sub OnOkClick(sender As Object, e As EventArgs)
        Dim errs As New List(Of String)
        If NamaLengkap = "" Then errs.Add("Nama lengkap wajib diisi.")
        If Not Regex.IsMatch(Email, "^[^@\s]+@[^@\s]+\.[^@\s]+$") Then errs.Add("Format email tidak valid.")
        If Username = "" OrElse Username.Contains(" ") Then errs.Add("Username wajib diisi dan tidak boleh mengandung spasi.")
        If Role Is Nothing Then errs.Add("Role wajib dipilih.")
        If Role = "Staff" AndAlso DivisionId = "" Then errs.Add("Divisi wajib dipilih untuk Staff.")
        If _demo Then
            ' Akun demo tidak memakai password.
        ElseIf _editing Is Nothing Then
            If Password.Length < 8 Then errs.Add("Password sementara minimal 8 karakter.")
        ElseIf Password <> "" AndAlso Password.Length < 8 Then
            errs.Add("Password baru minimal 8 karakter.")
        End If

        Dim others = _users.Where(Function(u) Not Object.ReferenceEquals(u, _editing))
        If others.Any(Function(u) String.Equals(u.Email, Email, StringComparison.OrdinalIgnoreCase)) Then errs.Add("Email sudah dipakai pengguna lain.")
        If others.Any(Function(u) String.Equals(u.Username, Username, StringComparison.OrdinalIgnoreCase)) Then errs.Add("Username sudah dipakai pengguna lain.")

        If errs.Count > 0 Then
            MessageBox.Show(String.Join(vbCrLf, errs), "Periksa kembali", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        DialogResult = DialogResult.OK
    End Sub
End Class
