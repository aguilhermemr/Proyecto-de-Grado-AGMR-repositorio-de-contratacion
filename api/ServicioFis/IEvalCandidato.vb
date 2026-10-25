Imports System.ServiceModel

' NOTE: You can use the "Rename" command on the context menu to change the interface name "IEvalCandidato" in both code and config file together.
<ServiceContract()>
Public Interface IEvalCandidato

    Class cIndispensable
        Public Property caracteristicaNecesaria As String
        Public Property caracteristicaNecesaria_label As String
    End Class
    Class cDeseable
        Public Property caracteristicaDeseable As String
        Public Property caracteristicaDeseable_label As String
    End Class
    Class cDeseablePesos
        Public Property caracteristicaDeseable As String
        Public Property caracteristicaDeseable_label As String
        Public Property Peso As String
        Public Property Peso_label As String
    End Class

    Class cPerfil
        Public Property nomPerfil As String
        Public Property carIndispensables As cIndispensable()
        Public Property carDeseabes As cDeseable()
    End Class
    Class cPerfilPesos
        Public Property nomPerfil As String
        Public Property carIndispensables As cIndispensable()
        Public Property carDeseabes As cDeseablePesos()
    End Class

    <OperationContract()>
    <System.ServiceModel.Web.WebInvoke(Method:="GET",
    ResponseFormat:=System.ServiceModel.Web.WebMessageFormat.Json,
    UriTemplate:="api/{perfil}/{param}")>
    Function EvaluarCandidato(ByVal perfil As String, ByVal param As String) As String

    <OperationContract()>
    <WebInvoke(Method:="POST",
    RequestFormat:=System.ServiceModel.Web.WebMessageFormat.Json,
    ResponseFormat:=System.ServiceModel.Web.WebMessageFormat.Json,
    BodyStyle:=WebMessageBodyStyle.Bare,
    UriTemplate:="api/perfilcargo")>
    Function definirPerfil(p As cPerfil) As String

    <OperationContract()>
    <WebInvoke(Method:="POST",
    RequestFormat:=System.ServiceModel.Web.WebMessageFormat.Json,
    ResponseFormat:=System.ServiceModel.Web.WebMessageFormat.Json,
    BodyStyle:=WebMessageBodyStyle.Bare,
    UriTemplate:="api/perfilcargopesos")>
    Function definirPerfilPesos(p As cPerfilPesos) As String

End Interface
