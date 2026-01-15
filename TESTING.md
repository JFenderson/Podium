# Testing Guide for Podium

## Overview
This document describes the comprehensive testing strategy for the Podium application, including backend tests, frontend tests, end-to-end tests, and load testing.

## Test Structure

### Backend Tests
- **Location**: `Backend/Podium/Podium.Tests/`
- **Framework**: xUnit, FluentAssertions, Moq
- **Types**: Integration tests with WebApplicationFactory

### Frontend Tests
- **Location**: `Frontend/podium-frontend/src/**/*.spec.ts`
- **Framework**: Vitest with Angular Testing Library
- **Types**: Unit tests for components and services

### E2E Tests
- **Location**: `Frontend/podium-frontend/tests/e2e/`
- **Framework**: Playwright
- **Types**: End-to-end user flow tests

### Load Tests
- **Location**: `load-tests/scripts/`
- **Framework**: K6
- **Types**: Performance and load testing

---

## Running Tests Locally

### Backend Tests

Navigate to the backend solution:
```bash
cd Backend/Podium
```

Run all tests:
```bash
dotnet test
```

Run tests with coverage:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

Generate HTML coverage report:
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

Run specific test file:
```bash
dotnet test --filter "FullyQualifiedName~HealthCheckTests"
```

### Frontend Unit Tests

Navigate to frontend:
```bash
cd Frontend/podium-frontend
```

Install dependencies (if not already done):
```bash
npm install
```

Run tests in watch mode:
```bash
npm test
```

Run tests once:
```bash
npm run test:run
```

Run tests with coverage:
```bash
npm run test:coverage
```

Run tests with UI:
```bash
npm run test:ui
```

### E2E Tests with Playwright

Prerequisites - Install Playwright browsers (first time only):
```bash
cd Frontend/podium-frontend
npx playwright install
```

Run all E2E tests:
```bash
npm run test:e2e
```

Run with UI mode (interactive):
```bash
npm run test:e2e:ui
```

Run in headed mode (see browser):
```bash
npm run test:e2e:headed
```

Run specific test file:
```bash
npx playwright test tests/e2e/login.spec.ts
```

Run specific browser:
```bash
npx playwright test --project=chromium
```

### Load Tests

Prerequisites - Install K6:

**macOS:**
```bash
brew install k6
```

**Linux:**
```bash
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6
```

Run load tests:
```bash
cd load-tests
k6 run scripts/auth-load.js
k6 run scripts/student-search-load.js
```

---

## Writing New Tests

### Backend Unit Test Example

```csharp
using Xunit;
using FluentAssertions;
using Moq;

public class StudentServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly StudentService _service;

    public StudentServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _service = new StudentService(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task GetStudentById_ReturnsStudent_WhenExists()
    {
        // Arrange
        var studentId = 1;
        var expectedStudent = new Student { Id = studentId, FirstName = "John" };
        _mockUnitOfWork.Setup(u => u.Students.GetByIdAsync(studentId))
            .ReturnsAsync(expectedStudent);

        // Act
        var result = await _service.GetStudentById(studentId);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("John");
    }
}
```

### Frontend Component Test Example

```typescript
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { LoginComponent } from './login.component';
import { AuthService } from '@core/services/auth.service';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let authService: AuthService;
  let router: Router;

  beforeEach(() => {
    const authServiceMock = {
      login: vi.fn(),
    };

    const routerMock = {
      navigate: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        LoginComponent,
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock },
      ],
    });

    component = TestBed.inject(LoginComponent);
    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  it('should call authService.login on form submit', async () => {
    const loginSpy = vi.spyOn(authService, 'login').mockReturnValue(of({ accessToken: 'token' }));
    
    component.email = 'test@test.com';
    component.password = 'password';
    
    await component.onSubmit();
    
    expect(loginSpy).toHaveBeenCalledWith('test@test.com', 'password');
  });

  it('should navigate to dashboard on successful login', async () => {
    vi.spyOn(authService, 'login').mockReturnValue(of({ accessToken: 'token' }));
    const navigateSpy = vi.spyOn(router, 'navigate');
    
    component.email = 'test@test.com';
    component.password = 'password';
    
    await component.onSubmit();
    
    expect(navigateSpy).toHaveBeenCalledWith(['/dashboard']);
  });
});
```

### E2E Test Example

```typescript
import { test, expect } from '@playwright/test';

test.describe('Registration Flow', () => {
  test('Student can register successfully', async ({ page }) => {
    // Navigate to register page
    await page.goto('/register');

    // Select Student role
    await page.click('text=Student');

    // Fill in form
    await page.fill('input[name="email"]', 'newstudent@test.com');
    await page.fill('input[name="password"]', 'TestPassword123!');
    await page.fill('input[name="confirmPassword"]', 'TestPassword123!');
    await page.fill('input[name="firstName"]', 'Test');
    await page.fill('input[name="lastName"]', 'Student');
    
    // Student-specific fields
    await page.fill('input[name="school"]', 'Test University');
    await page.fill('input[name="instrument"]', 'Trumpet');
    await page.fill('input[name="graduationYear"]', '2025');

    // Submit form
    await page.click('button[type="submit"]');

    // Verify success
    await expect(page).toHaveURL(/login|dashboard/);
  });
});
```

---

## Test Data Management

### Test Data Builders

Use builder pattern for creating test entities:

