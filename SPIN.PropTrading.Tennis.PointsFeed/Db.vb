Imports SS.Integration.Adapter.Model
Imports System.Data.SqlClient
Imports System.Configuration
Imports log4net

Public Class Db

#Region " Constants "

#End Region

#Region " Public "

    Public Sub New()

        Call ModuleConfigurationProvider.GetModuleConfiguration("SPIN.PropTrading.ConnectPlugin.config")
        Call Initialise()

    End Sub

    Public Function GetTable(ByRef sql As String) As DataTable
        Return GetTable(sql, True)
    End Function

    Public Function GetTable(ByRef sql As String, ByRef enforceConstraints As Boolean) As DataTable

        Dim sqlCmd As SqlCommand = GetSqlCmd(sql)

        Dim sqlReader As SqlDataReader
        If enforceConstraints Then
            sqlReader = sqlCmd.ExecuteReader(CommandBehavior.KeyInfo)
        Else : sqlReader = sqlCmd.ExecuteReader()
        End If

        Dim dt As DataTable = New DataTable()

        Call dt.Load(sqlReader)

        Call CleanConnection(sqlCmd, sqlReader)

        Return dt

    End Function

    Public Function InitTable(ByRef tableName As String, ByRef withData As Boolean) As DataTable
        Return InitTable(tableName, withData, Nothing)
    End Function

    Public Function InitTable(ByRef tableName As String, ByRef withData As Boolean, ByRef orderBy As String) As DataTable

        Dim sql As String = "SELECT "
        If Not withData Then sql &= "TOP 0 "
        sql &= "* FROM " & tableName
        If Not orderBy Is Nothing Then sql &= " ORDER BY " & orderBy

        Return GetTable(sql)

    End Function

    Public Function UpdateDataTable(ByRef dt As DataTable, ByRef tableName As String) As Boolean

        Dim updated As Boolean
        Dim da As SqlDataAdapter
        Dim cmdBuilder As SqlCommandBuilder
        Dim sql As String

        sql = "SELECT * FROM " & tableName
        Try
            OpenConnection()
            da = New SqlDataAdapter(sql, _conn)
            cmdBuilder = New SqlCommandBuilder(da)
            Call da.Update(dt)
            updated = True
        Catch ex As Exception
            Call Log.ErrorFormat("DB error updating table {0} : {1}", tableName, ex.ToString)
            updated = False
        End Try

        Call CloseConnection()

        Return updated

    End Function

    Public Function UpdateDataTable(ByRef dt As DataTable, ByRef tableName As String, ByRef rowState As DataViewRowState) As Boolean

        Dim updated As Boolean
        Dim da As SqlDataAdapter
        Dim cmdBuilder As SqlCommandBuilder
        Dim sql As String

        sql = "SELECT * FROM " & tableName
        Try
            OpenConnection()
            da = New SqlDataAdapter(sql, _conn)
            cmdBuilder = New SqlCommandBuilder(da)
            Dim rows() As DataRow = dt.Select(Nothing, Nothing, rowState)
            Call da.Update(rows)
            updated = True
        Catch ex As Exception
            Call Log.ErrorFormat("DB error updating table {0} : {1}", tableName, ex.ToString)
            updated = False
        End Try

        Call CloseConnection()

        Return updated

    End Function

#End Region

#Region " Private Stuff "

    Private Sub Initialise()
        'oUCH - HARD CODED 
        _conn = New SqlConnection("Data Source=Db9004-mscl1vs.oy.gb.sportingindex.com\instancea,59456;Initial Catalog=Trading;User ID=PropTrader;Password=sporting;")
    End Sub

    Private Sub OpenConnection()

        If _conn.State = ConnectionState.Closed Then
            Call _conn.Open()
        Else
            Call Log.Error("Error establishing a connection to DB. Connection is already open.")
            Throw New Exception("Error establishing a connection to DB. Connection is already open")
        End If

    End Sub

    Private Sub CloseConnection()
        Try
            Call _conn.Close()
        Catch x As Exception
            Call Log.ErrorFormat("Error when closing DB connection : {0}", x.ToString)
        End Try
    End Sub

    Private Sub CleanConnection(ByRef sqlCmd As SqlCommand, ByRef sqlReader As SqlDataReader)

        Call sqlReader.Close()
        Call sqlCmd.Dispose()
        sqlReader = Nothing
        sqlCmd = Nothing
        Call CloseConnection()

    End Sub

    Private Function GetSqlCmd(ByRef sql As String) As SqlCommand

        'Get
        Dim sqlCmd As New SqlCommand(sql)
        Call OpenConnection()
        sqlCmd.Connection = _conn
        sqlCmd.CommandTimeout = 360

        'Give
        Return sqlCmd

    End Function

#End Region

#Region " Variables "
    
    Private Shared ReadOnly Log As ILog = LogManager.GetLogger(GetType(Db).ToString)
    Private _conn As SqlConnection

#End Region

End Class
