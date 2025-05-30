import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/hunter.dart';
import '../utils/constants.dart';

class StorageService {
  static final StorageService _instance = StorageService._internal();
  factory StorageService() => _instance;
  StorageService._internal();

  SharedPreferences? _prefs;

  // Inicializar SharedPreferences
  Future<void> init() async {
    _prefs ??= await SharedPreferences.getInstance();
  }

  // Verificar si SharedPreferences está inicializado
  Future<SharedPreferences> get _preferences async {
    if (_prefs == null) {
      await init();
    }
    return _prefs!;
  }

  // ============================
  // MÉTODOS PARA AUTH TOKEN
  // ============================

  /// Guardar token de autenticación
  Future<bool> saveAuthToken(String token) async {
    try {
      final prefs = await _preferences;
      final success = await prefs.setString(StorageKeys.authToken, token);
      
      if (AppConfig.enableLogging && success) {
        print('🔐 Auth token saved successfully');
      }
      
      return success;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error saving auth token: $e');
      }
      return false;
    }
  }

  /// Obtener token de autenticación
  Future<String?> getAuthToken() async {
    try {
      final prefs = await _preferences;
      final token = prefs.getString(StorageKeys.authToken);
      
      if (AppConfig.enableLogging) {
        print('🔐 Auth token retrieved: ${token != null ? 'Found' : 'Not found'}');
      }
      
      return token;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error getting auth token: $e');
      }
      return null;
    }
  }

  /// Verificar si existe un token de autenticación
  Future<bool> hasAuthToken() async {
    final token = await getAuthToken();
    return token != null && token.isNotEmpty;
  }

  /// Limpiar token de autenticación
  Future<bool> clearAuthToken() async {
    try {
      final prefs = await _preferences;
      final success = await prefs.remove(StorageKeys.authToken);
      
      if (AppConfig.enableLogging && success) {
        print('🔐 Auth token cleared successfully');
      }
      
      return success;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error clearing auth token: $e');
      }
      return false;
    }
  }

  // ============================
  // MÉTODOS PARA HUNTER PROFILE
  // ============================

  /// Guardar perfil del hunter
  Future<bool> saveHunterProfile(Hunter hunter) async {
    try {
      final prefs = await _preferences;
      final hunterJson = jsonEncode(hunter.toJson());
      final success = await prefs.setString(StorageKeys.hunterProfile, hunterJson);
      
      if (AppConfig.enableLogging && success) {
        print('👤 Hunter profile saved: ${hunter.hunterName}');
      }
      
      return success;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error saving hunter profile: $e');
      }
      return false;
    }
  }

  /// Obtener perfil del hunter
  Future<Hunter?> getHunterProfile() async {
    try {
      final prefs = await _preferences;
      final hunterJson = prefs.getString(StorageKeys.hunterProfile);
      
      if (hunterJson != null) {
        final hunterMap = jsonDecode(hunterJson) as Map<String, dynamic>;
        final hunter = Hunter.fromJson(hunterMap);
        
        if (AppConfig.enableLogging) {
          print('👤 Hunter profile retrieved: ${hunter.hunterName}');
        }
        
        return hunter;
      }
      
      return null;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error getting hunter profile: $e');
      }
      return null;
    }
  }

  /// Verificar si existe un perfil de hunter
  Future<bool> hasHunterProfile() async {
    final hunter = await getHunterProfile();
    return hunter != null;
  }

  /// Limpiar perfil del hunter
  Future<bool> clearHunterProfile() async {
    try {
      final prefs = await _preferences;
      final success = await prefs.remove(StorageKeys.hunterProfile);
      
      if (AppConfig.enableLogging && success) {
        print('👤 Hunter profile cleared successfully');
      }
      
      return success;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error clearing hunter profile: $e');
      }
      return false;
    }
  }

  // ============================
  // MÉTODOS PARA CONFIGURACIONES
  // ============================

  /// Verificar si es la primera vez que se abre la app
  Future<bool> isFirstTime() async {
    try {
      final prefs = await _preferences;
      return prefs.getBool(StorageKeys.isFirstTime) ?? true;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error checking first time: $e');
      }
      return true;
    }
  }

  /// Marcar que ya no es la primera vez
  Future<bool> setNotFirstTime() async {
    try {
      final prefs = await _preferences;
      final success = await prefs.setBool(StorageKeys.isFirstTime, false);
      
      if (AppConfig.enableLogging && success) {
        print('🎯 First time flag updated');
      }
      
      return success;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error setting first time flag: $e');
      }
      return false;
    }
  }

  /// Guardar tiempo de última sincronización
  Future<bool> saveLastSyncTime(DateTime dateTime) async {
    try {
      final prefs = await _preferences;
      final timestamp = dateTime.millisecondsSinceEpoch;
      final success = await prefs.setInt(StorageKeys.lastSyncTime, timestamp);
      
      if (AppConfig.enableLogging && success) {
        print('🔄 Last sync time saved: $dateTime');
      }
      
      return success;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error saving last sync time: $e');
      }
      return false;
    }
  }

  /// Obtener tiempo de última sincronización
  Future<DateTime?> getLastSyncTime() async {
    try {
      final prefs = await _preferences;
      final timestamp = prefs.getInt(StorageKeys.lastSyncTime);
      
      if (timestamp != null) {
        return DateTime.fromMillisecondsSinceEpoch(timestamp);
      }
      
      return null;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error getting last sync time: $e');
      }
      return null;
    }
  }

  // ============================
  // MÉTODOS PARA DATOS OFFLINE
  // ============================

  /// Guardar datos para uso offline
  Future<bool> saveOfflineData(String key, Map<String, dynamic> data) async {
    try {
      final prefs = await _preferences;
      final dataJson = jsonEncode(data);
      final storageKey = '${StorageKeys.offlineData}_$key';
      final success = await prefs.setString(storageKey, dataJson);
      
      if (AppConfig.enableLogging && success) {
        print('💾 Offline data saved for key: $key');
      }
      
      return success;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error saving offline data: $e');
      }
      return false;
    }
  }

  /// Obtener datos offline
  Future<Map<String, dynamic>?> getOfflineData(String key) async {
    try {
      final prefs = await _preferences;
      final storageKey = '${StorageKeys.offlineData}_$key';
      final dataJson = prefs.getString(storageKey);
      
      if (dataJson != null) {
        final data = jsonDecode(dataJson) as Map<String, dynamic>;
        
        if (AppConfig.enableLogging) {
          print('💾 Offline data retrieved for key: $key');
        }
        
        return data;
      }
      
      return null;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error getting offline data: $e');
      }
      return null;
    }
  }

  /// Limpiar datos offline específicos
  Future<bool> clearOfflineData(String key) async {
    try {
      final prefs = await _preferences;
      final storageKey = '${StorageKeys.offlineData}_$key';
      final success = await prefs.remove(storageKey);
      
      if (AppConfig.enableLogging && success) {
        print('💾 Offline data cleared for key: $key');
      }
      
      return success;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error clearing offline data: $e');
      }
      return false;
    }
  }

  // ============================
  // MÉTODOS GENERICOS
  // ============================

  /// Guardar string
  Future<bool> saveString(String key, String value) async {
    try {
      final prefs = await _preferences;
      return await prefs.setString(key, value);
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error saving string: $e');
      }
      return false;
    }
  }

  /// Obtener string
  Future<String?> getString(String key) async {
    try {
      final prefs = await _preferences;
      return prefs.getString(key);
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error getting string: $e');
      }
      return null;
    }
  }

  /// Guardar int
  Future<bool> saveInt(String key, int value) async {
    try {
      final prefs = await _preferences;
      return await prefs.setInt(key, value);
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error saving int: $e');
      }
      return false;
    }
  }

  /// Obtener int
  Future<int?> getInt(String key) async {
    try {
      final prefs = await _preferences;
      return prefs.getInt(key);
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error getting int: $e');
      }
      return null;
    }
  }

  /// Guardar bool
  Future<bool> saveBool(String key, bool value) async {
    try {
      final prefs = await _preferences;
      return await prefs.setBool(key, value);
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error saving bool: $e');
      }
      return false;
    }
  }

  /// Obtener bool
  Future<bool> getBool(String key, {bool defaultValue = false}) async {
    try {
      final prefs = await _preferences;
      return prefs.getBool(key) ?? defaultValue;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error getting bool: $e');
      }
      return defaultValue;
    }
  }

  /// Guardar double
  Future<bool> saveDouble(String key, double value) async {
    try {
      final prefs = await _preferences;
      return await prefs.setDouble(key, value);
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error saving double: $e');
      }
      return false;
    }
  }

  /// Obtener double
  Future<double?> getDouble(String key) async {
    try {
      final prefs = await _preferences;
      return prefs.getDouble(key);
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error getting double: $e');
      }
      return null;
    }
  }

  /// Verificar si existe una key
  Future<bool> containsKey(String key) async {
    try {
      final prefs = await _preferences;
      return prefs.containsKey(key);
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error checking key: $e');
      }
      return false;
    }
  }

  /// Remover una key específica
  Future<bool> remove(String key) async {
    try {
      final prefs = await _preferences;
      return await prefs.remove(key);
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error removing key: $e');
      }
      return false;
    }
  }

  /// Obtener todas las keys
  Future<Set<String>> getAllKeys() async {
    try {
      final prefs = await _preferences;
      return prefs.getKeys();
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error getting all keys: $e');
      }
      return <String>{};
    }
  }

  // ============================
  // MÉTODOS DE LIMPIEZA
  // ============================

  /// Limpiar todos los datos de autenticación
  Future<bool> clearAuthData() async {
    try {
      bool success = true;
      
      success &= await clearAuthToken();
      success &= await clearHunterProfile();
      
      if (AppConfig.enableLogging && success) {
        print('🧹 All auth data cleared successfully');
      }
      
      return success;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error clearing auth data: $e');
      }
      return false;
    }
  }

  /// Limpiar todos los datos offline
  Future<bool> clearAllOfflineData() async {
    try {
      final prefs = await _preferences;
      final keys = prefs.getKeys();
      bool success = true;
      
      for (final key in keys) {
        if (key.startsWith(StorageKeys.offlineData)) {
          success &= await prefs.remove(key);
        }
      }
      
      if (AppConfig.enableLogging && success) {
        print('🧹 All offline data cleared successfully');
      }
      
      return success;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error clearing offline data: $e');
      }
      return false;
    }
  }

  /// Limpiar TODOS los datos almacenados
  Future<bool> clearAll() async {
    try {
      final prefs = await _preferences;
      final success = await prefs.clear();
      
      if (AppConfig.enableLogging && success) {
        print('🧹 ALL storage data cleared successfully');
      }
      
      return success;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error clearing all data: $e');
      }
      return false;
    }
  }

  // ============================
  // MÉTODOS DE UTILIDAD
  // ============================

  /// Obtener tamaño aproximado del storage (en bytes)
  Future<int> getStorageSize() async {
    try {
      final prefs = await _preferences;
      final keys = prefs.getKeys();
      int totalSize = 0;
      
      for (final key in keys) {
        final value = prefs.get(key);
        if (value is String) {
          totalSize += value.length * 2; // UTF-16 encoding
        } else {
          totalSize += 8; // Aproximación para otros tipos
        }
      }
      
      return totalSize;
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error calculating storage size: $e');
      }
      return 0;
    }
  }

  /// Verificar si el usuario está logueado
  Future<bool> isLoggedIn() async {
    return await hasAuthToken() && await hasHunterProfile();
  }

  /// Exportar datos para backup (sin datos sensibles)
  Future<Map<String, dynamic>?> exportUserData() async {
    try {
      final hunter = await getHunterProfile();
      final lastSync = await getLastSyncTime();
      final isFirstTime = await this.isFirstTime();
      
      return {
        'hunterProfile': hunter?.toJson(),
        'lastSyncTime': lastSync?.toIso8601String(),
        'isFirstTime': isFirstTime,
        'exportedAt': DateTime.now().toIso8601String(),
        'appVersion': AppConstants.appVersion,
      };
    } catch (e) {
      if (AppConfig.enableLogging) {
        print('❌ Error exporting user data: $e');
      }
      return null;
    }
  }
}