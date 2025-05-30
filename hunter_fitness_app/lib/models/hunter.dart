class Hunter {
  final String hunterID;
  final String username;
  final String email;
  final String hunterName;
  final int level;
  final int currentXP;
  final int totalXP;
  final String hunterRank;
  final String rankDisplayName;
  final int strength;
  final int agility;
  final int vitality;
  final int endurance;
  final int totalStats;
  final int dailyStreak;
  final int longestStreak;
  final int totalWorkouts;
  final int xpRequiredForNextLevel;
  final double levelProgressPercentage;
  final bool canLevelUp;
  final String nextRankRequirement;
  final DateTime createdAt;
  final DateTime? lastLoginAt;
  final String? profilePictureUrl;
  final List<EquippedItem> equippedItems;
  final Map<String, dynamic> additionalStats;

  Hunter({
    required this.hunterID,
    required this.username,
    required this.email,
    required this.hunterName,
    required this.level,
    required this.currentXP,
    required this.totalXP,
    required this.hunterRank,
    required this.rankDisplayName,
    required this.strength,
    required this.agility,
    required this.vitality,
    required this.endurance,
    required this.totalStats,
    required this.dailyStreak,
    required this.longestStreak,
    required this.totalWorkouts,
    required this.xpRequiredForNextLevel,
    required this.levelProgressPercentage,
    required this.canLevelUp,
    required this.nextRankRequirement,
    required this.createdAt,
    this.lastLoginAt,
    this.profilePictureUrl,
    required this.equippedItems,
    required this.additionalStats,
  });

  factory Hunter.fromJson(Map<String, dynamic> json) {
    return Hunter(
      hunterID: json['hunterID'] ?? '',
      username: json['username'] ?? '',
      email: json['email'] ?? '',
      hunterName: json['hunterName'] ?? '',
      level: json['level'] ?? 1,
      currentXP: json['currentXP'] ?? 0,
      totalXP: json['totalXP'] ?? 0,
      hunterRank: json['hunterRank'] ?? 'E',
      rankDisplayName: json['rankDisplayName'] ?? 'Rookie Hunter',
      strength: json['strength'] ?? 10,
      agility: json['agility'] ?? 10,
      vitality: json['vitality'] ?? 10,
      endurance: json['endurance'] ?? 10,
      totalStats: json['totalStats'] ?? 40,
      dailyStreak: json['dailyStreak'] ?? 0,
      longestStreak: json['longestStreak'] ?? 0,
      totalWorkouts: json['totalWorkouts'] ?? 0,
      xpRequiredForNextLevel: json['xpRequiredForNextLevel'] ?? 100,
      levelProgressPercentage: (json['levelProgressPercentage'] ?? 0.0).toDouble(),
      canLevelUp: json['canLevelUp'] ?? false,
      nextRankRequirement: json['nextRankRequirement'] ?? '',
      createdAt: DateTime.parse(json['createdAt'] ?? DateTime.now().toIso8601String()),
      lastLoginAt: json['lastLoginAt'] != null ? DateTime.parse(json['lastLoginAt']) : null,
      profilePictureUrl: json['profilePictureUrl'],
      equippedItems: (json['equippedItems'] as List<dynamic>? ?? [])
          .map((item) => EquippedItem.fromJson(item))
          .toList(),
      additionalStats: json['additionalStats'] ?? {},
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'hunterID': hunterID,
      'username': username,
      'email': email,
      'hunterName': hunterName,
      'level': level,
      'currentXP': currentXP,
      'totalXP': totalXP,
      'hunterRank': hunterRank,
      'rankDisplayName': rankDisplayName,
      'strength': strength,
      'agility': agility,
      'vitality': vitality,
      'endurance': endurance,
      'totalStats': totalStats,
      'dailyStreak': dailyStreak,
      'longestStreak': longestStreak,
      'totalWorkouts': totalWorkouts,
      'xpRequiredForNextLevel': xpRequiredForNextLevel,
      'levelProgressPercentage': levelProgressPercentage,
      'canLevelUp': canLevelUp,
      'nextRankRequirement': nextRankRequirement,
      'createdAt': createdAt.toIso8601String(),
      'lastLoginAt': lastLoginAt?.toIso8601String(),
      'profilePictureUrl': profilePictureUrl,
      'equippedItems': equippedItems.map((item) => item.toJson()).toList(),
      'additionalStats': additionalStats,
    };
  }

  String getRankIcon() {
    switch (hunterRank) {
      case 'E':
        return '🔰';
      case 'D':
        return '🥉';
      case 'C':
        return '🥈';
      case 'B':
        return '🥇';
      case 'A':
        return '💎';
      case 'S':
        return '👑';
      case 'SS':
        return '⭐';
      case 'SSS':
        return '🏹';
      default:
        return '🔰';
    }
  }

  String getMotivationalMessage() {
    if (canLevelUp) {
      return "Ready to level up! 🎉";
    } else if (levelProgressPercentage > 75) {
      return "Almost there, Hunter! 🔥";
    } else if (dailyStreak > 5) {
      return "Incredible streak! 💪";
    } else {
      return "Keep pushing forward! ⚔️";
    }
  }
}

class EquippedItem {
  final String equipmentID;
  final String itemName;
  final String itemType;
  final String rarity;
  final String rarityColor;
  final int strengthBonus;
  final int agilityBonus;
  final int vitalityBonus;
  final int enduranceBonus;
  final double xpMultiplier;
  final String statBonusDescription;
  final String? iconUrl;
  final int powerLevel;

  EquippedItem({
    required this.equipmentID,
    required this.itemName,
    required this.itemType,
    required this.rarity,
    required this.rarityColor,
    required this.strengthBonus,
    required this.agilityBonus,
    required this.vitalityBonus,
    required this.enduranceBonus,
    required this.xpMultiplier,
    required this.statBonusDescription,
    this.iconUrl,
    required this.powerLevel,
  });

  factory EquippedItem.fromJson(Map<String, dynamic> json) {
    return EquippedItem(
      equipmentID: json['equipmentID'] ?? '',
      itemName: json['itemName'] ?? '',
      itemType: json['itemType'] ?? '',
      rarity: json['rarity'] ?? 'Common',
      rarityColor: json['rarityColor'] ?? '#9E9E9E',
      strengthBonus: json['strengthBonus'] ?? 0,
      agilityBonus: json['agilityBonus'] ?? 0,
      vitalityBonus: json['vitalityBonus'] ?? 0,
      enduranceBonus: json['enduranceBonus'] ?? 0,
      xpMultiplier: (json['xpMultiplier'] ?? 1.0).toDouble(),
      statBonusDescription: json['statBonusDescription'] ?? '',
      iconUrl: json['iconUrl'],
      powerLevel: json['powerLevel'] ?? 0,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'equipmentID': equipmentID,
      'itemName': itemName,
      'itemType': itemType,
      'rarity': rarity,
      'rarityColor': rarityColor,
      'strengthBonus': strengthBonus,
      'agilityBonus': agilityBonus,
      'vitalityBonus': vitalityBonus,
      'enduranceBonus': enduranceBonus,
      'xpMultiplier': xpMultiplier,
      'statBonusDescription': statBonusDescription,
      'iconUrl': iconUrl,
      'powerLevel': powerLevel,
    };
  }

  String getTypeIcon() {
    switch (itemType) {
      case 'Weapon':
        return '⚔️';
      case 'Armor':
        return '🛡️';
      case 'Accessory':
        return '💍';
      default:
        return '⚡';
    }
  }
}