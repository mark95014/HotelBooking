namespace HotelBookingAPI.DTOs
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<BookingDto> Bookings { get; set; } = [];
    }
}