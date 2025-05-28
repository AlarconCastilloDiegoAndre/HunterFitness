using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HunterFitness.API.Data;
using HunterFitness.API.Models;
using HunterFitness.API.DTOs;

namespace HunterFitness.API.Services
{
    public interface IEquipmentService
    {
        Task<HunterInventoryDto> GetHunterInventoryAsync(Guid hunterId);
        Task<EquipmentOperationResponseDto> EquipItemAsync(Guid hunterId, EquipItemDto equipDto);
        Task<EquipmentOperationResponseDto> UnequipItemAsync(Guid hunterId, EquipItemDto equipDto);
        Task<List<EquipmentDto>> GetAvailableEquipmentAsync(Guid hunterId);
        Task<bool> UnlockEquipmentAsync(Guid hunterId, Guid equipmentId, string unlockReason = "Achievement");
        Task<List<HunterEquipmentDto>> GetEquippedItemsAsync(Guid hunterId);
        Task<Dictionary<string, object>> GetInventoryStatsAsync(Guid hunterId);
    }

    public class EquipmentService : IEquipmentService
    {
        private readonly HunterFitnessDbContext _context;
        private readonly ILogger<EquipmentService> _logger;
        private readonly IHunterService _hunterService;

        public EquipmentService(
            HunterFitnessDbContext context,
            ILogger<EquipmentService> logger,
            IHunterService hunterService)
        {
            _context = context;
            _logger = logger;
            _hunterService = hunterService;
        }

        public async Task<HunterInventoryDto> GetHunterInventoryAsync(Guid hunterId)
        {
            try
            {
                var hunter = await _context.Hunters
                    .Include(h => h.Equipment)
                        .ThenInclude(he => he.Equipment)
                    .FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);

                if (hunter == null)
                {
                    throw new ArgumentException("Hunter not found");
                }

                var hunterEquipment = hunter.Equipment?.Where(he => he.Equipment != null).ToList() 
                    ?? new List<HunterEquipment>();

                var allItems = hunterEquipment.Select(ConvertToHunterEquipmentDto).ToList();
                var equippedItems = allItems.Where(item => item.IsEquipped).ToList();

                // Agrupar por tipo
                var weapons = allItems.Where(item => item.ItemType == "Weapon").ToList();
                var armor = allItems.Where(item => item.ItemType == "Armor").ToList();
                var accessories = allItems.Where(item => item.ItemType == "Accessory").ToList();

                // Estadísticas por rareza
                var itemsByRarity = allItems
                    .GroupBy(item => item.Rarity)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Estadísticas por tipo
                var itemsByType = allItems
                    .GroupBy(item => item.ItemType)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Bonificaciones totales del equipamiento
                var totalStrengthBonus = equippedItems.Sum(item => item.StrengthBonus);
                var totalAgilityBonus = equippedItems.Sum(item => item.AgilityBonus);
                var totalVitalityBonus = equippedItems.Sum(item => item.VitalityBonus);
                var totalEnduranceBonus = equippedItems.Sum(item => item.EnduranceBonus);
                var totalXPMultiplier = equippedItems.Sum(item => item.XPMultiplier - 1.0m) + 1.0m;
                var totalPowerLevel = equippedItems.Sum(item => item.PowerScore);

                // Items recientes (últimos 7 días)
                var recentlyUnlocked = allItems
                    .Where(item => item.IsNewlyUnlocked)
                    .OrderByDescending(item => item.UnlockedAt)
                    .ToList();

                return new HunterInventoryDto
                {
                    HunterID = hunterId,
                    HunterName = hunter.HunterName,
                    EquippedItems = equippedItems,
                    AllItems = allItems,
                    Weapons = weapons,
                    Armor = armor,
                    Accessories = accessories,
                    TotalItems = allItems.Count,
                    EquippedItemsCount = equippedItems.Count,
                    UnequippedItems = allItems.Count - equippedItems.Count,
                    ItemsByRarity = itemsByRarity,
                    ItemsByType = itemsByType,
                    TotalStrengthBonus = totalStrengthBonus,
                    TotalAgilityBonus = totalAgilityBonus,
                    TotalVitalityBonus = totalVitalityBonus,
                    TotalEnduranceBonus = totalEnduranceBonus,
                    TotalXPMultiplier = totalXPMultiplier,
                    TotalPowerLevel = totalPowerLevel,
                    RecentlyUnlocked = recentlyUnlocked
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting hunter inventory: {HunterID}", hunterId);
                throw;
            }
        }

