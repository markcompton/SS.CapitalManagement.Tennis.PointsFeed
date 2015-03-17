Imports System.Collections.Generic
Imports System.ComponentModel.Composition
Imports log4net
Imports SS.Integration.Adapter.Model.Interfaces
Imports SS.Integration.Adapter.Model
Imports SS.Integration.Adapter.MarketRules
Imports Microsoft.Owin.Hosting
Imports System.Configuration

<Export(GetType(IAdapterPlugin))> _
Public Class PropPlugin : Implements IAdapterPlugin

#Region " Constatns "
    Private Const PendingFixturesTable As String = "[Tennis].[dbo].[tblPendingFixtures]"
    Private Const PointByPointTable As String = "[Tennis].[dbo].[tblInPlayPointByPoint]"
#End Region

#Region " Public "

    Public Sub New()

        'Load local config
        _config = ModuleConfigurationProvider.GetModuleConfiguration("SPIN.PropTrading.Tennis.PointsFeed.config")
        Log = LogManager.GetLogger(GetType(PropPlugin).ToString)
        Call Log.Debug("Plugin constructed OK")

    End Sub

    Public Sub Dispose() Implements IAdapterPlugin.Dispose
        Call Log.Debug("Disposing Plugin")
    End Sub

    Public Sub Initialise() Implements IAdapterPlugin.Initialise

        Call Log.Debug("Initialising PropAdapter")
        _signalRRoute = _config.AppSettings.Settings.Item("SignalRRoute").Value
        _signalR = WebApp.Start(Of MyApplication.SignalRStartup)(_signalRRoute)
        Call InitialiseFromDb()
        Call InitialiseMarketRules(Sports.Tennis)
        Call InitialiseHub()

    End Sub

    Public Sub ProcessFixtureDeletion(fixture As Fixture) Implements IAdapterPlugin.ProcessFixtureDeletion
        Call Log.DebugFormat("PropAdapter deleting fixture {0}", fixture.FixtureName)
    End Sub

    Public Sub ProcessMatchStatus(fixture As Fixture) Implements IAdapterPlugin.ProcessMatchStatus
        Call Log.DebugFormat("PropAdapter processing match status for fixture {0}", fixture.FixtureName)
        Call UpdateFixture(fixture)
    End Sub

    Public Sub ProcessSnapshot(fixture As Fixture, Optional hasEpochChanged As Boolean = False) Implements IAdapterPlugin.ProcessSnapshot

        'SyncLock lock
        Call Log.DebugFormat("PropAdapter processing snapshot for fixture {0}. Epoch changed : {1}", fixture.FixtureName, hasEpochChanged.ToString)
        Call UpdateFixture(fixture)
        'End SyncLock

    End Sub

    Public Sub ProcessStreamUpdate(fixture As Fixture, Optional hasEpochChanged As Boolean = False) Implements IAdapterPlugin.ProcessStreamUpdate
        Call Log.DebugFormat("PropAdapter processing stream update for fixture {0}. Epoch changed : {1}", fixture.FixtureName, hasEpochChanged.ToString)
        Call UpdateFixture(fixture)
    End Sub

    Public Sub Suspend(fixtureId As String) Implements IAdapterPlugin.Suspend
        Call Log.DebugFormat("PropAdapter suspending fixtureid {0}", fixtureId)
    End Sub

    Public Sub UnSuspend(fixture As Fixture) Implements IAdapterPlugin.UnSuspend
        Call Log.DebugFormat("PropAdapter suspending fixtureid {0}", fixture.FixtureName)
        'Call UpdateFixture(fixture)
    End Sub

#End Region

