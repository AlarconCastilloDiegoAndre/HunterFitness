import 'dart:convert';
import 'package:http/http.dart' as http;

class ApiService {
  // URL de tu API local
  static const String baseUrl = 'http://localhost:7207/api';
  
  // Headers comunes
  Map<String, String> get headers => {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  };
  
  // Test connection usando el health check
  Future<Map<String, dynamic>> testConnection() async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/health'),
        headers: headers,
      ).timeout(const Duration(seconds: 10));
      
      if (response.statusCode == 200) {
        return json.decode(response.body);
      } else {
        throw Exception('Health check failed: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Connection error: $e');
    }
  }
  
  // Login con debug completo
  Future<Map<String, dynamic>> login(String username, String password) async {
    try {
      final url = '$baseUrl/auth/login';
      final requestBody = {
        'username': username,
        'password': password,
      };
      
      print('🚀 LOGIN DEBUG:');
      print('   URL: $url');
      print('   Username: $username');
      print('   Password length: ${password.length}');
      print('   Request body: ${json.encode(requestBody)}');
      print('   Headers: $headers');
      
      final response = await http.post(
        Uri.parse(url),
        headers: headers,
        body: json.encode(requestBody),
      ).timeout(const Duration(seconds: 15));
      
      print('📡 RESPONSE DEBUG:');
      print('   Status code: ${response.statusCode}');
      print('   Response headers: ${response.headers}');
      print('   Response body: ${response.body}');
      
      if (response.statusCode == 200) {
        final responseData = json.decode(response.body);
        print('✅ Login successful: $responseData');
        
        if (responseData['success'] == true) {
          return responseData;
        } else {
          throw Exception(responseData['message'] ?? 'Login failed - success is false');
        }
      } else {
        print('❌ Login failed with status: ${response.statusCode}');
        throw Exception('Login failed: ${response.statusCode} - ${response.body}');
      }
    } catch (e) {
      print('💥 Login exception: $e');
      throw Exception('Login error: $e');
    }
  }
  
  // Register con debug
  Future<Map<String, dynamic>> register(String username, String email, String password, String hunterName) async {
    try {
      final url = '$baseUrl/auth/register';
      final requestBody = {
        'username': username,
        'email': email,
        'password': password,
        'hunterName': hunterName,
      };
      
      print('🚀 REGISTER DEBUG:');
      print('   URL: $url');
      print('   Request body: ${json.encode(requestBody)}');
      
      final response = await http.post(
        Uri.parse(url),
        headers: headers,
        body: json.encode(requestBody),
      ).timeout(const Duration(seconds: 15));
      
      print('📡 REGISTER RESPONSE:');
      print('   Status code: ${response.statusCode}');
      print('   Response body: ${response.body}');
      
      if (response.statusCode == 200 || response.statusCode == 201) {
        final responseData = json.decode(response.body);
        if (responseData['success'] == true) {
          return responseData;
        } else {
          throw Exception(responseData['message'] ?? 'Registration failed');
        }
      } else {
        throw Exception('Registration failed: ${response.statusCode} - ${response.body}');
      }
    } catch (e) {
      print('💥 Registration exception: $e');
      throw Exception('Registration error: $e');
    }
  }
  
  // Get Hunter Profile
  Future<Map<String, dynamic>> getHunterProfile(String hunterId) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/hunters/profile?hunterId=$hunterId'),
        headers: headers,
      ).timeout(const Duration(seconds: 10));
      
      if (response.statusCode == 200) {
        final responseData = json.decode(response.body);
        if (responseData['success'] == true) {
          return responseData['data'];
        } else {
          throw Exception(responseData['message'] ?? 'Failed to get profile');
        }
      } else {
        throw Exception('Failed to get profile: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Profile error: $e');
    }
  }
  
  // Resto de métodos simplificados para el debug
  Future<List<Map<String, dynamic>>> getDailyQuests(String hunterId) async {
    return [];
  }
  
  Future<Map<String, dynamic>> startQuest(String hunterId, String questId) async {
    return {'success': true};
  }
  
  Future<Map<String, dynamic>> completeQuest(String hunterId, String questId, Map<String, dynamic> questData) async {
    return {'success': true};
  }
  
  Future<List<Map<String, dynamic>>> getAvailableDungeons(String hunterId) async {
    return [];
  }
  
  Future<List<Map<String, dynamic>>> getLeaderboard() async {
    return [];
  }
  
  Future<List<Map<String, dynamic>>> getHunterEquipment(String hunterId) async {
    return [];
  }
}