        public async Task<EquipmentOperationResponseDto> EquipItemAsync(Guid hunterId, EquipItemDto equipDto)
        {
            try
            {
                var hunterEquipment = await _context.HunterEquipment
                    .Include(he => he.Equipment)
                    .Include(he => he.Hunter)
                    .FirstOrDefaultAsync(he => he.HunterEquipmentID == equipDto.HunterEquipmentID && 
                                             he.HunterID == hunterId);

                if (hunterEquipment == null)
                {
                    return new EquipmentOperationResponseDto
                    {
                        Success = false,
                        Message = "Equipment not found in your inventory."
                    };
                }

                if (hunterEquipment.IsEquipped)
                {
                    return new EquipmentOperationResponseDto
                    {
                        Success = false,
                        Message = "Item is already equipped."
                    };
                }

                if (!hunterEquipment.CanBeEquipped())
                {
                    return new EquipmentOperationResponseDto
                    {
                        Success = false,
                        Message = hunterEquipment.GetEquipmentStatusText()
                    };
                }

                // Desequipar otros items del mismo tipo
                var affectedItems = new List<HunterEquipmentDto>();
                var sameTypeEquipped = await _context.HunterEquipment
                    .Include(he => he.Equipment)
                    .Where(he => he.HunterID == hunterId && 
                               he.Equipment.ItemType == hunterEquipment.Equipment.ItemType && 
                               he.IsEquipped)
                    .ToListAsync();

                foreach (var item in sameTypeEquipped)
                {
                    item.UnequipItem();
                    affectedItems.Add(ConvertToHunterEquipmentDto(item));
                }

                // Equipar el nuevo item
                hunterEquipment.EquipItem();

                await _context.SaveChangesAsync();

                var updatedStats = await _hunterService.GetHunterStatsAsync(hunterId);

                _logger.LogInformation("⚔️ Equipment equipped: {ItemName} by Hunter {HunterID}", 
                    hunterEquipment.Equipment.ItemName, hunterId);

                return new EquipmentOperationResponseDto
                {
                    Success = true,
                    Message = $"⚔️ {hunterEquipment.Equipment.ItemName} equipped! Your power grows!",
                    Equipment = ConvertToHunterEquipmentDto(hunterEquipment),
                    UpdatedStats = updatedStats,
                    AffectedItems = affectedItems
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error equipping item: {HunterEquipmentID}", equipDto.HunterEquipmentID);
                return new EquipmentOperationResponseDto
                {
                    Success = false,
                    Message = "An error occurred while equipping the item."
                };
            }
        }

        public async Task<EquipmentOperationResponseDto> UnequipItemAsync(Guid hunterId, EquipItemDto equipDto)
        {
            try
            {
                var hunterEquipment = await _context.HunterEquipment
                    .Include(he => he.Equipment)
                    .Include(he => he.Hunter)
                    .FirstOrDefaultAsync(he => he.HunterEquipmentID == equipDto.HunterEquipmentID && 
                                             he.HunterID == hunterId);

                if (hunterEquipment == null)
                {
                    return new EquipmentOperationResponseDto
                    {
                        Success = false,
                        Message = "Equipment not found in your inventory."
                    };
                }

                if (!hunterEquipment.IsEquipped)
                {
                    return new EquipmentOperationResponseDto
                    {
                        Success = false,
                        Message = "Item is not currently equipped."
                    };
                }

                hunterEquipment.UnequipItem();
                await _context.SaveChangesAsync();

                var updatedStats = await _hunterService.GetHunterStatsAsync(hunterId);

                _logger.LogInformation("📦 Equipment unequipped: {ItemName} by Hunter {HunterID}", 
                    hunterEquipment.Equipment.ItemName, hunterId);

                return new EquipmentOperationResponseDto
                {
                    Success = true,
                    Message = $"📦 {hunterEquipment.Equipment.ItemName} unequipped.",
                    Equipment = ConvertToHunterEquipmentDto(hunterEquipment),
                    UpdatedStats = updatedStats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error unequipping item: {HunterEquipmentID}", equipDto.HunterEquipmentID);
                return new EquipmentOperationResponseDto
                {
                    Success = false,
                    Message = "An error occurred while unequipping the item."
                };
            }
        }

        public async Task<List<EquipmentDto>> GetAvailableEquipmentAsync(Guid hunterId)
        {
            try
            {
                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null) return new List<EquipmentDto>();

                // Obtener todos los equipment
                var allEquipment = await _context.Equipment
                    .Where(e => e.IsActive)
                    .OrderBy(e => e.ItemType)
                    .ThenBy(e => e.GetRarityValue())
                    .ThenBy(e => e.UnlockLevel)
                    .ToListAsync();

                // Obtener equipment que ya posee el hunter
                var ownedEquipmentIds = await _context.HunterEquipment
                    .Where(he => he.HunterID == hunterId)
                    .Select(he => he.EquipmentID)
                    .ToHashSetAsync();

                var equipmentDtos = new List<EquipmentDto>();

                foreach (var equipment in allEquipment)
                {
                    var isOwned = ownedEquipmentIds.Contains(equipment.EquipmentID);
                    var isEquipped = false;
                    var unlockedAt = (DateTime?)null;
                    var isNewlyUnlocked = false;

                    if (isOwned)
                    {
                        var hunterEquipment = await _context.HunterEquipment
                            .FirstOrDefaultAsync(he => he.HunterID == hunterId && he.EquipmentID == equipment.EquipmentID);
                        
                        if (hunterEquipment != null)
                        {
                            isEquipped = hunterEquipment.IsEquipped;
                            unlockedAt = hunterEquipment.UnlockedAt;
                            isNewlyUnlocked = hunterEquipment.IsNewlyUnlocked();
                        }
                    }

                    var dto = new EquipmentDto
                    {
                        EquipmentID = equipment.EquipmentID,
                        ItemName = equipment.ItemName,
                        Description = equipment.Description,
                        ItemType = equipment.ItemType,
                        ItemTypeIcon = equipment.GetItemTypeIcon(),
                        ItemTypeDisplayName = equipment.GetItemTypeDisplayName(),
                        Rarity = equipment.Rarity,
                        RarityDisplayName = equipment.GetRarityDisplayName(),
                        RarityColor = equipment.GetRarityColor(),
                        RarityStars = equipment.GetRarityStars(),
                        RarityValue = equipment.GetRarityValue(),
                        StrengthBonus = equipment.StrengthBonus,
                        AgilityBonus = equipment.AgilityBonus,
                        VitalityBonus = equipment.VitalityBonus,
                        EnduranceBonus = equipment.EnduranceBonus,
                        TotalStatBonus = equipment.GetTotalStatBonus(),
                        XPMultiplier = equipment.XPMultiplier,
                        StatBonusDescription = equipment.GetStatBonusDescription(),
                        UnlockLevel = equipment.UnlockLevel,
                        UnlockRank = equipment.UnlockRank,
                        UnlockCondition = equipment.UnlockCondition,
                        UnlockRequirementText = equipment.GetUnlockRequirementText(),
                        IsEligible = equipment.IsEligibleForHunter(hunter),
                        IneligibilityReason = !equipment.IsEligibleForHunter(hunter) 
                            ? $"Requires Level {equipment.UnlockLevel} and {equipment.UnlockRank} Rank"
                            : null,
                        PowerLevel = equipment.GetPowerLevel(),
                        HasXPBonus = equipment.HasXPBonus(),
                        HasStatBonuses = equipment.HasStatBonuses(),
                        IsHighTier = equipment.IsHighTier(),
                        FlavorText = equipment.GetFlavorText(),
                        SpecialEffects = equipment.GetSpecialEffects(),
                        IsOwned = isOwned,
                        IsEquipped = isEquipped,
                        UnlockedAt = unlockedAt,
                        IsNewlyUnlocked = isNewlyUnlocked,
                        ShouldShowGlow = isNewlyUnlocked || equipment.IsHighTier(),
                        IconUrl = equipment.IconUrl,
                        CreatedAt = equipment.CreatedAt
                    };

                    equipmentDtos.Add(dto);
                }

                return equipmentDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting available equipment for hunter: {HunterID}", hunterId);
                return new List<EquipmentDto>();
            }
        }

        public async Task<bool> UnlockEquipmentAsync(Guid hunterId, Guid equipmentId, string unlockReason = "Achievement")
        {
            try
            {
                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null) return false;

                var equipment = await _context.Equipment.FirstOrDefaultAsync(e => e.EquipmentID == equipmentId && e.IsActive);
                if (equipment == null) return false;

                // Verificar si ya lo posee
                var existingHunterEquipment = await _context.HunterEquipment
                    .FirstOrDefaultAsync(he => he.HunterID == hunterId && he.EquipmentID == equipmentId);

                if (existingHunterEquipment != null) return true; // Ya lo posee

                // Verificar elegibilidad
                if (!equipment.IsEligibleForHunter(hunter))
                {
                    _logger.LogWarning("🚫 Equipment unlock failed - not eligible: {ItemName} for Hunter {HunterID}", 
                        equipment.ItemName, hunterId);
                    return false;
                }

                // Crear nueva entrada de equipment
                var hunterEquipment = new HunterEquipment
                {
                    HunterID = hunterId,
                    EquipmentID = equipmentId,
                    UnlockedAt = DateTime.UtcNow
                };

                _context.HunterEquipment.Add(hunterEquipment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("🎁 Equipment unlocked: {ItemName} for Hunter {HunterID} - Reason: {Reason}", 
                    equipment.ItemName, hunterId, unlockReason);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error unlocking equipment: {EquipmentID} for Hunter {HunterID}", 
                    equipmentId, hunterId);
                return false;
            }
        }

