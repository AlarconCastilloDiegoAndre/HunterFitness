import 'dart:convert';
import 'package:http/http.dart' as http;

class ApiService {
  // URL de tu API de Azure Functions
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
  
  // Login
  Future<Map<String, dynamic>> login(String username, String password) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/LoginHunter'),
        headers: headers,
        body: json.encode({
          'username': username,
          'password': password,
        }),
      ).timeout(const Duration(seconds: 15));
      
      if (response.statusCode == 200) {
        final responseData = json.decode(response.body);
        if (responseData['success'] == true) {
          return responseData;
        } else {
          throw Exception(responseData['message'] ?? 'Login failed');
        }
      } else {
        throw Exception('Login failed: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Login error: $e');
    }
  }
  
  // Register
  Future<Map<String, dynamic>> register(String username, String email, String password, String hunterName) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/RegisterHunter'),
        headers: headers,
        body: json.encode({
          'username': username,
          'email': email,
          'password': password,
          'hunterName': hunterName,
        }),
      ).timeout(const Duration(seconds: 15));
      
      if (response.statusCode == 200 || response.statusCode == 201) {
        final responseData = json.decode(response.body);
        if (responseData['success'] == true) {
          return responseData;
        } else {
          throw Exception(responseData['message'] ?? 'Registration failed');
        }
      } else {
        throw Exception('Registration failed: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Registration error: $e');
    }
  }
  
  // Get Hunter Profile
  Future<Map<String, dynamic>> getHunterProfile(String hunterId) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/GetHunterProfile?hunterId=$hunterId'),
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
  
  // Get Daily Quests
  Future<List<Map<String, dynamic>>> getDailyQuests(String hunterId) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/GetDailyQuests?hunterId=$hunterId'),
        headers: headers,
      ).timeout(const Duration(seconds: 10));
      
      if (response.statusCode == 200) {
        final responseData = json.decode(response.body);
        if (responseData['success'] == true) {
          return List<Map<String, dynamic>>.from(responseData['data']);
        } else {
          throw Exception(responseData['message'] ?? 'Failed to get quests');
        }
      } else {
        throw Exception('Failed to get quests: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Quests error: $e');
    }
  }
  
  // Start Quest
  Future<Map<String, dynamic>> startQuest(String hunterId, String questId) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/StartQuest'),
        headers: headers,
        body: json.encode({
          'hunterId': hunterId,
          'questId': questId,
        }),
      ).timeout(const Duration(seconds: 10));
      
      if (response.statusCode == 200) {
        final responseData = json.decode(response.body);
        if (responseData['success'] == true) {
          return responseData;
        } else {
          throw Exception(responseData['message'] ?? 'Failed to start quest');
        }
      } else {
        throw Exception('Failed to start quest: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Start quest error: $e');
    }
  }
  
  // Complete Quest
  Future<Map<String, dynamic>> completeQuest(String hunterId, String questId, Map<String, dynamic> questData) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/CompleteQuest'),
        headers: headers,
        body: json.encode({
          'hunterId': hunterId,
          'questId': questId,
          'currentReps': questData['currentReps'] ?? 0,
          'currentSets': questData['currentSets'] ?? 0,
          'currentDuration': questData['currentDuration'] ?? 0,
          'currentDistance': questData['currentDistance'] ?? 0.0,
          'perfectExecution': questData['perfectExecution'] ?? false,
        }),
      ).timeout(const Duration(seconds: 15));
      
      if (response.statusCode == 200) {
        final responseData = json.decode(response.body);
        if (responseData['success'] == true) {
          return responseData;
        } else {
          throw Exception(responseData['message'] ?? 'Failed to complete quest');
        }
      } else {
        throw Exception('Failed to complete quest: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Complete quest error: $e');
    }
  }
  
  // Get Available Dungeons
  Future<List<Map<String, dynamic>>> getAvailableDungeons(String hunterId) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/GetAvailableDungeons?hunterId=$hunterId'),
        headers: headers,
      ).timeout(const Duration(seconds: 10));
      
      if (response.statusCode == 200) {
        final responseData = json.decode(response.body);
        if (responseData['success'] == true) {
          return List<Map<String, dynamic>>.from(responseData['data']);
        } else {
          throw Exception(responseData['message'] ?? 'Failed to get dungeons');
        }
      } else {
        throw Exception('Failed to get dungeons: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Dungeons error: $e');
    }
  }
  
  // Get Leaderboard
  Future<List<Map<String, dynamic>>> getLeaderboard() async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/GetLeaderboard'),
        headers: headers,
      ).timeout(const Duration(seconds: 10));
      
      if (response.statusCode == 200) {
        final responseData = json.decode(response.body);
        if (responseData['success'] == true) {
          return List<Map<String, dynamic>>.from(responseData['data']);
        } else {
          throw Exception(responseData['message'] ?? 'Failed to get leaderboard');
        }
      } else {
        throw Exception('Failed to get leaderboard: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Leaderboard error: $e');
    }
  }
  
  // Get Hunter Equipment
  Future<List<Map<String, dynamic>>> getHunterEquipment(String hunterId) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/GetHunterInventory?hunterId=$hunterId'),
        headers: headers,
      ).timeout(const Duration(seconds: 10));
      
      if (response.statusCode == 200) {
        final responseData = json.decode(response.body);
        if (responseData['success'] == true) {
          return List<Map<String, dynamic>>.from(responseData['data']);
        } else {
          throw Exception(responseData['message'] ?? 'Failed to get equipment');
        }
      } else {
        throw Exception('Failed to get equipment: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Equipment error: $e');
    }
  }
}