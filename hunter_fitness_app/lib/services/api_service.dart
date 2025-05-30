import 'dart:convert';
import 'dart:io';
import 'package:http/http.dart' as http;
import '../models/api_response.dart';
import '../utils/constants.dart';
import 'storage_service.dart';

class ApiService {
  static final ApiService _instance = ApiService._internal();
  factory ApiService() => _instance;
  ApiService._internal();

  final http.Client _client = http.Client();
  final StorageService _storage = StorageService();

  // Headers base para todas las requests
  Map<String, String> get _baseHeaders => {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
    'User-Agent': '${AppConstants.appName}/${AppConstants.appVersion}',
  };

  // Headers con autorización
  Future<Map<String, String>> get _authHeaders async {
    final headers = Map<String, String>.from(_baseHeaders);
    
    final token = await _storage.getAuthToken();
    if (token != null) {
      headers['Authorization'] = 'Bearer $token';
    }
    
    return headers;
  }

  // Método GET genérico
  Future<ApiResponse<T>> get<T>(
    String endpoint, {
    Map<String, String>? queryParams,
    bool requiresAuth = false,
    T Function(dynamic)? fromJson,
  }) async {
    try {
      final uri = _buildUri(endpoint, queryParams);
      final headers = requiresAuth ? await _authHeaders : _baseHeaders;

      if (AppConfig.enableLogging) {
        print('🌐 GET Request: $uri');
        print('📋 Headers: $headers');
      }

      final response = await _client
          .get(uri, headers: headers)
          .timeout(Duration(milliseconds: AppConstants.networkTimeout));

      return _handleResponse<T>(response, fromJson);
    } on SocketException {
      return ApiResponse.networkError();
    } on HttpException {
      return ApiResponse.serverError();
    } on FormatException {
      return ApiResponse.error('Invalid response format');
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ GET Error: $e');
      }
      return ApiResponse.error('Unexpected error: ${e.toString()}');
    }
  }

  // Método POST genérico
  Future<ApiResponse<T>> post<T>(
    String endpoint, {
    Map<String, dynamic>? body,
    Map<String, String>? queryParams,
    bool requiresAuth = false,
    T Function(dynamic)? fromJson,
  }) async {
    try {
      final uri = _buildUri(endpoint, queryParams);
      final headers = requiresAuth ? await _authHeaders : _baseHeaders;
      final bodyJson = body != null ? jsonEncode(body) : null;

      if (AppConfig.enableLogging) {
        print('🌐 POST Request: $uri');
        print('📋 Headers: $headers');
        print('📦 Body: $bodyJson');
      }

      final response = await _client
          .post(uri, headers: headers, body: bodyJson)
          .timeout(Duration(milliseconds: AppConstants.networkTimeout));

      return _handleResponse<T>(response, fromJson);
    } on SocketException {
      return ApiResponse.networkError();
    } on HttpException {
      return ApiResponse.serverError();
    } on FormatException {
      return ApiResponse.error('Invalid response format');
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ POST Error: $e');
      }
      return ApiResponse.error('Unexpected error: ${e.toString()}');
    }
  }

  // Método PUT genérico
  Future<ApiResponse<T>> put<T>(
    String endpoint, {
    Map<String, dynamic>? body,
    Map<String, String>? queryParams,
    bool requiresAuth = false,
    T Function(dynamic)? fromJson,
  }) async {
    try {
      final uri = _buildUri(endpoint, queryParams);
      final headers = requiresAuth ? await _authHeaders : _baseHeaders;
      final bodyJson = body != null ? jsonEncode(body) : null;

      if (AppConfig.enableLogging) {
        print('🌐 PUT Request: $uri');
        print('📋 Headers: $headers');
        print('📦 Body: $bodyJson');
      }

      final response = await _client
          .put(uri, headers: headers, body: bodyJson)
          .timeout(Duration(milliseconds: AppConstants.networkTimeout));

      return _handleResponse<T>(response, fromJson);
    } on SocketException {
      return ApiResponse.networkError();
    } on HttpException {
      return ApiResponse.serverError();
    } on FormatException {
      return ApiResponse.error('Invalid response format');
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ PUT Error: $e');
      }
      return ApiResponse.error('Unexpected error: ${e.toString()}');
    }
  }

  // Método DELETE genérico
  Future<ApiResponse<T>> delete<T>(
    String endpoint, {
    Map<String, String>? queryParams,
    bool requiresAuth = false,
    T Function(dynamic)? fromJson,
  }) async {
    try {
      final uri = _buildUri(endpoint, queryParams);
      final headers = requiresAuth ? await _authHeaders : _baseHeaders;

      if (AppConfig.enableLogging) {
        print('🌐 DELETE Request: $uri');
        print('📋 Headers: $headers');
      }

      final response = await _client
          .delete(uri, headers: headers)
          .timeout(Duration(milliseconds: AppConstants.networkTimeout));

      return _handleResponse<T>(response, fromJson);
    } on SocketException {
      return ApiResponse.networkError();
    } on HttpException {
      return ApiResponse.serverError();
    } on FormatException {
      return ApiResponse.error('Invalid response format');
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ DELETE Error: $e');
      }
      return ApiResponse.error('Unexpected error: ${e.toString()}');
    }
  }

  // Método para health check
  Future<ApiResponse<Map<String, dynamic>>> healthCheck() async {
    return get<Map<String, dynamic>>(
      ApiConstants.healthEndpoint,
      fromJson: (data) => data as Map<String, dynamic>,
    );
  }

  // Método para ping
  Future<ApiResponse<Map<String, dynamic>>> ping() async {
    return get<Map<String, dynamic>>(
      ApiConstants.pingEndpoint,
      fromJson: (data) => data as Map<String, dynamic>,
    );
  }

  // Método para verificar conectividad
  Future<bool> checkConnectivity() async {
    try {
      final response = await ping();
      return response.success;
    } catch (e) {
      return false;
    }
  }

  // Construir URI con query parameters
  Uri _buildUri(String endpoint, Map<String, String>? queryParams) {
    final baseUri = Uri.parse('${ApiConstants.baseUrl}$endpoint');
    
    if (queryParams != null && queryParams.isNotEmpty) {
      return baseUri.replace(queryParameters: queryParams);
    }
    
    return baseUri;
  }

  // Manejar la respuesta HTTP
  ApiResponse<T> _handleResponse<T>(
    http.Response response,
    T Function(dynamic)? fromJson,
  ) {
    if (AppConfig.enableLogging) {
      print('📥 Response Status: ${response.statusCode}');
      print('📄 Response Body: ${response.body}');
    }

    try {
      final Map<String, dynamic> jsonData = jsonDecode(response.body);

      switch (response.statusCode) {
        case 200:
        case 201:
          return ApiResponse.fromJson(jsonData, fromJson);
        
        case 400:
          return _handleBadRequest<T>(jsonData);
        
        case 401:
          return _handleUnauthorized<T>(jsonData);
        
        case 403:
          return ApiResponse.error(
            jsonData['message'] ?? 'Access forbidden',
            errors: List<String>.from(jsonData['errors'] ?? []),
          );
        
        case 404:
          return ApiResponse.notFound(
            jsonData['message'] ?? 'Resource not found',
          );
        
        case 429:
          return ApiResponse.error(
            'Too many requests. Please try again later.',
          );
        
        case 500:
        case 502:
        case 503:
        case 504:
          return ApiResponse.serverError(
            jsonData['message'] ?? 'Server error',
          );
        
        default:
          return ApiResponse.error(
            'Unexpected error (${response.statusCode})',
          );
      }
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Response parsing error: $e');
      }
      
      // Si no se puede parsear JSON, pero la respuesta fue exitosa
      if (response.statusCode >= 200 && response.statusCode < 300) {
        return ApiResponse.success(null as T, message: 'Success');
      }
      
      return ApiResponse.error('Invalid response format');
    }
  }

  // Manejar error 400 (Bad Request)
  ApiResponse<T> _handleBadRequest<T>(Map<String, dynamic> jsonData) {
    final errors = List<String>.from(jsonData['errors'] ?? []);
    final message = jsonData['message'] ?? 'Bad request';
    
    if (errors.isNotEmpty) {
      return ApiResponse.validationError(errors);
    }
    
    return ApiResponse.error(message);
  }

  // Manejar error 401 (Unauthorized)
  ApiResponse<T> _handleUnauthorized<T>(Map<String, dynamic> jsonData) {
    // Limpiar token si está expirado o es inválido
    _storage.clearAuthToken();
    
    return ApiResponse.unauthorized(
      jsonData['message'] ?? 'Authentication required',
    );
  }

  // Método para retry con backoff exponencial
  Future<ApiResponse<T>> _retryWithBackoff<T>(
    Future<ApiResponse<T>> Function() operation, {
    int maxRetries = AppConstants.maxRetries,
  }) async {
    int attempt = 0;
    
    while (attempt < maxRetries) {
      try {
        final result = await operation();
        
        // Si fue exitoso o es un error no recuperable, retornar
        if (result.success || !_isRetryableError(result)) {
          return result;
        }
        
        attempt++;
        
        if (attempt < maxRetries) {
          // Esperar antes del siguiente intento (backoff exponencial)
          final delay = Duration(milliseconds: 1000 * (1 << attempt));
          await Future.delayed(delay);
        }
      } catch (e) {
        attempt++;
        
        if (attempt >= maxRetries) {
          return ApiResponse.error('Max retries exceeded: ${e.toString()}');
        }
        
        // Esperar antes del siguiente intento
        final delay = Duration(milliseconds: 1000 * (1 << attempt));
        await Future.delayed(delay);
      }
    }
    
    return ApiResponse.error('Max retries exceeded');
  }

  // Verificar si un error es recuperable (para retry)
  bool _isRetryableError<T>(ApiResponse<T> response) {
    return response.errors.any((error) => 
        error == 'NETWORK_ERROR' || 
        error == 'TIMEOUT_ERROR' || 
        error == 'SERVER_ERROR'
    );
  }

  // Limpiar recursos
  void dispose() {
    _client.close();
  }
}

// Extensión para facilitar el uso del ApiService
extension ApiServiceExtension on ApiService {
  // Métodos con retry automático
  Future<ApiResponse<T>> getWithRetry<T>(
    String endpoint, {
    Map<String, String>? queryParams,
    bool requiresAuth = false,
    T Function(dynamic)? fromJson,
  }) {
    return _retryWithBackoff(() => get<T>(
      endpoint,
      queryParams: queryParams,
      requiresAuth: requiresAuth,
      fromJson: fromJson,
    ));
  }

  Future<ApiResponse<T>> postWithRetry<T>(
    String endpoint, {
    Map<String, dynamic>? body,
    Map<String, String>? queryParams,
    bool requiresAuth = false,
    T Function(dynamic)? fromJson,
  }) {
    return _retryWithBackoff(() => post<T>(
      endpoint,
      body: body,
      queryParams: queryParams,
      requiresAuth: requiresAuth,
      fromJson: fromJson,
    ));
  }
}