using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingAPI.Models
{
    public class HotelBooking
    {
        public int Id { get; set; }

        // Foreign key
        public int CustomerId { get; set; }

        [Required]
        [Range(101, 1401)]
        public int RoomNumber { get; set; }

        // Navigation property
        public Customer Customer { get; set; } = null!;
    }

    public class Customer
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        // Navigation property for one-to-many
        public ICollection<HotelBooking> Bookings { get; set; } = new List<HotelBooking>();
    }
}