class ApiConstants {
  // URL base de tu API de Azure
  static const String baseUrl = 'https://hunter-fitness-api.azurewebsites.net/api';
  
  // Endpoints de autenticación
  static const String loginEndpoint = '/auth/login';
  static const String registerEndpoint = '/auth/register';
  static const String validateTokenEndpoint = '/auth/validate';
  static const String refreshTokenEndpoint = '/auth/refresh';
  
  // Endpoints de hunter
  static const String hunterProfileEndpoint = '/hunters/profile';
  static const String hunterStatsEndpoint = '/hunters/stats';
  static const String leaderboardEndpoint = '/hunters/leaderboard';
  
  // Endpoints de quests
  static const String dailyQuestsEndpoint = '/quests/daily';
  static const String questHistoryEndpoint = '/quests/history';
  
  // Endpoints de dungeons
  static const String dungeonsEndpoint = '/dungeons';
  static const String activeRaidsEndpoint = '/dungeons/active-raids';
  
  // Endpoints de achievements
  static const String achievementsEndpoint = '/achievements';
  
  // Endpoints de equipment
  static const String inventoryEndpoint = '/equipment/inventory';
  
  // Health check
  static const String healthEndpoint = '/health';
  static const String pingEndpoint = '/ping';
}

class StorageKeys {
  static const String authToken = 'auth_token';
  static const String hunterProfile = 'hunter_profile';
  static const String isFirstTime = 'is_first_time';
  static const String lastSyncTime = 'last_sync_time';
  static const String offlineData = 'offline_data';
}

class AppConstants {
  // App Info
  static const String appName = 'Hunter Fitness';
  static const String appVersion = '1.0.0';
  static const String appDescription = 'Gamified fitness tracking inspired by Solo Leveling';
  
  // Timing
  static const int splashDuration = 3000; // milliseconds
  static const int tokenRefreshThreshold = 300000; // 5 minutes in milliseconds
  static const int networkTimeout = 30000; // 30 seconds
  
  // Pagination
  static const int defaultPageSize = 20;
  static const int maxRetries = 3;
  
  // Animation durations
  static const int shortAnimationDuration = 300;
  static const int mediumAnimationDuration = 500;
  static const int longAnimationDuration = 1000;
}

class HunterRanks {
  static const Map<String, String> rankNames = {
    'E': 'Rookie Hunter',
    'D': 'Bronze Hunter', 
    'C': 'Silver Hunter',
    'B': 'Gold Hunter',
    'A': 'Elite Hunter',
    'S': 'Master Hunter',
    'SS': 'Legendary Hunter',
    'SSS': 'Shadow Monarch',
  };
  
  static const Map<String, String> rankIcons = {
    'E': '🔰',
    'D': '🥉',
    'C': '🥈',
    'B': '🥇',
    'A': '💎',
    'S': '👑',
    'SS': '⭐',
    'SSS': '🏹',
  };
  
  static const Map<String, int> rankLevels = {
    'E': 1,
    'D': 11,
    'C': 21,
    'B': 36,
    'A': 51,
    'S': 71,
    'SS': 86,
    'SSS': 96,
  };
}

class AppColors {
  // Tema oscuro principal (estilo Solo Leveling)
  static const int _primaryBlue = 0xFF1E88E5;
  static const int _primaryGold = 0xFFFFD700;
  static const int _darkBg = 0xFF121212;
  static const int _darkCard = 0xFF1E1E1E;
  
  // Colores principales
  static const int primaryBlue = _primaryBlue;
  static const int primaryGold = _primaryGold;
  static const int accent = 0xFF03DAC6;
  
  // Backgrounds
  static const int background = _darkBg;
  static const int surface = _darkCard;
  static const int cardBackground = 0xFF2C2C2C;
  
  // Text colors
  static const int textPrimary = 0xFFFFFFFF;
  static const int textSecondary = 0xFFB0B0B0;
  static const int textHint = 0xFF757575;
  
  // Status colors
  static const int success = 0xFF4CAF50;
  static const int warning = 0xFFFF9800;
  static const int error = 0xFFE53E3E;
  static const int info = 0xFF2196F3;
  
  // Rank colors
  static const Map<String, int> rankColors = {
    'E': 0xFF9E9E9E,      // Gris
    'D': 0xFF8D6E63,      // Bronce
    'C': 0xFF90A4AE,      // Plata
    'B': 0xFFFFB300,      // Oro
    'A': 0xFF7B1FA2,      // Púrpura
    'S': 0xFFE91E63,      // Rosa
    'SS': 0xFFFF5722,     // Rojo-naranja
    'SSS': 0xFFFFD700,    // Dorado
  };
  