        public async Task<List<HunterEquipmentDto>> GetEquippedItemsAsync(Guid hunterId)
        {
            try
            {
                var equippedItems = await _context.HunterEquipment
                    .Include(he => he.Equipment)
                    .Where(he => he.HunterID == hunterId && he.IsEquipped)
                    .ToListAsync();

                return equippedItems.Select(ConvertToHunterEquipmentDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting equipped items for hunter: {HunterID}", hunterId);
                return new List<HunterEquipmentDto>();
            }
        }

        public async Task<Dictionary<string, object>> GetInventoryStatsAsync(Guid hunterId)
        {
            try
            {
                var inventory = await GetHunterInventoryAsync(hunterId);
                
                return new Dictionary<string, object>
                {
                    {"TotalItems", inventory.TotalItems},
                    {"EquippedItems", inventory.EquippedItemsCount},
                    {"UnequippedItems", inventory.UnequippedItems},
                    {"ItemsByRarity", inventory.ItemsByRarity},
                    {"ItemsByType", inventory.ItemsByType},
                    {"TotalPowerLevel", inventory.TotalPowerLevel},
                    {"TotalStatBonuses", new {
                        Strength = inventory.TotalStrengthBonus,
                        Agility = inventory.TotalAgilityBonus,
                        Vitality = inventory.TotalVitalityBonus,
                        Endurance = inventory.TotalEnduranceBonus
                    }},
                    {"XPMultiplier", inventory.TotalXPMultiplier},
                    {"RecentUnlocks", inventory.RecentlyUnlocked.Count},
                    {"HighTierItems", inventory.AllItems.Count(i => i.Rarity == "Legendary" || i.Rarity == "Mythic")},
                    {"CompletionPercentage", GetInventoryCompletionPercentage(inventory)}
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting inventory stats for hunter: {HunterID}", hunterId);
                return new Dictionary<string, object>();
            }
        }

        // Helper methods
        private HunterEquipmentDto ConvertToHunterEquipmentDto(HunterEquipment hunterEquipment)
        {
            return new HunterEquipmentDto
            {
                HunterEquipmentID = hunterEquipment.HunterEquipmentID,
                EquipmentID = hunterEquipment.EquipmentID,
                ItemName = hunterEquipment.Equipment.ItemName,
                Description = hunterEquipment.Equipment.Description,
                ItemType = hunterEquipment.Equipment.ItemType,
                ItemTypeIcon = hunterEquipment.GetItemTypeIcon(),
                Rarity = hunterEquipment.Equipment.Rarity,
                RarityColor = hunterEquipment.GetRarityColor(),
                RarityStars = hunterEquipment.Equipment.GetRarityStars(),
                StrengthBonus = hunterEquipment.Equipment.StrengthBonus,
                AgilityBonus = hunterEquipment.Equipment.AgilityBonus,
                VitalityBonus = hunterEquipment.Equipment.VitalityBonus,
                EnduranceBonus = hunterEquipment.Equipment.EnduranceBonus,
                TotalStatBonus = hunterEquipment.Equipment.GetTotalStatBonus(),
                XPMultiplier = hunterEquipment.Equipment.XPMultiplier,
                StatBonusDescription = hunterEquipment.GetStatBonusDescription(),
                PowerScore = hunterEquipment.GetPowerScore(),
                IsEquipped = hunterEquipment.IsEquipped,
                CanBeEquipped = hunterEquipment.CanBeEquipped(),
                EquipmentStatusText = hunterEquipment.GetEquipmentStatusText(),
                UnlockedAt = hunterEquipment.UnlockedAt,
                TimeOwned = FormatTimeOwned(hunterEquipment.GetTimeOwned()),
                IsNewlyUnlocked = hunterEquipment.IsNewlyUnlocked(),
                ShouldShowEquipmentGlow = hunterEquipment.ShouldShowEquipmentGlow(),
                IconUrl = hunterEquipment.Equipment.IconUrl,
                SpecialEffects = hunterEquipment.Equipment.GetSpecialEffects(),
                FlavorText = hunterEquipment.Equipment.GetFlavorText()
            };
        }

        private string FormatTimeOwned(TimeSpan timeOwned)
        {
            if (timeOwned.TotalDays >= 1)
                return $"{(int)timeOwned.TotalDays} days ago";
            else if (timeOwned.TotalHours >= 1)
                return $"{(int)timeOwned.TotalHours} hours ago";
            else if (timeOwned.TotalMinutes >= 1)
                return $"{(int)timeOwned.TotalMinutes} minutes ago";
            else
                return "Just unlocked";
        }

        private decimal GetInventoryCompletionPercentage(HunterInventoryDto inventory)
        {
            if (inventory.TotalItems == 0) return 0m;
            
            // Este cálculo se basa en el equipment total disponible vs el que posee
            // En una implementación real, necesitarías obtener el total de equipment disponible
            var totalAvailableEquipment = 100; // Placeholder - debería obtenerse de la base de datos
            
            return Math.Min(100m, (decimal)inventory.TotalItems / totalAvailableEquipment * 100m);
        }
    }
}