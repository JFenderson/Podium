using Podium.Core.Entities;
using Podium.Core.Constants;

namespace Podium.Tests.Builders
{
    /// <summary>
    /// Builder pattern for creating ScholarshipOffer test entities
    /// </summary>
    public class ScholarshipOfferBuilder
    {
        private ScholarshipOffer _offer = new ScholarshipOffer
        {
            ScholarshipAmount = 10000,
            OfferType = "Merit-Based",
            Description = "Test scholarship offer",
            Requirements = "Maintain 3.0 GPA",
            Status = ScholarshipStatus.Sent,
            ExpirationDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        public ScholarshipOfferBuilder WithId(int id)
        {
            _offer.Id = id;
            return this;
        }

        public ScholarshipOfferBuilder WithAmount(decimal amount)
        {
            _offer.ScholarshipAmount = amount;
            return this;
        }

        public ScholarshipOfferBuilder WithType(string type)
        {
            _offer.OfferType = type;
            return this;
        }

        public ScholarshipOfferBuilder WithDescription(string description)
        {
            _offer.Description = description;
            return this;
        }

        public ScholarshipOfferBuilder WithRequirements(string requirements)
        {
            _offer.Requirements = requirements;
            return this;
        }

        public ScholarshipOfferBuilder WithStatus(ScholarshipStatus status)
        {
            _offer.Status = status;
            return this;
        }

        public ScholarshipOfferBuilder WithExpirationDate(DateTime deadline)
        {
            _offer.ExpirationDate = deadline;
            return this;
        }

        public ScholarshipOfferBuilder ForStudent(int studentId)
        {
            _offer.StudentId = studentId;
            return this;
        }

        public ScholarshipOfferBuilder FromBand(int bandId)
        {
            _offer.BandId = bandId;
            return this;
        }

        public ScholarshipOfferBuilder CreatedBy(string userId)
        {
            _offer.CreatedByUserId = userId;
            return this;
        }

        public ScholarshipOfferBuilder WithCreatedAt(DateTime createdAt)
        {
            _offer.CreatedAt = createdAt;
            return this;
        }

        public ScholarshipOfferBuilder AsActive(bool isDeleted = false)
        {
            _offer.IsDeleted = isDeleted;
            return this;
        }

        public ScholarshipOfferBuilder AsPending()
        {
            _offer.Status = ScholarshipStatus.PendingApproval;
            return this;
        }

        public ScholarshipOfferBuilder AsAccepted()
        {
            _offer.Status = ScholarshipStatus.Accepted;
            _offer.ResponseDate = DateTime.UtcNow;
            return this;
        }

        public ScholarshipOfferBuilder AsDeclined()
        {
            _offer.Status = ScholarshipStatus.Declined;
            _offer.ResponseDate = DateTime.UtcNow;
            return this;
        }

        public ScholarshipOfferBuilder AsExpired()
        {
            _offer.Status = ScholarshipStatus.Expired;
            _offer.ExpirationDate = DateTime.UtcNow.AddDays(-1);
            return this;
        }

        public ScholarshipOffer Build()
        {
            return _offer;
        }

        public static ScholarshipOfferBuilder Default()
        {
            return new ScholarshipOfferBuilder();
        }

        public static ScholarshipOfferBuilder FullRide()
        {
            return new ScholarshipOfferBuilder()
                .WithAmount(50000)
                .WithType("Full Ride")
                .WithDescription("Complete tuition and fees coverage");
        }

        public static ScholarshipOfferBuilder PartialScholarship(decimal amount)
        {
            return new ScholarshipOfferBuilder()
                .WithAmount(amount)
                .WithType("Partial Scholarship");
        }
    }
}
