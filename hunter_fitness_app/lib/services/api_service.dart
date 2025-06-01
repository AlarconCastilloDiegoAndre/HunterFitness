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
      return {'success': true, 'message': 'Operación completada sin contenido.'}; // 'data' puede ser implicitamente null
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

    bool apiOverallSuccess = false;
    if (responseData.containsKey('Success')) {
        var successValue = responseData['Success'];
        if (successValue is bool) {
            apiOverallSuccess = successValue;
        } else if (successValue is String) {
            apiOverallSuccess = successValue.toLowerCase() == 'true';
        }
    } else if (responseData.containsKey('success')) {
        var successValueCamel = responseData['success'];
         if (successValueCamel is bool) {
            apiOverallSuccess = successValueCamel;
        } else if (successValueCamel is String) {
            apiOverallSuccess = successValueCamel.toLowerCase() == 'true';
        } else {
            print('$operation - ADVERTENCIA: Ninguna clave de éxito ("Success" o "success") encontrada o válida en el JSON principal.');
        }
    } else {
        print('$operation - ADVERTENCIA: Clave "Success" (PascalCase) ni "success" (camelCase) no encontrada en el JSON principal.');
    }
    
    String apiOverallMessage = 'Mensaje no proporcionado por la API.';
    if (responseData.containsKey('Message')) {
        apiOverallMessage = responseData['Message'] as String? ?? apiOverallMessage;
    } else if (responseData.containsKey('message')) {
        apiOverallMessage = responseData['message'] as String? ?? apiOverallMessage;
    }
    
    print('$operation - apiOverallSuccess (del JSON principal): $apiOverallSuccess');
    print('$operation - apiOverallMessage (del JSON principal): "$apiOverallMessage"');

    bool httpSuccess = (response.statusCode >= 200 && response.statusCode < 300);

    if (!httpSuccess) {
      print('$operation - Fallo HTTP. Mensaje de API: "$apiOverallMessage". Código: ${response.statusCode}.');
      String httpErrorMessage = responseData.containsKey('Message') || responseData.containsKey('message') 
                                ? apiOverallMessage 
                                : 'Error de comunicación con el servidor';
      return {'success': false, 'message': '$httpErrorMessage (HTTP ${response.statusCode})'};
    }

    if (!apiOverallSuccess) {
        print('$operation - Fallo declarado por API (Success: false en JSON principal). Mensaje: "$apiOverallMessage"');
        return {'success': false, 'message': apiOverallMessage};
    }

    Map<String, dynamic>? dataPayload;
    if (responseData.containsKey('data') && responseData['data'] is Map<String, dynamic>) {
        dataPayload = responseData['data'] as Map<String, dynamic>;
        print('ApiService ($operation): dataPayload (responseData["data"]) asignado.');
    } else if (responseData.containsKey('Data') && responseData['Data'] is Map<String, dynamic>) { 
        dataPayload = responseData['Data'] as Map<String, dynamic>;
        print('ApiService ($operation): dataPayload (responseData["Data"]) asignado como fallback.');
    } else {
        print('ApiService ($operation): dataPayload (ni "data" ni "Data") no encontrado o no es un Map en responseData. Keys: ${responseData.keys}');
    }
    
    String? token; // Token a ser guardado y potencialmente retornado
    Map<String, dynamic>? hunterDataObjectAsMap; // Perfil del hunter

    if (dataPayload != null) {
      print('ApiService ($operation): Procesando dataPayload. Keys: ${dataPayload.keys}');
      if (dataPayload.containsKey('token') && dataPayload['token'] is String) {
        token = dataPayload['token'] as String?; // Extraer token de dataPayload
        print('ApiService ($operation): Token encontrado DENTRO de dataPayload.');
      } else {
        print('ApiService ($operation): Clave "token" no encontrada o no es String dentro de dataPayload.');
      }
      
      if (dataPayload.containsKey('hunter')) {
        if (dataPayload['hunter'] is Map<String, dynamic>) {
          hunterDataObjectAsMap = dataPayload['hunter'] as Map<String, dynamic>;
          print('ApiService ($operation): Objeto Hunter ENCONTRADO DENTRO de dataPayload y asignado como Map<String, dynamic>.');
        } else {
          print('ApiService ($operation): Clave "hunter" encontrada en dataPayload, pero NO es un Map<String, dynamic>. Tipo: ${dataPayload['hunter']?.runtimeType}');
        }
      } else {
        print('ApiService ($operation): Clave "hunter" no encontrada dentro de dataPayload.');
      }
    } else {
        print('ApiService ($operation): dataPayload es null, no se puede extraer token ni hunter de él.');
    }

    // Si el token no se encontró en dataPayload, intentar buscarlo en el nivel superior
    // (esto es menos probable con tu estructura de API actual, pero es un fallback)
    token ??= responseData['token'] as String?;

    // Lógica para guardar el token si es una operación de LOGIN o REGISTER
    if (operation == 'LOGIN' || operation == 'REGISTER') {
      if (token != null) {
        await _storage.write(key: 'jwt_token', value: token);
        print('ApiService ($operation): Token guardado: $token');
      } else {
        print('ApiService ($operation): Token NO encontrado en la respuesta para guardar.');
      }
      // Log sobre hunterDataObjectAsMap (ya se hace más abajo antes del return)
      if (hunterDataObjectAsMap == null) {
         print('ApiService ($operation): hunterDataObjectAsMap SIGUE SIENDO NULL después de todos los intentos. NO SE PUEDE CONTINUAR con datos del hunter si se esperaba.');
      }
    }
    
    // Logs de los componentes clave ANTES de construir el mapa de retorno
    print('ApiService ($operation): VALOR FINAL apiOverallSuccess: $apiOverallSuccess');
    print('ApiService ($operation): VALOR FINAL apiOverallMessage: "$apiOverallMessage"');
    print('ApiService ($operation): VALOR FINAL token (que se intentó guardar): $token');
    print('ApiService ($operation): VALOR FINAL hunterDataObjectAsMap (perfil del hunter): $hunterDataObjectAsMap');
    print('ApiService ($operation): VALOR FINAL dataPayload (este es responseData["data"]): $dataPayload');

    // Construcción del mapa de retorno
    Map<String, dynamic> mapToReturn = {
      'success': apiOverallSuccess,
      'message': apiOverallMessage,
      // 'token': token, // Comentado: el token está en 'data', LoginScreen lo puede tomar de ahí si es necesario.
                       // O podrías decidir retornarlo aquí también si es más conveniente para otras partes de tu app.
      // 'hunter': hunterDataObjectAsMap, // Comentado: Esta es la asignación que resultaba en 'hunter: null' en LoginScreen.
                                       // La UI ahora debe leer de 'data.hunter'.
      'data': dataPayload, // dataPayload ES responseData['data'], y ya contiene 'token' y 'hunter' de la API.
    };

    // Para depurar si 'hunter' se pierde al asignarlo directamente a mapToReturn['hunter']
    // Forzamos la asignación de hunterDataObjectAsMap a una nueva clave para ver si se mantiene.
    if (hunterDataObjectAsMap != null) {
      mapToReturn['directHunterObjectFromService'] = Map<String, dynamic>.from(hunterDataObjectAsMap);
    } else {
      mapToReturn['directHunterObjectFromService'] = null;
    }

    print('ApiService ($operation): MAPA FINAL A RETORNAR: $mapToReturn');
    print('ApiService ($operation): MAPA FINAL A RETORNAR - Valor de "data": ${mapToReturn['data']}');
    print('ApiService ($operation): MAPA FINAL A RETORNAR - Valor de "directHunterObjectFromService": ${mapToReturn['directHunterObjectFromService']}');

    return mapToReturn;

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