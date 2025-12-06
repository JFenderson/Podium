namespace Podium.Application.DTOs.Guardian
{
    public class StudentNotificationOverrideDto
    {
        public int StudentId { get; set; }
        public bool? NotifyOnNewOffer { get; set; }
        public bool? NotifyOnContactRequest { get; set; }
        public bool? NotifyOnVideoUpload { get; set; }
    }
}