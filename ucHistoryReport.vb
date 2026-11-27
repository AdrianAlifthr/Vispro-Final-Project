Imports System
Imports System.Text.Json
Imports System.Threading.Tasks

Public Class HistoryEntry
    Public Property Kind As String
    Public Property Reference As String
    Public Property DateValue As Date
    Public Property AssetName As String
    Public Property Person As String
    Public Property Division As String
    Public Property Quantity As Integer
    Public Property Status As String
    Public Property LoanId As String
    Public Property RequestId As String
End Class

''' <summary>Riwayat baca saja; penutupan hanya tersedia untuk pinjaman lama tanpa request_id.</summary>
Public Class ucHistoryReport
    Inherits UserControl

    Private ReadOnly _all As New List(Of HistoryEntry)()
    Private ReadOnly _shown As New List(Of HistoryEntry)()
    Private ReadOnly _filter As ComboBox
    Private ReadOnly _grid As DataGridView
    Private ReadOnly _info As Label

    Public Sub New()
        BackColor = Theme.OffWhite
        Controls.Add(MakeLabel("Riwayat Barang Keluar & Peminjaman", 26, Theme.Charcoal, 24, 22, FontStyle.Bold))
        Controls.Add(MakeLabel("Transaksi yang sudah diserahkan, termasuk data lama.", 14, Theme.Grey, 24, 62))
        _filter = New ComboBox() With {.Location = New Point(24, 116), .Size = New Size(230, 32), .DropDownStyle = ComboBoxStyle.DropDownList}
        _filter.Items.AddRange(New Object() {"Semua jenis", "Barang Keluar", "Peminjaman"})
        _filter.SelectedIndex = 0
        AddHandler _filter.SelectedIndexChanged, Sub(s, e) ApplyFilter()
        Controls.Add(_filter)
        Dim refresh As New Button() With {.Text = "Muat ulang", .Location = New Point(978, 112), .Size = New Size(124, 34)}
        AddHandler refresh.Click, Async Sub(s, e) Await ReloadAsync()
        Controls.Add(refresh)
        _info = MakeLabel("Memuat riwayat...", 14, Theme.Grey, 24, 166)
        Controls.Add(_info)
        _grid = New DataGridView() With {.Location = New Point(24, 204), .Size = New Size(1078, 400),
            .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .MultiSelect = False,
            .RowHeadersVisible = False, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill}
        For Each heading In {"Tanggal", "Jenis", "Referensi", "Aset", "Pemohon/Tujuan", "Divisi", "Jumlah", "Status"}
            _grid.Columns.Add(heading, heading)
        Next
        Controls.Add(_grid)
        Dim closeLegacy As New Button() With {.Text = "Tutup pinjaman lama", .Location = New Point(24, 625), .Size = New Size(190, 36)}
        AddHandler closeLegacy.Click, Async Sub(s, e) Await CloseLegacyAsync()
        Controls.Add(closeLegacy)
        Dim back As New Button() With {.Text = "Kembali ke laporan aset", .Location = New Point(812, 625), .Size = New Size(290, 36), .Visible = Not SupabaseClient.IsDemoSession}
        AddHandler back.Click, Sub(s, e) TryCast(FindForm(), FormMain)?.ShowPage(New ucLaporan(), 7, "Laporan")
        Controls.Add(back)
        AddHandler Load, Async Sub(s, e) Await ReloadAsync()
    End Sub

    Private Async Function ReloadAsync() As Task
        Try
            _all.Clear()
            If SupabaseClient.IsDemoSession Then
                For Each r In DemoData.Requests().Where(Function(x) x.Status = "handed_out" OrElse x.Status = "return_pending" OrElse x.Status = "returned")
                    Dim handedDate As Date = r.CreatedAt
                    If r.HandedAt <> "" Then Date.TryParse(r.HandedAt, handedDate)
                    _all.Add(New HistoryEntry With {.Kind = r.RequestType, .Reference = r.Id.Substring(0, 8),
                        .DateValue = handedDate, .AssetName = r.AssetName, .Person = r.RequesterName,
                        .Division = r.DivisionName, .Quantity = r.Quantity, .Status = r.Status, .RequestId = r.Id})
                Next
                _all.Add(New HistoryEntry With {.Kind = "loan", .Reference = "LEG-001", .DateValue = Date.Today.AddDays(-4),
                    .AssetName = "Proyektor Ruang Rapat", .Person = "User Divisi Administrasi", .Division = "Divisi Administrasi",
                    .Quantity = 1, .Status = If(DemoData.LegacyLoanActive(), "dipinjam", "dikembalikan"), .LoanId = "demo-legacy-loan"})
            Else
                Dim outJson = Await SupabaseClient.GetJsonAsync("/rest/v1/v_stock_transactions?type=eq.keluar&order=trx_date.desc,created_at.desc")
                Using doc = JsonDocument.Parse(outJson)
                    For Each item In doc.RootElement.EnumerateArray()
                        _all.Add(New HistoryEntry With {.Kind = "out", .Reference = SupabaseClient.GetStr(item, "ref_no"),
                            .DateValue = SupabaseClient.GetDate(item, "trx_date"), .AssetName = SupabaseClient.GetStr(item, "asset_name"),
                            .Person = If(SupabaseClient.GetStr(item, "requester_name") <> "", SupabaseClient.GetStr(item, "requester_name"), SupabaseClient.GetStr(item, "officer_name")),
                            .Division = If(SupabaseClient.GetStr(item, "division_name") <> "", SupabaseClient.GetStr(item, "division_name"), SupabaseClient.GetStr(item, "destination")),
                            .Quantity = SupabaseClient.GetInt(item, "quantity"), .Status = "handed_out"})
                    Next
                End Using
                Dim loanJson = Await SupabaseClient.GetJsonAsync("/rest/v1/v_loans?order=loan_date.desc,created_at.desc")
                Using doc = JsonDocument.Parse(loanJson)
                    For Each item In doc.RootElement.EnumerateArray()
                        _all.Add(New HistoryEntry With {.Kind = "loan", .Reference = SupabaseClient.GetStr(item, "loan_no"),
                            .DateValue = SupabaseClient.GetDate(item, "loan_date"), .AssetName = SupabaseClient.GetStr(item, "asset_name"),
                            .Person = SupabaseClient.GetStr(item, "borrower_name"), .Division = SupabaseClient.GetStr(item, "division_name"), .Quantity = 1,
                            .Status = SupabaseClient.GetStr(item, "effective_status"), .LoanId = SupabaseClient.GetStr(item, "id"),
                            .RequestId = SupabaseClient.GetStr(item, "request_id")})
                    Next
                End Using
            End If
            ApplyFilter()
        Catch ex As Exception
            _info.Text = "Gagal memuat riwayat: " & ex.Message
        End Try
    End Function

    Private Sub ApplyFilter()
        If _grid Is Nothing OrElse _filter.SelectedIndex < 0 Then Return
        Dim kind = If(_filter.SelectedIndex = 1, "out", If(_filter.SelectedIndex = 2, "loan", ""))
        _shown.Clear()
        _shown.AddRange(_all.Where(Function(r) kind = "" OrElse r.Kind = kind).OrderByDescending(Function(r) r.DateValue))
        _grid.Rows.Clear()
        For Each r In _shown
            _grid.Rows.Add(r.DateValue.ToString("dd/MM/yyyy"), If(r.Kind = "loan", "Peminjaman", "Barang Keluar"),
                           r.Reference, r.AssetName, r.Person, r.Division, r.Quantity, StatusLabel(r.Kind, r.Status))
        Next
        _info.Text = _shown.Count & " transaksi"
    End Sub

    Private Shared Function StatusLabel(kind As String, value As String) As String
        Select Case value
            Case "handed_out" : Return If(kind = "loan", "Dipinjam", "Diserahkan")
            Case "dipinjam" : Return "Dipinjam"
            Case "return_pending" : Return "Menunggu pengembalian"
            Case "returned", "dikembalikan" : Return "Dikembalikan"
            Case "terlambat" : Return "Terlambat"
            Case Else : Return value
        End Select
    End Function

    Private Async Function CloseLegacyAsync() As Task
        If _grid.SelectedRows.Count = 0 Then Return
        Dim row = _shown(_grid.SelectedRows(0).Index)
        If row.Kind <> "loan" OrElse row.RequestId <> "" OrElse row.LoanId = "" OrElse
           (row.Status <> "dipinjam" AndAlso row.Status <> "terlambat") Then
            MessageBox.Show("Aksi ini hanya untuk pinjaman lama yang masih aktif dan tidak memiliki pengajuan.")
            Return
        End If
        If MessageBox.Show("Aset sudah diterima kembali?", "Tutup pinjaman lama", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
        Try
            If SupabaseClient.IsDemoSession Then
                DemoData.CloseLegacyLoan()
                Await ReloadAsync()
                Return
            End If
            Dim updated = Await SupabaseClient.PatchJsonAsync($"/rest/v1/loans?id=eq.{row.LoanId}&request_id=is.null&status=neq.dikembalikan",
                                                              New With {.status = "dikembalikan", .return_date = Date.Today.ToString("yyyy-MM-dd")})
            If updated = "[]" Then Throw New InvalidOperationException("Pinjaman sudah berubah atau tidak diizinkan.")
            Await ReloadAsync()
        Catch ex As Exception
            MessageBox.Show("Gagal menutup pinjaman lama: " & ex.Message, "Supabase", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function
End Class
