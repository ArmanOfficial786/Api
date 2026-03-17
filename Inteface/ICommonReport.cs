namespace JsSampleReport.Inteface
{

    /// <summary>
    /// jsreport specific interface following RDLC pattern
    /// </summary>
    //public interface IJsReportService
    //{
    //    byte[] GenerateReport<T>(
    //        string reportPath,
    //        IEnumerable<T> data,
    //        string format,
    //        string datasetName,
    //        Dictionary<string, string>? parameters = null,
    //        Dictionary<string, object>? subreportData = null)
    //        where T : class;
    //}


    public interface IJsReportService
    {
        /// <summary>
        /// Generate report from HTML template
        /// </summary>
        /// <param name="reportPath">Path to HTML template file</param>
        /// <param name="data">Report data (can be object, dictionary, or list)</param>
        /// <param name="format">Output format (PDF, EXCEL, WORD, HTML, PNG)</param>
        /// <returns>Report as byte array</returns>
        byte[] GenerateReport(
            string reportPath,
            object data,
            string format);
    }
}
