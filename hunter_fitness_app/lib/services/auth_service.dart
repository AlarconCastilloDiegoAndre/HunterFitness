import '../models/api_response.dart';
import '../models/auth_response.dart';
import '../models/hunter.dart';
import '../utils/constants.dart';
import 'api_service.dart';
import 'storage_service.dart';

class AuthService {
  static final AuthService _instance = AuthService._internal();
  factory AuthService() => _instance;
  AuthService._internal();

  final ApiService _apiService = ApiService();
  final StorageService _storageService = StorageService();

  // ============================
  // MÉTODOS DE AUTENTICACIÓN
  // ============================

  /// Login del usuario
  Future<ApiResponse<AuthResponse>> login(String username, String password) async {
    try {
      if (AppConfig.enableLogging) {
        print('🔐 Attempting login for: $username');
      }

      // Validar parámetros
      if (username.trim().isEmpty || password.isEmpty) {
        return ApiResponse.error('Username and password are required');
      }

      // Crear request
      final loginRequest = LoginRequest(
        username: username.trim(),
        password: password,
      );

      // Llamar a la API
      final response = await _apiService.post<Map<String, dynamic>>(
        ApiConstants.loginEndpoint,
        body: loginRequest.toJson(),
        fromJson: (data) => data as Map<String, dynamic>,
      );

      // Si la respuesta de la API no fue exitosa
      if (!response.success) {
        return ApiResponse<AuthResponse>(
          success: false,
          message: response.message,
          errors: response.errors,
        );
      }

      // Parsear respuesta de autenticación
      final authResponse = AuthResponse.fromJson(response.data!);

      // Si la autenticación no fue exitosa
      if (!authResponse.success) {
        return ApiResponse<AuthResponse>(
          success: false,
          message: authResponse.message,
        );
      }

      // Guardar token y perfil si la autenticación fue exitosa
      if (authResponse.isAuthenticated) {
        await _storageService.saveAuthToken(authResponse.token!);
        await _storageService.saveHunterProfile(authResponse.hunter!);
        
        if (AppConfig.enableLogging) {
          print('✅ Login successful for: ${authResponse.hunter!.hunterName}');
        }
      }

      return ApiResponse.success(authResponse);
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Login error: $e');
      }
      return ApiResponse.error('Login failed: ${e.toString()}');
    }
  }

  /// Registro de nuevo usuario
  Future<ApiResponse<AuthResponse>> register(
    String username,
    String email,
    String password,
    String hunterName,
  ) async {
    try {
      if (AppConfig.enableLogging) {
        print('📝 Attempting registration for: $username');
      }

      // Validar parámetros
      if (username.trim().isEmpty || 
          email.trim().isEmpty || 
          password.isEmpty || 
          hunterName.trim().isEmpty) {
        return ApiResponse.error('All fields are required');
      }

      // Crear request
      final registerRequest = RegisterRequest(
        username: username.trim(),
        email: email.trim(),
        password: password,
        hunterName: hunterName.trim(),
      );

      // Llamar a la API
      final response = await _apiService.post<Map<String, dynamic>>(
        ApiConstants.registerEndpoint,
        body: registerRequest.toJson(),
        fromJson: (data) => data as Map<String, dynamic>,
      );

      // Si la respuesta de la API no fue exitosa
      if (!response.success) {
        return ApiResponse<AuthResponse>(
          success: false,
          message: response.message,
          errors: response.errors,
        );
      }

      // Parsear respuesta de autenticación
      final authResponse = AuthResponse.fromJson(response.data!);

      // Si el registro no fue exitoso
      if (!authResponse.success) {
        return ApiResponse<AuthResponse>(
          success: false,
          message: authResponse.message,
        );
      }

      // Guardar token y perfil si el registro fue exitoso
      if (authResponse.isAuthenticated) {
        await _storageService.saveAuthToken(authResponse.token!);
        await _storageService.saveHunterProfile(authResponse.hunter!);
        await _storageService.setNotFirstTime();
        
        if (AppConfig.enableLogging) {
          print('✅ Registration successful for: ${authResponse.hunter!.hunterName}');
        }
      }

      return ApiResponse.success(authResponse);
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Registration error: $e');
      }
      return ApiResponse.error('Registration failed: ${e.toString()}');
    }
  }

  /// Logout del usuario
  Future<bool> logout() async {
    try {
      if (AppConfig.enableLogging) {
        print('🚪 Logging out user...');
      }

      // Limpiar datos de autenticación locales
      final success = await _storageService.clearAuthData();
      
      // TODO: Si la API tiene endpoint de logout, llamarlo aquí
      // await _apiService.post(ApiConstants.logoutEndpoint, requiresAuth: true);

      if (AppConfig.enableLogging && success) {
        print('✅ Logout successful');
      }

      return success;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Logout error: $e');
      }
      return false;
    }
  }

  /// Validar token actual
  Future<ApiResponse<TokenValidationResponse>> validateToken() async {
    try {
      if (AppConfig.enableLogging) {
        print('🔍 Validating current token...');
      }

      final token = await _storageService.getAuthToken();
      if (token == null) {
        return ApiResponse<TokenValidationResponse>(
          success: false,
          message: 'No token found',
        );
      }

      // Llamar a la API para validar token
      final response = await _apiService.get<Map<String, dynamic>>(
        ApiConstants.validateTokenEndpoint,
        requiresAuth: true,
        fromJson: (data) => data as Map<String, dynamic>,
      );

      if (!response.success) {
        // Si el token no es válido, limpiar datos locales
        await _storageService.clearAuthData();
        return ApiResponse<TokenValidationResponse>(
          success: false,
          message: response.message,
          errors: response.errors,
        );
      }

      final validationResponse = TokenValidationResponse.fromJson(response.data!);

      if (AppConfig.enableLogging) {
        print('🔍 Token validation result: ${validationResponse.isValid}');
      }

      return ApiResponse.success(validationResponse);
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Token validation error: $e');
      }
      return ApiResponse.error('Token validation failed: ${e.toString()}');
    }
  }

  /// Refrescar token
  Future<ApiResponse<AuthResponse>> refreshToken() async {
    try {
      if (AppConfig.enableLogging) {
        print('🔄 Refreshing token...');
      }

      final currentToken = await _storageService.getAuthToken();
      if (currentToken == null) {
        return ApiResponse<AuthResponse>(
          success: false,
          message: 'No token to refresh',
        );
      }

      // Llamar a la API para refrescar token
      final response = await _apiService.post<Map<String, dynamic>>(
        ApiConstants.refreshTokenEndpoint,
        requiresAuth: true,
        fromJson: (data) => data as Map<String, dynamic>,
      );

      if (!response.success) {
        // Si no se puede refrescar, limpiar datos locales
        await _storageService.clearAuthData();
        return ApiResponse<AuthResponse>(
          success: false,
          message: response.message,
          errors: response.errors,
        );
      }

      final authResponse = AuthResponse.fromJson(response.data!);

      // Guardar nuevo token si es exitoso
      if (authResponse.isAuthenticated) {
        await _storageService.saveAuthToken(authResponse.token!);
        if (authResponse.hunter != null) {
          await _storageService.saveHunterProfile(authResponse.hunter!);
        }
        
        if (AppConfig.enableLogging) {
          print('✅ Token refreshed successfully');
        }
      }

      return ApiResponse.success(authResponse);
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Token refresh error: $e');
      }
      return ApiResponse.error('Token refresh failed: ${e.toString()}');
    }
  }

  // ============================
  // MÉTODOS DE VERIFICACIÓN
  // ============================

  /// Verificar si el usuario está autenticado
  Future<bool> isAuthenticated() async {
    try {
      return await _storageService.isLoggedIn();
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error checking authentication: $e');
      }
      return false;
    }
  }

  /// Obtener token actual
  Future<String?> getCurrentToken() async {
    return await _storageService.getAuthToken();
  }

  /// Obtener perfil del hunter actual
  Future<Hunter?> getCurrentHunter() async {
    return await _storageService.getHunterProfile();
  }

  /// Verificar si el token necesita ser refrescado
  Future<bool> shouldRefreshToken() async {
    try {
      final token = await _storageService.getAuthToken();
      if (token == null) return false;

      // TODO: Implementar lógica para verificar expiración del token
      // Por ahora, usar un tiempo fijo desde el último login
      final lastSync = await _storageService.getLastSyncTime();
      if (lastSync != null) {
        final timeDiff = DateTime.now().difference(lastSync);
        return timeDiff.inMilliseconds > AppConstants.tokenRefreshThreshold;
      }

      return false;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error checking token refresh: $e');
      }
      return false;
    }
  }

  // ============================
  // MÉTODOS DE PERFIL
  // ============================

  /// Obtener perfil actualizado del servidor
  Future<ApiResponse<Hunter>> getProfile() async {
    try {
      if (AppConfig.enableLogging) {
        print('👤 Fetching hunter profile...');
      }

      final response = await _apiService.get<Map<String, dynamic>>(
        ApiConstants.hunterProfileEndpoint,
        requiresAuth: true,
        fromJson: (data) => data as Map<String, dynamic>,
      );

      if (!response.success) {
        return ApiResponse<Hunter>(
          success: false,
          message: response.message,
          errors: response.errors,
        );
      }

      // La API devuelve { success, message, data: { hunter } }
      final responseData = response.data!;
      final hunterData = responseData['data'] as Map<String, dynamic>;
      final hunter = Hunter.fromJson(hunterData);

      // Guardar perfil actualizado
      await _storageService.saveHunterProfile(hunter);
      await _storageService.saveLastSyncTime(DateTime.now());

      if (AppConfig.enableLogging) {
        print('✅ Profile updated: ${hunter.hunterName}');
      }

      return ApiResponse.success(hunter);
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Get profile error: $e');
      }
      return ApiResponse.error('Failed to get profile: ${e.toString()}');
    }
  }

  /// Sincronizar datos del usuario
  Future<bool> syncUserData() async {
    try {
      if (AppConfig.enableLogging) {
        print('🔄 Syncing user data...');
      }

      // Verificar autenticación
      if (!await isAuthenticated()) {
        return false;
      }

      // Obtener perfil actualizado
      final profileResponse = await getProfile();
      if (!profileResponse.success) {
        return false;
      }

      // TODO: Sincronizar otros datos (quests, achievements, etc.)

      if (AppConfig.enableLogging) {
        print('✅ User data synced successfully');
      }

      return true;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Sync error: $e');
      }
      return false;
    }
  }

  // ============================
  // MÉTODOS DE UTILIDAD
  // ============================

  /// Verificar conectividad con la API
  Future<bool> checkApiConnectivity() async {
    try {
      return await _apiService.checkConnectivity();
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Connectivity check error: $e');
      }
      return false;
    }
  }

  /// Limpiar todos los datos de sesión
  Future<bool> clearSession() async {
    try {
      if (AppConfig.enableLogging) {
        print('🧹 Clearing session data...');
      }

      final success = await _storageService.clearAuthData();
      
      if (AppConfig.enableLogging && success) {
        print('✅ Session cleared successfully');
      }

      return success;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Clear session error: $e');
      }
      return false;
    }
  }

  /// Verificar si es la primera vez que se usa la app
  Future<bool> isFirstTimeUser() async {
    return await _storageService.isFirstTime();
  }

  /// Marcar onboarding como completado
  Future<bool> completeOnboarding() async {
    return await _storageService.setNotFirstTime();
  }

  /// Obtener información de la sesión actual
  Future<Map<String, dynamic>> getSessionInfo() async {
    try {
      final isAuth = await isAuthenticated();
      final hunter = await getCurrentHunter();
      final token = await getCurrentToken();
      final lastSync = await _storageService.getLastSyncTime();
      final isFirstTime = await isFirstTimeUser();

      return {
        'isAuthenticated': isAuth,
        'hasToken': token != null,
        'hasProfile': hunter != null,
        'hunterName': hunter?.hunterName,
        'hunterLevel': hunter?.level,
        'hunterRank': hunter?.hunterRank,
        'lastSyncTime': lastSync?.toIso8601String(),
        'isFirstTime': isFirstTime,
        'sessionValid': isAuth && token != null && hunter != null,
      };
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Get session info error: $e');
      }
      return {
        'isAuthenticated': false,
        'hasToken': false,
        'hasProfile': false,
        'sessionValid': false,
        'error': e.toString(),
      };
    }
  }
}