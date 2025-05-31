import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class ApiService {
  static const String _baseUrl = "https://hunter-fitness-api.azurewebsites.net/api";
  final _storage = const FlutterSecureStorage();

  Future<String?> getToken() async {
    return await _storage.read(key: 'jwt_token');
  }

  Future<void> logout() async {
    await _storage.delete(key: 'jwt_token');
    print('ApiService: Token eliminado, sesión cerrada.');
  }

  Future<Map<String, dynamic>> _handleApiResponse(http.Response response, String operation) async {
    print('--- INICIO _handleApiResponse para: $operation ---');
    print('$operation API Status Code: ${response.statusCode}');
    final String rawResponseBody = response.body;
    print('$operation API Response Body (RAW): "$rawResponseBody"');

    if (rawResponseBody.isEmpty && response.statusCode != 204) { // 204 No Content es una respuesta vacía válida
      print('$operation API Error: Cuerpo de respuesta vacío (y no es 204).');
      return {'success': false, 'message': 'El servidor devolvió una respuesta vacía inesperada.'};
    }
     if (rawResponseBody.isEmpty && response.statusCode == 204) {
      print('$operation API Info: Respuesta 204 No Content, cuerpo vacío es esperado.');
      // Para operaciones como logout o delete que pueden devolver 204, esto es un éxito.
      // Ajustar según la necesidad de la operación específica.
      // Si la operación que llama espera datos, esto debería ser un error lógico.
      // Por ahora, asumiremos que si es 204, la operación fue exitosa a menos que se indique lo contrario.
      return {'success': true, 'message': 'Operación completada sin contenido de respuesta.', 'data': null};
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

    bool apiOuterSuccess = false;
    String apiOuterMessage = 'Mensaje no proporcionado por la API (predeterminado).';

    if (outerResponseData.containsKey('Success')) {
      var successValue = outerResponseData['Success'];
      if (successValue is bool) {
        apiOuterSuccess = successValue;
      } else if (successValue is String && successValue.toLowerCase() == 'true') {
        apiOuterSuccess = true;
      }
    } else if (outerResponseData.containsKey('success')) { // Fallback a camelCase 'success'
        var successValue = outerResponseData['success'];
        if (successValue is bool) {
          apiOuterSuccess = successValue;
        } else if (successValue is String && successValue.toLowerCase() == 'true') {
          apiOuterSuccess = true;
        }
    }


    if (outerResponseData.containsKey('Message')) {
      var messageValue = outerResponseData['Message'];
       if (messageValue is String) {
        apiOuterMessage = messageValue;
      } else if (messageValue != null) {
        apiOuterMessage = messageValue.toString();
      }
    } else if (outerResponseData.containsKey('message')) { // Fallback a camelCase 'message'
        var messageValue = outerResponseData['message'];
        if (messageValue is String) {
          apiOuterMessage = messageValue;
        } else if (messageValue != null) {
          apiOuterMessage = messageValue.toString();
        }
    }


    try {
      bool httpSuccess = (response.statusCode >= 200 && response.statusCode < 300);

      print('$operation - httpSuccess: $httpSuccess (StatusCode: ${response.statusCode})');
      print('$operation - apiOuterSuccess (determinado): $apiOuterSuccess');
      print('$operation - apiOuterMessage (determinado): "$apiOuterMessage"');

      if (!httpSuccess) {
        print('$operation - Fallo HTTP. Mensaje: "$apiOuterMessage". Error de comunicación (código: ${response.statusCode}).');
        return {'success': false, 'message': '$apiOuterMessage (Error HTTP: ${response.statusCode})'};
      }
      
      if (!apiOuterSuccess && httpSuccess) { // Si HTTP fue éxito pero la API dice que no (ej: validación fallida)
          print('$operation - Fallo declarado por API (apiOuterSuccess es false), aunque HTTP fue ${response.statusCode}. Mensaje: "$apiOuterMessage"');
          return {'success': false, 'message': apiOuterMessage};
      }
      
      // Si llegamos aquí, httpSuccess ES true Y apiOuterSuccess ES true (o no está presente y asumimos éxito por HTTP status).
      var dataField = outerResponseData['Data'] ?? outerResponseData['data']; // Considerar PascalCase y camelCase
      Map<String, dynamic>? actualDataPayload;
      String? token;
      var hunterData;
      String successMessageToShow = apiOuterMessage;

      if (dataField is Map<String, dynamic>) {
        actualDataPayload = dataField;
      } else if (dataField is String) {
        try {
          var decodedDataString = jsonDecode(dataField);
          if (decodedDataString is Map<String, dynamic>) {
            actualDataPayload = decodedDataString;
          }
        } catch (e) { /* No hacer nada si no es JSON, ya tenemos apiOuterSuccess */ }
      }
      
      // Para login/registro específicamente, esperamos token y hunter en el payload de Data
      if (operation == 'LOGIN' || operation == 'REGISTER') {
        if (actualDataPayload != null) {
          token = actualDataPayload['token'] as String?;
          hunterData = actualDataPayload['hunter'];
          String? apiInnerMessage = actualDataPayload['message'] as String?;
          if (apiInnerMessage != null && apiInnerMessage.isNotEmpty && apiOuterMessage == 'Mensaje no proporcionado por la API (predeterminado).') {
             successMessageToShow = apiInnerMessage;
          }
        }
        if (token != null) {
          await _storage.write(key: 'jwt_token', value: token);
        }
      }

      Map<String, dynamic> successResult = {
        'success': true,
        'message': successMessageToShow,
        'data': dataField, // Devolver el campo 'Data' completo para que el llamador lo procese
      };
      // Específicamente para login/registro, añadir token y hunter al nivel superior para fácil acceso
      if (operation == 'LOGIN' || operation == 'REGISTER') {
        successResult['token'] = token;
        successResult['hunter'] = hunterData;
      }

      print('--- FIN _handleApiResponse (ÉXITO LÓGICO): $successResult ---');
      return successResult;

    } catch (e, s) { 
      print('$operation API Error: Excepción INESPERADA procesando la respuesta. Error: $e');
      print('$operation API StackTrace: $s');
      return {'success': false, 'message': 'Error interno crítico procesando la respuesta.'};
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
      print('ApiService: Excepción en el método login (red/conexión): ${e.toString()}');
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
    print('ApiService: Intentando registrar: $username, Email: $email, HunterName: $hunterName');
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
      print('ApiService: Excepción en registerUser (red/conexión): ${e.toString()}');
      return {'success': false, 'message': 'Error de red o conexión (registro): ${e.toString()}'};
    }
  }

  Future<Map<String, dynamic>> getDailyQuests() async {
    final String? token = await getToken();
    if (token == null) {
      print('ApiService (getDailyQuests): Token no encontrado. Abortando.');
      return {'success': false, 'message': 'Autenticación requerida. No se encontró token.'};
    }

    final Uri dailyQuestsUrl = Uri.parse('$_baseUrl/quests/daily');
    print('ApiService: Obteniendo misiones diarias...');
    try {
      final response = await http.get(
        dailyQuestsUrl,
        headers: <String, String>{
          'Content-Type': 'application/json; charset=UTF-8',
          'Authorization': 'Bearer $token',
        },
      );
      return await _handleApiResponse(response, 'GET_DAILY_QUESTS');
    } catch (e) {
      print('ApiService: Excepción en getDailyQuests (red/conexión): ${e.toString()}');
      return {'success': false, 'message': 'Error de red o conexión (misiones diarias): ${e.toString()}'};
    }
  }
}