import 'package:flutter/foundation.dart';
import '../models/hunter.dart';
import '../models/auth_response.dart';
import '../services/auth_service.dart';
import '../services/storage_service.dart';

class AuthProvider extends ChangeNotifier {
  final AuthService _authService = AuthService();
  final StorageService _storageService = StorageService();

  Hunter? _currentHunter;
  bool _isAuthenticated = false;
  bool _isLoading = false;
  String? _errorMessage;

  // Getters
  Hunter? get currentHunter => _currentHunter;
  bool get isAuthenticated => _isAuthenticated;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;

  // Información del hunter para UI
  String get hunterName => _currentHunter?.hunterName ?? 'Hunter';
  int get level => _currentHunter?.level ?? 1;
  String get rank => _currentHunter?.hunterRank ?? 'E';
  String get rankDisplayName => _currentHunter?.getRankDisplayName() ?? 'Rookie Hunter';
  int get currentXP => _currentHunter?.currentXP ?? 0;
  int get totalXP => _currentHunter?.totalXP ?? 0;
  int get dailyStreak => _currentHunter?.dailyStreak ?? 0;
  bool get canLevelUp => _currentHunter?.canLevelUp() ?? false;

  // Stats del hunter
  int get strength => _currentHunter?.strength ?? 10;
  int get agility => _currentHunter?.agility ?? 10;
  int get vitality => _currentHunter?.vitality ?? 10;
  int get endurance => _currentHunter?.endurance ?? 10;
  int get totalStats => _currentHunter?.getStatsTotal() ?? 40;

  // Progreso de nivel
  int get xpRequiredForNextLevel => _currentHunter?.getXPRequiredForNextLevel() ?? 100;
  double get levelProgressPercentage => _currentHunter?.getLevelProgressPercentage() ?? 0.0;

  // Mensaje motivacional
  String get motivationalMessage => _currentHunter?.getMotivationalMessage() ?? 'Ready to train, Hunter!';

  AuthProvider() {
    _initializeAuth();
  }

  // Inicializar autenticación al arrancar la app
  Future<void> _initializeAuth() async {
    _setLoading(true);
    
    try {
      await _storageService.init();
      
      final bool isLoggedIn = await _storageService.isLoggedIn();
      
      if (isLoggedIn) {
        final hunter = await _storageService.getHunterProfile();
        final token = await _storageService.getAuthToken();
        
        if (hunter != null && token != null) {
          // Validar token con el servidor
          final isValid = await _authService.validateToken();
          
          if (isValid) {
            _currentHunter = hunter;
            _isAuthenticated = true;
            _clearError();
            
            // Sincronizar datos con el servidor
            await syncUserData();
          } else {
            // Token inválido, limpiar datos
            await logout();
          }
        }
      }
    } catch (e) {
      _setError('Error inicializando autenticación: ${e.toString()}');
      if (kDebugMode) {
        print('❌ Auth initialization error: $e');
      }
    } finally {
      _setLoading(false);
    }
  }

  // Login
  Future<bool> login(String username, String password) async {
    _setLoading(true);
    _clearError();

    try {
      final response = await _authService.login(username, password);

      if (response.success && response.hunter != null && response.token != null) {
        // Guardar datos localmente
        await _storageService.saveAuthToken(response.token!);
        await _storageService.saveHunterProfile(response.hunter!);
        await _storageService.setNotFirstTime();

        _currentHunter = response.hunter;
        _isAuthenticated = true;
        
        if (kDebugMode) {
          print('✅ Login successful: ${response.hunter!.hunterName}');
        }
        
        return true;
      } else {
        _setError(response.message);
        return false;
      }
    } catch (e) {
      _setError('Error durante el login: ${e.toString()}');
      if (kDebugMode) {
        print('❌ Login error: $e');
      }
      return false;
    } finally {
      _setLoading(false);
    }
  }

  // Registro
  Future<bool> register(String username, String email, String password, String hunterName) async {
    _setLoading(true);
    _clearError();

    try {
      final response = await _authService.register(username, email, password, hunterName);

      if (response.success && response.hunter != null && response.token != null) {
        // Guardar datos localmente
        await _storageService.saveAuthToken(response.token!);
        await _storageService.saveHunterProfile(response.hunter!);
        await _storageService.setNotFirstTime();

        _currentHunter = response.hunter;
        _isAuthenticated = true;
        
        if (kDebugMode) {
          print('✅ Registration successful: ${response.hunter!.hunterName}');
        }
        
        return true;
      } else {
        _setError(response.message);
        return false;
      }
    } catch (e) {
      _setError('Error durante el registro: ${e.toString()}');
      if (kDebugMode) {
        print('❌ Registration error: $e');
      }
      return false;
    } finally {
      _setLoading(false);
    }
  }

