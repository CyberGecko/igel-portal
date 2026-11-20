using IgelPortal.Web.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IgelPortal.Web.Extensions
{
    public static class EntityTypeBuilderExtensions
    {
        public static EntityTypeBuilder<TEntity> AddBaseMappings<TEntity>(this EntityTypeBuilder<TEntity> builder) where TEntity : EntityBase
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id);
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
            builder.Property(e => e.CreationDate).IsRequired();
            builder.Property(e => e.ChangeDate).IsRequired();
            builder.Property(e => e.DeletedDate);

            return builder;
        }
    }
}
