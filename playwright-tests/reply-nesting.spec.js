const { test, expect } = require('@playwright/test');

test('reply-to-reply-to-reply works at 3 levels', async ({ page }) => {
  // Login
  await page.goto('http://localhost:5173/login');
  await page.fill('input[placeholder*="Email"]', 'test@example.com');
  await page.fill('input[placeholder*="Password"]', 'Password123!');
  await page.click('button:has-text("Sign In")');
  await page.waitForURL('**/families**');

  // Go to first family feed
  await page.click('.btn:has-text("Enter Family")');
  await page.waitForURL('**/families/**');

  // Create a post
  await page.fill('textarea', 'Test post for reply nesting');
  await page.click('button:has-text("Post")');
  await page.waitForSelector('.card');

  // Open comments
  await page.click('button:has-text("comment")');
  await page.waitForSelector('input[placeholder="Write a comment"]');

  // Add top-level comment
  await page.fill('input[placeholder="Write a comment"]', 'Top comment');
  await page.press('input[placeholder="Write a comment"]', 'Enter');
  await page.waitForTimeout(1000);

  // Click reply on the top comment
  await page.click('button:has-text("Reply")');
  await page.waitForSelector('input[placeholder="Write a reply"]');

  // Add first-level reply
  await page.fill('input[placeholder="Write a reply"]', 'First-level reply');
  await page.press('input[placeholder="Write a reply"]', 'Enter');
  await page.waitForTimeout(1000);

  // Click reply on the first-level reply
  await page.click('button:has-text("Reply")');
  await page.waitForSelector('input[placeholder="Write a reply"]');

  // Add second-level reply (reply-to-reply-to-reply)
  await page.fill('input[placeholder="Write a reply"]', 'Second-level reply');
  await page.press('input[placeholder="Write a reply"]', 'Enter');
  await page.waitForTimeout(1000);

  // Verify all 3 levels are visible
  const comments = await page.locator('strong').allTextContents();
  console.log('Comments found:', comments);

  // Check that all 3 comments are visible
  await expect(page.locator('text=Top comment')).toBeVisible();
  await expect(page.locator('text=First-level reply')).toBeVisible();
  await expect(page.locator('text=Second-level reply')).toBeVisible();

  console.log('✅ Reply-to-reply-to-reply test passed!');
});
