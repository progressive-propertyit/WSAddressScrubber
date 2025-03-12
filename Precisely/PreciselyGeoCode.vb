Imports Newtonsoft.Json
Imports System.Collections.Generic
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Threading.Tasks
Imports System.Configuration
Imports WSAddressScrubber.Models.PreciselyModels
Imports System.Net
Imports System.Threading
Imports EINS.Models.Extensions '167353
Imports EINS.Models.Constants '167353

'Work Item 112041 

Namespace PreciselyGeoCode

    Public Class PreciselyGeoCodeCalls

        Private Shared ReadOnly log As log4net.ILog = log4net.LogManager.GetLogger("WSAddressScrubber.PreciselyGeoCode")
        Private ReadOnly preciselyGeocodeURI As String = ConfigurationManager.AppSettings("PreciselyGeocodeURI").ToString
        Private _enableSystemFeature As EINS.Models.Service.EnableSystemFeature '167353

        Private ReadOnly Property EnableSystemFeature As EINS.Models.Service.EnableSystemFeature '167353
            Get
                If _enableSystemFeature Is Nothing Then
                    _enableSystemFeature = New EINS.Models.Service.EnableSystemFeature()
                End If

                Return _enableSystemFeature
            End Get
        End Property


        Public Async Function GetGeoCodeForOneAddress(ByVal addressLine As String) As Task(Of PreciselyGeoCodeOneAddressOutput)

            'This function inputs one address and returns results for one address. If the Http response Status = 200, a row is inserted into the PreciselyGeocodeData table.
            'Precisely has the ability to input multiple addresses and return results for multiple addresses.
            'The input and output models will be different for one address and multiple addresses.

            log.Info($"Precisely GetGeoCodeForOneAddress begin for addressLine {addressLine}")
            Dim tokenVal As String = ""
            Dim tokenLock As New Object()
            Dim deserializedClass As New PreciselyGeoCodeOneAddressOutput

            '-------------------------------------------------------------------------------------------------------
            '-------------------------------------------------------------------------------------------------------
            'The commented out lines within this message are for testing only when Precisely is
            'unavailable. This logic bypasses the call to get a token and simulates the resultset
            'returned from a Precisely geocode.
            'For testing only Dim jsonResponse = "{""responses"":[{""status"":""OK"",""results"":[{""score"":100,""address"":{""formattedAddress"":""3100 S MANCHESTER ST APT 903, FALLS CHURCH VA 22044-2717, United States"",""formattedStreetAddress"":""3100 S MANCHESTER ST APT 903"",""formattedLocationAddress"":""FALLS CHURCH, VA  22044-2717"",""unitType"":""APT"",""addressNumber"":""3100"",""country"":{""name"":""United States"",""isoAlpha2Code"":""US"",""isoAlpha3Code"":""USA"",""isoNumericCode"":""840""},""admin1"":{""longName"":""Virginia"",""shortName"":""VA""},""admin2"":{""longName"":""FAIRFAX COUNTY"",""shortName"":""FAIRFAX COUNTY""},""city"":{""longName"":""FALLS CHURCH"",""shortName"":""FALLS CHURCH""},""postalCode"":""22044"",""postalCodeExt"":""2717"",""street"":""MANCHESTER"",""unit"":""903""},""addressLines"":[""3100 S MANCHESTER ST APT 903"",""FALLS CHURCH VA 22044-2717"",""United States""],""location"":{""explanation"":{""type"":""ADDRESS_POINT"",""description"":""BUILDING"",""designation"":""CENTROID""},""feature"":{""type"":""Feature"",""properties"":{""crsName"":""epsg: 4326""},""geometry"":{""type"":""Point"",""coordinates"":[-77.136541,38.864189]}}},""explanation"":{""addressMatch"":{""type"":""ADDRESS"",""description"":[{""label"":""placeName"",""matchType"":""NONE""},{""label"":""addressNumber"",""matchType"":""EXACT""},{""label"":""admin1"",""matchType"":""EXACT""},{""label"":""admin2"",""matchType"":""NONE""},{""label"":""city"",""matchType"":""EXACT""},{""label"":""neighborhood"",""matchType"":""NONE""},{""label"":""suburb"",""matchType"":""NONE""},{""label"":""street"",""matchType"":""EXACT""},{""label"":""postalCode"",""matchType"":""EXACT""},{""label"":""streetType"",""matchType"":""EXACT""},{""label"":""postalCodeExt"",""matchType"":""NONE""},{""label"":""streetDirectional"",""matchType"":""EXACT""}]},""source"":{""label"":""ggs""}},""customFields"":{""RESBUS"":""R"",""LOTSIZE_METERS"":""4509"",""CSA_NUMBER"":""548"",""TYPE_SHORT"":""ST"",""THOROUGHFARE_TYPE"":""ST"",""HIUNIT"":""903"",""PARCEN_ELEVATION_METERS"":""82"",""ROAD_CLASS"":""01"",""MATCH_CODE"":""S800"",""COUNTY"":""51059"",""PB_KEY"":""P0000NH3X5RE"",""LANGUAGE"":""en"",""UNIT_TYPE"":""APT"",""ADDRTYPE"":""M"",""INC_IND"":""N"",""BLOCK_2010"":""510594515013001"",""POINT_ID"":""100504682"",""CHECK_DIGIT"":""1"",""METRO_FLAG"":""Y"",""BLOCK"":""510594515013002"",""POST_THOROUGHFARE_TYPE"":""ST"",""QCITY"":""510232000"",""ZIP_FACILITY"":""P"",""TFID"":""207021296"",""UNIT_RANGE_PARITY"":""O"",""APN_ID"":""0514 13020903"",""LOT_CODE"":""A"",""LOT_NUM"":""0071"",""GEOHASH"":""dqcjhenbk3fj"",""CTYST_KEY"":""X26594"",""UACEPOP"":""4586770"",""NAME"":""MANCHESTER"",""ZIP_CARRTSORT"":""D"",""PARENT_ID"":""P0000NH5UV4B"",""LORANGE"":""3100"",""CLOSE_MATCH"":""T"",""STREET_SIDE"":""L"",""DATATYPE"":""12"",""INTERSECTION"":""F"",""ZIP_CITY_DELV"":""Y"",""LOUNIT"":""903"",""LOC_CODE"":""AP05"",""CART"":""C034"",""NAME_CITY"":""FALLS CHURCH"",""BLOCK_LEFT"":""510594515013002"",""COUNTY_FIPS"":""51059"",""PRECISION_LEVEL"":""19"",""HIRANGE"":""3100"",""UACE"":""92242"",""REC_TYPE"":""H"",""HI_RISE_DFLT"":""N"",""URBANICITY"":""L"",""RESOLVED_LINE"":""0"",""MATCH_TYPE"":""ADDRESS"",""PARCEN_ELEVATION"":""269"",""PREF_CITY"":""FALLS CHURCH"",""CBSA_NUMBER"":""47900"",""ALT_FLAG"":""B"",""SEGMENT_DIRECTION"":""F"",""ADDRLINE_SHORT"":""3100 S MANCHESTER ST APT 903"",""LOTSIZE"":""48534"",""CONFIDENCE"":""100"",""HIZIP4"":""2717"",""DATATYPE_NAME"":""MASTER LOCATION"",""SEGMENT_PARITY"":""R"",""CBSA_DIVISION_NAME"":""WASHINGTON-ARLINGTON-ALEXANDRIA, DC-VA-MD-WV METROPOLITAN DIVISION"",""LOZIP4"":""2717"",""PREDIR_SHORT"":""S"",""PRE_DIRECTIONAL"":""S"",""CSA_NAME"":""WASHINGTON-BALTIMORE-ARLINGTON, DC-MD-VA-WV-PA COMBINED STATISTICAL AREA"",""DPBC"":""28"",""LASTLINE_SHORT"":""FALLS CHURCH, VA  22044-2717"",""PLACE"":""5171216"",""CITY_SHORT"":""FALLS CHURCH"",""NAME_SHORT"":""MANCHESTER"",""ZIP9"":""220442717"",""IS_ALIAS"":""N01"",""PRECISION_CODE"":""S8HPNTSCZA"",""ZIP10"":""22044-2717"",""CBSA_NAME"":""WASHINGTON-ARLINGTON-ALEXANDRIA, DC-VA-MD-WV METROPOLITAN STATISTICAL AREA"",""RANGE_PARITY"":""E"",""CBSA_DIVISION_NUMBER"":""47894""}}]}]}" 
            'For testing only deserializedClass = JsonConvert.DeserializeObject(Of PreciselyGeoCodeOneAddressOutput)(jsonResponse)
            '-------------------------------------------------------------------------------------------------------
            '-------------------------------------------------------------------------------------------------------


            'For testing only deserializedClass = JsonConvert.DeserializeObject(Of PreciselyGeoCodeOneAddressOutput)("{""responses"":[{""status"":""OK"",""results"":[{""score"":100,""address"":{""formattedAddress"":""3100 S MANCHESTER ST APT 903, FALLS CHURCH VA 22044-2717, United States"",""formattedStreetAddress"":""3100 S MANCHESTER ST APT 903"",""formattedLocationAddress"":""FALLS CHURCH, VA  22044-2717"",""unitType"":""APT"",""addressNumber"":""3100"",""country"":{""name"":""United States"",""isoAlpha2Code"":""US"",""isoAlpha3Code"":""USA"",""isoNumericCode"":""840""},""admin1"":{""longName"":""Virginia"",""shortName"":""VA""},""admin2"":{""longName"":""FAIRFAX COUNTY"",""shortName"":""FAIRFAX COUNTY""},""city"":{""longName"":""FALLS CHURCH"",""shortName"":""FALLS CHURCH""},""postalCode"":""22044"",""postalCodeExt"":""2717"",""street"":""MANCHESTER"",""unit"":""903""},""addressLines"":[""3100 S MANCHESTER ST APT 903"",""FALLS CHURCH VA 22044-2717"",""United States""],""location"":{""explanation"":{""type"":""ADDRESS_POINT"",""description"":""BUILDING"",""designation"":""CENTROID""},""feature"":{""type"":""Feature"",""properties"":{""crsName"":""epsg: 4326""},""geometry"":{""type"":""Point"",""coordinates"":[-77.136541,38.864189]}}},""explanation"":{""addressMatch"":{""type"":""ADDRESS"",""description"":[{""label"":""placeName"",""matchType"":""NONE""},{""label"":""addressNumber"",""matchType"":""EXACT""},{""label"":""admin1"",""matchType"":""EXACT""},{""label"":""admin2"",""matchType"":""NONE""},{""label"":""city"",""matchType"":""EXACT""},{""label"":""neighborhood"",""matchType"":""NONE""},{""label"":""suburb"",""matchType"":""NONE""},{""label"":""street"",""matchType"":""EXACT""},{""label"":""postalCode"",""matchType"":""EXACT""},{""label"":""streetType"",""matchType"":""EXACT""},{""label"":""postalCodeExt"",""matchType"":""NONE""},{""label"":""streetDirectional"",""matchType"":""EXACT""}]},""source"":{""label"":""ggs""}},""customFields"":{""RESBUS"":""R"",""LOTSIZE_METERS"":""4509"",""CSA_NUMBER"":""548"",""TYPE_SHORT"":""ST"",""THOROUGHFARE_TYPE"":""ST"",""HIUNIT"":""903"",""PARCEN_ELEVATION_METERS"":""82"",""ROAD_CLASS"":""01"",""MATCH_CODE"":""S800"",""COUNTY"":""51059"",""PB_KEY"":""P0000NH3X5RE"",""LANGUAGE"":""en"",""UNIT_TYPE"":""APT"",""ADDRTYPE"":""M"",""INC_IND"":""N"",""BLOCK_2010"":""510594515013001"",""POINT_ID"":""100504682"",""CHECK_DIGIT"":""1"",""METRO_FLAG"":""Y"",""BLOCK"":""510594515013002"",""POST_THOROUGHFARE_TYPE"":""ST"",""QCITY"":""510232000"",""ZIP_FACILITY"":""P"",""TFID"":""207021296"",""UNIT_RANGE_PARITY"":""O"",""APN_ID"":""0514 13020903"",""LOT_CODE"":""A"",""LOT_NUM"":""0071"",""GEOHASH"":""dqcjhenbk3fj"",""CTYST_KEY"":""X26594"",""UACEPOP"":""4586770"",""NAME"":""MANCHESTER"",""ZIP_CARRTSORT"":""D"",""PARENT_ID"":""P0000NH5UV4B"",""LORANGE"":""3100"",""CLOSE_MATCH"":""T"",""STREET_SIDE"":""L"",""DATATYPE"":""12"",""INTERSECTION"":""F"",""ZIP_CITY_DELV"":""Y"",""LOUNIT"":""903"",""LOC_CODE"":""AP05"",""CART"":""C034"",""NAME_CITY"":""FALLS CHURCH"",""BLOCK_LEFT"":""510594515013002"",""COUNTY_FIPS"":""51059"",""PRECISION_LEVEL"":""19"",""HIRANGE"":""3100"",""UACE"":""92242"",""REC_TYPE"":""H"",""HI_RISE_DFLT"":""N"",""URBANICITY"":""L"",""RESOLVED_LINE"":""0"",""MATCH_TYPE"":""ADDRESS"",""PARCEN_ELEVATION"":""269"",""PREF_CITY"":""FALLS CHURCH"",""CBSA_NUMBER"":""47900"",""ALT_FLAG"":""B"",""SEGMENT_DIRECTION"":""F"",""ADDRLINE_SHORT"":""3100 S MANCHESTER ST APT 903"",""LOTSIZE"":""48534"",""CONFIDENCE"":""100"",""HIZIP4"":""2717"",""DATATYPE_NAME"":""MASTER LOCATION"",""SEGMENT_PARITY"":""R"",""CBSA_DIVISION_NAME"":""WASHINGTON-ARLINGTON-ALEXANDRIA, DC-VA-MD-WV METROPOLITAN DIVISION"",""LOZIP4"":""2717"",""PREDIR_SHORT"":""S"",""PRE_DIRECTIONAL"":""S"",""CSA_NAME"":""WASHINGTON-BALTIMORE-ARLINGTON, DC-MD-VA-WV-PA COMBINED STATISTICAL AREA"",""DPBC"":""28"",""LASTLINE_SHORT"":""FALLS CHURCH, VA  22044-2717"",""PLACE"":""5171216"",""CITY_SHORT"":""FALLS CHURCH"",""NAME_SHORT"":""MANCHESTER"",""ZIP9"":""220442717"",""IS_ALIAS"":""N01"",""PRECISION_CODE"":""S8HPNTSCZA"",""ZIP10"":""22044-2717"",""CBSA_NAME"":""WASHINGTON-ARLINGTON-ALEXANDRIA, DC-VA-MD-WV METROPOLITAN STATISTICAL AREA"",""RANGE_PARITY"":""E"",""CBSA_DIVISION_NUMBER"":""47894""}}]}]}") 

            Dim requestInput As New PreciselyGeoCodeOneAddressInput With {
                .preferences = New Preferences With {
                    .maxResults = 15,
                    .returnAllInfo = True,
                    .distance = New Distance With {.distanceUnit = "METER", .value = 150},
                    .streetOffset = New Streetoffset With {.distanceUnit = "METER", .value = 7},
                    .cornerOffset = New Corneroffset With {.distanceUnit = "METER", .value = 7},
                    .fallbackToGeographic = False,
                    .fallbackToPostal = False,
                    .clientCoordSysName = "",
                    .clientLocale = "",
                    .matchMode = "",
                    .customPreferences = New Custompreferences With {.SEARCH_ADDRESS_NUMBER = True, .RETURNALLCUSTOMFIELDS = True, .RETURNUNITINFORMATION = True},
                    .factoryDescription = New Factorydescription With {
                        .label = "",
                        .featureSpecific = New Featurespecific()
                    },
                    .originXY = New List(Of Object)()
                }
            }

            Dim oneAddress = New PreciselyAddress With {
            .addressLines = New List(Of String),
                .country = "USA",
                .addressNumber = "",
                .admin1 = "",
                .admin2 = "",
                .borough = "",
                .building = "",
                .city = "",
                .floor = "",
                .neighborhood = "",
                .placeName = "",
                .postalCode = "",
                .postalCodeExt = "",
                .room = "",
                .street = "",
                .suburb = "",
                .unit = "",
                .unitType = ""
            }

            requestInput.addresses = New List(Of PreciselyAddress)
            oneAddress.addressLines.Add(addressLine)
            requestInput.addresses.Add(oneAddress)
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12

            Dim jsonRequest = JsonConvert.SerializeObject(requestInput)

            'Keep token request as close to the geocode request to reduce the chance that the token will expire between the two requests.
            Monitor.Enter(tokenLock) 'Prevent multiple threads from getting a new token at the same time
            Try
                Dim _oAuthToken As New PreciselyToken.OAuthLogin
                tokenVal = _oAuthToken.GetOAuthTokenAsync().Result()
            Catch ex As Exception
                log.Error($"Precisely GetGeoCodeForOneAddress lock logic exception for addressLine {addressLine}")

            Finally
                Monitor.Exit(tokenLock)
            End Try

            If tokenVal Is Nothing OrElse String.IsNullOrWhiteSpace(tokenVal) Then
                log.Error($"Precisely GetGeoCodeForOneAddress Error retrieving token from GetOAuthTokenAsync for addressLine {addressLine}")
                deserializedClass.PreciselyCallStatusCode = 4 '4 is Client error (Unauthorized)
                Return deserializedClass

            Else
                deserializedClass.PreciselyCallStatusCode = 2 '167353 token was successfully retrieved
            End If

            deserializedClass = Await ProcessPreciselyResults_167353(deserializedClass, tokenVal, addressLine, jsonRequest)
            Return deserializedClass
        End Function
        Public Async Function ProcessPreciselyResults_167353(deserializedClass As PreciselyGeoCodeOneAddressOutput, tokenVal As String, addressLine As String, jsonRequest As String) As Task(Of PreciselyGeoCodeOneAddressOutput)

            '167353 New Function to split away from 112041 original logic for calling Precisely Geocode API and inserting a row into PreciselyGeocodeData

            Dim jsonResponse As String = ""
            Dim geocodeStatusCode As Integer = 0
            If deserializedClass.PreciselyCallStatusCode = 2 Then 'This is the status of the Get Token Web API call. If there isn't a valid token value, don't call the Geocode Web API

                Dim client As New HttpClient With {
                .BaseAddress = New Uri(preciselyGeocodeURI)
                }
                client.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", tokenVal)
                client.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue("application/json"))
                client.DefaultRequestHeaders.ConnectionClose = True
                client.DefaultRequestHeaders.Add("Connection", "keep-alive")

                log.Info($"Precisely ProcessPreciselyResults_167353: Before Precisely geocode call for addressLine: {addressLine}.")
                Dim response As HttpResponseMessage = Await client.PostAsync("geocode", New StringContent(jsonRequest, Encoding.UTF8, "application/json"))

                'Responses: 1 informational 2 successful 3 redirection 4 Client error 5 Server error.
                Dim responseDigit As Integer = CInt(response.StatusCode)
                geocodeStatusCode = CInt(responseDigit.ToString.Substring(0, 1))
                If geocodeStatusCode = 4 OrElse geocodeStatusCode = 5 Then
                    log.Info($"Precisely ProcessPreciselyResults_167353 HttpStatusCode '{response.StatusCode}' the connection with Precisely failed for addressLine {addressLine}")
                End If

                log.Info($"Precisely ProcessPreciselyResults_167353 after Precisely geocode call for addressLine {addressLine} StatusCode '{response.StatusCode}'")

                jsonResponse = Await response.Content.ReadAsStringAsync()

                deserializedClass = JsonConvert.DeserializeObject(Of PreciselyGeoCodeOneAddressOutput)(jsonResponse)

                deserializedClass.PreciselyCallStatusCode = geocodeStatusCode
            End If

            Dim pBKey As String = ""
            If deserializedClass IsNot Nothing AndAlso deserializedClass.responses IsNot Nothing AndAlso deserializedClass.responses(0) IsNot Nothing AndAlso deserializedClass.responses(0).results IsNot Nothing AndAlso deserializedClass.responses(0).results(0) IsNot Nothing AndAlso deserializedClass.responses(0).results(0).customFields IsNot Nothing AndAlso deserializedClass.responses(0).results(0).customFields.PB_KEY IsNot Nothing Then
                pBKey = deserializedClass.responses(0).results(0).customFields.PB_KEY
            End If

            Dim addressScrubberDal = New AddressScrubber_DAL
            deserializedClass.PreciselyGeocodeDataID = addressScrubberDal.Insert_PreciselyGeocodeData(pBKey, DateTime.Now, jsonRequest, jsonResponse)

            If deserializedClass.PreciselyGeocodeDataID = Guid.Empty Then
                log.Error($"Precisely ProcessPreciselyResults_167353: Error on insert into PreciselyGeocodeData table for addressLine: {addressLine}. Invalid Guid Primary Key was returned.")
            End If

            log.Info($"Precisely ProcessPreciselyResults_167353 end for addressLine {addressLine}")

            Return deserializedClass
        End Function

    End Class

End Namespace

