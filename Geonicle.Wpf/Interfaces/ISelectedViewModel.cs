using System.Threading.Tasks;

namespace Geonicle.Wpf.Interfaces
{
    public interface ISelectedViewModel
    {
        /// <summary>Есть ли несохранённые изменения на экране.</summary>
        bool HasChanges { get; }

        Task LoadAsync();

        Task SaveAsync();
    }
}
