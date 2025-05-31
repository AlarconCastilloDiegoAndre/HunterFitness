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

    Map<String, dynamic> responseData;
    try {
      var decodedJson = jsonDecode(rawResponseBody);
      if (decodedJson is Map<String, dynamic>) {
        responseData = decodedJson;
        print('$operation - jsonDecode exitoso. responseData es un Map.');
      } else {
        print('$operation API Error: El JSON decodificado no es un Mapa. Tipo actual: ${decodedJson.runtimeType}');
        return {'success': false, 'message': 'Respuesta del servidor con formato inesperado (no es un objeto JSON).'};
      }
    } catch (e) {
      print('$operation API Error: Fallo al decodificar JSON. Error: $e. Cuerpo: "$rawResponseBody"');
      return {'success': false, 'message': 'Respuesta inesperada del servidor (formato incorrecto).'};
    }
    
    try {
      bool httpSuccess = (response.statusCode == 200 || response.statusCode == 201);
      bool apiDeclaredSuccess = (responseData['success'] == true); 
      
      print('$operation - httpSuccess: $httpSuccess');
      print('$operation - apiDeclaredSuccess (evaluado como responseData["success"] == true): $apiDeclaredSuccess');

      bool combinedSuccessCondition = httpSuccess && apiDeclaredSuccess;
      print('$operation - combinedSuccessCondition (httpSuccess && apiDeclaredSuccess): $combinedSuccessCondition');

      Map<String, dynamic>? dataPayload;
      var dataField = responseData['data'];
      if (dataField is Map<String, dynamic>) {
          dataPayload = dataField;
          print('$operation - responseData["data"] SÍ es un Map<String, dynamic>.');
      } else {
          print('$operation - responseData["data"] NO es un Map<String, dynamic> (tipo actual: ${dataField?.runtimeType}) o es null.');
      }
      
      String messageForUi;
      if (apiDeclaredSuccess) { 
        messageForUi = dataPayload?['message'] as String? ?? 
                       responseData['message'] as String? ?? 
                       'Operación completada con éxito.';
      } else { 
        messageForUi = responseData['message'] as String? ?? 
                       'La operación falló (código API: ${response.statusCode}).';
      }
      print('$operation - messageForUi determinado: "$messageForUi"');

      if (combinedSuccessCondition) {
        print('$operation - ENTRANDO AL BLOQUE DE ÉXITO LÓGICO.');
        String? token = dataPayload?['token'] as String?;
        var hunterData = dataPayload?['hunter'];
        String finalSuccessMessage = messageForUi;

        if (token != null) {
          try {
            await _storage.write(key: 'jwt_token', value: token);
            print('ApiService ($operation): Token guardado. Mensaje: "$finalSuccessMessage"');
          } catch (storageError) {
            print('ApiService ($operation): ÉXITO API, PERO FALLÓ AL GUARDAR TOKEN. Error: $storageError. Mensaje: "$finalSuccessMessage"');
            finalSuccessMessage = '$messageForUi (Advertencia: Token no guardado localmente)';
          }
        } else {
          print('ApiService ($operation): Éxito API, no se encontró token. Mensaje: "$finalSuccessMessage"');
        }
        
        Map<String, dynamic> successResult = {
          'success': true,
          'message': finalSuccessMessage,
          'token': token,
          'hunter': hunterData
        };
        print('--- FIN _handleApiResponse (PREPARADO PARA RETORNAR ÉXITO): $successResult ---');
        return successResult;

      } else {
        print('$operation - ENTRANDO AL BLOQUE DE FALLO LÓGICO.');
        String finalErrorMessage = messageForUi; 
        if (!apiDeclaredSuccess && (responseData['message'] as String?)?.isNotEmpty == true) {
            finalErrorMessage = responseData['message'] as String;
        } else if (!apiDeclaredSuccess) {
            finalErrorMessage = 'La API reportó un fallo (código: ${response.statusCode}).';
        } else if (!httpSuccess) { // httpSuccess es false pero apiDeclaredSuccess podría ser true (raro)
            finalErrorMessage = 'Error de comunicación (código: ${response.statusCode}).';
        }

        Map<String, dynamic> failureResult = {
          'success': false,
          'message': finalErrorMessage 
        };
        print('ApiService ($operation): Fallido LÓGICO. StatusCode: ${response.statusCode}. API success flag: $apiDeclaredSuccess. Retornando: $failureResult');
        print('--- FIN _handleApiResponse (RETORNANDO FALLO LÓGICO) para: $operation ---');
        return failureResult;
      }
    } catch (e, s) { 
      print('$operation API Error: Excepción INESPERADA dentro de _handleApiResponse después de jsonDecode. Error: $e');
      print('$operation API StackTrace: $s');
      return {'success': false, 'message': 'Error interno crítico procesando la respuesta del servidor.'};
    }
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