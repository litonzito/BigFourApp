using BigFourApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace BigFourApp.Persistence
{
    public class EventRepository : IEventRepository
    {
        private readonly BaseDatos _context;

        public EventRepository(BaseDatos context)
        {
            _context = context;
        }
        public void EnsureCreated()
        {
            _context.Database.EnsureCreated();
        }


    }
}
