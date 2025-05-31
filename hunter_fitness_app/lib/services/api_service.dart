import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class ApiService {
  static const String _baseUrl = "https://hunter-fitness-api.azurewebsites.net/api";
  final _storage = const FlutterSecureStorage();

  Future<Map<String, dynamic>> _handleApiResponse(http.Response response, String operation) async {
    print('--- INICIO _handleApiResponse para: $operation ---');
    print('$operation API Status Code: ${response.statusCode}');
    final String rawResponseBody = response.body;
    print('$operation API Response Body (RAW): "$rawResponseBody"');

    if (rawResponseBody.isEmpty) {
      print('$operation API Error: Cuerpo de respuesta vacío.');
      return {'success': false, 'message': 'El servidor devolvió una respuesta vacía.'};
    }

    Map<String, dynamic> outerResponseData;
    try {
      var decodedJson = jsonDecode(rawResponseBody);
      if (decodedJson is Map<String, dynamic>) {
        outerResponseData = decodedJson;
        print('$operation - jsonDecode exitoso (nivel exterior). outerResponseData es un Map.');
      } else {
        print('$operation API Error: El JSON decodificado (nivel exterior) no es un Mapa. Tipo actual: ${decodedJson.runtimeType}');
        return {'success': false, 'message': 'Respuesta del servidor con formato inesperado (nivel exterior no es un objeto JSON).'};
      }
    } catch (e) {
      print('$operation API Error: Fallo al decodificar JSON (nivel exterior). Error: $e. Cuerpo: "$rawResponseBody"');
      return {'success': false, 'message': 'Respuesta inesperada del servidor (formato incorrecto exterior).'};
    }

    print('$operation - DEBUG: Listando todas las claves y valores en outerResponseData:');
    outerResponseData.forEach((key, value) {
      print('$operation - DEBUG: Key: "$key" | Value: "$value" | RuntimeType: ${value.runtimeType}');
    });

    bool apiOuterSuccess = false;
    String apiOuterMessage = 'Mensaje no proporcionado por la API (predeterminado).';

    if (outerResponseData.containsKey('Success')) {
      var successValue = outerResponseData['Success'];
      print('$operation - DEBUG: Clave "Success" (PascalCase) ENCONTRADA. Valor: $successValue, Tipo: ${successValue.runtimeType}');
      if (successValue is bool) {
        apiOuterSuccess = successValue;
      } else if (successValue is String && successValue.toLowerCase() == 'true') {
        apiOuterSuccess = true;
      }
    } else {
      print('$operation - DEBUG CRITICAL: Clave "Success" (PascalCase) NO ENCONTRADA en outerResponseData. Keys: ${outerResponseData.keys.toList()}');
    }

    if (outerResponseData.containsKey('Message')) {
      var messageValue = outerResponseData['Message'];
      print('$operation - DEBUG: Clave "Message" (PascalCase) ENCONTRADA. Valor: "$messageValue", Tipo: ${messageValue.runtimeType}');
      if (messageValue is String) {
        apiOuterMessage = messageValue;
      } else if (messageValue != null) {
        apiOuterMessage = messageValue.toString();
      }
    } else {
      print('$operation - DEBUG WARNING: Clave "Message" (PascalCase) NO ENCONTRADA en outerResponseData.');
    }

    try {
      bool httpSuccess = (response.statusCode == 200 || response.statusCode == 201);

      print('$operation - httpSuccess: $httpSuccess (StatusCode: ${response.statusCode})');
      print('$operation - apiOuterSuccess (determinado): $apiOuterSuccess');
      print('$operation - apiOuterMessage (determinado): "$apiOuterMessage"');

      if (!httpSuccess) {
        print('$operation - Fallo HTTP. Mensaje: "$apiOuterMessage". Error de comunicación (código: ${response.statusCode}).');
        return {'success': false, 'message': '$apiOuterMessage (Error HTTP: ${response.statusCode})'};
      }
      
      // Si la API externa (wrapper) indica que no fue exitoso, entonces es un fallo.
      if (!apiOuterSuccess) {
          print('$operation - Fallo declarado por API externa (apiOuterSuccess es false). Mensaje: "$apiOuterMessage"');
          return {'success': false, 'message': apiOuterMessage};
      }

      // Si llegamos aquí, httpSuccess ES true Y apiOuterSuccess ES true.
      // Esto se considera un ÉXITO para la UI.
      // Ahora intentamos extraer datos adicionales del campo 'Data'.

      var dataField = outerResponseData['Data'];
      Map<String, dynamic>? actualDataPayload;
      String? token;
      var hunterData; // Puede ser Map o cualquier otro tipo.
      String successMessageToShow = apiOuterMessage; // Por defecto, el mensaje de la API externa.

      if (dataField is Map<String, dynamic>) {
        actualDataPayload = dataField;
        print('$operation - Campo "Data" SÍ es un Map<String, dynamic>.');
      } else if (dataField is String) {
        print('$operation - Campo "Data" es un String. Intentando decodificarlo como JSON...');
        try {
          var decodedDataString = jsonDecode(dataField);
          if (decodedDataString is Map<String, dynamic>) {
            actualDataPayload = decodedDataString;
            print('$operation - String "Data" decodificado exitosamente a Map.');
          } else {
            print('$operation API WARNING: String "Data" decodificado pero no es un Mapa. Tipo: ${decodedDataString.runtimeType}. Se usará el mensaje externo.');
          }
        } catch (e) {
          print('$operation API WARNING: Fallo al decodificar String "Data". Error: $e. Contenido: "$dataField". Se usará el mensaje externo.');
        }
      } else if (dataField != null) {
         print('$operation API WARNING: El campo "Data" NO es un Mapa ni un String JSON válido. Tipo actual: ${dataField.runtimeType}. Se usará el mensaje externo.');
      } else {
        print('$operation - El campo "Data" es nulo. Se usará el mensaje externo.');
      }

      // Si tenemos un payload interno (actualDataPayload), intentamos extraer información.
      if (actualDataPayload != null) {
        token = actualDataPayload['token'] as String?;
        hunterData = actualDataPayload['hunter'];

        // Opcional: si el mensaje interno es más específico para el éxito Y la operación externa fue exitosa.
        String? apiInnerMessage = actualDataPayload['message'] as String?;
        if (apiInnerMessage != null && apiInnerMessage.isNotEmpty) {
          // Podrías decidir usar el mensaje interno si es más relevante para el éxito.
          // Por ejemplo, si el outer message es genérico como "Operación exitosa"
          // y el inner message es "Token generado y perfil cargado".
          // Para este caso, el `apiOuterMessage` ya parece ser el mensaje de éxito deseado ("Welcome back...").
          // successMessageToShow = apiInnerMessage; // Descomentar si prefieres el mensaje interno en caso de éxito.
        }
         print('$operation - apiInnerMessage (del actualDataPayload): "$apiInnerMessage"');
      }
        
      if (token != null) {
        try {
          await _storage.write(key: 'jwt_token', value: token);
          print('ApiService ($operation): Token guardado.');
        } catch (storageError) {
          print('ApiService ($operation): ÉXITO API, PERO FALLÓ AL GUARDAR TOKEN. Error: $storageError.');
          // Considera si esto debería convertir la operación en un fallo para la UI.
          // Por ahora, se mantiene como éxito ya que el login/registro en sí fue exitoso.
        }
      } else {
        print('ApiService ($operation): Éxito API, no se encontró token en actualDataPayload.');
      }
      
      Map<String, dynamic> successResult = {
        'success': true, // <--- ESTA ES LA CLAVE PARA EL COLOR VERDE
        'message': successMessageToShow, 
        'token': token,
        'hunter': hunterData
      };
      print('--- FIN _handleApiResponse (ÉXITO LÓGICO DEFINIDO POR HTTP Y API EXTERNA): $successResult ---');
      return successResult;

    } catch (e, s) { 
      print('$operation API Error: Excepción INESPERADA dentro de _handleApiResponse (después del parseo inicial). Error: $e');
      print('$operation API StackTrace: $s');
      return {'success': false, 'message': 'Error interno crítico procesando la respuesta del servidor.'};
    }
  }

  Future<Map<String, dynamic>> login(String username, String password) async {
    final Uri loginUrl = Uri.parse('$_baseUrl/auth/login');
    print('ApiService: Intentando login para usuario: $username');
    try {
      final response = await http.post(
        loginUrl,
        headers: <String, String>{'Content-Type': 'application/json; charset=UTF-8'},
        body: jsonEncode(<String, String>{'username': username, 'password': password}),
      );
      return await _handleApiResponse(response, 'LOGIN');
    } catch (e) {
      print('ApiService: Excepción en el método login (probablemente red/conexión): ${e.toString()}');
      return {'success': false, 'message': 'Error de red o conexión (login): ${e.toString()}'};
    }
  }

  Future<Map<String, dynamic>> registerUser({
    required String username,
    required String email,
    required String password,
    required String hunterName,
  }) async {
    final Uri registerUrl = Uri.parse('$_baseUrl/auth/register');
    print('ApiService: Intentando registrar nuevo usuario: $username, Email: $email, HunterName: $hunterName');
    try {
      final response = await http.post(
        registerUrl,
        headers: <String, String>{'Content-Type': 'application/json; charset=UTF-8'},
        body: jsonEncode(<String, String>{
          'username': username,
          'email': email,
          'password': password,
          'hunterName': hunterName,
        }),
      );
      return await _handleApiResponse(response, 'REGISTER');
    } catch (e) {
      print('ApiService: Excepción en registerUser (probablemente red/conexión): ${e.toString()}');
      return {'success': false, 'message': 'Error de red o conexión (registro): ${e.toString()}'};
    }
  }

  Future<String?> getToken() async {
    return await _storage.read(key: 'jwt_token');
  }

  Future<void> logout() async {
    await _storage.delete(key: 'jwt_token');
  }
}