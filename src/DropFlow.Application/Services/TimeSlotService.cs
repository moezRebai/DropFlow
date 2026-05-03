using DropFlow.Application.Interfaces;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Entities;
using DropFlow.Domain.Enums;
using DropFlow.Shared.Common;
using DropFlow.Shared.TimeSlots;
using Microsoft.EntityFrameworkCore;

namespace DropFlow.Application.Services;

public class TimeSlotService(
    IApplicationDbContext context,
    ITenantService tenantService,
    IAuditService auditService)
    : ITimeSlotService
{
    public async Task<List<TimeSlotDto>> GetAllAsync()
    {
        var timeSlots = await context.TimeSlots
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.StartTime)
            .ToListAsync();

        return timeSlots.Select(t => new TimeSlotDto
        {
            Id = t.Id,
            Name = t.Name,
            StartTime = t.StartTime,
            EndTime = t.EndTime,
            DisplayOrder = t.DisplayOrder
        }).ToList();
    }

    public async Task<TimeSlotDto?> GetByIdAsync(int id)
    {
        var timeSlot = await context.TimeSlots
            .FirstOrDefaultAsync(t => t.Id == id);

        return new TimeSlotDto
        {
            Id = timeSlot.Id,
            Name = timeSlot.Name,
            StartTime = timeSlot.StartTime,
            EndTime = timeSlot.EndTime,
            DisplayOrder = timeSlot.DisplayOrder
        };
    }

    public async Task<ResponseResult<int>> CreateAsync(CreateTimeSlotDto dto)
    {
        // Validation : EndTime > StartTime
        if (dto.EndTime <= dto.StartTime)
        {
            return ResponseResult<int>.Failure("L'heure de fin doit être après l'heure de début");
        }

        var timeSlot = new TimeSlot
        {
            Name = dto.Name,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
        };
        
        var tenantId = tenantService.GetTenantId();
        var currentUser = tenantService.GetCurrentUser();
        if (currentUser == null)
            return ResponseResult<int>.Failure("User not found");
        
        timeSlot.TenantId = tenantId;

        // Trouver le dernier DisplayOrder
        var maxOrder = await context.TimeSlots
            .MaxAsync(t => (int?)t.DisplayOrder) ?? 0;
        timeSlot.DisplayOrder = maxOrder + 1;

        context.TimeSlots.Add(timeSlot);
        await context.SaveChangesAsync(CancellationToken.None);

        await auditService.LogAsync(
            tenantId: tenantId,
            userId: currentUser.Id,
            action: "ClientCreated",
            entityName: nameof(TimeSlot),
            entityId: timeSlot.Id,
            changes: new
            {
                StartTime = timeSlot.StartTime,
                EndTime = timeSlot.EndTime
            },
            severity: AuditSeverity.Info);

        return ResponseResult<int>.Success(timeSlot.Id);
    }

    public async Task<ResponseResult> UpdateAsync(int id, UpdateTimeSlotDto dto)
    {
        var timeSlot = await context.TimeSlots
            .FirstOrDefaultAsync(t => t.Id == id);

        if (timeSlot == null)
        {
            return ResponseResult.Failure("Créneau introuvable");
        }

        // Validation
        if (dto.EndTime <= dto.StartTime)
        {
            return ResponseResult.Failure("L'heure de fin doit être après l'heure de début");
        }

        timeSlot.StartTime = dto.StartTime;
        timeSlot.EndTime = dto.EndTime;
        timeSlot.Name = dto.Name;
        timeSlot.DisplayOrder = dto.DisplayOrder;
        
        await context.SaveChangesAsync(CancellationToken.None);
        
        return ResponseResult.Success();
    }

    public async Task<ResponseResult> DeleteAsync(int id)
    {
        var timeSlot = await context.TimeSlots
            .FirstOrDefaultAsync(t => t.Id == id);

        if (timeSlot == null)
        {
            return ResponseResult.Failure("Créneau introuvable");
        }

        // Vérifier si utilisé dans des livraisons
        var usedInDeliveries = await context.Deliveries
            .AnyAsync(d => d.TimeSlotId == id);

        if (usedInDeliveries)
        {
            return ResponseResult.Failure(
                "Impossible de supprimer ce créneau car il est utilisé dans des livraisons. " +
                "Vous pouvez le désactiver à la place.");
        }

        context.TimeSlots.Remove(timeSlot);
        await context.SaveChangesAsync(CancellationToken.None);
        
        return ResponseResult.Success();
    }

}