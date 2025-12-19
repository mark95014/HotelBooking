using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBookingAPI.Models;
using HotelBookingAPI.Data;
using HotelBookingAPI.DTOs;

namespace HotelBookingAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class HotelBookingController : ControllerBase
    {
        private readonly ApiContext _context;

        public HotelBookingController(ApiContext context)
        {
            _context = context;
        }

        // -------------------- BOOKINGS --------------------

        [HttpPost]
        public async Task<IActionResult> CreateEditBooking([FromBody] BookingDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var customer = await _context.Customers.FindAsync(dto.CustomerId);
            if (customer == null)
                return NotFound($"Customer with Id {dto.CustomerId} not found");

            HotelBooking? entity;
            if (dto.Id == 0)
            {
                entity = new HotelBooking
                {
                    CustomerId = dto.CustomerId,
                    RoomNumber = dto.RoomNumber
                };
                _context.Bookings.Add(entity);
            }
            else
            {
                entity = await _context.Bookings.FindAsync(dto.Id);
                if (entity == null)
                    return NotFound($"Booking with Id {dto.Id} not found");

                entity.CustomerId = dto.CustomerId;
                entity.RoomNumber = dto.RoomNumber;
            }

            await _context.SaveChangesAsync();
            return Ok(dto);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBooking(int id)
        {
            var result = await _context.Bookings
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (result == null)
                return NotFound();

            var dto = new BookingDto
            {
                Id = result.Id,
                CustomerId = result.CustomerId,
                RoomNumber = result.RoomNumber
            };

            return Ok(dto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var result = await _context.Bookings.FindAsync(id);
            if (result == null)
                return NotFound();

            _context.Bookings.Remove(result);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBookings()
        {
            var result = await _context.Bookings
                .Include(b => b.Customer)
                .Select(b => new BookingDto
                {
                    Id = b.Id,
                    CustomerId = b.CustomerId,
                    RoomNumber = b.RoomNumber
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> ImportBookings([FromBody] List<BookingDto> dtos)
        {
            if (dtos == null || dtos.Count == 0)
                return BadRequest("No bookings provided");

            var entities = dtos.Select(b => new HotelBooking
            {
                CustomerId = b.CustomerId,
                RoomNumber = b.RoomNumber
                // Id is ignored here, EF will assign it
            }).ToList();

            _context.Bookings.AddRange(entities);
            await _context.SaveChangesAsync();

            // Return the saved entities with their new Ids
            var result = entities.Select(e => new BookingDto
            {
                Id = e.Id,
                CustomerId = e.CustomerId,
                RoomNumber = e.RoomNumber
            });

            return Ok(new { Count = entities.Count, Imported = result });
        }

        // -------------------- CUSTOMERS --------------------

        [HttpPost]
        public async Task<IActionResult> CreateEditCustomer([FromBody] CustomerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            Customer? entity;
            if (dto.Id == 0)
            {
                entity = new Customer { Name = dto.Name };
                _context.Customers.Add(entity);
            }
            else
            {
                entity = await _context.Customers.FindAsync(dto.Id);
                if (entity == null)
                    return NotFound($"Customer with Id {dto.Id} not found");

                entity.Name = dto.Name;
            }

            await _context.SaveChangesAsync();
            return Ok(dto);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomer(int id)
        {
            var result = await _context.Customers
                .Include(c => c.Bookings)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (result == null)
                return NotFound();

            var dto = new CustomerDto
            {
                Id = result.Id,
                Name = result.Name
            };

            return Ok(dto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var result = await _context.Customers.FindAsync(id);
            if (result == null)
                return NotFound();

            _context.Customers.Remove(result); //Cascading delete of Bookings by default
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCustomers()
        {
            var result = await _context.Customers
                .Include(c => c.Bookings) // ensure EF loads bookings
                .Select(c => new CustomerDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Bookings = c.Bookings.Select(b => new BookingDto
                    {
                        Id = b.Id,
                        CustomerId = b.CustomerId,
                        RoomNumber = b.RoomNumber
                    }).ToList()
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> ImportCustomers([FromBody] List<CustomerDto> dtos)
        {
            if (dtos == null || dtos.Count == 0)
                return BadRequest("No customers provided");

            var entities = dtos.Select(c => new Customer
            {
                Name = c.Name
            }).ToList();

            _context.Customers.AddRange(entities);
            await _context.SaveChangesAsync();

            return Ok(new { dtos.Count, Imported = dtos });
        }
    }
}