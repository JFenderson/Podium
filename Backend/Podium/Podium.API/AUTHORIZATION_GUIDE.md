# Band Recruitment Platform - Authorization Implementation Guide

## Overview
This authorization system implements role-based access control (RBAC) with fine-grained permissions for the Band Recruitment Platform.

## Security Architecture

### 1. Roles
- **Student**: Can view and edit their own profile, view their offers
- **Guardian**: Can view linked students' profiles and activities
- **Recruiter**: BandStaff with recruiting capabilities (permissions defined below)
- **Director**: BandStaff with administrative capabilities (permissions defined below)

### 2. BandStaff Permissions
Fine-grained permissions stored in the BandStaff entity:
- **CanViewStudents**: View all student profiles
- **CanRateStudents**: Submit ratings/evaluations for students
- **CanSendOffers**: Create recruitment offers
- **CanManageEvents**: Create and manage recruitment events
- **CanManageStaff**: Manage other staff members (typically Directors only)

## Implementation Components

### Authorization Requirements (`Authorization/Requirements.cs`)
Custom authorization requirements that define what needs to be checked:
- `RoleRequirement`: Basic role checking
- `BandStaffPermissionRequirement`: Permission flag checking
- `SelfAccessRequirement`: User accessing their own resources
- `GuardianStudentAccessRequirement`: Guardian accessing linked students
- `ScholarshipApprovalRequirement`: Directors approving scholarships
- `ResourceAccessRequirement`: Resource-based authorization with operations

### Authorization Handlers (`Authorization/Handlers.cs`)
Implementations that verify requirements:
- `RoleAuthorizationHandler`: Checks user's role claim
- `BandStaffPermissionHandler`: Queries database for permission flags
- `SelfAccessHandler`: Verifies user owns the resource
- `GuardianStudentAccessHandler`: Checks guardian-student relationship
- `ScholarshipApprovalHandler`: Verifies Director role
- `StudentResourceAuthorizationHandler`: Complex resource-based checks

### Authorization Service (`Authorization/AuthorizationService.cs`)
Business logic service for permission checking:
```csharp
// Usage in services
var canApprove = await _authService.CanApproveScholarshipsAsync();
var isOwner = await _authService.IsStudentOwnerAsync(studentId);
var permissions = await _authService.GetBandStaffPermissionsAsync();
```

### Extension Methods (`Authorization/Extensions.cs`)
Helper methods for controllers:
```csharp
// Usage in controllers
var userId = this.GetCurrentUserId();
var isDirector = this.IsDirector();
var isBandStaff = this.IsBandStaff();
```

## Usage Examples

### 1. Simple Role-Based Authorization
```csharp
[HttpGet]
[Authorize(Policy = "StudentOnly")]
public async Task<ActionResult> GetMyProfile()
{
    // Only students can access this endpoint
}
```

### 2. Permission-Based Authorization
```csharp
[HttpGet]
[Authorize(Policy = "CanViewStudents")]
public async Task<ActionResult> GetAllStudents()
{
    // Only BandStaff with CanViewStudents permission
}
```

### 3. Complex Combined Policies
```csharp
[HttpPost]
[Authorize(Policy = "CanCreateOffer")]
public async Task<ActionResult> CreateOffer()
{
    // Must be Recruiter or Director AND have CanSendOffers permission
}
```

### 4. Resource-Based Authorization
```csharp
[HttpPut("{id}")]
public async Task<IActionResult> UpdateStudent(int id)
{
    var authResult = await _policyAuthService.AuthorizeAsync(
        User,
        id,
        new ResourceAccessRequirement(Operations.Update));

    if (!authResult.Succeeded)
    {
        return Forbid();
    }
    
    // Proceed with update
}
```

### 5. Custom Authorization in Business Logic
```csharp
public async Task<ServiceResult> ProcessOfferAsync(int offerId)
{
    // Check if user can send offers
    if (!await _authService.CanSendOffersAsync())
    {
        return ServiceResult.Forbidden("You don't have permission to send offers");
    }
    
    // Process the offer
}
```

### 6. Multiple Authorization Checks
```csharp
public async Task<ActionResult> ComplexOperation(int studentId)
{
    var role = await _authService.GetCurrentUserRoleAsync();
    var userId = await _authService.GetCurrentUserIdAsync();
    
    if (role == Roles.Student)
    {
        if (!await _authService.IsStudentOwnerAsync(studentId))
        {
            return Forbid();
        }
    }
    else if (role == Roles.Guardian)
    {
        if (!await _authService.IsGuardianOfStudentAsync(studentId))
        {
            return Forbid();
        }
    }
    else if (role == Roles.Recruiter || role == Roles.Director)
    {
        if (!await _authService.HasPermissionAsync(Permissions.ViewStudents))
        {
            return Forbid();
        }
    }
    else
    {
        return Forbid();
    }
    
    // Proceed with operation
}
```

## Security Best Practices