#Region " Private "

    Private Sub InitialiseMarketRules(ByRef sport As String)

        Dim pendingMarketFilteringRule = New PendingMarketFilteringRule()

        pendingMarketFilteringRule.RemoveSportFromRule(Sports.AmericanFootball)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.AustralianRules)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.Badminton)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.Baseball)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.Basketball)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.Boxing)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.Cricket)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.Darts)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.Football)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.GaelicFootball)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.GaelicHurling)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.GreyhoundRacing)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.Handball)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.HorseRacing)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.IceHockey)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.RugbyLeague)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.RugbyUnion)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.Snooker)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.TennisDoubles)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.TestCricket)
        pendingMarketFilteringRule.RemoveSportFromRule(Sports.Volleyball)
        pendingMarketFilteringRule.AddSportToRule(sport)

        Call _marketRuleList.Add(pendingMarketFilteringRule)

        Call Log.Debug("Initialised market rules")

    End Sub

    Private Sub InitialiseFromDb()

        _pointsTable = _db.InitTable(PointByPointTable, True)

        Dim dt As DataTable = _db.GetTable("SELECT f.ConnectFixtureId, MAX(p.PointId) AS LastPointId, CAST(f.HomeSets AS VARCHAR) + CAST(f.HomeGames AS VARCHAR) + f.HomePoints + CAST(f.AwaySets AS VARCHAR) + CAST(f.AwayGames AS VARCHAR) + f.AwayPoints AS LastPointKey FROM " & PendingFixturesTable & " f INNER JOIN " & PointByPointTable & " p ON f.ConnectFixtureId = p.FixtureId WHERE f.GameState < 2 GROUP BY f.ConnectFixtureId, CAST(f.HomeSets AS VARCHAR) + CAST(f.HomeGames AS VARCHAR) + f.HomePoints + CAST(f.AwaySets AS VARCHAR) + CAST(f.AwayGames AS VARCHAR) + f.AwayPoints")
        For Each dr As DataRow In dt.Rows
            Call _pointIds.Add(dr(0), New LastPoint(dr(1), dr(2)))
        Next

        Call Log.Debug("Initialised from local DB")

    End Sub

    Private Sub InitialiseHub()
        'Needed??
        Call Log.Debug("Initialised SignalR hubs")

    End Sub

    Private Sub UpdateFixture(ByRef f As Fixture)

        SyncLock lock

            Try

                'Dim dr As DataRow = FetchFixture(fixture)
                'Call UpdateFixture(fixture, dr)

                Dim home As String, away As String
                Dim homePrice As Double, awayPrice As Double
                Call GetHomeAwayName(f.Participants, home, away)
                Call GetHomeAwayPrice(f.Markets, home, away, homePrice, awayPrice)
                If f.IsPreMatch Then

                    If Not String.IsNullOrEmpty(f.Tags.Item("TournamentName")) Then
                        If Not _pointIds.ContainsKey(f.Id) Then Call _pointIds.Add(f.Id, New LastPoint(0, Nothing))
                        Call _hub.SendFixtureUpdate(f.Id, home, away, 0, f.Tags.Item("TournamentName").ToString, f.StartTime)
                    End If

                ElseIf f.IsInPlay Then

                    Dim homeSets As Integer, awaySets As Integer, homeGames As Integer, awayGames As Integer, server As Integer
                    Dim homePoints As String, awayPoints As String

                    Call GetGameState(f, homeSets, homeGames, homePoints, awaySets, awayGames, awayPoints, server)
                    Call AddPoint(f.Id, homeSets, homeGames, homePoints, awaySets, awayGames, awayPoints, server, homePrice, awayPrice)

                    Call _hub.SendPriceUpdate(f.Id, home, away, 1, homeSets, homeGames, homePoints, awaySets, awayGames, awayPoints, server, homePrice, awayPrice, Now)

                ElseIf f.IsMatchOver Then
                    If Not String.IsNullOrEmpty(f.Tags.Item("TournamentName")) Then Call _hub.SendFixtureUpdate(f.Id, home, away, 2, f.Tags.Item("TournamentName").ToString, f.StartTime)
                Else : Throw New Exception("New fixture state")
                End If

            Catch x As Exception : Call Log.ErrorFormat("Error parsing game state {0}", x)
            End Try

        End SyncLock

    End Sub

    'Private Function FetchFixture(ByRef f As Fixture) As DataRow

    '    Dim dr As DataRow = _fixtureTable.Rows.Find(f.Id)

    '    If dr Is Nothing Then

    '        dr = _fixtureTable.NewRow
    '        If Not f.StartTime Is Nothing Then
    '            dr("FixtureDateTime") = CDate(f.StartTime).ToLocalTime
    '        Else : dr("FixtureDateTime") = Now 'Best guess
    '        End If
    '        dr("Competition") = f.Tags.Item("SSLNCompetitionName").ToString
    '        dr("ConnectFixtureId") = f.Id
    '        dr("FixtureName") = f.FixtureName

    '        Dim home As String, away As String
    '        Call GetHomeAwayName(f.Participants, home, away)

    '        dr("GameState") = 0
    '        _fixtureTable.Rows.Add(dr)

    '    End If

    '    Return dr

    'End Function

    Private Sub GetHomeAwayName(ByRef participants As List(Of Participant), ByRef home As String, ByRef away As String)

        home = "Unknown"
        away = "Unknown"

        For Each participant As Participant In participants
            Select Case participant.Tags.Item("HomeOrAway")
                Case "Home" : home = participant.Name
                Case "Away" : away = participant.Name
                Case Else : Throw New Exception("New participant type " & participant.Tags.Item("HomeOrAway"))
            End Select
        Next

    End Sub

    Private Sub GetHomeAwayPrice(ByRef markets As List(Of Market), ByRef home As String, ByRef away As String, ByRef homePrice As Double, ByRef awayPrice As Double)

        For Each market As Market In markets
            If market.Name = "Match Winner" Then
                For Each selection As Selection In market.Selections
                    Select Case selection.Name
                        Case home : homePrice = selection.Price
                        Case away : awayPrice = selection.Price
                        Case Else : Throw New Exception("Selection is not home or away " & selection.Name)
                    End Select
                Next
                Exit For
            End If
        Next

    End Sub

    Private Sub GetGameState(ByRef f As Fixture, ByRef homeSets As Integer, ByRef homeGames As Integer, ByRef homePoints As String, ByRef awaySets As Integer, ByRef awayGames As Integer, ByRef awayPoints As String, ByRef server As Integer)

        Dim currentSet As Integer = f.GameState("currentset")
        Dim currentGame As Integer = f.GameState("currentsetgame")
        Dim s As String, s1() As String

        For i As Integer = 1 To currentSet - 1
            s = f.GameState("set" & i & "summary").split(" ")(0)
            s1 = s.Split("-")
            If s1(0) > s1(1) Then
                homeSets += 1
            Else : awaySets += 1
            End If
        Next

        s = f.GameState("set" & currentSet & "summary").split(" ")(0)
        s1 = s.Split("-")

        homeGames = s1(0)
        awayGames = s1(1)
        homePoints = f.GameState("set" & currentSet & "game" & currentGame & "player1score")
        awayPoints = f.GameState("set" & currentSet & "game" & currentGame & "player2score")

        If f.GameState.ContainsKey("set" & currentSet & "game" & currentGame & "server") Then
            Select Case f.GameState("set" & currentSet & "game" & currentGame & "server")
                Case "Player1" : server = 1
                Case "Player2" : server = 2
                Case Else : server = 0
            End Select
        Else : server = 0
        End If

    End Sub

    Private Sub AddPoint(ByRef id As String, ByRef homeSets As Integer, ByRef homeGames As Integer, ByRef homePoints As String, ByRef awaySets As Integer, ByRef awayGames As Integer, ByRef awayPoints As String, ByRef server As Integer, ByRef homePrice As Double, ByRef awayPrice As Double)

        Dim pointId As Integer
        If _pointIds.ContainsKey(id) Then
            pointId = _pointIds(id).lastPointId
        Else : _pointIds.Add(id, New LastPoint(0, Nothing))
        End If

        Dim pointKey As String = homeSets & homeGames & homePoints & awaySets & awayGames & awayPoints

        If _pointIds(id).lastPointKey <> pointKey Then

            pointId += 1
            _pointIds(id).lastPointId = pointId
            _pointIds(id).lastPointKey = pointKey

            Dim drPoint As DataRow = _pointsTable.NewRow
            drPoint("FixtureId") = id
            drPoint("PointId") = pointId
            drPoint("HomeSets") = homeSets
            drPoint("HomeGames") = homeGames
            drPoint("HomePoints") = homePoints
            drPoint("AwaySets") = awaySets
            drPoint("AwayGames") = awayGames
            drPoint("AwayPoints") = awayPoints
            drPoint("Serving") = server
            drPoint("ConnectHomeMatchPrice") = homePrice
            drPoint("ConnectAwayMatchPrice") = awayPrice

            Call _pointsTable.Rows.Add(drPoint)
            Call _db.UpdateDataTable(_pointsTable, PointByPointTable)

        End If

    End Sub

    Private Function PrintGameState(ByRef d As Dictionary(Of String, Object)) As String

        Dim s As New Text.StringBuilder()
        For i As Integer = 0 To d.Count - 1
            Call s.AppendLine(d.Keys(i) & " : " & d.Item(d.Keys(i)))
        Next

        Return s.ToString

    End Function

#End Region

#Region " Properties "

    Public ReadOnly Property MarketRules As IEnumerable(Of IMarketRule) Implements IAdapterPlugin.MarketRules
        Get
            Return _marketRuleList
        End Get
    End Property

#End Region

#Region " Variables "

    Private Shared Log As ILog
    Private _marketRuleList = New List(Of IMarketRule)
    Private _db As New Db()
    'Private _fixtureTable As DataTable
    Private _pointsTable As DataTable
    Private Shared lock As New Object()
    Private _pointIds As New Dictionary(Of String, LastPoint)

    Private _hub As New PropDataHub()
    Private _signalR
    Private _signalRRoute As String
    Private _config As Configuration

    Private Class LastPoint
        Public Sub New(ByRef pointId As Integer, pointKey As String)
            lastPointId = pointId : lastPointKey = pointKey
        End Sub
        Public lastPointId As Integer
        Public lastPointKey As String
    End Class

#End Region

End Class