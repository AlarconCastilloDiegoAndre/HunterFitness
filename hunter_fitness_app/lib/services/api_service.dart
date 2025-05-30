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

      // ----- IMPRESIONES DE DEBUG CRUCIALES -----
      print('LOGIN API Status Code: ${response.statusCode}');
      print('LOGIN API Response Body (RAW): "${response.body}"'); // ESTA LÍNEA ES MUY IMPORTANTE
      // ----- FIN DE IMPRESIONES DE DEBUG -----

      if (response.body.isEmpty) {
        print('LOGIN API Error: El cuerpo de la respuesta está vacío.');
        return {'success': false, 'message': 'El servidor devolvió una respuesta vacía.'};
      }

      // Esta es la línea que probablemente está causando el FormatException
      // si response.body no es un JSON válido.
      final responseData = jsonDecode(response.body);

      if (response.statusCode == 200 && responseData['success'] == true) {
        final apiResponseData = responseData['data'];
        if (apiResponseData != null && apiResponseData['token'] != null) {
          String token = apiResponseData['token'];
          await _storage.write(key: 'jwt_token', value: token);
          print('ApiService: Login exitoso, token guardado.');
          return {
            'success': true,
            'message': apiResponseData['message'] ?? 'Login successful!',
            'token': token,
            'hunter': apiResponseData['hunter']
          };
        } else {
          print('ApiService: Login fallido - Token no encontrado o data es null.');
          return {
            'success': false,
            'message': responseData['message'] ?? 'Login failed: Token not found in response.'
          };
        }
      } else {
        print('ApiService: Login fallido - StatusCode: ${response.statusCode}, Mensaje API: ${responseData['message'] ?? "No message from API"}');
        return {
          'success': false,
          'message': responseData['message'] ?? 'Login fallido con estado: ${response.statusCode}'
        };
      }
    } catch (e) {
      print('ApiService: Excepción en el método login: ${e.toString()}');
      if (e is FormatException) {
        print('ApiService: Ocurrió un FormatException. Revisa el "LOGIN API Response Body (RAW)" impreso arriba.');
      }
      return {'success': false, 'message': 'Error conectando al servidor: ${e.toString()}'};
    }
  }

  // ... resto de tus métodos de ApiService (getToken, logout, etc.) ...
}