### 1. Prevent Privilege Escalation
```csharp
// Don't allow users to modify their own critical permissions
if (staff.UserId == currentUserId && !dto.CanManageStaff)
{
    return BadRequest("You cannot remove your own ManageStaff permission");
}

// Don't allow users to delete themselves
if (staff.UserId == currentUserId)
{
    return BadRequest("You cannot remove yourself");
}
```

### 2. Always Verify Resource Ownership
```csharp
// Bad - Only checks role
[Authorize(Roles = "Student")]
public async Task<IActionResult> UpdateProfile(int id)
{
    // ANY student could update ANY profile!
}

// Good - Verifies ownership
public async Task<IActionResult> UpdateProfile(int id)
{
    if (!await _authService.IsStudentOwnerAsync(id))
    {
        return Forbid();
    }
    // Now only the owner can update
}
```

### 3. Fail Securely
```csharp
// Always default to denying access
var canAccess = false;

try
{
    canAccess = await CheckAccessAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error checking access");
    // canAccess remains false
}

if (!canAccess)
{
    return Forbid();
}
```

### 4. Use Policy-Based Authorization Over Role Checks
```csharp
// Bad - Hardcoded role checks scattered everywhere
if (User.IsInRole("Director"))
{
    // Do something
}

// Good - Policy-based
[Authorize(Policy = "DirectorOnly")]
public async Task<IActionResult> AdminFunction()
{
    // Policy is enforced at framework level
}
```

### 5. Validate at Multiple Layers
```csharp
// Controller layer - Initial check
[Authorize(Policy = "CanSendOffers")]
public async Task<IActionResult> CreateOffer(CreateOfferDto dto)
{
    // Service layer - Business logic validation
    var result = await _offerService.CreateOfferAsync(dto);
    
    if (result.ResultType == ServiceResultType.Forbidden)
    {
        return Forbid();
    }
    
    return Ok(result.Data);
}

// Service layer
public async Task<ServiceResult> CreateOfferAsync(CreateOfferDto dto)
{
    // Re-verify permission in business logic
    if (!await _authService.CanSendOffersAsync())
    {
        return ServiceResult.Forbidden();
    }
    
    // Additional business rules
    if (dto.ScholarshipAmount > 10000 && !await _authService.CanApproveScholarshipsAsync())
    {
        return ServiceResult.Forbidden("Large scholarships require Director approval");
    }
    
    // Create the offer
}
```

## Testing Authorization

### Unit Tests
```csharp
[Fact]
public async Task UpdateStudent_AsOwner_Succeeds()
{
    // Arrange
    var student = CreateTestStudent();
    var controller = CreateControllerWithUser(student.UserId, Roles.Student);
    
    // Act
    var result = await controller.UpdateStudent(student.StudentId, new UpdateStudentDto());
    
    // Assert
    Assert.IsType<NoContentResult>(result);
}

[Fact]
public async Task UpdateStudent_AsNonOwner_ReturnsForbidden()
{
    // Arrange
    var student = CreateTestStudent();
    var controller = CreateControllerWithUser(999, Roles.Student); // Different user
    
    // Act
    var result = await controller.UpdateStudent(student.StudentId, new UpdateStudentDto());
    
    // Assert
    Assert.IsType<ForbidResult>(result);
}
```

### Integration Tests
```csharp
[Fact]
public async Task CreateOffer_WithoutPermission_Returns403()
{
    // Arrange
    var token = await GetTokenForRecruiterWithoutSendOffersPermission();
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    
    // Act
    var response = await client.PostAsJsonAsync("/api/offers", new CreateOfferDto());
    
    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

## Common Pitfalls to Avoid

1. **Don't check permissions only in the UI** - Always validate on the backend
2. **Don't trust client-provided user IDs** - Always use authenticated user's ID from token
3. **Don't expose internal user IDs in URLs** - Use GUIDs or encrypted identifiers
4. **Don't return detailed error messages** - Avoid leaking information about permissions
5. **Don't forget to check permissions in background jobs** - Same rules apply
6. **Don't cache authorization decisions indefinitely** - Permissions can change
7. **Don't use [AllowAnonymous] without careful consideration** - It bypasses all auth

## Monitoring and Auditing

```csharp
// Log authorization failures
public class AuditingAuthorizationHandler : IAuthorizationHandler
{
    private readonly ILogger<AuditingAuthorizationHandler> _logger;
    
    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        if (!context.HasSucceeded)
        {
            _logger.LogWarning(
                "Authorization failed for user {UserId} accessing {Resource}",
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                context.Resource);
        }
    }
}
```

## Configuration Checklist

- [ ] JWT authentication configured in Program.cs
- [ ] All authorization handlers registered
- [ ] All policies defined
- [ ] HttpContextAccessor registered
- [ ] Authorization service registered
- [ ] Authentication middleware before Authorization middleware
- [ ] All sensitive endpoints have [Authorize] attribute
- [ ] Resource-based authorization implemented for entity operations
- [ ] Permission checks in business logic layer
- [ ] Unit tests for authorization logic
- [ ] Integration tests for protected endpoints
- [ ] Audit logging for authorization failures
- [ ] Regular security reviews scheduled