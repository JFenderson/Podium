using System.ComponentModel.DataAnnotations;

namespace Podium.Application.Validation
{
    /// <summary>
    /// Validates that an instrument is from the allowed list of band instruments
    /// </summary>
    public class InstrumentAttribute : ValidationAttribute
    {
        // Common band instruments - can be expanded as needed
        private static readonly string[] AllowedInstruments = new[]
        {
            // Woodwinds
            "Flute", "Piccolo", "Clarinet", "Bass Clarinet", "Oboe", "Bassoon",
            "Saxophone", "Alto Saxophone", "Tenor Saxophone", "Baritone Saxophone", "Soprano Saxophone",
            
            // Brass
            "Trumpet", "Cornet", "French Horn", "Horn", "Trombone", "Bass Trombone",
            "Euphonium", "Baritone", "Tuba", "Sousaphone",
            
            // Percussion
            "Percussion", "Drums", "Snare Drum", "Bass Drum", "Cymbals", "Timpani",
            "Xylophone", "Marimba", "Vibraphone", "Glockenspiel", "Bells",
            
            // Auxiliary
            "Piano", "Guitar", "Bass Guitar", "Synthesizer", "Harp",
            
            // General
            "Multiple Instruments", "Other"
        };

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success;
            }

            var instrument = value.ToString()!;

            // Case-insensitive comparison
            if (!AllowedInstruments.Any(i => i.Equals(instrument, StringComparison.OrdinalIgnoreCase)))
            {
                return new ValidationResult(
                    ErrorMessage ?? $"The field {validationContext.DisplayName} must be a valid instrument. Allowed values include: {string.Join(", ", AllowedInstruments.Take(10))}... and more."
                );
            }

            return ValidationResult.Success;
        }

        /// <summary>
        /// Gets the list of allowed instruments
        /// </summary>
        public static string[] GetAllowedInstruments() => AllowedInstruments;
    }
}
