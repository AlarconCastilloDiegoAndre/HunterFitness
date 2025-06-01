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

    if (rawResponseBody.isEmpty && response.statusCode != 204) {
      print('$operation API Error: Cuerpo de respuesta vacío (y no es 204).');
      return {'success': false, 'message': 'El servidor devolvió una respuesta vacía inesperada.'};
    }
    if (rawResponseBody.isEmpty && response.statusCode == 204) {
      print('$operation API Info: Respuesta 204 No Content.');
      return {'success': true, 'message': 'Operación completada sin contenido.', 'data': null};
    }

    Map<String, dynamic> responseData;
    try {
      var decodedJson = jsonDecode(rawResponseBody);
      if (decodedJson is Map<String, dynamic>) {
        responseData = decodedJson;
        print('$operation - jsonDecode exitoso. responseData es un Map.');
      } else {
        print('$operation API Error: El JSON decodificado no es un Mapa. Tipo actual: ${decodedJson.runtimeType}');
        return {'success': false, 'message': 'Respuesta del servidor con formato inesperado (no es objeto JSON).'};
      }
    } catch (e) {
      print('$operation API Error: Fallo al decodificar JSON. Error: $e. Cuerpo: "$rawResponseBody"');
      return {'success': false, 'message': 'Respuesta inesperada del servidor (formato incorrecto).'};
    }

    // LEER 'Success' y 'Message' (PascalCase) del NIVEL SUPERIOR del JSON
    bool apiOverallSuccess = false;
    if (responseData.containsKey('Success')) {
        var successValue = responseData['Success'];
        if (successValue is bool) {
            apiOverallSuccess = successValue;
        } else if (successValue is String) {
            apiOverallSuccess = successValue.toLowerCase() == 'true';
        }
    } else {
        print('$operation - ADVERTENCIA: Clave "Success" (PascalCase) no encontrada en el JSON principal. Buscando "success" (camelCase)...');
        // Fallback a camelCase si 'Success' no existe, aunque el log indica que 'Success' sí está
        var successValueCamel = responseData['success'];
         if (successValueCamel is bool) {
            apiOverallSuccess = successValueCamel;
        } else if (successValueCamel is String) {
            apiOverallSuccess = successValueCamel.toLowerCase() == 'true';
        } else {
            print('$operation - ADVERTENCIA: Ninguna clave de éxito ("Success" o "success") encontrada o válida en el JSON principal.');
        }
    }
    
    String apiOverallMessage = 'Mensaje no proporcionado por la API.';
    if (responseData.containsKey('Message')) {
        apiOverallMessage = responseData['Message'] as String? ?? apiOverallMessage;
    } else if (responseData.containsKey('message')) { // Fallback
        apiOverallMessage = responseData['message'] as String? ?? apiOverallMessage;
    }
    
    print('$operation - apiOverallSuccess (del JSON principal, esperando PascalCase): $apiOverallSuccess');
    print('$operation - apiOverallMessage (del JSON principal, esperando PascalCase): "$apiOverallMessage"');

    bool httpSuccess = (response.statusCode >= 200 && response.statusCode < 300);

    if (!httpSuccess) {
      print('$operation - Fallo HTTP. Mensaje de API: "$apiOverallMessage". Código: ${response.statusCode}.');
      // Devolver el mensaje de la API si está disponible y es más descriptivo que un error HTTP genérico
      String httpErrorMessage = responseData.containsKey('Message') || responseData.containsKey('message') 
                                ? apiOverallMessage 
                                : 'Error de comunicación con el servidor';
      return {'success': false, 'message': '$httpErrorMessage (HTTP ${response.statusCode})'};
    }

    // Si HTTP tuvo éxito, pero la API en su payload principal dice que no fue exitoso
    if (!apiOverallSuccess) {
        print('$operation - Fallo declarado por API (Success: false en JSON principal). Mensaje: "$apiOverallMessage"');
        return {'success': false, 'message': apiOverallMessage};
    }

    // Si llegamos aquí, tanto HTTP como el 'Success' del JSON principal son verdaderos.
    Map<String, dynamic>? dataPayload = responseData['Data'] as Map<String, dynamic>? ?? responseData['data'] as Map<String, dynamic>?;
    String? token = responseData['token'] as String?; 
    dynamic hunterDataObject = null;

    if (dataPayload != null) {
      token ??= dataPayload['token'] as String?;
      hunterDataObject = dataPayload['hunter'];
      
      // Considerar si el mensaje de 'data' debe sobrescribir el mensaje general
      // String? dataMessage = dataPayload['message'] as String?;
      // bool dataInnerSuccess = dataPayload['success'] as bool? ?? true; // Si está en data, debe ser true también
      // if (dataMessage != null && dataMessage.isNotEmpty && dataInnerSuccess) {
      //    apiOverallMessage = dataMessage; // Opcional: priorizar mensaje de 'data' si es más específico para éxito
      // }
    } else if (operation == 'LOGIN' || operation == 'REGISTER') { // Si Data es null pero es Login/Register, el hunter puede estar en el nivel superior
        hunterDataObject = responseData['hunter'];
    }
    
    if (operation == 'LOGIN' || operation == 'REGISTER') {
      if (token != null) {
        await _storage.write(key: 'jwt_token', value: token);
        print('ApiService ($operation): Token guardado.');
      } else {
        print('ApiService ($operation): Token no encontrado en la respuesta.');
      }
      if (hunterDataObject == null) {
         print('ApiService ($operation): Objeto Hunter no encontrado en la respuesta.');
      }
    }

    return {
      'success': true, 
      'message': apiOverallMessage,
      'token': token, 
      'hunter': hunterDataObject, 
      'data': dataPayload, 
    };

  } // Fin de _handleApiResponse

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
      print('ApiService (getDailyQuests): Token no encontrado.');
      return {'success': false, 'message': 'Autenticación requerida.'};
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
      return {'success': false, 'message': 'Error de red o conexión (misiones): ${e.toString()}'};
    }
  }
}