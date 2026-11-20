Imports System.Drawing.Drawing2D
Imports System.IO

''' <summary>Helper & kontrol tambahan: dropdown, tanggal, kotak foto, badge status, gaya tabel.</summary>
Public Module UiExtras

    ''' <summary>Gambar badge status; mengembalikan area yang terpakai.</summary>
    Public Function DrawBadge(g As Graphics, text As String, x As Integer, centerY As Integer) As Rectangle
        Dim f = AppFont(12, FontStyle.Bold)
        Dim w = TextRenderer.MeasureText(text, f, Size.Empty, TextFormatFlags.NoPadding).Width + 22
        Dim h = 26
        Dim r As New Rectangle(x, centerY - h \ 2, w, h)
        Select Case text
            Case "Stok Menipis"
                FillRounded(g, New Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1), 4, Color.White, Theme.Orange)
                DrawText(g, text, f, Theme.Orange, r, StringAlignment.Center)
            Case Else
                Dim bg As Color
                Select Case text
                    Case "Tersedia" : bg = Theme.StatusGreen
                    Case "Dipinjam" : bg = Theme.Orange
                    Case "Maintenance" : bg = ColorTranslator.FromHtml("#7B6A2C")
                    Case "Rusak" : bg = ColorTranslator.FromHtml("#9A4F4A")
                    Case Else : bg = Theme.Grey
                End Select
                FillRounded(g, r, 4, bg)
                DrawText(g, text, f, Color.White, r, StringAlignment.Center)
        End Select
        Return r
    End Function

    ''' <summary>Gaya tabel standar AsetKu (header beige, baris selang-seling).</summary>
    Public Sub StyleGrid(g As DataGridView, rowHeight As Integer)
        g.AllowUserToAddRows = False
        g.AllowUserToDeleteRows = False
        g.AllowUserToResizeRows = False
        g.AllowUserToResizeColumns = False
        g.ReadOnly = True
        g.RowHeadersVisible = False
        g.ScrollBars = ScrollBars.None
        g.BorderStyle = BorderStyle.None
        g.CellBorderStyle = DataGridViewCellBorderStyle.None
        g.EnableHeadersVisualStyles = False
        g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
        g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        g.ColumnHeadersHeight = 40
        g.RowTemplate = New DataGridViewRow() With {.Height = rowHeight}
        g.BackgroundColor = Theme.OffWhite
        g.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        g.MultiSelect = False

        With g.ColumnHeadersDefaultCellStyle
            .BackColor = Theme.Beige
            .ForeColor = Theme.Charcoal
            .Font = AppFont(13, FontStyle.Bold)
            .Padding = New Padding(13, 0, 0, 0)
            .SelectionBackColor = Theme.Beige
            .SelectionForeColor = Theme.Charcoal
        End With
        With g.DefaultCellStyle
            .BackColor = Theme.OffWhite
            .ForeColor = Theme.Grey
            .Font = AppFont(13)
            .Padding = New Padding(13, 0, 0, 0)
            .SelectionBackColor = Theme.OffWhite
            .SelectionForeColor = Theme.Grey
        End With
        With g.AlternatingRowsDefaultCellStyle
            .BackColor = Theme.Beige
            .SelectionBackColor = Theme.Beige
            .SelectionForeColor = Theme.Grey
        End With
        AddHandler g.SelectionChanged, Sub(s, e) g.ClearSelection()
    End Sub
End Module

''' <summary>Dropdown sudut membulat (daftar muncul sebagai menu).</summary>
Public Class AppComboBox
    Inherits Panel

    Public ReadOnly Property Items As New List(Of String)
    Public Property PlaceholderText As String = ""
    Public Event SelectedIndexChanged As EventHandler

    Private _selected As Integer = -1
    Private _open As Boolean = False

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint Or ControlStyles.ResizeRedraw, True)
        Height = 36
        Cursor = Cursors.Hand
        BackColor = Theme.OffWhite
    End Sub

    Public Sub SetItems(ParamArray values() As String)
        Items.Clear()
        Items.AddRange(values)
        _selected = -1
        Invalidate()
    End Sub

    Public Property SelectedIndex As Integer
        Get
            Return _selected
        End Get
        Set(value As Integer)
            _selected = value
            Invalidate()
            RaiseEvent SelectedIndexChanged(Me, EventArgs.Empty)
        End Set
    End Property

    Public ReadOnly Property SelectedItem As String
        Get
            If _selected >= 0 AndAlso _selected < Items.Count Then Return Items(_selected)
            Return Nothing
        End Get
    End Property

    ''' <summary>Pilih berdasarkan teks (abaikan jika tidak ada).</summary>
    Public Sub SelectByText(value As String)
        Dim i = Items.IndexOf(value)
        If i >= 0 Then SelectedIndex = i
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        Dim menu As New ContextMenuStrip() With {.Font = AppFont(14), .ShowImageMargin = False, .BackColor = Theme.OffWhite}
        For i = 0 To Items.Count - 1
            Dim idx As Integer = i
            Dim mi As New ToolStripMenuItem(Items(i)) With {.AutoSize = False, .Size = New Size(Math.Max(Width - 4, 120), 30), .ForeColor = Theme.Charcoal}
            AddHandler mi.Click, Sub(s, ev)
                                     SelectedIndex = idx
                                 End Sub
            menu.Items.Add(mi)
        Next
        AddHandler menu.Closed, Sub(s, ev)
                                    _open = False
                                    Invalidate()
                                End Sub
        _open = True
        Invalidate()
        menu.Show(Me, New Point(0, Height + 2))
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.Clear(If(Parent IsNot Nothing, Parent.BackColor, Theme.OffWhite))
        FillRounded(g, New Rectangle(0, 0, Width - 1, Height - 1), 6, Theme.OffWhite, If(_open, Theme.Orange, Theme.Beige))
        Dim txt = If(SelectedItem, PlaceholderText)
        Dim c = If(SelectedItem Is Nothing, Theme.Grey, Theme.Charcoal)
        DrawText(g, txt, AppFont(14), c, New Rectangle(12, 0, Width - 40, Height))
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim cy = Height \ 2
        Using p As New Pen(Theme.Grey, 1.5F)
            g.DrawLines(p, {New Point(Width - 24, cy - 2), New Point(Width - 19, cy + 3), New Point(Width - 14, cy - 2)})
        End Using
    End Sub
End Class

''' <summary>Input tanggal: DateTimePicker di dalam kotak membulat.</summary>
Public Class AppDateBox
    Inherits Panel

    Public ReadOnly Property Picker As DateTimePicker

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint Or ControlStyles.ResizeRedraw, True)
        Height = 36
        BackColor = Theme.OffWhite
        Picker = New DateTimePicker() With {
            .Format = DateTimePickerFormat.Custom,
            .CustomFormat = "dd/MM/yyyy",
            .Font = AppFont(14),
            .CalendarFont = New Font(Theme.FontFamilyName, 12, GraphicsUnit.Point),
            .Value = Date.Today
        }
        Controls.Add(Picker)
    End Sub

    Protected Overrides Sub OnLayout(e As LayoutEventArgs)
        MyBase.OnLayout(e)
        If Picker Is Nothing Then Return
        Picker.Left = 8
        Picker.Width = Width - 16
        Picker.Top = (Height - Picker.Height) \ 2
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        e.Graphics.Clear(If(Parent IsNot Nothing, Parent.BackColor, Theme.OffWhite))
        FillRounded(e.Graphics, New Rectangle(0, 0, Width - 1, Height - 1), 6, Theme.OffWhite, Theme.Beige)
    End Sub
