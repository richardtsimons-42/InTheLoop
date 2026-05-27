import asyncio
from playwright.async_api import async_playwright
import random
import string

async def test_reply_nesting():
    async with async_playwright() as p:
        browser = await p.chromium.launch(headless=True)
        context = await browser.new_context()
        page = await context.new_page()
        
        # Generate unique email
        random_str = ''.join(random.choices(string.ascii_lowercase, k=8))
        email = f"test{random_str}@example.com"
        
        print(f"Using email: {email}")
        
        # Enable request/response logging
        page.on("response", lambda resp: print(f"  [API] {resp.status} {resp.url}"))
        
        try:
            # Register a new user
            print("1. Registering test user...")
            await page.goto("http://localhost:5173/register", timeout=10000)
            await page.fill('input[placeholder="John"]', "Test")
            await page.fill('input[placeholder="Doe"]', "User")
            await page.fill('input[placeholder="you@example.com"]', email)
            await page.fill('input[placeholder="Min. 8 characters"]', "Password123!")
            await page.click('button:has-text("Create Account")')
            # Registration redirects to /feed
            await page.wait_for_url("**/feed**", timeout=10000)
            print("  ✅ User registered, on /feed")
            
            # Navigate to FamiliesPage to create a family
            print("2. Creating family...")
            await page.click('a:has-text("Families")', timeout=5000)
            await page.wait_for_url("**/families**", timeout=5000)
            await page.click('button:has-text("New Family")', timeout=5000)
            await page.wait_for_url("**/families/new**", timeout=5000)
            await page.fill('input[placeholder="The Smiths"]', "Test Family")
            await page.click('button:has-text("Create Family")')
            await page.wait_for_url("**/families/**", timeout=10000)
            print("  ✅ Family created")
            
            # Wait for feed to load
            await page.wait_for_selector('.card, .empty-state', timeout=5000)
            print("  ✅ Feed loaded")
            
            # Create a post
            print("3. Creating post...")
            textarea = page.locator('textarea').first
            await textarea.wait_for(state='visible', timeout=5000)
            await textarea.fill("Test post for reply nesting")
            await page.click('button:has-text("Post")', timeout=5000)
            await page.wait_for_selector('.card', timeout=5000)
            print("  ✅ Post created")
            
            # Open comments
            print("4. Opening comments...")
            await page.click('button:has-text("comment")')
            await page.wait_for_selector('input[placeholder="Write a comment"]', timeout=5000)
            print("  ✅ Comments opened")
            
            # Add top-level comment
            print("5. Adding top-level comment...")
            await page.fill('input[placeholder="Write a comment"]', "Top comment")
            await page.press('input[placeholder="Write a comment"]', "Enter")
            await asyncio.sleep(1)
            print("  ✅ Top comment added")
            
            # Click reply on the top comment
            print("6. Replying to top comment...")
            await page.click('button:has-text("Reply")')
            await page.wait_for_selector('input[placeholder="Write a reply"]', timeout=5000)
            
            # Add first-level reply
            await page.fill('input[placeholder="Write a reply"]', "First-level reply")
            await page.press('input[placeholder="Write a reply"]', "Enter")
            await asyncio.sleep(1)
            print("  ✅ First-level reply added")
            
            # Click reply on the first-level reply
            print("7. Replying to first-level reply...")
            await page.click('button:has-text("Reply")')
            await page.wait_for_selector('input[placeholder="Write a reply"]', timeout=5000)
            
            # Add second-level reply (reply-to-reply-to-reply)
            await page.fill('input[placeholder="Write a reply"]', "Second-level reply")
            await page.press('input[placeholder="Write a reply"]', "Enter")
            await asyncio.sleep(1)
            print("  ✅ Second-level reply added")
            
            # Verify all 3 levels are visible
            print("8. Verifying all 3 levels...")
            top_comment = await page.locator('text=Top comment').first.is_visible()
            first_reply = await page.locator('text=First-level reply').first.is_visible()
            second_reply = await page.locator('text=Second-level reply').first.is_visible()
            
            print(f"  Top comment visible: {top_comment}")
            print(f"  First-level reply visible: {first_reply}")
            print(f"  Second-level reply visible: {second_reply}")
            
            if top_comment and first_reply and second_reply:
                print("\n✅ Reply-to-reply-to-reply test PASSED!")
                return True
            else:
                print("\n❌ Reply-to-reply-to-reply test FAILED!")
                return False
                
        except Exception as e:
            print(f"\n❌ Test error: {e}")
            # Take screenshot on failure
            await page.screenshot(path="reply-test-failure.png", full_page=True)
            print("  Screenshot saved to reply-test-failure.png")
            return False
        finally:
            await browser.close()

if __name__ == "__main__":
    result = asyncio.run(test_reply_nesting())
    exit(0 if result else 1)
