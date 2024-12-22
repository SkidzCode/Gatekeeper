using System.Collections.Generic;
using System.Threading.Tasks;
using GateKeeper.Server.Models.Account;

namespace GateKeeper.Server.Interface
{
    /// <summary>
    /// Interface for user analytics and reporting operations.
    /// </summary>
    public interface IUserAnalyticsService
    {
        /// <summary>
        /// Tracks user login frequency and activity patterns.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A task that returns the user's activity analytics.</returns>
        Task<UserActivityAnalytics> GetUserActivityAsync(int userId);

        /// <summary>
        /// Retrieves aggregated user login and activity statistics.
        /// </summary>
        /// <returns>A task that returns global activity analytics.</returns>
        Task<GlobalActivityAnalytics> GetGlobalActivityAnalyticsAsync();

        /// <summary>
        /// Generates a report on user demographics.
        /// </summary>
        /// <returns>A task that returns a demographics report.</returns>
        Task<DemographicsReport> GenerateDemographicsReportAsync();

        /// <summary>
        /// Generates a report on user activity.
        /// </summary>
        /// <returns>A task that returns an activity report.</returns>
        Task<ActivityReport> GenerateActivityReportAsync();

        /// <summary>
        /// Generates a report on security incidents.
        /// </summary>
        /// <returns>A task that returns a security incidents report.</returns>
        Task<SecurityIncidentReport> GenerateSecurityIncidentReportAsync();

        /// <summary>
        /// Retrieves a summary of user activity over a specified time period.
        /// </summary>
        /// <param name="startDate">The start date for the period.</param>
        /// <param name="endDate">The end date for the period.</param>
        /// <returns>A task that returns a summary of user activity.</returns>
        Task<UserActivitySummary> GetUserActivitySummaryAsync(string startDate, string endDate);
    }
}
