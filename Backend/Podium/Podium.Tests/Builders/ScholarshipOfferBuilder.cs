using Podium.Core.Entities;

namespace Podium.Tests.Builders
{
    /// <summary>
    /// Builder pattern for creating ScholarshipOffer test entities
    /// </summary>
    public class ScholarshipOfferBuilder
    {
        private ScholarshipOffer _offer = new ScholarshipOffer
        {
            Amount = 10000,
            ScholarshipType = "Merit-Based",
            Description = "Test scholarship offer",
            Requirements = "Maintain 3.0 GPA",
            Status = "Pending",
            Deadline = DateTime.UtcNow.AddDays(30),
            SentAt = DateTime.UtcNow,
            IsActive = true
        };

        public ScholarshipOfferBuilder WithId(int id)
        {
            _offer.Id = id;
            return this;
        }

        public ScholarshipOfferBuilder WithAmount(decimal amount)
        {
            _offer.Amount = amount;
            return this;
        }

        public ScholarshipOfferBuilder WithType(string type)
        {
            _offer.ScholarshipType = type;
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

        public ScholarshipOfferBuilder WithStatus(string status)
        {
            _offer.Status = status;
            return this;
        }

        public ScholarshipOfferBuilder WithDeadline(DateTime deadline)
        {
            _offer.Deadline = deadline;
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

        public ScholarshipOfferBuilder SentBy(string userId)
        {
            _offer.SentByUserId = userId;
            return this;
        }

        public ScholarshipOfferBuilder WithSentAt(DateTime sentAt)
        {
            _offer.SentAt = sentAt;
            return this;
        }

        public ScholarshipOfferBuilder AsActive(bool isActive = true)
        {
            _offer.IsActive = isActive;
            return this;
        }

        public ScholarshipOfferBuilder AsPending()
        {
            _offer.Status = "Pending";
            return this;
        }

        public ScholarshipOfferBuilder AsAccepted()
        {
            _offer.Status = "Accepted";
            _offer.RespondedAt = DateTime.UtcNow;
            return this;
        }

        public ScholarshipOfferBuilder AsRejected()
        {
            _offer.Status = "Rejected";
            _offer.RespondedAt = DateTime.UtcNow;
            return this;
        }

        public ScholarshipOfferBuilder AsExpired()
        {
            _offer.Status = "Expired";
            _offer.Deadline = DateTime.UtcNow.AddDays(-1);
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
