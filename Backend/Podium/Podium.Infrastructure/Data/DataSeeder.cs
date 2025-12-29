using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Podium.Core.Constants;
using Podium.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Podium.Infrastructure.Data
{
    public static class DataSeeder
    {
        public static async Task SeedDataAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            // 1. Ensure Database is Created & Migrated
            await context.Database.MigrateAsync();

            // 2. Seed Roles
            await SeedRolesAsync(roleManager);

            // 3. Seed Admin User (CRITICAL FOR TESTS)
            await SeedAdminUserAsync(userManager);
            // 3. Seed Bands (Using the list we created earlier)
            if (!await context.Bands.AnyAsync())
            {
                var bands = GetBandsList();
                await context.Bands.AddRangeAsync(bands);
                await context.SaveChangesAsync();
            }

            await SeedBandBudgetsAsync(context);

            string commonPassword = "Password123!";

            // ==========================================
            // 4. SEED SPECIFIC TESTING ACCOUNTS (Fixed)
            // ==========================================

            // --- A. ASU Staff (Director & Recruiter) ---
            var asuBand = await context.Bands.FirstOrDefaultAsync(b => b.UniversityName == "Alabama State University");
            if (asuBand != null)
            {
                await CreateStaffUser(userManager, context, "director@asu.edu", "James", "Oliver", Roles.Director, asuBand.Id, "Director of Bands", commonPassword);
                await CreateStaffUser(userManager, context, "recruiter@asu.edu", "Sarah", "Jenkins", Roles.BandStaff, asuBand.Id, "Percussion Instructor", commonPassword);
                // Link Director to Band (Owner)
                var director = await userManager.FindByEmailAsync("director@asu.edu");
                if (director != null)
                {
                    asuBand.DirectorApplicationUserId = director.Id;
                }
            }

            // --- B. FAMU Staff (To test multiple bands) ---
            var famuBand = await context.Bands.FirstOrDefaultAsync(b => b.UniversityName == "Florida A&M University");
            if (famuBand != null)
            {
                await CreateStaffUser(userManager, context, "director@famu.edu", "William", "Foster", Roles.Director, famuBand.Id, "Director of Bands", commonPassword);
                await CreateStaffUser(userManager, context, "recruiter@famu.edu", "Robert", "Lee", Roles.BandStaff, famuBand.Id, "Woodwind Coordinator", commonPassword);
                // Link Director
                var director = await userManager.FindByEmailAsync("director@famu.edu");
                if (director != null)
                {
                    famuBand.DirectorApplicationUserId = director.Id;
                }
            }

            // --- C. Southern U Staff ---
            var suBand = await context.Bands.FirstOrDefaultAsync(b => b.UniversityName == "Southern University");
            if (suBand != null)
            {
                await CreateStaffUser(userManager, context, "director@subr.edu", "Isaac", "Greggs", Roles.Director, suBand.Id, "Director of Bands", commonPassword);
                var director = await userManager.FindByEmailAsync("director@subr.edu");
                if (director != null)
                {
                    suBand.DirectorApplicationUserId = director.Id;
                }
            }

            // --- D. Fixed Student (Jordan Smith) ---
            var mainStudent = await CreateStudentUser(userManager, context, "student@gmail.com", "Jordan", "Smith", "Tuba", "AL", 2025, commonPassword);

            // --- E. Fixed Guardian (Martha Smith) ---
            await CreateGuardianUser(userManager, context, "mom@gmail.com", "Martha", "Smith", mainStudent, GuardianRelationshipTypes.Mother, commonPassword);

            // ==========================================
            // 7. SEED TEST DATA FOR DASHBOARD
            // ==========================================
            if (mainStudent != null && asuBand != null)
            {
                await SeedTestInteractionData(context, mainStudent, asuBand);
            }


            // ==========================================
            // 5. SEED BULK RANDOM STUDENTS
            // ==========================================

            // Only seed if we don't have many students yet
            if (await context.Students.CountAsync() < 10)
            {
                await SeedRandomStudentsAsync(userManager, context, commonPassword);
            }
        }

        // --- HELPER METHODS ---

        private static async Task SeedBandBudgetsAsync(ApplicationDbContext context)
        {
            var currentYear = DateTime.UtcNow.Year;
            var bands = await context.Bands.ToListAsync();

            foreach (var band in bands)
            {
                if (!await context.BandBudgets.AnyAsync(bb => bb.BandId == band.Id && bb.FiscalYear == currentYear))
                {
                    context.BandBudgets.Add(new BandBudget
                    {
                        BandId = band.Id,
                        FiscalYear = currentYear,
                        TotalBudget = 500000m, // $500k default
                        AllocatedAmount = 0m,
                        RemainingAmount = 500000m,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System"
                    });
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedTestInteractionData(ApplicationDbContext context, Student student, Band band)
        {
            // 1. Fetch a valid Staff Member to act as the creator/recruiter
            // We try to find the Director or any staff member for this band
            var staffMember = await context.BandStaff
                .FirstOrDefaultAsync(bs => bs.BandId == band.Id);

            if (staffMember == null)
            {
                // Fallback: If no staff found (unlikely), skip seeding interaction data
                return;
            }

            // 1. Create a Pending Scholarship Offer (So Guardian has something to Approve)
            if (!await context.ScholarshipOffers.AnyAsync(o => o.StudentId == student.Id && o.BandId == band.Id))
            {
                context.ScholarshipOffers.Add(new ScholarshipOffer
                {
                    StudentId = student.Id,
                    BandId = band.Id,
                    OfferType = "Full Tuition",
                    ScholarshipAmount = 25000m,
                    Description = "Presidential Band Scholarship",
                    Status = ScholarshipStatus.Sent,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    ExpirationDate = DateTime.UtcNow.AddDays(28),
                    RequiresGuardianApproval = true,
                    CreatedByUserId = staffMember.ApplicationUserId, // Use real user ID
                    CreatedByStaffId = staffMember.Id
                });
            }

            // 2. Create a Pending Contact Request
            if (!await context.ContactRequests.AnyAsync(cr => cr.StudentId == student.Id && cr.BandId == band.Id))
            {
                context.ContactRequests.Add(new ContactRequest
                {
                    StudentId = student.Id,
                    BandId = band.Id,
                    Status = "Pending",
                    RequestedDate = DateTime.UtcNow.AddDays(-1),
                    IsUrgent = true, // To test "High Priority" badge
                    CreatedBy = staffMember.ApplicationUserId, // Use real user ID
                    BandStaffId = staffMember.Id
                });
            }

            // 3. Update Budget for the Offer
            var budget = await context.BandBudgets.FirstOrDefaultAsync(b => b.BandId == band.Id && b.FiscalYear == DateTime.UtcNow.Year);
            if (budget != null)
            {
                // Allocated includes pending offers in our logic
                budget.AllocatedAmount += 25000m;
                budget.RemainingAmount -= 25000m;
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
        {
            var adminEmail = "admin@podium.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Admin",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                    // IMPORTANT: Give Admin the Guardian role so they can access the Dashboard in tests
                    await userManager.AddToRoleAsync(adminUser, Roles.Guardian);
                }
            }
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = { Roles.Admin, Roles.Director, Roles.BandStaff, Roles.Student, Roles.Guardian };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        private static async Task CreateStaffUser(UserManager<ApplicationUser> userManager, ApplicationDbContext context, string email, string fName, string lName, string role, int bandId, string title, string password)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = fName,
                    LastName = lName,
                    IsActive = true,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, password);
                await userManager.AddToRoleAsync(user, role);
            }

            if (!context.BandStaff.Any(bs => bs.ApplicationUserId == user.Id && bs.BandId == bandId))
            {
                bool isDirector = role == Roles.Director;

                context.BandStaff.Add(new BandStaff
                {
                    BandId = bandId,
                    ApplicationUserId = user.Id,
                    FirstName = fName,
                    LastName = lName,
                    Role = role,
                    Title = title,
                    IsActive = true,
                    JoinedDate = DateTime.UtcNow,

                    // Set Permissions based on Role
                    CanViewStudents = true,
                    CanContact = true,
                    CanSendOffers = isDirector,
                    CanMakeOffers = isDirector, // New
                    CanViewFinancials = isDirector, // New
                    CanManageStaff = isDirector,
                    CanManageEvents = true,
                    CanRateStudents = true,

                    CreatedBy = "System"
                });
                await context.SaveChangesAsync();
            }
        }


        private static async Task<Student?> CreateStudentUser(UserManager<ApplicationUser> userManager, ApplicationDbContext context, string email, string fName, string lName, string instrument, string state, int gradYear, string password)
        {
            if (await userManager.FindByEmailAsync(email) == null)
            {
                var user = new ApplicationUser { UserName = email, Email = email, FirstName = fName, LastName = lName, IsActive = true, EmailConfirmed = true };
                await userManager.CreateAsync(user, password);
                await userManager.AddToRoleAsync(user, Roles.Student);

                var student = new Student
                {
                    ApplicationUserId = user.Id,
                    FirstName = fName,
                    LastName = lName,
                    Email = email,
                    PrimaryInstrument = instrument,
                    SecondaryInstruments = new List<string> { instrument },
                    GraduationYear = gradYear,
                    State = state,
                    LastActivityDate = DateTime.UtcNow,
                    HighSchool = "Test HS", // Added default to avoid null errors
                    GPA = 3.5m, // Added default
                    GuardianInviteCode = GenerateInviteCode(),
                };
                context.Students.Add(student);
                await context.SaveChangesAsync();
                return student;
            }
            return await context.Students.FirstOrDefaultAsync(s => s.Email == email);
        }

        private static string GenerateInviteCode()
        {
            // Simple 6-char code
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static async Task CreateGuardianUser(UserManager<ApplicationUser> userManager, ApplicationDbContext context, string email, string fName, string lName, Student? linkedStudent, string relationship, string password)
        {
            if (await userManager.FindByEmailAsync(email) == null)
            {
                var user = new ApplicationUser { UserName = email, Email = email, FirstName = fName, LastName = lName, IsActive = true, EmailConfirmed = true };
                await userManager.CreateAsync(user, password);
                await userManager.AddToRoleAsync(user, Roles.Guardian);

                var guardian = new Guardian { ApplicationUserId = user.Id, FirstName = fName, LastName = lName, Email = email, EmailNotificationsEnabled = true };
                context.Guardians.Add(guardian);
                await context.SaveChangesAsync();

                if (linkedStudent != null)
                {
                    context.StudentGuardians.Add(new StudentGuardian
                    {
                        StudentId = linkedStudent.Id,
                        GuardianId = guardian.Id,
                        RelationshipType = relationship,
                        IsVerified = true,
                        IsActive = true,
                        // Default Permissions for seeded guardian
                        CanViewActivity = true,
                        CanApproveContacts = true,
                        CanRespondToOffers = true,
                        ReceivesNotifications = true,
                        LinkedDate = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }
            }
        }

        private static async Task SeedRandomStudentsAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context, string password)
        {
            var instruments = new[] { "Trumpet", "Trombone", "Saxophone", "Clarinet", "Percussion", "Tuba", "Flute", "Mellophone", "Drum Major" };
            var states = new[] { "AL", "GA", "FL", "TX", "LA", "MS", "NC", "SC", "VA", "MD" };
            var firstNames = new[] { "Marcus", "Keisha", "Darius", "Jasmine", "Tyrell", "Aaliyah", "Isaiah", "Ebony", "Malik", "Brianna", "Xavier", "Chloe", "Andre", "Maya", "Trey" };
            var lastNames = new[] { "Johnson", "Williams", "Jones", "Brown", "Davis", "Miller", "Wilson", "Moore", "Taylor", "Anderson", "Thomas", "Jackson", "White", "Harris" };

            var random = new Random();
            var createdStudents = new List<Student>();

            for (int i = 0; i < 20; i++)
            {
                string fName = firstNames[random.Next(firstNames.Length)];
                string lName = lastNames[random.Next(lastNames.Length)];
                string email = $"{fName.ToLower()}.{lName.ToLower()}{i}@test.com";

                var student = await CreateStudentUser(
                    userManager,
                    context,
                    email,
                    fName,
                    lName,
                    instruments[random.Next(instruments.Length)],
                    states[random.Next(states.Length)],
                    2024 + random.Next(3),
                    password
                );

                if (student != null) createdStudents.Add(student);
            }

            // Seed Random Interests
            var allBandIds = await context.Bands.Select(b => b.Id).ToListAsync();
            foreach (var student in createdStudents)
            {
                int numberOfInterests = random.Next(2, 6);
                var pickedBandIds = allBandIds.OrderBy(x => random.Next()).Take(numberOfInterests).ToList();

                foreach (var bandId in pickedBandIds)
                {
                    if (!context.StudentInterests.Any(si => si.StudentId == student.Id && si.BandId == bandId))
                    {
                        context.StudentInterests.Add(new StudentInterest
                        {
                            StudentId = student.Id,
                            BandId = bandId,
                            IsInterested = true,
                            InterestedDate = DateTime.UtcNow.AddDays(-random.Next(1, 90)),
                            Notes = "Interested in scholarship opportunities."
                        });
                    }
                }
            }
            await context.SaveChangesAsync();
        }

        private static List<Band> GetBandsList()
        {
            // Paste the full list of ~60 bands from the previous step here
            // I will include a shortened version for brevity, but you should use the full list
            return new List<Band>
            {
               // --- Alabama ---
                    new Band { UniversityName = "Alabama A&M University", BandName = "The Marching Maroon & White", City = "Huntsville", State = "AL", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Alabama State University", BandName = "The Mighty Marching Hornets", City = "Montgomery", State = "AL", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Miles College", BandName = "Purple Marching Machine", City = "Fairfield", State = "AL", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Stillman College", BandName = "Blue Pride Marching Band", City = "Tuscaloosa", State = "AL", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Talladega College", BandName = "The Great Tornado Band", City = "Talladega", State = "AL", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Tuskegee University", BandName = "The Marching Crimson Pipers", City = "Tuskegee", State = "AL", IsActive = true, ScholarshipBudget = 500000 },

                    // --- Arkansas ---
                    new Band { UniversityName = "University of Arkansas at Pine Bluff", BandName = "Marching Musical Machine of the Mid-South", City = "Pine Bluff", State = "AR", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Philander Smith University", BandName = "The Panther Marching Band", City = "Little Rock", State = "AR", IsActive = true, ScholarshipBudget = 500000 },

                    // --- Delaware ---
                    new Band { UniversityName = "Delaware State University", BandName = "The Approaching Storm", City = "Dover", State = "DE", IsActive = true, ScholarshipBudget = 500000 },

                    // --- District of Columbia ---
                    new Band { UniversityName = "Howard University", BandName = "Showtime Marching Band", City = "Washington", State = "DC", IsActive = true, ScholarshipBudget = 500000 },

                    // --- Florida ---
                    new Band { UniversityName = "Bethune-Cookman University", BandName = "The Marching Wildcats", City = "Daytona Beach", State = "FL", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Edward Waters University", BandName = "The Triple Threat Marching Band", City = "Jacksonville", State = "FL", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Florida A&M University", BandName = "The Marching 100", City = "Tallahassee", State = "FL", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Florida Memorial University", BandName = "The Roar", City = "Miami Gardens", State = "FL", IsActive = true, ScholarshipBudget = 500000 },

                    // --- Georgia ---
                    new Band { UniversityName = "Albany State University", BandName = "The Marching Rams Show Band", City = "Albany", State = "GA", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Clark Atlanta University", BandName = "The Mighty Marching Panthers", City = "Atlanta", State = "GA", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Fort Valley State University", BandName = "The Blue Machine", City = "Fort Valley", State = "GA", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Morehouse College", BandName = "House of Funk", City = "Atlanta", State = "GA", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Savannah State University", BandName = "The Powerhouse of the South", City = "Savannah", State = "GA", IsActive = true, ScholarshipBudget = 500000 },

                    // --- Kentucky ---
                    new Band { UniversityName = "Kentucky State University", BandName = "The Mighty Marching Thorobreds", City = "Frankfort", State = "KY", IsActive = true, ScholarshipBudget = 500000 },

                    // --- Louisiana ---
                    new Band { UniversityName = "Grambling State University", BandName = "The Tiger Marching Band", City = "Grambling", State = "LA", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Southern University", BandName = "The Human Jukebox", City = "Baton Rouge", State = "LA", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Xavier University of Louisiana", BandName = "The Golden Sound", City = "New Orleans", State = "LA", IsActive = true, ScholarshipBudget = 500000 },

                    // --- Maryland ---
                    new Band { UniversityName = "Bowie State University", BandName = "Symphony of Soul", City = "Bowie", State = "MD", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Morgan State University", BandName = "The Magnificent Marching Machine", City = "Baltimore", State = "MD", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "University of Maryland Eastern Shore", BandName = "Thunderin' Hawks Pep Band", City = "Princess Anne", State = "MD", IsActive = true, ScholarshipBudget = 500000 },

                    // --- Mississippi ---
                    new Band { UniversityName = "Alcorn State University", BandName = "The Sounds of Dyn-O-Mite", City = "Lorman", State = "MS", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Coahoma Community College", BandName = "The Marching Maroon Typhoon", City = "Clarksdale", State = "MS", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Jackson State University", BandName = "The Sonic Boom of the South", City = "Jackson", State = "MS", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Mississippi Valley State University", BandName = "The Mean Green Marching Machine", City = "Itta Bena", State = "MS", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Rust College", BandName = "The Marching Bearcats", City = "Holly Springs", State = "MS", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Tougaloo College", BandName = "Tougaloo Marching Band", City = "Tougaloo", State = "MS", IsActive = true, ScholarshipBudget = 500000 },

                    // --- Missouri ---
                    new Band { UniversityName = "Lincoln University of Missouri", BandName = "The Marching Musical Storm", City = "Jefferson City", State = "MO", IsActive = true, ScholarshipBudget = 500000 },

                    // --- North Carolina ---
                    new Band { UniversityName = "Elizabeth City State University", BandName = "Marching Sound of Class", City = "Elizabeth City", State = "NC", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Fayetteville State University", BandName = "Marching Bronco Express", City = "Fayetteville", State = "NC", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Johnson C. Smith University", BandName = "International Institution of Sound", City = "Charlotte", State = "NC", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Livingstone College", BandName = "Marching Blue Thunder", City = "Salisbury", State = "NC", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "North Carolina A&T State University", BandName = "The Blue and Gold Marching Machine", City = "Greensboro", State = "NC", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "North Carolina Central University", BandName = "The Sound Machine", City = "Durham", State = "NC", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Saint Augustine's University", BandName = "Superior Sound Marching Band", City = "Raleigh", State = "NC", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Shaw University", BandName = "Platinum Sound", City = "Raleigh", State = "NC", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Winston-Salem State University", BandName = "The Red Sea of Sound", City = "Winston-Salem", State = "NC", IsActive = true, ScholarshipBudget = 500000 },

                    // --- Ohio ---
                    new Band { UniversityName = "Central State University", BandName = "Invincible Marching Marauders", City = "Wilberforce", State = "OH", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Wilberforce University", BandName = "The Hounds of Sound", City = "Wilberforce", State = "OH", IsActive = true, ScholarshipBudget = 500000 },

                    // --- Oklahoma ---
                    new Band { UniversityName = "Langston University", BandName = "The Marching Pride", City = "Langston", State = "OK", IsActive = true, ScholarshipBudget = 500000 },

                    // --- Pennsylvania ---
                    new Band { UniversityName = "Lincoln University", BandName = "Orange Crush Roaring Lion Marching Band", City = "Lincoln University", State = "PA", IsActive = true, ScholarshipBudget = 500000 },

                    // --- South Carolina ---
                    new Band { UniversityName = "Allen University", BandName = "The Band of Gold", City = "Columbia", State = "SC", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Benedict College", BandName = "The Band of Distinction", City = "Columbia", State = "SC", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Claflin University", BandName = "The Claflin Pep Band", City = "Orangeburg", State = "SC", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "South Carolina State University", BandName = "The Marching 101", City = "Orangeburg", State = "SC", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Voorhees University", BandName = "The Walking Tigers", City = "Denmark", State = "SC", IsActive = true, ScholarshipBudget = 500000 },

                    // --- Tennessee ---
                    new Band { UniversityName = "Lane College", BandName = "The Quiet Storm Marching Band", City = "Jackson", State = "TN", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Tennessee State University", BandName = "Aristocrat of Bands", City = "Nashville", State = "TN", IsActive = true, ScholarshipBudget = 500000 },

                    // --- Texas ---
                    new Band { UniversityName = "Huston-Tillotson University", BandName = "HT Rams Pep Band", City = "Austin", State = "TX", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Jarvis Christian University", BandName = "Sophisticated Sounds of Soul", City = "Hawkins", State = "TX", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Prairie View A&M University", BandName = "The Marching Storm", City = "Prairie View", State = "TX", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Texas College", BandName = "Texas College Marching Band", City = "Tyler", State = "TX", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Texas Southern University", BandName = "Ocean of Soul", City = "Houston", State = "TX", IsActive = true, ScholarshipBudget = 500000 },

                    // --- Virginia ---
                    new Band { UniversityName = "Hampton University", BandName = "The Marching Force", City = "Hampton", State = "VA", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Norfolk State University", BandName = "The Spartan Legion", City = "Norfolk", State = "VA", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Virginia State University", BandName = "The Trojan Explosion", City = "Petersburg", State = "VA", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "Virginia Union University", BandName = "The Ambassadors of Sound", City = "Richmond", State = "VA", IsActive = true, ScholarshipBudget = 500000 },

                    // --- West Virginia ---
                    new Band { UniversityName = "Bluefield State University", BandName = "Blue Soul Marching Band", City = "Bluefield", State = "WV", IsActive = true, ScholarshipBudget = 500000 },
                    new Band { UniversityName = "West Virginia State University", BandName = "Marching Yellow Jackets", City = "Institute", State = "WV", IsActive = true, ScholarshipBudget = 500000 },
            };
        }
    }
}