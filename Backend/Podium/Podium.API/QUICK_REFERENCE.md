# Authorization Quick Reference Guide

## Quick Policy Reference

### Role-Based Policies
```csharp
[Authorize(Policy = "StudentOnly")]        // Only students
[Authorize(Policy = "GuardianOnly")]       // Only guardians
[Authorize(Policy = "RecruiterOnly")]      // Only recruiters
[Authorize(Policy = "DirectorOnly")]       // Only directors
[Authorize(Policy = "BandStaffOnly")]      // Any BandStaff (Recruiter or Director)
```

### Permission-Based Policies
```csharp
[Authorize(Policy = "CanViewStudents")]    // BandStaff with CanViewStudents = true
[Authorize(Policy = "CanRateStudents")]    // BandStaff with CanRateStudents = true
[Authorize(Policy = "CanSendOffers")]      // BandStaff with CanSendOffers = true
[Authorize(Policy = "CanManageEvents")]    // BandStaff with CanManageEvents = true
[Authorize(Policy = "CanManageStaff")]     // BandStaff with CanManageStaff = true
```

### Complex Policies
```csharp
[Authorize(Policy = "CanCreateOffer")]         // Recruiter/Director + CanSendOffers
[Authorize(Policy = "CanApproveScholarships")] // Directors only
[Authorize(Policy = "AdminAccess")]            // Director + CanManageStaff
[Authorize(Policy = "GuardianStudentAccess")]  // Guardian accessing linked student
```

## Common Authorization Patterns

### 1. Get Current User Info
```csharp
// In Controllers
var userId = this.GetCurrentUserId();
var role = this.GetCurrentUserRole();
var isDirector = this.IsDirector();

// In Services
var userId = await _authService.GetCurrentUserIdAsync();
var role = await _authService.GetCurrentUserRoleAsync();
```

### 2. Check Permissions
```csharp
// Check specific permission
if (await _authService.HasPermissionAsync(Permissions.ViewStudents))
{
    // User has ViewStudents permission
}

// Check if can send offers
if (await _authService.CanSendOffersAsync())
{
    // User can send offers
}

// Get all permissions
var permissions = await _authService.GetBandStaffPermissionsAsync();
```

### 3. Resource-Based Authorization
```csharp
// Check if user can perform operation on resource
var authResult = await _policyAuthService.AuthorizeAsync(
    User,
    resourceId,
    new ResourceAccessRequirement(Operations.Update));

if (!authResult.Succeeded)
{
    return Forbid();
}
```

### 4. Check Resource Ownership
```csharp
// Check if student owns profile
if (await _authService.IsStudentOwnerAsync(studentId))
{
    // Student owns this profile
}

// Check if guardian is linked to student
if (await _authService.IsGuardianOfStudentAsync(studentId))
{
    // Guardian can access this student
}
```

## Controller Templates

### Student Profile Endpoint
```csharp
[HttpGet("{id}")]
public async Task<ActionResult<StudentDto>> GetStudent(int id)
{
    var authResult = await _policyAuthService.AuthorizeAsync(
        User, 
        id, 
        new ResourceAccessRequirement(Operations.Read));

    if (!authResult.Succeeded)
    {
        return Forbid();
    }

    var student = await _context.Students.FindAsync(id);
    if (student == null)
    {
        return NotFound();
    }

    return Ok(MapToDto(student));
}
```

### Create Offer Endpoint
```csharp
[HttpPost]
[Authorize(Policy = "CanCreateOffer")]
public async Task<ActionResult<OfferDto>> CreateOffer(CreateOfferDto dto)
{
    var userId = await _authService.GetCurrentUserIdAsync();
    
    var offer = new Offer
    {
        StudentId = dto.StudentId,
        CreatedByUserId = userId.Value,
        // ... other properties
    };

    _context.Offers.Add(offer);
    await _context.SaveChangesAsync();

    return CreatedAtAction(nameof(GetOffer), new { id = offer.OfferId }, MapToDto(offer));
}
```

### Approve Scholarship Endpoint
```csharp
[HttpPost("{id}/approve")]
[Authorize(Policy = "CanApproveScholarships")]
public async Task<IActionResult> ApproveScholarship(int id)
{
    var offer = await _context.Offers.FindAsync(id);
    if (offer == null)
    {
        return NotFound();
    }

    if (offer.OfferType != "Scholarship")
    {
        return BadRequest("Only scholarship offers can be approved");
    }

    var userId = await _authService.GetCurrentUserIdAsync();
    offer.Status = "Approved";
    offer.ApprovedByUserId = userId.Value;
    offer.ApprovedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    return Ok(new { Message = "Scholarship approved successfully" });
}
```

