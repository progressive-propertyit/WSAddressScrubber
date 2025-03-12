Imports System.Collections.Generic

Namespace Models.PreciselyModels

    '167310 Data retrieval from PreciselyGeoCodeData table for 30days cache
    Public Class Precisely30DaysCache
        Public Property Address As String
        Public Property City As String
        Public Property State As String
        Public Property Zipcode As String
        Public Property PrecisionLevel As Integer?
        Public Property MatchConfidence As String
        Public Property PBKey As String
        Public Property PreciselyGeocodeDataID As Guid
        Public Property County As String
        Public Property Latitude As Double
        Public Property Longitude As Double
        Public Property Countyfips As String
    End Class

End Namespace
