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
                // Create new customer, optionally include bookings passed in dto
                entity = new Customer
                {
                    Name = dto.Name,
                    Bookings = dto.Bookings?.Select(b => new HotelBooking
                    {
                        RoomNumber = b.RoomNumber
                        // CustomerId will be set by EF when relationship is saved
                    }).ToList() ?? new List<HotelBooking>()
                };
                _context.Customers.Add(entity);

                await _context.SaveChangesAsync();

                // Update dto with generated ids
                dto.Id = entity.Id;
                if (entity.Bookings != null && entity.Bookings.Count > 0)
                {
                    dto.Bookings = entity.Bookings.Select(b => new BookingDto
                    {
                        Id = b.Id,
                        CustomerId = b.CustomerId,
                        RoomNumber = b.RoomNumber
                    }).ToList();
                }
                else
                {
                    dto.Bookings = new List<BookingDto>();
                }
            }
            else
            {
                // Edit existing customer. Include bookings for easier upsert.
                entity = await _context.Customers
                    .Include(c => c.Bookings)
                    .FirstOrDefaultAsync(c => c.Id == dto.Id);

                if (entity == null)
                    return NotFound($"Customer with Id {dto.Id} not found");

                entity.Name = dto.Name;

                if (dto.Bookings != null)
                {
                    // Upsert incoming bookings: update existing ones, add new ones.
                    var incoming = dto.Bookings;

                    // Update or add
                    foreach (var bDto in incoming)
                    {
                        if (bDto.Id == 0)
                        {
                            // new booking
                            var newBooking = new HotelBooking
                            {
                                CustomerId = entity.Id,
                                RoomNumber = bDto.RoomNumber
                            };
                            entity.Bookings.Add(newBooking);
                        }
                        else
                        {
                            // try to find in tracked entity bookings first
                            var existing = entity.Bookings.FirstOrDefault(b => b.Id == bDto.Id);

                            if (existing == null)
                            {
                                // Might be detached or belong to another customer; try DB
                                existing = await _context.Bookings.FindAsync(bDto.Id);
                            }

                            if (existing == null)
                            {
                                // referenced booking id not found
                                return NotFound($"Booking with Id {bDto.Id} not found");
                            }

                            // Ensure it is associated with this customer
                            existing.CustomerId = entity.Id;
                            existing.RoomNumber = bDto.RoomNumber;
                        }
                    }

                    // Optionally: do not remove bookings missing from dto.Bookings.
                    // If you want to remove missing bookings, uncomment below:
                    /*
                    var incomingIds = incoming.Where(b => b.Id > 0).Select(b => b.Id).ToHashSet();
                    var toRemove = entity.Bookings.Where(b => b.Id != 0 && !incomingIds.Contains(b.Id)).ToList();
                    foreach (var r in toRemove)
                        _context.Bookings.Remove(r);
                    */
                }

                await _context.SaveChangesAsync();

                // Refresh dto.Bookings from DB to return current state
                dto.Bookings = await _context.Bookings
                    .Where(b => b.CustomerId == entity.Id)
                    .Select(b => new BookingDto
                    {
                        Id = b.Id,
                        CustomerId = b.CustomerId,
                        RoomNumber = b.RoomNumber
                    })
                    .ToListAsync();
            }

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