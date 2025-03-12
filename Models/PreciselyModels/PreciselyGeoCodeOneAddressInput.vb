Imports System.Collections.Generic
Namespace Models.PreciselyModels

    'Work Item 112041
    Public Class PreciselyGeoCodeOneAddressInput
        Public Property preferences As Preferences
        Public Property addresses As List(Of PreciselyAddress)
    End Class

    Public Class Preferences
        Public Property maxResults As Integer
        Public Property returnAllInfo As Boolean
        Public Property factoryDescription As Factorydescription
        Public Property clientLocale As String
        Public Property clientCoordSysName As String
        Public Property distance As Distance
        Public Property streetOffset As Streetoffset
        Public Property cornerOffset As Corneroffset
        Public Property fallbackToGeographic As Boolean
        Public Property fallbackToPostal As Boolean
        Public Property matchMode As String
        Public Property returnOfAdditionalFields As Boolean
        Public Property originXY As List(Of Object)
        Public Property customPreferences As Custompreferences
    End Class

    Public Class Factorydescription
        Public Property label As String
        Public Property featureSpecific As Featurespecific
    End Class

    Public Class Featurespecific
    End Class

    Public Class Distance
        Public Property value As Integer
        Public Property distanceUnit As String
    End Class

    Public Class Streetoffset
        Public Property value As Integer
        Public Property distanceUnit As String
    End Class

    Public Class Corneroffset
        Public Property value As Integer
        Public Property distanceUnit As String
    End Class

    Public Class Custompreferences
        Public Property SEARCH_ADDRESS_NUMBER As Boolean
        Public Property EXPANDED_RANGE_UNIT As String
        Public Property SEARCH_UNIT_INFORMATION As String
        Public Property RETURNALLCUSTOMFIELDS As Boolean
        Public Property RETURNUNITINFORMATION As Boolean

    End Class

    Public Class PreciselyAddress
        Public Property addressLines As List(Of String)
        Public Property country As String
        Public Property addressNumber As String
        Public Property admin1 As String
        Public Property admin2 As String
        Public Property city As String
        Public Property borough As String
        Public Property neighborhood As String
        Public Property suburb As String
        Public Property postalCode As String
        Public Property postalCodeExt As String
        Public Property placeName As String
        Public Property street As String
        Public Property building As String
        Public Property floor As String
        Public Property room As String
        Public Property unit As String
        Public Property unitType As String
    End Class

End Namespace