  // Logout
  Future<void> logout() async {
    _setLoading(true);

    try {
      // Limpiar datos locales
      await _storageService.clearAuthData();
      
      _currentHunter = null;
      _isAuthenticated = false;
      _clearError();
      
      if (kDebugMode) {
        print('✅ Logout successful');
      }
    } catch (e) {
      _setError('Error durante el logout: ${e.toString()}');
      if (kDebugMode) {
        print('❌ Logout error: $e');
      }
    } finally {
      _setLoading(false);
    }
  }

  // Refrescar token
  Future<bool> refreshToken() async {
    try {
      final response = await _authService.refreshToken();
      
      if (response.success && response.token != null) {
        await _storageService.saveAuthToken(response.token!);
        
        if (response.hunter != null) {
          await _storageService.saveHunterProfile(response.hunter!);
          _currentHunter = response.hunter;
          notifyListeners();
        }
        
        return true;
      }
      
      return false;
    } catch (e) {
      if (kDebugMode) {
        print('❌ Token refresh error: $e');
      }
      return false;
    }
  }

  // Sincronizar datos del usuario con el servidor
  Future<void> syncUserData() async {
    if (!_isAuthenticated) return;

    try {
      final success = await _authService.syncUserData();
      
      if (success) {
        final hunter = await _storageService.getHunterProfile();
        if (hunter != null) {
          _currentHunter = hunter;
          notifyListeners();
        }
        
        if (kDebugMode) {
          print('✅ User data synced successfully');
        }
      }
    } catch (e) {
      if (kDebugMode) {
        print('❌ Sync error: $e');
      }
      // No mostrar error al usuario para sync automático
    }
  }

  // Actualizar perfil del hunter
  Future<bool> updateProfile({String? hunterName, String? profilePictureUrl}) async {
    if (!_isAuthenticated || _currentHunter == null) return false;

    _setLoading(true);

    try {
      // TODO: Implementar llamada a API para actualizar perfil
      // Por ahora solo actualizar localmente
      if (hunterName != null) {
        _currentHunter = _currentHunter!.copyWith(hunterName: hunterName);
      }
      
      if (profilePictureUrl != null) {
        _currentHunter = _currentHunter!.copyWith(profilePictureUrl: profilePictureUrl);
      }

      await _storageService.saveHunterProfile(_currentHunter!);
      
      if (kDebugMode) {
        print('✅ Profile updated successfully');
      }
      
      return true;
    } catch (e) {
      _setError('Error actualizando perfil: ${e.toString()}');
      if (kDebugMode) {
        print('❌ Profile update error: $e');
      }
      return false;
    } finally {
      _setLoading(false);
    }
  }

  // Verificar si es la primera vez usando la app
  Future<bool> isFirstTime() async {
    return await _storageService.isFirstTime();
  }

  // Obtener información de la sesión
  Future<Map<String, dynamic>> getSessionInfo() async {
    return await _authService.getSessionInfo();
  }

  // Verificar conectividad con la API
  Future<bool> checkApiConnectivity() async {
    return await _authService.checkApiConnectivity();
  }

  // Métodos helper privados
  void _setLoading(bool loading) {
    _isLoading = loading;
    notifyListeners();
  }

  void _setError(String error) {
    _errorMessage = error;
    notifyListeners();
  }

  void _clearError() {
    _errorMessage = null;
    notifyListeners();
  }

  // Método para forzar actualización de UI
  void refresh() {
    notifyListeners();
  }

  // Método para simular level up (para testing)
  void simulateLevelUp() {
    if (_currentHunter != null) {
      _currentHunter = _currentHunter!.copyWith(
        level: _currentHunter!.level + 1,
        currentXP: 0,
        totalXP: _currentHunter!.totalXP + _currentHunter!.getXPRequiredForNextLevel(),
      );
      notifyListeners();
    }
  }

  // Método para simular ganar XP (para testing)
  void simulateXPGain(int xp) {
    if (_currentHunter != null) {
      final newCurrentXP = _currentHunter!.currentXP + xp;
      final newTotalXP = _currentHunter!.totalXP + xp;
      
      _currentHunter = _currentHunter!.copyWith(
        currentXP: newCurrentXP,
        totalXP: newTotalXP,
      );
      
      // Verificar level up
      if (_currentHunter!.canLevelUp()) {
        simulateLevelUp();
      }
      
      notifyListeners();
    }
  }

  @override
  void dispose() {
    super.dispose();
  }
}