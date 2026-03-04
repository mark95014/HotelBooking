using Microsoft.AspNetCore.Mvc;

namespace HotelBookingAPI.Controllers
{
    [Route("")]
    public class UiController : Controller
    {
        [HttpGet("")]
        public IActionResult Root()
        {
            return Content(@"<html>
  <head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
    <title>HotelBookingAPI UI</title>
    <style>
      body { font-family:Segoe UI, Roboto, Arial; margin:24px; }
      .panel { border:1px solid #ddd; padding:12px; margin-bottom:12px; border-radius:6px; }
      label{display:block;margin-top:6px}
      input, textarea { width:100%; padding:6px; box-sizing:border-box; }
      button { margin-top:8px; padding:6px 12px; }
      pre { background:#f7f7f7; padding:8px; overflow:auto; }
    </style>
  </head>
  <body>
    <h1>HotelBookingAPI - Minimal UI</h1>

    <div class=""panel"">
      <h2>Lookup Booking</h2>
      <label>Booking Id <input id=""bookingId"" type=""number"" /></label>
      <button onclick=""lookupBooking()"">Get Booking</button>
      <pre id=""bookingResult""></pre>
    </div>

    <div class=""panel"">
      <h2>Lookup Customer</h2>
      <label>Customer Id <input id=""customerId"" type=""number"" /></label>
      <button onclick=""lookupCustomer()"">Get Customer</button>
      <pre id=""customerResult""></pre>
    </div>

    <div class=""panel"">
      <h2>Create / Edit Customer</h2>
      <small>Use Id = 0 to create (server may ignore client id for create), non-zero to edit/upsert depending on API.</small>
      <label>Id <input id=""custFormId"" type=""number"" value=""0"" /></label>
      <label>Name <input id=""custFormName"" type=""text"" /></label>
      <label>Bookings (JSON array) <textarea id=""custFormBookings"" rows=""3"">[]</textarea></label>
      <button onclick=""saveCustomer()"">Save Customer (POST CreateEditCustomer)</button>
      <button onclick=""deleteCustomer()"">Delete Customer</button>
      <pre id=""custSaveResult""></pre>
    </div>

    <div class=""panel"">
      <h2>Create / Edit Booking</h2>
      <label>Id <input id=""bookFormId"" type=""number"" value=""0"" /></label>
      <label>CustomerId <input id=""bookFormCustomerId"" type=""number"" /></label>
      <label>RoomNumber <input id=""bookFormRoomNumber"" type=""number"" /></label>
      <button onclick=""saveBooking()"">Save Booking (POST CreateEditBooking)</button>
      <button onclick=""deleteBooking()"">Delete Booking</button>
      <pre id=""bookSaveResult""></pre>
    </div>

    <script>
      function showResult(elemId, obj) {
        document.getElementById(elemId).textContent = typeof obj === 'string' ? obj : JSON.stringify(obj, null, 2);
      }

      async function lookupBooking() {
        const id = document.getElementById('bookingId').value;
        if (!id) return showResult('bookingResult', 'Enter an id');
        try {
          const res = await fetch(`api/HotelBooking/GetBooking/${id}`);
          const text = await res.text();
          if (!res.ok) return showResult('bookingResult', `Error ${res.status}: ${text}`);
          showResult('bookingResult', JSON.parse(text || '{}'));
        } catch (e) { showResult('bookingResult', e.toString()); }
      }

      async function lookupCustomer() {
        const id = document.getElementById('customerId').value;
        if (!id) return showResult('customerResult', 'Enter an id');
        try {
          const res = await fetch(`api/HotelBooking/GetCustomer/${id}`);
          const text = await res.text();
          if (!res.ok) return showResult('customerResult', `Error ${res.status}: ${text}`);
          showResult('customerResult', JSON.parse(text || '{}'));
        } catch (e) { showResult('customerResult', e.toString()); }
      }

      async function saveCustomer() {
        const id = Number(document.getElementById('custFormId').value || 0);
        const name = document.getElementById('custFormName').value;
        let bookings;
        try { bookings = JSON.parse(document.getElementById('custFormBookings').value || '[]'); }
        catch (e) { return showResult('custSaveResult', 'Bookings must be valid JSON array'); }

        const dto = { Id: id, Name: name, Bookings: bookings };
        try {
          const res = await fetch('api/HotelBooking/CreateEditCustomer', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto)
          });
          const text = await res.text();
          if (!res.ok) return showResult('custSaveResult', `Error ${res.status}: ${text}`);
          showResult('custSaveResult', JSON.parse(text || '{}'));
        } catch (e) { showResult('custSaveResult', e.toString()); }
      }

      async function deleteCustomer() {
        const id = Number(document.getElementById('custFormId').value || 0);
        if (!id) return showResult('custSaveResult', 'Provide id to delete');
        try {
          const res = await fetch(`api/HotelBooking/DeleteCustomer/${id}`, { method: 'DELETE' });
          const text = await res.text();
          if (!res.ok) return showResult('custSaveResult', `Error ${res.status}: ${text}`);
          showResult('custSaveResult', text || 'Deleted');
        } catch (e) { showResult('custSaveResult', e.toString()); }
      }

      async function saveBooking() {
        const id = Number(document.getElementById('bookFormId').value || 0);
        const customerId = Number(document.getElementById('bookFormCustomerId').value || 0);
        const roomNumber = Number(document.getElementById('bookFormRoomNumber').value || 0);

        const dto = { Id: id, CustomerId: customerId, RoomNumber: roomNumber };
        try {
          const res = await fetch('api/HotelBooking/CreateEditBooking', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto)
          });
          const text = await res.text();
          if (!res.ok) return showResult('bookSaveResult', `Error ${res.status}: ${text}`);
          showResult('bookSaveResult', JSON.parse(text || '{}'));
        } catch (e) { showResult('bookSaveResult', e.toString()); }
      }

      async function deleteBooking() {
        const id = Number(document.getElementById('bookFormId').value || 0);
        if (!id) return showResult('bookSaveResult', 'Provide id to delete');
        try {
          const res = await fetch(`api/HotelBooking/DeleteBooking/${id}`, { method: 'DELETE' });
          const text = await res.text();
          if (!res.ok) return showResult('bookSaveResult', `Error ${res.status}: ${text}`);
          showResult('bookSaveResult', text || 'Deleted');
        } catch (e) { showResult('bookSaveResult', e.toString()); }
      }
    </script>
  </body>
</html>", "text/html");
        }

        [HttpGet("ui")]
        public IActionResult Index()
        {
            return Root();
        }
    }
}