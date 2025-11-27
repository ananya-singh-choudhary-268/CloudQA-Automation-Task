using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;

namespace CloudQATests
{
    [TestFixture]
    public class SeleniumTests
    {
        private IWebDriver? driver;

        [SetUp]
        public void Setup()
        {
            var options = new ChromeOptions();
            // options.AddArgument("--headless=new"); // uncomment to run headless
            driver = new ChromeDriver(options); // Selenium Manager should manage driver automatically
            driver.Manage().Window.Size = new System.Drawing.Size(1280, 800);
            driver.Navigate().GoToUrl("https://app.cloudqa.io/home/AutomationPracticeForm");
            
            // Give the page extra time to load
            System.Threading.Thread.Sleep(2000);
        }

        [TearDown]
        public void TearDown()
        {
            // Dispose / Quit the driver to satisfy NUnit analyzer and avoid orphan processes
            try
            {
                driver?.Quit();
            }
            catch { /* ignore */ }

            try
            {
                driver?.Dispose();
            }
            catch { /* ignore */ }

            driver = null;
        }

        // Robust helper: returns IWebElement? to match possible null fallback
        private IWebElement? TryFind(By by, int timeoutSeconds = 5)
        {
            if (driver == null) return null;
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
                return wait.Until(d =>
                {
                    try
                    {
                        var e = d.FindElement(by);
                        return (e != null && e.Displayed) ? e : null;
                    }
                    catch (NoSuchElementException)
                    {
                        return null;
                    }
                    catch (StaleElementReferenceException)
                    {
                        return null;
                    }
                });
            }
            catch
            {
                return null;
            }
        }

        // Find input/select/textarea by visible label or placeholder text.
        // Throws if not found.
        private IWebElement FindFieldByLabel(string labelText)
        {
            if (driver == null) throw new InvalidOperationException("Driver is not initialized.");

            // 1) label -> following input/textarea/select
            var xpath1 = $"//label[contains(normalize-space(.), \"{labelText}\")]/following::input[1] | //label[contains(normalize-space(.), \"{labelText}\")]/following::textarea[1] | //label[contains(normalize-space(.), \"{labelText}\")]/following::select[1]";
            var el = TryFind(By.XPath(xpath1));
            if (el != null) return el;

            // 2) input/textarea/select with placeholder or aria-label containing the text
            var xpath2 = $"//input[contains(@placeholder, \"{labelText}\") or contains(@aria-label, \"{labelText}\")] | //textarea[contains(@placeholder, \"{labelText}\") or contains(@aria-label, \"{labelText}\")] | //select[contains(@aria-label, \"{labelText}\")]";
            el = TryFind(By.XPath(xpath2));
            if (el != null) return el;

            // 3) fallback: find node with text then nearest following input/select/textarea
            var xpath3 = $"//*[contains(normalize-space(.), \"{labelText}\")]//following::input[1] | //*[contains(normalize-space(.), \"{labelText}\")]//following::select[1] | //*[contains(normalize-space(.), \"{labelText}\")]//following::textarea[1]";
            el = TryFind(By.XPath(xpath3));
            if (el != null) return el;

            throw new NoSuchElementException($"Field with label or placeholder containing '{labelText}' not found.");
        }

        [Test]
        public void Test_FirstName_Input()
        {
            var input = FindFieldByLabel("First Name");
            input.Clear();
            input.SendKeys("Ananya");

            Assert.That(input.GetAttribute("value"), Is.EqualTo("Ananya"));
        }

        [Test]
        public void Test_Email_Input()
        {
            var input = FindFieldByLabel("Email");
            input.Clear();
            input.SendKeys("test@example.com");
            Assert.That(input.GetAttribute("value"), Is.EqualTo("test@example.com"));
        }

        [Test]
        public void Test_Gender_Radio()
        {
            if (driver == null) Assert.Fail("Driver was null.");

            // Strategy: Find all radio buttons, then identify which one is for "male"
            var allRadios = driver.FindElements(By.XPath("//input[@type='radio']"));
            Assert.That(allRadios.Count, Is.GreaterThan(0), "No radio buttons found on the page.");

            IWebElement? maleRadio = null;
            
            // Check each radio button to see if it's associated with "male" text
            foreach (var radio in allRadios)
            {
                try
                {
                    // Get the parent element and all its text
                    var parent = radio.FindElement(By.XPath("./.."));
                    var parentText = parent.Text.ToLower();
                    
                    // Also check for id/name/value attributes
                    var radioId = radio.GetAttribute("id")?.ToLower() ?? "";
                    var radioName = radio.GetAttribute("name")?.ToLower() ?? "";
                    var radioValue = radio.GetAttribute("value")?.ToLower() ?? "";
                    
                    // If any of these contain "male" (but not "female"), we found it
                    if ((parentText.Contains("male") && !parentText.Contains("female")) ||
                        (radioValue.Contains("male") && !radioValue.Contains("female")) ||
                        (radioId.Contains("male") && !radioId.Contains("female")))
                    {
                        maleRadio = radio;
                        break;
                    }
                }
                catch
                {
                    // If we can't access this radio, skip it
                    continue;
                }
            }

            Assert.That(maleRadio, Is.Not.Null, "Could not find a radio button associated with 'male'.");

            // Click the radio button (or its parent if it's not directly clickable)
            try
            {
                maleRadio!.Click();
            }
            catch (ElementClickInterceptedException)
            {
                // If direct click doesn't work, try clicking the parent label
                var parent = maleRadio.FindElement(By.XPath("./.."));
                parent.Click();
            }

            // Give it a moment to register
            System.Threading.Thread.Sleep(500);

            // Verify it's selected
            Assert.That(maleRadio.Selected, Is.True, "Expected the Male radio button to be selected after clicking.");
        }
    }
}