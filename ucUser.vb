Imports System.Text.RegularExpressions

''' <summary>Satu pengguna aplikasi (nanti dari tabel users / Supabase Auth).</summary>
Public Class UserRow
    Public Property Id As String
    Public Property Nama As String
    Public Property Email As String
    Public Property Username As String
    Public Property Role As String        ' Admin / Staff / Viewer
    Public Property Aktif As Boolean = True

    Public Sub New(id As String, nama As String, email As String, username As String, role As String, aktif As Boolean)
        Me.Id = id : Me.Nama = nama : Me.Email = email : Me.Username = username : Me.Role = role : Me.Aktif = aktif
    End Sub
End Class

''' <summary>Halaman Manajemen User.</summary>
Public Class ucUser
    Inherits UserControl

    Private Const PageSize As Integer = 8
    Private Const RoleCol As Integer = 3
    Private Const StatusCol As Integer = 4
    Private Const AksiCol As Integer = 5

    Private ReadOnly _all As New List(Of UserRow)
    Private _view As New List(Of UserRow)
    Private _page As Integer = 0

    Private ReadOnly _txtSearch As AppTextBox
    Private ReadOnly _cbRole As AppComboBox
    Private ReadOnly _cbStatus As AppComboBox
    Private ReadOnly _lblTotal As Label
    Private ReadOnly _grid As DataGridView
    Private ReadOnly _lblFooter As Label
    Private ReadOnly _lblPage As Label

    Public Sub New()
        BackColor = Theme.OffWhite
        DoubleBuffered = True
        LoadSampleData()

        Controls.Add(MakeLabel("Manajemen User", 26, Theme.Charcoal, 24, 22, FontStyle.Bold))
        Controls.Add(MakeLabel("Atur akses pengguna sesuai peran dan tanggung jawab.", 14, Theme.Grey, 24, 62))
        Dim btnAdd As New AppButton() With {.Text = "+ Tambah User", .Location = New Point(950, 30), .Size = New Size(152, 36), .BackColor = Theme.OffWhite}
        AddHandler btnAdd.Click, AddressOf OnAddClick
        Controls.Add(btnAdd)

        ' ===== Filter (y=102) =====
        _txtSearch = New AppTextBox() With {.Location = New Point(24, 102), .Width = 344, .PlaceholderText = "Cari nama, email, atau username..."}
        _cbRole = New AppComboBox() With {.Location = New Point(384, 102), .Width = 184}
        _cbStatus = New AppComboBox() With {.Location = New Point(584, 102), .Width = 184}
        _cbRole.SetItems("Semua role", "Admin", "Staff", "Viewer")
        _cbStatus.SetItems("Semua status", "Aktif", "Nonaktif")
        _cbRole.SelectedIndex = 0 : _cbStatus.SelectedIndex = 0
        Controls.Add(_txtSearch)
        Controls.Add(_cbRole)
        Controls.Add(_cbStatus)
        _lblTotal = MakeLabel("", 12, Theme.Grey, 784, 112)
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
        Dim heads As String() = {"ID user", "Nama lengkap", "Email", "Role", "Status", "Aksi"}
        Dim widths As Integer() = {116, 222, 270, 122, 132, 214}
        For i = 0 To heads.Length - 1
            _grid.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = heads(i), .Width = widths(i), .SortMode = DataGridViewColumnSortMode.NotSortable})
        Next
        _grid.Columns(1).DefaultCellStyle.ForeColor = Theme.Charcoal
        _grid.Columns(1).DefaultCellStyle.SelectionForeColor = Theme.Charcoal
        AddHandler _grid.CellPainting, AddressOf GridCellPainting
        AddHandler _grid.CellMouseMove, AddressOf GridMouseMove
        AddHandler _grid.CellMouseClick, AddressOf GridMouseClick
        card.Controls.Add(_grid)

        _lblFooter = MakeLabel("", 12, Theme.Grey, 16, 417)
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
        bar.Controls.Add(MakeLabel("Admin: akses penuh     •     Staff: kelola aset && transaksi     •     Viewer: lihat data && laporan", 12, Theme.Grey, 16, 13))
        Controls.Add(bar)

        AddHandler _txtSearch.Inner.TextChanged, AddressOf OnFilterChanged
        AddHandler _cbRole.SelectedIndexChanged, AddressOf OnFilterChanged
        AddHandler _cbStatus.SelectedIndexChanged, AddressOf OnFilterChanged
        ApplyFilter()
    End Sub

    ' ---------- data ----------
    Private Sub LoadSampleData()
        ' TODO (Supabase): SELECT dari tabel users (profil) yang terhubung ke auth.users.
        _all.Add(New UserRow("USR-001", "Aditya Pratama", "aditya@asetku.co.id", "aditya.pratama", "Admin", True))
        _all.Add(New UserRow("USR-002", "Siti Rahmawati", "siti@asetku.co.id", "siti.rahmawati", "Staff", True))
        _all.Add(New UserRow("USR-003", "Budi Santoso", "budi@asetku.co.id", "budi.santoso", "Staff", True))
        _all.Add(New UserRow("USR-004", "Dewi Anggraini", "dewi@asetku.co.id", "dewi.anggraini", "Viewer", True))
        _all.Add(New UserRow("USR-005", "Rizky Ramadhan", "rizky@asetku.co.id", "rizky.ramadhan", "Staff", True))
        _all.Add(New UserRow("USR-006", "Nadia Putri", "nadia@asetku.co.id", "nadia.putri", "Viewer", True))
        _all.Add(New UserRow("USR-007", "Fajar Hidayat", "fajar@asetku.co.id", "fajar.hidayat", "Admin", True))
        _all.Add(New UserRow("USR-008", "Rina Lestari", "rina@asetku.co.id", "rina.lestari", "Viewer", False))
    End Sub

    Private Function NextId() As String
        Dim maxN = 0
        For Each u In _all
            Dim n As Integer
            If Integer.TryParse(u.Id.Replace("USR-", ""), n) Then maxN = Math.Max(maxN, n)
        Next
        Return "USR-" & (maxN + 1).ToString("000")
    End Function

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

        _view = _all.Where(Function(u)
                               If q <> "" AndAlso Not (u.Id.ToLower().Contains(q) OrElse u.Nama.ToLower().Contains(q) OrElse u.Email.ToLower().Contains(q) OrElse u.Username.ToLower().Contains(q)) Then Return False
                               If role IsNot Nothing AndAlso u.Role <> role Then Return False
                               If sta IsNot Nothing AndAlso (If(u.Aktif, "Aktif", "Nonaktif")) <> sta Then Return False
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
            _grid.Rows.Add(u.Id, u.Nama, u.Email, u.Role, If(u.Aktif, "Aktif", "Nonaktif"), "")
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

    Private Shared ReadOnly ActionNames As String() = {"Edit", "Hapus"}

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
        If act = 0 Then EditUser(u) Else DeleteUser(u)
    End Sub

    ' ---------- aksi ----------
    Private Sub OnAddClick(sender As Object, e As EventArgs)
        Dim owner = FindForm()
        If owner Is Nothing Then Return
        Using dlg As New DlgUser(Nothing, _all)
            If ShowDialogOver(owner, owner.ClientRectangle, dlg) <> DialogResult.OK Then Return
            ' TODO (Supabase): buat akun lewat Supabase Auth (admin createUser) dengan dlg.Password,
            ' lalu INSERT profil ke tabel users (full_name, email, username, role).
            _all.Add(New UserRow(NextId(), dlg.NamaLengkap, dlg.Email, dlg.Username, dlg.Role, True))
            ApplyFilter()
            GoPage(PageCount() - 1)
        End Using
    End Sub

    Private Sub EditUser(u As UserRow)
        Dim owner = FindForm()
        If owner Is Nothing Then Return
        Using dlg As New DlgUser(u, _all)
            If ShowDialogOver(owner, owner.ClientRectangle, dlg) <> DialogResult.OK Then Return
            ' TODO (Supabase): UPDATE users ...; jika dlg.Password tidak kosong, ubah password lewat Auth.
            u.Nama = dlg.NamaLengkap
            u.Email = dlg.Email
            u.Username = dlg.Username
            u.Role = dlg.Role
            ApplyFilter()
        End Using
    End Sub

    Private Sub DeleteUser(u As UserRow)
        If u.Role = "Admin" AndAlso _all.Count(Function(x) x.Role = "Admin") <= 1 Then
            MessageBox.Show("Admin terakhir tidak dapat dihapus.", "Tidak dapat dihapus", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Dim msg = u.Nama & " (" & u.Id & ") akan dihapus." & vbCrLf & "Tindakan ini tidak dapat dibatalkan."
        If ConfirmDialog.Ask(FindForm(), "Hapus user ini?", msg, "Hapus User") Then
            ' TODO (Supabase): hapus/nonaktifkan akun di Auth dan baris di tabel users.
            _all.Remove(u)
            ApplyFilter()
        End If
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

    Public Sub New(editing As UserRow, users As List(Of UserRow))
        _editing = editing
        _users = users
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
        _cbRole.SetItems("Admin", "Staff", "Viewer")
        _txtPassword = New AppTextBox() With {.PlaceholderText = If(isEdit, "Kosongkan jika tidak diubah", "Minimal 8 karakter")}
        _txtPassword.Inner.UseSystemPasswordChar = True

        Place("Nama lengkap *", _txtNama, 24, 113, 512)
        Place("Email *", _txtEmail, 24, 191, 512)
        Place("Username *", _txtUsername, 24, 269, 248)
        Place("Role *", _cbRole, 288, 269, 248)
        Place(If(isEdit, "Password baru (opsional)", "Password sementara *"), _txtPassword, 24, 347, 512)

        Controls.Add(MakeLabel("Pengguna dapat mengganti password setelah masuk.", 12, Theme.Grey, 24, 435))

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
            _cbRole.SelectByText(editing.Role)
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
        If _editing Is Nothing Then
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