```csharp
public class StudentBuilder
{
    private Student _student = new Student 
    { 
        FirstName = "Test", 
        LastName = "User",
        Email = "test@test.com"
    };

    public StudentBuilder WithId(int id)
    {
        _student.Id = id;
        return this;
    }

    public StudentBuilder WithInstrument(string instrument)
    {
        _student.Instrument = instrument;
        return this;
    }

    public StudentBuilder WithSchool(string school)
    {
        _student.School = school;
        return this;
    }

    public Student Build() => _student;
}

// Usage:
var student = new StudentBuilder()
    .WithId(1)
    .WithInstrument("Trumpet")
    .WithSchool("Berkeley")
    .Build();
```

### Database Seeding for Tests

Integration tests should use seeded test data:

```csharp
public class TestDataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Clear existing data
        context.Students.RemoveRange(context.Students);
        await context.SaveChangesAsync();

        // Seed test data
        var students = new List<Student>
        {
            new Student 
            { 
                FirstName = "John",
                LastName = "Doe",
                Email = "student1@test.com",
                Instrument = "Trumpet"
            },
            new Student 
            { 
                FirstName = "Jane",
                LastName = "Smith",
                Email = "student2@test.com",
                Instrument = "Clarinet"
            }
        };
        
        context.Students.AddRange(students);
        await context.SaveChangesAsync();
    }
}
```

---

## CI/CD Integration

Tests run automatically in GitHub Actions on:
- Every pull request
- Every push to main/develop
- Daily schedule (for E2E tests)

To trigger E2E tests on a specific commit:
```bash
git commit -m "Your message [e2e]"
```

### GitHub Actions Workflows

**Backend Tests**: `.github/workflows/backend-ci.yml`
- Runs unit and integration tests
- Generates code coverage reports
- Uploads to Codecov

**Frontend Tests**: `.github/workflows/frontend-ci.yml`
- Runs linting
- Runs unit tests with coverage
- Builds production bundle

---

## Coverage Requirements

### Backend
- **Target**: Minimum 80% line coverage for business logic
- **Services**: Focus on service layer coverage
- **Controllers**: Integration tests cover controller logic

### Frontend
- **Target**: Minimum 70% coverage for components and services
- **Components**: Test component logic, not Angular internals
- **Services**: Test all HTTP calls and data transformations

### Viewing Coverage Reports

**Backend**:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
# Open coverage-report/index.html in browser
```

**Frontend**:
```bash
npm run test:coverage
# Open coverage/index.html in browser
```

---

## Troubleshooting

### Backend Tests

**Issue**: Tests fail with database connection error

**Solution**:
- Ensure tests are using InMemoryDatabase
- Check `PodiumWebApplicationFactory` configuration
- Verify connection string in test configuration

**Issue**: WebApplicationFactory not starting

**Solution**:
- Ensure `Program` class is public (check `public partial class Program { }` at end of Program.cs)
- Verify environment is set to "Testing"
- Check for unhandled exceptions in Program.cs startup code

### Frontend Tests

**Issue**: Tests timeout or hang

**Solution**:
- Increase timeout in vitest.config.ts
- Check for unresolved promises in async tests
- Verify test-setup.ts is configured correctly

**Issue**: Module not found errors

**Solution**:
- Run `npm install` to ensure all dependencies are installed
- Check tsconfig.spec.json includes test files
- Verify path aliases in vitest.config.ts

### E2E Tests

**Issue**: Tests fail with timeout

**Solution**:
- Increase timeout in playwright.config.ts
- Ensure backend is fully started before running tests
- Check browser installation: `npx playwright install`

**Issue**: Selector not found

**Solution**:
- Use Playwright Inspector: `npx playwright test --debug`
- Check for dynamic content loading
- Add appropriate wait conditions

### Load Tests

**Issue**: High error rate

**Solution**:
- Check backend logs for errors
- Verify test data exists (users, students, etc.)
- Reduce VU count if system is overwhelmed
- Check network connectivity to backend

**Issue**: Slow response times

**Solution**:
- Profile backend endpoints
- Check database query performance
- Verify adequate system resources (CPU, memory)

---

## Best Practices

### General
- Write tests before fixing bugs (TDD for bug fixes)
- Keep tests focused and independent
- Use descriptive test names
- Don't test framework internals
- Mock external dependencies

### Backend Tests
- Use InMemoryDatabase for integration tests
- Separate unit tests from integration tests
- Test both success and failure paths
- Verify authorization logic
- Test validation rules

### Frontend Tests
- Test user interactions, not implementation details
- Mock HTTP calls
- Test error handling
- Verify navigation
- Test form validation

### E2E Tests
- Test critical user journeys only
- Use Page Object Model for complex tests
- Keep tests independent
- Use test data that's always available
- Clean up after tests

### Load Tests
- Start with realistic load
- Monitor system resources during tests
- Test both average and peak load
- Include think time in scripts
- Document performance baselines

---

## Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Vitest Documentation](https://vitest.dev/)
- [Playwright Documentation](https://playwright.dev/)
- [K6 Documentation](https://k6.io/docs/)
- [Angular Testing Guide](https://angular.dev/guide/testing)

---

## Getting Help

If you encounter issues:
1. Check this troubleshooting guide
2. Review test logs for specific errors
3. Consult framework documentation
4. Ask in team chat or create an issue
