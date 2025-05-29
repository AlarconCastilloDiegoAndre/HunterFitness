using System.ComponentModel.DataAnnotations;

namespace HunterFitness.API.Models
{
    public class Equipment
    {
        [Key]
        public Guid EquipmentID { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string ItemName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(20)]
        public string ItemType { get; set; } = "Weapon"; // Weapon, Armor, Accessory

        [Required]
        [StringLength(15)]
        public string Rarity { get; set; } = "Common"; // Common, Rare, Epic, Legendary, Mythic

        // Bonificaciones de stats
        public int StrengthBonus { get; set; } = 0;
        public int AgilityBonus { get; set; } = 0;
        public int VitalityBonus { get; set; } = 0;
        public int EnduranceBonus { get; set; } = 0;

        [Range(0.5, 3.0)]
        public decimal XPMultiplier { get; set; } = 1.00m;

        // Requisitos para desbloquear
        public int UnlockLevel { get; set; } = 1;

        [Required]
        [StringLength(10)]
        public string UnlockRank { get; set; } = "E";

        [StringLength(500)]
        public string? UnlockCondition { get; set; } // Descripción de cómo desbloquear

        // Metadatos
        [StringLength(500)]
        public string? IconUrl { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<HunterEquipment> HunterEquipment { get; set; } = new List<HunterEquipment>();

        // Métodos Helper
        public int GetTotalStatBonus()
        {
            return StrengthBonus + AgilityBonus + VitalityBonus + EnduranceBonus;
        }

        public string GetRarityDisplayName()
        {
            return Rarity switch
            {
                "Common" => "Common Equipment",
                "Rare" => "Rare Equipment",
                "Epic" => "Epic Equipment", 
                "Legendary" => "Legendary Equipment",
                "Mythic" => "Mythic Equipment",
                _ => "Unknown Rarity"
            };
        }

        public string GetRarityColor()
        {
            return Rarity switch
            {
                "Common" => "#9E9E9E",     // Gris
                "Rare" => "#2196F3",      // Azul
                "Epic" => "#9C27B0",      // Púrpura
                "Legendary" => "#FF9800", // Naranja
                "Mythic" => "#F44336",    // Rojo
                _ => "#757575"            // Gris por defecto
            };
        }

        public string GetItemTypeDisplayName()
        {
            return ItemType switch
            {
                "Weapon" => "Weapon",
                "Armor" => "Armor",
                "Accessory" => "Accessory",
                _ => "Unknown Type"
            };
        }

        public string GetItemTypeIcon()
        {
            return ItemType switch
            {
                "Weapon" => "⚔️",
                "Armor" => "🛡️",
                "Accessory" => "💍",
                _ => "⚡"
            };
        }

        public int GetPowerLevel()
        {
            var basePower = GetTotalStatBonus();
            var xpBonus = (int)((XPMultiplier - 1) * 10);
            
            var rarityMultiplier = Rarity switch
            {
                "Common" => 1.0,
                "Rare" => 1.2,
                "Epic" => 1.5,
                "Legendary" => 2.0,
                "Mythic" => 3.0,
                _ => 1.0
            };

            return (int)((basePower + xpBonus) * rarityMultiplier);
        }

        public bool IsEligibleForHunter(Hunter hunter)
        {
            if (hunter.Level < UnlockLevel) return false;

            var rankOrder = new Dictionary<string, int>
            {
                {"E", 1}, {"D", 2}, {"C", 3}, {"B", 4}, 
                {"A", 5}, {"S", 6}, {"SS", 7}, {"SSS", 8}
            };

            if (rankOrder.TryGetValue(UnlockRank, out var requiredRankValue) && 
                rankOrder.TryGetValue(hunter.HunterRank, out var hunterRankValue))
            {
                return hunterRankValue >= requiredRankValue;
            }

            return true;
        }

        public string GetUnlockRequirementText()
        {
            var requirements = new List<string>();

            if (UnlockLevel > 1)
                requirements.Add($"Level {UnlockLevel}");

            if (UnlockRank != "E")
                requirements.Add($"{UnlockRank} Rank");

            if (!string.IsNullOrWhiteSpace(UnlockCondition))
                requirements.Add(UnlockCondition);

            return requirements.Any() ? string.Join(", ", requirements) : "No requirements";
        }

        public string GetStatBonusDescription()
        {
            var bonuses = new List<string>();

            if (StrengthBonus > 0)
                bonuses.Add($"+{StrengthBonus} STR");
            
            if (AgilityBonus > 0)
                bonuses.Add($"+{AgilityBonus} AGI");
            
            if (VitalityBonus > 0)
                bonuses.Add($"+{VitalityBonus} VIT");
            
            if (EnduranceBonus > 0)
                bonuses.Add($"+{EnduranceBonus} END");

            if (XPMultiplier > 1.0m)
                bonuses.Add($"+{(XPMultiplier - 1) * 100:F0}% XP");

            return bonuses.Any() ? string.Join(", ", bonuses) : "No bonuses";
        }

        public bool HasXPBonus()
        {
            return XPMultiplier > 1.0m;
        }

        public bool HasStatBonuses()
        {
            return GetTotalStatBonus() > 0;
        }

        public string GetRarityStars()
        {
            return Rarity switch
            {
                "Common" => "⭐",
                "Rare" => "⭐⭐",
                "Epic" => "⭐⭐⭐",
                "Legendary" => "⭐⭐⭐⭐",
                "Mythic" => "⭐⭐⭐⭐⭐",
                _ => ""
            };
        }

        public int GetRarityValue()
        {
            return Rarity switch
            {
                "Common" => 1,
                "Rare" => 2,
                "Epic" => 3,
                "Legendary" => 4,
                "Mythic" => 5,
                _ => 0
            };
        }

        public bool IsHighTier()
        {
            return Rarity == "Legendary" || Rarity == "Mythic";
        }

        public string GetFlavorText()
        {
            return Rarity switch
            {
                "Mythic" => "A legendary artifact wielded by the Shadow Monarch himself...",
                "Legendary" => "Equipment of legendary hunters, passed down through generations.",
                "Epic" => "Forged by master craftsmen for elite hunters.",
                "Rare" => "Quality equipment used by experienced hunters.",
                "Common" => "Standard hunter equipment, reliable and sturdy.",
                _ => "Equipment of unknown origin."
            };
        }

        public List<string> GetSpecialEffects()
        {
            var effects = new List<string>();

            if (XPMultiplier > 1.5m)
                effects.Add("🌟 Major XP Boost");
            else if (XPMultiplier > 1.0m)
                effects.Add("✨ XP Boost");

            if (GetTotalStatBonus() >= 20)
                effects.Add("💪 Major Stat Boost");
            else if (GetTotalStatBonus() >= 10)
                effects.Add("📈 Stat Boost");

            if (IsHighTier())
                effects.Add("👑 Prestige Item");

            return effects;
        }

        public Dictionary<string, object> GetEquipmentSummary()
        {
            return new Dictionary<string, object>
            {
                {"Name", ItemName},
                {"Type", ItemType},
                {"Rarity", Rarity},
                {"PowerLevel", GetPowerLevel()},
                {"StatBonus", GetTotalStatBonus()},
                {"XPMultiplier", XPMultiplier},
                {"UnlockLevel", UnlockLevel},
                {"UnlockRank", UnlockRank},
                {"Requirements", GetUnlockRequirementText()},
                {"Effects", GetSpecialEffects()}
            };
        }

        // Métodos adicionales para análisis y estadísticas
        public bool IsWeapon()
        {
            return ItemType == "Weapon";
        }

        public bool IsArmor()
        {
            return ItemType == "Armor";
        }

        public bool IsAccessory()
        {
            return ItemType == "Accessory";
        }

        public bool IsCommon()
        {
            return Rarity == "Common";
        }

        public bool IsRare()
        {
            return Rarity == "Rare";
        }

        public bool IsEpic()
        {
            return Rarity == "Epic";
        }

        public bool IsLegendary()
        {
            return Rarity == "Legendary";
        }

        public bool IsMythic()
        {
            return Rarity == "Mythic";
        }

        public string GetDominantStat()
        {
            var stats = new Dictionary<string, int>
            {
                {"Strength", StrengthBonus},
                {"Agility", AgilityBonus},
                {"Vitality", VitalityBonus},
                {"Endurance", EnduranceBonus}
            };

            var maxStat = stats.OrderByDescending(s => s.Value).FirstOrDefault();
            return maxStat.Value > 0 ? maxStat.Key : "Balanced";
        }

        public string GetEquipmentClass()
        {
            var totalStats = GetTotalStatBonus();
            var xpBonus = XPMultiplier - 1.0m;

            if (xpBonus > 0.5m)
                return "XP Booster";
            else if (totalStats >= 15)
                return "Combat Focused";
            else if (totalStats >= 5)
                return "Stat Enhancer";
            else
                return "Basic Equipment";
        }

        public int GetRequiredHunterRankValue()
        {
            var rankOrder = new Dictionary<string, int>
            {
                {"E", 1}, {"D", 2}, {"C", 3}, {"B", 4}, 
                {"A", 5}, {"S", 6}, {"SS", 7}, {"SSS", 8}
            };

            return rankOrder.TryGetValue(UnlockRank, out var value) ? value : 1;
        }

        public string GetDetailedDescription()
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(Description))
                parts.Add(Description);
            else
                parts.Add($"A {Rarity.ToLower()} {ItemType.ToLower()} for hunters.");

