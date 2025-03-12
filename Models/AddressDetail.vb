Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Friend Class AddressDetailComparer
    Implements IEqualityComparer(Of AddressDetail)

    Public Function Equals1(x As AddressDetail, y As AddressDetail) As Boolean Implements IEqualityComparer(Of AddressDetail).Equals
        Return x = y
    End Function

    Public Function GetHashCode1(obj As AddressDetail) As Integer Implements IEqualityComparer(Of AddressDetail).GetHashCode
        Return obj.GetHashCode()
    End Function
End Class

Friend Module AddressExtensions
    <Extension()>
    Public Function BuildFullAddress(ad As AddressDetail) As String
        Dim tmp As String

        tmp = ad.HouseNumber & " " & ad.Predirection
        tmp = tmp.Trim & " " & ad.StreetName
        tmp = tmp.Trim & " " & ad.Suffix
        tmp = tmp.Trim & " " & ad.Postdirection

        Return tmp.Trim
    End Function
End Module

<Serializable()>
Public Class AddressDetail
    Public AssociatedName As String
    Public HouseNumber As String
    Public Predirection As String
    Public StreetName As String
    Public Suffix As String
    Public Postdirection As String
    Public SuiteName As String
    Public SuiteRange As String
    Public City As String
    Public CityAbbreviation As String
    Public State As String
    Public ZipCode As String
    Public Zip4 As String
    Public County As String
    Public CountyFips As String
    Public CoastalCounty As Integer
    Public Latitude As Double
    Public Longitude As Double
    Public Fulladdress1 As String
    Public Fulladdress2 As String
    Public HighRiseDefault As Boolean
    Public PrecisionLevel? As Integer '112041 From Precisely Geocode web service
    Public MatchConfidence As String '112041 From Precisely Geocode web service
    Public PBKey As String '112041 From Precisely Geocode web service
    Public PreciselyGeocodeDataID As Guid '112041 From PreciselyGeocodeData table row insert


    Public Sub New()
        MyBase.New()
    End Sub

    Public Shared Operator =(x As AddressDetail, y As AddressDetail) As Boolean
        Return x.ToString.Equals(y.ToString, StringComparison.OrdinalIgnoreCase)
    End Operator

    Public Shared Operator <>(x As AddressDetail, y As AddressDetail) As Boolean
        Return Not (x = y)
    End Operator

    Public Overrides Function ToString() As String
        Return (Me.Fulladdress1 + "|" + Me.Fulladdress2)
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (Me.Fulladdress1 + "|" + Me.Fulladdress2).GetHashCode
    End Function

    Public Function ShallowCopy() As AddressDetail
        Return DirectCast(Me.MemberwiseClone(), AddressDetail)
    End Function
End Class
