Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports EINS.Models.Homeowners.Credit
Imports Microsoft.VisualBasic.Logging
Imports WSAddressScrubber.Models.PreciselyModels

Public Class AddressScrubber_DAL
    Inherits PropertyIT.Common.Data.SQLBase_DAL

    '112041
    Private Shared ReadOnly log As log4net.ILog = log4net.LogManager.GetLogger("WSAddressScrubber.PreciselyGeoCode")

    Public Sub New()
        MyBase.New(ConfigurationManager.ConnectionStrings("HomeownersRW").ConnectionString)
    End Sub

    Public ReadOnly Property DBConnection As IDbConnection
        '112041
        Get
            Dim HomeownersRW = ConfigurationManager.ConnectionStrings("HomeownersRW")?.ConnectionString
            If String.IsNullOrWhiteSpace(HomeownersRW) Then Throw New ArgumentNullException($"Required connection string {NameOf(HomeownersRW)} is missing.")
            Return New SqlClient.SqlConnection(HomeownersRW)
        End Get
    End Property

#Region "PreciselyGeocode* SQL Statements"
    Public Function Insert_PreciselyGeocodeData(ByVal pBKey As String, ByVal timeStamp As DateTime, ByVal geocodeRequest As String, ByVal geocodeResponse As String) As Guid
        '112041

        log.Info($"Insert_PreciselyGeocodeData: Begin Function for pBKey: {pBKey}. timeStamp: {timeStamp}")

        Dim SQLStmt As String = "pr_CRUD_PreciselyGeocodeData_I"

        Dim Params As New List(Of SqlClient.SqlParameter) From {
                            New SqlClient.SqlParameter("@i_PBKey", SqlDbType.VarChar, 50) With {.Value = pBKey},
                            New SqlClient.SqlParameter("@i_TimeStamp", SqlDbType.DateTime) With {.Value = timeStamp},
                            New SqlClient.SqlParameter("@i_GeocodeRequest", SqlDbType.NVarChar, -1) With {.Value = geocodeRequest}, '-1 signifies max
                            New SqlClient.SqlParameter("@i_GeocodeResponse", SqlDbType.NVarChar, -1) With {.Value = geocodeResponse} '-1 signifies max
        }

        Using dt As DataTable = OpenQuery(SQLStmt, Params.ToArray)
            If dt.Rows.Count = 0 Then
                log.Error($"Insert_PreciselyGeocodeData: Error: pr_CRUD_PreciselyGeocodeData_I did not return PreciselyCeocodeDataID. pBKey: {pBKey} timeStamp: {timeStamp}")
                Return New Guid()
            Else
                log.Info($"Insert_PreciselyGeocodeData: End Function for pBKey: {pBKey}. timeStamp: {timeStamp}")
                Return New Guid(dt.Rows(0)("PreciselyGeocodeDataID").ToString)
            End If
        End Using

    End Function
    'feature 167310 apoosala Precisely for 30days cache start
    Public Function IsAddressExistsInTheLast30DaysForPrecisely(ByVal Address As String, ByVal City As String, ByVal State As String, ByVal ZipCode As String) As Precisely30DaysCache
        Dim addressPrecisely30Days = Address + City + State + ZipCode
        log.Info($"IsAddressExistsInTheLast30DaysForPrecisely: Begin Function for address: {addressPrecisely30Days}")
        addressPrecisely30Days = addressPrecisely30Days.Replace(" ", "")

        Dim pre30 As New Precisely30DaysCache With {
            .County = "",
            .Countyfips = "",
            .PrecisionLevel = Nothing,
            .MatchConfidence = "",
            .PBKey = "",
            .Latitude = Nothing,
            .Longitude = Nothing,
            .PreciselyGeocodeDataID = Guid.Empty}
        Dim SQLStmt As String = "usp_IsSessionAddressExistsInTheLast30Days"
        Dim Params As New List(Of SqlClient.SqlParameter) From {
                            New SqlClient.SqlParameter("@sessionAddress", SqlDbType.VarChar, 500) With {.Value = addressPrecisely30Days}
                                  }

        Using dt As DataTable = OpenQuery(SQLStmt, Params.ToArray)
            If dt.Rows.Count = 0 Then
                log.Info($"IsAddressExistsInTheLast30DaysForPrecisely: Info: usp_IsSessionAddressExistsInTheLast30Days did not return address. address: {addressPrecisely30Days}")
                Return Nothing
            ElseIf dt.Rows.Count > 0 Then
                pre30.PreciselyGeocodeDataID = New Guid(dt.Rows(0)("PreciselyGeocodeDataID").ToString)
                pre30.PBKey = dt.Rows(0)("PBKey").ToString
                pre30.County = dt.Rows(0)("CustomFields_COUNTY").ToString
                pre30.Countyfips = dt.Rows(0)("CustomFields_COUNTY_FIPS").ToString
                pre30.Latitude = Double.Parse(dt.Rows(0)("Location_Feature_Geometry_Coordinates_Latitude").ToString)
                pre30.Longitude = Double.Parse(dt.Rows(0)("Location_Feature_Geometry_Coordinates_Longitude").ToString)
                pre30.MatchConfidence = dt.Rows(0)("CustomFields_CONFIDENCE").ToString()
                If IsNumeric(dt.Rows(0)("CustomFields_PRECISION_LEVEL")) Then
                    pre30.PrecisionLevel = Integer.Parse(dt.Rows(0)("CustomFields_PRECISION_LEVEL").ToString)
                End If
                log.Info($"IsAddressExistsInTheLast30DaysForPrecisely: Returned values for address: {addressPrecisely30Days}")
                log.Info($"IsAddressExistsInTheLast30DaysForPrecisely: End Function for address: {addressPrecisely30Days}")
                Return pre30
            End If
        End Using
        Return pre30
    End Function
    'feature 167310 apoosala Precisely for 30days cache end
#End Region

End Class