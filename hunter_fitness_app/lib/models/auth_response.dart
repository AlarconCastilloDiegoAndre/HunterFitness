import 'hunter.dart';

class AuthResponse {
  final bool success;
  final String message;
  final String? token;
  final Hunter? hunter;

  AuthResponse({
    required this.success,
    required this.message,
    this.token,
    this.hunter,
  });

  factory AuthResponse.fromJson(Map<String, dynamic> json) {
    return AuthResponse(
      success: json['success'] ?? false,
      message: json['message'] ?? '',
      token: json['token'],
      hunter: json['hunter'] != null ? Hunter.fromJson(json['hunter']) : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'success': success,
      'message': message,
      'token': token,
      'hunter': hunter?.toJson(),
    };
  }

  bool get isAuthenticated => success && token != null && hunter != null;

  @override
  String toString() {
    return 'AuthResponse{success: $success, message: $message, hasToken: ${token != null}, hasHunter: ${hunter != null}}';
  }
}

class LoginRequest {
  final String username;
  final String password;

  LoginRequest({
    required this.username,
    required this.password,
  });

  Map<String, dynamic> toJson() {
    return {
      'username': username,
      'password': password,
    };
  }

  @override
  String toString() {
    return 'LoginRequest{username: $username}';
  }
}

class RegisterRequest {
  final String username;
  final String email;
  final String password;
  final String hunterName;

  RegisterRequest({
    required this.username,
    required this.email,
    required this.password,
    required this.hunterName,
  });

  Map<String, dynamic> toJson() {
    return {
      'username': username,
      'email': email,
      'password': password,
      'hunterName': hunterName,
    };
  }

  @override
  String toString() {
    return 'RegisterRequest{username: $username, email: $email, hunterName: $hunterName}';
  }
}

class TokenValidationResponse {
  final bool success;
  final String message;
  final bool isValid;
  final HunterInfo? hunterInfo;

  TokenValidationResponse({
    required this.success,
    required this.message,
    required this.isValid,
    this.hunterInfo,
  });

  factory TokenValidationResponse.fromJson(Map<String, dynamic> json) {
    final data = json['data'] as Map<String, dynamic>?;
    
    return TokenValidationResponse(
      success: json['success'] ?? false,
      message: json['message'] ?? '',
      isValid: data?['isValid'] ?? false,
      hunterInfo: data?['hunterInfo'] != null 
          ? HunterInfo.fromJson(data!['hunterInfo']) 
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'success': success,
      'message': message,
      'data': {
        'isValid': isValid,
        'hunterInfo': hunterInfo?.toJson(),
      },
    };
  }

  @override
  String toString() {
    return 'TokenValidationResponse{success: $success, isValid: $isValid, hasHunterInfo: ${hunterInfo != null}}';
  }
}

class HunterInfo {
  final String hunterID;
  final String username;
  final String hunterName;
  final int level;
  final String hunterRank;

  HunterInfo({
    required this.hunterID,
    required this.username,
    required this.hunterName,
    required this.level,
    required this.hunterRank,
  });

  factory HunterInfo.fromJson(Map<String, dynamic> json) {
    return HunterInfo(
      hunterID: json['hunterID'] ?? '',
      username: json['username'] ?? '',
      hunterName: json['hunterName'] ?? '',
      level: json['level'] ?? 1,
      hunterRank: json['hunterRank'] ?? 'E',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'hunterID': hunterID,
      'username': username,
      'hunterName': hunterName,
      'level': level,
      'hunterRank': hunterRank,
    };
  }

  @override
  String toString() {
    return 'HunterInfo{hunterID: $hunterID, username: $username, hunterName: $hunterName, level: $level, rank: $hunterRank}';
  }
}