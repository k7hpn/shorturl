using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ShortURL.Data
{
    public class Update
    {
        private const string RecordIdParameter = "@RecordId";
        private const string GroupIdParameter = "@GroupId";

        private const string VisitQuery =
            "BEGIN TRANSACTION;"
            + "UPDATE [Records] SET [Visits] += 1, [LatestVisit] = GETDATE() WHERE [RecordId] = " + RecordIdParameter + ";"
            + "INSERT INTO [RecordVisits] (VisitedAt, RecordId) VALUES(GETDATE(), " + RecordIdParameter + ");"
            + "COMMIT TRANSACTION";

        private const string GroupQuery =
            "BEGIN TRANSACTION;"
            + "UPDATE [Groups] SET [Visits] += 1, [LatestVisit] = GETDATE() WHERE [GroupId] = " + GroupIdParameter + ";"
            + "INSERT INTO [GroupVisits] (VisitedAt, GroupId) VALUES(GETDATE(), " + GroupIdParameter + ");"
            + "COMMIT TRANSACTION";

        private readonly Context _context;

        public Update(Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task UpdateRecordVisitAsync(int recordId)
        {
            var parameter = new System.Data.SqlClient.SqlParameter(RecordIdParameter, recordId);
            await _context.Database.ExecuteSqlCommandAsync(VisitQuery, parameter);
        }

        public async Task UpdateGroupVisitAsync(int groupId)
        {
            var parameter = new System.Data.SqlClient.SqlParameter(GroupIdParameter, groupId);
            await _context.Database.ExecuteSqlCommandAsync(GroupQuery, parameter);
        }
    }
}
