Imports System.Drawing.Drawing2D

''' <summary>Strip tab "Informasi | Riwayat Transaksi | Pemeliharaan" sesuai desain.</summary>
Public Class DetailTabs
    Inherits Control

    Private Shared ReadOnly Captions As String() = {"Informasi", "Riwayat Transaksi", "Pemeliharaan"}
    Private Shared ReadOnly Widths As Integer() = {178, 292, 244}
    Private _selected As Integer = 0
    Public Event SelectedChanged As EventHandler

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        Cursor = Cursors.Hand
        Size = New Size(712, 48)
    End Sub

    Public Property SelectedIndex As Integer
        Get
            Return _selected
        End Get
        Set(value As Integer)
            If value = _selected Then Return
            _selected = value
            Invalidate()
            RaiseEvent SelectedChanged(Me, EventArgs.Empty)
        End Set
    End Property

    Private Function TabAt(x As Integer) As Integer
        Dim start = 0
        For i = 0 To Widths.Length - 1
            If x >= start AndAlso x < start + Widths(i) Then Return i
            start += Widths(i)
        Next
        Return -1
    End Function

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        Dim i = TabAt(e.X)
        If i >= 0 Then SelectedIndex = i
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        Dim x = 0
        For i = 0 To Captions.Length - 1
            Dim w = If(i = Captions.Length - 1, Width - x, Widths(i))
            Dim r As New Rectangle(x, 0, w, Height)
            Dim active = (i = _selected)
            Using b As New SolidBrush(If(active, Theme.OffWhite, Theme.Beige))
                g.FillRectangle(b, r)
            End Using
            If active Then
                Using b As New SolidBrush(Theme.Orange)
                    g.FillRectangle(b, x, Height - 3, w, 3)
                End Using
            End If
            DrawText(g, Captions(i), AppFont(14, If(active, FontStyle.Bold, FontStyle.Regular)), If(active, Theme.Orange, Theme.Grey), New Rectangle(x + 20, 0, w - 20, Height - 2))
            x += Widths(i)
        Next
    End Sub
End Class

''' <summary>Halaman Detail Aset: kartu ringkasan, 3 tab, dan aktivitas terakhir.</summary>
Public Class ucAsetDetail
    Inherits UserControl

    Private ReadOnly _row As AssetRow
    Private ReadOnly _tabs As DetailTabs
    Private ReadOnly _panels(2) As Panel

    Public Sub New(row As AssetRow)
        _row = row
        BackColor = Theme.OffWhite
        DoubleBuffered = True

        ' ===== Breadcrumb, judul, tombol Edit =====
        Dim crumb = MakeLabel("Data Aset / Barang", 12, Theme.Grey, 24, 20)
        crumb.Cursor = Cursors.Hand
        AddHandler crumb.Click, AddressOf GoBack
        Controls.Add(crumb)
        Controls.Add(MakeLabel("/", 12, Theme.Grey, 134, 20))
        Controls.Add(MakeLabel("Detail Aset", 12, Theme.Grey, 155, 20))
        Controls.Add(MakeLabel(row.Nama, 26, Theme.Charcoal, 24, 46, FontStyle.Bold))

        Dim btnEdit As New AppButton() With {.Text = "Edit Aset", .Kind = ButtonKind.Outline, .Location = New Point(966, 48), .Size = New Size(136, 36), .BackColor = Theme.OffWhite}
        AddHandler btnEdit.Click, AddressOf OnEditClick
        Controls.Add(btnEdit)

        BuildSummaryCard()
        _tabs = BuildTabCard()
        BuildLatestActivity()
        ShowTab(0)
    End Sub

    ' ---------- kartu kiri: ringkasan (24,104 | 340 x 420) ----------
    Private Sub BuildSummaryCard()
        Dim card As New RoundedPanel() With {.Location = New Point(24, 104), .Size = New Size(340, 420), .Radius = 8}

        ' TODO (Supabase): tampilkan foto dari photo_url. Sementara placeholder.
        Dim photo As New RoundedPanel() With {.Location = New Point(20, 16), .Size = New Size(298, 200), .Radius = 6, .FillColor = Theme.BeigeLight, .BorderColor = Theme.BeigeLight}
        photo.Controls.Add(MakeLabel("Belum ada foto", 14, Theme.Grey, 0, 90, FontStyle.Regular, 298, 20))
        DirectCast(photo.Controls(0), Label).TextAlign = ContentAlignment.MiddleCenter
        card.Controls.Add(photo)

        card.Controls.Add(MakeLabel(_row.Kode, 12, Theme.Grey, 20, 232))
        card.Controls.Add(MakeLabel(_row.Nama, 18, Theme.Charcoal, 20, 262, FontStyle.Bold))
        Dim badge As New StatusBadge() With {.Location = New Point(20, 301)}
        badge.Status = _row.Status
        card.Controls.Add(badge)
        card.Controls.Add(MakeLabel(_row.Jumlah & " unit  •  Kondisi " & _row.Kondisi.ToLower(), 14, Theme.Grey, 20, 350))
        Controls.Add(card)
    End Sub

    ' ---------- kartu kanan: tab (388,104 | 714 x 420) ----------
    Private Function BuildTabCard() As DetailTabs
        Dim card As New RoundedPanel() With {.Location = New Point(388, 104), .Size = New Size(714, 420), .Radius = 8}

        For i = 0 To 2
            _panels(i) = New Panel() With {.Location = New Point(1, 49), .Size = New Size(712, 369), .BackColor = Theme.OffWhite}
            card.Controls.Add(_panels(i))
        Next
        BuildInfoPanel(_panels(0))
        BuildHistoryPanel(_panels(1))
        BuildMaintenancePanel(_panels(2))

        Dim tabs As New DetailTabs() With {.Location = New Point(1, 1)}
        AddHandler tabs.SelectedChanged, AddressOf OnTabChanged
        card.Controls.Add(tabs)
        Controls.Add(card)
        Return tabs
    End Function

    Private Sub OnTabChanged(sender As Object, e As EventArgs)
        ShowTab(_tabs.SelectedIndex)
    End Sub

    Private Sub ShowTab(index As Integer)
        For i = 0 To 2
            _panels(i).Visible = (i = index)
        Next
    End Sub

    Private Sub AddInfo(p As Panel, caption As String, value As String, col As Integer, row As Integer)
        Dim x = If(col = 0, 23, 369)
        p.Controls.Add(MakeLabel(caption, 12, Theme.Grey, x, 23 + row * 66))
        p.Controls.Add(MakeLabel(value, 14, Theme.Charcoal, x, 45 + row * 66, FontStyle.Bold))
    End Sub

    Private Sub BuildInfoPanel(p As Panel)
        ' TODO (Supabase): data di bawah ini contoh; ambil dari tabel assets + suppliers.
        AddInfo(p, "Kategori", _row.Kategori, 0, 0)
        AddInfo(p, "Lokasi", _row.Lokasi, 1, 0)
        AddInfo(p, "Supplier", "PT Teknologi Nusantara", 0, 1)
        AddInfo(p, "Tanggal Perolehan", "1 Mei 2025", 1, 1)
        AddInfo(p, "Harga Perolehan", "Rp 14.500.000", 0, 2)
        AddInfo(p, "Jumlah", _row.Jumlah & " unit", 1, 2)
        AddInfo(p, "Kondisi", _row.Kondisi, 0, 3)
        AddInfo(p, "Penanggung Jawab", "Aditya Pratama", 1, 3)
        p.Controls.Add(New Panel() With {.BackColor = Theme.Beige, .Location = New Point(23, 287), .Size = New Size(666, 1)})
        p.Controls.Add(MakeLabel("Keterangan", 12, Theme.Grey, 23, 302))
        p.Controls.Add(MakeLabel("Laptop operasional untuk tim IT dan pengembangan sistem.", 14, Theme.Charcoal, 23, 327))
    End Sub

    Private Sub BuildHistoryPanel(p As Panel)
        p.Controls.Add(MakeLabel("Riwayat Transaksi", 20, Theme.Charcoal, 23, 21, FontStyle.Bold))
        p.Controls.Add(MakeLabel("Catatan pergerakan dan perubahan stok aset.", 12, Theme.Grey, 23, 54))
        ' TODO (Supabase): SELECT dari stock_transactions & loans WHERE asset_id = ...
        Dim rows As New List(Of String())
        rows.Add({"16 Jun 2025", "Barang masuk · 5 unit", "Aditya Pratama", "BM-202506-001"})
        rows.Add({"12 Jun 2025", "Pengembalian · 1 unit", "Budi Santoso", "PJM-202506-008"})
        rows.Add({"10 Jun 2025", "Peminjaman · 1 unit", "Budi Santoso", "PJM-202506-008"})
        p.Controls.Add(MiniTable({"Tanggal", "Aktivitas", "Petugas", "Referensi"}, {113, 212, 160, 181}, rows, 81))
        p.Controls.Add(MakeLabel("Menampilkan " & rows.Count & " transaksi", 12, Theme.Grey, 23, 316))
    End Sub

    Private Sub BuildMaintenancePanel(p As Panel)
        p.Controls.Add(MakeLabel("Pemeliharaan aset", 20, Theme.Charcoal, 23, 21, FontStyle.Bold))
        p.Controls.Add(MakeLabel("Jadwal dan catatan perawatan aset ini.", 12, Theme.Grey, 23, 54))
        ' TODO (Supabase): SELECT dari maintenances WHERE asset_id = ...
        Dim rows As New List(Of String())
        rows.Add({"01 Jul 2025", "Pemeriksaan berkala", "Rp 0", "Terjadwal"})
        rows.Add({"01 Mei 2025", "Pembersihan & servis", "Rp 350.000", "Selesai"})
        p.Controls.Add(MiniTable({"Tanggal", "Pekerjaan", "Biaya", "Status"}, {113, 263, 140, 150}, rows, 81))
        p.Controls.Add(MakeLabel("Biaya aktual diperbarui setelah pekerjaan selesai.", 12, Theme.Grey, 23, 316))
    End Sub

    ''' <summary>Tabel kecil berbingkai untuk isi tab (header 40, baris 48).</summary>
    Private Function MiniTable(heads As String(), widths As Integer(), rows As List(Of String()), top As Integer) As Control
        Dim height = 41 + rows.Count * 48
        Dim wrap As New RoundedPanel() With {.Location = New Point(23, top), .Size = New Size(666, height), .Radius = 6}
        Dim g As New DataGridView() With {.Location = New Point(1, 1), .Size = New Size(664, height - 2)}
        StyleGrid(g, 48)
        g.ColumnHeadersHeight = 40
        For i = 0 To heads.Length - 1
            g.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = heads(i), .Width = widths(i), .SortMode = DataGridViewColumnSortMode.NotSortable})
        Next
        g.Columns(0).DefaultCellStyle.ForeColor = Theme.Charcoal
        g.Columns(0).DefaultCellStyle.SelectionForeColor = Theme.Charcoal
        For Each r In rows
            g.Rows.Add(r)
        Next
        g.ClearSelection()
        wrap.Controls.Add(g)
        Return wrap
    End Function

    ' ---------- kartu bawah: aktivitas terakhir (24,548 | 1078 x 120) ----------
    Private Sub BuildLatestActivity()
        Dim card As New RoundedPanel() With {.Location = New Point(24, 548), .Size = New Size(1078, 120), .Radius = 8}
        card.Controls.Add(MakeLabel("Aktivitas terakhir", 18, Theme.Charcoal, 20, 20, FontStyle.Bold))
        card.Controls.Add(MakeLabel("16 Jun 2025, 09:42", 12, Theme.Grey, 20, 58))
        card.Controls.Add(MakeLabel("Penambahan stok 5 unit", 14, Theme.Charcoal, 224, 56, FontStyle.Bold))
        card.Controls.Add(MakeLabel("Oleh Aditya Pratama  •  Dokumen BM-202506-001", 12, Theme.Grey, 224, 82))
        Dim btn As New AppButton() With {.Text = "Lihat riwayat", .Kind = ButtonKind.Outline, .Location = New Point(912, 54), .Size = New Size(144, 36), .BackColor = Theme.OffWhite}
        AddHandler btn.Click, Sub(s, e)
                                  _tabs.SelectedIndex = 1
                              End Sub
        card.Controls.Add(btn)
        Controls.Add(card)
    End Sub

    ' ---------- navigasi ----------
    Private Sub OnEditClick(sender As Object, e As EventArgs)
        TryCast(FindForm(), FormMain)?.ShowPage(New ucAsetForm(_row), 1)
    End Sub

    Private Sub GoBack(sender As Object, e As EventArgs)
        TryCast(FindForm(), FormMain)?.ShowPage(New ucAsetList(), 1)
    End Sub
End Class
