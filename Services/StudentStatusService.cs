using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using talim_platforma.Data;

namespace talim_platforma.Services
{
    public class StudentStatusService : IStudentStatusService
    {
        private readonly ApplicationDbContext _context;
        private static readonly string[] AbsentStatuses = new[] { "Kelmadi" };

        public StudentStatusService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RefreshStudentStatusAsync(int talabaId, int guruhId)
        {
            var talaba = await _context.Foydalanuvchilar.FirstOrDefaultAsync(f => f.Id == talabaId);
            if (talaba == null)
            {
                return;
            }

            var lastThree = await _context.Davomatlar
                .Where(d => d.TalabaId == talabaId && d.GuruhId == guruhId)
                .OrderByDescending(d => d.Sana)
                .Take(3)
                .ToListAsync();

            if (lastThree.Count < 3)
            {
                talaba.Faolmi = true;
            }
            else
            {
                var lastThreeIds = lastThree.Select(d => d.Id).ToList();
                var reasonIds = await _context.DavomatSabablar
                    .Where(ds => lastThreeIds.Contains(ds.DavomatId))
                    .Select(ds => ds.DavomatId)
                    .ToListAsync();

                var threeMissed = lastThree.All(r =>
                    AbsentStatuses.Contains(r.Holati, StringComparer.OrdinalIgnoreCase));
                var anyReason = reasonIds.Any();

                talaba.Faolmi = !(threeMissed && !anyReason);
            }

            await _context.SaveChangesAsync();
        }

        public async Task RefreshGroupStatusesAsync(int guruhId)
        {
            var talabaIds = await _context.TalabaGuruhlar
                .Where(tg => tg.GuruhId == guruhId)
                .Select(tg => tg.TalabaId)
                .Distinct()
                .ToListAsync();

            foreach (var talabaId in talabaIds)
            {
                await RefreshStudentStatusAsync(talabaId, guruhId);
            }
        }
    }
}

