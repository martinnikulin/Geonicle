using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geonicle.Wpf.Interfaces
{
    public interface ISelectedViewModel
    {
        //ValueTask DisposeAsync();

        Task LoadAsync();

        Task SaveAsync();
    }
}