            parts.Add($"Power Level: {GetPowerLevel()}");
            
            var statBonus = GetStatBonusDescription();
            if (statBonus != "No bonuses")
                parts.Add($"Bonuses: {statBonus}");

            var requirements = GetUnlockRequirementText();
            if (requirements != "No requirements")
                parts.Add($"Requirements: {requirements}");

            return string.Join(" | ", parts);
        }

        // Validaciones
        public List<string> ValidateData()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ItemName))
                errors.Add("Item name is required");

            var validTypes = new[] { "Weapon", "Armor", "Accessory" };
            if (!validTypes.Contains(ItemType))
                errors.Add("Invalid item type");

            var validRarities = new[] { "Common", "Rare", "Epic", "Legendary", "Mythic" };
            if (!validRarities.Contains(Rarity))
                errors.Add("Invalid rarity");

            if (UnlockLevel < 1)
                errors.Add("Unlock level must be at least 1");

            var validRanks = new[] { "E", "D", "C", "B", "A", "S", "SS", "SSS" };
            if (!validRanks.Contains(UnlockRank))
                errors.Add("Invalid unlock rank");

            if (XPMultiplier < 0.5m || XPMultiplier > 3.0m)
                errors.Add("XP multiplier must be between 0.5 and 3.0");

            if (StrengthBonus < 0 || AgilityBonus < 0 || VitalityBonus < 0 || EnduranceBonus < 0)
                errors.Add("Stat bonuses cannot be negative");

            return errors;
        }

        // Override para mejor debugging
        public override string ToString()
        {
            return $"{Rarity} {ItemType}: {ItemName} (Power: {GetPowerLevel()})";
        }

        public override bool Equals(object? obj)
        {
            if (obj is Equipment other)
            {
                return EquipmentID == other.EquipmentID;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return EquipmentID.GetHashCode();
        }
    }
}