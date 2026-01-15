import { test, expect } from '@playwright/test';

/**
 * E2E tests for Student Registration Flow
 * Tests the complete user journey from registration to dashboard access
 */
test.describe('Student Registration Flow', () => {
  // Generate unique email for each test run
  const timestamp = Date.now();
  const testEmail = `student-${timestamp}@e2etest.com`;

  test.beforeEach(async ({ page }) => {
    // Navigate to registration page
    await page.goto('/register');
    await page.waitForLoadState('networkidle');
  });

  test('should display registration form with all required fields', async ({ page }) => {
    // Check for role selection
    await expect(page.locator('text=Student')).toBeVisible();
    await expect(page.locator('text=Band Staff')).toBeVisible();
    await expect(page.locator('text=Director')).toBeVisible();

    // Check for common fields
    await expect(page.locator('input[name="email"], input[type="email"]')).toBeVisible();
    await expect(page.locator('input[name="password"], input[type="password"]').first()).toBeVisible();
    await expect(page.locator('input[name="firstName"]')).toBeVisible();
    await expect(page.locator('input[name="lastName"]')).toBeVisible();
  });

  test('should show student-specific fields when Student role is selected', async ({ page }) => {
    // Select Student role
    await page.click('text=Student');
    
    // Wait for student-specific fields to appear
    await expect(page.locator('input[name="school"]')).toBeVisible();
    await expect(page.locator('input[name="instrument"], select[name="instrument"]')).toBeVisible();
    await expect(page.locator('input[name="graduationYear"], select[name="graduationYear"]')).toBeVisible();
  });

  test('should validate required fields', async ({ page }) => {
    // Select Student role
    await page.click('text=Student');

    // Try to submit without filling fields
    await page.click('button[type="submit"]');

    // Check for validation errors
    // Note: Adjust selectors based on your actual error message implementation
    const errorElements = page.locator('.error, .text-red-500, [role="alert"]');
    await expect(errorElements.first()).toBeVisible({ timeout: 3000 });
  });

  test('should validate email format', async ({ page }) => {
    // Select Student role
    await page.click('text=Student');

    // Fill in invalid email
    await page.fill('input[name="email"], input[type="email"]', 'invalid-email');
    await page.fill('input[name="password"], input[type="password"]', 'ValidPass123!');
    
    // Click outside to trigger validation
    await page.click('input[name="firstName"]');

    // Check for email validation error
    const emailError = page.locator('text=/invalid.*email|email.*invalid/i').first();
    await expect(emailError).toBeVisible({ timeout: 3000 });
  });

  test('should validate password strength', async ({ page }) => {
    // Select Student role
    await page.click('text=Student');

    // Fill in weak password
    await page.fill('input[name="email"], input[type="email"]', testEmail);
    await page.fill('input[name="password"], input[type="password"]', 'weak');

    // Click outside to trigger validation
    await page.click('input[name="firstName"]');

    // Check for password strength error
    const passwordError = page.locator('text=/password.*strong|weak.*password/i').first();
    await expect(passwordError).toBeVisible({ timeout: 3000 });
  });

  test('should validate password confirmation match', async ({ page }) => {
    // Select Student role
    await page.click('text=Student');

    const password = 'ValidPass123!';
    await page.fill('input[name="password"], input[type="password"]', password);
    
    // Fill different confirm password
    const confirmPasswordInput = page.locator('input[name="confirmPassword"]').or(
      page.locator('input[type="password"]').nth(1)
    );
    await confirmPasswordInput.fill('DifferentPass123!');

    // Click outside to trigger validation
    await page.click('input[name="firstName"]');

    // Check for password match error
    const matchError = page.locator('text=/password.*match|confirm.*password/i').first();
    await expect(matchError).toBeVisible({ timeout: 3000 });
  });

  test('should successfully register a new student', async ({ page }) => {
    // Select Student role
    await page.click('text=Student');

    // Fill in all required fields
    await page.fill('input[name="email"], input[type="email"]', testEmail);
    await page.fill('input[name="password"], input[type="password"]', 'TestPassword123!');
    
    const confirmPasswordInput = page.locator('input[name="confirmPassword"]').or(
      page.locator('input[type="password"]').nth(1)
    );
    await confirmPasswordInput.fill('TestPassword123!');

    await page.fill('input[name="firstName"]', 'E2E');
    await page.fill('input[name="lastName"]', 'Test');

    // Fill student-specific fields
    await page.fill('input[name="school"]', 'Test University');
    
    // Handle instrument field (could be input or select)
    const instrumentInput = page.locator('input[name="instrument"]');
    const instrumentSelect = page.locator('select[name="instrument"]');
    
    if (await instrumentInput.isVisible({ timeout: 1000 }).catch(() => false)) {
      await instrumentInput.fill('Trumpet');
    } else if (await instrumentSelect.isVisible({ timeout: 1000 }).catch(() => false)) {
      await instrumentSelect.selectOption('Trumpet');
    }

    // Handle graduation year
    const yearInput = page.locator('input[name="graduationYear"]');
    const yearSelect = page.locator('select[name="graduationYear"]');
    
    if (await yearInput.isVisible({ timeout: 1000 }).catch(() => false)) {
      await yearInput.fill('2025');
    } else if (await yearSelect.isVisible({ timeout: 1000 }).catch(() => false)) {
      await yearSelect.selectOption('2025');
    }

    // Submit form
    await page.click('button[type="submit"]');

    // Wait for either success redirect or success message
    await page.waitForTimeout(2000);

    // Check if we were redirected to login or dashboard
    const url = page.url();
    const isSuccessful = url.includes('/login') || 
                         url.includes('/dashboard') ||
                         await page.locator('text=/success|registered|welcome/i').isVisible({ timeout: 5000 });

    expect(isSuccessful).toBeTruthy();
  });

  test('should prevent duplicate email registration', async ({ page }) => {
    // This test assumes a user already exists with this email
    // Use a known test user email from seeded data
    const existingEmail = 'student@gmail.com';

    // Select Student role
    await page.click('text=Student');

    // Fill in form with existing email
    await page.fill('input[name="email"], input[type="email"]', existingEmail);
    await page.fill('input[name="password"], input[type="password"]', 'TestPassword123!');
    
    const confirmPasswordInput = page.locator('input[name="confirmPassword"]').or(
      page.locator('input[type="password"]').nth(1)
    );
    await confirmPasswordInput.fill('TestPassword123!');

    await page.fill('input[name="firstName"]', 'Test');
    await page.fill('input[name="lastName"]', 'User');
    await page.fill('input[name="school"]', 'Test University');

    // Submit form
    await page.click('button[type="submit"]');

    // Wait for error message
    await page.waitForTimeout(1000);

    // Check for duplicate email error
    const errorMessage = page.locator('text=/email.*already.*exists|already.*registered/i');
    await expect(errorMessage).toBeVisible({ timeout: 5000 });
  });

  test('should allow navigation back to login', async ({ page }) => {
    // Look for login link
    const loginLink = page.locator('a[href*="/login"], text=/already.*account|login/i');
    await expect(loginLink.first()).toBeVisible();

    // Click login link
    await loginLink.first().click();

    // Verify navigation to login page
    await expect(page).toHaveURL(/login/);
  });
});
