namespace Berryfy.Application.Dtos.AuthDtos.Responses
{
    public class BulkAssignmentResult
    {
        public int TotalUsers { get; set; }
        public int SuccessfulAssignments { get; set; }
        public int FailedAssignments { get; set; }
        public List<string> FailedUserIds { get; set; }
        public List<string> ErrorMessages { get; set; }
        public bool IsSuccess => FailedAssignments == 0;
    }
}