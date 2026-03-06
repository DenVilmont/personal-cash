using Domain.Contracts;
using Infrastructure.Models;

namespace Infrastructure.Mapping
{
    public static class CategoryMapping
    {
        public static CategoryDto ToDto(this Category m) => new()
        {
            Id = m.Id,
            UserId = m.UserId,
            Name = m.Name,
            CreatedAt = m.CreatedAt
        };

        public static Category ToModel(this CategoryDto d)
        {
            var m = new Category
            {
                Id = d.Id,
                UserId = d.UserId,
                Name = d.Name
            };
            if (d.CreatedAt != default)
                m.CreatedAt = d.CreatedAt;
            return m;
        }
    }
}
