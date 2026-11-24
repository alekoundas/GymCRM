using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Tests.Shared;

namespace DataAccess.Tests.Configurations
{
    /// <summary>
    /// Every table as the code describes it: each column, whether it may be empty, how long it may
    /// be, what is unique, and what happens to it when the row it points at is deleted. Read it once
    /// in Model.approved.txt; after that any line of any configuration class that changes is named.
    /// </summary>
    public sealed class ModelTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        [Fact]
        public void The_model_is_the_approved_one()
        {
            using ApiDbContext db = _database.Open();

            StringBuilder text = new StringBuilder();
            foreach (IEntityType entity in db.Model.GetEntityTypes().OrderBy(x => x.GetTableName(), StringComparer.Ordinal))
                Describe(entity, text);

            ApprovedFile.ShouldMatch(text.ToString(), "Model");
        }

        private static void Describe(IEntityType entity, StringBuilder text)
        {
            text.AppendLine(entity.GetTableName());

            foreach (IProperty property in entity.GetProperties().OrderBy(x => x.Name, StringComparer.Ordinal))
                text.AppendLine($"  {property.Name}: {Describe(property)}");

            foreach (IKey key in entity.GetKeys())
                text.AppendLine($"  key ({NamesOf(key.Properties)})");

            foreach (IIndex index in entity.GetIndexes().OrderBy(x => NamesOf(x.Properties), StringComparer.Ordinal))
                text.AppendLine($"  {(index.IsUnique ? "unique" : "index")} ({NamesOf(index.Properties)})");

            foreach (IForeignKey foreignKey in entity.GetForeignKeys().OrderBy(x => NamesOf(x.Properties), StringComparer.Ordinal))
                text.AppendLine($"  {NamesOf(foreignKey.Properties)} -> {foreignKey.PrincipalEntityType.GetTableName()}, {foreignKey.DeleteBehavior}{(foreignKey.IsRequired ? ", required" : "")}");

            text.AppendLine();
        }

        private static string Describe(IProperty property)
        {
            StringBuilder text = new StringBuilder();

            text.Append(property.GetColumnType());

            if (property.IsNullable)
                text.Append(", may be empty");

            if (property.GetMaxLength() != null)
                text.Append($", at most {property.GetMaxLength()}");

            if (property.ValueGenerated != ValueGenerated.Never)
                text.Append($", made {property.ValueGenerated}");

            if (property.IsConcurrencyToken)
                text.Append(", checked when saved");

            return text.ToString();
        }

        private static string NamesOf(IEnumerable<IReadOnlyProperty> properties)
        {
            return string.Join(", ", properties.Select(x => x.Name));
        }
    }
}
