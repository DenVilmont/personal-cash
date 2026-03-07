using Domain.Contracts;
using Infrastructure.Models;

namespace Infrastructure.Mapping
{
    public static class UserPageStateMapping
    {
        public static UserPageStateDto ToDto(this UserPageState model) => new()
        {
            Id = model.Id,
            UserId = model.UserId,
            PageKey = model.PageKey,
            StateJson = model.StateJson,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };

        public static UserPageState ToModel(this UserPageStateDto dto)
        {
            var model = new UserPageState
            {
                Id = dto.Id,
                UserId = dto.UserId,
                PageKey = dto.PageKey,
                StateJson = dto.StateJson,
                UpdatedAt = dto.UpdatedAt == default ? DateTimeOffset.UtcNow : dto.UpdatedAt
            };

            if (dto.CreatedAt != default)
                model.CreatedAt = dto.CreatedAt;

            return model;
        }
    }
}
