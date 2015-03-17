Imports Microsoft.AspNet.SignalR
Imports Newtonsoft.Json

Public Class PropDataHub : Inherits Hub

    Public Sub SendPriceUpdate(ByRef connectId As String, _
                               ByRef home As String, _
                               ByRef away As String, _
                               ByRef gameState As Integer, _
                               ByRef homeSets As Integer, _
                               ByRef homeGames As Integer, _
                               ByRef homePoints As String, _
                               ByRef awaySets As Integer, _
                               ByRef awayGames As Integer, _
                               ByRef awayPoints As String, _
                               ByRef serving As Integer, _
                               ByRef homePrice As Double, _
                               ByRef awayPrice As Double, _
                               ByRef timestamp As Date)

        Dim t As New TennisPointUpdate
        t.ConnectId = connectId
        t.GameState = gameState
        t.HomeSets = homeSets
        t.HomeGames = homeGames
        t.HomePoints = homePoints
        t.AwaySets = awaySets
        t.AwayGames = awayGames
        t.AwayPoints = awayPoints
        t.Serving = serving
        t.HomePrice = homePrice
        t.AwayPrice = awayPrice
        t.Timestamp = timestamp
        Dim s As String = JsonConvert.SerializeObject(t)
        Dim context = GlobalHost.ConnectionManager.GetHubContext(Of PropDataHub)()
        Call context.Clients.All.PointUpdate(s)

    End Sub

    Public Sub SendFixtureUpdate(ByRef connectId As String, _
                                 ByRef home As String, _
                                 ByRef away As String, _
                                 ByRef gameState As Integer, _
                                 ByRef tournament As String, _
                                 ByRef startTime As Date)

        Dim t As New TennisFixtureUpdate
        t.ConnectId = connectId
        t.Tournament = tournament
        t.StartDate = startTime
        t.Home = home
        t.Away = away
        t.GameState = gameState
        Dim s As String = JsonConvert.SerializeObject(t)
        Dim context = GlobalHost.ConnectionManager.GetHubContext(Of PropDataHub)()
        Call context.Clients.All.FixtureUpdate(s)

    End Sub


    Private Class TennisPointUpdate
        Public ConnectId As String
        Public GameState As Integer
        Public HomeSets As Integer
        Public HomeGames As Integer
        Public HomePoints As String
        Public AwaySets As Integer
        Public AwayGames As Integer
        Public AwayPoints As String
        Public Serving As Integer
        Public HomePrice As Double
        Public AwayPrice As Double
        Public Timestamp As Date
    End Class

    Private Class TennisFixtureUpdate
        Public ConnectId As String
        Public Tournament As String
        Public StartDate As Date
        Public Home As String
        Public Away As String
        Public GameState As Integer
    End Class

End Class
