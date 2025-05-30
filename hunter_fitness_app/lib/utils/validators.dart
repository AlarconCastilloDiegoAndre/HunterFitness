import 'constants.dart';

class Validators {
  // Validador de email
  static String? email(String? value) {
    if (value == null || value.trim().isEmpty) {
      return AppStrings.validationRequired;
    }
    
    final emailRegex = RegExp(RegexPatterns.email);
    if (!emailRegex.hasMatch(value.trim())) {
      return AppStrings.validationEmail;
    }
    
    return null;
  }

  // Validador de username
  static String? username(String? value) {
    if (value == null || value.trim().isEmpty) {
      return AppStrings.validationRequired;
    }
    
    final cleanValue = value.trim();
    
    if (cleanValue.length < 3 || cleanValue.length > 20) {
      return AppStrings.validationUsername;
    }
    
    final usernameRegex = RegExp(RegexPatterns.username);
    if (!usernameRegex.hasMatch(cleanValue)) {
      return 'Username can only contain letters, numbers, underscores and hyphens';
    }
    
    return null;
  }

  // Validador de password
  static String? password(String? value) {
    if (value == null || value.isEmpty) {
      return AppStrings.validationRequired;
    }
    
    if (value.length < 6) {
      return AppStrings.validationPassword;
    }
    
    return null;
  }

  // Validador de confirmación de password
  static String? confirmPassword(String? value, String? originalPassword) {
    if (value == null || value.isEmpty) {
      return AppStrings.validationRequired;
    }
    
    if (value != originalPassword) {
      return AppStrings.validationPasswordMatch;
    }
    
    return null;
  }

  // Validador de nombre de hunter
  static String? hunterName(String? value) {
    if (value == null || value.trim().isEmpty) {
      return AppStrings.validationRequired;
    }
    
    final cleanValue = value.trim();
    
    if (cleanValue.length < 2 || cleanValue.length > 50) {
      return AppStrings.validationHunterName;
    }
    
    final hunterNameRegex = RegExp(RegexPatterns.hunterName);
    if (!hunterNameRegex.hasMatch(cleanValue)) {
      return 'Hunter name can only contain letters, numbers and spaces';
    }
    
    return null;
  }

  // Validador genérico para campos requeridos
  static String? required(String? value, [String? fieldName]) {
    if (value == null || value.trim().isEmpty) {
      return fieldName != null 
          ? '$fieldName is required'
          : AppStrings.validationRequired;
    }
    return null;
  }

  // Validador de longitud mínima
  static String? minLength(String? value, int minLength, [String? fieldName]) {
    if (value == null || value.isEmpty) {
      return AppStrings.validationRequired;
    }
    
    if (value.length < minLength) {
      return fieldName != null
          ? '$fieldName must be at least $minLength characters'
          : 'Must be at least $minLength characters';
    }
    
    return null;
  }

  // Validador de longitud máxima
  static String? maxLength(String? value, int maxLength, [String? fieldName]) {
    if (value != null && value.length > maxLength) {
      return fieldName != null
          ? '$fieldName must not exceed $maxLength characters'
          : 'Must not exceed $maxLength characters';
    }
    
    return null;
  }

  // Validador de rango de longitud
  static String? lengthRange(String? value, int minLength, int maxLength, [String? fieldName]) {
    if (value == null || value.isEmpty) {
      return AppStrings.validationRequired;
    }
    
    if (value.length < minLength || value.length > maxLength) {
      return fieldName != null
          ? '$fieldName must be between $minLength and $maxLength characters'
          : 'Must be between $minLength and $maxLength characters';
    }
    
    return null;
  }

  // Validador numérico
  static String? numeric(String? value, [String? fieldName]) {
    if (value == null || value.trim().isEmpty) {
      return AppStrings.validationRequired;
    }
    
    if (double.tryParse(value.trim()) == null) {
      return fieldName != null
          ? '$fieldName must be a valid number'
          : 'Must be a valid number';
    }
    
    return null;
  }

  // Validador de número entero
  static String? integer(String? value, [String? fieldName]) {
    if (value == null || value.trim().isEmpty) {
      return AppStrings.validationRequired;
    }
    
    if (int.tryParse(value.trim()) == null) {
      return fieldName != null
          ? '$fieldName must be a valid integer'
          : 'Must be a valid integer';
    }
    
    return null;
  }

  // Validador de rango numérico
  static String? numberRange(String? value, double min, double max, [String? fieldName]) {
    if (value == null || value.trim().isEmpty) {
      return AppStrings.validationRequired;
    }
    
    final number = double.tryParse(value.trim());
    if (number == null) {
      return fieldName != null
          ? '$fieldName must be a valid number'
          : 'Must be a valid number';
    }
    
    if (number < min || number > max) {
      return fieldName != null
          ? '$fieldName must be between $min and $max'
          : 'Must be between $min and $max';
    }
    
    return null;
  }

