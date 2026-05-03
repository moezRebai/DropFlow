using DropFlow.Shared.Common;
using DropFlow.Shared.TimeSlots;

namespace DropFlow.Application.Interfaces;

public interface ITimeSlotService
{
    Task<List<TimeSlotDto>> GetAllAsync();
    Task<TimeSlotDto?> GetByIdAsync(int id);
    Task<ResponseResult<int>> CreateAsync(CreateTimeSlotDto dto);
    Task<ResponseResult> UpdateAsync(int id, UpdateTimeSlotDto dto);
    Task<ResponseResult> DeleteAsync(int id);
}