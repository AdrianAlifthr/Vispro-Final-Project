Imports System.Drawing.Drawing2D

Public Partial Class FormMain
    Private ReadOnly _userName As String
    Private ReadOnly _menuItems As New List(Of SidebarItem)
    Private ReadOnly _verificationMenu As SidebarItem
    Private ReadOnly _logoutMenu As SidebarItem

    Public Property LogoutRequested As Boolean = False
    Private Shared ReadOnly MenuNames As String() = {
        "Dashboard", "Data Aset / Barang", "Barang Masuk", "Barang Keluar",
        "Peminjaman Aset", "Pemeliharaan", "Master Data", "Laporan", "Manajemen User"}

    Public Sub New()
        Me.New("Pengguna")
    End Sub

    Public Sub New(userName As String)
        _userName = If(String.IsNullOrWhiteSpace(userName), "Pengguna", userName)
        InitializeComponent()
        DoubleBuffered = True
        lblUserName.Text = Capitalize(_userName)
        lblRole.Text = If(String.IsNullOrEmpty(SupabaseClient.CurrentUserRole), "Pengguna", Capitalize(SupabaseClient.CurrentUserRole))
        If SupabaseClient.IsDemoSession Then
            lblRole.Text &= " • DEMO"
        End If
        lblHelp.Visible = False
        _menuItems.AddRange({menu0, menu1, menu2, menu3, menu4, menu5, menu6, menu7, menu8})
        If IsAdmin Then
            _verificationMenu = New SidebarItem() With {.Text = "Verifikasi Pengajuan", .Tag = "9", .Location = New Point(12, 548), .Size = New Size(216, 40)}
            sidebar.Controls.Add(_verificationMenu)
            _menuItems.Add(_verificationMenu)
            menu3.Visible = False
            menu4.Visible = False
            menu5.Location = New Point(12, 260)
            menu6.Location = New Point(12, 308)
            menu7.Location = New Point(12, 356)
            menu8.Location = New Point(12, 404)
            _verificationMenu.Location = New Point(12, 452)
            If SupabaseClient.IsDemoSession Then
                For Each item In New SidebarItem() {menu1, menu2, menu3, menu4, menu5, menu6}
                    item.Visible = False
                Next
                menu0.Text = "Dashboard Demo"
                menu7.Location = New Point(12, 164)
                menu8.Location = New Point(12, 212)
                _verificationMenu.Location = New Point(12, 260)
            End If
        Else
            For Each item In New SidebarItem() {menu2, menu5, menu6, menu7, menu8}
                item.Visible = False
            Next
            menu0.Text = "Dashboard User"
            menu1.Text = "Aset Tersedia"
            menu3.Text = "Ajukan Barang Keluar"
            menu4.Text = "Ajukan Peminjaman"
            menu3.Location = New Point(12, 212)
            menu4.Location = New Point(12, 260)
        End If
        For Each item In _menuItems
            AddHandler item.Click, AddressOf OnMenuClick
        Next
        _logoutMenu = New SidebarItem() With {.Text = "Logout", .Location = New Point(12, 636), .Size = New Size(216, 40)}
        AddHandler _logoutMenu.Click, Sub(s, e)
                                          LogoutRequested = True
                                          Close()
                                      End Sub
        sidebar.Controls.Add(_logoutMenu)
        AddHandler topbar.Paint, AddressOf TopbarPaint
        AddHandler avatar.Paint, AddressOf AvatarPaint
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        If Not DesignMode AndAlso Not System.ComponentModel.LicenseManager.UsageMode = System.ComponentModel.LicenseUsageMode.Designtime Then
            NavigateTo(0)
        End If
    End Sub

    Private Sub AvatarPaint(sender As Object, e As PaintEventArgs)
        FillRounded(e.Graphics, New Rectangle(0, 0, 31, 31), 6, Theme.Beige)
        DrawText(e.Graphics, Initials(_userName), AppFont(12, FontStyle.Bold), Theme.Charcoal, New Rectangle(0, 0, 32, 32), StringAlignment.Center)
    End Sub

    Private Sub TopbarPaint(sender As Object, e As PaintEventArgs)
        Using p As New Pen(Theme.Beige, 1)
            e.Graphics.DrawLine(p, 0, 63, 1126, 63)
        End Using
    End Sub

    Private Sub OnMenuClick(sender As Object, e As EventArgs)
        NavigateTo(CInt(DirectCast(sender, Control).Tag))
    End Sub

    Private ReadOnly Property IsAdmin As Boolean
        Get
            Return String.Equals(SupabaseClient.CurrentUserRole, "admin", StringComparison.OrdinalIgnoreCase)
        End Get
    End Property

    ''' <summary>Ganti halaman lewat menu sidebar. Tambahkan UserControl baru di sini.</summary>
    Public Sub NavigateTo(index As Integer)
        If SupabaseClient.IsDemoSession AndAlso IsAdmin Then
            Select Case index
                Case 0, 9 : ShowPage(New ucRequestPage("admin"), index, If(index = 0, "Dashboard Demo", "Verifikasi Pengajuan"))
                Case 7 : ShowPage(New ucHistoryReport(), 7, "Laporan")
                Case 8 : ShowPage(New ucUser(), 8, "Manajemen User")
            End Select
            Return
        End If
        If Not IsAdmin Then
            Select Case index
                Case 0 : ShowPage(New ucRequestPage("dashboard"), 0, "Dashboard User")
                Case 1 : ShowPage(New ucAvailableAssets(), 1, "Aset Tersedia")
                Case 3 : ShowPage(New ucRequestPage("out"), 3, "Ajukan Barang Keluar")
                Case 4 : ShowPage(New ucRequestPage("loan"), 4, "Ajukan Peminjaman")
                Case Else : Return
            End Select
            Return
        End If
        Select Case index
            Case 0 : ShowPage(New ucDashboard(_userName), 0)
            Case 1 : ShowPage(New ucAsetList(), 1)
            Case 2 : ShowPage(New ucTransaksi(TrxMode.Masuk, _userName), 2, "Barang Masuk")
            Case 3, 4 : Return
            Case 5 : ShowPage(New ucPemeliharaan(), 5, "Pemeliharaan")
            Case 6 : ShowPage(New ucMasterData(), 6, "Master Data")
            Case 7 : ShowPage(New ucLaporan(), 7, "Laporan")
            Case 8 : ShowPage(New ucUser(), 8, "Manajemen User")
            Case 9 : ShowPage(New ucRequestPage("admin"), 9, "Verifikasi Pengajuan")
            Case Else : ShowPage(New ucPlaceholder(MenuNames(index)), index, MenuNames(index))
        End Select
    End Sub

    ''' <summary>Tampilkan halaman apa pun di area konten dan tandai menu sidebar yang aktif.</summary>
    Public Sub ShowPage(page As Control, activeIndex As Integer, Optional title As String = "Inventaris")
        For i = 0 To _menuItems.Count - 1
            _menuItems(i).IsActive = (i = activeIndex)
            _menuItems(i).Invalidate()
        Next

        _lblTitle.Text = title
        For Each old As Control In _content.Controls.Cast(Of Control).ToList()
            _content.Controls.Remove(old)
            Dim o As Control = old
            BeginInvoke(Sub() o.Dispose())
        Next
        page.Dock = DockStyle.Fill
        _content.Controls.Add(page)
    End Sub

    Private Shared Function Capitalize(s As String) As String
        If String.IsNullOrEmpty(s) Then Return s
        Return Char.ToUpper(s(0)) & s.Substring(1)
    End Function

    Private Shared Function Initials(s As String) As String
        Dim parts = s.Split({" "c, "."c, "_"c}, StringSplitOptions.RemoveEmptyEntries)
        If parts.Length = 0 Then Return "?"
        If parts.Length = 1 Then Return parts(0).Substring(0, 1).ToUpper()
        Return (parts(0).Substring(0, 1) & parts(1).Substring(0, 1)).ToUpper()
    End Function
End Class

''' <summary>Halaman sementara untuk menu yang belum dibuat.</summary>
Public Class ucPlaceholder
    Inherits UserControl

    Public Sub New(title As String)
        BackColor = Theme.OffWhite
        Controls.Add(MakeLabel(title, 24, Theme.Charcoal, 24, 24, FontStyle.Bold))
        Controls.Add(MakeLabel("Halaman ini belum dibuat.", 14, Theme.Grey, 24, 66))
    End Sub
End Class
