using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Geonicle.Data.Services
{
    /// <summary>
    /// Строка формы «Анализы» пробы: один актуальный показатель (AssayVars) с его группой и значением пробы (Assays.Val).
    /// Правится только значение; остальное справочное (приходит из представлений AssayVars и AssayGroups).
    /// </summary>
    public class AssayRow : INotifyPropertyChanged
    {
        private string? val;

        public AssayRow(int varId, double n, string description, int groupId, string groupName, int groupN, string? val)
        {
            VarId = varId;
            N = n;
            Description = description;
            GroupId = groupId;
            GroupName = groupName;
            GroupN = groupN;
            this.val = val;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public int VarId { get; }

        /// <summary>Номер показателя (AssayVars.N), только для чтения.</summary>
        public double N { get; }

        /// <summary>Название показателя («Ad, %», «Aa, %» …).</summary>
        public string Description { get; }

        public int GroupId { get; }

        public string GroupName { get; }

        /// <summary>Порядок группы (AssayGroups.N): по нему сортируются группы.</summary>
        public int GroupN { get; }

        /// <summary>Значение показателя у пробы (Assays.Val); пусто — показателя у пробы нет.</summary>
        public string? Val
        {
            get => val;
            set
            {
                if (val == value)
                    return;
                val = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Val)));
            }
        }
    }
}
