Imports System.Collections.Generic
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Threading.Tasks
Imports Newtonsoft.Json
Imports System.Runtime.Serialization
Imports System.Configuration
Imports System.Net
Imports StackExchange.Redis

'Work Item 112041

Namespace PreciselyToken

    ''<summary> OAuthtokenModel </summary>
    <DataContract>
    Public Class OAuthTokenModel
        <DataMember(Name:="access_token", IsRequired:=True)>
        Public access_token As String
        <DataMember(Name:="token_type", IsRequired:=True)>
        Public token_type As String
        <DataMember(Name:="expires_in", IsRequired:=True)>
        Public expires_in As String
    End Class

    '' <summary>OAuthLogin</summary>
    Public Class OAuthLogin
        Private Shared ReadOnly log As log4net.ILog = log4net.LogManager.GetLogger("WSAddressScrubber.PreciselyToken")
        Private ReadOnly _url As String = ConfigurationManager.AppSettings("PreciselyBasicAuthURI").ToString()
        Private ReadOnly userpwd As String = ConfigurationManager.AppSettings("PreciselyBasicAuthUsername") + ":" + ConfigurationManager.AppSettings("PreciselyBasicAuthP")
        Private Shared _oAuthTokenModel As OAuthTokenModel
        Private Shared ReadOnly Client As New HttpClient(New HttpClientHandler With {.AutomaticDecompression = DecompressionMethods.GZip})

        Public needToken As Integer
        Private ReadOnly _redis As ConnectionMultiplexer
        Private ReadOnly _db As IDatabase
        Private ReadOnly _cacheRetention As New TimeSpan()
        Public Sub New()
            log4net.LogicalThreadContext.Properties.Item("activityid") = Guid.NewGuid.ToString
            _redis = ConnectionMultiplexer.Connect(ConfigurationManager.AppSettings("RedisHostName"))
            _db = _redis.GetDatabase(CInt(ConfigurationManager.AppSettings("RedisDBID")))
            _cacheRetention = New TimeSpan(0, CInt(ConfigurationManager.AppSettings("PreciselyRedisCacheRetention")), 0)
        End Sub

        '' <summary>CreateRequest</summary>
        Private Function CreateRequest() As HttpRequestMessage
            Dim message = New HttpRequestMessage(HttpMethod.Post, _url) With {
            .Content = CreateContent()
            }
            message.Headers.Clear()
            AssignTokenHeaderValues(message.Headers)
            Return message
        End Function

        '' <summary>AssignHeaderValues</summary>
        Private Sub AssignTokenHeaderValues(ByVal headers As HttpRequestHeaders)
            headers.Accept.Add(New MediaTypeWithQualityHeaderValue("application/json"))
            Dim UTF8userpwd = Encoding.UTF8.GetBytes(userpwd)
            Dim userSecret = Convert.ToBase64String(UTF8userpwd)
            headers.Authorization = New AuthenticationHeaderValue("Token", userSecret)
            headers.Add("Accept", "*/*")
            headers.Add("Connection", "keep-alive")
            headers.Add("Accept-Encoding", "gzip, deflate, br")
        End Sub

        '' <summary>CreateContent</summary>
        Private Function CreateContent() As FormUrlEncodedContent
            Dim requestData = New List(Of KeyValuePair(Of String, String)) From {
                New KeyValuePair(Of String, String)("grant_type", "client_credentials"),
                New KeyValuePair(Of String, String)("scope", "default")
            }
            Return New FormUrlEncodedContent(requestData)

        End Function

        '' <summary> GetAccessToken returns unexpired token or gets and returns a new token if current token is expired. </summary>
        Public Async Function GetOAuthTokenAsync() As Task(Of String)

            Try
                Dim Precisely As String = ""

                log.Info($"Precisely GetOAuthTokenAsync begin for username {ConfigurationManager.AppSettings("PreciselyBasicAuthUsername")}")

                If _db.KeyExists(Precisely) Then
                    Try
                        _oAuthTokenModel = JsonConvert.DeserializeObject(Of OAuthTokenModel)(_db.StringGet(Precisely))
                        If _oAuthTokenModel Is Nothing Then
                            log.Error($"Precisely GetOAuthTokenAsync cached _oAuthTokenModel is Nothing for username {ConfigurationManager.AppSettings("PreciselyBasicAuthUsername")}")
                            _db.KeyDelete(Precisely) 'If for some reason the token in cache was corrupted, clear the current entry and let the logic fall to get a new token.
                        ElseIf _oAuthTokenModel.access_token Is Nothing Then
                            log.Error($"Precisely GetOAuthTokenAsync cached _oAuthTokenModel.access_token Is Nothing for username {ConfigurationManager.AppSettings("PreciselyBasicAuthUsername")}")
                            _db.KeyDelete(Precisely) 'If for some reason the token in cache was corrupted, clear the current entry and let the logic fall to get a new token.
                        Else
                            log.Info($"Precisely GetOAuthTokenAsync use unexpired token for username {ConfigurationManager.AppSettings("PreciselyBasicAuthUsername")}")
                            Return _oAuthTokenModel.access_token
                        End If
                    Catch ex As Exception
                        ' If for some reason caching breaks, just use the code below and clear the current entry
                        log.Error($"Precisely GetOAuthTokenAsync retrieve token from cache exception for username {ConfigurationManager.AppSettings("PreciselyBasicAuthUsername")}, {ex}")
                        _db.KeyDelete(Precisely)
                    End Try
                End If

                _oAuthTokenModel = New OAuthTokenModel()
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                Dim request = CreateRequest()
                Using result = Await Client.SendAsync(request)
                    If result.IsSuccessStatusCode Then
                        Dim content = Await result.Content.ReadAsStringAsync()
                        If String.IsNullOrWhiteSpace(content) Then
                            log.Error($"Precisely GetOAuthTokenAsync returned no result for username {ConfigurationManager.AppSettings("PreciselyBasicAuthUsername")}")
                            Return String.Empty
                        End If
                        _oAuthTokenModel = JsonConvert.DeserializeObject(Of OAuthTokenModel)(content)
                        _db.StringSet(Precisely, JsonConvert.SerializeObject(_oAuthTokenModel, Formatting.Indented), _cacheRetention)
                        log.Info($"Precisely GetOAuthTokenAsync Refresh expired token successful for username {ConfigurationManager.AppSettings("PreciselyBasicAuthUsername")}, token_type: {_oAuthTokenModel.token_type}, expires_in: {_oAuthTokenModel.expires_in}.")
                    Else
                        log.Error($"Precisely GetOAuthTokenAsync invalid StatusCode '{result.StatusCode}' for username {ConfigurationManager.AppSettings("PreciselyBasicAuthUsername")}.")
                        Return String.Empty
                    End If
                End Using
            Catch ex As Exception
                log.Error($"Precisely GetOAuthTokenAsync retrieve token exception, {ex}")
                Return String.Empty
            End Try

            log.Info($"Precisely GetOAuthTokenAsync end for username {ConfigurationManager.AppSettings("PreciselyBasicAuthUsername")}")

            Return _oAuthTokenModel.access_token
        End Function
    End Class

End Namespace
