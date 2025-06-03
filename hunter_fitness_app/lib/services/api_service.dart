// hunter_fitness_app/lib/services/api_service.dart
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
    // Intentar leer 'Success' (PascalCase) primero, luego 'success' (camelCase)
    dynamic successValue = responseData['Success'] ?? responseData['success']; 
    if (successValue is bool) {
        apiOverallSuccess = successValue;
    } else if (successValue is String) {
        apiOverallSuccess = successValue.toLowerCase() == 'true';
    } else {
        print('$operation - ADVERTENCIA: Clave de éxito ("Success" o "success") no encontrada o no es bool/String en el JSON principal. Asumiendo éxito si HTTP es 2xx.');
        // Si no hay una clave de éxito explícita en el nivel superior, depender del código HTTP
        apiOverallSuccess = (response.statusCode >= 200 && response.statusCode < 300);
    }
    
    // Intentar leer 'Message' (PascalCase) primero, luego 'message' (camelCase)
    String apiOverallMessage = responseData['Message'] as String? ?? responseData['message'] as String? ?? 'Mensaje no proporcionado por la API.';
    
    print('$operation - apiOverallSuccess (del JSON principal o HTTP): $apiOverallSuccess');
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
    // (y la clave 'Success'/'success' estaba presente y era false)
    if (httpSuccess && (responseData.containsKey('Success') || responseData.containsKey('success')) && !apiOverallSuccess) {
        print('$operation - Fallo declarado por API (Success: false en JSON principal). Mensaje: "$apiOverallMessage"');
        Map<String, dynamic>? originalDataPayload = responseData['data'] as Map<String, dynamic>? ?? responseData['Data'] as Map<String, dynamic>?;
        return {'success': false, 'message': apiOverallMessage, 'data': originalDataPayload, 'hunterProfile': null};
    }

    // En este punto, HTTP fue exitoso Y (la bandera 'success' principal de la API es true O no había bandera de éxito principal).
    // El campo 'data' o 'Data' contiene la carga útil real (por ejemplo, AuthResponseDto, DailyQuestsSummaryDto, QuestOperationResponseDto).
    Map<String, dynamic>? dataFieldFromResponse = responseData['data'] as Map<String, dynamic>? ?? responseData['Data'] as Map<String, dynamic>?;
    
    String? tokenFromDataField;
    Map<String, dynamic>? hunterProfileFromDataField;

    if (dataFieldFromResponse != null) {
      print('$operation - dataFieldFromResponse (contenido de responseData["data"] o ["Data"]) Keys: ${dataFieldFromResponse.keys}');
      
      // Para LOGIN y REGISTER, esperamos que el token y el perfil estén dentro de 'dataFieldFromResponse'
      if (operation == 'LOGIN' || operation == 'REGISTER') {
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
      }
    } else {
        print('$operation - dataFieldFromResponse (responseData["data"] o ["Data"]) es null o no es un Map. No se puede extraer token ni hunter de él.');
        // Si el dataField es null, pero la operación fue LOGIN/REGISTER, y el token está en el nivel superior de responseData, tomarlo de allí.
        // Esto es un fallback en caso de que la API devuelva el token en el nivel raíz para login/register.
        if ((operation == 'LOGIN' || operation == 'REGISTER') && tokenFromDataField == null) {
            tokenFromDataField = responseData['token'] as String? ?? responseData['Token'] as String?;
            if (tokenFromDataField != null) {
                 print('$operation - Fallback: Token encontrado en el NIVEL RAÍZ de responseData: $tokenFromDataField');
            }
        }
    }

    if ((operation == 'LOGIN' || operation == 'REGISTER') && tokenFromDataField != null) {
      await _storage.write(key: 'jwt_token', value: tokenFromDataField);
      print('ApiService ($operation): Token guardado: $tokenFromDataField');
    } else if (operation == 'LOGIN' || operation == 'REGISTER') {
      print('ApiService ($operation): Token NO encontrado en la respuesta para guardar.');
      // Si el login/registro fue exitoso pero no hay token, podría ser un error de la API
      if (apiOverallSuccess) {
        apiOverallSuccess = false; // Marcar como fallo si no hay token en una operación de auth exitosa
        apiOverallMessage = "Respuesta de autenticación exitosa pero no se encontró el token.";
        print('ApiService ($operation): $apiOverallMessage');
      }
    }
    
    print('ApiService ($operation): VALOR FINAL apiOverallSuccess: $apiOverallSuccess');
    print('ApiService ($operation): VALOR FINAL apiOverallMessage: "$apiOverallMessage"');
    print('ApiService ($operation): VALOR FINAL token (extraído): $tokenFromDataField');
    print('ApiService ($operation): VALOR FINAL hunterProfile (extraído de dataField): $hunterProfileFromDataField');
    print('ApiService ($operation): VALOR FINAL dataFieldFromResponse (objeto "data" completo de la API): $dataFieldFromResponse');

    return {
      'success': apiOverallSuccess,
      'message': apiOverallMessage,
      'data': dataFieldFromResponse, // El AuthResponseDto, DailyQuestsSummaryDto, QuestOperationResponseDto, etc.
      'token': tokenFromDataField, 
      'hunterProfile': hunterProfileFromDataField, 
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
      // _handleApiResponse devuelve el DailyQuestsSummaryDto en el campo 'data'
      return await _handleApiResponse(response, 'GET_DAILY_QUESTS');
    } catch (e) {
      print('ApiService: Excepción en getDailyQuests (red/conexión): ${e.toString()}');
      return {'success': false, 'message': 'Error de red o conexión (misiones): ${e.toString()}'};
    }
  }

  // NUEVO MÉTODO
  Future<Map<String, dynamic>> completeQuest(
      String assignmentId, {
      int? finalReps,
      int? finalSets,
      int? finalDuration,
      double? finalDistance,
      bool perfectExecution = false,
  }) async {
    final String? token = await getToken();
    if (token == null) {
      print('ApiService (completeQuest): Token no encontrado.');
      return {'success': false, 'message': 'Autenticación requerida.'};
    }

    final Uri completeQuestUrl = Uri.parse('$_baseUrl/quests/complete');
    print('ApiService: Completando quest con AssignmentID: $assignmentId');

    Map<String, dynamic> body = {
      'assignmentID': assignmentId, // Asegúrate que el backend espera 'assignmentID' (camelCase)
      'perfectExecution': perfectExecution,
    };
    if (finalReps != null) body['finalReps'] = finalReps;
    if (finalSets != null) body['finalSets'] = finalSets;
    if (finalDuration != null) body['finalDuration'] = finalDuration;
    if (finalDistance != null) body['finalDistance'] = finalDistance;
    
    try {
      final response = await http.post(
        completeQuestUrl,
        headers: <String, String>{
          'Content-Type': 'application/json; charset=UTF-8',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode(body),
      );
      // _handleApiResponse devolverá el QuestOperationResponseDto en el campo 'data'
      return await _handleApiResponse(response, 'COMPLETE_QUEST');
    } catch (e) {
      print('ApiService: Excepción en completeQuest (red/conexión): ${e.toString()}');
      return {'success': false, 'message': 'Error de red o conexión (completar quest): ${e.toString()}'};
    }
  }
}