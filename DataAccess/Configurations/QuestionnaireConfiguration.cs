using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class QuestionnaireConfiguration : IEntityTypeConfiguration<Questionnaire>
    {
        public void Configure(EntityTypeBuilder<Questionnaire> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).HasMaxLength(200);
        }
    }

    public class QuestionnaireQuestionConfiguration : IEntityTypeConfiguration<QuestionnaireQuestion>
    {
        public void Configure(EntityTypeBuilder<QuestionnaireQuestion> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
            builder.Property(x => x.AnswerPlaceholder).HasMaxLength(300);

            builder.HasOne(x => x.Questionnaire)
                .WithMany(x => x.Questions)
                .HasForeignKey(x => x.QuestionnaireId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class QuestionnaireAnswerConfiguration : IEntityTypeConfiguration<QuestionnaireAnswer>
    {
        public void Configure(EntityTypeBuilder<QuestionnaireAnswer> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);

            // One answer per member per question.
            builder.HasIndex(x => new { x.QuestionnaireQuestionId, x.UserId }).IsUnique();

            builder.Property(x => x.Answer).HasMaxLength(2000);

            // A question the admin deletes takes its answers with it; the confirmation
            // on that delete says so.
            builder.HasOne(x => x.QuestionnaireQuestion)
                .WithMany(x => x.Answers)
                .HasForeignKey(x => x.QuestionnaireQuestionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