  // Equipment rarity colors
  static const Map<String, int> rarityColors = {
    'Common': 0xFF9E9E9E,     // Gris
    'Rare': 0xFF2196F3,      // Azul
    'Epic': 0xFF9C27B0,      // Púrpura
    'Legendary': 0xFFFF9800, // Naranja
    'Mythic': 0xFFE53E3E,    // Rojo
  };
  
  // Difficulty colors
  static const Map<String, int> difficultyColors = {
    'Easy': 0xFF4CAF50,      // Verde
    'Medium': 0xFFFF9800,    // Naranja
    'Hard': 0xFFE53E3E,      // Rojo
    'Extreme': 0xFF9C27B0,   // Púrpura
  };
}

class AppStrings {
  // Auth
  static const String loginTitle = 'Hunter Login';
  static const String registerTitle = 'Become a Hunter';
  static const String username = 'Username';
  static const String email = 'Email';
  static const String password = 'Password';
  static const String confirmPassword = 'Confirm Password';
  static const String hunterName = 'Hunter Name';
  static const String login = 'Login';
  static const String register = 'Register';
  static const String forgotPassword = 'Forgot Password?';
  static const String dontHaveAccount = "Don't have an account?";
  static const String alreadyHaveAccount = 'Already have an account?';
  
  // Dashboard
  static const String welcome = 'Welcome back, Hunter!';
  static const String dailyQuests = 'Daily Quests';
  static const String profile = 'Profile';
  static const String inventory = 'Inventory';
  static const String dungeons = 'Dungeons';
  static const String achievements = 'Achievements';
  static const String leaderboard = 'Leaderboard';
  
  // Stats
  static const String level = 'Level';
  static const String xp = 'XP';
  static const String rank = 'Rank';
  static const String strength = 'STR';
  static const String agility = 'AGI';
  static const String vitality = 'VIT';
  static const String endurance = 'END';
  
  // Messages
  static const String loading = 'Loading...';
  static const String retry = 'Retry';
  static const String cancel = 'Cancel';
  static const String save = 'Save';
  static const String delete = 'Delete';
  static const String edit = 'Edit';
  static const String logout = 'Logout';
  static const String settings = 'Settings';
  
  // Errors
  static const String errorGeneral = 'Something went wrong. Please try again.';
  static const String errorNetwork = 'Network error. Check your connection.';
  static const String errorInvalidCredentials = 'Invalid username or password.';
  static const String errorServerUnavailable = 'Server is temporarily unavailable.';
  static const String errorTimeout = 'Request timed out. Please try again.';
  
  // Validation
  static const String validationRequired = 'This field is required';
  static const String validationEmail = 'Enter a valid email address';
  static const String validationPassword = 'Password must be at least 6 characters';
  static const String validationPasswordMatch = 'Passwords do not match';
  static const String validationUsername = 'Username must be 3-20 characters';
  static const String validationHunterName = 'Hunter name must be 2-50 characters';
}

class AppIcons {
  // Navigation
  static const String home = '🏠';
  static const String quests = '📋';
  static const String dungeons = '🏰';
  static const String profile = '👤';
  static const String inventory = '🎒';
  static const String achievements = '🏆';
  static const String leaderboard = '🥇';
  static const String settings = '⚙️';
  
  // Stats
  static const String strength = '💪';
  static const String agility = '⚡';
  static const String vitality = '❤️';
  static const String endurance = '🛡️';
  static const String xp = '⭐';
  static const String level = '🔺';
  
  // Actions
  static const String play = '▶️';
  static const String pause = '⏸️';
  static const String stop = '⏹️';
  static const String check = '✅';
  static const String close = '❌';
  static const String info = 'ℹ️';
  static const String warning = '⚠️';
  static const String error = '🚨';
  static const String success = '🎉';
  
  // Equipment types
  static const String weapon = '⚔️';
  static const String armor = '🛡️';
  static const String accessory = '💍';
  
  // Quest types
  static const String cardio = '🏃‍♂️';
  static const String strengthTraining = '💪';
  static const String flexibility = '🤸‍♂️';
  static const String enduranceTraining = '⏱️';
  static const String mixed = '🔥';
}

class RegexPatterns {
  static const String email = r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$';
  static const String username = r'^[a-zA-Z0-9_-]{3,20}$';
  static const String hunterName = r'^[a-zA-Z0-9\s]{2,50}$';
}

class AppConfig {
  // Environment
  static const bool isProduction = bool.fromEnvironment('dart.vm.product');
  static const bool enableLogging = !isProduction;
  static const bool enableDebugMode = !isProduction;
  
  // Features
  static const bool enablePushNotifications = true;
  static const bool enableAnalytics = true;
  static const bool enableCrashReporting = true;
  static const bool enableOfflineMode = true;
  
  // Cache
  static const int cacheExpirationHours = 24;
  static const int maxCacheSize = 50; // MB
}