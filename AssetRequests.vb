Imports System
Imports System.Text.Json
Imports System.Threading.Tasks

Public Class AssetRequestRow
    Public Property Id As String
    Public Property AssetId As String
    Public Property RequesterId As String
    Public Property RequestType As String
    Public Property AssetName As String
    Public Property AssetCode As String
    Public Property RequesterName As String
    Public Property RequesterCode As String
    Public Property DivisionName As String
    Public Property Quantity As Integer
    Public Property Status As String
    Public Property DecisionNote As String
    Public Property Notes As String
    Public Property CreatedAt As Date
    Public Property DueDate As String
    Public Property HandedBy As String
    Public Property HandedAt As String
End Class

''' <summary>Kontrak REST/RPC untuk pengajuan. Keputusan dan perubahan stok hanya terjadi di RPC database.</summary>
Public Module AssetRequests
    Public Async Function ListAsync() As Task(Of List(Of AssetRequestRow))
        If SupabaseClient.IsDemoSession Then Return DemoData.Requests()
        Dim raw = Await SupabaseClient.GetJsonAsync("/rest/v1/v_asset_requests?select=*&order=created_at.desc")
        Dim rows As New List(Of AssetRequestRow)()
        Using doc = JsonDocument.Parse(raw)
            For Each item In doc.RootElement.EnumerateArray()
                rows.Add(New AssetRequestRow With {
                    .Id = SupabaseClient.GetStr(item, "id"),
                    .RequestType = SupabaseClient.GetStr(item, "request_type"),
                    .AssetName = SupabaseClient.GetStr(item, "asset_name"),
                    .AssetCode = SupabaseClient.GetStr(item, "asset_code"),
                    .RequesterName = SupabaseClient.GetStr(item, "requester_name"),
                    .RequesterCode = SupabaseClient.GetStr(item, "requester_code"),
                    .DivisionName = SupabaseClient.GetStr(item, "division_name"),
                    .Quantity = SupabaseClient.GetInt(item, "quantity"),
                    .Status = SupabaseClient.GetStr(item, "status"),
                    .DecisionNote = SupabaseClient.GetStr(item, "decision_note"),
                    .Notes = SupabaseClient.GetStr(item, "notes"),
                    .CreatedAt = SupabaseClient.GetDate(item, "created_at"),
                    .DueDate = SupabaseClient.GetStr(item, "due_date"),
                    .HandedBy = SupabaseClient.GetStr(item, "handed_by_name"),
                    .HandedAt = SupabaseClient.GetStr(item, "handed_at")
                })
            Next
        End Using
        Return rows
    End Function

    Public Async Function SubmitAsync(kind As String, assetId As String, quantity As Integer, notes As String, dueDate As String) As Task
        If Not String.Equals(SupabaseClient.CurrentUserRole, "staff", StringComparison.OrdinalIgnoreCase) Then Throw New InvalidOperationException("Hanya user divisi yang dapat mengajukan.")
        If String.IsNullOrWhiteSpace(SupabaseClient.CurrentDivisionId) Then Throw New InvalidOperationException("Akun belum terhubung ke divisi.")
        If SupabaseClient.IsDemoSession Then
            DemoData.Submit(kind, assetId, quantity, notes, dueDate)
            Return
        End If
        Dim payload As New Dictionary(Of String, Object) From {
            {"request_type", kind}, {"asset_id", assetId}, {"quantity", quantity},
            {"notes", If(String.IsNullOrWhiteSpace(notes), Nothing, notes)},
            {"due_date", If(kind = "loan", dueDate, Nothing)}
        }
        Await SupabaseClient.PostJsonAsync("asset_requests", payload)
    End Function

    Public Async Function DecideAsync(id As String, approved As Boolean, note As String) As Task
        If Not String.Equals(SupabaseClient.CurrentUserRole, "admin", StringComparison.OrdinalIgnoreCase) Then Throw New InvalidOperationException("Hanya admin yang dapat memverifikasi.")
        If SupabaseClient.IsDemoSession Then
            DemoData.Decide(id, approved, note)
            Return
        End If
        Await SupabaseClient.RpcAsync(If(approved, "approve_asset_request", "reject_asset_request"), New With {.p_request_id = id, .p_note = note})
    End Function

    Public Async Function HandoverAsync(id As String) As Task
        If Not String.Equals(SupabaseClient.CurrentUserRole, "admin", StringComparison.OrdinalIgnoreCase) Then Throw New InvalidOperationException("Hanya admin yang dapat menyerahkan barang.")
        If SupabaseClient.IsDemoSession Then
            DemoData.Handover(id)
            Return
        End If
        Await SupabaseClient.RpcAsync("handover_asset_request", New With {.p_request_id = id})
    End Function

    Public Async Function CancelAsync(id As String, note As String) As Task
        If Not String.Equals(SupabaseClient.CurrentUserRole, "admin", StringComparison.OrdinalIgnoreCase) Then Throw New InvalidOperationException("Hanya admin yang dapat membatalkan persetujuan.")
        If SupabaseClient.IsDemoSession Then
            DemoData.Cancel(id, note)
            Return
        End If
        Await SupabaseClient.RpcAsync("cancel_asset_request", New With {.p_request_id = id, .p_note = note})
    End Function

    Public Async Function RequestReturnAsync(id As String) As Task
        If Not String.Equals(SupabaseClient.CurrentUserRole, "staff", StringComparison.OrdinalIgnoreCase) Then Throw New InvalidOperationException("Hanya pemohon yang dapat mengajukan pengembalian.")
        If SupabaseClient.IsDemoSession Then
            DemoData.RequestReturn(id)
            Return
        End If
        Await SupabaseClient.RpcAsync("request_loan_return", New With {.p_request_id = id})
    End Function

    Public Async Function ConfirmReturnAsync(id As String) As Task
        If Not String.Equals(SupabaseClient.CurrentUserRole, "admin", StringComparison.OrdinalIgnoreCase) Then Throw New InvalidOperationException("Hanya admin yang dapat memverifikasi pengembalian.")
        If SupabaseClient.IsDemoSession Then
            DemoData.ConfirmReturn(id)
            Return
        End If
        Await SupabaseClient.RpcAsync("confirm_loan_return", New With {.p_request_id = id})
    End Function
End Module
