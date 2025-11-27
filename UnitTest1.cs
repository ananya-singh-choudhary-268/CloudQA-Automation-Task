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
            driver = new ChromeDriver(options);
            driver.Manage().Window.Size = new System.Drawing.Size(1280, 800);
            driver.Navigate().GoToUrl("https://app.cloudqa.io/home/AutomationPracticeForm");
            System.Threading.Thread.Sleep(2000);
        }

        [TearDown]
        public void TearDown()
        {
            try { driver?.Quit(); } catch { }
            try { driver?.Dispose(); } catch { }
            driver = null;
        }

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
                    catch (NoSuchElementException) { return null; }
                    catch (StaleElementReferenceException) { return null; }
                });
            }
            catch { return null; }
        }

        private IWebElement FindFieldByLabel(string labelText)
        {
            if (driver == null) throw new InvalidOperationException("Driver is not initialized.");

            var xpath1 = $"//label[contains(normalize-space(.), \"{labelText}\")]/following::input[1] | //label[contains(normalize-space(.), \"{labelText}\")]/following::textarea[1] | //label[contains(normalize-space(.), \"{labelText}\")]/following::select[1]";
            var el = TryFind(By.XPath(xpath1));
            if (el != null) return el;

            var xpath2 = $"//input[contains(@placeholder, \"{labelText}\") or contains(@aria-label, \"{labelText}\")] | //textarea[contains(@placeholder, \"{labelText}\") or contains(@aria-label, \"{labelText}\")] | //select[contains(@aria-label, \"{labelText}\")]";
            el = TryFind(By.XPath(xpath2));
            if (el != null) return el;

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

            var allRadios = driver.FindElements(By.XPath("//input[@type='radio']"));
            Assert.That(allRadios.Count, Is.GreaterThan(0), "No radio buttons found on the page.");

            IWebElement? maleRadio = null;
            
            foreach (var radio in allRadios)
            {
                try
                {
                    var parent = radio.FindElement(By.XPath("./.."));
                    var parentText = parent.Text.ToLower();
                    var radioId = radio.GetAttribute("id")?.ToLower() ?? "";
                    var radioName = radio.GetAttribute("name")?.ToLower() ?? "";
                    var radioValue = radio.GetAttribute("value")?.ToLower() ?? "";
                    
                    if ((parentText.Contains("male") && !parentText.Contains("female")) ||
                        (radioValue.Contains("male") && !radioValue.Contains("female")) ||
                        (radioId.Contains("male") && !radioId.Contains("female")))
                    {
                        maleRadio = radio;
                        break;
                    }
                }
                catch { continue; }
            }

            Assert.That(maleRadio, Is.Not.Null, "Could not find a radio button associated with 'male'.");

            try
            {
                maleRadio!.Click();
            }
            catch (ElementClickInterceptedException)
            {
                var parent = maleRadio.FindElement(By.XPath("./.."));
                parent.Click();
            }

            System.Threading.Thread.Sleep(500);
            Assert.That(maleRadio.Selected, Is.True, "Expected the Male radio button to be selected after clicking.");
        }
    }
}