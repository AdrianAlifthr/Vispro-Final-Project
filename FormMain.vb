Imports System.Drawing.Drawing2D

Public Class FormMain
    Inherits Form

    Private ReadOnly _userName As String
    Private ReadOnly _menuItems As New List(Of SidebarItem)
    Private ReadOnly _content As Panel
    Private ReadOnly _lblTitle As Label

    Private Shared ReadOnly MenuNames As String() = {
        "Dashboard", "Data Aset / Barang", "Barang Masuk", "Barang Keluar",
        "Peminjaman Aset", "Pemeliharaan", "Master Data", "Laporan", "Manajemen User"}

    Public Sub New(userName As String)
        _userName = If(String.IsNullOrWhiteSpace(userName), "Pengguna", userName)

        Text = "AsetKu - Inventaris & Manajemen Aset"
        ClientSize = New Size(1366, 768)
        StartPosition = FormStartPosition.CenterScreen
        FormBorderStyle = FormBorderStyle.FixedSingle
        MaximizeBox = False
        BackColor = Theme.OffWhite
        DoubleBuffered = True

        ' ===== SIDEBAR (240px, Dark Charcoal) =====
        Dim sidebar As New Panel() With {.Location = New Point(0, 0), .Size = New Size(240, 768), .BackColor = Theme.Charcoal}
        sidebar.Controls.Add(MakeLabel("AsetKu", 28, Theme.OffWhite, 24, 26, FontStyle.Bold))
        sidebar.Controls.Add(New Panel() With {.BackColor = Theme.Orange, .Location = New Point(132, 48), .Size = New Size(8, 8)})
        sidebar.Controls.Add(MakeLabel("INVENTORY && ASSET", 11, Theme.Beige, 24, 70))

        For i = 0 To MenuNames.Length - 1
            Dim item As New SidebarItem(MenuNames(i)) With {.Location = New Point(12, 116 + i * 48), .Tag = i}
            AddHandler item.Click, AddressOf OnMenuClick
            _menuItems.Add(item)
            sidebar.Controls.Add(item)
        Next

        sidebar.Controls.Add(New Panel() With {.BackColor = Theme.Grey, .Location = New Point(24, 620), .Size = New Size(192, 1)})
        sidebar.Controls.Add(MakeLabel("Bantuan && panduan", 12, Theme.Beige, 24, 644))
        sidebar.Controls.Add(MakeLabel("AsetKu Desktop  •  v1.0", 12, Theme.Beige, 24, 710))
        Controls.Add(sidebar)

        ' ===== TOP BAR (64px) =====
        Dim topbar As New Panel() With {.Location = New Point(240, 0), .Size = New Size(1126, 64), .BackColor = Theme.OffWhite}
        AddHandler topbar.Paint, AddressOf TopbarPaint
        _lblTitle = MakeLabel("Inventaris", 18, Theme.Charcoal, 24, 20, FontStyle.Bold)
        topbar.Controls.Add(_lblTitle)

        Dim search As New AppTextBox() With {.Location = New Point(478, 14), .Width = 288, .PlaceholderText = "Cari aset, transaksi...", .BackColor = Theme.OffWhite}
        topbar.Controls.Add(search)

        topbar.Controls.Add(New Label() With {.Text = ChrW(&HD83D) & ChrW(&HDD14), .Font = New Font("Segoe UI Emoji", 14, GraphicsUnit.Pixel), .ForeColor = Theme.Grey, .AutoSize = True, .Location = New Point(792, 20), .BackColor = Color.Transparent})

        Dim avatar As New Panel() With {.Location = New Point(848, 16), .Size = New Size(32, 32), .BackColor = Theme.OffWhite}
        AddHandler avatar.Paint, Sub(s, e)
                                     FillRounded(e.Graphics, New Rectangle(0, 0, 31, 31), 6, Theme.Beige)
                                     DrawText(e.Graphics, Initials(_userName), AppFont(12, FontStyle.Bold), Theme.Charcoal, New Rectangle(0, 0, 32, 32), StringAlignment.Center)
                                 End Sub
        topbar.Controls.Add(avatar)
        topbar.Controls.Add(MakeLabel(Capitalize(_userName), 12, Theme.Charcoal, 892, 14, FontStyle.Bold))
        Dim roleLabel = If(String.IsNullOrEmpty(SupabaseClient.CurrentUserRole), "Pengguna", Capitalize(SupabaseClient.CurrentUserRole))
        topbar.Controls.Add(MakeLabel(roleLabel, 12, Theme.Grey, 892, 34))
        Controls.Add(topbar)

        ' ===== AREA KONTEN =====
        _content = New Panel() With {.Location = New Point(240, 64), .Size = New Size(1126, 704), .BackColor = Theme.OffWhite}
        Controls.Add(_content)

        NavigateTo(0)
    End Sub

    Private Sub TopbarPaint(sender As Object, e As PaintEventArgs)
        Using p As New Pen(Theme.Beige, 1)
            e.Graphics.DrawLine(p, 0, 63, 1126, 63)
        End Using
    End Sub

    Private Sub OnMenuClick(sender As Object, e As EventArgs)
        NavigateTo(CInt(DirectCast(sender, Control).Tag))
    End Sub

    ''' <summary>Ganti halaman lewat menu sidebar. Tambahkan UserControl baru di sini.</summary>
    Public Sub NavigateTo(index As Integer)
        Select Case index
            Case 0 : ShowPage(New ucDashboard(_userName), 0)
            Case 1 : ShowPage(New ucAsetList(), 1)
            Case 2 : ShowPage(New ucTransaksi(TrxMode.Masuk, _userName), 2, "Barang Masuk")
            Case 3 : ShowPage(New ucTransaksi(TrxMode.Keluar, _userName), 3, "Barang Keluar")
            Case 4 : ShowPage(New ucPeminjaman(_userName), 4, "Peminjaman Aset")
            Case 5 : ShowPage(New ucPemeliharaan(), 5, "Pemeliharaan")
            Case 6 : ShowPage(New ucMasterData(), 6, "Master Data")
            Case 7 : ShowPage(New ucLaporan(), 7, "Laporan")
            Case 8 : ShowPage(New ucUser(), 8, "Manajemen User")
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
