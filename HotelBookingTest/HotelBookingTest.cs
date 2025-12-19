using HotelBookingAPI.DTOs;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace HotelBookingTest
{
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

        [TestCase(31)]
        [TestCase(41)]
        public async Task GetCustomer(int id)
        {
            var response = await client.GetAsync($"GetCustomer/{id}");
            Assert.That(response.IsSuccessStatusCode, Is.True, $"Failed to get customer {id}");

            var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();
            Assert.That(customer, Is.Not.Null);
            Console.WriteLine($"Customer Id: {customer.Id}, Name: {customer.Name}");
        }
    }
}