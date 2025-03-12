Imports System.Collections.Generic

Namespace Models.PreciselyModels

    '112041
    Public Class PreciselyGeoCodeOneAddressOutput
        'PrecidelyGeocodeDataID is retrieved from the insert into the PreciselyGeocodeData table,
        'not from Precisely. It is the Primary Key to PreciselyGeocodeData that will
        'be inserted into the PLlocation table.
        Public Property PreciselyCallStatusCode As Integer '167353 one digit to represent httpstatuscode. Values: 2 successful 1 informational 3 redirection 4 Client error 5 Server error.
        Public Property PreciselyGeocodeDataID As Guid
        Public Property responses As List(Of Response)
    End Class

    Public Class Response
        Public Property status As String
        Public Property results As List(Of Result)
    End Class

    Public Class Result
        Public Property score As Integer
        Public Property address As Address
        Public Property addressLines As List(Of String)
        Public Property location As Location
        Public Property explanation As ResultExplanation 'Both Result and Location classes have a "Explanation" object.
        Public Property customFields As Customfields
    End Class

    Public Class Address
        Public Property formattedAddress As String
        Public Property formattedStreetAddress As String
        Public Property formattedLocationAddress As String
        Public Property unitType As String
        Public Property addressNumber As String
        Public Property country As Country
        Public Property admin1 As Admin1
        Public Property admin2 As Admin2
        Public Property city As City
        Public Property postalCode As String
        Public Property postalCodeExt As String
        Public Property street As String
        Public Property unit As String
    End Class

    Public Class Country
        Public Property name As String
        Public Property isoAlpha2Code As String
        Public Property isoAlpha3Code As String
        Public Property isoNumericCode As String
    End Class

    Public Class Admin1
        Public Property longName As String
        Public Property shortName As String
    End Class

    Public Class Admin2
        Public Property longName As String 'Contains County, but also contains the text " County" after the county name. The text " County" needs to be split off before it is stored in database tables.
        Public Property shortName As String 'Contains County, but also contains the text " County" after the county name. The text " County" needs to be split off before it is stored in database tables.
    End Class

    Public Class City
        Public Property longName As String
        Public Property shortName As String
    End Class

    Public Class Location
        Public Property explanation As LocationExplanation 'Both Result and Location classes have a "Explanation" object.
        Public Property feature As Feature
    End Class

    Public Class LocationExplanation
        Public Property type As String
        Public Property description As String
        Public Property designation As String
    End Class

    Public Class Feature
        Public Property type As String
        Public Property properties As Properties
        Public Property geometry As Geometry
    End Class

    Public Class Properties
        Public Property crsName As String
    End Class

    Public Class Geometry
        Public Property type As String
        Public Property coordinates As List(Of Double?)
    End Class

    Public Class ResultExplanation
        Public Property addressMatch As Addressmatch
        Public Property source As Source
    End Class

    Public Class Addressmatch
        Public Property type As String
        Public Property description As List(Of Description)
    End Class

    Public Class Description
        Public Property label As String
        Public Property matchType As String
    End Class

    Public Class Source
        Public Property label As String
    End Class

    Public Class Customfields
        Public Property RESBUS As String
        Public Property LOTSIZE_METERS As String
        Public Property CSA_NUMBER As String
        Public Property TYPE_SHORT As String
        Public Property THOROUGHFARE_TYPE As String
        Public Property PARCEN_ELEVATION_METERS As String
        Public Property ROAD_CLASS As String
        Public Property MATCH_CODE As String
        Public Property DFLT As String
        Public Property COUNTY As String
        Public Property PB_KEY As String
        Public Property LANGUAGE As String
        Public Property UNIT_TYPE As String
        Public Property ADDRTYPE As String
        Public Property INC_IND As String
        Public Property BLOCK_2010 As String
        Public Property POINT_ID As String
        Public Property CHECK_DIGIT As String
        Public Property METRO_FLAG As String
        Public Property BLOCK As String
        Public Property POST_THOROUGHFARE_TYPE As String
        Public Property QCITY As String
        Public Property ZIP_FACILITY As String
        Public Property TFID As String
        Public Property APN_ID As String
        Public Property LOT_CODE As String
        Public Property LOT_NUM As String
        Public Property GEOHASH As String
        Public Property CTYST_KEY As String
        Public Property UACEPOP As String
        Public Property NAME As String
        Public Property ZIP_CARRTSORT As String
        Public Property LORANGE As String
        Public Property CLOSE_MATCH As String
        Public Property STREET_SIDE As String
        Public Property DATATYPE As String
        Public Property INTERSECTION As String
        Public Property ZIP_CITY_DELV As String
        Public Property LOC_CODE As String
        Public Property CART As String
        Public Property NAME_CITY As String
        Public Property BLOCK_LEFT As String
        Public Property COUNTY_FIPS As String
        Public Property PRECISION_LEVEL As Integer
        Public Property HIRANGE As String
        Public Property UACE As String
        Public Property REC_TYPE As String
        Public Property HI_RISE_DFLT As String
        Public Property URBANICITY As String
        Public Property RESOLVED_LINE As String
        Public Property MATCH_TYPE As String
        Public Property PARCEN_ELEVATION As String
        Public Property PREF_CITY As String
        Public Property CBSA_NUMBER As String
        Public Property ALT_FLAG As String
        Public Property SEGMENT_DIRECTION As String
        Public Property ADDRLINE_SHORT As String
        Public Property LOTSIZE As String
        Public Property CONFIDENCE As String
        Public Property HIZIP4 As String
        Public Property DATATYPE_NAME As String
        Public Property SEGMENT_PARITY As String
        Public Property LOZIP4 As String
        Public Property CSA_NAME As String
        Public Property DPBC As String
        Public Property LASTLINE_SHORT As String
        Public Property PLACE As String
        Public Property MAIL_STOP As String
        Public Property CITY_SHORT As String
        Public Property NAME_SHORT As String
        Public Property ZIP9 As String
        Public Property IS_ALIAS As String
        Public Property PRECISION_CODE As String
        Public Property ZIP10 As String
        Public Property PROPERTY_ACCOUNT_ID As String
        Public Property CBSA_NAME As String
        Public Property RANGE_PARITY As String
    End Class

End Namespace
