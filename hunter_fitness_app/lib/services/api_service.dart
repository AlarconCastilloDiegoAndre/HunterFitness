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

      if (response.statusCode == 200 && responseData['success'] == true) { // Exitoso login desde API
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
           print('ApiService: Login exitoso pero token no encontrado o data es null.');
          return {
            'success': false,
            'message': responseData['message'] ?? 'Login exitoso pero respuesta inesperada.'
          };
        }
      } else if (response.statusCode == 201 && responseData['success'] == true) { // Exitoso registro desde API (código 201 Created)
        final apiResponseData = responseData['data'];
         if (apiResponseData != null && apiResponseData['token'] != null) {
          String token = apiResponseData['token'];
          await _storage.write(key: 'jwt_token', value: token);
          print('ApiService: Registro exitoso, token guardado.');
          return {
            'success': true,
            'message': apiResponseData['message'] ?? 'Registration successful!',
            'token': token,
            'hunter': apiResponseData['hunter']
          };
        } else {
          print('ApiService: Registro exitoso pero token no encontrado o data es null.');
          return {
            'success': false,
            'message': responseData['message'] ?? 'Registro exitoso pero respuesta inesperada.'
          };
        }
      }
      else {
        print('ApiService: Login/Registro fallido - StatusCode: ${response.statusCode}, Mensaje API: ${responseData['message']}');
        return {
          'success': false,
          'message': responseData['message'] ?? 'Operación fallida con estado: ${response.statusCode}'
        };
      }
    } catch (e) {
      print('ApiService: Excepción en el método login/registro: ${e.toString()}');
      if (e is FormatException) {
        print('ApiService: Ocurrió un FormatException. Revisa el "LOGIN API Response Body (RAW)" impreso arriba.');
      }
      return {'success': false, 'message': 'Error conectando al servidor: ${e.toString()}'};
    }
  }

  // NUEVO MÉTODO PARA REGISTRO
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

      if ((response.statusCode == 201 || response.statusCode == 200) && responseData['success'] == true) {
        final apiResponseData = responseData['data']; // Los datos útiles están aquí

        if (apiResponseData != null && apiResponseData['token'] != null && apiResponseData['hunter'] != null) {
          String token = apiResponseData['token'];
          await _storage.write(key: 'jwt_token', value: token);
          print('ApiService: Registro exitoso y token guardado.');
          return {
            'success': true,
            // USA EL MENSAJE DE apiResponseData
            'message': apiResponseData['message'] ?? '¡Registro Exitoso! Token y datos de cazador recibidos.',
            'token': token,
            'hunter': apiResponseData['hunter']
          };
        } else if (apiResponseData != null && apiResponseData['message'] != null) {
           // Caso donde el registro es exitoso pero quizás no devuelve token (solo mensaje)
           print('ApiService: Registro exitoso, pero sin token/hunter en la respuesta principal de data.');
            return {
              'success': true,
              'message': apiResponseData['message'] // Usa el mensaje de 'data'
            };
        } else {
           print('ApiService: Registro exitoso (201/200) pero la estructura de "data" es inesperada o faltan campos.');
           return {
            'success': true, // El registro fue exitoso a nivel de API
            'message': responseData['message'] ?? 'Registro completado, pero respuesta con formato inesperado.'
          };
        }
      } else {
         // Usa responseData['message'] si está disponible, sino el mensaje genérico
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