End Class

''' <summary>Kotak pilih foto dengan pratinjau.</summary>
Public Class PhotoBox
    Inherits Control

    Private _img As Image
    Public Property ImagePath As String = Nothing

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint Or ControlStyles.ResizeRedraw, True)
        Cursor = Cursors.Hand
        Size = New Size(300, 180)
    End Sub

    Public Sub LoadImage(path As String)
        If _img IsNot Nothing Then _img.Dispose()
        _img = Nothing
        ' baca lewat memori supaya file tidak terkunci
        Using ms As New MemoryStream(File.ReadAllBytes(path))
            _img = New Bitmap(Image.FromStream(ms))
        End Using
        ImagePath = path
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.Clear(If(Parent IsNot Nothing, Parent.BackColor, Theme.OffWhite))
        FillRounded(g, New Rectangle(0, 0, Width - 1, Height - 1), 8, Theme.OffWhite, Theme.Beige)
        If _img IsNot Nothing Then
            Dim box As New Rectangle(8, 8, Width - 17, Height - 17)
            Dim ratio = Math.Min(box.Width / _img.Width, box.Height / _img.Height)
            Dim w = CInt(_img.Width * ratio), h = CInt(_img.Height * ratio)
            g.InterpolationMode = InterpolationMode.HighQualityBicubic
            g.DrawImage(_img, box.X + (box.Width - w) \ 2, box.Y + (box.Height - h) \ 2, w, h)
        Else
            DrawText(g, ChrW(&H2191), AppFont(26, FontStyle.Regular), Theme.Charcoal, New Rectangle(0, Height \ 2 - 52, Width, 40), StringAlignment.Center)
            DrawText(g, "Pilih foto untuk aset ini", AppFont(14), Theme.Grey, New Rectangle(0, Height \ 2 - 12, Width, 24), StringAlignment.Center)
        End If
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing AndAlso _img IsNot Nothing Then _img.Dispose()
        MyBase.Dispose(disposing)
    End Sub
End Class

''' <summary>Badge status sebagai kontrol (ukuran menyesuaikan teks).</summary>
Public Class StatusBadge
    Inherits Control

    Private _status As String = ""

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint Or ControlStyles.SupportsTransparentBackColor, True)
    End Sub

    Public Property Status As String
        Get
            Return _status
        End Get
        Set(value As String)
            _status = value
            Dim f = AppFont(12, FontStyle.Bold)
            Size = New Size(TextRenderer.MeasureText(value, f, Size.Empty, TextFormatFlags.NoPadding).Width + 23, 26)
            Invalidate()
        End Set
    End Property

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        e.Graphics.Clear(If(Parent IsNot Nothing, Parent.BackColor, Theme.OffWhite))
        DrawBadge(e.Graphics, _status, 0, Height \ 2)
    End Sub
