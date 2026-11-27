Imports System

''' <summary>Data sementara untuk mencoba alur tanpa menyentuh Supabase. Hilang saat aplikasi ditutup.</summary>
Public Module DemoData
    Public Class DemoAsset
        Public Property Id As String
        Public Property Code As String
        Public Property Name As String
        Public Property Quantity As Integer
        Public Property Status As String
        Public Property RequestKind As String
    End Class

    Public Class DemoUser
        Public Property Id As String
        Public Property Code As String
        Public Property Name As String
        Public Property Email As String
        Public Property Password As String
        Public Property DivisionId As String
        Public Property DivisionName As String
        Public Property Active As Boolean = True
    End Class

    Private ReadOnly _users As New List(Of DemoUser) From {
        New DemoUser With {.Id = "demo-user-it", .Code = "USR-IT-01", .Name = "Andi Pratama", .Email = "andi@demo.local", .Password = "demo123", .DivisionId = "demo-divisi-it", .DivisionName = "Divisi IT"},
        New DemoUser With {.Id = "demo-user-keuangan", .Code = "USR-KEU-01", .Name = "Siti Rahma", .Email = "siti@demo.local", .Password = "demo123", .DivisionId = "demo-divisi-keuangan", .DivisionName = "Divisi Keuangan"},
        New DemoUser With {.Id = "demo-user-administrasi", .Code = "USR-ADM-01", .Name = "Budi Santoso", .Email = "budi@demo.local", .Password = "demo123", .DivisionId = "demo-divisi-administrasi", .DivisionName = "Divisi Administrasi"}
    }
    Public Function Divisions() As Dictionary(Of String, String)
        Return New Dictionary(Of String, String) From {
            {"Divisi IT", "demo-divisi-it"},
            {"Divisi Keuangan", "demo-divisi-keuangan"},
            {"Divisi Administrasi", "demo-divisi-administrasi"}
        }
    End Function

    Private ReadOnly _assets As New List(Of DemoAsset) From {
        New DemoAsset With {.Id = "demo-mouse", .Code = "BRG-001", .Name = "Mouse Logitech M170", .Quantity = 24, .Status = "tersedia", .RequestKind = "out"},
        New DemoAsset With {.Id = "demo-paper", .Code = "BRG-002", .Name = "Kertas A4 80 gsm", .Quantity = 40, .Status = "tersedia", .RequestKind = "out"},
        New DemoAsset With {.Id = "demo-laptop", .Code = "AST-001", .Name = "Laptop Kantor", .Quantity = 1, .Status = "tersedia", .RequestKind = "loan"},
        New DemoAsset With {.Id = "demo-projector", .Code = "AST-002", .Name = "Proyektor Ruang Rapat", .Quantity = 1, .Status = "dipinjam", .RequestKind = "loan"}
    }
    Private _legacyLoanActive As Boolean = True
    Private ReadOnly _requests As New List(Of AssetRequestRow)()
    Private ReadOnly _gate As New Object()

    Public Function Assets() As List(Of DemoAsset)
        SyncLock _gate
            Return _assets.Select(Function(a) New DemoAsset With {.Id = a.Id, .Code = a.Code, .Name = a.Name, .Quantity = a.Quantity, .Status = a.Status, .RequestKind = a.RequestKind}).ToList()
        End SyncLock
    End Function

    Public Function Users() As List(Of DemoUser)
        SyncLock _gate
            Return _users.Select(Function(u) New DemoUser With {.Id = u.Id, .Code = u.Code, .Name = u.Name, .Email = u.Email, .Password = u.Password, .DivisionId = u.DivisionId, .DivisionName = u.DivisionName, .Active = u.Active}).ToList()
        End SyncLock
    End Function

    Public Function Authenticate(email As String, password As String) As DemoUser
        SyncLock _gate
            Dim user = _users.SingleOrDefault(Function(u) u.Active AndAlso
                String.Equals(u.Email, email.Trim(), StringComparison.OrdinalIgnoreCase) AndAlso u.Password = password)
            If user Is Nothing Then Return Nothing
            Return New DemoUser With {.Id = user.Id, .Code = user.Code, .Name = user.Name, .Email = user.Email,
                .Password = user.Password, .DivisionId = user.DivisionId, .DivisionName = user.DivisionName, .Active = user.Active}
        End SyncLock
    End Function

    Public Sub SaveUser(user As DemoUser)
        SyncLock _gate
            Dim existing = _users.SingleOrDefault(Function(u) u.Id = user.Id)
            If existing Is Nothing Then
                user.Id = Guid.NewGuid().ToString()
                user.Code = "USR-" & (_users.Count + 1).ToString("000")
                If String.IsNullOrEmpty(user.Password) Then user.Password = "demo123"
                _users.Add(user)
            Else
                existing.Name = user.Name
                existing.Email = user.Email
                existing.DivisionId = user.DivisionId
                existing.DivisionName = user.DivisionName
                existing.Active = user.Active
            End If
        End SyncLock
    End Sub

    Public Function Requests() As List(Of AssetRequestRow)
        SyncLock _gate
            Return _requests.Where(Function(r) SupabaseClient.CurrentUserRole = "admin" OrElse r.RequesterId = SupabaseClient.CurrentUserId).
                Select(Function(r) Copy(r)).OrderByDescending(Function(r) r.CreatedAt).ToList()
        End SyncLock
    End Function

    Public Function LegacyLoanActive() As Boolean
        SyncLock _gate
            Return _legacyLoanActive
        End SyncLock
    End Function

    Public Sub CloseLegacyLoan()
        SyncLock _gate
            If Not _legacyLoanActive Then Throw New InvalidOperationException("Pinjaman lama sudah ditutup.")
            _legacyLoanActive = False
            _assets.Single(Function(a) a.Id = "demo-projector").Status = "tersedia"
        End SyncLock
    End Sub

    Public Sub Submit(kind As String, assetId As String, quantity As Integer, notes As String, dueDate As String)
        SyncLock _gate
            If Not _users.Any(Function(u) u.Id = SupabaseClient.CurrentUserId AndAlso u.Active) Then Throw New InvalidOperationException("Akun pemohon tidak aktif.")
            Dim asset = _assets.SingleOrDefault(Function(a) a.Id = assetId)
            If asset Is Nothing Then Throw New InvalidOperationException("Aset demo tidak ditemukan.")
            If asset.RequestKind <> kind Then Throw New InvalidOperationException("Jenis pengajuan tidak sesuai dengan aset.")
            If quantity < 1 OrElse (kind = "loan" AndAlso quantity <> 1) Then Throw New InvalidOperationException("Jumlah tidak valid.")
            If kind = "loan" AndAlso asset.Status <> "tersedia" Then Throw New InvalidOperationException("Aset sedang tidak tersedia.")
            _requests.Add(New AssetRequestRow With {
                .Id = Guid.NewGuid().ToString(), .RequestType = kind, .AssetId = asset.Id,
                .AssetName = asset.Name, .AssetCode = asset.Code, .Quantity = quantity,
                .RequesterId = SupabaseClient.CurrentUserId, .RequesterName = SupabaseClient.CurrentUserName,
                .RequesterCode = SupabaseClient.CurrentUserCode,
                .DivisionName = SupabaseClient.CurrentDivisionName, .Status = "pending", .Notes = notes,
                .DueDate = dueDate, .CreatedAt = Date.Now
            })
        End SyncLock
    End Sub

    Public Sub Decide(id As String, approved As Boolean, note As String)
        SyncLock _gate
            Dim row = GetRequest(id)
            If row.Status <> "pending" Then Throw New InvalidOperationException("Pengajuan sudah diproses.")
            If approved Then
                row.Status = "approved"
            Else
                If String.IsNullOrWhiteSpace(note) Then Throw New InvalidOperationException("Alasan penolakan wajib diisi.")
                row.Status = "rejected"
            End If
            row.DecisionNote = note
        End SyncLock
    End Sub

    Public Sub Handover(id As String)
        SyncLock _gate
            Dim row = GetRequest(id)
            If row.Status <> "approved" Then Throw New InvalidOperationException("Pengajuan belum disetujui atau sudah diserahkan.")
            Dim user = _users.SingleOrDefault(Function(u) u.Id = row.RequesterId)
            If user Is Nothing OrElse Not user.Active Then Throw New InvalidOperationException("Akun pemohon tidak aktif.")
            Dim asset = _assets.Single(Function(a) a.Id = row.AssetId)
            If row.RequestType = "out" Then
                If asset.Quantity < row.Quantity Then Throw New InvalidOperationException("Stok tidak mencukupi saat penyerahan.")
                asset.Quantity -= row.Quantity
            Else
                If asset.Status <> "tersedia" Then Throw New InvalidOperationException("Aset tidak tersedia saat penyerahan.")
                asset.Status = "dipinjam"
            End If
            row.Status = "handed_out"
            row.HandedBy = SupabaseClient.CurrentUserName
            row.HandedAt = Date.Now.ToString("yyyy-MM-ddTHH:mm:ss")
        End SyncLock
    End Sub

    Public Sub Cancel(id As String, note As String)
        SyncLock _gate
            Dim row = GetRequest(id)
            If row.Status <> "approved" Then Throw New InvalidOperationException("Hanya persetujuan yang belum diserahkan dapat dibatalkan.")
            If String.IsNullOrWhiteSpace(note) Then Throw New InvalidOperationException("Alasan pembatalan wajib diisi.")
            row.Status = "cancelled"
            row.DecisionNote = note
        End SyncLock
    End Sub

    Public Sub RequestReturn(id As String)
        SyncLock _gate
            If Not _users.Any(Function(u) u.Id = SupabaseClient.CurrentUserId AndAlso u.Active) Then Throw New InvalidOperationException("Akun pemohon tidak aktif.")
            Dim row = GetRequest(id)
            If row.RequesterId <> SupabaseClient.CurrentUserId OrElse row.RequestType <> "loan" OrElse row.Status <> "handed_out" Then
                Throw New InvalidOperationException("Peminjaman ini tidak dapat diajukan untuk dikembalikan.")
            End If
            row.Status = "return_pending"
        End SyncLock
    End Sub

    Public Sub ConfirmReturn(id As String)
        SyncLock _gate
            Dim row = GetRequest(id)
            If row.RequestType <> "loan" OrElse row.Status <> "return_pending" Then Throw New InvalidOperationException("Pengembalian belum diajukan.")
            _assets.Single(Function(a) a.Id = row.AssetId).Status = "tersedia"
            row.Status = "returned"
        End SyncLock
    End Sub

    Private Function GetRequest(id As String) As AssetRequestRow
        Dim row = _requests.SingleOrDefault(Function(r) r.Id = id)
        If row Is Nothing Then Throw New InvalidOperationException("Pengajuan tidak ditemukan.")
        Return row
    End Function

    Private Function Copy(r As AssetRequestRow) As AssetRequestRow
        Return New AssetRequestRow With {
            .Id = r.Id, .RequestType = r.RequestType, .AssetId = r.AssetId, .AssetName = r.AssetName,
            .AssetCode = r.AssetCode, .RequesterId = r.RequesterId, .RequesterName = r.RequesterName, .RequesterCode = r.RequesterCode,
            .DivisionName = r.DivisionName, .Quantity = r.Quantity, .Status = r.Status,
            .DecisionNote = r.DecisionNote, .Notes = r.Notes, .CreatedAt = r.CreatedAt, .DueDate = r.DueDate,
            .HandedBy = r.HandedBy, .HandedAt = r.HandedAt
        }
    End Function
End Module
