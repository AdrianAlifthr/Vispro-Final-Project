<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormLogin
    Inherits System.Windows.Forms.Form

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then components.Dispose()
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.hero = New AsetKu.GradientPanel()
        Me.lblBrand = New System.Windows.Forms.Label()
        Me.lblTagline = New System.Windows.Forms.Label()
        Me.lblHeroTitle = New System.Windows.Forms.Label()
        Me.lblHeroDescription = New System.Windows.Forms.Label()
        Me.heroDivider = New System.Windows.Forms.Panel()
        Me.lblHeroNote = New System.Windows.Forms.Label()
        Me.lblVersion = New System.Windows.Forms.Label()
        Me.lblWelcome = New System.Windows.Forms.Label()
        Me.lblSubtitle = New System.Windows.Forms.Label()
        Me.lblEmail = New System.Windows.Forms.Label()
        Me.txtEmail = New AsetKu.AppTextBox()
        Me.lblPassword = New System.Windows.Forms.Label()
        Me.txtPassword = New AsetKu.AppTextBox()
        Me.lblToggle = New System.Windows.Forms.Label()
        Me.btnLogin = New AsetKu.AppButton()
        Me.lblError = New System.Windows.Forms.Label()
        Me.lblAccountHint = New System.Windows.Forms.Label()
        Me.lblCopyright = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        Me.hero.SuspendLayout()
        ' hero
        Me.hero.Name = "hero"
        Me.hero.Location = New System.Drawing.Point(0, 0)
        Me.hero.Size = New System.Drawing.Size(672, 768)
        Me.hero.Angle = 30.0!
        ' lblBrand
        Me.lblBrand.Name = "lblBrand"
        Me.lblBrand.Location = New System.Drawing.Point(48, 40)
        Me.lblBrand.Size = New System.Drawing.Size(450, 24)
        Me.lblBrand.AutoSize = True
        Me.lblBrand.Text = "AsetKu"
        Me.lblBrand.Font = New System.Drawing.Font("Segoe UI", 32.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel)
        Me.lblBrand.ForeColor = System.Drawing.Color.FromArgb(48, 50, 54)
        Me.lblBrand.BackColor = System.Drawing.Color.Transparent
        ' lblTagline
        Me.lblTagline.Name = "lblTagline"
        Me.lblTagline.Location = New System.Drawing.Point(48, 94)
        Me.lblTagline.Size = New System.Drawing.Size(450, 24)
        Me.lblTagline.AutoSize = True
        Me.lblTagline.Text = "INVENTORY && ASSET MANAGEMENT"
        Me.lblTagline.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel)
        Me.lblTagline.ForeColor = System.Drawing.Color.FromArgb(48, 50, 54)
        Me.lblTagline.BackColor = System.Drawing.Color.Transparent
        ' lblHeroTitle
        Me.lblHeroTitle.Name = "lblHeroTitle"
        Me.lblHeroTitle.Location = New System.Drawing.Point(48, 240)
        Me.lblHeroTitle.Size = New System.Drawing.Size(560, 130)
        Me.lblHeroTitle.AutoSize = False
        Me.lblHeroTitle.Text = "Semua aset." & Global.Microsoft.VisualBasic.Constants.vbCrLf & "Satu kendali."
        Me.lblHeroTitle.Font = New System.Drawing.Font("Segoe UI", 48.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel)
        Me.lblHeroTitle.ForeColor = System.Drawing.Color.FromArgb(48, 50, 54)
        Me.lblHeroTitle.BackColor = System.Drawing.Color.Transparent
        ' lblHeroDescription
        Me.lblHeroDescription.Name = "lblHeroDescription"
        Me.lblHeroDescription.Location = New System.Drawing.Point(48, 380)
        Me.lblHeroDescription.Size = New System.Drawing.Size(560, 56)
        Me.lblHeroDescription.AutoSize = False
        Me.lblHeroDescription.Text = "Kelola aset, pantau persediaan, dan catat setiap" & Global.Microsoft.VisualBasic.Constants.vbCrLf & "pergerakan barang dengan lebih teratur."
        Me.lblHeroDescription.Font = New System.Drawing.Font("Segoe UI", 18.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel)
        Me.lblHeroDescription.ForeColor = System.Drawing.Color.FromArgb(48, 50, 54)
        Me.lblHeroDescription.BackColor = System.Drawing.Color.Transparent
        ' heroDivider
        Me.heroDivider.Name = "heroDivider"
        Me.heroDivider.Location = New System.Drawing.Point(48, 534)
        Me.heroDivider.Size = New System.Drawing.Size(72, 4)
        Me.heroDivider.BackColor = System.Drawing.Color.FromArgb(48, 50, 54)
        ' lblHeroNote
        Me.lblHeroNote.Name = "lblHeroNote"
        Me.lblHeroNote.Location = New System.Drawing.Point(48, 562)
        Me.lblHeroNote.Size = New System.Drawing.Size(450, 24)
        Me.lblHeroNote.AutoSize = True
        Me.lblHeroNote.Text = "Tertata hari ini. Siap untuk esok."
        Me.lblHeroNote.Font = New System.Drawing.Font("Segoe UI", 14.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel)
        Me.lblHeroNote.ForeColor = System.Drawing.Color.FromArgb(48, 50, 54)
        Me.lblHeroNote.BackColor = System.Drawing.Color.Transparent
        ' lblVersion
        Me.lblVersion.Name = "lblVersion"
        Me.lblVersion.Location = New System.Drawing.Point(48, 698)
        Me.lblVersion.Size = New System.Drawing.Size(450, 24)
        Me.lblVersion.AutoSize = True
        Me.lblVersion.Text = "AsetKu Desktop  /  v1.0"
        Me.lblVersion.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel)
        Me.lblVersion.ForeColor = System.Drawing.Color.FromArgb(48, 50, 54)
        Me.lblVersion.BackColor = System.Drawing.Color.Transparent
        ' lblWelcome
        Me.lblWelcome.Name = "lblWelcome"
        Me.lblWelcome.Location = New System.Drawing.Point(792, 176)
        Me.lblWelcome.Size = New System.Drawing.Size(450, 24)
        Me.lblWelcome.AutoSize = True
        Me.lblWelcome.Text = "Selamat datang kembali"
        Me.lblWelcome.Font = New System.Drawing.Font("Segoe UI", 24.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel)
        Me.lblWelcome.ForeColor = System.Drawing.Color.FromArgb(48, 50, 54)
        Me.lblWelcome.BackColor = System.Drawing.Color.Transparent
        ' lblSubtitle
        Me.lblSubtitle.Name = "lblSubtitle"
        Me.lblSubtitle.Location = New System.Drawing.Point(792, 220)
        Me.lblSubtitle.Size = New System.Drawing.Size(450, 24)
        Me.lblSubtitle.AutoSize = True
        Me.lblSubtitle.Text = "Masuk untuk mengelola inventaris perusahaan Anda."
        Me.lblSubtitle.Font = New System.Drawing.Font("Segoe UI", 14.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel)
        Me.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(82, 85, 90)
        Me.lblSubtitle.BackColor = System.Drawing.Color.Transparent
        ' lblEmail
        Me.lblEmail.Name = "lblEmail"
        Me.lblEmail.Location = New System.Drawing.Point(792, 280)
        Me.lblEmail.Size = New System.Drawing.Size(450, 24)
        Me.lblEmail.AutoSize = True
        Me.lblEmail.Text = "Username / Email"
        Me.lblEmail.Font = New System.Drawing.Font("Segoe UI", 14.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel)
        Me.lblEmail.ForeColor = System.Drawing.Color.FromArgb(48, 50, 54)
        Me.lblEmail.BackColor = System.Drawing.Color.Transparent
        ' txtEmail
        Me.txtEmail.Name = "txtEmail"
        Me.txtEmail.Location = New System.Drawing.Point(792, 308)
        Me.txtEmail.Size = New System.Drawing.Size(454, 36)
        Me.txtEmail.PlaceholderText = "admin@asetku.co.id"
        Me.txtEmail.TabIndex = 0
        ' lblPassword
        Me.lblPassword.Name = "lblPassword"
        Me.lblPassword.Location = New System.Drawing.Point(792, 368)
        Me.lblPassword.Size = New System.Drawing.Size(450, 24)
        Me.lblPassword.AutoSize = True
        Me.lblPassword.Text = "Password"
        Me.lblPassword.Font = New System.Drawing.Font("Segoe UI", 14.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel)
        Me.lblPassword.ForeColor = System.Drawing.Color.FromArgb(48, 50, 54)
        Me.lblPassword.BackColor = System.Drawing.Color.Transparent
        ' txtPassword
        Me.txtPassword.Name = "txtPassword"
        Me.txtPassword.Location = New System.Drawing.Point(792, 396)
        Me.txtPassword.Size = New System.Drawing.Size(454, 36)
        Me.txtPassword.RightPadding = 70
        Me.txtPassword.UseSystemPasswordChar = True
        Me.txtPassword.TabIndex = 1
        ' lblToggle
        Me.lblToggle.Name = "lblToggle"
        Me.lblToggle.Location = New System.Drawing.Point(1158, 405)
        Me.lblToggle.Size = New System.Drawing.Size(70, 20)
        Me.lblToggle.AutoSize = True
        Me.lblToggle.Text = "Tampilkan"
        Me.lblToggle.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel)
        Me.lblToggle.ForeColor = System.Drawing.Color.FromArgb(82, 85, 90)
        Me.lblToggle.Cursor = System.Windows.Forms.Cursors.Hand
        Me.lblToggle.BackColor = System.Drawing.Color.FromArgb(245, 245, 245)
        ' btnLogin
        Me.btnLogin.Name = "btnLogin"
        Me.btnLogin.Location = New System.Drawing.Point(792, 464)
        Me.btnLogin.Size = New System.Drawing.Size(454, 36)
        Me.btnLogin.Text = "Masuk"
        Me.btnLogin.Kind = AsetKu.ButtonKind.Primary
        Me.btnLogin.TabIndex = 2
        ' lblError
        Me.lblError.Name = "lblError"
        Me.lblError.Location = New System.Drawing.Point(792, 508)
        Me.lblError.Size = New System.Drawing.Size(450, 24)
        Me.lblError.AutoSize = True
        Me.lblError.Text = ""
        Me.lblError.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel)
        Me.lblError.ForeColor = System.Drawing.Color.FromArgb(230, 84, 22)
        Me.lblError.BackColor = System.Drawing.Color.Transparent
        ' lblAccountHint
        Me.lblAccountHint.Name = "lblAccountHint"
        Me.lblAccountHint.Location = New System.Drawing.Point(792, 524)
        Me.lblAccountHint.Size = New System.Drawing.Size(450, 24)
        Me.lblAccountHint.AutoSize = True
        Me.lblAccountHint.Text = "Gunakan akun yang diberikan oleh administrator."
        Me.lblAccountHint.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel)
        Me.lblAccountHint.ForeColor = System.Drawing.Color.FromArgb(82, 85, 90)
        Me.lblAccountHint.BackColor = System.Drawing.Color.Transparent
        ' lblCopyright
        Me.lblCopyright.Name = "lblCopyright"
        Me.lblCopyright.Location = New System.Drawing.Point(792, 714)
        Me.lblCopyright.Size = New System.Drawing.Size(450, 24)
        Me.lblCopyright.AutoSize = True
        Me.lblCopyright.Text = "© 2025 AsetKu  •  Sistem inventaris internal"
        Me.lblCopyright.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel)
        Me.lblCopyright.ForeColor = System.Drawing.Color.FromArgb(82, 85, 90)
        Me.lblCopyright.BackColor = System.Drawing.Color.Transparent
        Me.Controls.Add(Me.hero)
        Me.hero.Controls.Add(Me.lblBrand)
        Me.hero.Controls.Add(Me.lblTagline)
        Me.hero.Controls.Add(Me.lblHeroTitle)
        Me.hero.Controls.Add(Me.lblHeroDescription)
        Me.hero.Controls.Add(Me.heroDivider)
        Me.hero.Controls.Add(Me.lblHeroNote)
        Me.hero.Controls.Add(Me.lblVersion)
        Me.Controls.Add(Me.lblWelcome)
        Me.Controls.Add(Me.lblSubtitle)
        Me.Controls.Add(Me.lblEmail)
        Me.Controls.Add(Me.txtEmail)
        Me.Controls.Add(Me.lblPassword)
        Me.Controls.Add(Me.lblToggle)
        Me.Controls.Add(Me.txtPassword)
        Me.Controls.Add(Me.btnLogin)
        Me.Controls.Add(Me.lblError)
        Me.Controls.Add(Me.lblAccountHint)
        Me.Controls.Add(Me.lblCopyright)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None
        Me.ClientSize = New System.Drawing.Size(1366, 768)
        Me.BackColor = System.Drawing.Color.FromArgb(245, 245, 245)
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.Name = "FormLogin"
        Me.Text = "AsetKu - Masuk"
        Me.AcceptButton = Me.btnLogin
        Me.hero.ResumeLayout(False)
        Me.hero.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Friend WithEvents hero As AsetKu.GradientPanel
    Friend WithEvents lblBrand As System.Windows.Forms.Label
    Friend WithEvents lblTagline As System.Windows.Forms.Label
    Friend WithEvents lblHeroTitle As System.Windows.Forms.Label
    Friend WithEvents lblHeroDescription As System.Windows.Forms.Label
    Friend WithEvents heroDivider As System.Windows.Forms.Panel
    Friend WithEvents lblHeroNote As System.Windows.Forms.Label
    Friend WithEvents lblVersion As System.Windows.Forms.Label
    Friend WithEvents lblWelcome As System.Windows.Forms.Label
    Friend WithEvents lblSubtitle As System.Windows.Forms.Label
    Friend WithEvents lblEmail As System.Windows.Forms.Label
    Friend WithEvents txtEmail As AsetKu.AppTextBox
    Friend WithEvents lblPassword As System.Windows.Forms.Label
    Friend WithEvents txtPassword As AsetKu.AppTextBox
    Friend WithEvents lblToggle As System.Windows.Forms.Label
    Friend WithEvents btnLogin As AsetKu.AppButton
    Friend WithEvents lblError As System.Windows.Forms.Label
    Friend WithEvents lblAccountHint As System.Windows.Forms.Label
    Friend WithEvents lblCopyright As System.Windows.Forms.Label
End Class

