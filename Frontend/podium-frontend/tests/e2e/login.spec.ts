import { test, expect } from '@playwright/test';

test.describe('Login Flow', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to login page before each test
    await page.goto('/login');
  });

  test('should display login form', async ({ page }) => {
    // Check if login form elements are visible
    await expect(page.locator('input[type="email"]')).toBeVisible();
    await expect(page.locator('input[type="password"]')).toBeVisible();
    await expect(page.locator('button[type="submit"]')).toBeVisible();
  });

  test('should show validation errors for empty form', async ({ page }) => {
    // Click submit without filling form
    await page.click('button[type="submit"]');

    // Check for validation messages
    // Note: Update selectors based on actual implementation
    const errorMessages = page.locator('.error-message, .text-red-500, [role="alert"]');
    await expect(errorMessages.first()).toBeVisible();
  });

  test('should show error for invalid credentials', async ({ page }) => {
    // Fill in invalid credentials
    await page.fill('input[type="email"]', 'invalid@example.com');
    await page.fill('input[type="password"]', 'wrongpassword');

    // Submit form
    await page.click('button[type="submit"]');

    // Wait for error message
    await page.waitForTimeout(1000);

    // Check for error notification
    const errorNotification = page.locator('.error, .alert-error, [role="alert"]').first();
    await expect(errorNotification).toBeVisible({ timeout: 5000 });
  });

  test('should successfully login with valid credentials', async ({ page }) => {
    // Note: This test requires seeded test data in the backend
    // Use test credentials that exist in your test database
    await page.fill('input[type="email"]', 'student@gmail.com');
    await page.fill('input[type="password"]', 'Password123!');

    // Submit form
    await page.click('button[type="submit"]');

    // Wait for navigation to dashboard
    await page.waitForURL(/dashboard/, { timeout: 10000 });

    // Verify we're on the dashboard
    expect(page.url()).toContain('dashboard');
  });
});
