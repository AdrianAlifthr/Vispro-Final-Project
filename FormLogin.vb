Public Class FormLogin
    Inherits Form

    Private ReadOnly txtEmail As AppTextBox
    Private ReadOnly txtPassword As AppTextBox
    Private ReadOnly lblToggle As Label
    Private ReadOnly lblError As Label
    Private ReadOnly btnLogin As AppButton

    Public Sub New()
        ' ===== Form (1366x768 sesuai frame Figma) =====
        Text = "AsetKu - Masuk"
        ClientSize = New Size(1366, 768)
        StartPosition = FormStartPosition.CenterScreen
        FormBorderStyle = FormBorderStyle.FixedSingle
        MaximizeBox = False
        BackColor = Theme.OffWhite
        DoubleBuffered = True

        ' ===== Panel kiri: hero gradient (672 x 768) =====
        Dim hero As New GradientPanel() With {.Location = New Point(0, 0), .Size = New Size(672, 768), .Angle = 30.0F}
        hero.Controls.Add(MakeLabel("AsetKu", 32, Theme.Charcoal, 48, 40, FontStyle.Bold))
        hero.Controls.Add(MakeLabel("INVENTORY && ASSET MANAGEMENT", 12, Theme.Charcoal, 48, 94, FontStyle.Bold))
        hero.Controls.Add(MakeLabel("Semua aset." & vbCrLf & "Satu kendali.", 48, Theme.Charcoal, 48, 240, FontStyle.Bold, 560, 130))
        hero.Controls.Add(MakeLabel("Kelola aset, pantau persediaan, dan catat setiap" & vbCrLf & "pergerakan barang dengan lebih teratur.", 18, Theme.Charcoal, 48, 380, FontStyle.Regular, 560, 56))
        Dim divider As New Panel() With {.BackColor = Theme.Charcoal, .Location = New Point(48, 534), .Size = New Size(72, 4)}
        hero.Controls.Add(divider)
        hero.Controls.Add(MakeLabel("Tertata hari ini. Siap untuk esok.", 14, Theme.Charcoal, 48, 562, FontStyle.Bold))
        hero.Controls.Add(MakeLabel("AsetKu Desktop  /  v1.0", 12, Theme.Charcoal, 48, 698))
        Controls.Add(hero)

        ' ===== Panel kanan: form login (mulai x=792, y=176, lebar 454) =====
        Const fx As Integer = 792
        Const fy As Integer = 176
        Controls.Add(MakeLabel("Selamat datang kembali", 24, Theme.Charcoal, fx, fy, FontStyle.Bold))
        Controls.Add(MakeLabel("Masuk untuk mengelola inventaris perusahaan Anda.", 14, Theme.Grey, fx, fy + 44))

        Controls.Add(MakeLabel("Username / Email", 14, Theme.Charcoal, fx, fy + 104, FontStyle.Bold))
        txtEmail = New AppTextBox() With {.Location = New Point(fx, fy + 132), .Width = 454, .PlaceholderText = "admin@asetku.co.id"}
        Controls.Add(txtEmail)

        Controls.Add(MakeLabel("Password", 14, Theme.Charcoal, fx, fy + 192, FontStyle.Bold))
        txtPassword = New AppTextBox() With {.Location = New Point(fx, fy + 220), .Width = 454, .RightPadding = 70}
        txtPassword.Inner.UseSystemPasswordChar = True
        Controls.Add(txtPassword)

        lblToggle = MakeLabel("Tampilkan", 12, Theme.Grey, fx + 366, fy + 229)
        lblToggle.Cursor = Cursors.Hand
        AddHandler lblToggle.Click, AddressOf ToggleVisible
        Controls.Add(lblToggle)
        lblToggle.BringToFront()

        btnLogin = New AppButton() With {.Text = "Masuk", .Location = New Point(fx, fy + 288), .Size = New Size(454, 36), .Kind = ButtonKind.Primary}
        AddHandler btnLogin.Click, AddressOf DoLogin
        Controls.Add(btnLogin)
        AcceptButton = btnLogin

        lblError = MakeLabel("", 12, Theme.Orange, fx, fy + 332)
        Controls.Add(lblError)

        Controls.Add(MakeLabel("Gunakan akun yang diberikan oleh administrator.", 12, Theme.Grey, fx, fy + 348))
        Controls.Add(MakeLabel("© 2025 AsetKu  •  Sistem inventaris internal", 12, Theme.Grey, fx, 714))
    End Sub

    Private Sub ToggleVisible(sender As Object, e As EventArgs)
        Dim hidden = txtPassword.Inner.UseSystemPasswordChar
        txtPassword.Inner.UseSystemPasswordChar = Not hidden
        lblToggle.Text = If(hidden, "Sembunyikan", "Tampilkan")
    End Sub

    Private Sub DoLogin(sender As Object, e As EventArgs)
        lblError.Text = ""
        Dim email = txtEmail.Inner.Text.Trim()
        Dim pass = txtPassword.Inner.Text
        If email = "" OrElse pass = "" Then
            lblError.Text = "Username/Email dan password wajib diisi."
            Return
        End If

        ' TODO (Supabase): ganti blok ini dengan autentikasi sebenarnya,
        ' mis. POST {SUPABASE_URL}/auth/v1/token?grant_type=password
        ' Sementara: semua input yang terisi dianggap berhasil.
        Dim displayName As String = email.Split("@"c)(0)

        Dim main As New FormMain(displayName)
        AddHandler main.FormClosed, AddressOf OnMainClosed
        main.Show()
        Hide()
    End Sub

    Private Sub OnMainClosed(sender As Object, e As FormClosedEventArgs)
        Close()
    End Sub
End Class
