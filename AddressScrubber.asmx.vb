Imports System.Web.Services
Imports System.ComponentModel
Imports MelissaData
Imports System.Collections
Imports System.Linq
Imports WSAddressScrubber.AddressExtensions
Imports System.Diagnostics
Imports StackExchange.Redis
Imports System.Configuration
Imports Newtonsoft.Json
Imports System.Threading.Tasks
Imports EINS.Models.Constants
Imports EINS.Models.Extensions
Imports WSAddressScrubber.Models.PreciselyModels
Imports System

<System.Web.Services.WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)>
<System.Web.Services.WebService(Namespace:="http://services.e-ins.net/WSAddressScrubber/")>
<ToolboxItem(False)>
Public Class AddressScrubber
    Inherits System.Web.Services.WebService

    Private Shared log As log4net.ILog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType)
    Private DataFiles_Path As String
    'MAP 11/13/2012 new DLLs
    Private addPtr As mdAddr
    Private strPtr As mdStreet
    Private geoPtr As mdGeo
    Private zipPtr As mdZip

    Private _EnableDPV As Boolean
    Private _ErrorMessages As New Collections.Generic.List(Of String)
    Private _WarningMessages As New Collections.Generic.List(Of String)
    Private _GeoErrorMessages As New Collections.Generic.List(Of String)
    Private _MDLicenseString As String
    Private _Stopwatch As New Stopwatch()
    Private _redis As ConnectionMultiplexer
    Private _db As IDatabase
    Private _cacheRetention As New TimeSpan()
    Private SuccessfulGeocodeReturn As Boolean = False '112041
    Private GeocodeResponse As New PreciselyGeoCodeOneAddressOutput '112041
    Private precisely30DaysCache As New Precisely30DaysCache '167310 checks for 30days cache Precisely

    Private _enableSystemFeature As EINS.Models.Service.EnableSystemFeature '112041
    Private ReadOnly Property EnableSystemFeature As EINS.Models.Service.EnableSystemFeature '112041
        Get
            If _enableSystemFeature Is Nothing Then
                _enableSystemFeature = New EINS.Models.Service.EnableSystemFeature()
            End If

            Return _enableSystemFeature
        End Get
    End Property

    Private ReadOnly Property MDLicenseString As String
        Get
            If String.IsNullOrEmpty(Me._MDLicenseString) Then
                Me._MDLicenseString = Environment.GetEnvironmentVariable("MD_LICENSE")
            End If

            Return Me._MDLicenseString
        End Get
    End Property
    Public Sub New()
        log4net.LogicalThreadContext.Properties.Item("activityid") = Guid.NewGuid.ToString
        DataFiles_Path = My.Settings.DataFilesPath
        _redis = ConnectionMultiplexer.Connect(ConfigurationManager.AppSettings("RedisHostName"))
        _db = _redis.GetDatabase(CInt(ConfigurationManager.AppSettings("RedisDBID")))
        _cacheRetention = TimeSpan.FromMinutes(CInt(ConfigurationManager.AppSettings("RedisCacheRetention")))
    End Sub

    Protected Overrides Sub Finalize()
        If addPtr IsNot Nothing Then addPtr.Dispose()
        If strPtr IsNot Nothing Then strPtr.Dispose()
        If geoPtr IsNot Nothing Then geoPtr.Dispose()
        If zipPtr IsNot Nothing Then zipPtr.Dispose()

        addPtr = Nothing
        strPtr = Nothing
        geoPtr = Nothing
        zipPtr = Nothing

        MyBase.Finalize()
    End Sub

    <WebMethod(Description:="Method used to Enable or Disable DPV Validation")>
    Public Sub ValidateMailing(ByVal value As Boolean)
        _EnableDPV = value
    End Sub

    Private Sub InitializeVariables()
        _ErrorMessages = New Collections.Generic.List(Of String)
        _WarningMessages = New Collections.Generic.List(Of String)
        _GeoErrorMessages = New Collections.Generic.List(Of String)
    End Sub

    Private Function InitializeScrubber(ByVal VerifyMailing As Boolean) As Boolean

        addPtr = New mdAddr
        addPtr.SetLicenseString(MDLicenseString)

        Diagnostics.Debug.WriteLine($"Address dll BuildNumber: {addPtr.GetBuildNumber()}")

        With addPtr
            .SetPathToUSFiles(DataFiles_Path.ToString)
            If VerifyMailing Then .SetPathToDPVDataFiles(DataFiles_Path)

            If .InitializeDataFiles <> mdAddr.ProgramStatus.ErrorNone Then
                Dim InitError As String = .GetInitializeErrorString
                _ErrorMessages.Add("Unable to initialize the address scrubber - " & InitError)
                log.Error($"Scrubber [InitializeScrubber] {InitError}")
                Return False
            Else
                Return True
            End If

        End With
    End Function

    Private Function InitializeStreet() As Boolean
        strPtr = New mdStreet
        strPtr.SetLicenseString(MDLicenseString)

        Diagnostics.Debug.WriteLine($"Street dll BuildNumber: {strPtr.GetBuildNumber()}")

        With strPtr
            If .Initialize(DataFiles_Path, DataFiles_Path, "") <> mdStreet.ProgramStatus.ErrorNone Then
                Dim InitError As String = .GetInitializeErrorString
                _ErrorMessages.Add("Unable to initialize the address scrubber - " & InitError)
                log.Error($"Scrubber [InitializeScrubber] {InitError}")
                Return False
            Else
                Return True
            End If
        End With
    End Function

    Private Function InitializeZip() As Boolean
        zipPtr = New mdZip
        zipPtr.SetLicenseString(MDLicenseString)

        Diagnostics.Debug.WriteLine($"Zip dll BuildNumber:  {zipPtr.GetBuildNumber()}")

        With zipPtr
            If .Initialize(DataFiles_Path, DataFiles_Path, "") <> mdZip.ProgramStatus.ErrorNone Then
                Dim InitError As String = .GetInitializeErrorString
                _GeoErrorMessages.Add("Unable to initialize geo data - " & InitError)
                log.Error($"Scrubber [InitializeZip] {InitError}")
                InitializeZip = False
            Else
                Diagnostics.Debug.WriteLine($"Geo initialized.")
                InitializeZip = True
            End If

        End With
    End Function

    Private Function InitializeGeo() As Boolean
        geoPtr = New mdGeo
        geoPtr.SetLicenseString(MDLicenseString)

        Diagnostics.Debug.WriteLine($"geoPtr dll BuildNumber: {geoPtr.GetBuildNumber()}")

        With geoPtr
            If .Initialize(DataFiles_Path, DataFiles_Path) <> mdGeo.ProgramStatus.ErrorNone Then
                Dim InitError As String = .GetInitializeErrorString
                _GeoErrorMessages.Add("Unable to load geo data - " & InitError)
                log.Error($"Scrubber [InitializeGeo] {InitError}")
                InitializeGeo = False
            Else
                InitializeGeo = True
            End If

        End With
    End Function

    <WebMethod(Description:="test parse - this is for testing specific address problems")>
    Public Sub TestParse()
        Dim AddressItems As New Generic.List(Of (Address As String, City As String, State As String, Zipcode As String, County As String))

        'AddressItems.Add(("1431 ziptesttrailanusha", "Fauxtown", "NC", "28217", "Fairfax"))
        'AddressItems.Add(("1431ziptesttrailanushajanp", "Fauxtown", "NC", "28217", "Fairfax"))
        'AddressItems.Add(("5924 quercus cove ct apt 110", "charlotte", "NC", "28217", "Fairfax"))
        AddressItems.Add(("2557 Berkley Hills", "Tuscaloosa", "AL", "25404", "Fairfax"))
        AddressItems.Add(("3100 S Manchester st APT", "ziptown", "VA", "22044", "Fairfax"))
        AddressItems.Add(("3100 S Manchester st APT", "Falls Church", "ZA", "99999", "Fairfax"))
        AddressItems.Add(("3100 S Manchester st APT 903", "Falls Church", "VA", "22044", "Fairfax"))
        AddressItems.Add(("25 WALLIS RD", "PORTSMOUTH", "NH", "03801", "United States"))
        AddressItems.Add(("20 Deep Clay Cir", "Angier", "NC", "27501", "Johnston"))
        AddressItems.Add(("18925 Willowmore Cedar Dr, ", "Lutz", "FL", "33558", "Hillsborough"))
        AddressItems.Add(("303 Prestwood Ln", "Wendell", "NC", "27591", "Johnston"))
        AddressItems.Add(("2627 Juliet Ct", "Franklin", "OH", "45005", "Warren"))
        AddressItems.Add(("1304 Hudgins Farm Cir", "Fredericksburg", "VA", "22408", "Spotsylvania"))
        AddressItems.Add(("2223 Merrill Hills Cir", "Katy", "TX", "77450", "Fort Bend"))
        AddressItems.Add(("3916 Mendenhall Dr", "Zebulon", "NC", "27597", "Wake"))
        AddressItems.Add(("336 Silver Ln", "Azle", "TX", "76020", "Parker"))

        For Each AddressItem In AddressItems
            Dim results = StandardizeAddress(AddressItem.Address,
                                             AddressItem.City,
                                             AddressItem.State,
                                             AddressItem.Zipcode,
                                             False,
                                             Nothing,
                                             Nothing,
                                             Nothing)

            If results.Count = 1 Then
                Diagnostics.Debug.WriteLine($"Scrubber county: {results.First.County} Expected county: {AddressItem.County}")
            Else
                Diagnostics.Debug.WriteLine($"Problem with {AddressItem.Address} results: {results.Count}")
            End If

        Next
    End Sub

    <WebMethod(Description:="Get Latitude and Logitude by zipcode")>
    Public Function GetGeoCoordinate(ByVal ZipCode As String) As Coordinate

        '112041 Precisely apoosala-as per Chuck Seibold ,this method is no longer used. The logic to get latitude, longitude is inside of the StandardizeAddress web Method.

        _Stopwatch.Restart()
        Dim coordinate As New Coordinate
        If _db.KeyExists(ZipCode) Then
            Try
                coordinate = JsonConvert.DeserializeObject(Of Coordinate)(_db.StringGet(ZipCode))
                _Stopwatch.Stop()
                log.Info($"Web Call (Cached) complete in: {_Stopwatch.Elapsed.Minutes}m {_Stopwatch.Elapsed.Seconds}s {_Stopwatch.Elapsed.Milliseconds}ms")
                Return coordinate
            Catch
                'if the caching failed for any reason just invalidate the key and continue
                _db.KeyDelete(ZipCode)
            End Try
        End If

        If InitializeGeo() Then
            Try
                With coordinate
                    .Longitude = 0
                    .Latitude = 0

                    If geoPtr.GeoCode(ZipCode.Trim.Substring(0, 5)) = 1 AndAlso (geoPtr.GetErrorCode Is Nothing OrElse geoPtr.GetErrorCode.Trim = "") Then
                        .Latitude = Double.Parse(geoPtr.GetLatitude)
                        .Longitude = Double.Parse(geoPtr.GetLongitude)
                    End If
                    If ZipCode.Contains("-") AndAlso ZipCode.Length = 10 Then
                        If geoPtr.GeoCode(ZipCode.Substring(0, 5), ZipCode.Substring(6, 4)) = 1 AndAlso (geoPtr.GetErrorCode Is Nothing OrElse geoPtr.GetErrorCode.Trim = "") Then
                            .Latitude = Double.Parse(geoPtr.GetLatitude)
                            .Longitude = Double.Parse(geoPtr.GetLongitude)
                        End If
                    End If
                End With
            Catch
                'return nothing
            Finally
                If geoPtr IsNot Nothing Then
                    geoPtr.Dispose()
                    geoPtr = Nothing
                End If
                _db.StringSet(ZipCode, JsonConvert.SerializeObject(coordinate), _cacheRetention)
                _Stopwatch.Stop()
                log.Info($"Web Call complete in: {_Stopwatch.Elapsed.Minutes}m {_Stopwatch.Elapsed.Seconds}s {_Stopwatch.Elapsed.Milliseconds}ms")
            End Try
        End If
        Return coordinate

    End Function

    <WebMethod(Description:="Method used to standardize an address - With new Result codes", MessageName:="StandardizeAddress")>
    Public Function StandardizeAddress(ByVal Address As String, ByVal City As String,
                                        ByVal State As String, ByVal ZipCode As String,
                                        ByVal UseDefaultForHighrise As Boolean,
                                      ByRef ErrorMessages As Collections.Generic.List(Of String),
                                      ByRef WarningMessages As Collections.Generic.List(Of String),
                                      ByRef GeoErrorMessages As Collections.Generic.List(Of String)) As System.Collections.Generic.List(Of AddressDetail)
        Return StandardizeAddressWithPreciselyToggle(Address, City, State, ZipCode, UseDefaultForHighrise, ErrorMessages, WarningMessages, GeoErrorMessages, UsePrecisely:=False)
    End Function

    <WebMethod(Description:="Method used to standardize an address - With new Result codes", MessageName:="StandardizeAddressWithPrecisely")>
    Public Function StandardizeAddressWithPrecisely(ByVal Address As String, ByVal City As String,
                                        ByVal State As String, ByVal ZipCode As String,
                                        ByVal UseDefaultForHighrise As Boolean,
                                      ByRef ErrorMessages As Collections.Generic.List(Of String),
                                      ByRef WarningMessages As Collections.Generic.List(Of String),
                                      ByRef GeoErrorMessages As Collections.Generic.List(Of String)) As System.Collections.Generic.List(Of AddressDetail)
        Return StandardizeAddressWithPreciselyToggle(Address, City, State, ZipCode, UseDefaultForHighrise, ErrorMessages, WarningMessages, GeoErrorMessages, UsePrecisely:=True)
    End Function

    Private Function StandardizeAddressWithPreciselyToggle(ByVal Address As String, ByVal City As String,
                                        ByVal State As String, ByVal ZipCode As String,
                                        ByVal UseDefaultForHighrise As Boolean,
                                      ByRef ErrorMessages As Collections.Generic.List(Of String),
                                      ByRef WarningMessages As Collections.Generic.List(Of String),
                                      ByRef GeoErrorMessages As Collections.Generic.List(Of String),
                                       UsePrecisely As Boolean) As System.Collections.Generic.List(Of AddressDetail)

        _Stopwatch.Restart()
        Dim _AddressList As New System.Collections.Generic.List(Of AddressDetail)
        Dim isCached = False
        Dim sessionAddress = GenerateAddressSessionVariable(Address, City, State, ZipCode)
        Dim ad As AddressDetail = New AddressDetail  '112041
        ad.PrecisionLevel = Nothing
        ad.MatchConfidence = ""
        ad.PBKey = ""
        ad.PreciselyGeocodeDataID = Guid.Empty

        InitializeVariables()
        Try
            If String.IsNullOrWhiteSpace(sessionAddress) Then
                log.Error($"StandardizeAddress input address is empty, returned an error to caller")
                _ErrorMessages.Add("Input address is empty")
                ErrorMessages = _ErrorMessages
                Return New Generic.List(Of AddressDetail)
            End If

            If _db.KeyExists(sessionAddress) Then
                Try
                    SetValidationMessagesFromCache(sessionAddress)
                    ErrorMessages = _ErrorMessages '112041 populate output ByRef variable
                    WarningMessages = _WarningMessages '112041 populate output ByRef variable
                    GeoErrorMessages = _GeoErrorMessages '112041 populate output ByRef variable
                    _AddressList = JsonConvert.DeserializeObject(Of Generic.List(Of AddressDetail))(_db.StringGet(sessionAddress))
                    If _AddressList Is Nothing OrElse _AddressList.Count = 0 Then
                        '--------------------------------
                        '112041 Don't delete this address from cache because doing so causes the address to execute the Address Validation and Precisely
                        'logic again. The reason _AddressList is empty is because the Melissa data address validation determined that the address is
                        'invalid and the AddressDetail model did not populate. An empty AddressDetail model was saved in cache for this scenario. Just
                        'return ErrorMessages, WarningMessages, GeoErrrorMessages and an empty AddressDetail List. This prevents invalid addresses from
                        'calling Melissa data and Precisely every time Standardize Address is called, even when the address key was cached.
                        '--------------------------------
                        log.Info($"StandardizeAddress use unexpired cache, cached _AddressList IS Empty for {Address} {City} {State} {ZipCode}")
                        isCached = True
                        Return New Generic.List(Of AddressDetail)
                    Else
                        Dim addComp As New AddressDetailComparer
                        isCached = True
                        log.Info($"StandardizeAddress use unexpired cache, cached _AddressList is NOT Empty for {Address} {City} {State} {ZipCode}")
                        RemoveBadMelissaDataAddress(_AddressList, Address)
                        Return _AddressList.Distinct(addComp).ToList
                    End If
                Catch ex As Exception
                    ' If for some reason caching breaks, just use the code below and clear the current entry
                    log.Error($"StandardizeAddress retrieve address from cache exception for {Address} {City} {State} {ZipCode}, {ex}")
                    _db.KeyDelete(sessionAddress)
                End Try
            End If

            InitializeScrubber(_EnableDPV)
            Try
                addPtr.ClearProperties()

                With addPtr
                    .SetAddress(Address)
                    .SetCity(City)
                    .SetState(State)
                    .SetZip(ZipCode)
                End With

                addPtr.VerifyAddress()

                Dim AddressResults As String() = addPtr.GetResults().Split(","c)
                Dim ConsiderAlternatives As String = String.Empty
                SetMessagesFromCodes(AddressResults, MessageConstants.StatusCodeList, ConsiderAlternatives)

                ' It's now been standardized.  Next step, find out if we need to create a list of addresses(missing secondary, etc), or just return this one(perfect match).
                Dim DPVFootnotes As String = addPtr.GetDPVFootnotes.Trim

                For ndx As Integer = 0 To DPVFootnotes.Length - 1 Step 2
                    Dim DPVStr As String = DPVFootnotes.Substring(ndx, 2)
                    If MessageConstants.DPVStatusCodeList.ContainsKey(DPVStr) Then
                        With MessageConstants.DPVStatusCodeList(DPVStr)
                            Select Case .MessageType
                                Case MessageConstants.TMessageType.ConsiderSucess ' Skip!
                                Case MessageConstants.TMessageType.ReturnSelectionPrimary : _WarningMessages.Add(.FriendlyMessage) : ConsiderAlternatives = "PRIMARY"
                                Case MessageConstants.TMessageType.ReturnSelectionSecondary : _WarningMessages.Add(.FriendlyMessage) : ConsiderAlternatives = "SECONDARY"
                                Case MessageConstants.TMessageType.REturnError : _ErrorMessages.Add(.FriendlyMessage)
                            End Select
                        End With
                    End If
                Next

                ' Did we classify any of these messages as warnings or errors? If not, let it be Verified!
                Dim AddressVerified As Boolean = Not (_WarningMessages.Count > 0 OrElse _ErrorMessages.Count > 0)

                'feature 167310 apoosala precisely varaible for 30days table check preciselygeocodedata (30days database cache) start
                If UsePrecisely AndAlso EnableSystemFeature.IsFeatureEnabled(FeatureToggleNames.Policy.Reporting.WO112041_PreciselyWebService, Now) Then
                    Dim addressscrubberdal = New AddressScrubber_DAL
                    precisely30DaysCache = addressscrubberdal.IsAddressExistsInTheLast30DaysForPrecisely(Address, City, State, ZipCode)
                    If (precisely30DaysCache Is Nothing) Then
                        SuccessfulGeocodeReturn = GetPreciselyGeocodeData(Address, City, State, ZipCode)
                    Else
                        SuccessfulGeocodeReturn = True
                    End If
                    'feature 167310 apoosala precisely varaible for 30days cache end
                End If

                'If Not (_WarningMessages.Count > 0 OrElse _ErrorMessages.Count > 0) Then    '' if error msgs or warnmsgs
                If AddressVerified Then
                    With ad
                        .AssociatedName = addPtr.GetCompany
                        .City = addPtr.GetCity.Trim.ToUpper
                        .CityAbbreviation = addPtr.GetCityAbbreviation.ToUpper
                        .State = addPtr.GetState.Trim.ToUpper
                        .ZipCode = addPtr.GetZip.Trim.ToUpper
                        .Zip4 = addPtr.GetPlus4.Trim.ToUpper
                        .County = addPtr.GetCountyName.Trim.ToUpper
                        .HouseNumber = addPtr.GetParsedAddressRange.Trim.ToUpper
                        .Predirection = addPtr.GetParsedPreDirection.Trim.ToUpper
                        .StreetName = addPtr.GetParsedStreetName.Trim.ToUpper
                        .Suffix = addPtr.GetParsedSuffix.Trim.ToUpper
                        .Postdirection = addPtr.GetParsedPostDirection.Trim.ToUpper
                        .SuiteName = addPtr.GetParsedSuiteName.Trim.ToUpper
                        .SuiteRange = addPtr.GetParsedSuiteRange.Trim.ToUpper
                        .CountyFips = addPtr.GetCountyFips
                        .HighRiseDefault = (addPtr.GetPS3553_E_HighRiseDefault = 1)

                        If addPtr.GetAddressTypeCode = "P" Then
                            .Fulladdress1 = addPtr.GetAddress.ToUpper
                        Else
                            .Fulladdress1 = ad.BuildFullAddress()
                        End If
                        .Fulladdress2 = (addPtr.GetParsedSuiteName.Trim.ToUpper & " " & addPtr.GetParsedSuiteRange.Trim.ToUpper).Trim

                        .Latitude = 0
                        .Longitude = 0

                        'Bypass retrieving the geolocation from Melissa data if the Precisely Geocode web service is being called.
                        Try
                            If InitializeZip() Then
                                Try
                                    If zipPtr.FindZip(addPtr.GetZip, True) Then  '' zip code found
                                        .Latitude = Double.Parse(zipPtr.GetLatitude)
                                        .Longitude = Double.Parse(zipPtr.GetLongitude)
                                        If InitializeGeo() Then
                                            Try
                                                ' Get a more specific lat long position if possible
                                                If geoPtr.GeoCode(addPtr.GetZip, addPtr.GetPlus4) = 1 AndAlso (geoPtr.GetErrorCode Is Nothing OrElse geoPtr.GetErrorCode.Trim = "") Then
                                                    geoPtr.SetInputParameter("Addresskey", addPtr.GetAddressKey)
                                                    geoPtr.FindGeo()

                                                    Diagnostics.Debug.WriteLine($"Updating county from { .County} to {geoPtr.GetCountyName()}.")
                                                    Diagnostics.Debug.WriteLine($"GetAddressTypeCode: {addPtr.GetAddressTypeCode()}  GetResults: '{geoPtr.GetResults()}' GetErrorCode: '{geoPtr.GetErrorCode()}' GetStatusCode: '{geoPtr.GetStatusCode()}'")
                                                    .Latitude = Double.Parse(geoPtr.GetLatitude)
                                                    .Longitude = Double.Parse(geoPtr.GetLongitude)
                                                    .County = geoPtr.GetCountyName()
                                                    .CountyFips = geoPtr.GetCountyFips()
                                                End If
                                            Finally
                                                If geoPtr IsNot Nothing Then
                                                    geoPtr.Dispose()
                                                    geoPtr = Nothing
                                                End If
                                            End Try
                                        End If
                                    Else
                                        Dim sb As New StringBuilder

                                        sb.AppendLine("ZipCode: " & zipPtr.GetZip)
                                        sb.AppendLine("Build: " & zipPtr.GetBuildNumber())
                                        sb.AppendLine("Expire (License): " & zipPtr.GetLicenseExpirationDate.ToString)
                                        sb.AppendLine("Expire (Data): " & zipPtr.GetDatabaseDate.ToString)
                                        log.Error($"Scrubber [zipPtr] {sb.ToString}")
                                        _GeoErrorMessages.Add("Unable to locate Zip Code")
                                    End If
                                Finally
                                    If zipPtr IsNot Nothing Then
                                        zipPtr.Dispose()
                                        zipPtr = Nothing
                                    End If
                                End Try
                            End If
                        Catch ex As Exception
                            Dim sb As New StringBuilder
                            sb.AppendLine("ZipCode:  " & zipPtr.GetZip)
                            sb.AppendLine("Build: " & zipPtr.GetBuildNumber())
                            sb.AppendLine("Expire (License): " & zipPtr.GetLicenseExpirationDate.ToString)
                            sb.AppendLine("Expire (Data): " & zipPtr.GetDatabaseDate.ToString)
                            sb.AppendLine("Exception: " & ex.Message)
                            log.Error($"Scrubber [zipPtr] {sb.ToString}")
                            _GeoErrorMessages.Add("error in geozip")
                        End Try
                    End With

                    'feature 167310 apoosala start
                    If UsePrecisely AndAlso EnableSystemFeature.IsFeatureEnabled(FeatureToggleNames.Policy.Reporting.WO112041_PreciselyWebService, Now) Then
                        If (precisely30DaysCache IsNot Nothing) Then
                            ad.PreciselyGeocodeDataID = precisely30DaysCache.PreciselyGeocodeDataID
                            ad.PBKey = precisely30DaysCache.PBKey
                            ad.PrecisionLevel = precisely30DaysCache.PrecisionLevel
                            ad.MatchConfidence = precisely30DaysCache.MatchConfidence
                            ad.Latitude = precisely30DaysCache.Latitude
                            ad.Longitude = precisely30DaysCache.Longitude
                            ad.CountyFips = precisely30DaysCache.Countyfips
                            log.Info($"Precisely fields populated from Precisely 30Days cached Address for {Address} {City} {State} {ZipCode}")
                            'feature 167310 end
                        ElseIf UsePrecisely AndAlso EnableSystemFeature.IsFeatureEnabled(FeatureToggleNames.Policy.Reporting.WO112041_PreciselyWebService, Now) Then
                            PopulateAddressDetailWithPreciselyGeocodeData(Address, City, State, ZipCode, ad)
                            If Not SuccessfulGeocodeReturn OrElse ad.Latitude = 0 OrElse ad.Longitude = 0 Then '4 is Client error 5 is Server error.
                                _GeoErrorMessages.Add("The connection with Precisely failed.")
                            End If
                        End If
                    End If

                    _AddressList.Add(ad.ShallowCopy())

                ElseIf _WarningMessages.Count > 0 Then
                    '112041 If the User-entered address is not found, this logic returns a best guess of a list of possible addresses. Once the
                    'user selects an address from the suggested list, the address runs through AddressSrubber again.
                    If InitializeStreet() Then
                        Try
                            If Not strPtr.FindStreet(addPtr.GetParsedStreetName, addPtr.GetZip, False) Then
                                _WarningMessages.Clear()
                                _ErrorMessages.Add("Unable to find street and no suggestions were found.")
                            Else

                                Do
                                    Dim PrimaryRangeLow As String = addPtr.GetParsedAddressRange
                                    Dim PrimaryRangeHigh As String = addPtr.GetParsedAddressRange

                                    If (strPtr.IsAddressInRange2(addPtr.GetParsedAddressRange, strPtr.GetPrimaryRangeLow, strPtr.GetPrimaryRangeHigh, strPtr.GetPrimaryRangeOddEven) OrElse
                                                                            ConsiderAlternatives.Equals("PRIMARY", StringComparison.OrdinalIgnoreCase)) Then
                                        With ad
                                            .AssociatedName = strPtr.GetCompany
                                            .City = addPtr.GetCity.Trim.ToUpper
                                            .State = addPtr.GetState.Trim.ToUpper
                                            .ZipCode = strPtr.GetZip.Trim.ToUpper
                                            .Zip4 = ""
                                            .County = addPtr.GetCountyName.Trim.ToUpper

                                            .HouseNumber = addPtr.GetParsedAddressRange().Trim.ToUpper
                                            .Predirection = strPtr.GetPreDirection.Trim.ToUpper
                                            .StreetName = strPtr.GetStreetName.Trim.ToUpper
                                            .Suffix = strPtr.GetSuffix.Trim.ToUpper
                                            .Postdirection = strPtr.GetPostDirection.Trim.ToUpper
                                            .CountyFips = strPtr.GetCountyFips
                                            .HighRiseDefault = (addPtr.GetPS3553_E_HighRiseDefault = 1)
                                            .Fulladdress1 = ad.BuildFullAddress()
                                            .Fulladdress2 = String.Empty

                                            Dim LowValue As String = strPtr.GetSuiteRangeLow.Trim
                                            Dim highValue As String = strPtr.GetSuiteRangeHigh.Trim
                                            Dim SuiteName As String = strPtr.GetSuiteName.Trim

                                            If addPtr.GetAddressTypeCode = "H" AndAlso Not String.IsNullOrEmpty(SuiteName.Trim) Then
                                                If String.IsNullOrEmpty(LowValue) OrElse String.IsNullOrEmpty(highValue) Then
                                                    .Fulladdress2 = SuiteName
                                                    _AddressList.Add(ad.ShallowCopy())
                                                Else
                                                    For Each suiteNumber As String In GenerateSuiteNumbers(LowValue, highValue, strPtr.GetSuiteRangeOddEven).ToArray
                                                        .Fulladdress2 = SuiteName & " " & suiteNumber
                                                        _AddressList.Add(ad.ShallowCopy())
                                                    Next
                                                End If
                                            Else
                                                _AddressList.Add(ad.ShallowCopy())
                                            End If
                                        End With
                                    End If
                                Loop Until Not strPtr.FindStreetNext()
                            End If

                        Finally
                            If strPtr IsNot Nothing Then
                                strPtr.Dispose()
                                strPtr = Nothing
                            End If
                        End Try
                    End If
                End If

                RemoveBadMelissaDataAddress(_AddressList, Address)
                Dim singleWarning = _AddressList.Where(Function(x) x.HighRiseDefault)

                If UseDefaultForHighrise AndAlso _WarningMessages.Count > 0 AndAlso singleWarning.Count = 1 Then
                    ' this is mainly for commerical.
                    _WarningMessages.Clear()
                    Return singleWarning.ToList()
                Else
                    ' Even if it's 0, it's still better then "nothing"
                    Dim addComp As New AddressDetailComparer
                    Return _AddressList.Distinct(addComp).ToList
                End If

            Finally
                If addPtr IsNot Nothing Then
                    addPtr.Dispose()
                    addPtr = Nothing
                End If
                If UsePrecisely AndAlso EnableSystemFeature.IsFeatureEnabled(FeatureToggleNames.Policy.Reporting.WO112041_PreciselyWebService, Now) Then
                    If SuccessfulGeocodeReturn Then 'not httpstatuscode 4 or 5.
                        '167353 Dont save address to cache if the Precisely call returned a 4xxs or 5xxs httpstatuscode; this prevents one from getting a successful hit until cache is expired.
                        _db.StringSet(sessionAddress, JsonConvert.SerializeObject(_AddressList, Formatting.Indented), _cacheRetention)
                    End If
                Else
                    _db.StringSet(sessionAddress, JsonConvert.SerializeObject(_AddressList, Formatting.Indented), _cacheRetention)
                End If
            End Try

        Catch ex As Exception
            If ad IsNot Nothing Then
                log.Error($"StandardizeAddress Web Call exception for {Address} {City} {State} {ZipCode}, ad: {{JsonConvert.SerializeObject(ad)}}, {ex}")
            Else
                log.Error($"StandardizeAddress Web Call exception for Address {Address} {City} {State} {ZipCode}, {ex}")
            End If
            Throw ex
        Finally
            ErrorMessages = _ErrorMessages
            WarningMessages = _WarningMessages
            GeoErrorMessages = _GeoErrorMessages

            _Stopwatch.Stop()
            If isCached Then
                log.Info($"StandardizeAddress web Call (Cached) for {Address} {City} {State} {ZipCode} complete in: {_Stopwatch.Elapsed.Minutes}m {_Stopwatch.Elapsed.Seconds}s {_Stopwatch.Elapsed.Milliseconds}ms")
            Else
                AddValidationMessagesToCache(sessionAddress)
                If ad IsNot Nothing Then
                    log.Info($"StandardizeAddress web call for {Address} {City} {State} {ZipCode} complete in: {_Stopwatch.Elapsed.Minutes}m {_Stopwatch.Elapsed.Seconds}s {_Stopwatch.Elapsed.Milliseconds}ms, ad: {JsonConvert.SerializeObject(ad)}")
                Else
                    log.Info($"StandardizeAddress web call for {Address} {City} {State} {ZipCode} complete in: {_Stopwatch.Elapsed.Minutes}m {_Stopwatch.Elapsed.Seconds}s {_Stopwatch.Elapsed.Milliseconds}ms")
                End If
            End If
        End Try
    End Function

    Private Sub RemoveBadMelissaDataAddress(ByRef _AddressList As Generic.List(Of AddressDetail), Address As String)
        If EnableSystemFeature.IsFeatureEnabled(FeatureToggleNames.Policy.General.HandleBadAddressFromMelissaData) AndAlso _AddressList.Count() > 0 Then
            Dim badAddressList = _AddressList.Where(Function(x) String.Equals(x.Fulladdress2.Trim(), "AT WATERFORD AT WATERFORD", StringComparison.OrdinalIgnoreCase) OrElse
                    (x.Fulladdress1.ToUpper().Contains("AT WATERFORD") AndAlso String.Equals(x.Fulladdress2.Trim(), "AT WATERFORD", StringComparison.OrdinalIgnoreCase))).ToList()
            log.Info($"Removing {badAddressList.Count()} bad address2 values from overall addressList {_AddressList.Count()} for address {Address}")
            If "53 ROYAL CT AT WATERFORD".Equals(_AddressList.First().Fulladdress1) Then
                log.Info($"Address 1: '{_AddressList.First().Fulladdress1}'{Environment.NewLine}Address 2: '{_AddressList.First().Fulladdress2}'")
            End If
            For Each badAddress In badAddressList
                badAddress.Fulladdress2 = String.Empty
            Next
        End If
    End Sub

    Private Function GenerateSuiteNumbers(LowValue As String, HighValue As String, SuiteRangeType As String) As Generic.List(Of String)
        Dim rx As New System.Text.RegularExpressions.Regex("(\d+|\W|_|[a-zA-Z])")
        Dim StepValue As Integer
        If SuiteRangeType = "B" Then
            StepValue = 1
        Else
            StepValue = 2
        End If

        Dim mLowValue As System.Text.RegularExpressions.MatchCollection = rx.Matches(LowValue)
        Dim mHighValue As System.Text.RegularExpressions.MatchCollection = rx.Matches(HighValue)

        Dim ranges(mLowValue.Count - 1) As Generic.List(Of String)

        ' This assumes that mLowValue.count = mHighValue.count, which is true as this is how it comes back from the address scrubber
        For ndx As Integer = 0 To mHighValue.Count - 1
            Dim tmpStepValue As Integer = StepValue
            If ndx = mHighValue.Count - 1 Then
                tmpStepValue = StepValue
            Else
                tmpStepValue = 1
            End If
            ranges(ndx) = GenerateRange(mLowValue(ndx).Value, mHighValue(ndx).Value, StepValue)
        Next

        Dim suiteList As New Generic.List(Of String)

        GenerateNumbers(suiteList, String.Empty, ranges, 0)
        Return suiteList
    End Function

    Private Sub GenerateNumbers(ByRef suiteList As Generic.List(Of String), preFix As String, ranges() As Generic.List(Of String), depth As Integer)
        For Each suffix As String In ranges(depth)
            If ranges.Length - 1 = depth Then
                suiteList.Add(preFix & suffix)
            Else
                GenerateNumbers(suiteList, preFix & suffix, ranges, depth + 1)
            End If
        Next
    End Sub

    Private Function GenerateRange(StartValue As String, Endvalue As String, StepValue As Integer) As Generic.List(Of String)
        Dim results As New Generic.List(Of String)
        If StartValue = Endvalue Then ' this takes care of - or A for start and A for end.  no wasted cycles.
            results.Add(StartValue)
        ElseIf Long.TryParse(StartValue, 0) Then
            Dim padToLength As Integer = 1

            ' this will correctly pad on any numbers if the range of the number is 0001 - 1000
            If StartValue.Length = Endvalue.Length Then
                padToLength = Endvalue.Length
            End If

            For i As Long = Long.Parse(StartValue) To Long.Parse(Endvalue) Step StepValue
                results.Add(i.ToString().PadLeft(padToLength, "0"c))
            Next
        Else
            For c As Integer = Asc(StartValue.Substring(0, 1)) To Asc(Endvalue.Substring(0, 1)) Step StepValue
                results.Add(Chr(c))
            Next
        End If

        Return results
    End Function

    Private Function GenerateAddressSessionVariable(ByVal Address As String, ByVal City As String, ByVal State As String, ByVal ZipCode As String) As String
        Dim sessionAddress = Address + City + State + ZipCode
        sessionAddress = sessionAddress.Replace(" ", "")

        Return sessionAddress.Normalize()
    End Function

    Private Function GenerateAddressSessionPreciselyVariable(ByVal Address As String, ByVal City As String, ByVal State As String, ByVal ZipCode As String) As String
        '112041 

        Dim sessionAddressPrecisely = Address + City + State + ZipCode + "PreciselyAddress"
        sessionAddressPrecisely = sessionAddressPrecisely.Replace(" ", "")
        Return sessionAddressPrecisely.Normalize()
    End Function
    Private Sub AddValidationMessagesToCache(ByVal SessionAddr As String)
        _db.StringSet(SessionAddr + "_Err", JsonConvert.SerializeObject(_ErrorMessages), _cacheRetention)
        _db.StringSet(SessionAddr + "_Warn", JsonConvert.SerializeObject(_WarningMessages), _cacheRetention)
        _db.StringSet(SessionAddr + "_GeoErr", JsonConvert.SerializeObject(_GeoErrorMessages), _cacheRetention)
    End Sub

    Private Sub AddValidationMessagesPreciselyToCache(ByVal SessionAddrPrecisely As String, ByRef _GeoErrorMessagesPrecisely As Collections.Generic.List(Of String))
        '112041

        _db.StringSet(SessionAddrPrecisely + "_GeoErr", JsonConvert.SerializeObject(_GeoErrorMessagesPrecisely), _cacheRetention)
    End Sub

    Private Sub SetValidationMessagesFromCache(ByVal SessionAddr As String)
        If _db.KeyExists(SessionAddr + "_Err") Then
            _ErrorMessages = JsonConvert.DeserializeObject(Of Generic.List(Of String))(_db.StringGet(SessionAddr + "_Err"))
        Else
            _ErrorMessages = New Collections.Generic.List(Of String)
        End If
        If _db.KeyExists(SessionAddr + "_Warn") Then
            _WarningMessages = JsonConvert.DeserializeObject(Of Generic.List(Of String))(_db.StringGet(SessionAddr + "_Warn"))
        Else
            _WarningMessages = New Collections.Generic.List(Of String)
        End If
        If _db.KeyExists(SessionAddr + "_GeoErr") Then
            _GeoErrorMessages = JsonConvert.DeserializeObject(Of Generic.List(Of String))(_db.StringGet(SessionAddr + "_GeoErr"))
        Else
            _GeoErrorMessages = New Collections.Generic.List(Of String)
        End If
    End Sub

    Private Sub SetValidationMessagesPreciselyFromCache(ByVal SessionAddrPrecisely As String, ByRef _GeoErrorMessagesPrecisely As Collections.Generic.List(Of String))
        '112041

        If _db.KeyExists(SessionAddrPrecisely + "_GeoErr") Then
            _GeoErrorMessagesPrecisely = JsonConvert.DeserializeObject(Of Generic.List(Of String))(_db.StringGet(SessionAddrPrecisely + "_GeoErr"))
        Else
            _GeoErrorMessagesPrecisely = New Collections.Generic.List(Of String)
        End If
    End Sub

    Private Sub SetMessagesFromCodes(ResultCodes As String(), listType As Collections.Generic.Dictionary(Of String, ScrubberMessage), ByRef ConsiderAlternatives As String)
        For Each ResultCode As String In ResultCodes
            If listType.ContainsKey(ResultCode) Then
                With listType(ResultCode)
                    If .MessageType = MessageConstants.TMessageType.REturnError Then
                        _ErrorMessages.Add(.FriendlyMessage)
                    ElseIf .MessageType = MessageConstants.TMessageType.ReturnSelectionPrimary Then
                        ConsiderAlternatives = "PRIMARY"
                        _WarningMessages.Add(.FriendlyMessage)
                    ElseIf .MessageType = MessageConstants.TMessageType.ReturnSelectionSecondary Then
                        ConsiderAlternatives = "SECONDARY"
                        _WarningMessages.Add(.FriendlyMessage)
                    End If

                    log.Debug($"ResultCode: { .FriendlyMessage} [{ .DetailedMessage}]")
                End With
            End If
        Next
    End Sub

    Private Function GetPreciselyGeocodeData(ByVal Address As String, ByVal City As String, ByVal State As String, ByVal ZipCode As String) As Boolean
        '112041
        '167353 Changed logic from Error logging to Info logging.
        Dim _geocodeapiobj As New PreciselyGeoCode.PreciselyGeoCodeCalls()
        Try
            GeocodeResponse = Task.Run(Async Function() Await _geocodeapiobj.GetGeoCodeForOneAddress(Address + " " + City + " " + State + " " + ZipCode)).Result

            If GeocodeResponse IsNot Nothing AndAlso (GeocodeResponse.PreciselyCallStatusCode = 4 OrElse GeocodeResponse.PreciselyCallStatusCode = 5) Then '4 is Client error 5 is Server error.
                log.Info($"Precisely GetPreciselyGeocodeData the connection with Precisely failed for {Address} {City} {State} {ZipCode}")
                Return False
            End If
            '167353 Changed messages from Error to Info because all Results will be written to PreciselyGeocodeData. This logic is now simply for logging information.
            If GeocodeResponse Is Nothing OrElse GeocodeResponse.responses Is Nothing OrElse GeocodeResponse.responses(0) Is Nothing OrElse GeocodeResponse.responses(0).status Is Nothing OrElse GeocodeResponse.responses(0).results Is Nothing OrElse GeocodeResponse.responses(0).results(0) Is Nothing Then
                log.Info($"Precisely GetPreciselyGeocodeData no data for PLLocation columns returned for {Address} {City} {State} {ZipCode}")
            End If
            If GeocodeResponse IsNot Nothing AndAlso GeocodeResponse.responses IsNot Nothing AndAlso GeocodeResponse.responses(0) IsNot Nothing Then
                If GeocodeResponse.responses(0).status IsNot Nothing Then
                    log.Info($"Precisely GetPreciselyGeocodeData Response Status '{GeocodeResponse.responses(0).status}' for {Address} {City} {State} {ZipCode}")
                End If
                If GeocodeResponse.responses(0).results IsNot Nothing AndAlso GeocodeResponse.responses(0).results(0) IsNot Nothing Then
                    If GeocodeResponse.responses(0).results(0).address Is Nothing OrElse GeocodeResponse.responses(0).results(0).address.admin2 Is Nothing OrElse GeocodeResponse.responses(0).results(0).address.admin2.longName Is Nothing Then
                        log.Info($"Precisely GetPreciselyGeocodeData County not returned for {Address} {City} {State} {ZipCode}")
                    End If
                    If GeocodeResponse.responses(0).results(0).customFields IsNot Nothing Then
                        If GeocodeResponse.responses(0).results(0).customFields.COUNTY_FIPS Is Nothing Then
                            log.Info($"Precisely GetPreciselyGeocodeData CountyFIPS not returned for {Address} {City} {State} {ZipCode}")
                        End If
                        If GeocodeResponse.responses(0).results(0).customFields.CONFIDENCE Is Nothing Then
                            log.Info($"Precisely GetPreciselyGeocodeData Confidence not returned for {Address} {City} {State} {ZipCode}")
                        End If
                        If GeocodeResponse.responses(0).results(0).customFields.PB_KEY Is Nothing Then
                            log.Info($"Precisely GetPreciselyGeocodeData PBKey not returned for {Address} {City} {State} {ZipCode}")
                        End If
                    Else
                        log.Info($"Precisely GetPreciselyGeocodeData CountyFIPS not returned for {Address} {City} {State} {ZipCode}")
                        log.Info($"Precisely GetPreciselyGeocodeData Confidence not returned for {Address} {City} {State} {ZipCode}")
                        log.Info($"Precisely GetPreciselyGeocodeData PBKey not returned for {Address} {City} {State} {ZipCode}")
                    End If
                    If GeocodeResponse.responses(0).results(0).location Is Nothing OrElse GeocodeResponse.responses(0).results(0).location.feature Is Nothing OrElse GeocodeResponse.responses(0).results(0).location.feature.geometry Is Nothing OrElse GeocodeResponse.responses(0).results(0).location.feature.geometry.coordinates Is Nothing OrElse Not GeocodeResponse.responses(0).results(0).location.feature.geometry.coordinates(0).HasValue OrElse Not GeocodeResponse.responses(0).results(0).location.feature.geometry.coordinates(1).HasValue Then
                        log.Info($"Precisely GetPreciselyGeocodeData Latitude/Longitude not returned for {Address} {City} {State} {ZipCode}")
                    End If
                Else
                    log.Info($"Precisely GetPreciselyGeocodeData County not returned for {Address} {City} {State} {ZipCode}")
                    log.Info($"Precisely GetPreciselyGeocodeData CountyFIPS not returned for {Address} {City} {State} {ZipCode}")
                    log.Info($"Precisely GetPreciselyGeocodeData Confidence not returned for {Address} {City} {State} {ZipCode}")
                    log.Info($"Precisely GetPreciselyGeocodeData PBKey not returned for {Address} {City} {State} {ZipCode}")
                    log.Info($"Precisely GetPreciselyGeocodeData Latitude/Longitude not returned for {Address} {City} {State} {ZipCode}")
                End If
            End If
            log.Info($"Precisely GetPreciselyGeocodeData end call for {Address} {City} {State} {ZipCode}")
            Return True

        Catch ex As Exception
            GeocodeResponse = New PreciselyGeoCodeOneAddressOutput
            log.Error($"Precisely GetPreciselyGeocodeData unexpected or invalid response exception for {Address} {City} {State} {ZipCode}, {ex}")
            Return False
        End Try

    End Function

    Private Sub PopulateAddressDetailWithPreciselyGeocodeData(ByVal Address As String, ByVal City As String, ByVal State As String, ByVal ZipCode As String, ByRef ad As AddressDetail)
        '112041 replace Melissa data fields if StandardizeAddress called this method. Populate new fields retrieved from Precisely if Precisely web service call returned data for any of the columns.
        '167353 Added logic to save any Precisely result field that has data into PLLocation table regardless if any other field has data or not.
        'Always write the PreciselyGeocodeDataID value to the PLLocation table. Write any other column that has a value returned from Precisely.

        '167353 Clear County and CountyFIPS values that may have been populated by Melissa Data. Only Precisely data is used for these fields, even if Precisely doesn't return values.
        ad.CountyFips = ""

        If GeocodeResponse IsNot Nothing Then
            ad.PreciselyGeocodeDataID = GeocodeResponse.PreciselyGeocodeDataID 'If there is no other data, at least populate the PK to PreciselyGeocodeData table.
            If GeocodeResponse IsNot Nothing AndAlso GeocodeResponse.responses IsNot Nothing AndAlso GeocodeResponse.responses(0) IsNot Nothing AndAlso GeocodeResponse.responses(0).results IsNot Nothing AndAlso GeocodeResponse.responses(0).results(0) IsNot Nothing Then
                If GeocodeResponse.responses(0).results(0).customFields IsNot Nothing Then
                    ad.PrecisionLevel = GeocodeResponse.responses(0).results(0).customFields.PRECISION_LEVEL 'returned as integer
                End If
                If GeocodeResponse.responses(0).results(0).customFields IsNot Nothing Then
                    If GeocodeResponse.responses(0).results(0).customFields.COUNTY_FIPS IsNot Nothing Then
                        ad.CountyFips = GeocodeResponse.responses(0).results(0).customFields.COUNTY_FIPS
                    End If
                    If GeocodeResponse.responses(0).results(0).customFields.CONFIDENCE IsNot Nothing Then
                        ad.MatchConfidence = GeocodeResponse.responses(0).results(0).customFields.CONFIDENCE
                    End If
                    If GeocodeResponse.responses(0).results(0).customFields.PB_KEY IsNot Nothing Then
                        ad.PBKey = GeocodeResponse.responses(0).results(0).customFields.PB_KEY
                    End If
                End If
                If GeocodeResponse.responses(0).results(0).location IsNot Nothing AndAlso GeocodeResponse.responses(0).results(0).location.feature IsNot Nothing AndAlso GeocodeResponse.responses(0).results(0).location.feature.geometry IsNot Nothing AndAlso GeocodeResponse.responses(0).results(0).location.feature.geometry.coordinates IsNot Nothing AndAlso GeocodeResponse.responses(0).results(0).location.feature.geometry.coordinates(0).HasValue AndAlso GeocodeResponse.responses(0).results(0).location.feature.geometry.coordinates(1).HasValue Then
                    ad.Longitude = Convert.ToDouble(GeocodeResponse.responses(0).results(0).location.feature.geometry.coordinates(0))
                    ad.Latitude = Convert.ToDouble(GeocodeResponse.responses(0).results(0).location.feature.geometry.coordinates(1))
                End If
            End If
        End If

        log.Info($"Precisely PopulateAddressDetailWithPreciselyGeocodeData end for {Address} {City} {State} {ZipCode}")
    End Sub

    <WebMethod(Description:="Method used to Parse an address that could not be standardized")>
    Public Function ParseAddress(ByVal Address As String, ByVal City As String, ByVal State As String, ByVal ZipCode As String) As AddressDetail
        _Stopwatch.Restart()
        Dim addr As New mdParse

        Try
            addr.Parse(Address)
            Dim ad As AddressDetail = New AddressDetail

            With ad
                .HouseNumber = addr.GetRange
                .StreetName = addr.GetStreetName.ToUpper.Replace("'", " ")
                .Predirection = addr.GetPreDirection.ToUpper
                .Postdirection = addr.GetPostDirection.ToUpper
                .Suffix = addr.GetSuffix.ToUpper
                .SuiteName = addr.GetSuiteName.ToUpper
                .SuiteRange = addr.GetSuiteNumber
                .City = City.ToUpper
                .CityAbbreviation = .City
                .State = State.ToUpper
                .ZipCode = ZipCode
                .AssociatedName = ""
                .Zip4 = ""
                .County = ""
                .CountyFips = ""
                .Fulladdress1 = ad.BuildFullAddress()
                .Fulladdress2 = (.SuiteName.Trim & " " & .SuiteRange).Trim
            End With

            Return ad
        Finally
            If addPtr IsNot Nothing Then
                addPtr.Dispose()
                addPtr = Nothing
            End If
            _Stopwatch.Stop()
            log.Info($"[AddressScrubber] ParseAddress complete in: {_Stopwatch.Elapsed.Minutes}m {_Stopwatch.Elapsed.Seconds}s {_Stopwatch.Elapsed.Milliseconds}ms")
        End Try
    End Function
    <WebMethod(Description:="Method used to call Precisely Geocode web service")>
    Public Function GetPreciselyGeocode(ByVal Address As String, ByVal City As String, ByVal State As String, ByVal ZipCode As String) As (AddressDetail, Collections.Generic.List(Of String))

        '112041 New Web Service 
        'Note adding a ByRef makes the input complex; a complex input can't be tested via localhost.

        log.Info($"Precisely GetPreciselyGeocode web service begin for {Address} {City} {State} {ZipCode}")

        Dim _GeoErrorMessagesPrecisely As New Collections.Generic.List(Of String)

        Dim sessionAddressPrecisely = GenerateAddressSessionPreciselyVariable(Address, City, State, ZipCode)

        Dim isCached = False

        Dim ad As New AddressDetail With {
            .County = "",
            .CountyFips = "",
            .PrecisionLevel = Nothing,
            .MatchConfidence = "",
            .PBKey = "",
            .PreciselyGeocodeDataID = Guid.Empty}

        Try
            If _db.KeyExists(sessionAddressPrecisely) Then
                Try
                    SetValidationMessagesPreciselyFromCache(sessionAddressPrecisely, _GeoErrorMessagesPrecisely)
                    ad = JsonConvert.DeserializeObject(Of AddressDetail)(_db.StringGet(sessionAddressPrecisely))
                    Dim addComp As New AddressDetailComparer
                    isCached = True
                    log.Info($"Precisely GetPreciselyGeocode web service Return (Cached) Address for {Address} {City} {State} {ZipCode}")
                    Return (ad, _GeoErrorMessagesPrecisely)
                Catch ex As Exception
                    ' If for some reason caching breaks, just use the code below and clear the current entry
                    _db.KeyDelete(sessionAddressPrecisely)
                    log.Info($"Precisely GetPreciselyGeocode retrieve cache exception for {Address} {City} {State} {ZipCode}, {ex}")
                End Try
                'feature 167310 apoosala Precisely varaible for 30days table check PreciselyGeocodeData (30days database cache) start
            End If
            If EnableSystemFeature.IsFeatureEnabled(FeatureToggleNames.Policy.Reporting.WO112041_PreciselyWebService, Now) Then
                Dim addressscrubberdal = New AddressScrubber_DAL
                precisely30DaysCache = addressscrubberdal.IsAddressExistsInTheLast30DaysForPrecisely(Address, City, State, ZipCode)
                If (precisely30DaysCache IsNot Nothing) Then
                    ad.PreciselyGeocodeDataID = precisely30DaysCache.PreciselyGeocodeDataID
                    ad.PBKey = precisely30DaysCache.PBKey
                    ad.PrecisionLevel = precisely30DaysCache.PrecisionLevel
                    ad.MatchConfidence = precisely30DaysCache.MatchConfidence
                    ad.Latitude = precisely30DaysCache.Latitude
                    ad.Longitude = precisely30DaysCache.Longitude
                    ad.CountyFips = precisely30DaysCache.Countyfips
                    log.Info($"Precisely GetPreciselyGeocode web service Return (From Precisely 30Days Cached) Address for {Address} {City} {State} {ZipCode}")
                    _db.StringSet(sessionAddressPrecisely, JsonConvert.SerializeObject(ad, Formatting.Indented), _cacheRetention)
                    AddValidationMessagesPreciselyToCache(sessionAddressPrecisely, _GeoErrorMessagesPrecisely)
                    Return (ad, _GeoErrorMessagesPrecisely)
                End If
                'feature 167310 apoosala Precisely varaible for 30days cache end
            End If

            SuccessfulGeocodeReturn = GetPreciselyGeocodeData(Address, City, State, ZipCode)

            If Not EnableSystemFeature.IsFeatureEnabled(FeatureToggleNames.Policy.Reporting.WO112041_PreciselyWebService, Now) Then
                If Not SuccessfulGeocodeReturn Then
                    _GeoErrorMessagesPrecisely.Add("There was an error in 'GetPreciselyGeocode' web service")
                    log.Error($"Precisely GetPreciselyGeocode error from GetPreciselyGeocodeData for {Address} {City} {State} {ZipCode}")
                Else
                    PopulateAddressDetailWithPreciselyGeocodeData(Address, City, State, ZipCode, ad)
                    log.Info($"Precisely GetPreciselyGeocode web service end for {Address} {City} {State}, {ZipCode}")
                End If
                _db.StringSet(sessionAddressPrecisely, JsonConvert.SerializeObject(ad, Formatting.Indented), _cacheRetention)
                AddValidationMessagesPreciselyToCache(sessionAddressPrecisely, _GeoErrorMessagesPrecisely)
            End If

            If EnableSystemFeature.IsFeatureEnabled(FeatureToggleNames.Policy.Reporting.WO112041_PreciselyWebService, Now) Then
                PopulateAddressDetailWithPreciselyGeocodeData(Address, City, State, ZipCode, ad)
                If Not SuccessfulGeocodeReturn OrElse ad.Latitude = 0 OrElse ad.Longitude = 0 Then '4 is Client error 5 is Server error.
                    _GeoErrorMessagesPrecisely.Add("The connection with Precisely failed.")
                End If
                log.Info($"Precisely GetPreciselyGeocode web service end for {Address} {City} {State} {ZipCode}")
                If SuccessfulGeocodeReturn Then 'Not httpstatuscode 4 or 5.
                    '167353 Dont save address to cache if Precisely call failed; this prevents a user from getting a successful hit until cache is expired.
                    _db.StringSet(sessionAddressPrecisely, JsonConvert.SerializeObject(ad, Formatting.Indented), _cacheRetention)
                    AddValidationMessagesPreciselyToCache(sessionAddressPrecisely, _GeoErrorMessagesPrecisely)
                End If
            End If

        Catch ex As Exception
            If ad IsNot Nothing Then
                log.Info($"Precisely GetPreciselyGeocode exception for {Address} {City} {State} {ZipCode} ad: {JsonConvert.SerializeObject(ad)}, {ex}")
            Else
                log.Info($"Precisely GetPreciselyGeocode exception for {Address} {City} {State} {ZipCode}, {ex}")
            End If
            _GeoErrorMessagesPrecisely.Add("There was an error in 'GetPreciselyGeocode' web service")
        End Try

        Return (ad, _GeoErrorMessagesPrecisely)

    End Function

End Class