using GITTUI.Models;
using System.Data;

namespace GITTUI.Views
{
    internal static class DataTableBuilder
    {
        public static DataTable BuildRepoTable(IEnumerable<GITRepositoryModel> repos)
        {
            var dt = new DataTable();
            dt.Columns.Add("Owner", typeof(string));
            dt.Columns.Add("Repository", typeof(string));

            foreach (var r in repos)
            {
                dt.Rows.Add(r.Owner, r.Name);
            }

            return dt;
        }

        public static DataTable BuildActivityTable(IEnumerable<GITActivityModel> activities)
        {
            var dt = new DataTable();
            dt.Columns.Add("Status", typeof(string));
            dt.Columns.Add("Workflow", typeof(string));
            dt.Columns.Add("Event", typeof(string));
            dt.Columns.Add("Date", typeof(string));
            dt.Columns.Add("Logs", typeof(string));

            foreach (var act in activities)
            {
                dt.Rows.Add(
                    act.StatusIcon,
                    act.WorkflowName,
                    act.Event.ToString().ToUpper(),
                    act.CreatedAt.ToString("g"),
                    act.HasLogs ? "[View]" : ""
                );
            }

            return dt;
        }

        public static (DataTable Table, List<WorkflowActivitySummary> Summaries) BuildSummaryTable(IEnumerable<GITActivityModel> intervalActivities)
        {
            var summaries = intervalActivities
                .GroupBy(a => a.CreatedAt.Date)
                .Select(g =>
                {
                    int success = 0, failure = 0, cancelled = 0, total = 0;
                    foreach (var a in g)
                    {
                        total++;
                        switch (a.Conclusion)
                        {
                            case WorkflowConclusion.Success: success++; break;
                            case WorkflowConclusion.Failure: failure++; break;
                            case WorkflowConclusion.Cancelled: cancelled++; break;
                        }
                    }
                    return new WorkflowActivitySummary
                    {
                        Date = g.Key,
                        SuccessCount = success,
                        FailureCount = failure,
                        CancelledCount = cancelled,
                        TotalRuns = total
                    };
                })
                .OrderBy(s => s.Date)
                .ToList();

            return (BuildSummaryDataTable(summaries), summaries);
        }

        private static DataTable BuildSummaryDataTable(List<WorkflowActivitySummary> summaries)
        {
            var dt = new DataTable();
            dt.Columns.Add("Date", typeof(string));
            dt.Columns.Add("Success", typeof(int));
            dt.Columns.Add("Failure", typeof(int));
            dt.Columns.Add("Cancelled", typeof(int));
            dt.Columns.Add("Total Runs", typeof(int));

            foreach (var s in summaries)
            {
                dt.Rows.Add(
                    s.Date.ToString("MM/dd"),
                    s.SuccessCount,
                    s.FailureCount,
                    s.CancelledCount,
                    s.TotalRuns
                );
            }

            return dt;
        }
    }
}
