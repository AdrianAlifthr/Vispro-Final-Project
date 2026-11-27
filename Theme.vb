Imports System.Drawing.Drawing2D
Imports System.Drawing.Text

''' <summary>
''' Palet warna, font, dan kontrol kustom AsetKu (sesuai desain Figma).
''' </summary>
Public Module Theme
    ' ===== PALET WARNA =====
    Public ReadOnly Orange As Color = ColorTranslator.FromHtml("#E65416")
    Public ReadOnly OrangeHover As Color = ColorTranslator.FromHtml("#CF4A12")
    Public ReadOnly Beige As Color = ColorTranslator.FromHtml("#D8D6C9")
    Public ReadOnly BeigeLight As Color = ColorTranslator.FromHtml("#E9E8DF")
    Public ReadOnly Grey As Color = ColorTranslator.FromHtml("#52555A")
    Public ReadOnly OffWhite As Color = ColorTranslator.FromHtml("#F5F5F5")
    Public ReadOnly Charcoal As Color = ColorTranslator.FromHtml("#303236")
    Public ReadOnly StatusGreen As Color = ColorTranslator.FromHtml("#55795A")
    Public ReadOnly Danger As Color = ColorTranslator.FromHtml("#9A4F4A")

    ' ===== FONT =====
    ' Pakai Inter jika terpasang, jika tidak jatuh ke Segoe UI.
    Private _family As String = Nothing

    Public ReadOnly Property FontFamilyName As String
        Get
            If _family Is Nothing Then
                _family = "Segoe UI"
                Using fc As New InstalledFontCollection()
                    For Each f In fc.Families
                        If f.Name = "Inter" Then _family = "Inter" : Exit For
                    Next
                End Using
            End If
            Return _family
        End Get
    End Property

    ' Ukuran dalam piksel supaya sama dengan angka di Figma.
    Public Function AppFont(sizePx As Single, Optional style As FontStyle = FontStyle.Regular) As Font
        Return New Font(FontFamilyName, sizePx, style, GraphicsUnit.Pixel)
    End Function

    ' ===== HELPER GAMBAR =====
    Public Function RoundedRect(r As Rectangle, radius As Integer) As GraphicsPath
        Dim p As New GraphicsPath()
        Dim d As Integer = Math.Max(1, radius * 2)
        p.AddArc(r.X, r.Y, d, d, 180, 90)
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90)
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90)
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90)
        p.CloseFigure()
        Return p
    End Function

    Public Sub FillRounded(g As Graphics, r As Rectangle, radius As Integer, fill As Color, Optional border As Color = Nothing)
        g.SmoothingMode = SmoothingMode.AntiAlias
        Using path = RoundedRect(r, radius)
            Using b As New SolidBrush(fill)
                g.FillPath(b, path)
            End Using
            If border <> Color.Empty Then
                Using pen As New Pen(border, 1)
                    g.DrawPath(pen, path)
                End Using
            End If
        End Using
    End Sub

    ''' <summary>Gambar teks rata kiri/tengah di dalam kotak.</summary>
    Public Sub DrawText(g As Graphics, text As String, f As Font, c As Color, r As Rectangle, Optional align As StringAlignment = StringAlignment.Near)
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit
        Using sf As New StringFormat() With {.Alignment = align, .LineAlignment = StringAlignment.Center, .Trimming = StringTrimming.EllipsisCharacter, .FormatFlags = StringFormatFlags.NoWrap}
            Using b As New SolidBrush(c)
                g.DrawString(text, f, b, r, sf)
            End Using
        End Using
    End Sub

    ''' <summary>Label sederhana dengan latar transparan.</summary>
    Public Function MakeLabel(text As String, sizePx As Single, color As Color, x As Integer, y As Integer, Optional style As FontStyle = FontStyle.Regular, Optional w As Integer = 0, Optional h As Integer = 0) As Label
        Dim l As New Label() With {
            .Text = text,
            .Font = AppFont(sizePx, style),
            .ForeColor = color,
            .BackColor = Color.Transparent,
            .Location = New Point(x, y)
        }
        If w > 0 AndAlso h > 0 Then
            l.AutoSize = False
            l.Size = New Size(w, h)
        Else
            l.AutoSize = True
        End If
        Return l
    End Function
End Module

' =====================================================================
' KONTROL KUSTOM
' =====================================================================

''' <summary>Panel dengan sudut membulat + border (untuk kartu).</summary>
Public Class RoundedPanel
    Inherits Panel

    Public Property Radius As Integer = 8
    Public Property FillColor As Color = Theme.OffWhite
    Public Property BorderColor As Color = Theme.Beige

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint Or ControlStyles.ResizeRedraw, True)
        BackColor = Theme.OffWhite
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        e.Graphics.Clear(BackColor)
        FillRounded(e.Graphics, New Rectangle(0, 0, Width - 1, Height - 1), Radius, FillColor, BorderColor)
    End Sub
End Class