### Update Own Profile
```csharp
[HttpPut("{id}")]
public async Task<IActionResult> UpdateProfile(int id, UpdateProfileDto dto)
{
    // Verify user owns this profile
    if (!await _authService.IsStudentOwnerAsync(id))
    {
        return Forbid();
    }

    var student = await _context.Students.FindAsync(id);
    if (student == null)
    {
        return NotFound();
    }

    // Update properties
    student.FirstName = dto.FirstName;
    student.LastName = dto.LastName;
    // ... other updates

    await _context.SaveChangesAsync();

    return NoContent();
}
```

## Service Layer Pattern

```csharp
public class MyService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthorizationService _authService;

    public MyService(ApplicationDbContext context, IAuthorizationService authService)
    {
        _context = context;
        _authService = authService;
    }

    public async Task<ServiceResult<T>> DoSomethingAsync(int resourceId)
    {
        // Check authorization
        var role = await _authService.GetCurrentUserRoleAsync();
        
        if (role == Roles.Student)
        {
            if (!await _authService.IsStudentOwnerAsync(resourceId))
            {
                return ServiceResult<T>.Forbidden("You can only access your own resources");
            }
        }
        else if (role == Roles.Recruiter || role == Roles.Director)
        {
            if (!await _authService.HasPermissionAsync(Permissions.ViewStudents))
            {
                return ServiceResult<T>.Forbidden("You don't have permission");
            }
        }
        else
        {
            return ServiceResult<T>.Forbidden("Invalid role");
        }

        // Perform operation
        // ...

        return ServiceResult<T>.Success(result);
    }
}
```

## Testing Templates

### Unit Test
```csharp
[Fact]
public async Task CreateOffer_WithPermission_Succeeds()
{
    // Arrange
    var mockAuthService = new Mock<IAuthorizationService>();
    mockAuthService
        .Setup(x => x.CanSendOffersAsync())
        .ReturnsAsync(true);
    mockAuthService
        .Setup(x => x.GetCurrentUserIdAsync())
        .ReturnsAsync(1);

    var controller = new OffersController(_context, mockAuthService.Object);

    // Act
    var result = await controller.CreateOffer(new CreateOfferDto());

    // Assert
    Assert.IsType<CreatedAtActionResult>(result.Result);
}
```

### Integration Test
```csharp
[Fact]
public async Task GetStudent_AsOwner_Returns200()
{
    // Arrange
    var token = await GetTokenForStudent(studentId: 1);
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await client.GetAsync("/api/students/1");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

## Common Security Mistakes to Avoid

❌ **Wrong**: Only checking role
```csharp
[Authorize(Roles = "Student")]
public async Task<IActionResult> UpdateProfile(int id)
{
    // ANY student can update ANY profile!
}
```

✅ **Correct**: Check role AND ownership
```csharp
public async Task<IActionResult> UpdateProfile(int id)
{
    if (!await _authService.IsStudentOwnerAsync(id))
    {
        return Forbid();
    }
    // Only owner can update
}
```

❌ **Wrong**: Trusting client-provided user ID
```csharp
public async Task<IActionResult> GetProfile(int userId)
{
    var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
    return Ok(student);
}
```

✅ **Correct**: Using authenticated user ID
```csharp
public async Task<IActionResult> GetProfile()
{
    var userId = await _authService.GetCurrentUserIdAsync();
    var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
    return Ok(student);
}
```

❌ **Wrong**: Only checking in controller
```csharp
[Authorize(Policy = "CanSendOffers")]
public async Task<IActionResult> CreateOffer(CreateOfferDto dto)
{
    await _offerService.CreateOfferAsync(dto); // Service doesn't check permissions
}
```

✅ **Correct**: Checking in both controller and service
```csharp
[Authorize(Policy = "CanSendOffers")]
public async Task<IActionResult> CreateOffer(CreateOfferDto dto)
{
    var result = await _offerService.CreateOfferAsync(dto); // Service also checks
    
    if (result.ResultType == ServiceResultType.Forbidden)
    {
        return Forbid();
    }
    
    return Ok(result.Data);
}
```

## Dependency Injection Registration

Add to Program.cs:
```csharp
// Authorization Handlers
builder.Services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, BandStaffPermissionHandler>();
builder.Services.AddScoped<IAuthorizationHandler, SelfAccessHandler>();
builder.Services.AddScoped<IAuthorizationHandler, GuardianStudentAccessHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ScholarshipApprovalHandler>();
builder.Services.AddScoped<IAuthorizationHandler, StudentResourceAuthorizationHandler>();

// Custom Authorization Service
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();

// HttpContextAccessor
builder.Services.AddHttpContextAccessor();
```

## Useful Constants

```csharp
// Roles
Roles.Student
Roles.Guardian
Roles.Recruiter
Roles.Director
Roles.BandStaff

// Permissions
Permissions.ViewStudents
Permissions.RateStudents
Permissions.SendOffers
Permissions.ManageEvents
Permissions.ManageStaff

// Operations
Operations.Create
Operations.Read
Operations.Update
Operations.Delete
Operations.Approve
```