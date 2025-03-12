Imports WSAddressScrubber.MessageConstants

Friend Class ScrubberMessage
    Public MessageType As TMessageType
    Public DetailedMessage As String
    Public FriendlyMessage As String

    Public Sub New(MessageType As TMessageType, DetailedMessage As String, FriendlyMessage As String)
        Me.MessageType = MessageType
        Me.DetailedMessage = DetailedMessage
        Me.FriendlyMessage = FriendlyMessage
    End Sub
End Class