  // Validador de URL
  static String? url(String? value, [String? fieldName]) {
    if (value == null || value.trim().isEmpty) {
      return null; // URL es opcional generalmente
    }
    
    final uri = Uri.tryParse(value.trim());
    if (uri == null || !uri.hasScheme || (!uri.isScheme('http') && !uri.isScheme('https'))) {
      return fieldName != null
          ? '$fieldName must be a valid URL'
          : 'Must be a valid URL';
    }
    
    return null;
  }

  // Validador para confirmar que no contiene espacios
  static String? noSpaces(String? value, [String? fieldName]) {
    if (value == null || value.isEmpty) {
      return AppStrings.validationRequired;
    }
    
    if (value.contains(' ')) {
      return fieldName != null
          ? '$fieldName cannot contain spaces'
          : 'Cannot contain spaces';
    }
    
    return null;
  }

  // Validador personalizado que combina múltiples validaciones
  static String? Function(String?) combine(List<String? Function(String?)> validators) {
    return (String? value) {
      for (final validator in validators) {
        final result = validator(value);
        if (result != null) {
          return result;
        }
      }
      return null;
    };
  }

  // Validaciones específicas para la app
  
  // Validador para login (puede ser username o email)
  static String? loginIdentifier(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'Username or email is required';
    }
    
    final cleanValue = value.trim();
    
    // Si contiene @, validar como email
    if (cleanValue.contains('@')) {
      return email(cleanValue);
    } else {
      // Si no, validar como username
      return username(cleanValue);
    }
  }

  // Validador para stats de hunter (0-100)
  static String? hunterStat(String? value, [String? statName]) {
    final error = numberRange(value, 0, 100, statName);
    if (error != null) return error;
    
    return integer(value, statName);
  }

  // Validador para level de hunter (1-100)
  static String? hunterLevel(String? value) {
    final error = numberRange(value, 1, 100, 'Level');
    if (error != null) return error;
    
    return integer(value, 'Level');
  }

  // Validador para XP (no negativo)
  static String? xpValue(String? value) {
    if (value == null || value.trim().isEmpty) {
      return AppStrings.validationRequired;
    }
    
    final xp = int.tryParse(value.trim());
    if (xp == null) {
      return 'XP must be a valid number';
    }
    
    if (xp < 0) {
      return 'XP cannot be negative';
    }
    
    return null;
  }

  // Validador para duración en segundos
  static String? duration(String? value) {
    if (value == null || value.trim().isEmpty) {
      return AppStrings.validationRequired;
    }
    
    final seconds = int.tryParse(value.trim());
    if (seconds == null) {
      return 'Duration must be a valid number';
    }
    
    if (seconds <= 0) {
      return 'Duration must be greater than 0';
    }
    
    if (seconds > 7200) { // Máximo 2 horas
      return 'Duration cannot exceed 2 hours';
    }
    
    return null;
  }

  // Validador para repeticiones
  static String? reps(String? value) {
    if (value == null || value.trim().isEmpty) {
      return AppStrings.validationRequired;
    }
    
    final repsCount = int.tryParse(value.trim());
    if (repsCount == null) {
      return 'Reps must be a valid number';
    }
    
    if (repsCount <= 0) {
      return 'Reps must be greater than 0';
    }
    
    if (repsCount > 1000) {
      return 'Reps cannot exceed 1000';
    }
    
    return null;
  }

  // Validador para sets
  static String? sets(String? value) {
    if (value == null || value.trim().isEmpty) {
      return AppStrings.validationRequired;
    }
    
    final setsCount = int.tryParse(value.trim());
    if (setsCount == null) {
      return 'Sets must be a valid number';
    }
    
    if (setsCount <= 0) {
      return 'Sets must be greater than 0';
    }
    
    if (setsCount > 20) {
      return 'Sets cannot exceed 20';
    }
    
    return null;
  }

  // Validador para distancia en metros
  static String? distance(String? value) {
    if (value == null || value.trim().isEmpty) {
      return AppStrings.validationRequired;
    }
    
    final meters = double.tryParse(value.trim());
    if (meters == null) {
      return 'Distance must be a valid number';
    }
    
    if (meters <= 0) {
      return 'Distance must be greater than 0';
    }
    
    if (meters > 50000) { // Máximo 50km
      return 'Distance cannot exceed 50,000 meters';
    }
    
    return null;
  }
}

// Extensión para facilitar el uso de validadores
extension ValidatorExtension on String? {
  String? get isRequired => Validators.required(this);
  String? get isEmail => Validators.email(this);
  String? get isUsername => Validators.username(this);
  String? get isPassword => Validators.password(this);
  String? get isHunterName => Validators.hunterName(this);
  String? get isNumeric => Validators.numeric(this);
  String? get isInteger => Validators.integer(this);
  String? get isUrl => Validators.url(this);
  String? get noSpaces => Validators.noSpaces(this);
  String? get isLoginIdentifier => Validators.loginIdentifier(this);
  
  String? minLength(int length) => Validators.minLength(this, length);
  String? maxLength(int length) => Validators.maxLength(this, length);
  String? lengthRange(int min, int max) => Validators.lengthRange(this, min, max);
  String? numberRange(double min, double max) => Validators.numberRange(this, min, max);
  String? confirmPassword(String? original) => Validators.confirmPassword(this, original);
}