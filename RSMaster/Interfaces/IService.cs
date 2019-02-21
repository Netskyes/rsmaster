using System.Threading.Tasks;

namespace RSMaster.Interfaces
{
    public interface IService
    {
        string Name { get; set; }
        string Description { get; set; }
        string LastError { get; set; }
        bool IsRunning { get; set; }
        Task<bool> Start();
        void Stop();
    }
}
