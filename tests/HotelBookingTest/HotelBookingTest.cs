using HotelBookingAPI.DTOs;
using System.Net.Http.Json;
using Microsoft.Playwright;
using System.Text.Json;

namespace HotelBookingTest
{
    using System.Collections.Generic;
    using HotelBookingAPI.DTOs;

    [TestFixture]
    public class HotelBookingTest
    {
        private static readonly HttpClient client = new();

        [OneTimeSetUp]
        public void Setup()
        {
            var baseUrl = TestContext.Parameters["BaseUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                Assert.Fail("BaseUrl is not configured. Please provide it via TestContext parameters or appsettings.");
            }
            else
            {
                client.BaseAddress = new Uri(baseUrl);
            }
        }

        [TestCase(1)]
        [TestCase(5)]
        public async Task GetBooking(int id)
        {
            var response = await client.GetAsync($"GetBooking/{id}");
            Assert.That(response.IsSuccessStatusCode, Is.True, $"Failed to get booking {id}");

            var booking = await response.Content.ReadFromJsonAsync<BookingDto>();
            Assert.That(booking, Is.Not.Null);
            Console.WriteLine($"Booking Id: {booking.Id}, CustomerId: {booking.CustomerId}, Room: {booking.RoomNumber}");
        }

        [TestCase(1)]
        [TestCase(7)]
        [TestCase(10)]
        public async Task GetCustomer(int id)
        {
            var response = await client.GetAsync($"GetCustomer/{id}");
            Assert.That(response.IsSuccessStatusCode, Is.True, $"Failed to get customer {id}");

            var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();
            Assert.That(customer, Is.Not.Null);
            Console.WriteLine($"Customer Id: {customer.Id}, Name: {customer.Name}");
        }

        [TestCase(0, "Kosmo Kramer", null)] //id =0 for create, id>0 for edit
        [TestCase(7, "Gary Seven", null)]
        public async Task CreateEditCustomer(int id, string name, List<BookingDto> bookings)
        {
            var dto = new CustomerDto
            {
                Id = id,
                Name = name,
                Bookings = bookings ?? []
            };

            var response = await client.PostAsJsonAsync("CreateEditCustomer", dto);
            Assert.That(response.IsSuccessStatusCode, Is.True, $"Failed to create/edit customer {dto.Id}");

            var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();
            Assert.That(customer, Is.Not.Null);
            Console.WriteLine($"Customer Id: {customer.Id}, Name: {customer.Name}");
        }

        // Playwright-based API test: performs the same GetBooking(int id) via Playwright's APIRequest.
        [TestCase(1)]
        [TestCase(5)]
        public async Task Playwright_GetBooking(int id)
        {
            var baseUrl = TestContext.Parameters["BaseUrl"];
            Assert.That(!string.IsNullOrWhiteSpace(baseUrl), Is.True, "BaseUrl is not configured");

            using var playwright = await Playwright.CreateAsync();
            await using var requestContext = await playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
            {
                BaseURL = baseUrl,
                IgnoreHTTPSErrors = true
            });

            var response = await requestContext.GetAsync($"GetBooking/{id}");
            Assert.That(response.Ok, Is.True, $"Playwright failed to get booking {id} (status {response.Status})");

            var body = await response.TextAsync();
            var booking = JsonSerializer.Deserialize<BookingDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.That(booking, Is.Not.Null);
            TestContext.WriteLine($"[Playwright] Booking Id: {booking.Id}, CustomerId: {booking.CustomerId}, Room: {booking.RoomNumber}");
        }
    }
}