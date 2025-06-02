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
      return {'success': true, 'message': 'Operación completada sin contenido.', 'data': null, 'hunterProfile': null};
    }

    Map<String, dynamic> responseData;
    try {
      var decodedJson = jsonDecode(rawResponseBody);
      if (decodedJson is Map<String, dynamic>) {
        responseData = decodedJson;
        print('$operation - jsonDecode exitoso. responseData es un Map.');
      } else {
        print('$operation API Error: El JSON decodificado no es un Mapa. Tipo actual: ${decodedJson.runtimeType}');
        return {'success': false, 'message': 'Respuesta del servidor con formato inesperado (no es objeto JSON).', 'data': null, 'hunterProfile': null};
      }
    } catch (e) {
      print('$operation API Error: Fallo al decodificar JSON. Error: $e. Cuerpo: "$rawResponseBody"');
      return {'success': false, 'message': 'Respuesta inesperada del servidor (formato incorrecto).', 'data': null, 'hunterProfile': null};
    }

    bool apiOverallSuccess = false;
    dynamic successValue = responseData['Success'] ?? responseData['success'];
    if (successValue is bool) {
        apiOverallSuccess = successValue;
    } else if (successValue is String) {
        apiOverallSuccess = successValue.toLowerCase() == 'true';
    } else {
        print('$operation - ADVERTENCIA: Clave de éxito ("Success" o "success") no encontrada o no es bool/String en el JSON principal.');
    }
    
    String apiOverallMessage = responseData['Message'] as String? ?? responseData['message'] as String? ?? 'Mensaje no proporcionado por la API.';
    
    print('$operation - apiOverallSuccess (del JSON principal): $apiOverallSuccess');
    print('$operation - apiOverallMessage (del JSON principal): "$apiOverallMessage"');

    bool httpSuccess = (response.statusCode >= 200 && response.statusCode < 300);

    if (!httpSuccess) {
      print('$operation - Fallo HTTP. Mensaje de API: "$apiOverallMessage". Código: ${response.statusCode}.');
      String httpErrorMessage = (responseData.containsKey('Message') || responseData.containsKey('message')) 
                                ? apiOverallMessage 
                                : 'Error de comunicación con el servidor';
      return {'success': false, 'message': '$httpErrorMessage (HTTP ${response.statusCode})', 'data': null, 'hunterProfile': null};
    }

    // Si el HTTP fue exitoso pero la API declara fallo explícitamente
    if (!apiOverallSuccess) {
        print('$operation - Fallo declarado por API (Success: false en JSON principal). Mensaje: "$apiOverallMessage"');
        // Retornar el 'data' original si existe, podría contener más detalles del error de la API anidado.
        Map<String, dynamic>? originalDataPayload = responseData['data'] as Map<String, dynamic>? ?? responseData['Data'] as Map<String, dynamic>?;
        return {'success': false, 'message': apiOverallMessage, 'data': originalDataPayload, 'hunterProfile': null};
    }

    // En este punto, HTTP fue exitoso Y la bandera 'success' principal de la API es true.
    Map<String, dynamic>? dataFieldFromResponse = responseData['data'] as Map<String, dynamic>? ?? responseData['Data'] as Map<String, dynamic>?;
    
    String? tokenFromDataField;
    Map<String, dynamic>? hunterProfileFromDataField;

    if (dataFieldFromResponse != null) {
      print('$operation - dataFieldFromResponse (contenido de responseData["data"] o ["Data"]) Keys: ${dataFieldFromResponse.keys}');
      
      tokenFromDataField = dataFieldFromResponse['token'] as String? ?? dataFieldFromResponse['Token'] as String?;
      if (tokenFromDataField != null) {
        print('$operation - Token encontrado DENTRO de dataFieldFromResponse: $tokenFromDataField');
      } else {
        print('$operation - Token NO encontrado dentro de dataFieldFromResponse (keys: ${dataFieldFromResponse.keys}).');
      }
      
      dynamic hunterRawData = dataFieldFromResponse['hunter'] ?? dataFieldFromResponse['Hunter'];
      if (hunterRawData != null) {
        if (hunterRawData is Map) {
          try {
            hunterProfileFromDataField = Map<String, dynamic>.from(hunterRawData);
            print('$operation - Perfil Hunter extraído exitosamente de dataFieldFromResponse["hunter" o "Hunter"]: $hunterProfileFromDataField');
          } catch (e) {
            print('$operation - Error al castear hunterRawData a Map<String, dynamic>: $e. hunterRawData: $hunterRawData');
          }
        } else {
          print('$operation - "hunter" (o "Hunter") encontrado en dataFieldFromResponse, PERO NO es un Map. Tipo actual: ${hunterRawData.runtimeType}. Valor: $hunterRawData');
        }
      } else {
         print('$operation - "hunter" (o "Hunter") NO encontrado dentro de dataFieldFromResponse (keys: ${dataFieldFromResponse.keys}).');
      }
    } else {
        print('$operation - dataFieldFromResponse (responseData["data"] o ["Data"]) es null o no es un Map. No se puede extraer token ni hunter de él.');
    }

    if ((operation == 'LOGIN' || operation == 'REGISTER') && tokenFromDataField != null) {
      await _storage.write(key: 'jwt_token', value: tokenFromDataField);
      print('ApiService ($operation): Token guardado: $tokenFromDataField');
    } else if (operation == 'LOGIN' || operation == 'REGISTER') {
      print('ApiService ($operation): Token NO encontrado en la respuesta para guardar.');
    }
    
    print('ApiService ($operation): VALOR FINAL apiOverallSuccess: $apiOverallSuccess');
    print('ApiService ($operation): VALOR FINAL apiOverallMessage: "$apiOverallMessage"');
    print('ApiService ($operation): VALOR FINAL token (extraído de dataField): $tokenFromDataField');
    print('ApiService ($operation): VALOR FINAL hunterProfile (extraído de dataField): $hunterProfileFromDataField');
    print('ApiService ($operation): VALOR FINAL dataFieldFromResponse (objeto "data" completo de la API): $dataFieldFromResponse');

    return {
      'success': apiOverallSuccess,
      'message': apiOverallMessage,
      'data': dataFieldFromResponse, // El AuthResponseDto completo o el payload de quests/etc.
      'token': tokenFromDataField, // Token extraído para conveniencia (puede ser null)
      'hunterProfile': hunterProfileFromDataField, // Perfil del hunter extraído para conveniencia (puede ser null)
    };
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
      // _handleApiResponse ahora devuelve un mapa con 'data' que contiene el DailyQuestsSummaryDto
      // y 'hunterProfile' que será null aquí.
      Map<String,dynamic> parsedResponse = await _handleApiResponse(response, 'GET_DAILY_QUESTS');
      
      // Para mantener la compatibilidad con cómo HomeScreen espera los datos de quests:
      // Si _handleApiResponse fue exitoso, y parsedResponse['data'] tiene la estructura de DailyQuestsSummaryDto,
      // entonces la lógica en HomeScreen para acceder a parsedResponse['data']['quests'] debería seguir funcionando.
      // Si necesitas devolver la estructura anterior exacta para getDailyQuests, podrías hacer esto:
      // return {
      //   'success': parsedResponse['success'],
      //   'message': parsedResponse['message'],
      //   'data': parsedResponse['data'] // Aquí 'data' es el DailyQuestsSummaryDto
      // };
      // Por ahora, devolvemos el mapa completo de _handleApiResponse. HomeScreen deberá adaptarse si es necesario,
      // o puedes ajustar este retorno como se comentó arriba.
      return parsedResponse;


    } catch (e) {
      print('ApiService: Excepción en getDailyQuests (red/conexión): ${e.toString()}');
      return {'success': false, 'message': 'Error de red o conexión (misiones): ${e.toString()}'};
    }
  }
}