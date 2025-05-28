using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HunterFitness.API.Models
{
    public class HunterEquipment
    {
        [Key]
        public Guid HunterEquipmentID { get; set; } = Guid.NewGuid();

        [Required]
        public Guid HunterID { get; set; }

        [Required]
        public Guid EquipmentID { get; set; }

        // Estado del item
        public bool IsEquipped { get; set; } = false;
        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

        // Metadatos
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("HunterID")]
        public virtual Hunter Hunter { get; set; } = null!;

        [ForeignKey("EquipmentID")]
        public virtual Equipment Equipment { get; set; } = null!;

        // Métodos Helper
        public void EquipItem()
        {
            if (Equipment != null && CanBeEquipped())
            {
                // Desequipar otros items del mismo tipo si es necesario
                UnequipSameTypeItems();
                IsEquipped = true;
            }
        }

        public void UnequipItem()
        {
            IsEquipped = false;
        }

        public bool CanBeEquipped()
        {
            if (Equipment == null || Hunter == null) return false;

            // Verificar requisitos de nivel y rank
            if (Hunter.Level < Equipment.UnlockLevel) return false;

            var rankOrder = new Dictionary<string, int>
            {
                {"E", 1}, {"D", 2}, {"C", 3}, {"B", 4}, 
                {"A", 5}, {"S", 6}, {"SS", 7}, {"SSS", 8}
            };

            if (rankOrder.ContainsKey(Equipment.UnlockRank) && rankOrder.ContainsKey(Hunter.HunterRank))
            {
                return rankOrder[Hunter.HunterRank] >= rankOrder[Equipment.UnlockRank];
            }

            return true;
        }

        private void UnequipSameTypeItems()
        {
            if (Equipment == null || Hunter?.Equipment == null) return;

            // Desequipar otros items del mismo tipo
            foreach (var item in Hunter.Equipment.Where(e => e.Equipment?.ItemType == Equipment.ItemType && e.IsEquipped))
            {
                if (item.HunterEquipmentID != this.HunterEquipmentID)
                {
                    item.IsEquipped = false;
                }
            }
        }

        public int GetTotalStatBonus()
        {
            if (!IsEquipped || Equipment == null) return 0;

            return Equipment.StrengthBonus + Equipment.AgilityBonus + 
                   Equipment.VitalityBonus + Equipment.EnduranceBonus;
        }

        public string GetEquipmentStatusText()
        {
            if (IsEquipped)
                return "Equipped";

            if (!CanBeEquipped())
            {
                if (Equipment != null)
                {
                    if (Hunter != null && Hunter.Level < Equipment.UnlockLevel)
                        return $"Requires Level {Equipment.UnlockLevel}";
                    
                    if (Hunter != null && !IsRankRequirementMet())
                        return $"Requires {Equipment.UnlockRank} Rank";
                }
                return "Requirements not met";
            }

            return "Available";
        }

        private bool IsRankRequirementMet()
        {
            if (Equipment == null || Hunter == null) return false;

            var rankOrder = new Dictionary<string, int>
            {
                {"E", 1}, {"D", 2}, {"C", 3}, {"B", 4}, 
                {"A", 5}, {"S", 6}, {"SS", 7}, {"SSS", 8}
            };

            if (rankOrder.ContainsKey(Equipment.UnlockRank) && rankOrder.ContainsKey(Hunter.HunterRank))
            {
                return rankOrder[Hunter.HunterRank] >= rankOrder[Equipment.UnlockRank];
            }

            return true;
        }

        public string GetRarityColor()
        {
            if (Equipment == null) return "#757575";

            return Equipment.Rarity switch
            {
                "Common" => "#9E9E9E",     // Gris
                "Rare" => "#2196F3",      // Azul
                "Epic" => "#9C27B0",      // Púrpura
                "Legendary" => "#FF9800", // Naranja
                "Mythic" => "#F44336",    // Rojo
                _ => "#757575"            // Gris por defecto
            };
        }

        public string GetItemTypeIcon()
        {
            if (Equipment == null) return "⚡";

            return Equipment.ItemType switch
            {
                "Weapon" => "⚔️",
                "Armor" => "🛡️",
                "Accessory" => "💍",
                _ => "⚡"
            };
        }

        public TimeSpan GetTimeOwned()
        {
            return DateTime.UtcNow - UnlockedAt;
        }

        public bool IsNewlyUnlocked(int hours = 24)
        {
            return DateTime.UtcNow - UnlockedAt <= TimeSpan.FromHours(hours);
        }

        public string GetStatBonusDescription()
        {
            if (Equipment == null || !IsEquipped) return "No bonuses";

            var bonuses = new List<string>();

            if (Equipment.StrengthBonus > 0)
                bonuses.Add($"+{Equipment.StrengthBonus} STR");
            
            if (Equipment.AgilityBonus > 0)
                bonuses.Add($"+{Equipment.AgilityBonus} AGI");
            
            if (Equipment.VitalityBonus > 0)
                bonuses.Add($"+{Equipment.VitalityBonus} VIT");
            
            if (Equipment.EnduranceBonus > 0)
                bonuses.Add($"+{Equipment.EnduranceBonus} END");

            if (Equipment.XPMultiplier > 1.0m)
                bonuses.Add($"+{(Equipment.XPMultiplier - 1) * 100:F0}% XP");

            return bonuses.Any() ? string.Join(", ", bonuses) : "No stat bonuses";
        }

        public int GetPowerScore()
        {
            if (Equipment == null) return 0;

            var statTotal = Equipment.StrengthBonus + Equipment.AgilityBonus + 
                           Equipment.VitalityBonus + Equipment.EnduranceBonus;
            
            var xpBonus = (int)((Equipment.XPMultiplier - 1) * 10); // XP multiplier convertido a puntos
            
            var rarityMultiplier = Equipment.Rarity switch
            {
                "Common" => 1.0,
                "Rare" => 1.2,
                "Epic" => 1.5,
                "Legendary" => 2.0,
                "Mythic" => 3.0,
                _ => 1.0
            };

            return (int)((statTotal + xpBonus) * rarityMultiplier);
        }

        public bool ShouldShowEquipmentGlow()
        {
            return IsNewlyUnlocked(48) || // Brilla si es nuevo (48 horas)
                   (Equipment?.Rarity == "Legendary" || Equipment?.Rarity == "Mythic"); // Brilla si es legendario o mítico
        }
    }
}