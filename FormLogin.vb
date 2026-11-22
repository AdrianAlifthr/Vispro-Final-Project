Public Partial Class FormLogin
    Public Sub New()
        InitializeComponent()
        DoubleBuffered = True
        AddHandler lblToggle.Click, AddressOf ToggleVisible
        AddHandler btnLogin.Click, AddressOf DoLogin
        lblToggle.BringToFront()
    End Sub

    Private Sub ToggleVisible(sender As Object, e As EventArgs)
        Dim hidden = txtPassword.Inner.UseSystemPasswordChar
        txtPassword.Inner.UseSystemPasswordChar = Not hidden
        lblToggle.Text = If(hidden, "Sembunyikan", "Tampilkan")
    End Sub

    Private Async Sub DoLogin(sender As Object, e As EventArgs)
        lblError.Text = ""
        Dim email = txtEmail.Inner.Text.Trim()
        Dim pass = txtPassword.Inner.Text
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

            Dim main As New FormMain(SupabaseClient.CurrentUserName)
            AddHandler main.FormClosed, AddressOf OnMainClosed
            main.Show()
            Hide()
        Catch ex As Exception
            lblError.Text = "Gagal terhubung ke Supabase: " & ex.Message
        Finally
            btnLogin.Enabled = True
        End Try
    End Sub

    Private Sub OnMainClosed(sender As Object, e As FormClosedEventArgs)
        Close()
    End Sub
End Class
