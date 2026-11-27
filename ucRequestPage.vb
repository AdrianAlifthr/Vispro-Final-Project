Imports System
Imports System.Text.Json
Imports System.Threading.Tasks

''' <summary>Pengajuan user dan antrean verifikasi admin.</summary>
Public Class ucRequestPage
    Inherits UserControl

    Private ReadOnly _mode As String
    Private ReadOnly _rows As New List(Of AssetRequestRow)()
    Private ReadOnly _assetIds As New Dictionary(Of String, String)()
    Private ReadOnly _grid As DataGridView
    Private ReadOnly _info As Label
    Private _assets As ComboBox
    Private _quantity As NumericUpDown
    Private _dueDate As DateTimePicker
    Private _notes As TextBox
    Private _busy As Boolean

    Public Sub New(mode As String)
        _mode = mode
        BackColor = Theme.OffWhite
        Dim title = If(mode = "admin", "Verifikasi Pengajuan", If(mode = "out", "Ajukan Barang Keluar", If(mode = "loan", "Ajukan Peminjaman", "Dashboard User")))
        Controls.Add(MakeLabel(title, 26, Theme.Charcoal, 24, 22, FontStyle.Bold))
        Controls.Add(MakeLabel(If(mode = "admin", "Pantau permintaan semua divisi dan verifikasi sebelum transaksi berlaku.",
                                 "Divisi: " & SupabaseClient.CurrentDivisionName & "  •  Pengajuan menunggu persetujuan admin."), 14, Theme.Grey, 24, 62))

        If mode = "out" OrElse mode = "loan" Then BuildForm()

        Dim top = If(mode = "out" OrElse mode = "loan", 286, 116)
        _info = MakeLabel("Memuat pengajuan...", 14, Theme.Grey, 24, top)
        Controls.Add(_info)
        Dim refresh As New Button() With {.Text = "Muat ulang", .Location = New Point(978, top - 5), .Size = New Size(124, 32)}
        AddHandler refresh.Click, Async Sub(s, e) Await ReloadAsync()
        Controls.Add(refresh)

        _grid = New DataGridView() With {.Location = New Point(24, top + 42), .Size = New Size(1078, If(top = 116, 434, 264)),
            .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, .RowHeadersVisible = False}
        For Each heading In {"Tanggal", "Jenis", "Aset", "Jumlah", "Pemohon", "Kode user", "Divisi", "Status", "Catatan admin"}
            _grid.Columns.Add(heading, heading)
        Next
        Controls.Add(_grid)

        Dim actionsTop = top + _grid.Height + 54
        Dim detail As New Button() With {.Text = "Lihat detail", .Location = New Point(If(mode = "admin", 790, 226), actionsTop), .Size = New Size(124, 36)}
        AddHandler detail.Click, Sub(s, e)
                                     If _grid.SelectedRows.Count = 0 Then Return
                                     Dim r = _rows(_grid.SelectedRows(0).Index)
                                     MessageBox.Show("Jenis: " & If(r.RequestType = "loan", "Peminjaman", "Barang keluar") & vbCrLf &
                                                     "Aset: " & r.AssetName & " (" & r.AssetCode & ")" & vbCrLf &
                                                     "Jumlah: " & r.Quantity & vbCrLf &
                                                     "Pemohon: " & r.RequesterName & vbCrLf &
                                                     "Kode user: " & r.RequesterCode & vbCrLf &
                                                     "Divisi: " & r.DivisionName & vbCrLf &
                                                     "Keperluan: " & r.Notes & vbCrLf &
                                                     "Batas kembali: " & If(r.DueDate = "", "-", r.DueDate) & vbCrLf &
                                                     "Status: " & StatusLabel(r.Status) & vbCrLf &
                                                     "Catatan admin: " & r.DecisionNote & vbCrLf &
                                                     "Diserahkan oleh: " & r.HandedBy & vbCrLf &
                                                     "Waktu penyerahan: " & r.HandedAt, "Detail pengajuan")
                                 End Sub
        Controls.Add(detail)
        If mode = "admin" Then
            AddAction("Setujui", 24, actionsTop, Async Function(r) As Task
                                                     Await AssetRequests.DecideAsync(r.Id, True, "")
                                                 End Function)
            AddAction("Tolak", 154, actionsTop, Async Function(r) As Task
                                                   Dim reason = Microsoft.VisualBasic.Interaction.InputBox("Alasan penolakan:", "Tolak pengajuan")
                                                   If String.IsNullOrWhiteSpace(reason) Then Return
                                                   Await AssetRequests.DecideAsync(r.Id, False, reason.Trim())
                                               End Function)
            AddAction("Serahkan", 284, actionsTop, Async Function(r) As Task
                                                      Await AssetRequests.HandoverAsync(r.Id)
                                                  End Function, 140)
            AddAction("Batalkan", 434, actionsTop, Async Function(r) As Task
                                                      Dim reason = Microsoft.VisualBasic.Interaction.InputBox("Alasan pembatalan:", "Batalkan persetujuan")
                                                      If String.IsNullOrWhiteSpace(reason) Then Return
                                                      Await AssetRequests.CancelAsync(r.Id, reason.Trim())
                                                  End Function, 120)
            AddAction("Verifikasi pengembalian", 564, actionsTop, Async Function(r) As Task
                                                                         Await AssetRequests.ConfirmReturnAsync(r.Id)
                                                                     End Function, 210)
        ElseIf mode <> "out" Then
            AddAction("Ajukan pengembalian", 24, actionsTop, Async Function(r) As Task
                                                                      Await AssetRequests.RequestReturnAsync(r.Id)
                                                                  End Function, 186)
        End If
        AddHandler Load, Async Sub(s, e)
                             If _mode = "out" OrElse _mode = "loan" Then Await LoadAssetsAsync()
                             Await ReloadAsync()
                         End Sub
    End Sub

    Private Sub BuildForm()
        Dim card As New RoundedPanel() With {.Location = New Point(24, 104), .Size = New Size(1078, 160), .Radius = 8}
        card.Controls.Add(MakeLabel("Pengajuan baru", 18, Theme.Charcoal, 16, 12, FontStyle.Bold))
        card.Controls.Add(MakeLabel("Aset / barang", 12, Theme.Charcoal, 16, 49))
        _assets = New ComboBox() With {.Location = New Point(16, 72), .Size = New Size(275, 30), .DropDownStyle = ComboBoxStyle.DropDownList}
        card.Controls.Add(_assets)
        card.Controls.Add(MakeLabel("Jumlah", 12, Theme.Charcoal, 309, 49))
        _quantity = New NumericUpDown() With {.Location = New Point(309, 72), .Size = New Size(90, 30), .Minimum = 1, .Maximum = 100000}
        If _mode = "loan" Then _quantity.Value = 1 : _quantity.Enabled = False
        card.Controls.Add(_quantity)
        If _mode = "loan" Then
            card.Controls.Add(MakeLabel("Batas kembali", 12, Theme.Charcoal, 417, 49))
            _dueDate = New DateTimePicker() With {.Location = New Point(417, 72), .Size = New Size(170, 30), .Format = DateTimePickerFormat.Short, .MinDate = Date.Today.AddDays(1)}
            card.Controls.Add(_dueDate)
        End If
        card.Controls.Add(MakeLabel("Keperluan", 12, Theme.Charcoal, 605, 49))
        _notes = New TextBox() With {.Location = New Point(605, 72), .Size = New Size(240, 30)}
        card.Controls.Add(_notes)
        Dim submit As New Button() With {.Text = "Kirim pengajuan", .Location = New Point(861, 69), .Size = New Size(198, 36)}
        AddHandler submit.Click, Async Sub(s, e) Await SubmitAsync()
        card.Controls.Add(submit)
        Controls.Add(card)
    End Sub

    Private Async Function LoadAssetsAsync() As Task
        Try
            If SupabaseClient.IsDemoSession Then
                _assetIds.Clear()
                _assets.Items.Clear()
                For Each asset In DemoData.Assets()
                    Dim available = asset.RequestKind = _mode AndAlso If(_mode = "loan", asset.Status = "tersedia", asset.Quantity > 0)
                    If Not available Then Continue For
                    Dim label = asset.Name & " (" & asset.Code & ")"
                    _assetIds(label) = asset.Id
                    _assets.Items.Add(label)
                Next
                If _assets.Items.Count > 0 Then _assets.SelectedIndex = 0
                Return
            End If
            Dim raw = Await SupabaseClient.GetJsonAsync($"/rest/v1/assets?select=id,asset_code,name,quantity,status,request_kind&request_kind=eq.{_mode}&order=name.asc")
            _assetIds.Clear()
            _assets.Items.Clear()
            Using doc = JsonDocument.Parse(raw)
                For Each item In doc.RootElement.EnumerateArray()
                    Dim available = SupabaseClient.GetInt(item, "quantity") > 0
                    If _mode = "loan" Then available = SupabaseClient.GetStr(item, "status") = "tersedia"
                    If Not available Then Continue For
                    Dim label = SupabaseClient.GetStr(item, "name") & " (" & SupabaseClient.GetStr(item, "asset_code") & ")"
                    _assetIds(label) = SupabaseClient.GetStr(item, "id")
                    _assets.Items.Add(label)
                Next
            End Using
            If _assets.Items.Count > 0 Then _assets.SelectedIndex = 0
        Catch ex As Exception
            _info.Text = "Gagal memuat aset: " & ex.Message
        End Try
    End Function

    Private Async Function SubmitAsync() As Task
        If _busy Then Return
        If _assets.SelectedItem Is Nothing Then MessageBox.Show("Pilih aset yang tersedia.") : Return
        If String.IsNullOrWhiteSpace(_notes.Text) Then MessageBox.Show("Isi keperluan pengajuan.") : Return
        _busy = True
        Try
            Await AssetRequests.SubmitAsync(If(_mode = "loan", "loan", "out"), _assetIds(_assets.SelectedItem.ToString()), CInt(_quantity.Value), _notes.Text.Trim(), If(_mode = "loan", _dueDate.Value.ToString("yyyy-MM-dd"), Nothing))
            _notes.Clear()
            MessageBox.Show("Pengajuan dikirim dan menunggu verifikasi admin.")
            Await ReloadAsync()
        Catch ex As Exception
            MessageBox.Show("Gagal mengirim pengajuan: " & ex.Message, "Supabase", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            _busy = False
        End Try
    End Function

    Private Async Function ReloadAsync() As Task
        Try
            Dim all = Await AssetRequests.ListAsync()
            _rows.Clear()
            _rows.AddRange(all.Where(Function(r) _mode <> "out" AndAlso _mode <> "loan" OrElse r.RequestType = _mode).
                               OrderBy(Function(r) If(_mode = "admin" AndAlso (r.Status = "pending" OrElse r.Status = "approved" OrElse r.Status = "return_pending"), 0, 1)).
                               ThenByDescending(Function(r) r.CreatedAt))
            _grid.Rows.Clear()
            For Each r In _rows
                _grid.Rows.Add(r.CreatedAt.ToString("dd/MM/yyyy HH:mm"), If(r.RequestType = "loan", "Peminjaman", "Barang keluar"),
                               r.AssetName & " (" & r.AssetCode & ")", r.Quantity, r.RequesterName, r.RequesterCode, r.DivisionName, StatusLabel(r.Status), r.DecisionNote)
            Next
            _info.Text = If(_mode = "admin", all.Where(Function(r) r.Status = "pending" OrElse r.Status = "approved" OrElse r.Status = "return_pending").Count() & " perlu tindakan", _rows.Count & " pengajuan")
        Catch ex As Exception
            _info.Text = "Gagal memuat pengajuan: " & ex.Message
        End Try
    End Function

    Private Shared Function StatusLabel(status As String) As String
        Select Case status
            Case "pending" : Return "Menunggu"
            Case "approved" : Return "Disetujui"
            Case "handed_out" : Return "Diserahkan"
            Case "rejected" : Return "Ditolak"
            Case "cancelled" : Return "Dibatalkan"
            Case "return_pending" : Return "Menunggu pengembalian"
            Case "returned" : Return "Dikembalikan"
            Case Else : Return status
        End Select
    End Function

    Private Sub AddAction(caption As String, x As Integer, y As Integer, action As Func(Of AssetRequestRow, Task), Optional width As Integer = 124)
        Dim button As New Button() With {.Text = caption, .Location = New Point(x, y), .Size = New Size(width, 36)}
        AddHandler button.Click, Async Sub(s, e)
                                     If _busy OrElse _grid.SelectedRows.Count = 0 Then Return
                                     Dim r = _rows(_grid.SelectedRows(0).Index)
                                     If caption = "Setujui" OrElse caption = "Tolak" Then
                                         If r.Status <> "pending" Then MessageBox.Show("Pilih pengajuan yang menunggu verifikasi.") : Return
                                     ElseIf caption = "Serahkan" OrElse caption = "Batalkan" Then
                                         If r.Status <> "approved" Then MessageBox.Show("Pilih pengajuan disetujui yang belum diserahkan.") : Return
                                     ElseIf caption = "Verifikasi pengembalian" Then
                                         If r.Status <> "return_pending" Then MessageBox.Show("Pilih pengembalian yang menunggu verifikasi.") : Return
                                     ElseIf r.RequestType <> "loan" OrElse r.Status <> "handed_out" Then
                                         MessageBox.Show("Pilih peminjaman aktif milik Anda.") : Return
                                     End If
                                     If caption = "Serahkan" Then
                                         Dim identity = "Serahkan hanya kepada:" & vbCrLf & r.RequesterName & " (" & r.RequesterCode & ")" & vbCrLf &
                                                        r.DivisionName & vbCrLf & vbCrLf & "Identitas sudah diperiksa dan barang benar-benar diserahkan?"
                                         If MessageBox.Show(identity, "Konfirmasi serah terima", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
                                     End If
                                     If caption = "Setujui" OrElse caption = "Verifikasi pengembalian" Then
                                         If MessageBox.Show("Pastikan aset, jumlah, dan divisi sudah diperiksa. Lanjutkan?", caption,
                                                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
                                     End If
                                     _busy = True
                                     Try
                                         Await action(r)
                                         Await ReloadAsync()
                                     Catch ex As Exception
                                         MessageBox.Show("Operasi gagal: " & ex.Message, "Supabase", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                     Finally
                                         _busy = False
                                     End Try
                                 End Sub
        Controls.Add(button)
    End Sub
End Class