End Class

''' <summary>Dialog konfirmasi modal (latar gelap + kotak di tengah) sesuai desain Figma.</summary>
Public Class ConfirmDialog
    Inherits Form

    Private Sub New(title As String, message As String, confirmText As String)
        FormBorderStyle = FormBorderStyle.None
        StartPosition = FormStartPosition.CenterParent
        ShowInTaskbar = False
        ClientSize = New Size(560, 244)
        BackColor = Theme.OffWhite
        Region = New Region(RoundedRect(New Rectangle(0, 0, Width, Height), 8))

        Controls.Add(MakeLabel(title, 18, Theme.Charcoal, 24, 26, FontStyle.Bold))
        Controls.Add(MakeLabel(message, 14, Theme.Grey, 24, 76, FontStyle.Regular, 512, 48))

        Dim btnCancel As New AppButton() With {.Text = "Batal", .Kind = ButtonKind.Outline, .Location = New Point(280, 183), .Size = New Size(120, 36), .DialogResult = DialogResult.Cancel}
        Dim btnOk As New AppButton() With {.Text = confirmText, .Kind = ButtonKind.Danger, .Location = New Point(416, 183), .Size = New Size(120, 36), .DialogResult = DialogResult.OK}
        Controls.Add(btnCancel)
        Controls.Add(btnOk)
        CancelButton = btnCancel
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Using p As New Pen(Theme.Beige, 1)
            e.Graphics.DrawLine(p, 0, 159, Width, 159)
        End Using
    End Sub

    ''' <summary>Tampilkan dialog di atas jendela owner. True jika pengguna menekan tombol konfirmasi.</summary>
    Public Shared Function Ask(owner As Form, title As String, message As String, confirmText As String) As Boolean
        Using overlay As New Form()
            overlay.FormBorderStyle = FormBorderStyle.None
            overlay.StartPosition = FormStartPosition.Manual
            overlay.ShowInTaskbar = False
            overlay.BackColor = Theme.Charcoal
            overlay.Opacity = 0.45
            overlay.Bounds = owner.RectangleToScreen(owner.ClientRectangle)
            overlay.Show(owner)
            Using dlg As New ConfirmDialog(title, message, confirmText)
                Dim result = dlg.ShowDialog(overlay)
                overlay.Close()
                Return result = DialogResult.OK
            End Using
        End Using
    End Function
End Class

''' <summary>Helper tambahan: badge garis & penampil dialog modal di atas area tertentu.</summary>
Public Module UiMore

    ''' <summary>Badge outline (teks & garis berwarna), dipakai di daftar peminjaman.</summary>
    Public Function DrawOutlineBadge(g As Graphics, text As String, x As Integer, centerY As Integer, c As Color) As Rectangle
        Dim f = AppFont(12, FontStyle.Bold)
        Dim w = TextRenderer.MeasureText(text, f, Size.Empty, TextFormatFlags.NoPadding).Width + 22
        Dim r As New Rectangle(x, centerY - 12, w, 24)
        FillRounded(g, New Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1), 4, Theme.OffWhite, c)
        DrawText(g, text, f, c, r, StringAlignment.Center)
        Return r
    End Function

    ''' <summary>
    ''' Gelapkan area (koordinat client milik owner) lalu tampilkan dlg di tengahnya.
    ''' Area sidebar/top bar di luar area tetap terang, seperti di desain.
    ''' </summary>
    Public Function ShowDialogOver(owner As Form, areaClient As Rectangle, dlg As Form) As DialogResult
        Using overlay As New Form()
            overlay.FormBorderStyle = FormBorderStyle.None
            overlay.StartPosition = FormStartPosition.Manual
            overlay.ShowInTaskbar = False
            overlay.BackColor = Theme.Charcoal
            overlay.Opacity = 0.45
            Dim area = owner.RectangleToScreen(areaClient)
            overlay.Bounds = area
            overlay.Show(owner)
            dlg.StartPosition = FormStartPosition.Manual
            dlg.Location = New Point(area.X + (area.Width - dlg.Width) \ 2, area.Y + (area.Height - dlg.Height) \ 2)
            Dim result = dlg.ShowDialog(overlay)
            overlay.Close()
            Return result
        End Using
    End Function
End Module
