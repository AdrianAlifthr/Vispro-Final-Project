Imports System
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports System.Text.Json
Imports System.Threading.Tasks

Public Module SupabaseClient
    Public Const SupabaseUrl As String = "https://zmjtairylfldbmyqtoor.supabase.co"
    Public Const SupabaseAnonKey As String = "sb_publishable_FzP9voNUeL0eMSTqLphtjQ_R1EnpB_L"

    Public AccessToken As String = ""
    Public CurrentUserId As String = ""
    Public CurrentUserName As String = ""
    Public CurrentUserCode As String = ""
    Public CurrentUserRole As String = ""
    Public CurrentDivisionId As String = ""
    Public CurrentDivisionName As String = ""
    Public IsDemoSession As Boolean = False

    Private ReadOnly _http As New HttpClient()

    Private Function CreateRequest(method As HttpMethod, path As String, Optional jsonBody As String = Nothing, Optional useAnonOnly As Boolean = False) As HttpRequestMessage
        Dim req As New HttpRequestMessage(method, SupabaseUrl & path)
        req.Headers.Add("apikey", SupabaseAnonKey)
        Dim bearer = If(useAnonOnly OrElse String.IsNullOrEmpty(AccessToken), SupabaseAnonKey, AccessToken)
        req.Headers.Authorization = New AuthenticationHeaderValue("Bearer", bearer)
        req.Headers.Accept.Add(New MediaTypeWithQualityHeaderValue("application/json"))
        If jsonBody IsNot Nothing Then
            req.Content = New StringContent(jsonBody, Encoding.UTF8, "application/json")
        End If
        Return req
    End Function

    Public Async Function SignInAsync(email As String, password As String) As Task(Of (Ok As Boolean, Message As String))
        ClearSession()
        Dim body = JsonSerializer.Serialize(New With {.email = email, .password = password})
        Using req = CreateRequest(HttpMethod.Post, "/auth/v1/token?grant_type=password", body, True)
            Dim res = Await _http.SendAsync(req)
            Dim raw = Await res.Content.ReadAsStringAsync()
            If Not res.IsSuccessStatusCode Then
                Return (False, "Email atau password salah.")
            End If

            Using doc = JsonDocument.Parse(raw)
                AccessToken = doc.RootElement.GetProperty("access_token").GetString()
                CurrentUserId = doc.RootElement.GetProperty("user").GetProperty("id").GetString()
            End Using
        End Using

        ' Ambil profil & role dari tabel public.users
        Dim profileJson = Await GetJsonAsync($"/rest/v1/users?id=eq.{CurrentUserId}&select=full_name,user_code,role,is_active")
        Using doc = JsonDocument.Parse(profileJson)
            If doc.RootElement.GetArrayLength() = 0 Then
                ClearSession()
                Return (False, "Profil pengguna tidak ditemukan di tabel users.")
            End If
            Dim u = doc.RootElement(0)
            If Not GetBool(u, "is_active") Then
                ClearSession()
                Return (False, "Akun ini sedang dinonaktifkan.")
            End If
            CurrentUserName = GetStr(u, "full_name")
            CurrentUserCode = GetStr(u, "user_code")
            CurrentUserRole = GetStr(u, "role")
        End Using

        If String.Equals(CurrentUserRole, "staff", StringComparison.OrdinalIgnoreCase) Then
            Try
                Dim divisionJson = Await GetJsonAsync($"/rest/v1/users?id=eq.{CurrentUserId}&select=division_id,divisions(name)")
                Using doc = JsonDocument.Parse(divisionJson)
                    Dim profile = doc.RootElement(0)
                    CurrentDivisionId = GetStr(profile, "division_id")
                    Dim division As JsonElement
                    If profile.TryGetProperty("divisions", division) AndAlso division.ValueKind = JsonValueKind.Object Then
                        CurrentDivisionName = GetStr(division, "name")
                    End If
                End Using
            Catch
                ClearSession()
                Return (False, "Profil divisi belum tersedia. Hubungi pengelola database.")
            End Try
            If String.IsNullOrWhiteSpace(CurrentDivisionId) Then
                ClearSession()
                Return (False, "Akun ini belum terhubung ke divisi.")
            End If
        End If

        Return (True, "")
    End Function

    Public Sub ClearSession()
        IsDemoSession = False
        AccessToken = ""
        CurrentUserId = ""
        CurrentUserName = ""
        CurrentUserCode = ""
        CurrentUserRole = ""
        CurrentDivisionId = ""
        CurrentDivisionName = ""
    End Sub

    Public Sub StartDemoSession(role As String, Optional demoUserId As String = "demo-user-it")
        Dim demoUser As DemoData.DemoUser = Nothing
        If role <> "admin" Then
            demoUser = DemoData.Users().SingleOrDefault(Function(u) u.Id = demoUserId AndAlso u.Active)
            If demoUser Is Nothing Then Throw New InvalidOperationException("Akun demo tidak ditemukan atau sudah dinonaktifkan.")
        End If
        ClearSession()
        IsDemoSession = True
        CurrentUserRole = role
        If role = "admin" Then
            CurrentUserId = "demo-admin"
            CurrentUserName = "Admin Demo"
            CurrentUserCode = "ADM-DEMO"
        Else
            CurrentUserId = demoUser.Id
            CurrentUserCode = demoUser.Code
            CurrentUserName = demoUser.Name
            CurrentDivisionId = demoUser.DivisionId
            CurrentDivisionName = demoUser.DivisionName
        End If
    End Sub

    Public Async Function RpcAsync(name As String, payload As Object) As Task(Of String)
        Dim body = JsonSerializer.Serialize(payload)
        Using req = CreateRequest(HttpMethod.Post, "/rest/v1/rpc/" & name, body)
            Dim res = Await _http.SendAsync(req)
            Dim raw = Await res.Content.ReadAsStringAsync()
            If Not res.IsSuccessStatusCode Then Throw New Exception(raw)
            Return raw
        End Using
    End Function

    ''' <summary>Mendaftarkan akun baru lewat Supabase Auth tanpa mengganti sesi admin saat ini.</summary>
    Public Async Function SignUpUserAsync(email As String, password As String, fullName As String, username As String) As Task(Of String)
        Dim payload = New With {
            .email = email,
            .password = password,
            .data = New With {.full_name = fullName, .username = username}
        }
        Dim body = JsonSerializer.Serialize(payload)
        Using req = CreateRequest(HttpMethod.Post, "/auth/v1/signup", body, True)
            Dim res = Await _http.SendAsync(req)
            Dim raw = Await res.Content.ReadAsStringAsync()
            If Not res.IsSuccessStatusCode Then Throw New Exception(raw)
            Using doc = JsonDocument.Parse(raw)
                Dim u As JsonElement
                If doc.RootElement.TryGetProperty("user", u) Then
                    Return GetStr(u, "id")
                End If
                Return GetStr(doc.RootElement, "id")
            End Using
        End Using
    End Function

    Public Async Function GetJsonAsync(path As String) As Task(Of String)
        Using req = CreateRequest(HttpMethod.Get, path)
            Dim res = Await _http.SendAsync(req)
            Dim raw = Await res.Content.ReadAsStringAsync()
            If Not res.IsSuccessStatusCode Then Throw New Exception(raw)
            Return raw
        End Using
    End Function

    Public Async Function PostJsonAsync(table As String, payload As Object) As Task(Of String)
        Dim body = JsonSerializer.Serialize(payload)
        Using req = CreateRequest(HttpMethod.Post, "/rest/v1/" & table, body)
            req.Headers.Add("Prefer", "return=representation")
            Dim res = Await _http.SendAsync(req)
            Dim raw = Await res.Content.ReadAsStringAsync()
            If Not res.IsSuccessStatusCode Then Throw New Exception(raw)
            Return raw
        End Using
    End Function

    Public Async Function PatchJsonAsync(path As String, payload As Object) As Task(Of String)
        Dim body = JsonSerializer.Serialize(payload)
        Using req = CreateRequest(HttpMethod.Patch, path, body)
            req.Headers.Add("Prefer", "return=representation")
            Dim res = Await _http.SendAsync(req)
            Dim raw = Await res.Content.ReadAsStringAsync()
            If Not res.IsSuccessStatusCode Then Throw New Exception(raw)
            Return raw
        End Using
    End Function

    Public Async Function DeleteAsync(path As String) As Task
        Using req = CreateRequest(HttpMethod.Delete, path)
            Dim res = Await _http.SendAsync(req)
            Dim raw = Await res.Content.ReadAsStringAsync()
            If Not res.IsSuccessStatusCode Then Throw New Exception(raw)
        End Using
    End Function

    ' ---------- JSON helper aman ----------
    Public Function GetStr(el As JsonElement, prop As String) As String
        Dim v As JsonElement
        If el.TryGetProperty(prop, v) Then
            If v.ValueKind = JsonValueKind.String Then Return v.GetString()
            If v.ValueKind = JsonValueKind.Number OrElse v.ValueKind = JsonValueKind.True OrElse v.ValueKind = JsonValueKind.False Then Return v.ToString()
        End If
        Return ""
    End Function

    Public Function GetInt(el As JsonElement, prop As String) As Integer
        Dim v As JsonElement
        If el.TryGetProperty(prop, v) AndAlso v.ValueKind = JsonValueKind.Number Then
            Dim n As Integer
            If v.TryGetInt32(n) Then Return n
        End If
        Return 0
    End Function

    Public Function GetDec(el As JsonElement, prop As String) As Decimal
        Dim v As JsonElement
        If el.TryGetProperty(prop, v) AndAlso v.ValueKind = JsonValueKind.Number Then
            Dim d As Decimal
            If v.TryGetDecimal(d) Then Return d
        End If
        Return 0D
    End Function

    Public Function GetBool(el As JsonElement, prop As String) As Boolean
        Dim v As JsonElement
        If el.TryGetProperty(prop, v) Then
            If v.ValueKind = JsonValueKind.True Then Return True
            If v.ValueKind = JsonValueKind.False Then Return False
        End If
        Return False
    End Function

    Public Function GetDate(el As JsonElement, prop As String, Optional fallback As Date = Nothing) As Date
        Dim s = GetStr(el, prop)
        Dim dt As DateTime
        If DateTime.TryParse(s, dt) Then Return dt.ToLocalTime()
        Return If(fallback = Date.MinValue, Date.Today, fallback)
    End Function
End Module
