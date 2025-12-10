using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Constants
{
    public static class GuardianRelationshipTypes
    {
        public const string Mother = "Mother";
        public const string Father = "Father";
        public const string LegalGuardian = "Legal Guardian";
        public const string StepMother = "Step-Mother";
        public const string StepFather = "Step-Father";
        public const string Grandparent = "Grandparent";
        public const string Aunt = "Aunt";
        public const string Uncle = "Uncle";
        public const string Other = "Other";

        // Helper to get all as a list (useful for frontend dropdowns)
        public static readonly string[] All = {
            Mother, Father, LegalGuardian, StepMother, StepFather, Grandparent, Aunt, Uncle, Other
        };
    }
}
