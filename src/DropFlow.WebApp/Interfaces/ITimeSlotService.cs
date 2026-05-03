using DropFlow.Shared.Common;
using DropFlow.Shared.TimeSlots;

namespace DropFlow.WebApp.Interfaces;

/// <summary>
/// Service de gestion des créneaux horaires (TimeSlots) avec cache
/// </summary>
public interface ITimeSlotService
{
    /// <summary>
    /// Récupère tous les créneaux avec mise en cache
    /// </summary>
    Task<List<TimeSlotDto>> GetAllAsync(bool forceRefresh = false);

    /// <summary>
    /// Récupère un créneau par ID
    /// </summary>
    Task<TimeSlotDto?> GetByIdAsync(int id);

    /// <summary>
    /// Crée un nouveau créneau
    /// </summary>
    Task<ResponseResult<int>> CreateAsync(CreateTimeSlotDto dto);

    /// <summary>
    /// Met à jour un créneau existant
    /// </summary>
    Task<ResponseResult> UpdateAsync(int id, UpdateTimeSlotDto dto);

    /// <summary>
    /// Supprime un créneau
    /// </summary>
    Task<ResponseResult> DeleteAsync(int id);

    /// <summary>
    /// Invalide le cache des créneaux
    /// </summary>
    void InvalidateCache();

    /// <summary>
    /// Force le rechargement depuis l'API
    /// </summary>
    Task<List<TimeSlotDto>> RefreshAsync();
}