using Data.Models;
using System.ComponentModel;

namespace Geonicle.Data.Services
{
    /// <summary>
    /// Вариант (кондиция) выбранного пласта для списка «Вариант» на ленте: строка SeamConds с названием кондиции.
    /// Правится только «Ссылка на вариант» (SeamConds.RealCondId); правка сохраняется вместе с экраном.
    /// </summary>
    public class VariantRow : INotifyPropertyChanged
    {
        private readonly SeamCond def;

        public VariantRow(SeamCond def, int number, string name)
        {
            this.def = def;
            Number = number;
            Name = name;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Код кондиции (Condition.Id): по нему отбираются блоки.</summary>
        public int ConditionId => def.ConditionId;

        /// <summary>Номер варианта (Condition.Variant).</summary>
        public int Number { get; }

        /// <summary>Название варианта (Condition.Variant1).</summary>
        public string Name { get; }

        /// <summary>Код фактической кондиции, на которую ссылается вариант (список — кондиции того же прослоя).</summary>
        public int? RealCondId
        {
            get => def.RealCondId;
            set
            {
                if (def.RealCondId == value)
                    return;
                def.RealCondId = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RealCondId)));
            }
        }
    }
}
