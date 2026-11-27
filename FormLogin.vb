Public Partial Class FormLogin
    Private _selectedPortal As String = ""
    Private ReadOnly _portalPanel As Panel

    Public Sub New()
        InitializeComponent()
        DoubleBuffered = True
        AddHandler lblToggle.Click, AddressOf ToggleVisible
        AddHandler btnLogin.Click, AddressOf DoLogin
        lblToggle.BringToFront()
        _portalPanel = New Panel() With {.Location = New Point(720, 120), .Size = New Size(590, 485), .BackColor = BackColor}
        _portalPanel.Controls.Add(MakeLabel("Pilih portal AsetKu", 26, Theme.Charcoal, 72, 65, FontStyle.Bold))
        _portalPanel.Controls.Add(MakeLabel("Masuk sesuai peran akun yang diberikan admin.", 14, Theme.Grey, 72, 112))
        Dim adminButton As New AppButton() With {.Text = "Portal Admin", .Kind = ButtonKind.Primary, .Location = New Point(72, 180), .Size = New Size(454, 52)}
        Dim userButton As New AppButton() With {.Text = "Portal User", .Kind = ButtonKind.Outline, .Location = New Point(72, 250), .Size = New Size(454, 52)}
        AddHandler adminButton.Click, Sub(s, e) SelectPortal("admin")
        AddHandler userButton.Click, Sub(s, e) SelectPortal("staff")
        _portalPanel.Controls.Add(adminButton)
        _portalPanel.Controls.Add(userButton)
        Controls.Add(_portalPanel)
        _portalPanel.BringToFront()
        Dim backButton As New LinkLabel() With {.Text = "← Ganti portal", .Location = New Point(792, 566), .AutoSize = True, .LinkColor = Theme.Charcoal}
        AddHandler backButton.Click, Sub(s, e)
                                         SupabaseClient.ClearSession()
                                         _selectedPortal = ""
                                         lblError.Text = ""
                                         _portalPanel.Visible = True
                                     End Sub
        Controls.Add(backButton)
    End Sub

    Private Sub SelectPortal(role As String)
        _selectedPortal = role
        lblSubtitle.Text = If(role = "admin", "Masuk ke Portal Admin.", "Masuk ke Portal User (mode mock).")
        lblAccountHint.Text = If(role = "admin", "Gunakan akun yang diberikan oleh administrator.", "Akun mock tersedia di README • password: demo123")
        lblEmail.Text = "Email"
        txtEmail.Visible = True
        lblPassword.Visible = True
        txtPassword.Visible = True
        lblToggle.Visible = True
        _portalPanel.Visible = False
        txtEmail.Inner.Focus()
    End Sub

    Private Sub OpenDemo(role As String, Optional userId As String = "demo-user-it")
        SupabaseClient.StartDemoSession(role, userId)
        OpenMain()
    End Sub

    Private Sub OpenMain()
        Dim main As New FormMain(SupabaseClient.CurrentUserName)
        AddHandler main.FormClosed, AddressOf OnMainClosed
        main.Show()
        Hide()
    End Sub

    Private Sub ToggleVisible(sender As Object, e As EventArgs)
        Dim hidden = txtPassword.Inner.UseSystemPasswordChar
        txtPassword.Inner.UseSystemPasswordChar = Not hidden
        lblToggle.Text = If(hidden, "Sembunyikan", "Tampilkan")
    End Sub

    Private Async Sub DoLogin(sender As Object, e As EventArgs)
        If _selectedPortal = "" Then Return
        lblError.Text = ""
        Dim email = txtEmail.Inner.Text.Trim()
        Dim pass = txtPassword.Inner.Text
        If _selectedPortal = "staff" Then
            If email = "" OrElse pass = "" Then
                lblError.Text = "Email dan password wajib diisi."
                Return
            End If
            Dim demoUser = DemoData.Authenticate(email, pass)
            If demoUser Is Nothing Then
                lblError.Text = "Email atau password akun dummy salah."
                Return
            End If
            Try
                OpenDemo("staff", demoUser.Id)
            Catch ex As Exception
                lblError.Text = ex.Message
            End Try
            Return
        End If
        If email = "" OrElse pass = "" Then
            lblError.Text = "Email dan password wajib diisi."
            Return
        End If

        btnLogin.Enabled = False
        Try
            Dim result = Await SupabaseClient.SignInAsync(email, pass)
            If Not result.Ok Then
                lblError.Text = result.Message
                Return
            End If

            If Not String.Equals(SupabaseClient.CurrentUserRole, _selectedPortal, StringComparison.OrdinalIgnoreCase) Then
                SupabaseClient.ClearSession()
                lblError.Text = "Peran akun tidak sesuai dengan portal yang dipilih."
                Return
            End If

            OpenMain()
        Catch ex As Exception
            SupabaseClient.ClearSession()
            lblError.Text = "Gagal terhubung ke Supabase: " & ex.Message
        Finally
            btnLogin.Enabled = True
        End Try
    End Sub

    Private Sub OnMainClosed(sender As Object, e As FormClosedEventArgs)
        Dim main = TryCast(sender, FormMain)
        If (main IsNot Nothing AndAlso main.LogoutRequested) OrElse SupabaseClient.IsDemoSession Then
            SupabaseClient.ClearSession()
            _selectedPortal = ""
            _portalPanel.Visible = True
            txtEmail.Inner.Clear()
            txtPassword.Inner.Clear()
            lblError.Text = ""
            Show()
            Return
        End If
        Close()
    End Sub
End Class
