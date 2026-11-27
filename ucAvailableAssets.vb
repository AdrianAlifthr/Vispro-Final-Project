Imports System
Imports System.Text.Json

''' <summary>Daftar aset baca saja untuk user divisi.</summary>
Public Class ucAvailableAssets
    Inherits UserControl

    Private ReadOnly _grid As DataGridView
    Private ReadOnly _info As Label

    Public Sub New()
        BackColor = Theme.OffWhite
        Controls.Add(MakeLabel("Aset Tersedia", 26, Theme.Charcoal, 24, 22, FontStyle.Bold))
        Controls.Add(MakeLabel("Pilih aset di menu pengajuan untuk meminta barang atau peminjaman.", 14, Theme.Grey, 24, 62))
        _info = MakeLabel("Memuat aset...", 14, Theme.Grey, 24, 116)
        Controls.Add(_info)
        Dim refresh As New Button() With {.Text = "Muat ulang", .Location = New Point(978, 110), .Size = New Size(124, 32)}
        AddHandler refresh.Click, Async Sub(s, e) Await ReloadAsync()
        Controls.Add(refresh)
        _grid = New DataGridView() With {.Location = New Point(24, 164), .Size = New Size(1078, 490),
            .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, .RowHeadersVisible = False}
        For Each heading In {"Kode", "Nama aset", "Jenis pengajuan", "Stok", "Status"}
            _grid.Columns.Add(heading, heading)
        Next
        Controls.Add(_grid)
        AddHandler Load, Async Sub(s, e) Await ReloadAsync()
    End Sub

    Private Async Function ReloadAsync() As Threading.Tasks.Task
        Try
            If SupabaseClient.IsDemoSession Then
                _grid.Rows.Clear()
                For Each asset In DemoData.Assets()
                    If asset.Quantity > 0 OrElse asset.Status = "tersedia" Then
                        _grid.Rows.Add(asset.Code, asset.Name, If(asset.RequestKind = "loan", "Peminjaman", "Barang Keluar"), asset.Quantity, asset.Status)
                    End If
                Next
                _info.Text = _grid.Rows.Count & " aset demo tersedia"
                Return
            End If
            Dim raw = Await SupabaseClient.GetJsonAsync("/rest/v1/assets?select=asset_code,name,quantity,status,request_kind&order=name.asc")
            _grid.Rows.Clear()
            Using doc = JsonDocument.Parse(raw)
                For Each item In doc.RootElement.EnumerateArray()
                    Dim quantity = SupabaseClient.GetInt(item, "quantity")
                    Dim status = SupabaseClient.GetStr(item, "status")
                    If quantity > 0 OrElse status = "tersedia" Then
                        Dim kind = SupabaseClient.GetStr(item, "request_kind")
                        _grid.Rows.Add(SupabaseClient.GetStr(item, "asset_code"), SupabaseClient.GetStr(item, "name"), If(kind = "loan", "Peminjaman", "Barang Keluar"), quantity, status)
                    End If
                Next
            End Using
            _info.Text = _grid.Rows.Count & " aset tersedia"
        Catch ex As Exception
            _info.Text = "Gagal memuat aset: " & ex.Message
        End Try
    End Function
End Class
