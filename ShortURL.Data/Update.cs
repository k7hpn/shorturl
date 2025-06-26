using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ShortURL.Data
{
    public class Update
    {
        private readonly Context _context;

        public Update(Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task UpdateGroupVisitAsync(int groupId)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($"BEGIN TRANSACTION; UPDATE [Groups] SET [Visits] += 1, [LatestVisit] = GETDATE() WHERE [GroupId] = {groupId}; INSERT INTO [GroupVisits] (VisitedAt, GroupId) VALUES(GETDATE(), {groupId}); COMMIT TRANSACTION");
        }

        public async Task UpdateRecordVisitAsync(int recordId)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($"BEGIN TRANSACTION; UPDATE [Records] SET [Visits] += 1, [LatestVisit] = GETDATE() WHERE [RecordId] = {recordId}; INSERT INTO [RecordVisits] (VisitedAt, RecordId) VALUES(GETDATE(), {recordId}); COMMIT TRANSACTION");
        }
    }
}