Imports System.Web.Script.Serialization
Imports ServicioFis


' NOTE: You can use the "Rename" command on the context menu to change the class name "EvalCandidato" in code, svc and config file together.
Public Class EvalCandidato
    Implements IEvalCandidato

    Function EvaluarCandidato(ByVal perfil As String, ByVal param As String) As String Implements IEvalCandidato.EvaluarCandidato
        Dim ML As New MLApp.MLApp
        Dim res, dir As String
        Dim resDob As Double

        dir = My.Computer.FileSystem.SpecialDirectories.MyDocuments
        res = ML.Execute("fis = readfis('" + dir + "\" + perfil + ".fis');")
        param = param.Replace("_", " ")
        param = param.Replace("-", ".")

        res = ML.Execute("re = evalfis([" + param + "],fis)")
        res = ML.GetVariable("re", "base")

        Double.TryParse(res, resDob) '(New JavaScriptSerializer().Serialize(res))
        Return resDob
    End Function

    Private Function DefPerfilCargo(p As IEvalCandidato.cPerfil) As String Implements IEvalCandidato.definirPerfil
        Dim ML As New MLApp.MLApp
        Dim res, aux, map, tod_indisp_optimo As String
        Dim i, j, d, contDeseables, conSalida As Integer
        Dim min, max, ini, mid, fin, desp As Double
        i = 1
        res = ML.Execute("fis = newfis('" + p.nomPerfil + " ');")

        'Generar Variables de Entrada
        For Each c In p.carIndispensables()
            res = ML.Execute("fis = addvar(fis,'input','" + c.caracteristicaNecesaria_label + "',[1 10]);")
            res = ML.Execute("fis = addmf(fis,'input'," + i.ToString() + ",'Insufieciente','gaussmf',[3 1]);")
            res = ML.Execute("fis = addmf(fis,'input'," + i.ToString() + ",'Optimo','gaussmf',[3 10]);")
            i += 1
        Next
        For Each c In p.carDeseabes()
            res = ML.Execute("fis = addvar(fis,'input','" + c.caracteristicaDeseable_label + "',[1 10]);")
            res = ML.Execute("fis = addmf(fis,'input'," + i.ToString() + ",'Insufieciente','gaussmf',[3 1]);")
            res = ML.Execute("fis = addmf(fis,'input'," + i.ToString() + ",'Optimo','gaussmf',[3 10]);")
            i += 1
        Next

        'Establecer rango variable de salida
        min = 0.0
        max = 11.0
        ini = 2.5
        mid = 5.5
        fin = 8.5
        'Generar Variable de Salida
        res = ML.Execute("fis = addvar(fis,'output','" + "Recomendacion" + "',[" + min.ToString + " " + max.ToString + "]);")
        res = ML.Execute("fis = addmf(fis,'output',1,'Insuficiente','trimf',[" + min.ToString + " " + min.ToString + " " + mid.ToString + "]);")
        res = ML.Execute("fis = addmf(fis,'output',1,'Suficiente','trimf', [" + ini.ToString + " " + mid.ToString + " " + fin.ToString + "]);")
        d = p.carDeseabes.Count()

        If d > 1 Then
            desp = (max - mid) / d
            For j = 1 To d - 1
                ini += desp
                mid += desp
                fin += desp
                res = ML.Execute("fis = addmf(fis,'output',1,'Adecuado" + j.ToString + "','trimf', [" + ini.ToString + " " + mid.ToString + " " + fin.ToString + "]);")
            Next
        End If

        ini = max - 3
        mid = max
        fin = max + 3
        res = ML.Execute("fis = addmf(fis,'output',1,'Optimo','trimf', [" + ini.ToString + " " + mid.ToString + " " + fin.ToString + "]);")
        'res = ML.Execute("fis = addmf(fis,'output',1,'Optimo','trimf', [8.75 " + max.ToString + " 16.99]);")

        'Generar Reglas
        'Regla 1: IF NOT car_ind_1=Optimo OR NOT car_ind_2=Optimo ... OR NOT car_ind_n=Optimo THEN Salida = Insuficiente 
        aux = ""
        For j = 1 To p.carIndispensables.Count()
            aux += "-2 " 'car_indispensable_j Not Optimo
        Next
        For j = 1 To d ' d= p.carDeseables.Count()
            aux += "0 " 'no tomar en cuenta car_deseable_j
        Next
        aux = Trim(aux)
        res = ML.Execute("fis.rule(1).antecedent = [" + aux + "];") 'Antecedente - Conjuntos de Variables de Entrada
        res = ML.Execute("fis.rule(1).consequent = 1;")     'Consecuente - Salida = Insuficiente
        res = ML.Execute("fis.rule(1).weight = 1;")         'Peso = 1
        res = ML.Execute("fis.rule(1).connection = 2;")     'Operador = 2 = OR

        ''Regla 2: IF car_ind_1=Optimo AND car_ind_2=Optimo ... AND car_ind_n=Optimo AND NOT car_des1=Optimo ... THEN Salida = Suficiente 
        'aux = ""
        'tod_indisp_optimo = ""
        'For j = 1 To p.carIndispensables.Count()
        '    tod_indisp_optimo += "2 " 'car_indispensable_j Optimo
        'Next

        'For j = 1 To d ' d= p.carDeseables.Count()
        '    aux += "-2 " 'car_deseable_j Not Optimo
        'Next
        'aux = Trim(tod_indisp_optimo + aux)
        'res = ML.Execute("fis.rule(2).antecedent = [" + aux + "];") 'Antecedente - Conjuntos de Variables de Entrada
        'res = ML.Execute("fis.rule(2).consequent = 2;")     'Consecuente - Salida = Suficiente
        'res = ML.Execute("fis.rule(2).weight = 1;")         'Peso = 1
        'res = ML.Execute("fis.rule(2).connection = 1;")     'Operador = 1 = AND

        ''Regla 3: IF car_ind_1=Optimo AND car_ind_2=Optimo ... AND car_ind_n=Optimo AND car_des1=Optimo ... THEN Salida = Optimo 
        'aux = ""
        'For j = 1 To d ' d=p.carDeseabes.Count()
        '    aux += "2 " 'car_deseable_j Optimo
        'Next
        'aux = Trim(tod_indisp_optimo + aux)
        'res = ML.Execute("fis.rule(3).antecedent = [" + aux + "];") 'Antecedente - Conjuntos de Variables de Entrada
        'res = ML.Execute("fis.rule(3).consequent = 3;")     'Consecuente - Salida = Optimo
        'res = ML.Execute("fis.rule(3).weight = 1;")         'Peso = 1
        'res = ML.Execute("fis.rule(3).connection = 1;")     'Operador = 1 = AND

        'Reglas carDeseables: IF car_ind_1=Optimo AND car_ind_2=Optimo ... AND car_ind_n=Optimo AND car_des1=Optimo ... THEN Salida = Adecuado_n 
        Dim numReglas As Decimal
        Dim bits() As Integer
        numReglas = 2 ^ d 'd=p.carDeseabes.Count()

        tod_indisp_optimo = ""
        For j = 1 To p.carIndispensables.Count()
            tod_indisp_optimo += "2 " 'car_indispensable_j Optimo
        Next

        For r As Decimal = 1 To numReglas
            bits = Decimal.GetBits(r - 1)
            map = Convert.ToString(bits(2), 2).PadLeft(32, "0"c)
            map += Convert.ToString(bits(1), 2).PadLeft(32, "0"c)
            map += Convert.ToString(bits(0), 2).PadLeft(32, "0"c)
            map = Right(map, d)
            contDeseables = 0
            aux = ""
            For j = 1 To d ' d=p.carDeseabes.Count()
                If map(j - 1) = "1" Then
                    aux += "2 " 'Var_entrada_j Optimo
                    contDeseables += 1
                Else
                    aux += "-2 " 'Var_entrada_j Optimo
                End If
            Next
            i = r + 1
            conSalida = contDeseables + 2
            aux = Trim(tod_indisp_optimo + aux)
            res = ML.Execute("fis.rule(" + i.ToString + ").antecedent = [" + aux + "];") 'Antecedente - Conjuntos de Variables de Entrada
            res = ML.Execute("fis.rule(" + i.ToString + ").consequent = " + conSalida.ToString + ";")     'Consecuente - Salida = Optimo
            res = ML.Execute("fis.rule(" + i.ToString + ").weight = 1;")         'Peso = 1
            res = ML.Execute("fis.rule(" + i.ToString + ").connection = 1;")     'Operador = 1 = AND
        Next
        aux = My.Computer.FileSystem.SpecialDirectories.MyDocuments
        'Grabar perfil a disco
        res = ML.Execute("fis = writefis(fis, '" + aux + "\" + p.nomPerfil + ".fis');")
        If res <> "" Then
            res = ML.Execute("fis = writefis(fis, '" + aux + "\" + p.nomPerfil + ".fis', 'dialog');")
        End If

        Return "OK"
        'Return (New JavaScriptSerializer().Serialize(res))
    End Function

    Private Function DefPerfilCargoPesos(p As IEvalCandidato.cPerfilPesos) As String Implements IEvalCandidato.definirPerfilPesos
        Dim ML As New MLApp.MLApp
        Dim res, aux, map, tod_indisp_optimo As String
        Dim i, j, d, contDeseables, conSalida As Integer
        Dim min, max, ini, mid, fin, desp As Double
        i = 1
        res = ML.Execute("fis = newfis('" + p.nomPerfil + " ');")

        'Generar Variables de Entrada
        For Each c In p.carIndispensables()
            res = ML.Execute("fis = addvar(fis,'input','" + c.caracteristicaNecesaria_label + "',[1 10]);")
            res = ML.Execute("fis = addmf(fis,'input'," + i.ToString() + ",'Insufieciente','gaussmf',[3 1]);")
            res = ML.Execute("fis = addmf(fis,'input'," + i.ToString() + ",'Optimo','gaussmf',[3 10]);")
            i += 1
        Next
        For Each c In p.carDeseabes()
            res = ML.Execute("fis = addvar(fis,'input','" + c.caracteristicaDeseable_label + "',[1 10]);")
            res = ML.Execute("fis = addmf(fis,'input'," + i.ToString() + ",'Insufieciente','gaussmf',[3 1]);")
            res = ML.Execute("fis = addmf(fis,'input'," + i.ToString() + ",'Optimo','gaussmf',[3 10]);")
            i += 1
        Next

        'Establecer rango variable de salida
        min = 0.0
        max = 11.0
        ini = 2.5
        mid = 5.5
        fin = 8.5
        'Generar Variable de Salida
        res = ML.Execute("fis = addvar(fis,'output','" + "Recomendacion" + "',[" + min.ToString + " " + max.ToString + "]);")
        res = ML.Execute("fis = addmf(fis,'output',1,'Insuficiente','trimf',[" + min.ToString + " " + min.ToString + " " + mid.ToString + "]);")
        res = ML.Execute("fis = addmf(fis,'output',1,'Suficiente','trimf', [" + ini.ToString + " " + mid.ToString + " " + fin.ToString + "]);")
        d = p.carDeseabes.Count()

        If d > 1 Then
            desp = (max - mid) / d
            For j = 1 To d - 1
                ini += desp
                mid += desp
                fin += desp
                res = ML.Execute("fis = addmf(fis,'output',1,'Adecuado" + j.ToString + "','trimf', [" + ini.ToString + " " + mid.ToString + " " + fin.ToString + "]);")
            Next
        End If

        ini = max - 3
        mid = max
        fin = max + 3
        res = ML.Execute("fis = addmf(fis,'output',1,'Optimo','trimf', [" + ini.ToString + " " + mid.ToString + " " + fin.ToString + "]);")
        'res = ML.Execute("fis = addmf(fis,'output',1,'Optimo','trimf', [8.75 " + max.ToString + " 16.99]);")

        'Generar Reglas
        'Regla 1: IF NOT car_ind_1=Optimo OR NOT car_ind_2=Optimo ... OR NOT car_ind_n=Optimo THEN Salida = Insuficiente 
        aux = ""
        For j = 1 To p.carIndispensables.Count()
            aux += "-2 " 'car_indispensable_j Not Optimo
        Next
        For j = 1 To d ' d= p.carDeseables.Count()
            aux += "0 " 'no tomar en cuenta car_deseable_j
        Next
        aux = Trim(aux)
        res = ML.Execute("fis.rule(1).antecedent = [" + aux + "];") 'Antecedente - Conjuntos de Variables de Entrada
        res = ML.Execute("fis.rule(1).consequent = 1;")     'Consecuente - Salida = Insuficiente
        res = ML.Execute("fis.rule(1).weight = 1;")         'Peso = 1
        res = ML.Execute("fis.rule(1).connection = 2;")     'Operador = 2 = OR

        ''Regla 2: IF car_ind_1=Optimo AND car_ind_2=Optimo ... AND car_ind_n=Optimo AND NOT car_des1=Optimo ... THEN Salida = Suficiente 
        'aux = ""
        'tod_indisp_optimo = ""
        'For j = 1 To p.carIndispensables.Count()
        '    tod_indisp_optimo += "2 " 'car_indispensable_j Optimo
        'Next

        'For j = 1 To d ' d= p.carDeseables.Count()
        '    aux += "-2 " 'car_deseable_j Not Optimo
        'Next
        'aux = Trim(tod_indisp_optimo + aux)
        'res = ML.Execute("fis.rule(2).antecedent = [" + aux + "];") 'Antecedente - Conjuntos de Variables de Entrada
        'res = ML.Execute("fis.rule(2).consequent = 2;")     'Consecuente - Salida = Suficiente
        'res = ML.Execute("fis.rule(2).weight = 1;")         'Peso = 1
        'res = ML.Execute("fis.rule(2).connection = 1;")     'Operador = 1 = AND

        ''Regla 3: IF car_ind_1=Optimo AND car_ind_2=Optimo ... AND car_ind_n=Optimo AND car_des1=Optimo ... THEN Salida = Optimo 
        'aux = ""
        'For j = 1 To d ' d=p.carDeseabes.Count()
        '    aux += "2 " 'car_deseable_j Optimo
        'Next
        'aux = Trim(tod_indisp_optimo + aux)
        'res = ML.Execute("fis.rule(3).antecedent = [" + aux + "];") 'Antecedente - Conjuntos de Variables de Entrada
        'res = ML.Execute("fis.rule(3).consequent = 3;")     'Consecuente - Salida = Optimo
        'res = ML.Execute("fis.rule(3).weight = 1;")         'Peso = 1
        'res = ML.Execute("fis.rule(3).connection = 1;")     'Operador = 1 = AND

        'Reglas carDeseables: IF car_ind_1=Optimo AND car_ind_2=Optimo ... AND car_ind_n=Optimo AND car_des1=Optimo ... THEN Salida = Adecuado_n 
        Dim numReglas As Decimal
        Dim bits() As Integer
        numReglas = 2 ^ d 'd=p.carDeseabes.Count()

        tod_indisp_optimo = ""
        For j = 1 To p.carIndispensables.Count()
            tod_indisp_optimo += "2 " 'car_indispensable_j Optimo
        Next

        For r As Decimal = 1 To numReglas
            bits = Decimal.GetBits(r - 1)
            map = Convert.ToString(bits(2), 2).PadLeft(32, "0"c)
            map += Convert.ToString(bits(1), 2).PadLeft(32, "0"c)
            map += Convert.ToString(bits(0), 2).PadLeft(32, "0"c)
            map = Right(map, d)
            contDeseables = 0
            aux = ""
            For j = 1 To d ' d=p.carDeseabes.Count()
                If map(j - 1) = "1" Then
                    aux += "2 " 'Var_entrada_j Optimo
                    contDeseables += 1
                Else
                    aux += "-2 " 'Var_entrada_j Optimo
                End If
            Next
            i = r + 1
            conSalida = contDeseables + 2
            aux = Trim(tod_indisp_optimo + aux)
            res = ML.Execute("fis.rule(" + i.ToString + ").antecedent = [" + aux + "];") 'Antecedente - Conjuntos de Variables de Entrada
            res = ML.Execute("fis.rule(" + i.ToString + ").consequent = " + conSalida.ToString + ";")     'Consecuente - Salida = Optimo
            res = ML.Execute("fis.rule(" + i.ToString + ").weight = 1;")         'Peso = 1
            res = ML.Execute("fis.rule(" + i.ToString + ").connection = 1;")     'Operador = 1 = AND
        Next
        aux = My.Computer.FileSystem.SpecialDirectories.MyDocuments
        'Grabar perfil a disco
        res = ML.Execute("fis = writefis(fis, '" + aux + "\" + p.nomPerfil + ".fis');")
        If res <> "" Then
            res = ML.Execute("fis = writefis(fis, '" + aux + "\" + p.nomPerfil + ".fis', 'dialog');")
        End If

        Return "OK"
        'Return (New JavaScriptSerializer().Serialize(res))
    End Function
End Class
