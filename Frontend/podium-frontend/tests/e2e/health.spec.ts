import { test, expect } from '@playwright/test';

test.describe('Health Check', () => {
  test('should load landing page successfully', async ({ page }) => {
    // Navigate to the landing page
    await page.goto('/');

    // Wait for page to load
    await page.waitForLoadState('networkidle');

    // Check that the page loaded
    await expect(page).toHaveTitle(/Podium/i);
  });

  test('should have working navigation', async ({ page }) => {
    await page.goto('/');

    // Check if main navigation elements exist
    const nav = page.locator('nav');
    await expect(nav).toBeVisible();
  });
});