''' <summary>Panel gradient oranye -> beige (login hero & banner dashboard).</summary>
Public Class GradientPanel
    Inherits Panel

    Public Property Radius As Integer = 0
    Public Property Angle As Single = 30.0F

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint Or ControlStyles.ResizeRedraw, True)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.Clear(If(Parent IsNot Nothing, Parent.BackColor, Theme.OffWhite))
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, Width, Height)
        Using br As New LinearGradientBrush(rect, Theme.Orange, Theme.Beige, Angle)
            Dim cb As New ColorBlend(3)
            cb.Colors = {Theme.Orange, Theme.Beige, Theme.Beige}
            cb.Positions = {0.0F, 0.75F, 1.0F}
            br.InterpolationColors = cb
            If Radius > 0 Then
                Using path = RoundedRect(New Rectangle(0, 0, Width - 1, Height - 1), Radius)
                    g.FillPath(br, path)
                End Using
            Else
                g.FillRectangle(br, rect)
            End If
        End Using
    End Sub
End Class

Public Enum ButtonKind
    Primary
    Outline
    Danger
End Enum

''' <summary>Tombol datar sudut membulat: Primary (oranye) / Outline (garis).</summary>
Public Class AppButton
    Inherits Button

    Public Property Kind As ButtonKind = ButtonKind.Primary
    Private _hover As Boolean = False

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        FlatStyle = FlatStyle.Flat
        FlatAppearance.BorderSize = 0
        Cursor = Cursors.Hand
        Font = AppFont(14, FontStyle.Bold)
        Height = 36
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        _hover = True : Invalidate() : MyBase.OnMouseEnter(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        _hover = False : Invalidate() : MyBase.OnMouseLeave(e)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.Clear(If(Parent IsNot Nothing, Parent.BackColor, Theme.OffWhite))
        Dim r As New Rectangle(0, 0, Width - 1, Height - 1)
        If Kind = ButtonKind.Primary Then
            FillRounded(g, r, 6, If(_hover, Theme.OrangeHover, Theme.Orange))
            DrawText(g, Text, Font, Color.White, r, StringAlignment.Center)
        ElseIf Kind = ButtonKind.Danger Then
            FillRounded(g, r, 6, If(_hover, Theme.BeigeLight, Theme.OffWhite), Theme.Danger)
            DrawText(g, Text, Font, Theme.Danger, r, StringAlignment.Center)
        Else
            FillRounded(g, r, 6, If(_hover, Theme.BeigeLight, Theme.OffWhite), Theme.Charcoal)
            DrawText(g, Text, Font, Theme.Charcoal, r, StringAlignment.Center)
        End If
    End Sub
End Class

''' <summary>Kotak input sudut membulat; border jadi oranye saat fokus.</summary>
Public Class AppTextBox
    Inherits Panel

    <System.ComponentModel.Browsable(False), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)>
    Public ReadOnly Property Inner As TextBox
    Private _focus As Boolean = False

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint Or ControlStyles.ResizeRedraw, True)
        Height = 36
        BackColor = Theme.OffWhite
        Inner = New TextBox() With {
            .BorderStyle = BorderStyle.None,
            .Font = AppFont(14),
            .ForeColor = Theme.Charcoal,
            .BackColor = Theme.OffWhite
        }
        Controls.Add(Inner)
        AddHandler Inner.GotFocus, AddressOf OnInnerFocusChanged
        AddHandler Inner.LostFocus, AddressOf OnInnerFocusChanged
        AddHandler Me.Click, AddressOf OnBoxClick
    End Sub

    Public Property PlaceholderText As String
        Get
            Return Inner.PlaceholderText
        End Get
        Set(value As String)
            Inner.PlaceholderText = value
        End Set
    End Property

    <System.ComponentModel.DefaultValue(False)>
    Public Property UseSystemPasswordChar As Boolean
        Get
            Return Inner.UseSystemPasswordChar
        End Get
        Set(value As Boolean)
            Inner.UseSystemPasswordChar = value
        End Set
    End Property

    Private Sub OnInnerFocusChanged(sender As Object, e As EventArgs)
        _focus = Inner.Focused
        Invalidate()
    End Sub

    Private Sub OnBoxClick(sender As Object, e As EventArgs)
        Inner.Focus()
    End Sub

    Protected Overrides Sub OnLayout(e As LayoutEventArgs)
        MyBase.OnLayout(e)
        If Inner Is Nothing Then Return
        Inner.Left = 12
        Inner.Width = Width - 24 - RightPadding
        Inner.Top = (Height - Inner.Height) \ 2
    End Sub

    ''' <summary>Ruang kosong di kanan (untuk tombol "Tampilkan").</summary>
    Public Property RightPadding As Integer = 0

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        e.Graphics.Clear(If(Parent IsNot Nothing, Parent.BackColor, Theme.OffWhite))
        FillRounded(e.Graphics, New Rectangle(0, 0, Width - 1, Height - 1), 6, Theme.OffWhite, If(_focus, Theme.Orange, Theme.Beige))
    End Sub
End Class

''' <summary>Item menu sidebar. Aktif = latar abu + indikator oranye di kiri.</summary>
Public Class SidebarItem
    Inherits Control

    Public Property IsActive As Boolean = False
    Public Property Icon As Image   ' opsional: ekspor ikon 18x18 putih dari Figma
    Private _hover As Boolean = False

    Public Sub New()
        Me.New("Menu")
    End Sub

    Public Sub New(caption As String)
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        Text = caption
        Size = New Size(216, 40)
        Cursor = Cursors.Hand
        Font = AppFont(14)
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        _hover = True : Invalidate() : MyBase.OnMouseEnter(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        _hover = False : Invalidate() : MyBase.OnMouseLeave(e)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.Clear(Theme.Charcoal)
        Dim r As New Rectangle(0, 0, Width - 1, Height - 1)
        If IsActive Then
            FillRounded(g, r, 6, Theme.Grey)
            g.FillRectangle(New SolidBrush(Theme.Orange), 0, 8, 3, 24)
        ElseIf _hover Then
            FillRounded(g, r, 6, Color.FromArgb(60, 62, 66))
        End If
        If Icon IsNot Nothing Then g.DrawImage(Icon, 14, 11, 18, 18)
        DrawText(g, Text, Font, If(IsActive, Theme.Beige, Color.FromArgb(226, 225, 218)), New Rectangle(44, 0, Width - 48, Height), StringAlignment.Near)
    End Sub
End Class
