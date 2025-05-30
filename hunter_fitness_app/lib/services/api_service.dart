import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class ApiService {
  static const String _baseUrl = "https://hunter-fitness-api.azurewebsites.net/api";
  final _storage = const FlutterSecureStorage();

  Future<Map<String, dynamic>> login(String username, String password) async {
    final Uri loginUrl = Uri.parse('$_baseUrl/auth/login');
    print('ApiService: Intentando login para usuario: $username');

    try {
      final response = await http.post(
        loginUrl,
        headers: <String, String>{
          'Content-Type': 'application/json; charset=UTF-8',
        },
        body: jsonEncode(<String, String>{
          'username': username,
          'password': password,
        }),
      );

      print('LOGIN API Status Code: ${response.statusCode}');
      print('LOGIN API Response Body (RAW): "${response.body}"');

      if (response.body.isEmpty) {
        print('LOGIN API Error: El cuerpo de la respuesta está vacío.');
        return {'success': false, 'message': 'El servidor devolvió una respuesta vacía.'};
      }

      final responseData = jsonDecode(response.body);

      // El login exitoso debería devolver 200 OK
      if (response.statusCode == 200 && responseData['success'] == true) {
        final apiResponseData = responseData['data'];
        if (apiResponseData != null && apiResponseData['token'] != null) {
          String token = apiResponseData['token'];
          await _storage.write(key: 'jwt_token', value: token);
          print('ApiService: Login exitoso, token guardado.');
          return {
            'success': true,
            'message': apiResponseData['message'] ?? '¡Login Exitoso!',
            'token': token,
            'hunter': apiResponseData['hunter']
          };
        } else {
           print('ApiService: Login exitoso pero token/data no encontrado o es null.');
          return {
            'success': false, // Considerarlo fallo si no hay token
            'message': responseData['message'] ?? 'Login exitoso pero respuesta inesperada del servidor.'
          };
        }
      } else {
        // Otros códigos de estado (ej. 401, 400) o success == false
        print('ApiService: Login fallido - StatusCode: ${response.statusCode}, Mensaje API: ${responseData['message']}');
        return {
          'success': false,
          'message': responseData['message'] ?? 'Login fallido con estado: ${response.statusCode}'
        };
      }
    } catch (e) {
      print('ApiService: Excepción en el método login: ${e.toString()}');
      if (e is FormatException) {
        print('ApiService: Ocurrió un FormatException en Login. Revisa el "LOGIN API Response Body (RAW)" impreso arriba.');
      }
      return {'success': false, 'message': 'Error conectando al servidor: ${e.toString()}'};
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
        headers: <String, String>{
          'Content-Type': 'application/json; charset=UTF-8',
        },
        body: jsonEncode(<String, String>{
          'username': username,
          'email': email,
          'password': password,
          'hunterName': hunterName,
        }),
      );

      print('REGISTER API Status Code: ${response.statusCode}');
      print('REGISTER API Response Body (RAW): "${response.body}"');

      if (response.body.isEmpty) {
        print('REGISTER API Error: El cuerpo de la respuesta está vacío.');
        return {'success': false, 'message': 'El servidor devolvió una respuesta vacía en el registro.'};
      }
      
      final responseData = jsonDecode(response.body);

      // El registro exitoso en tu API devuelve 201 Created (o 200 OK)
      if ((response.statusCode == 201 || response.statusCode == 200) && responseData['success'] == true) {
        final apiResponseData = responseData['data']; 
        
        if (apiResponseData != null) { 
          String successMessage = apiResponseData['message'] ?? responseData['message'] ?? '¡Registro Exitoso!';
          String? token = apiResponseData['token']; // Puede ser null si la API no devuelve token al registrar
          var hunterData = apiResponseData['hunter'];

          if (token != null) {
            await _storage.write(key: 'jwt_token', value: token);
            print('ApiService: Registro exitoso y token guardado.');
          } else {
            print('ApiService: Registro exitoso pero no se encontró token en la sub-respuesta "data".');
          }
           return {
            'success': true,
            'message': successMessage,
            'token': token, // Será null si no se devuelve
            'hunter': hunterData // Será null si no se devuelve
          };
        } else {
           // Esto pasaría si responseData['data'] es null, pero responseData['success'] es true
           print('ApiService: Registro exitoso (201/200) pero el objeto "data" en la respuesta es null o inesperado.');
           return {
            'success': true, 
            'message': responseData['message'] ?? 'Registro completado, pero respuesta con formato inesperado.'
          };
        }
      } else {
         print('ApiService: Registro fallido - StatusCode: ${response.statusCode}, Mensaje API: ${responseData['message']}');
        return {
          'success': false,
          'message': responseData['message'] ?? 'Registro fallido con estado: ${response.statusCode}'
        };
      }
    } catch (e) {
      print('ApiService: Excepción en registerUser: ${e.toString()}');
       if (e is FormatException) {
        print('ApiService: Ocurrió un FormatException en Registro. Revisa el "REGISTER API Response Body (RAW)" impreso arriba.');
      }
      return {'success': false, 'message': 'Error conectando al servidor: ${e.toString()}'};
    }
  }

  Future<String?> getToken() async {
    return await _storage.read(key: 'jwt_token');
  }

  Future<void> logout() async {
    await _storage.delete(key: 'jwt_token');
  }
}
