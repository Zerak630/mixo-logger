using Domain.MyBar;

namespace Domain.Interfaces.Repositories;
public interface IBarRepository
{
    Task<Bar> GetBar();
}