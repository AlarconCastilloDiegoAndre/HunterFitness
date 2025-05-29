import 'package:flutter/material.dart';

class Hunter {
  final String hunterId;
  final String username;
  final String email;
  final String hunterName;
  
  // Stats del Hunter
  final int level;
  final int currentXP;
  final int totalXP;
  final String hunterRank;
  
  // Stats principales
  final int strength;
  final int agility;
  final int vitality;
  final int endurance;
  
  // Progreso y streaks
  final int dailyStreak;
  final int longestStreak;
  final int totalWorkouts;
  
  // Metadatos
  final DateTime createdAt;
  final DateTime? lastLoginAt;
  final bool isActive;
  final String? profilePictureUrl;

  Hunter({
    required this.hunterId,
    required this.username,
    required this.email,
    required this.hunterName,
    required this.level,
    required this.currentXP,
    required this.totalXP,
    required this.hunterRank,
    required this.strength,
    required this.agility,
    required this.vitality,
    required this.endurance,
    required this.dailyStreak,
    required this.longestStreak,
    required this.totalWorkouts,
    required this.createdAt,
    this.lastLoginAt,
    required this.isActive,
    this.profilePictureUrl,
  });

  factory Hunter.fromJson(Map<String, dynamic> json) {
    return Hunter(
      hunterId: json['hunterId'] ?? '',
      username: json['username'] ?? '',
      email: json['email'] ?? '',
      hunterName: json['hunterName'] ?? '',
      level: json['level'] ?? 1,
      currentXP: json['currentXP'] ?? 0,
      totalXP: json['totalXP'] ?? 0,
      hunterRank: json['hunterRank'] ?? 'E',
      strength: json['strength'] ?? 10,
      agility: json['agility'] ?? 10,
      vitality: json['vitality'] ?? 10,
      endurance: json['endurance'] ?? 10,
      dailyStreak: json['dailyStreak'] ?? 0,
      longestStreak: json['longestStreak'] ?? 0,
      totalWorkouts: json['totalWorkouts'] ?? 0,
      createdAt: json['createdAt'] != null 
          ? DateTime.parse(json['createdAt']) 
          : DateTime.now(),
      lastLoginAt: json['lastLoginAt'] != null 
          ? DateTime.parse(json['lastLoginAt']) 
          : null,
      isActive: json['isActive'] ?? true,
      profilePictureUrl: json['profilePictureUrl'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'hunterId': hunterId,
      'username': username,
      'email': email,
      'hunterName': hunterName,
      'level': level,
      'currentXP': currentXP,
      'totalXP': totalXP,
      'hunterRank': hunterRank,
      'strength': strength,
      'agility': agility,
      'vitality': vitality,
      'endurance': endurance,
      'dailyStreak': dailyStreak,
      'longestStreak': longestStreak,
      'totalWorkouts': totalWorkouts,
      'createdAt': createdAt.toIso8601String(),
      'lastLoginAt': lastLoginAt?.toIso8601String(),
      'isActive': isActive,
      'profilePictureUrl': profilePictureUrl,
    };
  }

  // Métodos de utilidad
  String getRankName() {
    switch (hunterRank) {
      case 'E':
        return 'Rookie Hunter';
      case 'D':
        return 'Bronze Hunter';
      case 'C':
        return 'Silver Hunter';
      case 'B':
        return 'Gold Hunter';
      case 'A':
        return 'Elite Hunter';
      case 'S':
        return 'Master Hunter';
      case 'SS':
        return 'Legendary Hunter';
      case 'SSS':
        return 'Shadow Monarch';
      default:
        return 'Unknown Hunter';
    }
  }

  Color getRankColor() {
    switch (hunterRank) {
      case 'E':
        return const Color(0xFF8B4513); // Brown
      case 'D':
        return const Color(0xFFCD7F32); // Bronze
      case 'C':
        return const Color(0xFFC0C0C0); // Silver
      case 'B':
        return const Color(0xFFFFD700); // Gold
      case 'A':
        return const Color(0xFF9932CC); // Purple
      case 'S':
        return const Color(0xFFFF4500); // Red-Orange
      case 'SS':
        return const Color(0xFFFF1493); // Deep Pink
      case 'SSS':
        return const Color(0xFF8A2BE2); // Blue Violet
      default:
        return const Color(0xFF808080); // Gray
    }
  }

  int getXPForNextLevel() {
    // Curva de XP exponencial, como en Solo Leveling
    return (level * 100) + (level * level * 50);
  }

  int getXPForCurrentLevel() {
    if (level <= 1) return 0;
    return ((level - 1) * 100) + ((level - 1) * (level - 1) * 50);
  }

  double getXPProgress() {
    int xpForNext = getXPForNextLevel();
    int xpForCurrent = getXPForCurrentLevel();
    int xpInLevel = currentXP - xpForCurrent;
    int xpNeededForLevel = xpForNext - xpForCurrent;
    
    if (xpNeededForLevel <= 0) return 1.0;
    return (xpInLevel / xpNeededForLevel).clamp(0.0, 1.0);
  }

  int getTotalStats() {
    return strength + agility + vitality + endurance;
  }

  // Para debugging
  @override
  String toString() {
    return 'Hunter{hunterId: $hunterId, hunterName: $hunterName, level: $level, rank: $hunterRank}';
  }
}

// Modelo para las respuestas de la API
class ApiResponse<T> {
  final bool success;
  final String message;
  final T? data;

  ApiResponse({
    required this.success,
    required this.message,
    this.data,
  });

  factory ApiResponse.fromJson(Map<String, dynamic> json, T Function(Map<String, dynamic>)? fromJsonT) {
    return ApiResponse<T>(
      success: json['success'] ?? false,
      message: json['message'] ?? '',
      data: json['data'] != null && fromJsonT != null 
          ? fromJsonT(json['data']) 
          : null,
    );
  }
}

// Modelo para login/register
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
}

class LoginResponse {
  final String hunterId;
  final String username;
  final String hunterName;
  final String token;

  LoginResponse({
    required this.hunterId,
    required this.username,
    required this.hunterName,
    required this.token,
  });

  factory LoginResponse.fromJson(Map<String, dynamic> json) {
    return LoginResponse(
      hunterId: json['hunterId'] ?? '',
      username: json['username'] ?? '',
      hunterName: json['hunterName'] ?? '',
      token: json['token'] ?? '',
    );
  }
}