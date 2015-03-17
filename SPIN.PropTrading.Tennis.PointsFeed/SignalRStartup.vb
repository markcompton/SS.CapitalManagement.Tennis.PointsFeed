Imports Microsoft.Owin
Imports Owin

<Assembly: OwinStartup(GetType(MyApplication.SignalRStartup))> 
Namespace MyApplication
    Public Class SignalRStartup
        Public Sub Configuration(app As IAppBuilder)
            ' Any connection or hub wire up and configuration should go here
            app.MapSignalR()
        End Sub
    End Class
End Namespace