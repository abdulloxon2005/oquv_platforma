using System.Threading.Tasks;

namespace talim_platforma.Services
{
    public interface IStudentStatusService
    {
        Task RefreshStudentStatusAsync(int talabaId, int guruhId);
        Task RefreshGroupStatusesAsync(int guruhId);
    }
}



