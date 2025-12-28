using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.BandStaff
{
    public class SearchSuggestionDto
    {
        public string Text { get; set; }
        public string Type { get; set; } // student, instrument, location, school
        public object? Metadata { get; set; }
    }
}
