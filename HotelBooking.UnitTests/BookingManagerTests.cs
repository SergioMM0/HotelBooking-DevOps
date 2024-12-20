using System;
using System.Collections.Generic;
using System.Linq;
using HotelBooking.Core;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace HotelBooking.UnitTests
{
    public class BookingManagerTests
    {
        private readonly BookingManager bookingManager;
        private readonly Mock<IRepository<Booking>> mockBookingRepository;
        private readonly Mock<IRepository<Room>> mockRoomRepository;

        public BookingManagerTests()
        {
            // Setup the mocks
            mockBookingRepository = new Mock<IRepository<Booking>>();
            mockRoomRepository = new Mock<IRepository<Room>>();
            bookingManager = new BookingManager(mockBookingRepository.Object, mockRoomRepository.Object);
        }

        #region FindAvailableRoom Tests

        [Fact]
        public void FindAvailableRoom_StartDateNotInTheFuture_ThrowsArgumentException()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(-1);
            DateTime endDate = DateTime.Today.AddDays(1);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => bookingManager.FindAvailableRoom(startDate, endDate));
            Assert.Equal("The start date cannot be in the past or later than the end date.", ex.Message);
        }

        [Fact]
        public void FindAvailableRoom_StartDateAfterEndDate_ThrowsArgumentException()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(10);
            DateTime endDate = DateTime.Today.AddDays(5);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => bookingManager.FindAvailableRoom(startDate, endDate));
            Assert.Equal("The start date cannot be in the past or later than the end date.", ex.Message);
        }

        [Fact]
        public void FindAvailableRoom_RoomAvailable_ReturnsValidRoomId()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(10);
            DateTime endDate = DateTime.Today.AddDays(12);

            // Mock room repository
            var rooms = new List<Room> { new() { Id = 1 }, new() { Id = 2 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (no conflicting bookings)
            var bookings = new List<Booking>();
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.NotEqual(-1, roomId);
            Assert.Contains(roomId, rooms.Select(r => r.Id));
        }

        [Fact]
        public void FindAvailableRoom_NoRoomAvailable_ReturnsMinusOne()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(10);
            DateTime endDate = DateTime.Today.AddDays(12);

            // Mock room repository
            var rooms = new List<Room> { new Room { Id = 1 }, new Room { Id = 2 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (all rooms are booked)
            var bookings = new List<Booking>
            {
                new () { RoomId = 1, StartDate = startDate, EndDate = endDate, IsActive = true },
                new () { RoomId = 2, StartDate = startDate, EndDate = endDate, IsActive = true }
            };
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(-1, roomId);
        }

        #endregion
        
        #region FindAvailableRoom Tests (MC/DC)
        //1. Start date is today or earlier, triggering an exception.
        [Fact]
        public void FindAvailableRoom_StartDateToday_ThrowsArgumentException()
        {
            // Arrange
            DateTime startDate = DateTime.Today;
            DateTime endDate = DateTime.Today.AddDays(1);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => bookingManager.FindAvailableRoom(startDate, endDate));
        }
        
        //2. Start date is after the end date, triggering an exception.
        //See: FindAvailableRoom_StartDateAfterEndDate_ThrowsArgumentException()
        
        //3. Valid date range; proceeds to room availability.
        [Fact]
        public void FindAvailableRoom_ValidDateRange_ReturnsValidRoomId()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(10);
            DateTime endDate = DateTime.Today.AddDays(12);

            // Mock room repository
            var rooms = new List<Room> { new() { Id = 1 }, new() { Id = 2 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (no conflicting bookings)
            var bookings = new List<Booking>();
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            mockRoomRepository.Verify(r => r.GetAll(), Times.Once);
            mockBookingRepository.Verify(b => b.GetAll(), Times.Once);
            
            Assert.NotEqual(-1, roomId);
            Assert.Contains(roomId, rooms.Select(r => r.Id));
        }
        
        //4.  Start and end dates are both before the room’s booked start date.
        [Fact]
        public void FindAvailableRoom_StartDateAndEndDateBeforeRoomBooking_ReturnsValidRoomId()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(10);
            DateTime endDate = DateTime.Today.AddDays(12);

            // Mock room repository
            var rooms = new List<Room> { new() { Id = 1 }, new() { Id = 2 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (no conflicting bookings)
            var bookings = new List<Booking>
            {
                new () { RoomId = 1, StartDate = DateTime.Today.AddDays(15), EndDate = DateTime.Today.AddDays(17), IsActive = true },
                new () { RoomId = 2, StartDate = DateTime.Today.AddDays(15), EndDate = DateTime.Today.AddDays(17), IsActive = true }
            };
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.NotEqual(-1, roomId);
            Assert.Contains(roomId, rooms.Select(r => r.Id));
        }
        
        //5. Start and end dates are both after the room’s booked end date.
        [Fact]
        public void FindAvailableRoom_StartDateAndEndDateAfterRoomBooking_ReturnsValidRoomId()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(10);
            DateTime endDate = DateTime.Today.AddDays(12);

            // Mock room repository
            var rooms = new List<Room> { new() { Id = 1 }, new() { Id = 2 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (no conflicting bookings)
            var bookings = new List<Booking>
            {
                new () { RoomId = 1, StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(7), IsActive = true },
                new () { RoomId = 2, StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(7), IsActive = true }
            };
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.NotEqual(-1, roomId);
            Assert.Contains(roomId, rooms.Select(r => r.Id));
        }
        
        //6. Start and end dates overlap with the booking dates.
        [Fact]
        public void FindAvailableRoom_StartDateAndEndDateOverlapRoomBooking_ReturnsMinusOne()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(10);
            DateTime endDate = DateTime.Today.AddDays(12);

            // Mock room repository
            var rooms = new List<Room> { new() { Id = 1 }, new() { Id = 2 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (no conflicting bookings)
            var bookings = new List<Booking>
            {
                new () { RoomId = 1, StartDate = DateTime.Today.AddDays(10), EndDate = DateTime.Today.AddDays(12), IsActive = true },
                new () { RoomId = 2, StartDate = DateTime.Today.AddDays(10), EndDate = DateTime.Today.AddDays(12), IsActive = true }
            };
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(-1, roomId);
        }
        
        //7. Start date overlaps with a booking's period but not the end date.
        [Fact]
        public void FindAvailableRoom_StartDateOverlapRoomBooking_ReturnsMinusOne()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(10);
            DateTime endDate = DateTime.Today.AddDays(12);

            // Mock room repository
            var rooms = new List<Room> { new() { Id = 1 }, new() { Id = 2 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (no conflicting bookings)
            var bookings = new List<Booking>
            {
                new () { RoomId = 1, StartDate = DateTime.Today.AddDays(10), EndDate = DateTime.Today.AddDays(15), IsActive = true },
                new () { RoomId = 2, StartDate = DateTime.Today.AddDays(10), EndDate = DateTime.Today.AddDays(15), IsActive = true }
            };
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(-1, roomId);
        }
        
        //8. Dates entirely outside the booking window.
        [Fact]
        public void FindAvailableRoom_DatesOutsideRoomBooking_ReturnsValidRoomId()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(10);
            DateTime endDate = DateTime.Today.AddDays(12);

            // Mock room repository
            var rooms = new List<Room> { new() { Id = 1 }, new() { Id = 2 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (no conflicting bookings)
            var bookings = new List<Booking>
            {
                new () { RoomId = 1, StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(7), IsActive = true },
                new () { RoomId = 2, StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(7), IsActive = true }
            };
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.NotEqual(-1, roomId);
            Assert.Contains(roomId, rooms.Select(r => r.Id));
        }
        
        
        #endregion

        #region CreateBooking Tests

        [Fact]
        public void CreateBooking_RoomAvailable_BookingCreatedSuccessfully()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(10);
            DateTime endDate = DateTime.Today.AddDays(12);

            var booking = new Booking { StartDate = startDate, EndDate = endDate };

            // Mock room and booking repositories
            var rooms = new List<Room> { new () { Id = 1 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);
            mockBookingRepository.Setup(b => b.GetAll()).Returns(new List<Booking>());

            // Act
            var result = bookingManager.CreateBooking(booking);

            // Assert
            Assert.True(result);
            mockBookingRepository.Verify(b => b.Add(It.IsAny<Booking>()), Times.Once);
        }

        [Fact]
        public void CreateBooking_NoRoomAvailable_BookingNotCreated()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(10);
            DateTime endDate = DateTime.Today.AddDays(12);

            var booking = new Booking { StartDate = startDate, EndDate = endDate };

            // Mock room and booking repositories
            var rooms = new List<Room> { new () { Id = 1 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // All rooms booked
            var bookings = new List<Booking> 
            { 
                new () { RoomId = 1, StartDate = startDate, EndDate = endDate, IsActive = true }
            };
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            var result = bookingManager.CreateBooking(booking);

            // Assert
            Assert.False(result);
            mockBookingRepository.Verify(b => b.Add(It.IsAny<Booking>()), Times.Never);
        }

        #endregion

        #region GetFullyOccupiedDates Tests

        [Theory]
        [InlineData("2024-09-01", "2024-09-10", 10)]
        [InlineData("2024-09-11", "2024-09-15", 0)]
        public void GetFullyOccupiedDates_MultipleBookings_ReturnsCorrectOccupiedDates(string start, string end, int expectedOccupiedCount)
        {
            // Arrange
            DateTime startDate = DateTime.Parse(start);
            DateTime endDate = DateTime.Parse(end);

            var bookings = new List<Booking>
            {
                new () { RoomId = 1, StartDate = DateTime.Parse("2024-09-01"), EndDate = DateTime.Parse("2024-09-10"), IsActive = true },
                new () { RoomId = 2, StartDate = DateTime.Parse("2024-09-01"), EndDate = DateTime.Parse("2024-09-10"), IsActive = true }
            };

            // Mock repositories
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);
            mockRoomRepository.Setup(r => r.GetAll()).Returns(new List<Room> { new () { Id = 1 }, new () { Id = 2 } });

            // Act
            var fullyOccupiedDates = bookingManager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.Equal(expectedOccupiedCount, fullyOccupiedDates.Count);
        }
        
        [Fact]
        public void GetFullyOccupiedDates_StartDateAfterEndDate_ThrowsArgumentException()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(10);
            DateTime endDate = DateTime.Today.AddDays(5);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => bookingManager.GetFullyOccupiedDates(startDate, endDate));
            Assert.Equal("The start date cannot be later than the end date.", ex.Message);
        }

        #endregion

        #region GetFullyOccupiedDates (MCC) Tests
        
        //1: Full overlap with the booking dates
        [Fact]
        public void FindAvailableRoom_FullOverlap_ReturnsNegativeOne()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(5);
            DateTime endDate = DateTime.Today.AddDays(7);

            // Mock room repository
            var rooms = new List<Room> { new() { Id = 1 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (full overlap)
            var bookings = new List<Booking>
            {
                new () { RoomId = 1, StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(7), IsActive = true }
            };
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(-1, roomId); // Expect no available room
        }

        //2: Overlaps with the start date of the booking.
        [Fact]
        public void FindAvailableRoom_OverlapWithStartDate_ReturnsNegativeOne()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(4);
            DateTime endDate = DateTime.Today.AddDays(6);

            // Mock room repository
            var rooms = new List<Room> { new() { Id = 1 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (overlap with start date)
            var bookings = new List<Booking>
            {
                new () { RoomId = 1, StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(7), IsActive = true }
            };
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(-1, roomId); // Expect no available room
        }
        //3: Overlaps with the end date of the booking.
        [Fact]
        public void FindAvailableRoom_OverlapWithEndDate_ReturnsNegativeOne()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(6);
            DateTime endDate = DateTime.Today.AddDays(8);

            // Mock room repository
            var rooms = new List<Room> { new() { Id = 1 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (overlap with end date)
            var bookings = new List<Booking>
            {
                new () { RoomId = 1, StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(7), IsActive = true }
            };
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(-1, roomId); // Expect no available room
        }
        //4: Partial overlap (invalid scenario).
        [Fact]
        public void FindAvailableRoom_PartialOverlap_ReturnsNegativeOne()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(6);
            DateTime endDate = DateTime.Today.AddDays(8);

            // Mock room repository
            var rooms = new List<Room> { new() { Id = 1 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (partial overlap)
            var bookings = new List<Booking>
            {
                new () { RoomId = 1, StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(7), IsActive = true }
            };
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(-1, roomId); // Expect no available room
        }
        //5: Overlaps at the start, with an earlier end date.
        [Fact]
        public void FindAvailableRoom_OverlapAtStart_EarlierEndDate_ReturnsNegativeOne()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(5);
            DateTime endDate = DateTime.Today.AddDays(6);

            // Mock room repository
            var rooms = new List<Room> { new() { Id = 1 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (overlap at start, earlier end date)
            var bookings = new List<Booking>
            {
                new () { RoomId = 1, StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(7), IsActive = true }
            };
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(-1, roomId); // Expect no available room
        }
        //6: No overlap, completely outside the booking period.
        [Fact]
        public void FindAvailableRoom_NoOverlap_CompletelyOutsideBookingPeriod_ReturnsValidRoomId()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(8);
            DateTime endDate = DateTime.Today.AddDays(10);

            // Mock room repository
            var rooms = new List<Room> { new() { Id = 1 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (no overlap, completely outside booking period)
            var bookings = new List<Booking>
            {
                new () { RoomId = 1, StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(7), IsActive = true }
            };
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.NotEqual(-1, roomId); // Expect a valid room ID
            Assert.Contains(roomId, rooms.Select(r => r.Id));
        }
        //7: Overlaps at the end, starting after the booking begins.
        [Fact]
        public void FindAvailableRoom_OverlapAtEnd_ReturnsNegativeOne()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(4);
            DateTime endDate = DateTime.Today.AddDays(6);

            // Mock room repository
            var rooms = new List<Room> { new() { Id = 1 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (overlap at the end)
            var bookings = new List<Booking>
            {
                new () { RoomId = 1, StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(7), IsActive = true }
            };
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(-1, roomId); // Expect no available room
        }

        //8: No overlap, completely outside the booking period.
        [Fact]
        public void FindAvailableRoom_NoOverlap_BeforeBookingPeriod_ReturnsValidRoomId()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(1);
            DateTime endDate = DateTime.Today.AddDays(3);

            // Mock room repository
            var rooms = new List<Room> { new() { Id = 1 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (no overlap, before booking period)
            var bookings = new List<Booking>
    {
        new () { RoomId = 1, StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(7), IsActive = true }
    };
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.NotEqual(-1, roomId); // Expect a valid room ID
            Assert.Contains(roomId, rooms.Select(r => r.Id));
        }
        //9: No overlap, completely outside the booking period.
        [Fact]
        public void FindAvailableRoom_NoOverlap_AfterBookingPeriod_ReturnsValidRoomId()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(8);
            DateTime endDate = DateTime.Today.AddDays(10);

            // Mock room repository
            var rooms = new List<Room> { new() { Id = 1 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (no overlap, after booking period)
            var bookings = new List<Booking>
            {
                new () { RoomId = 1, StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(7), IsActive = true }
            };
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.NotEqual(-1, roomId); // Expect a valid room ID
            Assert.Contains(roomId, rooms.Select(r => r.Id));
        }
        //10: No overlap, completely outside the booking period.
        // Test cases 10-12 are similar to 8 and 9, with different dates
        /*
        [Fact]
        public void FindAvailableRoom_NoOverlap10_ReturnsValidRoomId()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(12);
            DateTime endDate = DateTime.Today.AddDays(15);

            // Mock room repository
            var rooms = new List<Room> { new() { Id = 1 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (no overlap)
            var bookings = new List<Booking>
            {
                new () { RoomId = 1, StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(7), IsActive = true }
            };
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.NotEqual(-1, roomId); // Expect a valid room ID
            Assert.Contains(roomId, rooms.Select(r => r.Id));
        }

        //11: No overlap, completely outside the booking period.
        [Fact]
        public void FindAvailableRoom_NoOverlap11_ReturnsValidRoomId()
        {
            // Arrange
            DateTime startDate = DateTime.Today;
            DateTime endDate = DateTime.Today.AddDays(1);

            // Mock room repository
            var rooms = new List<Room> { new() { Id = 1 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (no overlap)
            var bookings = new List<Booking>
            {
                new () { RoomId = 1, StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(7), IsActive = true }
            };
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.NotEqual(-1, roomId); // Expect a valid room ID
            Assert.Contains(roomId, rooms.Select(r => r.Id));
        }

        //12: No overlap, completely outside the booking period.
        [Fact]
        public void FindAvailableRoom_NoOverlap12_ReturnsValidRoomId()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(18);
            DateTime endDate = DateTime.Today.AddDays(20);

            // Mock room repository
            var rooms = new List<Room> { new() { Id = 1 } };
            mockRoomRepository.Setup(r => r.GetAll()).Returns(rooms);

            // Mock booking repository (no overlap)
            var bookings = new List<Booking>
            {
                new () { RoomId = 1, StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(7), IsActive = true }
            };
            mockBookingRepository.Setup(b => b.GetAll()).Returns(bookings);

            // Act
            int roomId = bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.NotEqual(-1, roomId); // Expect a valid room ID
            Assert.Contains(roomId, rooms.Select(r => r.Id));
        }
        */
        #endregion
    }
}
