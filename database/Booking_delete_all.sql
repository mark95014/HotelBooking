use BookingDb
delete from bookings
DBCC CHECKIDENT ('Bookings', RESEED, 0);
delete from customers
DBCC CHECKIDENT ('Customers', RESEED, 0);