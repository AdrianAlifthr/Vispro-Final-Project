<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormMain
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
        Me.sidebar = New System.Windows.Forms.Panel()
        Me.lblBrand = New System.Windows.Forms.Label()
        Me.brandDot = New System.Windows.Forms.Panel()
        Me.lblBrandCaption = New System.Windows.Forms.Label()
        Me.menu0 = New AsetKu.SidebarItem()
        Me.menu1 = New AsetKu.SidebarItem()
        Me.menu2 = New AsetKu.SidebarItem()
        Me.menu3 = New AsetKu.SidebarItem()
        Me.menu4 = New AsetKu.SidebarItem()
        Me.menu5 = New AsetKu.SidebarItem()
        Me.menu6 = New AsetKu.SidebarItem()
        Me.menu7 = New AsetKu.SidebarItem()
        Me.menu8 = New AsetKu.SidebarItem()
        Me.sidebarDivider = New System.Windows.Forms.Panel()
        Me.lblHelp = New System.Windows.Forms.Label()
        Me.lblVersion = New System.Windows.Forms.Label()
        Me.topbar = New System.Windows.Forms.Panel()
        Me._lblTitle = New System.Windows.Forms.Label()
        Me.txtSearch = New AsetKu.AppTextBox()
        Me.lblNotification = New System.Windows.Forms.Label()
        Me.avatar = New System.Windows.Forms.Panel()
        Me.lblUserName = New System.Windows.Forms.Label()
        Me.lblRole = New System.Windows.Forms.Label()
        Me._content = New System.Windows.Forms.Panel()
        Me.SuspendLayout()
        Me.sidebar.SuspendLayout()
        Me.topbar.SuspendLayout()
        Me._content.SuspendLayout()
        ' sidebar
        Me.sidebar.Name = "sidebar"
        Me.sidebar.Location = New System.Drawing.Point(0, 0)
        Me.sidebar.Size = New System.Drawing.Size(240, 768)
        Me.sidebar.BackColor = System.Drawing.Color.FromArgb(48, 50, 54)
        ' lblBrand
        Me.lblBrand.Name = "lblBrand"
        Me.lblBrand.Location = New System.Drawing.Point(24, 26)
        Me.lblBrand.Size = New System.Drawing.Size(450, 24)
        Me.lblBrand.AutoSize = True
        Me.lblBrand.Text = "AsetKu"
        Me.lblBrand.Font = New System.Drawing.Font("Segoe UI", 28.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel)
        Me.lblBrand.ForeColor = System.Drawing.Color.FromArgb(245, 245, 245)
        Me.lblBrand.BackColor = System.Drawing.Color.Transparent
        ' brandDot
        Me.brandDot.Name = "brandDot"
        Me.brandDot.Location = New System.Drawing.Point(132, 48)
        Me.brandDot.Size = New System.Drawing.Size(8, 8)
        Me.brandDot.BackColor = System.Drawing.Color.FromArgb(230, 84, 22)
        ' lblBrandCaption
        Me.lblBrandCaption.Name = "lblBrandCaption"
        Me.lblBrandCaption.Location = New System.Drawing.Point(24, 70)
        Me.lblBrandCaption.Size = New System.Drawing.Size(450, 24)
        Me.lblBrandCaption.AutoSize = True
        Me.lblBrandCaption.Text = "INVENTORY && ASSET"
        Me.lblBrandCaption.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel)
        Me.lblBrandCaption.ForeColor = System.Drawing.Color.FromArgb(216, 214, 201)
        Me.lblBrandCaption.BackColor = System.Drawing.Color.Transparent
        ' menu0
        Me.menu0.Name = "menu0"
        Me.menu0.Location = New System.Drawing.Point(12, 116)
        Me.menu0.Size = New System.Drawing.Size(216, 40)
        Me.menu0.Text = "Dashboard"
        Me.menu0.Tag = "0"
        Me.menu0.IsActive = True
        ' menu1
        Me.menu1.Name = "menu1"
        Me.menu1.Location = New System.Drawing.Point(12, 164)
        Me.menu1.Size = New System.Drawing.Size(216, 40)
        Me.menu1.Text = "Data Aset / Barang"
        Me.menu1.Tag = "1"
        Me.menu1.IsActive = False
        ' menu2
        Me.menu2.Name = "menu2"
        Me.menu2.Location = New System.Drawing.Point(12, 212)
        Me.menu2.Size = New System.Drawing.Size(216, 40)
        Me.menu2.Text = "Barang Masuk"
        Me.menu2.Tag = "2"
        Me.menu2.IsActive = False
        ' menu3
        Me.menu3.Name = "menu3"
        Me.menu3.Location = New System.Drawing.Point(12, 260)
        Me.menu3.Size = New System.Drawing.Size(216, 40)
        Me.menu3.Text = "Barang Keluar"
        Me.menu3.Tag = "3"
        Me.menu3.IsActive = False
        ' menu4
        Me.menu4.Name = "menu4"
        Me.menu4.Location = New System.Drawing.Point(12, 308)
        Me.menu4.Size = New System.Drawing.Size(216, 40)
        Me.menu4.Text = "Peminjaman Aset"
        Me.menu4.Tag = "4"
        Me.menu4.IsActive = False
        ' menu5
        Me.menu5.Name = "menu5"
        Me.menu5.Location = New System.Drawing.Point(12, 356)
        Me.menu5.Size = New System.Drawing.Size(216, 40)
        Me.menu5.Text = "Pemeliharaan"
        Me.menu5.Tag = "5"
        Me.menu5.IsActive = False
        ' menu6
        Me.menu6.Name = "menu6"
        Me.menu6.Location = New System.Drawing.Point(12, 404)
        Me.menu6.Size = New System.Drawing.Size(216, 40)
        Me.menu6.Text = "Master Data"
        Me.menu6.Tag = "6"
        Me.menu6.IsActive = False
        ' menu7
        Me.menu7.Name = "menu7"
        Me.menu7.Location = New System.Drawing.Point(12, 452)
        Me.menu7.Size = New System.Drawing.Size(216, 40)
        Me.menu7.Text = "Laporan"
        Me.menu7.Tag = "7"
        Me.menu7.IsActive = False
        ' menu8
        Me.menu8.Name = "menu8"
        Me.menu8.Location = New System.Drawing.Point(12, 500)
        Me.menu8.Size = New System.Drawing.Size(216, 40)
        Me.menu8.Text = "Manajemen User"
        Me.menu8.Tag = "8"
        Me.menu8.IsActive = False
        ' sidebarDivider
        Me.sidebarDivider.Name = "sidebarDivider"
        Me.sidebarDivider.Location = New System.Drawing.Point(24, 620)
        Me.sidebarDivider.Size = New System.Drawing.Size(192, 1)
        Me.sidebarDivider.BackColor = System.Drawing.Color.FromArgb(82, 85, 90)
        ' lblHelp
        Me.lblHelp.Name = "lblHelp"
        Me.lblHelp.Location = New System.Drawing.Point(24, 644)
        Me.lblHelp.Size = New System.Drawing.Size(450, 24)
        Me.lblHelp.AutoSize = True
        Me.lblHelp.Text = "Bantuan && panduan"
        Me.lblHelp.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel)
        Me.lblHelp.ForeColor = System.Drawing.Color.FromArgb(216, 214, 201)
        Me.lblHelp.BackColor = System.Drawing.Color.Transparent
        ' lblVersion
        Me.lblVersion.Name = "lblVersion"
        Me.lblVersion.Location = New System.Drawing.Point(24, 710)
        Me.lblVersion.Size = New System.Drawing.Size(450, 24)
        Me.lblVersion.AutoSize = True
        Me.lblVersion.Text = "AsetKu Desktop  •  v1.0"
        Me.lblVersion.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel)
        Me.lblVersion.ForeColor = System.Drawing.Color.FromArgb(216, 214, 201)
        Me.lblVersion.BackColor = System.Drawing.Color.Transparent
        ' topbar
        Me.topbar.Name = "topbar"
        Me.topbar.Location = New System.Drawing.Point(240, 0)
        Me.topbar.Size = New System.Drawing.Size(1126, 64)
        Me.topbar.BackColor = System.Drawing.Color.FromArgb(245, 245, 245)
        ' _lblTitle
        Me._lblTitle.Name = "_lblTitle"
        Me._lblTitle.Location = New System.Drawing.Point(24, 20)
        Me._lblTitle.Size = New System.Drawing.Size(450, 24)
        Me._lblTitle.AutoSize = True
        Me._lblTitle.Text = "Inventaris"
        Me._lblTitle.Font = New System.Drawing.Font("Segoe UI", 18.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel)
        Me._lblTitle.ForeColor = System.Drawing.Color.FromArgb(48, 50, 54)
        Me._lblTitle.BackColor = System.Drawing.Color.Transparent
        ' txtSearch
        Me.txtSearch.Name = "txtSearch"
        Me.txtSearch.Location = New System.Drawing.Point(478, 14)
        Me.txtSearch.Size = New System.Drawing.Size(288, 36)
        Me.txtSearch.PlaceholderText = "Cari aset, transaksi..."
        ' lblNotification
        Me.lblNotification.Name = "lblNotification"
        Me.lblNotification.Location = New System.Drawing.Point(792, 20)
        Me.lblNotification.Size = New System.Drawing.Size(450, 24)
        Me.lblNotification.AutoSize = True
        Me.lblNotification.Text = "🔔"
        Me.lblNotification.Font = New System.Drawing.Font("Segoe UI", 14.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel)
        Me.lblNotification.ForeColor = System.Drawing.Color.FromArgb(48, 50, 54)
        Me.lblNotification.BackColor = System.Drawing.Color.Transparent
        ' avatar
        Me.avatar.Name = "avatar"
        Me.avatar.Location = New System.Drawing.Point(848, 16)
        Me.avatar.Size = New System.Drawing.Size(32, 32)
        Me.avatar.BackColor = System.Drawing.Color.FromArgb(245, 245, 245)
        ' lblUserName
        Me.lblUserName.Name = "lblUserName"
        Me.lblUserName.Location = New System.Drawing.Point(892, 14)
        Me.lblUserName.Size = New System.Drawing.Size(450, 24)
        Me.lblUserName.AutoSize = True
        Me.lblUserName.Text = "Pengguna"
        Me.lblUserName.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel)
        Me.lblUserName.ForeColor = System.Drawing.Color.FromArgb(48, 50, 54)
        Me.lblUserName.BackColor = System.Drawing.Color.Transparent
        ' lblRole
        Me.lblRole.Name = "lblRole"
        Me.lblRole.Location = New System.Drawing.Point(892, 34)
        Me.lblRole.Size = New System.Drawing.Size(450, 24)
        Me.lblRole.AutoSize = True
        Me.lblRole.Text = "Administrator"
        Me.lblRole.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel)
        Me.lblRole.ForeColor = System.Drawing.Color.FromArgb(82, 85, 90)
        Me.lblRole.BackColor = System.Drawing.Color.Transparent
        ' _content
        Me._content.Name = "_content"
        Me._content.Location = New System.Drawing.Point(240, 64)
        Me._content.Size = New System.Drawing.Size(1126, 704)
        Me._content.BackColor = System.Drawing.Color.FromArgb(245, 245, 245)
        Me.Controls.Add(Me.sidebar)
        Me.sidebar.Controls.Add(Me.lblBrand)
        Me.sidebar.Controls.Add(Me.brandDot)
        Me.sidebar.Controls.Add(Me.lblBrandCaption)
        Me.sidebar.Controls.Add(Me.menu0)
        Me.sidebar.Controls.Add(Me.menu1)
        Me.sidebar.Controls.Add(Me.menu2)
        Me.sidebar.Controls.Add(Me.menu3)
        Me.sidebar.Controls.Add(Me.menu4)
        Me.sidebar.Controls.Add(Me.menu5)
        Me.sidebar.Controls.Add(Me.menu6)
        Me.sidebar.Controls.Add(Me.menu7)
        Me.sidebar.Controls.Add(Me.menu8)
        Me.sidebar.Controls.Add(Me.sidebarDivider)
        Me.sidebar.Controls.Add(Me.lblHelp)
        Me.sidebar.Controls.Add(Me.lblVersion)
        Me.Controls.Add(Me.topbar)
        Me.topbar.Controls.Add(Me._lblTitle)
        Me.topbar.Controls.Add(Me.txtSearch)
        Me.topbar.Controls.Add(Me.lblNotification)
        Me.topbar.Controls.Add(Me.avatar)
        Me.topbar.Controls.Add(Me.lblUserName)
        Me.topbar.Controls.Add(Me.lblRole)
        Me.Controls.Add(Me._content)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None
        Me.ClientSize = New System.Drawing.Size(1366, 768)
        Me.BackColor = System.Drawing.Color.FromArgb(245, 245, 245)
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.Name = "FormMain"
        Me.Text = "AsetKu - Inventaris & Manajemen Aset"
        Me.sidebar.ResumeLayout(False)
        Me.sidebar.PerformLayout()
        Me.topbar.ResumeLayout(False)
        Me.topbar.PerformLayout()
        Me._content.ResumeLayout(False)
        Me._content.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Friend WithEvents sidebar As System.Windows.Forms.Panel
    Friend WithEvents lblBrand As System.Windows.Forms.Label
    Friend WithEvents brandDot As System.Windows.Forms.Panel
    Friend WithEvents lblBrandCaption As System.Windows.Forms.Label
    Friend WithEvents menu0 As AsetKu.SidebarItem
    Friend WithEvents menu1 As AsetKu.SidebarItem
    Friend WithEvents menu2 As AsetKu.SidebarItem
    Friend WithEvents menu3 As AsetKu.SidebarItem
    Friend WithEvents menu4 As AsetKu.SidebarItem
    Friend WithEvents menu5 As AsetKu.SidebarItem
    Friend WithEvents menu6 As AsetKu.SidebarItem
    Friend WithEvents menu7 As AsetKu.SidebarItem
    Friend WithEvents menu8 As AsetKu.SidebarItem
    Friend WithEvents sidebarDivider As System.Windows.Forms.Panel
    Friend WithEvents lblHelp As System.Windows.Forms.Label
    Friend WithEvents lblVersion As System.Windows.Forms.Label
    Friend WithEvents topbar As System.Windows.Forms.Panel
    Friend WithEvents _lblTitle As System.Windows.Forms.Label
    Friend WithEvents txtSearch As AsetKu.AppTextBox
    Friend WithEvents lblNotification As System.Windows.Forms.Label
    Friend WithEvents avatar As System.Windows.Forms.Panel
    Friend WithEvents lblUserName As System.Windows.Forms.Label
    Friend WithEvents lblRole As System.Windows.Forms.Label
    Friend WithEvents _content As System.Windows.Forms.Panel
End Class

